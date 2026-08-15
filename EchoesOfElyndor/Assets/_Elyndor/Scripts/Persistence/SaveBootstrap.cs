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
    /// Der Zeitpunkt ist <c>AfterSceneLoad</c> und damit nach dem
    /// Zuruecksetzen der Sitzungszustaende, die auf
    /// <c>SubsystemRegistration</c> laufen. Umgekehrt haette das Laden seinen
    /// eigenen Fortschritt wieder geloescht.
    ///
    /// <b>Ein bereits eingehaengter Dienst wird nicht ersetzt.</b> Daran
    /// haengt die Testbarkeit: die Testreihe haengt ihren eigenen,
    /// temporaeren Speicher ein, und dieser Start hier haelt sich dann heraus,
    /// statt ihn gegen den echten Spielstand auszutauschen.
    /// </summary>
    public static class SaveBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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
