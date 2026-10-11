using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elyndor.Cameras;
using Elyndor.EditorTools.SceneIntegrity;
using Elyndor.Player;
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

        // Both choices remain south of the river: the memory bridge is not bypassed.
        public static readonly Vector2[] WestRoute =
        {
            new Vector2(-3, -70), new Vector2(-17, -62), new Vector2(-30, -50),
            new Vector2(-38, -35), new Vector2(-33, -24), new Vector2(-26, -16)
        };
        public static readonly Vector2[] EastRoute =
        {
            new Vector2(-3, -70), new Vector2(12, -62), new Vector2(23, -49),
            new Vector2(26, -36), new Vector2(17, -27), new Vector2(5, -25)
        };
        public static readonly Vector2[] ReturnRoute =
        {
            new Vector2(-26, -16), new Vector2(-17, -22),
            new Vector2(-8, -29), new Vector2(5, -25)
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
            Vector3 focus = player.position + Vector3.up * 1.2f;
            follow.transform.position = focus + Quaternion.Euler(60f, 0f, 0f) * Vector3.back * 21f;
            follow.transform.LookAt(focus);

            GameObject routes = new GameObject("[Prototype] Open Zone Routes");
            AddWaypoints(routes.transform, "West - Lichtung", WestRoute, terrain);
            AddWaypoints(routes.transform, "East - Rastplatz", EastRoute, terrain);
            AddWaypoints(routes.transform, "Return - Waldschleife", ReturnRoute, terrain);
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
            List<SceneIntegrityIssue> issues = SceneIntegrityAnalyzer.Analyze(
                scene, FinsterwaldSceneIntegrityValidator.CreateProfile());
            if (issues.Count != 0)
                throw new InvalidOperationException(string.Join("\n", issues.Select(issue => issue.ToString())));
            Terrain terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
            if (AssetDatabase.GetAssetPath(terrain.terrainData) != TerrainPath)
                throw new InvalidOperationException("Prototype shares production terrain data.");
            if (EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath))
                throw new InvalidOperationException("Prototype must not be in production build settings.");
            Debug.Log("OPEN_ZONE_VALIDATION_PASS: scene integrity and isolated terrain.");
        }
    }
}
