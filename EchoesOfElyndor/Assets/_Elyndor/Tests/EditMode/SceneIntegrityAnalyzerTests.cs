using System.Collections.Generic;
using System.Linq;
using Elyndor.EditorTools.SceneIntegrity;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.Tests.EditMode
{
    /// <summary>
    /// Prueft den <see cref="SceneIntegrityAnalyzer"/> gegen eigens gebaute
    /// Szenen. Bewusst mit Testkomponenten statt echten Elyndor-Systemen: So
    /// pruefen diese Tests die Analyselogik selbst und brechen nicht, sobald
    /// sich ein Spielsystem aendert.
    /// </summary>
    public sealed class SceneIntegrityAnalyzerTests
    {
        private Scene scene;

        [SetUp]
        public void SetUp()
        {
            scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid())
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GueltigeStruktur_MeldetKeineBefunde()
        {
            GameObject root = CreateRoot("Systems");
            TestRuntimeSystem system = root.AddComponent<TestRuntimeSystem>();
            system.AssignRequiredTarget(root.transform);

            List<SceneIntegrityIssue> issues =
                SceneIntegrityAnalyzer.Analyze(scene, CreateProfile());

            Assert.That(
                issues,
                Is.Empty,
                $"Unerwartete Befunde: {Describe(issues)}");
        }

        [Test]
        public void PflichtsystemUnterDeaktiviertemParent_WirdGemeldet()
        {
            GameObject parent = CreateRoot("Systems");
            GameObject child = new GameObject("Child");
            child.transform.SetParent(parent.transform);

            TestRuntimeSystem system = child.AddComponent<TestRuntimeSystem>();
            system.AssignRequiredTarget(parent.transform);

            // Genau der Finsterwald-Fall: Die Komponente existiert, aber der
            // Vorfahre ist aus — Awake und Update laufen nie.
            parent.SetActive(false);

            List<SceneIntegrityIssue> issues =
                SceneIntegrityAnalyzer.Analyze(scene, CreateProfile());

            Assert.That(
                issues.Select(issue => issue.Code),
                Does.Contain(SceneIntegrityIssueCode.RequiredComponentInactive),
                $"Befunde: {Describe(issues)}");
        }

        [Test]
        public void UiWurzelMitScaleNull_WirdGemeldet()
        {
            GameObject root = CreateRoot("Systems");
            TestRuntimeSystem system = root.AddComponent<TestRuntimeSystem>();
            system.AssignRequiredTarget(root.transform);

            GameObject uiRoot = CreateRoot("UiRoot");
            uiRoot.transform.localScale = Vector3.zero;

            List<SceneIntegrityIssue> issues =
                SceneIntegrityAnalyzer.Analyze(scene, CreateProfile());

            Assert.That(
                issues.Select(issue => issue.Code),
                Does.Contain(SceneIntegrityIssueCode.ZeroScaleRoot),
                $"Befunde: {Describe(issues)}");
        }

        [Test]
        public void FehlendesPflichtsystem_WirdGemeldet()
        {
            CreateRoot("Systems");

            List<SceneIntegrityIssue> issues =
                SceneIntegrityAnalyzer.Analyze(scene, CreateProfile());

            Assert.That(
                issues.Select(issue => issue.Code),
                Does.Contain(SceneIntegrityIssueCode.RequiredComponentMissing),
                $"Befunde: {Describe(issues)}");
        }

        [Test]
        public void DoppeltesEinzelsystem_WirdGemeldet()
        {
            GameObject first = CreateRoot("Systems");
            first.AddComponent<TestRuntimeSystem>()
                .AssignRequiredTarget(first.transform);

            GameObject second = CreateRoot("SystemsCopy");
            second.AddComponent<TestRuntimeSystem>()
                .AssignRequiredTarget(second.transform);

            List<SceneIntegrityIssue> issues =
                SceneIntegrityAnalyzer.Analyze(scene, CreateProfile());

            Assert.That(
                issues.Select(issue => issue.Code),
                Does.Contain(
                    SceneIntegrityIssueCode.DuplicateSingleInstanceComponent),
                $"Befunde: {Describe(issues)}");
        }

        [Test]
        public void NichtGesetztePflichtreferenz_WirdGemeldet()
        {
            GameObject root = CreateRoot("Systems");
            root.AddComponent<TestRuntimeSystem>();

            List<SceneIntegrityIssue> issues =
                SceneIntegrityAnalyzer.Analyze(scene, CreateProfile());

            Assert.That(
                issues.Select(issue => issue.Code),
                Does.Contain(SceneIntegrityIssueCode.MissingRequiredReference),
                $"Befunde: {Describe(issues)}");
        }

        [Test]
        public void ProfilMitUnbekanntemFeldnamen_MeldetProfilfehler()
        {
            GameObject root = CreateRoot("Systems");
            root.AddComponent<TestRuntimeSystem>()
                .AssignRequiredTarget(root.transform);

            SceneIntegrityProfile profile =
                new SceneIntegrityProfile("Kaputtes Profil")
                    .RequireReferences(
                        typeof(TestRuntimeSystem),
                        "gibtEsNichtMehr");

            List<SceneIntegrityIssue> issues =
                SceneIntegrityAnalyzer.Analyze(scene, profile);

            Assert.That(
                issues.Select(issue => issue.Code),
                Does.Contain(SceneIntegrityIssueCode.MissingRequiredReference),
                "Ein Profil, das nicht mehr zum Code passt, muss auffallen.");
        }

        [Test]
        public void FehlendesOptionalesSystem_IstKeinBefund()
        {
            GameObject root = CreateRoot("Systems");
            root.AddComponent<TestRuntimeSystem>()
                .AssignRequiredTarget(root.transform);

            // Genau der Regionsfall: Nebelmoor fuehrt kein Intro. Fehlt das
            // System, darf die Pruefung nicht meckern — sonst wuerden drei
            // Regionen kuenstlich gleich gemacht.
            List<SceneIntegrityIssue> issues =
                SceneIntegrityAnalyzer.Analyze(scene, CreateProfileWithOptional());

            Assert.That(
                issues,
                Is.Empty,
                $"Ein fehlendes optionales System ist kein Befund: {Describe(issues)}");
        }

        [Test]
        public void OptionalesSystemUnterDeaktiviertemParent_WirdGemeldet()
        {
            GameObject root = CreateRoot("Systems");
            root.AddComponent<TestRuntimeSystem>()
                .AssignRequiredTarget(root.transform);

            GameObject optionalRoot = CreateRoot("OptionalSystems");
            optionalRoot.AddComponent<TestOptionalSystem>();

            // Vorhanden, aber wirkungslos. Das ist die Fehlerklasse aus
            // Finsterwald und muss auch bei optionalen Systemen auffallen.
            optionalRoot.SetActive(false);

            List<SceneIntegrityIssue> issues =
                SceneIntegrityAnalyzer.Analyze(scene, CreateProfileWithOptional());

            Assert.That(
                issues.Select(issue => issue.Code),
                Does.Contain(SceneIntegrityIssueCode.RequiredComponentInactive),
                $"Befunde: {Describe(issues)}");
        }

        private static SceneIntegrityProfile CreateProfile()
        {
            return new SceneIntegrityProfile("Test")
                .RequireActive(typeof(TestRuntimeSystem))
                .RequireSingleInstance(typeof(TestRuntimeSystem))
                .RequireNonZeroScale("UiRoot")
                .RequireReferences(typeof(TestRuntimeSystem), "requiredTarget");
        }

        private static SceneIntegrityProfile CreateProfileWithOptional()
        {
            return new SceneIntegrityProfile("Test mit optionalem System")
                .RequireActive(typeof(TestRuntimeSystem))
                .AllowOptionalActive(typeof(TestOptionalSystem))
                .RequireReferences(typeof(TestRuntimeSystem), "requiredTarget");
        }

        private GameObject CreateRoot(string name)
        {
            GameObject root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static string Describe(IEnumerable<SceneIntegrityIssue> issues) =>
            string.Join(" | ", issues.Select(issue => issue.ToString()));
    }

    /// <summary>Testkomponente mit einer Pflichtreferenz.</summary>
    public sealed class TestRuntimeSystem : MonoBehaviour
    {
        [SerializeField] private Transform requiredTarget;

        public void AssignRequiredTarget(Transform target) =>
            requiredTarget = target;
    }

    /// <summary>
    /// Testkomponente fuer regionsabhaengige Systeme: darf fehlen, darf aber
    /// nicht wirkungslos vorhanden sein.
    /// </summary>
    public sealed class TestOptionalSystem : MonoBehaviour
    {
    }
}
