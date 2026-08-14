using System.Collections.Generic;
using System.Text;
using Elyndor.Core;
using Elyndor.Player;
using Elyndor.UI;
using Elyndor.UIFoundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools.SceneIntegrity
{
    /// <summary>
    /// Wendet den <see cref="SceneIntegrityAnalyzer"/> auf die Finsterwald-Szene
    /// an. Das Profil beschreibt, was der Vertical Slice dort tatsaechlich
    /// braucht — sowohl die neue HUD-Grundlage als auch die Erlebnisschicht.
    /// </summary>
    public static class FinsterwaldSceneIntegrityValidator
    {
        public const string FinsterwaldScenePath =
            "Assets/_Elyndor/Scenes/Finsterwald.unity";

        /// <summary>
        /// Beschreibt die Finsterwald-Anforderungen. Oeffentlich, damit die
        /// EditMode-Tests exakt dasselbe Profil pruefen, das auch im Editor und
        /// im Batchmode laeuft — sonst testeten wir etwas anderes als wir
        /// ausliefern.
        /// </summary>
        public static SceneIntegrityProfile CreateProfile()
        {
            return new SceneIntegrityProfile("Finsterwald")
                .RequireActive(
                    // Eingabe
                    typeof(PlayerInputReader),
                    // Neue HUD-Grundlage
                    typeof(HudVitalsPresenter),
                    typeof(QuickslotBarPresenter),
                    // Sichtbare Erlebnisschicht
                    typeof(InteractionPromptUI),
                    typeof(NarrationUI),
                    typeof(MemoryWatchActivationUI),
                    typeof(CompassUI),
                    typeof(InventoryUI),
                    // Nicht-visuelle Erlebnisschicht
                    typeof(IntroSequence),
                    typeof(TutorialSequence),
                    typeof(CombatTutorial),
                    typeof(SfxLibrary))
                .RequireSingleInstance(
                    typeof(PlayerInputReader),
                    typeof(HudVitalsPresenter),
                    typeof(QuickslotBarPresenter),
                    typeof(InteractionPromptUI),
                    typeof(NarrationUI),
                    typeof(MemoryWatchActivationUI),
                    typeof(CompassUI),
                    typeof(InventoryUI),
                    typeof(IntroSequence),
                    typeof(TutorialSequence),
                    typeof(CombatTutorial),
                    typeof(SfxLibrary))
                .RequireNonZeroScale(
                    "ElyndorUI",
                    ExperienceLayerMigrator.ExperienceUiName)
                .RequireReferences(
                    typeof(InteractionPromptUI),
                    "detector", "promptRoot", "promptText")
                .RequireReferences(
                    typeof(NarrationUI),
                    "narrationRoot", "narrationText")
                .RequireReferences(
                    typeof(MemoryWatchActivationUI),
                    "overlayGroup")
                .RequireReferences(
                    typeof(CompassUI),
                    "needle")
                .RequireReferences(
                    typeof(InventoryUI),
                    "panelRoot", "equipmentText", "bagText")
                .RequireReferences(
                    typeof(IntroSequence),
                    "blackScreen", "lineText")
                .RequireReferences(
                    typeof(TutorialSequence),
                    "intro", "promptRoot", "promptText")
                .RequireReferences(
                    typeof(CombatTutorial),
                    "promptRoot", "promptText", "playerCombat")
                .RequireReferences(
                    typeof(SfxLibrary),
                    "footstepSource");
        }

        [MenuItem("Elyndor/Validate/Szenenintegritaet Finsterwald")]
        public static void ValidateFromMenu()
        {
            List<SceneIntegrityIssue> issues = Validate();

            if (issues.Count == 0)
            {
                Debug.Log("Szenenintegritaet Finsterwald: OK — keine Befunde.");
                return;
            }

            Debug.LogError(Format(issues));
        }

        /// <summary>Einstiegspunkt fuer den Batchmode.</summary>
        public static void ValidateBatch()
        {
            List<SceneIntegrityIssue> issues = Validate();

            if (issues.Count == 0)
            {
                Debug.Log("Szenenintegritaet Finsterwald: OK — keine Befunde.");
                EditorApplication.Exit(0);
                return;
            }

            Debug.LogError(Format(issues));
            EditorApplication.Exit(1);
        }

        public static List<SceneIntegrityIssue> Validate()
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            if (scene.path != FinsterwaldScenePath)
            {
                scene = EditorSceneManager.OpenScene(
                    FinsterwaldScenePath,
                    OpenSceneMode.Single);
            }

            return SceneIntegrityAnalyzer.Analyze(scene, CreateProfile());
        }

        public static string Format(IReadOnlyList<SceneIntegrityIssue> issues)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(
                $"Szenenintegritaet Finsterwald: {issues.Count} Befund(e).");

            foreach (SceneIntegrityIssue issue in issues)
            {
                builder.AppendLine($"  - {issue}");
            }

            return builder.ToString();
        }
    }
}
