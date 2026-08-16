using System;
using System.Collections.Generic;
using Elyndor.Memory;
using Elyndor.Puzzles;
using Elyndor.World;
using UnityEngine;

namespace Elyndor.Persistence
{
    /// <summary>
    /// Haelt den Fortschritt ueber das Programmende hinaus.
    ///
    /// Der Dienst sitzt zwischen den drei Sitzungszustaenden und einem
    /// <see cref="ISaveStore"/>. Keine Szene und kein Spielsystem weiss, dass
    /// es ihn gibt: Erinnerungen, Raetsel und Regionen melden nur, dass sich
    /// etwas geaendert hat, und er entscheidet, was damit geschieht.
    ///
    /// <b>Er ist ausdruecklich nicht lebensnotwendig.</b> Faellt das
    /// Speichern aus — kein Schreibrecht, volle Platte, kaputte Datei —, dann
    /// laeuft das Spiel weiter und verliert nur seinen Fortschritt. Ein
    /// Speichersystem, ohne das sich das Spiel nicht mehr starten laesst,
    /// waere ein schlechterer Zustand als gar keines.
    /// </summary>
    public sealed class SaveService : IRegionStateStore
    {
        private readonly ISaveStore store;

        private SaveData data = SaveData.Empty();

        /// <summary>
        /// Waehrend des Wiederherstellens werden die Sitzungszustaende
        /// beschrieben, und die melden das pflichtgemaess als Aenderung.
        /// Ohne diese Sperre schriebe jedes Laden sofort wieder zurueck.
        /// </summary>
        private bool restoring;

        private bool subscribed;

