using System.Collections.Generic;
using System.IO;
using Elyndor.Interaction;
using Elyndor.Memory;
using Elyndor.Player;
using Elyndor.UI;
using Elyndor.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Baut die Regionen Sonnenfelder und Nebelmoor deterministisch als
    /// Kopien der Bootstrap-Szene — kleiner und einfacher als der
    /// Finsterwald, aber mit eigener Stimmung, eigenem Erinnerungsort und
    /// Portal-Anbindung. Bewusst eigenständig gehalten; gemeinsame
    /// Builder-Basis mit dem WhisperingForestSceneBuilder ist als
    /// technische Schuld dokumentiert.
    /// </summary>
    public static class RegionSceneBuilder
    {
        private const string SourceScenePath = "Assets/_Elyndor/Scenes/Bootstrap.unity";
        private const string MaterialFolder = "Assets/_Elyndor/Art/Materials/Prototype";
        private const string PostFxFolder = "Assets/_Elyndor/Art/PostProcessing";
        private const string TerrainFolder = "Assets/_Elyndor/Art/Terrain";
        private const string PackModelFolder = "Assets/ThirdParty/Natur Pack/FBX (Unity)";

        private const float TerrainSize = 110f;
        private const float TerrainHeight = 16f;
        private const float BaseLevel = 6f;
        private const int HeightmapResolution = 257;
        private const int AlphamapResolution = 256;
        private const float PlayableRadius = 48f;
        private const float PlayerScale = 0.75f;

        private static Terrain regionTerrain;
        private static List<Vector2> pathSamples;
        private static System.Func<float, float, float> rawHeightFunction;
        private static readonly Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, float> modelHeightCache = new Dictionary<string, float>();
        private static List<Vector3> placedTreePositions;

        [MenuItem("Elyndor/Setup/Alle Regionen erstellen")]
        public static void BuildAllRegions()
        {
            WhisperingForestSceneBuilder.BuildScene();
            BuildSonnenfelder();
            BuildNebelmoor();
        }

        // ==================================================================
        // SONNENFELDER
        // ==================================================================

        [MenuItem("Elyndor/Setup/Sonnenfelder erstellen")]
        public static void BuildSonnenfelder()
        {
            ResetState();

            Vector2[][] paths =
            {
                // Hauptweg West (Portal Wald) -> Ost
                new[]
                {
                    new Vector2(-50f, -8f), new Vector2(-40f, -6f), new Vector2(-28f, -8f),
                    new Vector2(-14f, -6f), new Vector2(0f, -4f), new Vector2(14f, -2f),
                    new Vector2(26f, 0f), new Vector2(38f, 2f), new Vector2(46f, 2f)
                },
                // Abzweig nach Norden (Portal Nebelmoor)
                new[]
                {
                    new Vector2(26f, 0f), new Vector2(28f, 14f), new Vector2(30f, 28f),
                    new Vector2(30f, 42f), new Vector2(30f, 48f)
                },
                // Stichweg zur Muehle
                new[]
                {
                    new Vector2(14f, -2f), new Vector2(13f, 8f), new Vector2(12f, 16f)
                }
            };

            rawHeightFunction = (wx, wz) =>
            {
                float rolling = (Mathf.PerlinNoise(wx * 0.025f + 91.2f, wz * 0.025f + 44.7f) - 0.5f) * 2.4f;
                float detail = (Mathf.PerlinNoise(wx * 0.1f + 5.1f, wz * 0.1f + 9.6f) - 0.5f) * 0.5f;
                float height = rolling + detail;
                height += Hill(wx, wz, new Vector2(-34f, 34f), 18f, 2.6f);
                height = Flatten(height, wx, wz, new Vector2(12f, 18f), 9f, 0.5f, 0.8f);
                return height;
            };

            Scene scene = OpenRegionScene("Sonnenfelder");
            SamplePaths(paths);

            ConfigureAtmosphere(
                sunColor: new Color(1f, 0.9f, 0.72f), sunIntensity: 1.15f,
                sunRotation: new Vector3(45f, -30f, 0f),
                fogColor: new Color(0.72f, 0.68f, 0.55f), fogDensity: 0.008f,
                ambientSky: new Color(0.65f, 0.62f, 0.5f),
                ambientEquator: new Color(0.5f, 0.47f, 0.36f),
                ambientGround: new Color(0.3f, 0.28f, 0.2f),
                skyboxName: "Sky_Sonnenfelder",
                skyTint: new Color(0.62f, 0.6f, 0.52f)
            );
            ConfigureVolumes("Sonnenfelder", vignette: 0.16f, saturation: -4f);

            CreateRegionTerrain(
                "Sonnenfelder",
                new[]
                {
                    ("Terrain_GoldWiese", new Color(0.45f, 0.42f, 0.2f)),
                    ("Terrain_Feld", new Color(0.52f, 0.44f, 0.24f)),
                    ("Terrain_Pfad", new Color(0.42f, 0.35f, 0.25f))
                },
                SunfieldsAlphaWeights
            );

            GameObject player = PreparePlayer(new Vector2(-47f, -8f));
            GameObject environment = new GameObject("Environment");
            Transform env = environment.transform;

            BuildWorldBounds(env);
            BuildWindmillSite(env);
            BuildFarmstead(env);
            BuildCropRows(env);
            ScatterSunfieldVegetation(env);

            BuildPortal(env, "Portal_Finsterwald", new Vector2(-47.5f, -8f),
                "Finsterwald", "von_sonnenfeldern");
            BuildSpawn(env, "von_wald", new Vector2(-44.5f, -8f));
            BuildPortal(env, "Portal_Nebelmoor", new Vector2(30f, 47f),
                "Nebelmoor", "von_sonnenfeldern");
            BuildSpawn(env, "von_moor", new Vector2(30f, 43.5f));

            // Pollenstaub im goldenen Licht.
            GameObject fieldAtmosphere = new GameObject("Atmosphaere");
            fieldAtmosphere.transform.SetParent(env);
            WhisperingForestSceneBuilder.BuildDustParticles(fieldAtmosphere.transform,
                new Vector3(0f, 4f, 0f), new Vector3(100f, 8f, 100f),
                new Color(0.95f, 0.85f, 0.5f, 0.14f), 55f);

            BuildSign(env, new Vector2(28f, 44f), "Wegweiser lesen",
                "„Nach Norden: Das Nebelmoor.“\n" +
                "Die Schrift ist feucht verlaufen. Jemand hat ein kleines " +
                "Boot daneben geritzt — oder ist es ein Steg?");

            FinishScene(scene, player);
            Debug.Log("Region erstellt: Sonnenfelder");
        }

        private static float[] SunfieldsAlphaWeights(Vector2 position)
        {
            float path = PathWeight(position);

            // Kornfelder als weiche Rechtecke.
            float field = Mathf.Max(
                RectWeight(position, new Vector2(-6f, 12f), new Vector2(14f, 9f)),
                RectWeight(position, new Vector2(22f, -16f), new Vector2(12f, 8f))
            );
            field = Mathf.Max(field, RectWeight(position, new Vector2(-28f, -22f), new Vector2(10f, 7f)));

            return new[] { path, field * 0.85f };
        }

        private static void BuildWindmillSite(Transform parent)
        {
            Vector2 sitePoint = new Vector2(12f, 18f);
            GameObject area = new GameObject("Alte Muehle");
            area.transform.SetParent(parent);

            Material stone = LoadMaterial("Proto_Stone");

            // Das steinerne Fundament ist alles, was geblieben ist.
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = "Fundamentstein";
                block.transform.SetParent(area.transform);
                Vector2 position = sitePoint + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2.4f;
                block.transform.position = new Vector3(position.x, GroundY(position) + 0.2f, position.y);
                block.transform.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
                block.transform.localScale = new Vector3(1.4f, 0.5f, 0.7f);
                ApplyMaterial(block, stone);
                MarkStatic(block);
            }

            // Geister-Windmuehle als Erinnerung.
            GameObject ghost = new GameObject("GhostWindmill");
            ghost.transform.SetParent(area.transform);
            ghost.transform.position = new Vector3(sitePoint.x, GroundY(sitePoint), sitePoint.y);

            GameObject visual = new GameObject("GhostWindmill_Visual");
            visual.transform.SetParent(ghost.transform);
            visual.transform.localPosition = Vector3.zero;

            Material ghostMaterial = LoadMaterial("Proto_MemoryGhost");

            BuildGhostPart(visual.transform, "Turm", new Vector3(0f, 3.5f, 0f), new Vector3(2.6f, 7f, 2.6f), ghostMaterial, cylinder: true);
            BuildGhostPart(visual.transform, "Kappe", new Vector3(0f, 7.4f, 0f), new Vector3(3f, 1f, 3f), ghostMaterial, cylinder: false);

            for (int i = 0; i < 4; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = "Fluegel";
                blade.transform.SetParent(visual.transform);
                blade.transform.localPosition = new Vector3(0f, 6.8f, -1.9f);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f + 25f);
                blade.transform.localScale = new Vector3(0.5f, 5.6f, 0.15f);
                blade.transform.Translate(0f, 2.4f, 0f, Space.Self);
                Object.DestroyImmediate(blade.GetComponent<Collider>());
                blade.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                ApplyMaterial(blade, ghostMaterial);
            }

            MemoryEcho echo = ghost.AddComponent<MemoryEcho>();
            SerializedObject serializedEcho = new SerializedObject(echo);
            serializedEcho.FindProperty("visualRoot").objectReferenceValue = visual;
            serializedEcho.FindProperty("fadeDuration").floatValue = 2.5f;
            serializedEcho.ApplyModifiedPropertiesWithoutUndo();

            BuildMemorySiteObject(area.transform, sitePoint + new Vector2(-4f, -2f), echo,
                "sonnenfelder_muehle_01",
                "Die Memory Watch dreht sich warm um Arens Handgelenk. Ueber dem " +
                "alten Fundament erheben sich Fluegel aus Licht und drehen sich in " +
                "einem Wind, der vor langer Zeit wehte. Irgendwo lacht ein Mueller, " +
                "den niemand mehr beim Namen nennt.");
        }

        private static void BuildFarmstead(Transform parent)
        {
            Vector2 farmPoint = new Vector2(-20f, 16f);
            GameObject farm = new GameObject("Verlassener Hof");
            farm.transform.SetParent(parent);

            Material stone = LoadMaterial("Proto_Stone");
            Material wood = LoadMaterial("Proto_Wood");

            BuildBlock(farm.transform, farmPoint + new Vector2(0f, 2.5f), new Vector3(6f, 1.4f, 0.5f), 0f, stone, "Mauerrest");
            BuildBlock(farm.transform, farmPoint + new Vector2(-2.8f, 0.8f), new Vector3(0.5f, 1.8f, 3.4f), 0f, stone, "Mauerrest");
            BuildBlock(farm.transform, farmPoint + new Vector2(2.6f, 0.2f), new Vector3(0.5f, 0.9f, 2.2f), 6f, stone, "Mauerrest");

            // Zaunreihe, halb verfallen.
            for (int i = 0; i < 6; i++)
            {
                Vector2 position = farmPoint + new Vector2(-5f + i * 1.6f, -4f);
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "Zaunpfahl";
                post.transform.SetParent(farm.transform);
                post.transform.position = new Vector3(position.x, GroundY(position) + 0.4f, position.y);
                post.transform.localScale = new Vector3(0.12f, 0.4f, 0.12f);
                post.transform.rotation = Quaternion.Euler((i * 37f) % 14f - 7f, 0f, (i * 53f) % 12f - 6f);
                ApplyMaterial(post, wood);
                MarkStatic(post);
            }

            AddExaminableSphere(farm.transform, farmPoint, 3f, "Hof untersuchen",
                "Ein Herdstein, kalt seit Jahren. In der Tuerschwelle sind Kerben — " +
                "eine fuer jedes Erntejahr. Die letzten Kerben werden immer flacher, " +
                "als haette die Hand vergessen, warum sie zaehlte.");
        }

        private static void BuildCropRows(Transform parent)
        {
            GameObject crops = new GameObject("Kornfelder");
            crops.transform.SetParent(parent);

            System.Random rng = new System.Random(53);

            PlantField(crops.transform, new Vector2(-6f, 12f), new Vector2(14f, 9f), rng);
            PlantField(crops.transform, new Vector2(22f, -16f), new Vector2(12f, 8f), rng);
            PlantField(crops.transform, new Vector2(-28f, -22f), new Vector2(10f, 7f), rng);
        }

        private static void PlantField(Transform parent, Vector2 center, Vector2 halfSize, System.Random rng)
        {
            for (float x = -halfSize.x; x <= halfSize.x; x += 1.3f)
            {
                for (float z = -halfSize.y; z <= halfSize.y; z += 1.3f)
                {
                    if (rng.NextDouble() < 0.2)
                    {
                        continue;
                    }

                    Vector2 position = center + new Vector2(
                        x + (float)(rng.NextDouble() * 0.5 - 0.25),
                        z + (float)(rng.NextDouble() * 0.5 - 0.25)
                    );

                    if (PathDistance(position) < 1.6f)
                    {
                        continue;
                    }

                    PlaceModel("Grass_Common_Tall", parent, position,
                        (float)rng.NextDouble() * 360f, TargetScale("Grass_Common_Tall", 0.75f));
                }
            }
        }

        private static void ScatterSunfieldVegetation(Transform parent)
        {
            GameObject vegetation = new GameObject("Vegetation");
            vegetation.transform.SetParent(parent);
            System.Random rng = new System.Random(59);

            // Einzelne Feldbaeume — weite, offene Landschaft.
            int trees = 0;
            int attempts = 0;

            while (trees < 34 && attempts < 900)
            {
                attempts++;
                Vector2 position = RandomPosition(rng);

                if (PathDistance(position) < 4f) continue;
                if (!IsClearOfTrees(position, 6f)) continue;
                if (RectWeight(position, new Vector2(-6f, 12f), new Vector2(14f, 9f)) > 0.2f) continue;
                if (RectWeight(position, new Vector2(22f, -16f), new Vector2(12f, 8f)) > 0.2f) continue;
                if (Vector2.Distance(position, new Vector2(12f, 18f)) < 8f) continue;
                if (Vector2.Distance(position, new Vector2(-20f, 16f)) < 7f) continue;

                string model = $"CommonTree_{1 + rng.Next(5)}";
                PlaceTree(model, vegetation.transform, position, 8.5f, rng);
                trees++;
            }

            BuildBorderTrees(vegetation.transform, rng, pineChance: 0.25);
            ScatterSimple(vegetation.transform, rng, "Bush_Common", 40, 1.1f, 2.5f);
            ScatterSimple(vegetation.transform, rng, "Flower_3_Group", 30, 0.45f, 2f);
            ScatterSimple(vegetation.transform, rng, "Grass_Wispy_Tall", 120, 0.5f, 1.6f);
            ScatterSimple(vegetation.transform, rng, "Pebble_Round_3", 25, 0.3f, 1.6f);
        }

        // ==================================================================
        // NEBELMOOR
        // ==================================================================

        private static readonly Vector2[] PoolCenters =
        {
            new Vector2(-18f, -22f), new Vector2(16f, -14f), new Vector2(-12f, 8f),
            new Vector2(18f, 20f), new Vector2(4f, 34f)
        };

        private static readonly float[] PoolRadii = { 7f, 6f, 8f, 6.5f, 10f };

        [MenuItem("Elyndor/Setup/Nebelmoor erstellen")]
        public static void BuildNebelmoor()
        {
            ResetState();

            Vector2[][] paths =
            {
                new[]
                {
                    new Vector2(0f, -48f), new Vector2(-6f, -38f), new Vector2(2f, -26f),
                    new Vector2(-4f, -14f), new Vector2(4f, -2f), new Vector2(-2f, 10f),
                    new Vector2(4f, 22f), new Vector2(4f, 26f)
                }
            };

            rawHeightFunction = (wx, wz) =>
            {
                float rolling = (Mathf.PerlinNoise(wx * 0.03f + 12.9f, wz * 0.03f + 71.4f) - 0.5f) * 1.4f;
                float height = rolling;

                for (int i = 0; i < PoolCenters.Length; i++)
                {
                    height -= Hill(wx, wz, PoolCenters[i], PoolRadii[i], 1.1f);
                }

                return height;
            };

            Scene scene = OpenRegionScene("Nebelmoor");
            SamplePaths(paths);

            ConfigureAtmosphere(
                sunColor: new Color(0.75f, 0.8f, 0.82f), sunIntensity: 0.7f,
                sunRotation: new Vector3(32f, -50f, 0f),
                fogColor: new Color(0.47f, 0.51f, 0.49f), fogDensity: 0.045f,
                ambientSky: new Color(0.4f, 0.44f, 0.44f),
                ambientEquator: new Color(0.3f, 0.33f, 0.32f),
                ambientGround: new Color(0.14f, 0.16f, 0.15f),
                skyboxName: "Sky_Nebelmoor",
                skyTint: new Color(0.42f, 0.46f, 0.46f)
            );
            ConfigureVolumes("Nebelmoor", vignette: 0.3f, saturation: -18f);

            CreateRegionTerrain(
                "Nebelmoor",
                new[]
                {
                    ("Terrain_Moorboden", new Color(0.16f, 0.2f, 0.15f)),
                    ("Terrain_Schlamm", new Color(0.24f, 0.22f, 0.17f)),
                    ("Terrain_MoorPfad", new Color(0.3f, 0.27f, 0.2f))
                },
                MoorAlphaWeights
            );

            GameObject player = PreparePlayer(new Vector2(0f, -45f));
            GameObject environment = new GameObject("Environment");
            Transform env = environment.transform;

            BuildWorldBounds(env);
            BuildPools(env);
            BuildJettySite(env);
            BuildSunkenCart(env);
            ScatterMoorVegetation(env);

            BuildPortal(env, "Portal_Sonnenfelder", new Vector2(0f, -47.5f),
                "Sonnenfelder", "von_moor");
            BuildSpawn(env, "von_sonnenfeldern", new Vector2(0f, -44f));

            // Traege Nebelschwaden ueber dem Moor.
            GameObject moorAtmosphere = new GameObject("Atmosphaere");
            moorAtmosphere.transform.SetParent(env);
            WhisperingForestSceneBuilder.BuildDustParticles(moorAtmosphere.transform,
                new Vector3(0f, 2.5f, 0f), new Vector3(100f, 5f, 100f),
                new Color(0.6f, 0.68f, 0.62f, 0.1f), 50f);
            WhisperingForestSceneBuilder.BuildGlowParticles(moorAtmosphere.transform,
                "Irrlichter_Steg", new Vector3(4f, 1.2f, 30f), 9f,
                new Color(0.55f, 0.75f, 0.7f, 0.6f), 6f);

            FinishScene(scene, player);
            Debug.Log("Region erstellt: Nebelmoor");
        }

        private static float[] MoorAlphaWeights(Vector2 position)
        {
            float path = PathWeight(position);

            float mud = 0f;
            for (int i = 0; i < PoolCenters.Length; i++)
            {
                mud = Mathf.Max(mud, RadialWeight(position, PoolCenters[i], PoolRadii[i] + 3f));
            }

            return new[] { path, mud * 0.9f };
        }

        private static void BuildPools(Transform parent)
        {
            GameObject pools = new GameObject("Moortuempel");
            pools.transform.SetParent(parent);

            Material water = LoadMaterial("Proto_Water");

            for (int i = 0; i < PoolCenters.Length; i++)
            {
                Vector2 center = PoolCenters[i];
                float bedY = GroundY(center);

                GameObject pool = GameObject.CreatePrimitive(PrimitiveType.Plane);
                pool.name = "Tuempel";
                pool.transform.SetParent(pools.transform);
                pool.transform.position = new Vector3(center.x, bedY + 0.45f, center.y);
                pool.transform.localScale = Vector3.one * (PoolRadii[i] * 0.19f);
                Object.DestroyImmediate(pool.GetComponent<Collider>());
                pool.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                ApplyMaterial(pool, water);
                MarkStatic(pool);
            }
        }

        private static void BuildJettySite(Transform parent)
        {
            Vector2 sitePoint = new Vector2(4f, 26f);
            Vector2 poolCenter = PoolCenters[4];

            GameObject area = new GameObject("Alter Steg");
            area.transform.SetParent(parent);

            Material wood = LoadMaterial("Proto_Wood");
            Material ghostMaterial = LoadMaterial("Proto_MemoryGhost");

            // Zwei morsche Pfaehle am Ufer — mehr ist nicht geblieben.
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = sitePoint + new Vector2(-0.6f + i * 1.2f, 1.5f);
                GameObject pile = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pile.name = "Morscher Pfahl";
                pile.transform.SetParent(area.transform);
                pile.transform.position = new Vector3(position.x, GroundY(position) + 0.35f, position.y);
                pile.transform.localScale = new Vector3(0.18f, 0.5f, 0.18f);
                pile.transform.rotation = Quaternion.Euler(i * 9f - 4f, 0f, 7f - i * 12f);
                ApplyMaterial(pile, wood);
                MarkStatic(pile);
            }

            // Geister-Steg: Planken aus Licht fuehren ueber den Tuempel.
            GameObject ghost = new GameObject("GhostJetty");
            ghost.transform.SetParent(area.transform);
            ghost.transform.position = new Vector3(sitePoint.x, GroundY(sitePoint), sitePoint.y);

            GameObject visual = new GameObject("GhostJetty_Visual");
            visual.transform.SetParent(ghost.transform);
            visual.transform.localPosition = Vector3.zero;

            Vector2 direction = (poolCenter - sitePoint).normalized;
            for (int i = 0; i < 8; i++)
            {
                Vector2 position = sitePoint + direction * (2f + i * 1.7f);
                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = "Geisterplanke";
                plank.transform.SetParent(visual.transform);
                plank.transform.position = new Vector3(position.x, GroundY(sitePoint) + 0.5f, position.y);
                plank.transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));
                plank.transform.localScale = new Vector3(1.4f, 0.1f, 1.5f);
                Object.DestroyImmediate(plank.GetComponent<Collider>());
                plank.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                ApplyMaterial(plank, ghostMaterial);
            }

            MemoryEcho echo = ghost.AddComponent<MemoryEcho>();
            SerializedObject serializedEcho = new SerializedObject(echo);
            serializedEcho.FindProperty("visualRoot").objectReferenceValue = visual;
            serializedEcho.FindProperty("fadeDuration").floatValue = 2.5f;
            serializedEcho.ApplyModifiedPropertiesWithoutUndo();

            BuildMemorySiteObject(area.transform, sitePoint + new Vector2(-2.5f, -1f), echo,
                "nebelmoor_steg_01",
                "Die Memory Watch wird schwer wie nasses Holz. Ueber dem schwarzen " +
                "Wasser erscheinen Planken aus Licht — der alte Steg der Moorleute. " +
                "Man hoerte ihre Laternen frueher bis zum Wald, sagt man. Dann kam " +
                "das Vergessen, und niemand fand den Weg mehr hinaus.");
        }

        private static void BuildSunkenCart(Transform parent)
        {
            Vector2 cartPoint = new Vector2(-16f, -6f);
            GameObject cart = new GameObject("Versunkener Karren");
            cart.transform.SetParent(parent);

            Material wood = LoadMaterial("Proto_Wood");

            BuildBlock(cart.transform, cartPoint, new Vector3(1.4f, 0.5f, 2.2f), 24f, wood, "Wagenkasten");

            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Rad";
            wheel.transform.SetParent(cart.transform);
            wheel.transform.position = new Vector3(cartPoint.x + 1.1f, GroundY(cartPoint) + 0.5f, cartPoint.y - 0.6f);
            wheel.transform.rotation = Quaternion.Euler(14f, 30f, 90f);
            wheel.transform.localScale = new Vector3(1f, 0.08f, 1f);
            ApplyMaterial(wheel, wood);
            MarkStatic(wheel);

            AddExaminableSphere(cart.transform, cartPoint, 2.5f, "Karren untersuchen",
                "Ein Haendlerkarren, bis zur Achse im Moor. Die Ladung ist fort — " +
                "aber unter dem Sitz klemmt eine Kinderzeichnung: eine Muehle mit " +
                "vier Fluegeln und eine winkende Familie davor.");
        }

        private static void ScatterMoorVegetation(Transform parent)
        {
            GameObject vegetation = new GameObject("Vegetation");
            vegetation.transform.SetParent(parent);
            System.Random rng = new System.Random(61);

            int trees = 0;
            int attempts = 0;

            while (trees < 90 && attempts < 2200)
            {
                attempts++;
                Vector2 position = RandomPosition(rng);

                if (PathDistance(position) < 3f) continue;
                if (!IsClearOfTrees(position, 3.2f)) continue;
                if (IsInPool(position, 2f)) continue;
                if (Vector2.Distance(position, new Vector2(4f, 26f)) < 5f) continue;

                string model = rng.NextDouble() < 0.55
                    ? $"DeadTree_{1 + rng.Next(5)}"
                    : $"TwistedTree_{1 + rng.Next(5)}";
                PlaceTree(model, vegetation.transform, position, 6.5f, rng);
                trees++;
            }

            BuildBorderTrees(vegetation.transform, rng, pineChance: 0.0, deadBorder: true);
            ScatterSimple(vegetation.transform, rng, "Fern_1", 70, 0.7f, 1.8f);
            ScatterSimple(vegetation.transform, rng, "Mushroom_Common", 35, 0.3f, 1.6f);
            ScatterSimple(vegetation.transform, rng, "Grass_Wispy_Short", 90, 0.4f, 1.5f);
        }

        private static bool IsInPool(Vector2 position, float margin)
        {
            for (int i = 0; i < PoolCenters.Length; i++)
            {
                if (Vector2.Distance(position, PoolCenters[i]) < PoolRadii[i] + margin)
                {
                    return true;
                }
            }

            return false;
        }

        // ==================================================================
        // Gemeinsame Bausteine
        // ==================================================================

        private static void ResetState()
        {
            modelCache.Clear();
            modelHeightCache.Clear();
            placedTreePositions = new List<Vector3>();
            pathSamples = new List<Vector2>();
        }

        // Erneuert nur die generierten Wurzeln; "[Handarbeit]" bleibt
        // unangetastet und ueberlebt jede Regenerierung.
        private static Scene OpenRegionScene(string sceneName)
        {
            string targetPath = $"Assets/_Elyndor/Scenes/{sceneName}.unity";
            Scene scene;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceScenePath, targetPath))
                {
                    throw new System.InvalidOperationException(
                        $"Bootstrap-Szene konnte nicht nach {targetPath} kopiert werden."
                    );
                }

                scene = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);

                foreach (string generatedRootName in new[]
                         { "Environment", $"{sceneName}_Terrain", "MemoryVisionVolume", "PrototypeHUD" })
                {
                    GameObject generatedRoot = GameObject.Find(generatedRootName);

                    if (generatedRoot != null)
                    {
                        Object.DestroyImmediate(generatedRoot);
                    }
                }
            }

            GameObject legacyGround = GameObject.Find("Ground");
            if (legacyGround != null)
            {
                Object.DestroyImmediate(legacyGround);
            }

            if (GameObject.Find("[Handarbeit]") == null)
            {
                new GameObject("[Handarbeit]");
            }

            return scene;
        }

        private static void FinishScene(Scene scene, GameObject player)
        {
            BuildMemoryVisionVolume(scene.name);
            BuildUserInterface(player.GetComponent<InteractionDetector>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddSceneToBuildSettings($"Assets/_Elyndor/Scenes/{scene.name}.unity");
            AssetDatabase.SaveAssets();
        }

        private static void SamplePaths(Vector2[][] paths)
        {
            foreach (Vector2[] path in paths)
            {
                pathSamples.AddRange(SampleSpline(path, 0.8f));
            }
        }

        private static void ConfigureAtmosphere(
            Color sunColor, float sunIntensity, Vector3 sunRotation,
            Color fogColor, float fogDensity,
            Color ambientSky, Color ambientEquator, Color ambientGround,
            string skyboxName, Color skyTint
        )
        {
            GameObject lightObject = GameObject.Find("Directional Light");

            if (lightObject == null)
            {
                lightObject = new GameObject("Directional Light");
                lightObject.AddComponent<Light>().type = LightType.Directional;
            }

            Light sun = lightObject.GetComponent<Light>();
            sun.color = sunColor;
            sun.intensity = sunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            lightObject.transform.rotation = Quaternion.Euler(sunRotation);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;

            Material skybox = LoadOrCreateMaterial(skyboxName, "Skybox/Procedural");
            skybox.SetColor("_SkyTint", skyTint);
            skybox.SetColor("_GroundColor", ambientGround);
            skybox.SetFloat("_Exposure", 1.0f);
            skybox.SetFloat("_AtmosphereThickness", 1.1f);
            EditorUtility.SetDirty(skybox);
            RenderSettings.skybox = skybox;
            RenderSettings.sun = sun;
        }

        private static void ConfigureVolumes(string regionName, float vignette, float saturation)
        {
            GameObject volumeObject = GameObject.Find("Global Volume");

            if (volumeObject == null)
            {
                volumeObject = new GameObject("Global Volume");
                volumeObject.AddComponent<Volume>().isGlobal = true;
            }

            Volume volume = volumeObject.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;

            volume.sharedProfile = CreateProfileAsset($"{regionName}_Base_Profile", profile =>
            {
                Vignette vignetteOverride = AddOverride<Vignette>(profile);
                vignetteOverride.intensity.Override(vignette);
                vignetteOverride.smoothness.Override(0.5f);

                ColorAdjustments colorAdjustments = AddOverride<ColorAdjustments>(profile);
                colorAdjustments.saturation.Override(saturation);
                colorAdjustments.contrast.Override(6f);

                Bloom bloom = AddOverride<Bloom>(profile);
                bloom.intensity.Override(0.35f);
                bloom.threshold.Override(0.95f);
                bloom.scatter.Override(0.6f);
            });
        }

        private static void BuildMemoryVisionVolume(string regionName)
        {
            GameObject volumeObject = new GameObject("MemoryVisionVolume");

            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 0f;

            volume.sharedProfile = CreateProfileAsset($"{regionName}_MemoryVision_Profile", profile =>
            {
                Vignette vignetteOverride = AddOverride<Vignette>(profile);
                vignetteOverride.intensity.Override(0.38f);
                vignetteOverride.smoothness.Override(0.6f);

                ColorAdjustments colorAdjustments = AddOverride<ColorAdjustments>(profile);
                colorAdjustments.saturation.Override(-45f);
                colorAdjustments.postExposure.Override(-0.5f);

                WhiteBalance whiteBalance = AddOverride<WhiteBalance>(profile);
                whiteBalance.temperature.Override(-18f);
            });

            MemoryVisionEffect effect = volumeObject.AddComponent<MemoryVisionEffect>();
            SerializedObject serializedEffect = new SerializedObject(effect);
            serializedEffect.FindProperty("visionVolume").objectReferenceValue = volume;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
        }

        private static VolumeProfile CreateProfileAsset(string name, System.Action<VolumeProfile> configure)
        {
            EnsureFolder("Assets/_Elyndor/Art", "PostProcessing");

            string assetPath = $"{PostFxFolder}/{name}.asset";
            AssetDatabase.DeleteAsset(assetPath);

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, assetPath);
            configure(profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(overrides: false);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        // ------------------------------------------------------------------
        // Terrain
        // ------------------------------------------------------------------

        private static void CreateRegionTerrain(
            string regionName,
            (string name, Color color)[] extraLayers,
            System.Func<Vector2, float[]> extraWeights
        )
        {
            EnsureFolder("Assets/_Elyndor/Art", "Terrain");

            string dataPath = $"{TerrainFolder}/{regionName}_TerrainData.asset";
            AssetDatabase.DeleteAsset(dataPath);

            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = HeightmapResolution;
            terrainData.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
            terrainData.SetHeights(0, 0, GenerateHeights());

            TerrainLayer[] layers = new TerrainLayer[extraLayers.Length + 1];
            layers[0] = CreateTerrainLayer(extraLayers[0].name + "_Basis", extraLayers[0].color);
            for (int i = 0; i < extraLayers.Length; i++)
            {
                layers[i + 1] = CreateTerrainLayer(extraLayers[i].name, extraLayers[i].color * 1.08f);
            }

            terrainData.alphamapResolution = AlphamapResolution;
            terrainData.terrainLayers = layers;
            terrainData.SetAlphamaps(0, 0, GenerateAlphamaps(extraLayers.Length, extraWeights));

            AssetDatabase.CreateAsset(terrainData, dataPath);

            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = $"{regionName}_Terrain";
            terrainObject.transform.position = new Vector3(-TerrainSize / 2f, -BaseLevel, -TerrainSize / 2f);

            regionTerrain = terrainObject.GetComponent<Terrain>();
            regionTerrain.heightmapPixelError = 6f;
            regionTerrain.drawInstanced = true;
            regionTerrain.basemapDistance = 400f;
            MarkStatic(terrainObject);
        }

        private static float[,] GenerateHeights()
        {
            float[,] heights = new float[HeightmapResolution, HeightmapResolution];

            for (int zi = 0; zi < HeightmapResolution; zi++)
            {
                for (int xi = 0; xi < HeightmapResolution; xi++)
                {
                    float wx = -TerrainSize / 2f + xi / (float)(HeightmapResolution - 1) * TerrainSize;
                    float wz = -TerrainSize / 2f + zi / (float)(HeightmapResolution - 1) * TerrainSize;

                    float height = rawHeightFunction(wx, wz);
                    height = ApplyPathSmoothing(height, new Vector2(wx, wz));
                    heights[zi, xi] = Mathf.Clamp01((BaseLevel + height) / TerrainHeight);
                }
            }

            return heights;
        }

        private static float ApplyPathSmoothing(float height, Vector2 position)
        {
            float bestDistance = float.MaxValue;
            Vector2 bestSample = position;

            foreach (Vector2 sample in pathSamples)
            {
                float distance = Vector2.Distance(position, sample);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestSample = sample;
                }
            }

            if (bestDistance >= 2.6f)
            {
                return height;
            }

            float target = rawHeightFunction(bestSample.x, bestSample.y);
            return Mathf.Lerp(height, target, (1f - bestDistance / 2.6f) * 0.75f);
        }

        private static float[,,] GenerateAlphamaps(int extraCount, System.Func<Vector2, float[]> extraWeights)
        {
            // Layer 0 = Basis; danach [Pfad, Extra1, ...] gemaess extraWeights,
            // wobei extraWeights [Pfad, Zusatz...] in Layer-Reihenfolge liefert.
            float[,,] alphamaps = new float[AlphamapResolution, AlphamapResolution, extraCount + 1];

            for (int zi = 0; zi < AlphamapResolution; zi++)
            {
                for (int xi = 0; xi < AlphamapResolution; xi++)
                {
                    float wx = -TerrainSize / 2f + xi / (float)(AlphamapResolution - 1) * TerrainSize;
                    float wz = -TerrainSize / 2f + zi / (float)(AlphamapResolution - 1) * TerrainSize;

                    float[] weights = extraWeights(new Vector2(wx, wz));
                    float total = 0f;

                    for (int w = 0; w < weights.Length && w < extraCount; w++)
                    {
                        total += weights[w];
                    }

                    if (total > 1f)
                    {
                        for (int w = 0; w < weights.Length; w++)
                        {
                            weights[w] /= total;
                        }

                        total = 1f;
                    }

                    alphamaps[zi, xi, 0] = 1f - total;

                    // Pfad liegt auf dem letzten Layer, Zusatzflaechen dazwischen
                    // (Layer 1 ist die Basis-Doppelung und bleibt ungenutzt).
                    for (int w = 0; w < weights.Length && w < extraCount; w++)
                    {
                        int layerIndex = w == 0 ? extraCount : w + 1;
                        alphamaps[zi, xi, layerIndex] = weights[w];
                    }
                }
            }

            return alphamaps;
        }

        private static TerrainLayer CreateTerrainLayer(string name, Color color)
        {
            Texture2D texture = CreateNoiseTexture(name, color);

            string layerPath = $"{TerrainFolder}/{name}.terrainlayer";
            AssetDatabase.DeleteAsset(layerPath);

            TerrainLayer layer = new TerrainLayer
            {
                diffuseTexture = texture,
                tileSize = new Vector2(4f, 4f)
            };

            AssetDatabase.CreateAsset(layer, layerPath);
            return layer;
        }

        private static Texture2D CreateNoiseTexture(string name, Color baseColor)
        {
            string texturePath = $"{TerrainFolder}/{name}.png";

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.35f, y * 0.35f) * 0.08f - 0.04f;
                    texture.SetPixel(x, y, new Color(
                        Mathf.Clamp01(baseColor.r + noise),
                        Mathf.Clamp01(baseColor.g + noise),
                        Mathf.Clamp01(baseColor.b + noise)
                    ));
                }
            }

            texture.Apply();
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(texturePath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        // ------------------------------------------------------------------
        // Spieler, Portale, Interaktion
        // ------------------------------------------------------------------

        private static GameObject PreparePlayer(Vector2 startPoint)
        {
            PlayerMovement playerMovement = Object.FindAnyObjectByType<PlayerMovement>();

            if (playerMovement == null)
            {
                throw new System.InvalidOperationException("Kein PlayerMovement in der Szene gefunden.");
            }

            GameObject player = playerMovement.gameObject;
            player.transform.localScale = Vector3.one * PlayerScale;

            if (player.GetComponent<InteractionDetector>() == null)
            {
                player.AddComponent<InteractionDetector>();
            }

            player.transform.position = new Vector3(startPoint.x, GroundY(startPoint) + 1.2f, startPoint.y);

            WhisperingForestSceneBuilder.AttachPlayerSilhouette(player);
            WhisperingForestSceneBuilder.ConfigureMainCamera();

            if (player.GetComponent<Elyndor.Combat.PlayerCombat>() == null)
            {
                player.AddComponent<Elyndor.Combat.PlayerCombat>();
            }

            return player;
        }

        private static void BuildPortal(
            Transform parent,
            string name,
            Vector2 position,
            string targetScene,
            string targetSpawnId
        )
        {
            GameObject portal = new GameObject(name);
            portal.transform.SetParent(parent);
            portal.transform.position = new Vector3(position.x, GroundY(position), position.y);

            BoxCollider trigger = portal.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(8f, 4f, 3f);
            trigger.center = new Vector3(0f, 2f, 0f);

            RegionPortal regionPortal = portal.AddComponent<RegionPortal>();
            SerializedObject serializedPortal = new SerializedObject(regionPortal);
            serializedPortal.FindProperty("targetSceneName").stringValue = targetScene;
            serializedPortal.FindProperty("targetSpawnId").stringValue = targetSpawnId;
            serializedPortal.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildSpawn(Transform parent, string spawnId, Vector2 position)
        {
            GameObject spawn = new GameObject($"Spawn_{spawnId}");
            spawn.transform.SetParent(parent);
            spawn.transform.position = new Vector3(position.x, GroundY(position) + 1.1f, position.y);

            RegionSpawnPoint spawnPoint = spawn.AddComponent<RegionSpawnPoint>();
            SerializedObject serializedSpawn = new SerializedObject(spawnPoint);
            serializedSpawn.FindProperty("spawnId").stringValue = spawnId;
            serializedSpawn.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildMemorySiteObject(
            Transform parent,
            Vector2 position,
            MemoryEcho echo,
            string siteId,
            string memoryText
        )
        {
            GameObject site = new GameObject($"MemorySite_{siteId}");
            site.transform.SetParent(parent);
            site.transform.position = new Vector3(position.x, GroundY(position), position.y);

            System.Random rng = new System.Random(siteId.GetHashCode());

            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f;
                Vector2 stonePosition = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.5f;
                PlaceModel($"Pebble_Round_{1 + i % 5}", site.transform, stonePosition,
                    (float)rng.NextDouble() * 360f, TargetScale($"Pebble_Round_{1 + i % 5}", 0.4f));
            }

            SphereCollider trigger = site.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 3f;
            trigger.center = new Vector3(0f, 1f, 0f);

            MemorySite memorySite = site.AddComponent<MemorySite>();
            SerializedObject serializedSite = new SerializedObject(memorySite);
            serializedSite.FindProperty("interactionPrompt").stringValue = "Memory Watch verwenden";
            serializedSite.FindProperty("siteId").stringValue = siteId;
            serializedSite.FindProperty("activationDuration").floatValue = 2.5f;
            serializedSite.FindProperty("memoryText").stringValue = memoryText;
            serializedSite.FindProperty("memoryTextDuration").floatValue = 8f;

            SerializedProperty echoesProperty = serializedSite.FindProperty("echoes");
            echoesProperty.arraySize = 1;
            echoesProperty.GetArrayElementAtIndex(0).objectReferenceValue = echo;
            serializedSite.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddExaminableSphere(
            Transform parent,
            Vector2 position,
            float radius,
            string prompt,
            string text
        )
        {
            GameObject examinableObject = new GameObject($"Examinable_{prompt}");
            examinableObject.transform.SetParent(parent);
            examinableObject.transform.position = new Vector3(position.x, GroundY(position), position.y);

            SphereCollider trigger = examinableObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
            trigger.center = new Vector3(0f, 1f, 0f);

            ExaminableObject examinable = examinableObject.AddComponent<ExaminableObject>();
            SerializedObject serialized = new SerializedObject(examinable);
            serialized.FindProperty("interactionPrompt").stringValue = prompt;
            serialized.FindProperty("examineText").stringValue = text;
            serialized.FindProperty("textDuration").floatValue = 7f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildSign(Transform parent, Vector2 position, string prompt, string text)
        {
            GameObject sign = new GameObject("Wegschild");
            sign.transform.SetParent(parent);
            sign.transform.position = new Vector3(position.x, GroundY(position), position.y);

            Material wood = LoadMaterial("Proto_Wood");

            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Pfosten";
            post.transform.SetParent(sign.transform);
            post.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            post.transform.localScale = new Vector3(0.12f, 0.6f, 0.12f);
            ApplyMaterial(post, wood);
            MarkStatic(post);

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Tafel";
            board.transform.SetParent(sign.transform);
            board.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            board.transform.localScale = new Vector3(1.1f, 0.5f, 0.08f);
            board.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            ApplyMaterial(board, wood);
            MarkStatic(board);

            SphereCollider trigger = sign.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.2f;
            trigger.center = new Vector3(0f, 1f, 0f);

            ExaminableObject examinable = sign.AddComponent<ExaminableObject>();
            SerializedObject serialized = new SerializedObject(examinable);
            serialized.FindProperty("interactionPrompt").stringValue = prompt;
            serialized.FindProperty("examineText").stringValue = text;
            serialized.FindProperty("textDuration").floatValue = 6f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildWorldBounds(Transform parent)
        {
            GameObject boundsRoot = new GameObject("Weltgrenzen");
            boundsRoot.transform.SetParent(parent);

            float edge = PlayableRadius + 4f;
            CreateBoundsWall(boundsRoot.transform, new Vector3(-edge, 2f, 0f), new Vector3(1f, 8f, TerrainSize));
            CreateBoundsWall(boundsRoot.transform, new Vector3(edge, 2f, 0f), new Vector3(1f, 8f, TerrainSize));
            CreateBoundsWall(boundsRoot.transform, new Vector3(0f, 2f, -edge), new Vector3(TerrainSize, 8f, 1f));
            CreateBoundsWall(boundsRoot.transform, new Vector3(0f, 2f, edge), new Vector3(TerrainSize, 8f, 1f));
        }

        private static void CreateBoundsWall(Transform parent, Vector3 position, Vector3 size)
        {
            GameObject wall = new GameObject("Grenze (unsichtbar)");
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.AddComponent<BoxCollider>().size = size;
        }

        // ------------------------------------------------------------------
        // Modelle und Vegetation
        // ------------------------------------------------------------------

        private static GameObject LoadModel(string modelName)
        {
            if (modelCache.TryGetValue(modelName, out GameObject cached))
            {
                return cached;
            }

            string modelPath = $"{PackModelFolder}/{modelName}.fbx";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

            if (model == null)
            {
                throw new System.InvalidOperationException($"Modell nicht gefunden: {modelPath}");
            }

            modelCache[modelName] = model;
            return model;
        }

        private static float TargetScale(string modelName, float targetHeight)
        {
            if (!modelHeightCache.TryGetValue(modelName, out float modelHeight))
            {
                GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel(modelName));
                modelHeight = CalculateBounds(temp).size.y;
                Object.DestroyImmediate(temp);
                modelHeightCache[modelName] = modelHeight;
            }

            if (modelHeight < 0.01f)
            {
                return 1f;
            }

            return Mathf.Clamp(targetHeight / modelHeight, 0.05f, 50f);
        }

        private static GameObject PlaceModel(
            string modelName,
            Transform parent,
            Vector2 position,
            float yRotation,
            float scale
        )
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel(modelName), parent);
            instance.transform.position = new Vector3(position.x, 0f, position.y);
            instance.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            instance.transform.localScale = Vector3.one * scale;

            Bounds bounds = CalculateBounds(instance);
            instance.transform.position += Vector3.up * (GroundY(position) - bounds.min.y);

            RemapMaterials(instance);
            MarkStaticRecursively(instance);
            return instance;
        }

        private static void PlaceTree(
            string modelName,
            Transform parent,
            Vector2 position,
            float targetHeight,
            System.Random rng
        )
        {
            float scale = TargetScale(modelName, targetHeight) *
                          Mathf.Lerp(0.85f, 1.25f, (float)rng.NextDouble());

            GameObject tree = PlaceModel(modelName, parent, position, (float)rng.NextDouble() * 360f, scale);

            Bounds bounds = CalculateBounds(tree);
            CapsuleCollider trunk = tree.AddComponent<CapsuleCollider>();
            trunk.radius = 0.35f / Mathf.Max(scale, 0.01f);
            trunk.height = bounds.size.y / Mathf.Max(scale, 0.01f);
            trunk.center = new Vector3(0f, trunk.height * 0.5f, 0f);

            placedTreePositions.Add(tree.transform.position);
        }

        private static void RemapMaterials(GameObject instance)
        {
            foreach (Renderer childRenderer in instance.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = childRenderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        continue;
                    }

                    string materialName = materials[i].name.Replace(" (Instance)", "");
                    Material replacement = AssetDatabase.LoadAssetAtPath<Material>(
                        $"{MaterialFolder}/Pack_{materialName}.mat"
                    );

                    if (replacement != null)
                    {
                        materials[i] = replacement;
                        changed = true;
                    }
                }

                if (changed)
                {
                    childRenderer.sharedMaterials = materials;
                }
            }
        }

        private static void BuildBorderTrees(
            Transform parent,
            System.Random rng,
            double pineChance,
            bool deadBorder = false
        )
        {
            GameObject border = new GameObject("Waldrand");
            border.transform.SetParent(parent);

            const int count = 90;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count * Mathf.PI * 2f;
                float ring = Mathf.Lerp(PlayableRadius + 1f, PlayableRadius + 6f, (float)rng.NextDouble());

                Vector2 direction = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
                float scaleToEdge = ring / Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
                Vector2 position = direction * scaleToEdge + new Vector2(
                    (float)(rng.NextDouble() * 2.0 - 1.0),
                    (float)(rng.NextDouble() * 2.0 - 1.0)
                );

                int variant = 1 + rng.Next(5);
                string model;

                if (deadBorder)
                {
                    model = rng.NextDouble() < 0.6 ? $"DeadTree_{variant}" : $"TwistedTree_{variant}";
                }
                else
                {
                    model = rng.NextDouble() < pineChance ? $"Pine_{variant}" : $"CommonTree_{variant}";
                }

                PlaceTree(model, border.transform, position,
                    Mathf.Lerp(8f, 11f, (float)rng.NextDouble()), rng);
            }
        }

        private static void ScatterSimple(
            Transform parent,
            System.Random rng,
            string modelName,
            int count,
            float targetHeight,
            float minPathDistance
        )
        {
            GameObject group = new GameObject(modelName);
            group.transform.SetParent(parent);

            int placed = 0;
            int attempts = 0;

            while (placed < count && attempts < count * 12)
            {
                attempts++;
                Vector2 position = RandomPosition(rng);

                if (PathDistance(position) < minPathDistance) continue;
                if (IsInPool(position, 0.5f)) continue;

                float scale = TargetScale(modelName, targetHeight) *
                              Mathf.Lerp(0.75f, 1.35f, (float)rng.NextDouble());
                PlaceModel(modelName, group.transform, position, (float)rng.NextDouble() * 360f, scale);
                placed++;
            }
        }

        private static void BuildBlock(
            Transform parent,
            Vector2 position,
            Vector3 scale,
            float yRotation,
            Material material,
            string name
        )
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = new Vector3(position.x, GroundY(position) + scale.y * 0.4f, position.y);
            block.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            block.transform.localScale = scale;
            ApplyMaterial(block, material);
            MarkStatic(block);
        }

        private static void BuildGhostPart(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 scale,
            Material material,
            bool cylinder
        )
        {
            GameObject part = GameObject.CreatePrimitive(
                cylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube
            );
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPosition;
            part.transform.localScale = cylinder
                ? new Vector3(scale.x, scale.y * 0.5f, scale.z)
                : scale;

            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            ApplyMaterial(part, material);
        }

        // ------------------------------------------------------------------
        // Geometrie- und Asset-Helfer
        // ------------------------------------------------------------------

        private static float GroundY(Vector2 position)
        {
            return regionTerrain.SampleHeight(new Vector3(position.x, 0f, position.y)) +
                   regionTerrain.transform.position.y;
        }

        private static Vector2 RandomPosition(System.Random rng)
        {
            return new Vector2(
                Mathf.Lerp(-PlayableRadius, PlayableRadius, (float)rng.NextDouble()),
                Mathf.Lerp(-PlayableRadius, PlayableRadius, (float)rng.NextDouble())
            );
        }

        private static bool IsClearOfTrees(Vector2 position, float minDistance)
        {
            foreach (Vector3 treePosition in placedTreePositions)
            {
                if (Vector2.Distance(position, new Vector2(treePosition.x, treePosition.z)) < minDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static float PathDistance(Vector2 position)
        {
            float best = float.MaxValue;

            foreach (Vector2 sample in pathSamples)
            {
                float distance = Vector2.Distance(position, sample);
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private static float PathWeight(Vector2 position)
        {
            float distance = PathDistance(position);
            return distance < 1.2f ? 1f : Mathf.Clamp01((2.4f - distance) / 1.2f);
        }

        private static float RadialWeight(Vector2 position, Vector2 center, float radius)
        {
            float distance = Vector2.Distance(position, center);
            return distance >= radius ? 0f : 0.5f + 0.5f * Mathf.Cos(Mathf.PI * distance / radius);
        }

        private static float RectWeight(Vector2 position, Vector2 center, Vector2 halfSize)
        {
            Vector2 delta = position - center;
            float dx = Mathf.Abs(delta.x) - halfSize.x;
            float dz = Mathf.Abs(delta.y) - halfSize.y;
            float outside = Mathf.Max(dx, dz);

            if (outside <= 0f) return 1f;
            if (outside >= 2.5f) return 0f;
            return 1f - outside / 2.5f;
        }

        private static float Hill(float wx, float wz, Vector2 center, float radius, float height)
        {
            float distance = Vector2.Distance(new Vector2(wx, wz), center);
            return distance >= radius
                ? 0f
                : height * (0.5f + 0.5f * Mathf.Cos(Mathf.PI * distance / radius));
        }

        private static float Flatten(
            float height, float wx, float wz, Vector2 center, float radius,
            float target, float strength
        )
        {
            float distance = Vector2.Distance(new Vector2(wx, wz), center);

            if (distance >= radius)
            {
                return height;
            }

            float weight = (0.5f + 0.5f * Mathf.Cos(Mathf.PI * distance / radius)) * strength;
            return Mathf.Lerp(height, target, weight);
        }

        private static List<Vector2> SampleSpline(Vector2[] points, float step)
        {
            List<Vector2> result = new List<Vector2>();

            if (points.Length < 2)
            {
                return result;
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 p0 = i == 0 ? points[0] : points[i - 1];
                Vector2 p1 = points[i];
                Vector2 p2 = points[i + 1];
                Vector2 p3 = i + 2 < points.Length ? points[i + 2] : points[points.Length - 1];

                float segmentLength = Vector2.Distance(p1, p2);
                int steps = Mathf.Max(2, Mathf.CeilToInt(segmentLength / step));

                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)steps;
                    float t2 = t * t;
                    float t3 = t2 * t;

                    result.Add(0.5f * (
                        2f * p1 +
                        (p2 - p0) * t +
                        (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                        (3f * p1 - p0 - 3f * p2 + p3) * t3
                    ));
                }
            }

            result.Add(points[points.Length - 1]);
            return result;
        }

        private static Bounds CalculateBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                return new Bounds(target.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;

            foreach (Renderer childRenderer in renderers)
            {
                bounds.Encapsulate(childRenderer.bounds);
            }

            return bounds;
        }

        private static Material LoadMaterial(string name)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");

            if (material == null)
            {
                throw new System.InvalidOperationException(
                    $"Material {name} nicht gefunden — zuerst den Finsterwald-Builder ausführen."
                );
            }

            return material;
        }

        private static Material LoadOrCreateMaterial(string name, string shaderName)
        {
            EnsureFolder("Assets/_Elyndor/Art", "Materials");
            EnsureFolder("Assets/_Elyndor/Art/Materials", "Prototype");

            string assetPath = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material == null)
            {
                material = new Material(Shader.Find(shaderName));
                AssetDatabase.CreateAsset(material, assetPath);
            }

            return material;
        }

        private static void EnsureFolder(string parentFolder, string folderName)
        {
            if (!AssetDatabase.IsValidFolder($"{parentFolder}/{folderName}"))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            target.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void MarkStatic(GameObject target)
        {
            GameObjectUtility.SetStaticEditorFlags(target, StaticEditorFlags.BatchingStatic);
        }

        private static void MarkStaticRecursively(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                MarkStatic(child.gameObject);
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            foreach (EditorBuildSettingsScene existingScene in EditorBuildSettings.scenes)
            {
                if (existingScene.path == scenePath)
                {
                    return;
                }
            }

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            System.Array.Resize(ref scenes, scenes.Length + 1);
            scenes[scenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = scenes;
        }

        // ------------------------------------------------------------------
        // UI (bewusste Kopie des HUD-Aufbaus — gemeinsame Basis ist Schuld)
        // ------------------------------------------------------------------

        private static void BuildUserInterface(InteractionDetector detector)
        {
            GameObject canvasObject = new GameObject("PrototypeHUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject promptRoot = BuildTextPanel(
                canvasObject.transform, "InteractionPrompt",
                new Vector2(0.5f, 0.12f), new Vector2(560f, 56f), font, 26, out Text promptText);

            GameObject narrationRoot = BuildTextPanel(
                canvasObject.transform, "NarrationPanel",
                new Vector2(0.5f, 0.85f), new Vector2(900f, 140f), font, 24, out Text narrationText);

            GameObject overlayRoot = BuildActivationOverlay(canvasObject.transform, font);
            CanvasGroup overlayGroup = overlayRoot.AddComponent<CanvasGroup>();
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;

            promptRoot.SetActive(false);
            narrationRoot.SetActive(false);
            overlayRoot.SetActive(false);

            InteractionPromptUI promptUI = canvasObject.AddComponent<InteractionPromptUI>();
            SerializedObject serializedPromptUI = new SerializedObject(promptUI);
            serializedPromptUI.FindProperty("detector").objectReferenceValue = detector;
            serializedPromptUI.FindProperty("promptRoot").objectReferenceValue = promptRoot;
            serializedPromptUI.FindProperty("promptText").objectReferenceValue = promptText;
            serializedPromptUI.ApplyModifiedPropertiesWithoutUndo();

            NarrationUI narrationUI = canvasObject.AddComponent<NarrationUI>();
            SerializedObject serializedNarrationUI = new SerializedObject(narrationUI);
            serializedNarrationUI.FindProperty("narrationRoot").objectReferenceValue = narrationRoot;
            serializedNarrationUI.FindProperty("narrationText").objectReferenceValue = narrationText;
            serializedNarrationUI.ApplyModifiedPropertiesWithoutUndo();

            MemoryWatchActivationUI activationUI = canvasObject.AddComponent<MemoryWatchActivationUI>();
            SerializedObject serializedActivationUI = new SerializedObject(activationUI);
            serializedActivationUI.FindProperty("overlayGroup").objectReferenceValue = overlayGroup;
            serializedActivationUI.ApplyModifiedPropertiesWithoutUndo();

            WhisperingForestSceneBuilder.BuildCompassUI(canvasObject, font);
            WhisperingForestSceneBuilder.BuildInventoryUI(canvasObject, font);
            WhisperingForestSceneBuilder.WireSfxLibrary(canvasObject, detector.transform);
        }

        private static GameObject BuildTextPanel(
            Transform parent, string name, Vector2 anchor, Vector2 size,
            Font font, int fontSize, out Text text
        )
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = anchor;
            panelRect.anchorMax = anchor;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = size;

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(panel.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);

            text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return panel;
        }

        private static GameObject BuildActivationOverlay(Transform parent, Font font)
        {
            GameObject overlay = new GameObject("MemoryWatchOverlay");
            overlay.transform.SetParent(parent, false);

            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image tint = overlay.AddComponent<Image>();
            tint.color = new Color(0.03f, 0.05f, 0.08f, 0.4f);
            tint.raycastTarget = false;

            GameObject label = new GameObject("Label");
            label.transform.SetParent(overlay.transform, false);

            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(700f, 60f);

            Text labelText = label.AddComponent<Text>();
            labelText.font = font;
            labelText.fontSize = 26;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.8f, 0.88f, 0.95f, 0.85f);
            labelText.text = "Die Memory Watch lauscht den Erinnerungen ...";

            return overlay;
        }
    }
}



