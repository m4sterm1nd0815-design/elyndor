using System;
using System.Collections.Generic;
using System.Linq;
using Elyndor.Memory;
using Elyndor.Player;
using Elyndor.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Adds a deterministic composition layer to existing region scenes.
    /// Gameplay-owned roots are never rebuilt or moved.
    /// </summary>
    public static class WorldVisualOverhaulBuilder
    {
        private const string GeneratedRootName =
            "[Generated] World Visual Overhaul";
        private const string SceneFolder = "Assets/_Elyndor/Scenes";
        private const string MaterialFolder =
            "Assets/_Elyndor/Art/Materials/Prototype";
        private const string NatureModelFolder =
            "Assets/ThirdParty/Ultimate Stylized Nature - May 2022/FBX";
        private const string NaturPackModelFolder =
            "Assets/ThirdParty/Natur Pack/FBX (Unity)";
        private const string TempleModelFolder =
            "Assets/ThirdParty/Modular Temple";
        private const string FanKitModelFolder =
            "Assets/ThirdParty/fan kit/Models/FBX format";
        private const string FinsterwaldRootGatePrefab =
            "Assets/_Elyndor/Art/Environment/Finsterwald/RootGate/Finsterwald_RootGate.prefab";

        private static Terrain terrain;

        [MenuItem("Elyndor/World Visuals/Build Current Regions")]
        public static void BuildCurrentRegions()
        {
            BuildRegion("Finsterwald", BuildFinsterwaldLayer);
            BuildRegion("Sonnenfelder", BuildSonnenfelderLayer);
            BuildRegion("Nebelmoor", BuildNebelmoorLayer);
            AssetDatabase.SaveAssets();
            Debug.Log("World visual layers built for all current regions.");
        }

        [MenuItem("Elyndor/World Visuals/Validate Current Regions")]
        public static void ValidateCurrentRegions()
        {
            int errors = 0;
            errors += ValidateRegion("Finsterwald", 1, 1);
            errors += ValidateRegion("Sonnenfelder", 1, 1);
            errors += ValidateRegion("Nebelmoor", 1, 1);

            if (errors > 0)
                throw new InvalidOperationException(
                    $"World validation failed with {errors} errors.");

            Debug.Log("World validation passed for all current regions.");
        }

        private static void BuildRegion(
            string sceneName,
            Action<Transform> buildLayer)
        {
            string path = $"{SceneFolder}/{sceneName}.unity";
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();

            if (terrain == null)
                throw new InvalidOperationException(
                    $"{sceneName}: No Terrain found.");

            GameObject previous = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == GeneratedRootName);

            if (previous != null)
                UnityEngine.Object.DestroyImmediate(previous);

            GameObject root = new GameObject(GeneratedRootName);
            buildLayer(root.transform);
            AddRegionalProbeNetwork(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildFinsterwaldLayer(Transform root)
        {
            Transform guidance = CreateGroup(root, "01_Player Guidance");
            BuildStoneThreshold(guidance, "Tutorial Threshold",
                new Vector2(0f, -45f), 12f, 7);
            PlaceModel(guidance, "Root Gate", FinsterwaldRootGatePrefab,
                new Vector2(-8f, -32f), 7f, new Vector3(0f, 18f, 0f));
            BuildStoneThreshold(guidance, "Bridge Approach",
                new Vector2(2f, -8f), 8f, 5);

            Transform moments = CreateGroup(root, "02_Memorable Moments");
            BuildPerchCircle(moments, "Link Perch - Clearing",
                new Vector2(-26f, -16f), 5.5f);
            BuildPerchCircle(moments, "Link Perch - Ancient Tree",
                new Vector2(-14f, 16f), 7f);
            BuildRuinSilhouette(moments, "Forgotten Processional Gate",
                new Vector2(-45f, 34f), 22f);
            BuildWarmRestPoint(moments, "Warm Rest Point",
                new Vector2(5f, -25f));

            Transform regeneration = CreateGroup(root,
                "03_Regeneration Foreshadowing");
            BuildSaplingCluster(regeneration, new Vector2(-54f, 8f), 13, 141);
            BuildSaplingCluster(regeneration, new Vector2(34f, 34f), 9, 142);

        }

        private static void BuildSonnenfelderLayer(Transform root)
        {
            Transform routes = CreateGroup(root, "01_Exploration Routes");
            BuildStoneThreshold(routes, "Western Arrival",
                new Vector2(-40f, -7f), 10f, 6);
            BuildIrrigationLine(routes, new Vector2(-34f, 27f),
                new Vector2(34f, 22f), 18);
            BuildOrchard(routes, new Vector2(-30f, 31f), 4, 3);

            Transform moments = CreateGroup(root, "02_Landmarks");
            BuildRuinSilhouette(moments, "Sun Dial Ruin",
                new Vector2(34f, -28f), 35f);
            BuildPerchCircle(moments, "Link Perch - Windmill View",
                new Vector2(5f, 8f), 5f);
            BuildWarmRestPoint(moments, "Orchard Shade",
                new Vector2(-28f, 24f));

            Transform contrast = CreateGroup(root, "03_Quiet Unease");
            BuildStoneThreshold(contrast, "Buried Foundation Trace",
                new Vector2(22f, -31f), 14f, 9);

            WhisperingForestSceneBuilder.BuildDustParticles(
                moments, At(new Vector2(4f, 2f), 3f),
                new Vector3(85f, 7f, 85f),
                new Color(0.94f, 0.79f, 0.42f, 0.12f), 32f);
        }

        private static void BuildNebelmoorLayer(Transform root)
        {
            Transform safeRoute = CreateGroup(root, "01_Safe High Route");
            BuildBoardwalk(safeRoute, new[]
            {
                new Vector2(-1f, -35f), new Vector2(7f, -24f),
                new Vector2(-2f, -12f), new Vector2(7f, 1f)
            });
            BuildPerchCircle(safeRoute, "Link Perch - Safe Island",
                new Vector2(7f, 1f), 4f);

            Transform moments = CreateGroup(root, "02_Fog Landmarks");
            BuildRuinSilhouette(moments, "Drowned Bell Marker",
                new Vector2(-31f, 24f), 12f);
            BuildWarmRestPoint(moments, "Raised Refuge",
                new Vector2(29f, 34f));
            BuildRootArch(moments, "Deadwood Passage",
                new Vector2(-16f, 13f), -24f);

            Transform danger = CreateGroup(root, "03_Danger Readability");
            BuildStoneThreshold(danger, "Sinking Ground Warning",
                new Vector2(14f, -13f), 10f, 7);

        }

        private static void BuildStoneThreshold(
            Transform parent, string name, Vector2 center,
            float width, int count)
        {
            Transform group = CreateGroup(parent, name);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (count - 1f);
                Vector2 point = center + Vector2.right * Mathf.Lerp(-width, width, t);
                PlaceModel(group, $"Way Stone {i + 1}",
                    $"{NaturPackModelFolder}/RockPath_Round_Small_{i % 3 + 1}.fbx", point,
                    0.42f + (i % 3) * 0.08f,
                    new Vector3(i % 2 == 0 ? -4f : 3f,
                        Mathf.Lerp(-18f, 18f, t), i % 3 - 1f));
            }
        }

        private static void BuildRootArch(
            Transform parent, string name, Vector2 center, float yaw)
        {
            Transform group = CreateGroup(parent, name);
            Material wood = LoadMaterial("Proto_Wood", "Proto_Charred");
            BuildBeam(group, "Left Root", center + new Vector2(-2.2f, 0f),
                new Vector3(0.7f, 4.8f, 0.7f), new Vector3(0f, yaw, -12f), wood);
            BuildBeam(group, "Right Root", center + new Vector2(2.2f, 0f),
                new Vector3(0.7f, 4.8f, 0.7f), new Vector3(0f, yaw, 12f), wood);
            BuildBeam(group, "Crown Root", center,
                new Vector3(5.1f, 0.65f, 0.75f), new Vector3(0f, yaw, 0f), wood, 4.2f);
        }

        private static void BuildPerchCircle(
            Transform parent, string name, Vector2 center, float radius)
        {
            Transform group = CreateGroup(parent, name);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                PlaceModel(group, $"Perch Stone {i + 1}",
                    $"{NaturPackModelFolder}/Rock_Medium_{i % 3 + 1}.fbx", point,
                    1.05f + i * 0.08f,
                    new Vector3(i % 2 == 0 ? 4f : -3f, i * 47f, 0f));
            }
        }

        private static void BuildRuinSilhouette(
            Transform parent, string name, Vector2 center, float yaw)
        {
            Transform group = CreateGroup(parent, name);
            PlaceModel(group, "Standing Pier",
                $"{FanKitModelFolder}/column-damaged.fbx",
                center + new Vector2(-2.3f, 0f), 5.2f,
                new Vector3(-2f, yaw, 0f));
            PlaceModel(group, "Broken Pier",
                $"{FanKitModelFolder}/column-damaged.fbx",
                center + new Vector2(2.2f, 0.2f), 3.5f,
                new Vector3(7f, yaw + 9f, -3f));
            PlaceModel(group, "Fallen Arch",
                $"{TempleModelFolder}/Floor_Ruined_Straight_1.obj",
                center + new Vector2(0.5f, -1.2f), 2.1f,
                new Vector3(74f, yaw - 12f, 8f));
        }

        private static void BuildWarmRestPoint(
            Transform parent, string name, Vector2 center)
        {
            Transform group = CreateGroup(parent, name);
            Material stone = LoadMaterial("Proto_Stone");
            Material wood = LoadMaterial("Proto_Wood");

            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.1f;
                Primitive(group, $"Hearth Stone {i + 1}", PrimitiveType.Cube,
                    point, new Vector3(0.55f, 0.3f, 0.45f), stone);
            }

            BuildBeam(group, "Seat Log", center + new Vector2(0f, -2.2f),
                new Vector3(4f, 0.55f, 0.7f), new Vector3(0f, 8f, 0f), wood);

            GameObject lightObject = new GameObject("Warm Guidance Light");
            lightObject.transform.SetParent(group);
            lightObject.transform.position = At(center, 1.3f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.32f);
            light.intensity = 1.1f;
            light.range = 7f;
            light.shadows = LightShadows.None;
        }

        private static void BuildSaplingCluster(
            Transform parent, Vector2 center, int count, int seed)
        {
            Transform group = CreateGroup(parent, $"Young Growth {seed}");
            System.Random random = new System.Random(seed);

            for (int i = 0; i < count; i++)
            {
                Vector2 point = center + UnityRandomInsideCircle(random) * 6f;
                int variant = i % 5 + 1;
                PlaceModel(group, $"Sapling {i + 1}",
                    $"{NatureModelFolder}/NormalTree_{variant}.fbx", point,
                    2.2f + (float)random.NextDouble() * 1.8f,
                    new Vector3(0f, (float)random.NextDouble() * 360f, 0f));
            }
        }

        private static void BuildIrrigationLine(
            Transform parent, Vector2 from, Vector2 to, int segments)
        {
            Transform group = CreateGroup(parent, "Old Irrigation Channel");
            for (int i = 0; i < segments; i++)
            {
                float t = i / (segments - 1f);
                Vector2 point = Vector2.Lerp(from, to, t) +
                    Vector2.up * Mathf.Sin(t * Mathf.PI * 3f) * 1.7f;
                PlaceModel(group, $"Channel Stone {i + 1}",
                    $"{NaturPackModelFolder}/RockPath_Round_Small_{i % 3 + 1}.fbx", point,
                    0.35f + (i % 3) * 0.06f,
                    new Vector3(0f, i * 37f, i % 2 == 0 ? 2f : -2f));
            }
        }

        private static void BuildOrchard(
            Transform parent, Vector2 center, int columns, int rows)
        {
            Transform group = CreateGroup(parent, "Old Orchard");
            for (int z = 0; z < rows; z++)
            for (int x = 0; x < columns; x++)
            {
                Vector2 point = center + new Vector2((x - 1.5f) * 4.5f, (z - 1f) * 4.5f);
                int variant = (x + z * columns) % 5 + 1;
                PlaceModel(group, $"Orchard Tree {x}-{z}",
                    $"{NatureModelFolder}/NormalTree_{variant}.fbx", point,
                    5.2f + ((x + z) % 3) * 0.45f,
                    new Vector3(0f, x * 43f + z * 19f, 0f));
            }
        }

        private static void BuildBoardwalk(Transform parent, Vector2[] points)
        {
            Transform group = CreateGroup(parent, "Raised Boardwalk");
            Material wood = LoadMaterial("Proto_Wood");
            int index = 0;

            for (int p = 0; p < points.Length - 1; p++)
            {
                float distance = Vector2.Distance(points[p], points[p + 1]);
                int count = Mathf.CeilToInt(distance / 1.25f);

                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)count;
                    Vector2 point = Vector2.Lerp(points[p], points[p + 1], t);
                    GameObject plank = Primitive(group, $"Plank {++index}",
                        PrimitiveType.Cube, point,
                        new Vector3(1.05f + (index % 3) * 0.08f,
                            0.18f + (index % 2) * 0.04f,
                            2.25f + (index % 4) * 0.12f), wood, 0.42f);
                    Vector2 direction = points[p + 1] - points[p];
                    plank.transform.rotation = Quaternion.Euler(
                        index % 2 == 0 ? 1.5f : -1f,
                        90f - Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg +
                        (index % 3 - 1) * 1.8f,
                        index % 2 == 0 ? 0.8f : -0.8f);
                }
            }
        }

        private static void AddRegionalProbeNetwork(Transform parent)
        {
            GameObject probes = new GameObject("04_Lighting Probes");
            probes.transform.SetParent(parent);

            LightProbeGroup group = probes.AddComponent<LightProbeGroup>();
            List<Vector3> positions = new List<Vector3>();

            for (int z = -35; z <= 35; z += 17)
            for (int x = -35; x <= 35; x += 17)
            {
                Vector2 point = new Vector2(x, z);
                float ground = Ground(point);
                positions.Add(new Vector3(x, ground + 1.5f, z));
                positions.Add(new Vector3(x, ground + 4.5f, z));
            }

            group.probePositions = positions.ToArray();
        }

        private static int ValidateRegion(
            string sceneName, int minimumPortals, int minimumMemorySites)
        {
            string path = $"{SceneFolder}/{sceneName}.unity";
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int errors = 0;

            errors += RequireCount<Terrain>(sceneName, 1, 1);
            errors += RequireCount<PlayerMovement>(sceneName, 1, 1);
            errors += RequireCount<RegionPortal>(sceneName, minimumPortals, int.MaxValue);
            errors += RequireCount<RegionSpawnPoint>(sceneName, 1, int.MaxValue);
            errors += RequireCount<MemorySite>(sceneName, minimumMemorySites, int.MaxValue);

            int layers = scene.GetRootGameObjects()
                .Count(root => root.name == GeneratedRootName);
            if (layers != 1)
            {
                Debug.LogError($"{sceneName}: Expected one visual layer, found {layers}.");
                errors++;
            }

            if (sceneName == "Finsterwald" && !scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Any(renderer => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                        renderer.gameObject) == FinsterwaldRootGatePrefab))
            {
                Debug.LogError("Finsterwald: Imported Root Gate prefab is missing.");
                errors++;
            }
            if (scene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<MonoBehaviour>(true))
                .Any(component => component == null))
            {
                Debug.LogError($"{sceneName}: Missing script reference found.");
                errors++;
            }

            return errors;
        }

        private static int RequireCount<T>(
            string sceneName, int minimum, int maximum) where T : Component
        {
            int count = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include).Length;
            if (count >= minimum && count <= maximum)
                return 0;

            Debug.LogError($"{sceneName}: {typeof(T).Name} count is {count}; " +
                           $"expected {minimum}..{maximum}.");
            return 1;
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent);
            return group.transform;
        }

        private static GameObject PlaceModel(
            Transform parent, string name, string assetPath,
            Vector2 position, float targetHeight, Vector3 rotation)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (source == null)
                throw new InvalidOperationException($"Required world model is missing: {assetPath}");

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate world model: {assetPath}");

            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.SetPositionAndRotation(
                new Vector3(position.x, Ground(position), position.y),
                Quaternion.Euler(rotation));

            Bounds bounds = CalculateRendererBounds(instance);
            float scale = targetHeight / Mathf.Max(bounds.size.y, 0.01f);
            instance.transform.localScale *= scale;
            bounds = CalculateRendererBounds(instance);
            instance.transform.position += new Vector3(
                position.x - bounds.center.x,
                Ground(position) - bounds.min.y,
                position.y - bounds.center.z);

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccludeeStatic);
            }

            return instance;
        }

        private static Bounds CalculateRendererBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException($"World model has no renderer: {instance.name}");

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static GameObject Primitive(
            Transform parent, string name, PrimitiveType type,
            Vector2 position, Vector3 scale, Material material,
            float verticalOffset = 0f)
        {
            GameObject instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.position = At(position, scale.y * 0.5f + verticalOffset);
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(instance,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccludeeStatic);
            return instance;
        }

        private static void BuildBeam(
            Transform parent, string name, Vector2 position,
            Vector3 scale, Vector3 rotation, Material material,
            float verticalOffset = 0f)
        {
            GameObject beam = Primitive(parent, name, PrimitiveType.Cube,
                position, scale, material, verticalOffset);
            beam.transform.rotation = Quaternion.Euler(rotation);
        }

        private static Vector3 At(Vector2 point, float offset)
        {
            return new Vector3(point.x, Ground(point) + offset, point.y);
        }

        private static float Ground(Vector2 point)
        {
            return terrain.SampleHeight(new Vector3(point.x, 0f, point.y)) +
                   terrain.transform.position.y;
        }

        private static Vector2 UnityRandomInsideCircle(
            System.Random random)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float radius = Mathf.Sqrt((float)random.NextDouble());
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private static Material LoadMaterial(params string[] names)
        {
            foreach (string name in names)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(
                    $"{MaterialFolder}/{name}.mat");
                if (material != null)
                    return material;
            }

            throw new InvalidOperationException(
                $"None of the required materials exists: {string.Join(", ", names)}");
        }
    }
}
