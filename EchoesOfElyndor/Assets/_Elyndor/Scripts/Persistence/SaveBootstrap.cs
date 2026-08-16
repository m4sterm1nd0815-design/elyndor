using System;
using UnityEngine;

namespace Elyndor.Persistence
{
    /// <summary>
    /// Startet das Speichersystem, ohne dass eine Szene davon weiss.
    ///
    /// Bewusst ueber <see cref="RuntimeInitializeOnLoadMethod"/> statt ueber
    /// ein Objekt in der Szene: der Vertical Slice ist menschlich abgenommen,
    /// und Persistenz nachzuruesten darf ihn nicht anfassen. Es gibt deshalb
    /// kein neues Prefab, keinen neuen Eintrag in einer Szene und keine
    /// zusaetzliche Referenz, die jemand versehentlich loesen koennte.
    ///
    /// Der Zeitpunkt ist <c>BeforeSceneLoad</c>: nach dem Zuruecksetzen der
    /// Sitzungszustaende, die auf <c>SubsystemRegistration</c> laufen —
    /// umgekehrt haette das Laden seinen eigenen Fortschritt wieder geloescht
    /// — und <b>vor</b> dem <c>Awake</c> der ersten Szene.
    ///
    /// <b>Warum nicht <c>AfterSceneLoad</c>, wie zuerst gebaut.</b> Die Welt
    /// liest den wiederhergestellten Stand beim Aufbau: <c>BridgePuzzle</c>
    /// und <c>FinsterwaldRegeneration</c> tun das in ihrem <c>Awake</c>.
    /// <c>AfterSceneLoad</c> laeuft nach genau diesem <c>Awake</c> — der
    /// Spielstand kam also erst an, als die Szene ihn schon gelesen hatte.
    /// Im Ergebnis stand die Bruecke nach jedem echten Programmstart wieder
    /// auf Anfang, waehrend die Datei den Fortschritt korrekt enthielt; der
    /// naechste stabile Uebergang schrieb den Stand dann obendrein zurueck.
    /// Aufgefallen ist das keinem Test, weil jeder Test seinen Spielstand vor
    /// dem Laden der Szene herstellt — eine Reihenfolge, die das Spiel selbst
    /// nie hatte.
    ///
    /// <b>Ein bereits eingehaengter Dienst wird nicht ersetzt.</b> Daran
    /// haengt die Testbarkeit: die Testreihe haengt ihren eigenen,
    /// temporaeren Speicher ein, und dieser Start hier haelt sich dann heraus,
    /// statt ihn gegen den echten Spielstand auszutauschen.
    /// </summary>
    public static class SaveBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (SaveService.IsInstalled)
            {
                return;
            }

            try
            {
                SaveService service = new SaveService(new FileSaveStore());
                SaveService.Install(service);

                LoadOutcome outcome = service.LoadAndRestore();

                if (outcome != LoadOutcome.NoSave &&
                    outcome != LoadOutcome.Loaded)
                {
                    Debug.LogWarning($"Spielstand geladen mit: {outcome}.");
                }
            }
            catch (Exception exception)
            {
                // Ohne Speichern laesst sich spielen; ohne Start nicht.
                Debug.LogWarning(
                    "Das Speichersystem konnte nicht starten. Das Spiel " +
                    $"laeuft ohne Fortschrittssicherung weiter: {exception.Message}");

                SaveService.Uninstall();
            }
        }
    }
}