        public SaveService(ISaveStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>Der laufende Dienst, oder <c>null</c>.</summary>
        public static SaveService Current { get; private set; }

        /// <summary>Laeuft ueberhaupt einer?</summary>
        public static bool IsInstalled => Current != null;

        /// <summary>Ergebnis des letzten Ladeversuchs.</summary>
        public LoadOutcome LastOutcome { get; private set; } = LoadOutcome.NoSave;

        /// <summary>
        /// Haengt einen Dienst ein und ersetzt einen etwaigen vorherigen.
        /// Tests benutzen das, um einen temporaeren Speicher unterzuschieben.
        /// </summary>
        public static void Install(SaveService service)
        {
            Current?.Detach();
            Current = service;
            Current?.Attach();
        }

        /// <summary>Haengt den laufenden Dienst aus. Danach speichert nichts mehr.</summary>
        public static void Uninstall()
        {
            Current?.Detach();
            Current = null;
            RegionRegenerationState.Attach(null);
        }

        // ------------------------------------------------------------------
        // Laden und Wiederherstellen
        // ------------------------------------------------------------------

        /// <summary>
        /// Liest den Spielstand und stellt ihn in den Sitzungszustaenden her.
        ///
        /// Wirft nie. Was schiefgehen kann, steht im Rueckgabewert und im Log.
        /// </summary>
        public LoadOutcome LoadAndRestore()
        {
            data = SaveData.Empty();
            LastOutcome = ReadData(out SaveData loaded);

            if (LastOutcome == LoadOutcome.Loaded ||
                LastOutcome == LoadOutcome.RecoveredFromBackup)
            {
                data = loaded;
            }

            Restore();

            return LastOutcome;
        }

        private LoadOutcome ReadData(out SaveData loaded)
        {
            loaded = null;

            string raw;

            try
            {
                if (!store.Exists)
                {
                    return LoadOutcome.NoSave;
                }

                raw = store.Read();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Speicherstand nicht lesbar: {exception.Message}");
                raw = null;
            }

            if (TryParse(raw, out loaded, out bool fromNewerVersion))
            {
                return LoadOutcome.Loaded;
            }

            if (fromNewerVersion)
            {
                // Nicht laden und vor allem nicht ueberschreiben: der Stand
                // gehoert einer neueren Fassung, und die weiss mehr ueber ihn
                // als wir.
                Debug.LogWarning(
                    "Speicherstand stammt aus einer neueren Fassung des " +
                    "Spiels und wird nicht geladen. Er bleibt unveraendert.");
                return LoadOutcome.FromNewerVersion;
            }

            string backup;

            try
            {
                backup = store.ReadBackup();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Sicherung nicht lesbar: {exception.Message}");
                backup = null;
            }

            if (TryParse(backup, out loaded, out _))
            {
                Debug.LogWarning(
                    "Speicherstand war beschaedigt; die Sicherung wurde " +
                    "verwendet.");
                return LoadOutcome.RecoveredFromBackup;
            }

            Debug.LogWarning(
                "Speicherstand und Sicherung sind beschaedigt. Es wird ohne " +
                "Fortschritt begonnen; die Dateien bleiben zur Untersuchung " +
                "liegen.");

            return LoadOutcome.Corrupt;
        }

        private static bool TryParse(
            string raw, out SaveData parsed, out bool fromNewerVersion)
        {
            parsed = null;
            fromNewerVersion = false;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            SaveData candidate;

            try
            {
                candidate = JsonUtility.FromJson<SaveData>(raw);
            }
            catch (Exception)
            {
                return false;
            }

            // JsonUtility liefert bei Unfug gern ein Objekt mit lauter
            // Standardwerten statt einer Ausnahme. Version 0 heisst deshalb
            // hier: das war kein Spielstand von uns.
            if (candidate == null || candidate.saveVersion <= 0)
            {
                return false;
            }

            if (candidate.saveVersion > SaveData.CurrentVersion)
            {
                fromNewerVersion = true;
                return false;
            }

            candidate.memorySiteIds ??= new List<string>();
            candidate.bridgePuzzles ??= new List<SaveData.BridgePuzzleEntry>();
            candidate.regeneratedRegionIds ??= new List<string>();

            parsed = candidate;
            return true;
        }

        /// <summary>
        /// Traegt die geladenen Daten in die Sitzungszustaende ein.
        ///
        /// <b>Wiederherstellen ist kein Nacherleben.</b> Es wird nur der
        /// Zustand gesetzt; die Welt liest ihn beim Aufbau und zeigt ihn an.
        /// Kein Ereignis wird nachtraeglich gefeuert, kein Ton gespielt,
        /// keine Pflanze ein zweites Mal gesetzt. Genau dafuer trennt
        /// <c>FinsterwaldRegeneration</c> ihr <c>Apply</c> von ihrem
        /// <c>Regenerate</c>.
        /// </summary>
        private void Restore()
        {
            restoring = true;

            // Ueber Kopien laufen. Das Eintragen schreibt ueber den
            // Regionsvertrag in dieselben Listen zurueck, ueber die hier
            // gelaufen wird — ohne Kopie waere das ein Absturz mitten im
            // Laden, und zwar nur dann, wenn tatsaechlich Fortschritt
            // vorhanden ist.
            List<string> memorySites = new List<string>(data.memorySiteIds);
            List<SaveData.BridgePuzzleEntry> puzzles =
                new List<SaveData.BridgePuzzleEntry>(data.bridgePuzzles);
            List<string> regions = new List<string>(data.regeneratedRegionIds);

            try
            {
                MemorySessionState.ForgetAll();
                PuzzleSessionState.ForgetAll();
                RegionRegenerationState.ForgetAll();

                foreach (string siteId in memorySites)
                {
                    MemorySessionState.MarkActivated(siteId);
                }

                foreach (SaveData.BridgePuzzleEntry entry in puzzles)
                {
                    if (string.IsNullOrEmpty(entry?.puzzleId))
                    {
                        continue;
                    }

                    if (Enum.TryParse(entry.state, out BridgePuzzleState state))
                    {
                        PuzzleSessionState.SetBridgeState(entry.puzzleId, state);
                        continue;
                    }

                    Debug.LogWarning(
                        $"Unbekannter Raetselzustand '{entry.state}' fuer " +
                        $"'{entry.puzzleId}'; der Eintrag wird uebergangen.");
                }

                foreach (string regionId in regions)
                {
                    RegionRegenerationState.MarkRegenerated(regionId);
                }
            }
            finally
            {
                restoring = false;
            }
        }

        // ------------------------------------------------------------------
        // IRegionStateStore — der Vertrag aus P1.10
        // ------------------------------------------------------------------

        /// <summary>
        /// Bedient den Vertrag, den P1.10 fuer genau diesen Tag angelegt hat.
        ///
        /// Der Regionszustand ist damit der einzige der drei, der zusaetzlich
        /// durchgeschrieben wird statt nur eingesammelt zu werden. Das ist
        /// Absicht: der Vertrag war da, und ihn ungenutzt liegen zu lassen
        /// hiesse, ihn beim naechsten Mal fuer tot zu halten.
        /// </summary>
        bool IRegionStateStore.IsRegenerated(string regionStateId)
        {
            return !string.IsNullOrEmpty(regionStateId) &&
                   data.regeneratedRegionIds.Contains(regionStateId);
        }

        void IRegionStateStore.SetRegenerated(string regionStateId, bool value)
        {
            if (string.IsNullOrEmpty(regionStateId))
            {
                return;
            }

            if (value)
            {
                if (!data.regeneratedRegionIds.Contains(regionStateId))
                {
                    data.regeneratedRegionIds.Add(regionStateId);
                }

                return;
            }

            data.regeneratedRegionIds.Remove(regionStateId);
        }

        // ------------------------------------------------------------------
        // Einsammeln und Schreiben
        // ------------------------------------------------------------------

        /// <summary>
        /// Liest die Sitzungszustaende aus und schreibt sie weg.
        ///
        /// Wirft nie. Ein fehlgeschlagenes Speichern kostet Fortschritt, aber
        /// niemals die laufende Sitzung.
        /// </summary>
        public bool Save()
        {
            Capture();

            try
            {
                store.Write(JsonUtility.ToJson(data, true));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Fortschritt konnte nicht gespeichert werden: " +
                    $"{exception.Message}");
                return false;
            }
        }

