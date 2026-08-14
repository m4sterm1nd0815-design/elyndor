using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Elyndor.UIFoundation;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Prueft die HUD-Grundlage einer Szene.
    ///
    /// Die Pruefung lief bisher ausschliesslich gegen die gerade offene Szene
    /// und hatte keinen Einstiegspunkt mit Exit-Code. Im Batchmode ist beim
    /// Start keine Szene geladen, die Pruefung fand also nichts und meldete
    /// trotzdem Erfolg. Deshalb laedt sie ihre Szene jetzt selbst.
    /// </summary>
    public static class HudPolishValidator
    {
        /// <summary>
        /// Nur Finsterwald traegt derzeit das HUD-Fundament. Sonnenfelder und
        /// Nebelmoor fuehren bewusst weder Vitals noch Quickslots und wuerden
        /// hier zu Recht durchfallen — sie werden ueber ihr eigenes
        /// Szenenintegritaets-Profil geprueft.
        /// </summary>
        public const string DefaultScenePath = RegionScenes.Finsterwald;

        [MenuItem("Elyndor/QA/Validate Polished HUD")]
        public static void Validate()
        {
            Debug.Log(RunChecks(out int errors));

            if (errors > 0)
            {
                Debug.LogError($"Polished HUD QA: {errors} blocking error(s).");
            }
        }

        /// <summary>Einstiegspunkt fuer den Batchmode.</summary>
        public static void ValidateBatch()
        {
            int errors = ValidateScene(DefaultScenePath);
            EditorApplication.Exit(errors == 0 ? 0 : 1);
        }

        /// <summary>
        /// Laedt die angegebene Szene und prueft sie. Liefert die Anzahl
        /// blockierender Fehler, damit Tests und Batchmode dasselbe auswerten.
        /// </summary>
        public static int ValidateScene(string scenePath)
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            if (scene.path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            string report = RunChecks(out int errors);

            if (errors > 0)
            {
                Debug.LogError($"{report}\n{errors} blockierende(r) Fehler in '{scenePath}'.");
                return errors;
            }

            Debug.Log(report);
            return 0;
        }

        /// <summary>
        /// Die eigentliche Pruefung auf den gerade geladenen Szenen.
        /// </summary>
        public static string RunChecks(out int errors)
        {
            errors = 0;
            StringBuilder report = new StringBuilder();

            HudFoundationMarker[] huds = Object.FindObjectsByType<HudFoundationMarker>(
                FindObjectsInactive.Include);

            if (huds.Length == 0)
            {
                report.AppendLine("Kein Elyndor HUD-Fundament in der Szene.");
                errors++;
            }

            QuickslotFocusVisual[] focusVisuals =
                Object.FindObjectsByType<QuickslotFocusVisual>(
                    FindObjectsInactive.Include);

            if (focusVisuals.Length != 8)
            {
                report.AppendLine(
                    $"Erwartet waren 8 Quickslot-Fokusvisuals, gefunden {focusVisuals.Length}.");
            }

            if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            {
                report.AppendLine("Kein EventSystem in der Szene.");
                errors++;
            }

            report.Append(
                $"Polished HUD QA: {huds.Length} HUDs, " +
                $"{focusVisuals.Length} Quickslots, {errors} blockierende Fehler.");

            return report.ToString();
        }
    }
}
