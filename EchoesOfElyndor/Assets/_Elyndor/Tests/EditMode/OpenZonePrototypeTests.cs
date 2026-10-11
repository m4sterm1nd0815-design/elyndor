using System;
using System.Linq;
using System.Reflection;
using Elyndor.EditorTools;
using Elyndor.EditorTools.SceneIntegrity;
using Elyndor.Enemies;
using Elyndor.Persistence;
using Elyndor.Player;
using Elyndor.Prototypes;
using Elyndor.Core;
using Elyndor.Interaction;
using Elyndor.UI;
using Elyndor.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Elyndor.Tests.EditMode
{
    public sealed class OpenZonePrototypeTests
    {
        [Test]
        public void Prototype_IsolatedAndBuildExcluded()
        {
            var source = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                "Assets/_Elyndor/Scenes/Finsterwald.unity");
            var prototype = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                OpenZonePrototypeBuilder.ScenePath);
            Assert.That(source, Is.Not.Null);
            Assert.That(prototype, Is.Not.Null);
            Assert.That(AssetDatabase.AssetPathToGUID(OpenZonePrototypeBuilder.ScenePath),
                Is.Not.EqualTo(AssetDatabase.AssetPathToGUID(
                    "Assets/_Elyndor/Scenes/Finsterwald.unity")));

            var scene = EditorSceneManager.OpenScene(OpenZonePrototypeBuilder.ScenePath);
            SceneIntegrityProfile profile = FinsterwaldSceneIntegrityValidator.CreateProfile();
            profile.SingleInstanceTypes.Remove(typeof(EnemyController));
            Assert.That(SceneIntegrityAnalyzer.Analyze(scene, profile), Is.Empty);
            var terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
            Assert.That(AssetDatabase.GetAssetPath(terrain.terrainData),
                Is.EqualTo(OpenZonePrototypeBuilder.TerrainPath));
            Assert.That(Array.Exists(EditorBuildSettings.scenes,
                entry => entry.enabled && entry.path == OpenZonePrototypeBuilder.ScenePath),
                Is.False);

            var routes = GameObject.Find("[Prototype] Open Zone Routes");
            Assert.That(routes, Is.Not.Null);
            Assert.That(routes.transform.Find("West - Lichtung"), Is.Not.Null);
            Assert.That(routes.transform.Find("East - Rastplatz"), Is.Not.Null);
            Assert.That(routes.transform.Find("Return - Waldschleife"), Is.Not.Null);
            Assert.That(routes.transform.Find("South - Brueckenansatz"), Is.Not.Null);
            Assert.That(routes.transform.Find("North - Schrein und Tor"), Is.Not.Null);
            Assert.That(routes.transform.Find("North - Hoehenweg"), Is.Not.Null);
            Assert.That(routes.transform.Find("North - Steinkreis und Aussicht"), Is.Not.Null);
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<OpenZoneMouseController>(), Is.Not.Null);
            var tutorial = UnityEngine.Object.FindAnyObjectByType<TutorialSequence>(
                FindObjectsInactive.Include);
            Assert.That(tutorial == null || !tutorial.enabled, Is.True,
                "Das alte WASD-Tutorial darf in der Maus-Probe nicht laufen.");
            var encounterGate = UnityEngine.Object.FindAnyObjectByType<OpenZoneEncounterGate>();
            Assert.That(encounterGate, Is.Not.Null);
            Assert.That(encounterGate.Regeneration, Is.Not.Null);
            Assert.That(encounterGate.EncounterRoot, Is.Not.Null);
            Assert.That(encounterGate.EncounterRoot.activeSelf, Is.False,
                "Das Nordpfad-Paar darf vor der Waldregeneration nicht aktiv sein.");
            Assert.That(scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnemyController>(true))
                .Count(), Is.EqualTo(3));
        }

        [Test]
        public void AddedRoutes_DoNotBypassTheBridge()
        {
            foreach (Vector2[] route in new[]
            {
                OpenZonePrototypeBuilder.NorthWestRoute,
                OpenZonePrototypeBuilder.NorthRidgeRoute,
                OpenZonePrototypeBuilder.NorthEastRoute
            })
                foreach (Vector2 point in route)
                    Assert.That(point.y, Is.GreaterThan(5f));

            foreach (Vector2 point in OpenZonePrototypeBuilder.SouthBridgeRoute)
                Assert.That(point.y, Is.LessThan(0f));
        }

        [Test]
        public void MouseMovement_SteersAroundImmediateObstacle()
        {
            GameObject player = new GameObject("MouseSteeringTestPlayer");
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                player.SetActive(false);
                player.AddComponent<CharacterController>();
                player.AddComponent<PlayerInputReader>();
                player.AddComponent<PlayerMovement>();
                OpenZoneMouseController controller =
                    player.AddComponent<OpenZoneMouseController>();
                player.SetActive(true);

                obstacle.name = "MouseSteeringTestObstacle";
                obstacle.transform.position = new Vector3(0f, 0.65f, 0.9f);
                obstacle.transform.localScale = new Vector3(0.7f, 1.3f, 0.4f);
                Physics.SyncTransforms();

                typeof(OpenZoneMouseController)
                    .GetField("destination", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(controller, new Vector3(0f, 0f, 10f));
                typeof(OpenZoneMouseController)
                    .GetField("hasDestination", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(controller, true);

                Vector3 movement = controller.GetMovement(Vector3.zero);
                Assert.That(movement.sqrMagnitude, Is.GreaterThan(0.1f));
                Assert.That(Mathf.Abs(movement.x), Is.GreaterThan(0.1f),
                    "Aren lenkt vor einem direkten Hindernis nicht seitlich aus.");
                Assert.That(movement.z, Is.GreaterThan(0f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(obstacle);
            }
        }

        [Test]
        public void MouseInteraction_TriggersOnceInRange()
        {
            GameObject player = new GameObject("MouseInteractionTestPlayer");
            GameObject target = new GameObject("MouseInteractionTestTarget");
            try
            {
                player.SetActive(false);
                player.AddComponent<CharacterController>();
                player.AddComponent<PlayerInputReader>();
                player.AddComponent<PlayerMovement>();
                OpenZoneMouseController controller =
                    player.AddComponent<OpenZoneMouseController>();
                player.SetActive(true);

                OpenZoneTestInteractable interactable =
                    target.AddComponent<OpenZoneTestInteractable>();
                target.transform.position = Vector3.forward;

                Type type = typeof(OpenZoneMouseController);
                type.GetField("interactionTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(controller, interactable);
                type.GetField("interactionTransform", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(controller, target.transform);
                type.GetField("hasDestination", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(controller, true);

                MethodInfo lateUpdate = type.GetMethod(
                    "LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                lateUpdate.Invoke(controller, null);
                lateUpdate.Invoke(controller, null);

                Assert.That(interactable.InteractionCount, Is.EqualTo(1));
                Assert.That(interactable.LastInteractor, Is.SameAs(player));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void InteractionPrompt_UsesMouseInstructionInPrototype()
        {
            GameObject player = new GameObject("MousePromptTestPlayer");
            GameObject prompt = new GameObject("MousePromptTestPrompt");
            GameObject target = new GameObject("MousePromptTestTarget");
            try
            {
                player.SetActive(false);
                player.AddComponent<CharacterController>();
                player.AddComponent<PlayerInputReader>();
                player.AddComponent<PlayerMovement>();
                InteractionDetector detector = player.AddComponent<InteractionDetector>();
                player.AddComponent<OpenZoneMouseController>();

                Text text = prompt.AddComponent<Text>();
                InteractionPromptUI promptUi = prompt.AddComponent<InteractionPromptUI>();
                promptUi.Configure(detector, prompt, text, prompt.transform as RectTransform);
                OpenZoneTestInteractable interactable =
                    target.AddComponent<OpenZoneTestInteractable>();

                typeof(InteractionPromptUI)
                    .GetMethod("HandleTargetChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(promptUi, new object[] { interactable });

                Assert.That(text.text, Is.EqualTo("[Linksklick] Test"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(prompt);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void NorthernEncounter_ActivatesAfterRegeneration()
        {
            const string regionId = "finsterwald_bruecke_regeneration";
            RegionRegenerationState.Forget(regionId);
            Scene preview = EditorSceneManager.NewPreviewScene();
            GameObject regenerationObject = new GameObject("TestRegeneration");
            GameObject gateObject = new GameObject("TestEncounterGate");
            GameObject encounter = new GameObject("TestEncounter");
            SceneManager.MoveGameObjectToScene(regenerationObject, preview);
            SceneManager.MoveGameObjectToScene(gateObject, preview);
            SceneManager.MoveGameObjectToScene(encounter, preview);
            try
            {
                FinsterwaldRegeneration regeneration =
                    regenerationObject.AddComponent<FinsterwaldRegeneration>();
                OpenZoneEncounterGate gate = gateObject.AddComponent<OpenZoneEncounterGate>();
                gate.Configure(regeneration, encounter);

                Assert.That(encounter.activeSelf, Is.False);
                regeneration.Regenerate();
                gate.Refresh();
                Assert.That(encounter.activeSelf, Is.True);
            }
            finally
            {
                RegionRegenerationState.Forget(regionId);
                UnityEngine.Object.DestroyImmediate(regenerationObject);
                UnityEngine.Object.DestroyImmediate(gateObject);
                UnityEngine.Object.DestroyImmediate(encounter);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        [Test]
        public void PrototypeSession_UsesMemoryStore()
        {
            SessionState.SetBool(SaveBootstrap.TransientSessionKey, true);
            SaveService.Uninstall();
            try
            {
                typeof(SaveBootstrap).GetMethod("Install", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, null);
                Assert.That(SaveService.Current, Is.Not.Null);
                var store = typeof(SaveService).GetField("store", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(SaveService.Current);
                Assert.That(store, Is.TypeOf<MemorySaveStore>());
            }
            finally
            {
                SaveService.Uninstall();
                SessionState.SetBool(SaveBootstrap.TransientSessionKey, false);
            }
        }
    }

    public sealed class OpenZoneTestInteractable : MonoBehaviour, IInteractable
    {
        public int InteractionCount { get; private set; }
        public GameObject LastInteractor { get; private set; }
        public string InteractionPrompt => "Test";
        public Vector3 Position => transform.position;
        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            InteractionCount++;
            LastInteractor = interactor;
        }
    }
}