        private void Capture()
        {
            data.saveVersion = SaveData.CurrentVersion;

            data.memorySiteIds = new List<string>(
                MemorySessionState.ActivatedSiteIds);

            data.regeneratedRegionIds = new List<string>(
                RegionRegenerationState.RegeneratedRegionIds);

            data.bridgePuzzles = new List<SaveData.BridgePuzzleEntry>();

            foreach (KeyValuePair<string, BridgePuzzleState> pair in
                     PuzzleSessionState.BridgeStates)
            {
                data.bridgePuzzles.Add(new SaveData.BridgePuzzleEntry
                {
                    puzzleId = pair.Key,
                    state = pair.Value.ToString()
                });
            }
        }

        /// <summary>
        /// Loescht den Spielstand und allen laufenden Fortschritt.
        ///
        /// Das ist „Neues Spiel" auf der Ebene der Daten. Ein Menue dafuer
        /// gibt es bewusst noch nicht.
        /// </summary>
        public void ResetProgress()
        {
            restoring = true;

            try
            {
                MemorySessionState.ForgetAll();
                PuzzleSessionState.ForgetAll();
                RegionRegenerationState.ForgetAll();
            }
            finally
            {
                restoring = false;
            }

            data = SaveData.Empty();

            try
            {
                store.Delete();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Spielstand konnte nicht geloescht werden: " +
                    $"{exception.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Anschluss an die Sitzungszustaende
        // ------------------------------------------------------------------

        private void Attach()
        {
            if (subscribed)
            {
                return;
            }

            MemorySessionState.Changed += HandleProgress;
            PuzzleSessionState.Changed += HandleProgress;
            RegionRegenerationState.Changed += HandleProgress;

            // Der Vertrag aus P1.10, endlich mit jemandem dahinter.
            RegionRegenerationState.Attach(this);

            subscribed = true;
        }

        private void Detach()
        {
            if (!subscribed)
            {
                return;
            }

            MemorySessionState.Changed -= HandleProgress;
            PuzzleSessionState.Changed -= HandleProgress;
            RegionRegenerationState.Changed -= HandleProgress;

            subscribed = false;
        }

        /// <summary>
        /// Speichert nach einem bestaetigten Fortschritt — nicht pro Bild.
        ///
        /// Die Zustaende melden sich nur bei stabilen Uebergaengen, also
        /// hoechstens ein paar Mal je Sitzung. Bei dieser Haeufigkeit ist
        /// sofortiges Schreiben die einfachste verlaessliche Loesung; alles
        /// mit Verzoegerung verloere beim Absturz genau das Ereignis, dessen
        /// Verlust am meisten schmerzt.
        /// </summary>
        private void HandleProgress()
        {
            if (restoring)
            {
                return;
            }

            Save();
        }
    }
}
