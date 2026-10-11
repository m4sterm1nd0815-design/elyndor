using System;
using System.Reflection;
using Elyndor.EditorTools;
using Elyndor.EditorTools.SceneIntegrity;
using Elyndor.Persistence;
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
