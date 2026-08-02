using System;
using System.Collections.Generic;
using System.Linq;
using Elyndor.Player;
using Elyndor.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools
{
    public static class RegionPortalValidator
    {
        private static readonly string[] RegionSceneNames =
        {
            "Finsterwald",
            "Sonnenfelder",
            "Nebelmoor"
        };

        [MenuItem("Elyndor/QA/Validate Region Portals")]
        public static void ValidateAllRegions()
        {
            HashSet<string> enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path))
                .ToHashSet(StringComparer.Ordinal);

            List<PortalRecord> portals = new List<PortalRecord>();
            Dictionary<string, HashSet<string>> spawnIdsByScene =
                new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (string sceneName in RegionSceneNames)
            {
                string scenePath = $"Assets/_Elyndor/Scenes/{sceneName}.unity";
                Scene scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Single);

                ValidateMissingScripts(scene);

                CharacterController player = UnityEngine.Object
                    .FindObjectsByType<CharacterController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .SingleOrDefault(controller =>
                        controller.gameObject.scene == scene &&
                        controller.gameObject.activeInHierarchy);

                if (player == null)
                    throw new InvalidOperationException(
                        $"{sceneName}: exactly one active CharacterController is required.");

                RegionSpawnPoint[] spawnPoints = UnityEngine.Object
                    .FindObjectsByType<RegionSpawnPoint>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Where(spawn => spawn.gameObject.scene == scene &&
                        spawn.gameObject.activeInHierarchy && spawn.enabled)
                    .ToArray();

                HashSet<string> spawnIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (RegionSpawnPoint spawnPoint in spawnPoints)
                {
                    SerializedObject serializedSpawn = new SerializedObject(spawnPoint);
                    string spawnId = serializedSpawn.FindProperty("spawnId").stringValue;
                    if (string.IsNullOrWhiteSpace(spawnId) || !spawnIds.Add(spawnId))
                        throw new InvalidOperationException(
                            $"{sceneName}: invalid or duplicate spawn id on {spawnPoint.name}.");
                }

                spawnIdsByScene[sceneName] = spawnIds;

                RegionPortal[] scenePortals = UnityEngine.Object
                    .FindObjectsByType<RegionPortal>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Where(portal => portal.gameObject.scene == scene)
                    .ToArray();

                foreach (RegionPortal portal in scenePortals)
                {
                    ValidatePortal(sceneName, portal, player.gameObject.layer);
                    portals.Add(new PortalRecord(
                        sceneName,
                        portal.TargetSceneName,
                        portal.TargetSpawnId,
                        portal.name));
                }
            }

            foreach (PortalRecord portal in portals)
            {
                if (!enabledScenes.Contains(portal.TargetScene))
                    throw new InvalidOperationException(
                        $"{portal.SourceScene}/{portal.Name}: target scene " +
                        $"{portal.TargetScene} is not enabled in build settings.");

                if (!spawnIdsByScene.TryGetValue(portal.TargetScene, out HashSet<string> spawnIds) ||
                    !spawnIds.Contains(portal.TargetSpawnId))
                    throw new InvalidOperationException(
                        $"{portal.SourceScene}/{portal.Name}: target spawn " +
                        $"{portal.TargetSpawnId} does not exist in {portal.TargetScene}.");
            }

            foreach (IGrouping<string, PortalRecord> route in portals.GroupBy(
                portal => $"{portal.SourceScene}->{portal.TargetScene}",
                StringComparer.Ordinal))
            {
                if (route.Count() != 1)
                    throw new InvalidOperationException(
                        $"Route {route.Key} has {route.Count()} active portal targets; expected one.");
            }

            Debug.Log(
                $"REGION_PORTAL_VALIDATION_OK: {portals.Count} active portals, " +
                $"{portals.Count} unique transitions, valid triggers, prompts, " +
                "target scenes and spawn ids.");
        }

        private static void ValidatePortal(
            string sceneName,
            RegionPortal portal,
            int playerLayer)
        {
            if (!portal.gameObject.activeInHierarchy || !portal.enabled)
                throw new InvalidOperationException(
                    $"{sceneName}/{portal.name}: portal is inactive.");

            if (string.IsNullOrWhiteSpace(portal.InteractionPrompt))
                throw new InvalidOperationException(
                    $"{sceneName}/{portal.name}: interaction prompt is empty.");

            if (string.IsNullOrWhiteSpace(portal.TargetSceneName) ||
                string.IsNullOrWhiteSpace(portal.TargetSpawnId))
                throw new InvalidOperationException(
                    $"{sceneName}/{portal.name}: target scene or spawn id is empty.");

            Collider[] triggers = portal.GetComponentsInChildren<Collider>(true)
                .Where(collider => collider.enabled && collider.isTrigger &&
                    collider.gameObject.activeInHierarchy)
                .ToArray();

            if (triggers.Length != 1)
                throw new InvalidOperationException(
                    $"{sceneName}/{portal.name}: expected one active trigger, found {triggers.Length}.");

            Bounds bounds = triggers[0].bounds;
            if (bounds.size.x < 1f || bounds.size.y < 1f || bounds.size.z < 1f ||
                bounds.SqrDistance(portal.transform.position) > 25f)
                throw new InvalidOperationException(
                    $"{sceneName}/{portal.name}: trigger is too small or separated from the portal.");

            if (Physics.GetIgnoreLayerCollision(
                playerLayer,
                triggers[0].gameObject.layer))
                throw new InvalidOperationException(
                    $"{sceneName}/{portal.name}: player layer cannot enter the portal trigger.");
        }

        private static void ValidateMissingScripts(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    int missingCount = GameObjectUtility
                        .GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                    if (missingCount > 0)
                        throw new InvalidOperationException(
                            $"{scene.name}/{transform.name}: {missingCount} missing script(s).");
                }
            }
        }

        private readonly struct PortalRecord
        {
            public PortalRecord(
                string sourceScene,
                string targetScene,
                string targetSpawnId,
                string name)
            {
                SourceScene = sourceScene;
                TargetScene = targetScene;
                TargetSpawnId = targetSpawnId;
                Name = name;
            }

            public string SourceScene { get; }
            public string TargetScene { get; }
            public string TargetSpawnId { get; }
            public string Name { get; }
        }
    }
}
