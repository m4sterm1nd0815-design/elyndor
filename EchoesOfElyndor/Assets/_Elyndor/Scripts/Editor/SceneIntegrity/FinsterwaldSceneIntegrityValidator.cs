using System.Collections.Generic;
using System.Text;
using Elyndor.Companion;
using Elyndor.Core;
using Elyndor.Enemies;
using Elyndor.Narration;
using Elyndor.Player;
using Elyndor.Puzzles;
using Elyndor.UI;
using Elyndor.UIFoundation;
using Elyndor.World;
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
                    typeof(SfxLibrary),
                    // Slice-Systeme. Bis hierhin beschrieb das Profil nur den
                    // Stand vor P1.5. Dass genau ein Wurzelstreifer, ein Link,
                    // ein Raetsel und eine Regeneration in der Szene stehen,
                    // war danach nur noch durch Handarbeit und einzelne
                    // PlayMode-Tests gedeckt — ein Baumeisterlauf, der etwas
                    // doppelt anlegt, waere hier gruen durchgekommen.
                    typeof(EnemyController),
                    typeof(LinkCompanion),
                    typeof(BridgePuzzle),
                    typeof(WatchResonanceZone),
                    typeof(FinsterwaldRegeneration))
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
                    typeof(SfxLibrary),
                    // Der erste Kampf ist Unterricht: genau ein Gegner. Link
                    // ist ein Begleiter, kein Schwarm. Raetsel, Resonanzzone
                    // und Regeneration halten je einen Zustand — doppelt
                    // liefen sie gegeneinander.
                    //
                    // Bewusst nicht aufgefuehrt: BridgeAnchor (drei) und
                    // LinkPerch (fuenf). Die sind absichtlich mehrfach.
                    typeof(EnemyController),
                    typeof(LinkCompanion),
                    typeof(BridgePuzzle),
                    typeof(WatchResonanceZone),
                    typeof(FinsterwaldRegeneration),
                    typeof(MemoryEchoNarration))
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
