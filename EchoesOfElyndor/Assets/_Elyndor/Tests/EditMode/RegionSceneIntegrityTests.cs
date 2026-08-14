using System.Collections.Generic;
using System.Linq;
using Elyndor.Core;
using Elyndor.EditorTools;
using Elyndor.EditorTools.SceneIntegrity;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.Tests.EditMode
{
    /// <summary>
    /// Regressionsschutz fuer alle produktiv verwendeten Szenen.
    ///
    /// Der Finsterwald-Defekt war unsichtbar: Projekt kompilierte, Konsole
    /// blieb still, Tests waren gruen — und trotzdem waren Interaktion,
    /// Narration, Erinnerungsuhr, Tutorial und der gesamte Ton tot. Dieselbe
    /// Fehlerklasse lag latent auch in Sonnenfelder und Nebelmoor, weil dort
    /// Audio und Runtime am UI-Canvas hingen. Diese Tests halten den reparierten
    /// Zustand fest.
    /// </summary>
    public sealed class RegionSceneIntegrityTests
    {
        [Test]
        public void JedeSzene_ErfuelltIhrIntegritaetsprofil(
            [ValueSource(nameof(Bindings))]
            RegionSceneIntegrityValidator.SceneProfileBinding binding)
        {
            List<SceneIntegrityIssue> issues =
                RegionSceneIntegrityValidator.ValidateScene(binding);

            Assert.That(
                issues,
                Is.Empty,
                $"{binding.ProfileName}: {Describe(issues)}");
        }

        /// <summary>
        /// Der Kern der Reparatur: Der Ton darf nicht am UI-Canvas haengen.
        /// Genau diese Kopplung hat in Finsterwald mit dem Canvas auch das
        /// gesamte Audio abgeschaltet.
        /// </summary>
        [Test]
        public void SfxLibrary_HaengtNiemalsAmUiCanvas(
            [ValueSource(nameof(RegionScenePaths))] string scenePath)
        {
            Scene scene = OpenScene(scenePath);

            SfxLibrary[] libraries = FindInScene<SfxLibrary>(scene);

            Assert.That(
                libraries,
                Is.Not.Empty,
                $"'{scene.name}' hat keine SfxLibrary.");

            foreach (SfxLibrary library in libraries)
            {
                Assert.That(
                    library.GetComponent<Canvas>(),
                    Is.Null,
                    $"'{scene.name}': SfxLibrary liegt auf einem Canvas " +
                    $"('{library.gameObject.name}'). Ein deaktiviertes UI-Panel " +
                    "wuerde damit den Ton mit abschalten.");

                Assert.That(
                    library.gameObject.name,
                    Is.EqualTo(ExperienceLayerMigrator.ExperienceRootName),
                    $"'{scene.name}': SfxLibrary gehoert auf " +
                    $"'{ExperienceLayerMigrator.ExperienceRootName}'.");
            }
        }

        /// <summary>
        /// Der alte Sammel-Canvas darf in keiner Region mehr existieren.
        /// </summary>
        [Test]
        public void KeineRegion_TraegtNochDenAltenSammelCanvas(
            [ValueSource(nameof(RegionScenePaths))] string scenePath)
        {
            Scene scene = OpenScene(scenePath);

            string[] rootNames = scene.GetRootGameObjects()
                .Select(root => root.name)
                .ToArray();

            Assert.That(
                rootNames,
                Does.Not.Contain(ExperienceLayerMigrator.LegacyCanvasName),
                $"'{scene.name}' traegt noch '{ExperienceLayerMigrator.LegacyCanvasName}'.");

            Assert.That(
                rootNames,
                Does.Contain(ExperienceLayerMigrator.ExperienceUiName),
                $"'{scene.name}' hat keinen '{ExperienceLayerMigrator.ExperienceUiName}'.");

            Assert.That(
                rootNames,
                Does.Contain(ExperienceLayerMigrator.ExperienceRootName),
                $"'{scene.name}' hat keinen '{ExperienceLayerMigrator.ExperienceRootName}'.");
        }

        /// <summary>
        /// Gegenprobe zur Regionsspezifik: Das Finsterwald-Profil fordert das
        /// HUD-Fundament samt Intro und Tutorial. Nebelmoor fuehrt beides
        /// bewusst nicht und muss gegen dieses Profil deshalb durchfallen.
        /// Faellt es nicht durch, ist die Pruefung wirkungslos geworden.
        /// </summary>
        [Test]
        public void FinsterwaldProfil_GegenNebelmoor_MeldetFehlendePflichtsysteme()
        {
            Scene scene = OpenScene(RegionScenes.Nebelmoor);

            List<SceneIntegrityIssue> issues = SceneIntegrityAnalyzer.Analyze(
                scene,
                RegionSceneIntegrityProfiles.CreateFinsterwald());

            Assert.That(
                issues.Select(issue => issue.Code),
                Does.Contain(SceneIntegrityIssueCode.RequiredComponentMissing),
                "Ein regionsfremdes Profil muss die fehlenden Pflichtsysteme melden.");
        }

        /// <summary>
        /// Und die Gegenrichtung: Das Regionsprofil von Nebelmoor fordert nichts,
        /// was Finsterwald nicht ohnehin besitzt. Es muss dort also gruen sein.
        /// Damit ist belegt, dass die gemeinsame Grundlage wirklich gemeinsam ist.
        /// </summary>
        [Test]
        public void NebelmoorProfil_GegenFinsterwald_IstGruen()
        {
            Scene scene = OpenScene(RegionScenes.Finsterwald);

            List<SceneIntegrityIssue> issues = SceneIntegrityAnalyzer.Analyze(
                scene,
                RegionSceneIntegrityProfiles.CreateNebelmoor());

            Assert.That(issues, Is.Empty, Describe(issues));
        }

        public static IEnumerable<RegionSceneIntegrityValidator.SceneProfileBinding>
            Bindings => RegionSceneIntegrityValidator.Bindings;

        public static IEnumerable<string> RegionScenePaths => RegionScenes.All;

        private static Scene OpenScene(string scenePath)
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            if (scene.path != scenePath)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Single);
            }

            return scene;
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component
        {
            List<T> found = new List<T>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return found.ToArray();
        }

        private static string Describe(IEnumerable<SceneIntegrityIssue> issues) =>
            string.Join(" | ", issues.Select(issue => issue.ToString()));
    }
}
