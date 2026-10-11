using System;
using System.Reflection;
using Elyndor.EditorTools;
using Elyndor.EditorTools.SceneIntegrity;
using Elyndor.Persistence;
using Elyndor.Player;
using Elyndor.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
            Assert.That(SceneIntegrityAnalyzer.Analyze(
                scene, FinsterwaldSceneIntegrityValidator.CreateProfile()), Is.Empty);
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
}
