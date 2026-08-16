using System;
using System.Collections.Generic;

namespace Elyndor.Persistence
{
    /// <summary>
    /// Der gespeicherte Fortschritt — fachlicher Zustand, sonst nichts.
    ///
    /// Bewusst <b>keine</b> Szene, <b>keine</b> GameObjects und <b>keine</b>
    /// Transform-Hierarchie. Was hier steht, ist das, was der Spieler
    /// erreicht hat; wo die Welt das anzeigt, weiss die Welt selbst.
    ///
    /// Bewusst flach und aus einfachen Feldern: <c>JsonUtility</c> kann keine
    /// Dictionaries und keine Schnittstellen, und ein Format, das sich in
    /// jedem Texteditor lesen laesst, ist bei einem kaputten Spielstand mehr
    /// wert als ein elegantes.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>
        /// Die Version dieses Formats. Ab Tag eins vorhanden, damit ein
        /// spaeterer Wechsel nicht raten muss, was er vor sich hat.
        /// </summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// Version der vorliegenden Daten. Ein hoeherer Wert stammt aus einer
        /// neueren Fassung des Spiels und wird nicht blind geladen.
        /// </summary>
        public int saveVersion = CurrentVersion;

        /// <summary>Bereits gesehene Erinnerungsorte.</summary>
        public List<string> memorySiteIds = new List<string>();

        /// <summary>Stand der Bruecken-Raetsel, je ein Eintrag.</summary>
        public List<BridgePuzzleEntry> bridgePuzzles = new List<BridgePuzzleEntry>();

        /// <summary>Regionsabschnitte, die bereits geantwortet haben.</summary>
        public List<string> regeneratedRegionIds = new List<string>();

        /// <summary>
        /// Der Ort, an dem ein spaeteres Laden den Spieler absetzen soll.
        ///
        /// <b>In P1.13A bewusst leer.</b> Das Feld existiert, damit P1.13B es
        /// fuellen kann, ohne das Format zu wechseln. Welcher Punkt das sein
        /// soll, ist eine Leveldesign-Entscheidung und wurde nicht getroffen —
        /// und ein geratener Startpunkt waere schlimmer als gar keiner.
        /// </summary>
        public string spawnId = string.Empty;

        /// <summary>Ein Raetsel und wie weit es gediehen ist.</summary>
        [Serializable]
        public sealed class BridgePuzzleEntry
        {
            public string puzzleId = string.Empty;

            /// <summary>
            /// Der Zustandsname, nicht seine Zahl. Eine Zahl haette sich
            /// beim Umsortieren der Aufzaehlung still verschoben und alte
            /// Spielstaende auf einen falschen Zustand gesetzt.
            /// </summary>
            public string state = string.Empty;
        }

        /// <summary>Ein leerer, gueltiger Spielstand: nichts erreicht.</summary>
        public static SaveData Empty()
        {
            return new SaveData();
        }
    }
}
