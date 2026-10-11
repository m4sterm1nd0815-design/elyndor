using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elyndor.Cameras;
using Elyndor.Core;
using Elyndor.EditorTools.SceneIntegrity;
using Elyndor.Enemies;
using Elyndor.Player;
using Elyndor.Prototypes;
using Elyndor.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools
{
    /// <summary>Creates the experiment once. Reopening never regenerates hand edits.</summary>
    public static class OpenZonePrototypeBuilder
    {
        public const string Folder = "Assets/_Elyndor/Prototypes/OpenZone";
        public const string ScenePath = Folder + "/Finsterwald_OpenZone.unity";
        public const string TerrainPath = Folder + "/Finsterwald_OpenZone_Terrain.asset";
        private const string SourcePath = "Assets/_Elyndor/Scenes/Finsterwald.unity";
        private const float ClearRadius = 7f;
        private const string EncounterRootName = "[Prototype] Open Zone Encounters";
        private const string NorthEncounterName = "Nordpfad - Streiferpaar";

        public static readonly Vector2[] NorthEncounterPositions =
        {
            new Vector2(-28f, 43f),
            new Vector2(-16f, 46f)
        };

        // Both choices remain south of the river: the memory bridge is not bypassed.
        public static readonly Vector2[] WestRoute =
        {
            new Vector2(-3, -70), new Vector2(-17, -62), new Vector2(-30, -50),
            new Vector2(-38, -35), new Vector2(-33, -24), new Vector2(-26, -16)
        };
        public static readonly Vector2[] EastRoute =
        {
            new Vector2(-3, -70), new Vector2(12, -62), new Vector2(23, -49),
            new Vector2(26, -40), new Vector2(17, -42),
            new Vector2(17, -27), new Vector2(5, -25)
        };
        public static readonly Vector2[] ReturnRoute =
        {
            new Vector2(-26, -16), new Vector2(-17, -22),
            new Vector2(-8, -29), new Vector2(5, -25)
        };
        // The southern branch rejoins the original puzzle approach. Every
        // northern route starts beyond the bridge, never across the river.
        public static readonly Vector2[] SouthBridgeRoute =
        {
            new Vector2(-26, -16), new Vector2(-18, -12),
            new Vector2(-10, -10), new Vector2(-3, -8)
        };
        public static readonly Vector2[] NorthWestRoute =
        {
            new Vector2(2, 7), new Vector2(-8, 13),
            new Vector2(-19, 11), new Vector2(-25, 10),
            new Vector2(-34, 12), new Vector2(-42, 20),
            new Vector2(-45, 25), new Vector2(-45, 34)
        };
        public static readonly Vector2[] NorthRidgeRoute =
        {
            new Vector2(-45, 34), new Vector2(-29, 44),
            new Vector2(-12, 46), new Vector2(5, 46),
            new Vector2(21, 44), new Vector2(36, 18)
        };
        public static readonly Vector2[] NorthEastRoute =
        {
            new Vector2(2, 7), new Vector2(19, 12),
            new Vector2(36, 18), new Vector2(44, 29),
            new Vector2(48, 44)
        };

        [MenuItem("Elyndor/Open Zone/Create or Open Finsterwald Prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before building the prototype.");
            // Never replace an unsaved scene, even when invoked from automation.
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save the open scene before creating the prototype.");

            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                Validate();
                return;
            }

            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            if (!AssetDatabase.CopyAsset(SourcePath, ScenePath))
                throw new InvalidOperationException("Could not copy the source scene.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath);
            Terrain terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
            if (terrain == null)
                throw new InvalidOperationException("Source terrain missing.");
            string originalData = AssetDatabase.GetAssetPath(terrain.terrainData);
            if (!AssetDatabase.CopyAsset(originalData, TerrainPath))
                throw new InvalidOperationException("Could not isolate terrain data.");
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
            terrain.terrainData = data;
            terrain.GetComponent<TerrainCollider>().terrainData = data;
            PaintRoutes(terrain);
            int moved = ClearRoutes(terrain);
            moved += ClearBlockingVegetation(terrain);

            CameraFollow follow = UnityEngine.Object.FindAnyObjectByType<CameraFollow>();
            if (follow == null)
                throw new InvalidOperationException("CameraFollow missing.");
            SerializedObject camera = new SerializedObject(follow);
            SetFloat(camera, "startYaw", 0f);
            SetFloat(camera, "startPitch", 60f);
            SetFloat(camera, "minPitch", 50f);
            SetFloat(camera, "maxPitch", 70f);
            SetFloat(camera, "distance", 21f);
            SetFloat(camera, "minDistance", 16f);
            SetFloat(camera, "maxDistance", 27f);
            camera.ApplyModifiedPropertiesWithoutUndo();
            Transform player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>().transform;
            player.gameObject.AddComponent<OpenZoneMouseController>();
            TutorialSequence tutorial = UnityEngine.Object.FindAnyObjectByType<TutorialSequence>(
                FindObjectsInactive.Include);
            if (tutorial != null) tutorial.enabled = false;
            Vector3 focus = player.position + Vector3.up * 1.2f;
            follow.transform.position = focus + Quaternion.Euler(60f, 0f, 0f) * Vector3.back * 21f;
            follow.transform.LookAt(focus);

            GameObject routes = new GameObject("[Prototype] Open Zone Routes");
            AddWaypoints(routes.transform, "West - Lichtung", WestRoute, terrain);
            AddWaypoints(routes.transform, "East - Rastplatz", EastRoute, terrain);
            AddWaypoints(routes.transform, "Return - Waldschleife", ReturnRoute, terrain);
            AddCompletionWaypoints(routes.transform, terrain);
            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log($"OPEN_ZONE_BUILT: {moved} vegetation objects moved; terrain {data.size}. " +
                      "Original scene untouched. Test progress is discarded on leaving Play Mode.");
        }

        private static void SetFloat(SerializedObject target, string name, float value) =>
            target.FindProperty(name).floatValue = value;

        private static IEnumerable<Vector2[]> Routes()
        {
            yield return WestRoute;
            yield return EastRoute;
            yield return ReturnRoute;
            yield return SouthBridgeRoute;
            yield return NorthWestRoute;
            yield return NorthRidgeRoute;
            yield return NorthEastRoute;
        }

        private static void AddCompletionWaypoints(Transform routes, Terrain terrain)
        {
            AddWaypoints(routes, "South - Brueckenansatz", SouthBridgeRoute, terrain);
            AddWaypoints(routes, "North - Schrein und Tor", NorthWestRoute, terrain);
            AddWaypoints(routes, "North - Hoehenweg", NorthRidgeRoute, terrain);
            AddWaypoints(routes, "North - Steinkreis und Aussicht", NorthEastRoute, terrain);
        }

        [MenuItem("Elyndor/Open Zone/Complete Finsterwald Routes")]
        public static void CompleteRoutes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode first.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved prototype scene first.");

            GameObject routes = GameObject.Find("[Prototype] Open Zone Routes");
            Terrain terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
            if (routes == null || terrain == null ||
                AssetDatabase.GetAssetPath(terrain.terrainData) != TerrainPath)
                throw new InvalidOperationException("Isolated prototype routes or terrain missing.");

            bool northComplete = routes.transform.Find("North - Hoehenweg") != null;
            bool eastRepaired = RepairEastRoute(routes.transform, terrain);
            if (northComplete && !eastRepaired)
            {
                Validate();
                return;
            }

            PaintRoutes(terrain);
            int moved = ClearRoutes(terrain);
            moved += ClearBlockingVegetation(terrain);
            if (!northComplete)
                AddCompletionWaypoints(routes.transform, terrain);
            EditorUtility.SetDirty(terrain.terrainData);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log($"OPEN_ZONE_ROUTES_COMPLETE: {moved} vegetation objects moved. " +
                      "The bridge remains the intended northern entry.");
        }

        public static float DistanceToRoutes(Vector2 point, out Vector2 closest)
        {
            float best = float.MaxValue;
            closest = point;
            foreach (Vector2[] route in Routes())
                for (int i = 1; i < route.Length; i++)
                {
                    Vector2 a = route[i - 1];
                    Vector2 segment = route[i] - a;
                    Vector2 candidate = a + segment * Mathf.Clamp01(
                        Vector2.Dot(point - a, segment) / segment.sqrMagnitude);
                    float distance = Vector2.Distance(point, candidate);
                    if (distance < best) { best = distance; closest = candidate; }
                }
            return best;
        }

        [MenuItem("Elyndor/Open Zone/Populate Encounter Flow")]
        public static void PopulateEncounterFlow()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode first.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved prototype scene first.");

            Terrain terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
            FinsterwaldRegeneration regeneration =
                UnityEngine.Object.FindAnyObjectByType<FinsterwaldRegeneration>();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                WurzelstreiferAssetBuilder.PrefabPath);
            if (terrain == null || regeneration == null || prefab == null)
                throw new InvalidOperationException(
                    "Terrain, regeneration, or Wurzelstreifer prefab missing.");

            GameObject existing = GameObject.Find(EncounterRootName);
            if (existing != null)
            {
                Validate();
                return;
            }

            GameObject root = new GameObject(EncounterRootName);
            OpenZoneEncounterGate gate = root.AddComponent<OpenZoneEncounterGate>();
            GameObject northEncounter = new GameObject(NorthEncounterName);
            northEncounter.transform.SetParent(root.transform, false);

            for (int i = 0; i < NorthEncounterPositions.Length; i++)
            {
                Vector2 point = NorthEncounterPositions[i];
                GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                enemy.name = $"Wurzelstreifer_Nordpfad_{i + 1}";
                enemy.transform.SetParent(northEncounter.transform, true);
                Vector3 position = new Vector3(point.x, 0f, point.y);
                position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
                enemy.transform.position = position;
                enemy.transform.rotation = Quaternion.Euler(0f, i == 0 ? 100f : 250f, 0f);
            }

            gate.Configure(regeneration, northEncounter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log(
                "OPEN_ZONE_ENCOUNTERS_READY: the northern Wurzelstreifer pair " +
                "activates after the forest regeneration.");
        }

        private static void PaintRoutes(Terrain terrain)
        {
            TerrainData data = terrain.terrainData;
            int pathLayer = Array.FindIndex(data.terrainLayers,
                layer => layer != null && layer.name.Contains("Pfad"));
            if (pathLayer < 0)
                throw new InvalidOperationException("Existing path terrain layer missing.");
            float[,,] weights = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            for (int z = 0; z < data.alphamapHeight; z++)
                for (int x = 0; x < data.alphamapWidth; x++)
                {
                    Vector2 world = new Vector2(
                        terrain.transform.position.x + x * data.size.x / (data.alphamapWidth - 1),
                        terrain.transform.position.z + z * data.size.z / (data.alphamapHeight - 1));
                    float distance = DistanceToRoutes(world, out _);
                    float blend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.6f, 4.6f, distance));
                    if (blend <= 0f) continue;
                    for (int layer = 0; layer < data.alphamapLayers; layer++)
                        weights[z, x, layer] = Mathf.Lerp(weights[z, x, layer], layer == pathLayer ? 1f : 0f, blend);
                }
            data.SetAlphamaps(0, 0, weights);
        }

        private static int ClearRoutes(Terrain terrain)
        {
            int moved = 0;
            string[] groups = { "Baeume", "Waldrand", "Buesche", "Farne", "Felsen", "Bodenpflanzen" };
            foreach (string group in groups)
            {
                GameObject root = GameObject.Find("Environment/Vegetation/" + group);
                if (root == null) continue;
                foreach (Transform item in root.transform)
                {
                    Vector2 point = new Vector2(item.position.x, item.position.z);
                    float distance = DistanceToRoutes(point, out Vector2 nearest);
                    if (distance >= ClearRadius) continue;
                    Vector2 direction = point - nearest;
                    if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
                    direction.Normalize();
                    Vector2 destination = point;
                    // Check the whole loop, not only the nearest segment.
                    for (int step = 0; step < 45; step++)
                    {
                        destination += direction;
                        if (DistanceToRoutes(destination, out _) >= ClearRadius + 1f) break;
                    }
                    float oldGround = terrain.SampleHeight(item.position) + terrain.transform.position.y;
                    Vector3 position = new Vector3(destination.x, 0f, destination.y);
                    position.y = terrain.SampleHeight(position) + terrain.transform.position.y + item.position.y - oldGround;
                    item.position = position;
                    moved++;
                }
            }
            return moved;
        }

        private static int ClearBlockingVegetation(Terrain terrain)
        {
            string[] names = { "Baeume", "Waldrand", "Buesche", "Farne", "Felsen", "Bodenpflanzen" };
            Transform[] roots = names
                .Select(name => GameObject.Find("Environment/Vegetation/" + name))
                .Where(group => group != null)
                .Select(group => group.transform)
                .ToArray();
            HashSet<Transform> moved = new HashSet<Transform>();

            // Imported trunk colliders can extend several metres beyond their
            // root, so centre-based clearing alone cannot guarantee passage.
            for (int pass = 0; pass < 5; pass++)
            {
                bool found = false;
                HashSet<Transform> movedThisPass = new HashSet<Transform>();
                foreach (Vector2[] route in Routes())
                    for (int i = 1; i < route.Length; i++)
                    {
                        Vector2 segment = route[i] - route[i - 1];
                        int samples = Mathf.CeilToInt(segment.magnitude);
                        for (int step = 0; step <= samples; step++)
                        {
                            Vector2 point = Vector2.Lerp(route[i - 1], route[i], step / (float)samples);
                            Vector3 ground = new Vector3(point.x, 0f, point.y);
                            ground.y = terrain.SampleHeight(ground) + terrain.transform.position.y;
                            foreach (Collider collider in Physics.OverlapSphere(ground + Vector3.up, 0.8f))
                            {
                                if (collider.isTrigger || collider is TerrainCollider) continue;
                                Transform item = VegetationItem(collider.transform, roots);
                                if (item == null || !movedThisPass.Add(item)) continue;
                                Vector3 direction = item.position - ground;
                                direction.y = 0f;
                                if (direction.sqrMagnitude < 0.01f)
                                    direction = new Vector3(-segment.y, 0f, segment.x);
                                direction.Normalize();
                                Vector3 old = item.position;
                                Vector3 next = old + direction * 3f;
                                next.y += terrain.SampleHeight(next) - terrain.SampleHeight(old);
                                item.position = next;
                                moved.Add(item);
                                found = true;
                            }
                        }
                    }
                Physics.SyncTransforms();
                if (!found) break;
            }
            return moved.Count;
        }

        private static Transform VegetationItem(Transform collider, Transform[] roots)
        {
            foreach (Transform root in roots)
                for (Transform current = collider; current != null; current = current.parent)
                    if (current.parent == root)
                        return current;
            return null;
        }

        private static bool RepairEastRoute(Transform routes, Terrain terrain)
        {
            Transform east = routes.Find("East - Rastplatz");
            if (east == null) return false;
            if (east.childCount == 6)
            {
                Transform marker = new GameObject("Waypoint 5").transform;
                marker.SetParent(east);
                marker.SetSiblingIndex(4);
            }
            if (east.childCount != EastRoute.Length) return false;

            bool changed = false;
            for (int i = 0; i < EastRoute.Length; i++)
            {
                Vector2 point = EastRoute[i];
                Vector3 destination = new Vector3(point.x, 0f, point.y);
                destination.y = terrain.SampleHeight(destination) + terrain.transform.position.y;
                Transform marker = east.GetChild(i);
                changed |= Vector3.Distance(marker.position, destination) > 0.01f;
                marker.position = destination;
                marker.name = $"Waypoint {i + 1}";
            }
            return changed;
        }

        private static void AddWaypoints(Transform parent, string name, Vector2[] points, Terrain terrain)
        {
            Transform route = new GameObject(name).transform;
            route.SetParent(parent);
            for (int i = 0; i < points.Length; i++)
            {
                Transform marker = new GameObject($"Waypoint {i + 1}").transform;
                marker.SetParent(route);
                Vector3 position = new Vector3(points[i].x, 0, points[i].y);
                position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
                marker.position = position;
            }
        }

        [MenuItem("Elyndor/Open Zone/Validate Prototype")]
        public static void Validate()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the prototype scene first.");
            SceneIntegrityProfile profile = FinsterwaldSceneIntegrityValidator.CreateProfile();
            // The production scene deliberately has one teaching enemy. The
            // prototype adds a later pair, while keeping every other integrity
            // requirement identical.
            profile.SingleInstanceTypes.Remove(typeof(EnemyController));
            List<SceneIntegrityIssue> issues = SceneIntegrityAnalyzer.Analyze(scene, profile);
            if (issues.Count != 0)
                throw new InvalidOperationException(string.Join("\n", issues.Select(issue => issue.ToString())));
            Terrain terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
            if (AssetDatabase.GetAssetPath(terrain.terrainData) != TerrainPath)
                throw new InvalidOperationException("Prototype shares production terrain data.");
            if (EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath))
                throw new InvalidOperationException("Prototype must not be in production build settings.");
            PlayerMovement player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if (player == null || player.GetComponent<OpenZoneMouseController>() == null)
                throw new InvalidOperationException("Prototype mouse control missing from Player.");
            TutorialSequence tutorial = UnityEngine.Object.FindAnyObjectByType<TutorialSequence>(
                FindObjectsInactive.Include);
            if (tutorial != null && tutorial.enabled)
                throw new InvalidOperationException("Legacy WASD tutorial is active in mouse-only prototype.");
            GameObject routeRoot = GameObject.Find("[Prototype] Open Zone Routes");
            if (routeRoot == null ||
                routeRoot.transform.Find("East - Rastplatz")?.childCount != EastRoute.Length ||
                routeRoot.transform.Find("South - Brueckenansatz") == null ||
                routeRoot.transform.Find("North - Schrein und Tor") == null ||
                routeRoot.transform.Find("North - Hoehenweg") == null ||
                routeRoot.transform.Find("North - Steinkreis und Aussicht") == null)
                throw new InvalidOperationException("Complete zone routes missing.");
            GameObject encounterRoot = GameObject.Find(EncounterRootName);
            if (encounterRoot != null)
            {
                OpenZoneEncounterGate gate =
                    encounterRoot.GetComponent<OpenZoneEncounterGate>();
                EnemyController[] enemies = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<EnemyController>(true))
                    .ToArray();
                if (gate == null || gate.Regeneration == null ||
                    gate.EncounterRoot == null || gate.EncounterRoot.activeSelf ||
                    enemies.Length != 3)
                    throw new InvalidOperationException(
                        "Prototype encounter flow is incomplete or active too early.");
            }
            Debug.Log("OPEN_ZONE_VALIDATION_PASS: scene integrity and isolated terrain.");
        }

        [MenuItem("Elyndor/Open Zone/Enable Mouse-Only Control In Prototype")]
        public static void EnableMouseControl()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode first.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException("Open the prototype scene first.");
            PlayerMovement player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if (player == null) throw new InvalidOperationException("Player missing.");
            bool changed = false;
            if (player.GetComponent<OpenZoneMouseController>() == null)
            {
                player.gameObject.AddComponent<OpenZoneMouseController>();
                changed = true;
            }
            TutorialSequence tutorial = UnityEngine.Object.FindAnyObjectByType<TutorialSequence>(
                FindObjectsInactive.Include);
            if (tutorial != null && tutorial.enabled)
            {
                tutorial.enabled = false;
                changed = true;
            }
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Validate();
        }
    }
}
