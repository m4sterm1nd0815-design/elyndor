using Elyndor.Cameras;
using Elyndor.Core;
using Elyndor.Interaction;
using Elyndor.Player;
using Elyndor.UI;
using Elyndor.UIFoundation;

namespace Elyndor.EditorTools.SceneIntegrity
{
    /// <summary>
    /// Die Integritaetsprofile aller derzeit produktiv verwendeten Szenen.
    ///
    /// Bewusst kein gemeinsames Zwangsprofil: Finsterwald ist der Vertical
    /// Slice und traegt das neue HUD-Fundament samt Intro und Tutorial,
    /// Sonnenfelder und Nebelmoor tun das nicht. Was eine Region nicht besitzt,
    /// wird nicht eingefordert — was sie besitzt, muss wirksam sein. Deshalb
    /// stehen die gemeinsamen Systeme unter <c>RequireActive</c> und die
    /// regionsabhaengigen unter <c>AllowOptionalActive</c>.
    /// </summary>
    public static class RegionSceneIntegrityProfiles
    {
        /// <summary>
        /// Referenzregion. Das Profil bleibt dort, wo es entstanden ist, damit
        /// Editor, Batchmode und die vorhandenen Tests weiterhin exakt dasselbe
        /// pruefen.
        /// </summary>
        public static SceneIntegrityProfile CreateFinsterwald()
        {
            return FinsterwaldSceneIntegrityValidator.CreateProfile();
        }

        public static SceneIntegrityProfile CreateSonnenfelder()
        {
            return CreateBaseRegionProfile("Sonnenfelder");
        }

        public static SceneIntegrityProfile CreateNebelmoor()
        {
            return CreateBaseRegionProfile("Nebelmoor");
        }

        /// <summary>
        /// Bootstrap ist eine Sandbox ohne Erlebnisschicht: kein HUD, kein Ton,
        /// keine Erinnerungsorte. Geprueft wird nur, dass die Spielfigur
        /// vollstaendig ist — inklusive <see cref="PlayerInputReader"/>, den
        /// <see cref="PlayerMovement"/> sonst zur Laufzeit mit einer Warnung
        /// nachtraeglich anlegt.
        /// </summary>
        public static SceneIntegrityProfile CreateBootstrap()
        {
            return new SceneIntegrityProfile("Bootstrap")
                .RequireActive(
                    typeof(PlayerInputReader),
                    typeof(PlayerMovement),
                    typeof(CameraFollow))
                .RequireSingleInstance(
                    typeof(PlayerInputReader),
                    typeof(PlayerMovement),
                    typeof(CameraFollow));
        }

        /// <summary>
        /// Gemeinsame Grundlage von Sonnenfelder und Nebelmoor. Beide Regionen
        /// sind strukturell identisch: sichtbare Erlebnisschicht auf
        /// <c>ElyndorExperienceUI</c>, nicht-visuelle Systeme auf
        /// <c>ElyndorExperience</c>, kein HUD-Fundament.
        /// </summary>
        private static SceneIntegrityProfile CreateBaseRegionProfile(string name)
        {
            return new SceneIntegrityProfile(name)
                .RequireActive(
                    // Spielfigur und Kamera
                    typeof(PlayerInputReader),
                    typeof(PlayerMovement),
                    typeof(InteractionDetector),
                    typeof(CameraFollow),
                    // Sichtbare Erlebnisschicht
                    typeof(InteractionPromptUI),
                    typeof(NarrationUI),
                    typeof(MemoryWatchActivationUI),
                    typeof(CompassUI),
                    typeof(InventoryUI),
                    // Nicht-visuelle Erlebnisschicht
                    typeof(SfxLibrary))
                .RequireSingleInstance(
                    typeof(PlayerInputReader),
                    typeof(PlayerMovement),
                    typeof(InteractionDetector),
                    typeof(CameraFollow),
                    typeof(InteractionPromptUI),
                    typeof(NarrationUI),
                    typeof(MemoryWatchActivationUI),
                    typeof(CompassUI),
                    typeof(InventoryUI),
                    typeof(SfxLibrary),
                    // Auch wenn optional: doppelt darf keines davon auftreten.
                    typeof(IntroSequence),
                    typeof(TutorialSequence),
                    typeof(CombatTutorial),
                    typeof(HudVitalsPresenter),
                    typeof(QuickslotBarPresenter))
                .AllowOptionalActive(
                    // Diese Regionen fuehren bewusst kein Intro, kein Tutorial
                    // und kein HUD-Fundament. Liegt trotzdem eines in der Szene,
                    // darf es nicht unter einem toten Vorfahren haengen.
                    typeof(IntroSequence),
                    typeof(TutorialSequence),
                    typeof(CombatTutorial),
                    typeof(HudVitalsPresenter),
                    typeof(QuickslotBarPresenter),
                    typeof(PlayerVitals))
                .RequireNonZeroScale(
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
                    typeof(SfxLibrary),
                    "footstepSource");
        }
    }
}
