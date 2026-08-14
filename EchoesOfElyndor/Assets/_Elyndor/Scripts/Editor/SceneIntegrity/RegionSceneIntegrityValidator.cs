using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools.SceneIntegrity
{
    /// <summary>
    /// Prueft alle produktiv verwendeten Szenen in einem einzigen Durchlauf
    /// gegen ihr jeweiliges Profil.
    ///
    /// Der Finsterwald-Validator prueft weiterhin genau eine Szene und bleibt
    /// unveraendert. Diese Klasse ist der Sammeleinstieg fuer die Regressions-
    /// pruefung: ein Aufruf, ein Report, ein Exit-Code.
    /// </summary>
    public static class RegionSceneIntegrityValidator
    {
        /// <summary>
        /// Szenenpfad und zugehoeriges Profil. Die Profile werden als Factory
        /// gehalten, damit jeder Lauf mit einem frischen Profil arbeitet.
        /// </summary>
        public static IReadOnlyList<SceneProfileBinding> Bindings { get; } =
            new[]
            {
                new SceneProfileBinding(
                    RegionScenes.Finsterwald,
                    RegionSceneIntegrityProfiles.CreateFinsterwald),
                new SceneProfileBinding(
                    RegionScenes.Sonnenfelder,
                    RegionSceneIntegrityProfiles.CreateSonnenfelder),
                new SceneProfileBinding(
                    RegionScenes.Nebelmoor,
                    RegionSceneIntegrityProfiles.CreateNebelmoor),
                new SceneProfileBinding(
                    RegionScenes.Bootstrap,
                    RegionSceneIntegrityProfiles.CreateBootstrap),
            };

        [MenuItem("Elyndor/QA/Validate Scene Integrity (alle Szenen)")]
        public static void ValidateFromMenu()
        {
            string report = Validate(out bool clean);

            if (clean)
            {
                Debug.Log(report);
                return;
            }

            Debug.LogError(report);
        }

        /// <summary>Einstiegspunkt fuer den Batchmode.</summary>
        public static void ValidateBatch()
        {
            string report = Validate(out bool clean);

            if (clean)
            {
                Debug.Log(report);
                EditorApplication.Exit(0);
                return;
            }

            Debug.LogError(report);
            EditorApplication.Exit(1);
        }

        /// <summary>
        /// Prueft alle Bindings und liefert einen zusammengefassten Report.
        /// Laedt jede Szene selbst und ist damit batchmode-tauglich.
        /// </summary>
        public static string Validate(out bool clean)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("Szenenintegritaet ueber alle Szenen:");

            int total = 0;

            foreach (SceneProfileBinding binding in Bindings)
            {
                List<SceneIntegrityIssue> issues = ValidateScene(binding);
                total += issues.Count;

                if (issues.Count == 0)
                {
                    report.AppendLine($"  {binding.ProfileName}: OK — keine Befunde.");
                    continue;
                }

                report.AppendLine($"  {binding.ProfileName}: {issues.Count} Befund(e).");

                foreach (SceneIntegrityIssue issue in issues)
                {
                    report.AppendLine($"      - {issue}");
                }
            }

            clean = total == 0;
            report.AppendLine($"Summe: {total} Befund(e).");
            return report.ToString();
        }

        /// <summary>
        /// Prueft eine einzelne Bindung. Oeffentlich, damit die EditMode-Tests
        /// jede Szene einzeln pruefen und einzeln melden koennen.
        /// </summary>
        public static List<SceneIntegrityIssue> ValidateScene(
            SceneProfileBinding binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            Scene scene = EditorSceneManager.GetActiveScene();

            if (scene.path != binding.ScenePath)
            {
                scene = EditorSceneManager.OpenScene(
                    binding.ScenePath,
                    OpenSceneMode.Single);
            }

            return SceneIntegrityAnalyzer.Analyze(scene, binding.CreateProfile());
        }

        public sealed class SceneProfileBinding
        {
            private readonly Func<SceneIntegrityProfile> profileFactory;

            public SceneProfileBinding(
                string scenePath,
                Func<SceneIntegrityProfile> profileFactory)
            {
                ScenePath = scenePath;
                this.profileFactory = profileFactory;
            }

            public string ScenePath { get; }

            public string ProfileName => CreateProfile().Name;

            public SceneIntegrityProfile CreateProfile() => profileFactory();
        }
    }
}
