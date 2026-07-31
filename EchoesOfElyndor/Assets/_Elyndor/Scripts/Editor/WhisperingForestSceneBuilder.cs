using System.Collections.Generic;
using System.IO;
using Elyndor.Interaction;
using Elyndor.Memory;
using Elyndor.Player;
using Elyndor.UI;
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
    /// Baut die große Flüsterwald-Szene deterministisch als Kopie der
    /// Bootstrap-Szene auf: Terrain mit Höhenprofil, acht fließend
    /// verbundene Bereiche, kurvige Pfade, Bach mit zwei entdeckbaren
    /// Querungen und Vegetation aus dem Natur Pack.
    /// Kann beliebig oft ausgeführt werden — die generierte Szene wird
    /// vollständig neu erzeugt, Bootstrap selbst bleibt unverändert.
    /// </summary>
    public static class WhisperingForestSceneBuilder
    {
        private const string SourceScenePath = "Assets/_Elyndor/Scenes/Bootstrap.unity";
        private const string TargetScenePath = "Assets/_Elyndor/Scenes/Finsterwald.unity";
        private const string MaterialFolder = "Assets/_Elyndor/Art/Materials/Prototype";
        private const string PostFxFolder = "Assets/_Elyndor/Art/PostProcessing";
        private const string TerrainFolder = "Assets/_Elyndor/Art/Terrain";
        private const string PackModelFolder = "Assets/ThirdParty/Natur Pack/FBX (Unity)";
        private const string PackTextureFolder = "Assets/ThirdParty/Natur Pack/Textures";

        // Zusaetzliche Packs (Quaternius, stylized low-poly wie das Natur Pack).
        // Modell-Keys tragen ein Praefix ("USN:", "TST:"), damit gleichnamige
        // Modelle und Materialien der Packs nicht kollidieren.
        private const string StylizedNatureModelFolder =
            "Assets/ThirdParty/Ultimate Stylized Nature - May 2022/FBX";
        private const string StylizedNatureTextureFolder =
            "Assets/ThirdParty/Ultimate Stylized Nature - May 2022/Textures";
        private const string TexturedTreesModelFolder =
            "Assets/ThirdParty/Textured Stylized Trees - May 2020/FBX";
        private const string TexturedTreesTextureFolder =
            "Assets/ThirdParty/Textured Stylized Trees - May 2020/Textures";

        // Modular Temple (OBJ): Ruinenteile werden einheitlich mit den
        // Proto-Steinmaterialien eingefaerbt statt mit den mtl-Farben.
        private const string TempleModelFolder = "Assets/ThirdParty/Modular Temple";

        // Der Spieler-Rig ist im Bootstrap zu gross angelegt — Vorgabe von Lars:
        // einheitlich 0.75 auf allen Achsen.
        private const float PlayerScale = 0.75f;

        // Storytelling-Ankerpunkte (M2.9).
        private static readonly Vector2 AncientTreePoint = new Vector2(-14f, 16f);
        // Rastplatz sichtbar vom Hauptweg, Schrein knapp neben dem versteckten Pfad.
        private static readonly Vector2 RestSitePoint = new Vector2(5f, -25f);
        private static readonly Vector2 ShrinePoint = new Vector2(-34.3f, 16.5f);
        private static readonly Vector2 OvergrownWallPoint = new Vector2(17f, -11.5f);
        private static readonly Vector2 RockFormationPoint = new Vector2(50f, -24f);
        private static readonly Vector2 JungleRegionPoint = new Vector2(-48f, -40f);

        private static readonly Vector2[] RuinFragmentPoints =
        {
            new Vector2(-44f, -20f), new Vector2(22f, -32f), new Vector2(18f, 30f)
        };

        private static readonly Vector2[] FallenGiantPoints =
        {
            new Vector2(-24f, -38f), new Vector2(26f, 22f)
        };

        // Terrain: 140 x 140 m, Welthöhe 0 liegt bei BaseLevel.
        private const float TerrainSize = 200f;
        private const float TerrainHeight = 24f;
        private const float BaseLevel = 8f;
        private const int HeightmapResolution = 257;
        private const int AlphamapResolution = 256;
        private const float PlayableRadius = 90f;

        // Zonen-Ankerpunkte (Weltkoordinaten X/Z).
        private static readonly Vector2 StartPoint = new Vector2(0f, -54f);
        private static readonly Vector2 ClearingPoint = new Vector2(-26f, -16f);
        private static readonly Vector2 RuinPoint = new Vector2(-40f, 28f);
        private static readonly Vector2 StoneCirclePoint = new Vector2(36f, 18f);
        private static readonly Vector2 ViewpointPoint = new Vector2(48f, 44f);
        private static readonly Vector2 BridgePoint = new Vector2(2f, -1.2f);
        private static readonly Vector2 MemorySitePoint = new Vector2(-3f, -7f);
        private static readonly Vector2 LogCrossingPoint = new Vector2(-31f, 4.9f);
        private static readonly Vector2 FordPoint = new Vector2(34f, 0f);
        private static readonly Vector2 ForkPoint = new Vector2(0f, -21f);
        private static readonly Vector2 ForkRockPoint = new Vector2(-5f, -23f);

        // Pfade als Stützpunkte, werden per Catmull-Rom geglättet.
        private static readonly Vector2[][] VisiblePaths =
        {
            // Hauptweg: Start -> Gabelung -> zerstoerte Bruecke
            new[]
            {
                new Vector2(0f, -58f), new Vector2(3f, -48f), new Vector2(-3f, -40f),
                new Vector2(-8f, -32f), new Vector2(-4f, -26f), new Vector2(0f, -21f),
                new Vector2(1f, -14f), new Vector2(-3f, -9f), new Vector2(2f, -6f),
                new Vector2(2f, -3.5f)
            },
            // Abzweig zur Lichtung
            new[]
            {
                new Vector2(0f, -21f), new Vector2(-8f, -19f), new Vector2(-16f, -17f),
                new Vector2(-24f, -16f), new Vector2(-27f, -15f)
            },
            // Uferweg: Bruecke -> Furt
            new[]
            {
                new Vector2(2f, -6f), new Vector2(10f, -8f), new Vector2(18f, -8f),
                new Vector2(26f, -6f), new Vector2(32f, -3f), new Vector2(34f, -1.5f)
            },
            // Nordost: Furt -> Steinkreis -> Aussichtspunkt
            new[]
            {
                new Vector2(34.5f, 2f), new Vector2(36f, 6f), new Vector2(36f, 12f),
                new Vector2(38f, 22f), new Vector2(42f, 30f), new Vector2(45f, 36f),
                new Vector2(48f, 42f)
            },
            // Nordufer: Rueckweg zur Brueckennordseite und weiter nach Westen
            new[]
            {
                new Vector2(36f, 10f), new Vector2(26f, 11f), new Vector2(14f, 9f),
                new Vector2(4f, 5f), new Vector2(2f, 3f)
            },
            new[]
            {
                new Vector2(2f, 3f), new Vector2(-8f, 7f), new Vector2(-18f, 9f),
                new Vector2(-26f, 8f), new Vector2(-30f, 7.5f)
            }
        };

        // Versteckter Pfad (schmaler, schwaecher gezeichnet), am Bach unterbrochen.
        private static readonly Vector2[][] HiddenPaths =
        {
            new[]
            {
                new Vector2(-27f, -12f), new Vector2(-30f, -6f), new Vector2(-31f, 1.5f)
            },
            new[]
            {
                new Vector2(-31f, 8.3f), new Vector2(-35f, 14f), new Vector2(-38f, 20f),
                new Vector2(-40f, 26f)
            }
        };

        // Der alte Bach quert die Karte von West nach Ost.
        private static readonly Vector2[] BrookPoints =
        {
            new Vector2(-96f, 12f), new Vector2(-80f, 9f), new Vector2(-66f, 8f), new Vector2(-48f, 4f), new Vector2(-30f, 5f),
            new Vector2(-12f, 4f), new Vector2(0f, -1f), new Vector2(18f, -3f),
            new Vector2(34f, 0f), new Vector2(48f, 6f), new Vector2(66f, 12f), new Vector2(82f, 14f), new Vector2(96f, 18f)
        };

        private const float BrookHalfWidth = 4.5f;
        private const float BrookDepth = 1.7f;

        // Farbpalette (ruhig, leicht entsättigt).
        private static readonly Color WaterColor = new Color(0.25f, 0.34f, 0.40f, 0.82f);
        private static readonly Color StoneColor = new Color(0.42f, 0.43f, 0.44f);
        private static readonly Color WoodColor = new Color(0.34f, 0.26f, 0.17f);
        private static readonly Color FadedMarkColor = new Color(0.45f, 0.55f, 0.65f);
        private static readonly Color GhostColor = new Color(0.55f, 0.85f, 1f, 0.45f);
        private static readonly Color FogColor = new Color(0.56f, 0.62f, 0.58f);
        private static readonly Color SunColor = new Color(1f, 0.95f, 0.85f);

        private struct PathSample
        {
            public Vector2 Position;
            public float TargetHeight;
            public bool Hidden;
        }

        private static Terrain forestTerrain;
        private static List<PathSample> pathSamples;
        private static List<Vector2> brookSamples;
        private static readonly Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, float> modelHeightCache = new Dictionary<string, float>();
        private static readonly HashSet<string> unmappedMaterialNames = new HashSet<string>();
        private static Dictionary<string, Material> packMaterials;
        private static List<Vector3> placedTreePositions;
        private static List<Vector3> accentTreePositions;

        [MenuItem("Elyndor/Setup/Finsterwald erstellen")]
        public static void BuildScene()
        {
            modelCache.Clear();
            modelHeightCache.Clear();
            unmappedMaterialNames.Clear();
            placedTreePositions = new List<Vector3>();
            accentTreePositions = new List<Vector3>();

            ConfigureRenderPipeline();
            SamplePathsAndBrook();
            PreparePackMaterials();

            Scene scene = PrepareTargetScene();

            ConfigureLightingAndAtmosphere();
            ConfigureBaseVolume();

            RemoveLegacyGround();
            CreateForestTerrain();

            GameObject player = PreparePlayer();

            GameObject environment = new GameObject("Environment");
            Transform env = environment.transform;

            BuildBrook(env);
            BuildWorldBounds(env);

            MemoryEcho ghostBridgeEcho = BuildBridgeArea(env);
            BuildStartArea(env);
            BuildClearing(env);
            BuildLogCrossing(env);
            BuildFord(env);
            BuildStoneCircle(env);
            BuildRuin(env);
            BuildViewpoint(env);
            BuildWaymarkers(env);

            // Environmental Storytelling (M2.9) — vor der Vegetation, damit
            // die Baumstreuung die neuen Orte respektiert.
            BuildAncientTree(env);
            BuildRestSite(env);
            BuildShrine(env);
            BuildOvergrownWall(env);
            BuildRockFormation(env);
            BuildScatteredRuins(env);
            // BuildFallenGiants entfernt (25.07.): Die liegenden 13-m-Riesen
            // lasen sich als Platzierungsfehler statt als Storytelling.
            BuildPathNarrows(env);

            BuildOuterRingZones(env);
            BuildReturnGates(env);
            BuildSideQuests(env);
            BuildBackpack(env);
            BuildTrainingDummy(env);
            BuildAtmosphereParticles(env);
            BuildRegionConnections(env);

            System.Random rng = new System.Random(7331);
            ScatterVegetation(env, rng);

            BuildMemoryVisionVolume();
            BuildUserInterface(player.GetComponent<InteractionDetector>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();

            Debug.Log($"Finsterwald-Szene erstellt: {TargetScenePath}");
        }

        // Rueckkehr-Tore der Lore-Bibel V1: in Kapitel 1 sichtbar, aber erst
        // mit spaeteren Watch-Faehigkeiten nutzbar. Vorerst Examinables,
        // die neugierig machen — die Mechaniken folgen mit den Kapiteln.
        private static readonly Vector2 BlackLakePoint = new Vector2(62f, -60f);
        private static readonly Vector2 ForestHutPoint = new Vector2(-66f, 44f);
        private static readonly Vector2 ShadowlessTreePoint = new Vector2(20f, 66f);

        private static void BuildReturnGates(Transform parent)
        {
            GameObject gates = new GameObject("Rueckkehr-Tore");
            gates.transform.SetParent(parent);

            Material wood = GetOrCreateMaterial("Proto_Wood", WoodColor);

            // Schwarzer See: unter der Oberflaeche sind Treppen zu erahnen.
            GameObject lake = new GameObject("Schwarzer See");
            lake.transform.SetParent(gates.transform);
            float lakeBedY = GroundY(BlackLakePoint.x, BlackLakePoint.y);

            GameObject lakeWater = GameObject.CreatePrimitive(PrimitiveType.Plane);
            lakeWater.name = "Seewasser";
            lakeWater.transform.SetParent(lake.transform);
            lakeWater.transform.position = new Vector3(BlackLakePoint.x, lakeBedY + 0.5f, BlackLakePoint.y);
            lakeWater.transform.localScale = new Vector3(1.6f, 1f, 1.4f);
            Object.DestroyImmediate(lakeWater.GetComponent<Collider>());
            lakeWater.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            ApplyMaterial(lakeWater, GetOrCreateWaterMaterial());
            MarkStatic(lakeWater);

            // Versunkene Stufen, knapp unter der Wasserlinie sichtbar.
            Material stone = GetOrCreateMaterial("Proto_Stone", StoneColor);
            for (int i = 0; i < 4; i++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = "Versunkene Stufe";
                step.transform.SetParent(lake.transform);
                step.transform.position = new Vector3(
                    BlackLakePoint.x - 3f + i * 1.1f, lakeBedY + 0.32f - i * 0.12f, BlackLakePoint.y + i * 0.6f
                );
                step.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
                step.transform.localScale = new Vector3(1.4f, 0.25f, 0.9f);
                Object.DestroyImmediate(step.GetComponent<Collider>());
                ApplyMaterial(step, stone);
                MarkStatic(step);
            }

            BuildSign(lake.transform, BlackLakePoint + new Vector2(-6f, 4f),
                "Seeufer betrachten",
                "Das Wasser ist schwarz und vollkommen still. Knapp unter der " +
                "Oberflaeche: Treppen. Mauern. Ein Ort, der einmal trocken war. " +
                "Die Memory Watch tickt zweimal — dann schweigt sie, als fehle " +
                "ihr noch die Kraft, so tief zu erinnern.");

            // Waldhuette: von innen verriegelt, Tagebuch und Uhrenteil sichtbar.
            GameObject hut = new GameObject("Waldhuette");
            hut.transform.SetParent(gates.transform);
            float hutY = GroundY(ForestHutPoint.x, ForestHutPoint.y);
            hut.transform.position = new Vector3(ForestHutPoint.x, hutY, ForestHutPoint.y);

            BuildHutWall(hut.transform, new Vector3(0f, 1.1f, 2f), new Vector3(4f, 2.2f, 0.3f), wood);
            BuildHutWall(hut.transform, new Vector3(-2f, 1.1f, 0f), new Vector3(0.3f, 2.2f, 4f), wood);
            BuildHutWall(hut.transform, new Vector3(2f, 1.1f, 0f), new Vector3(0.3f, 2.2f, 4f), wood);
            BuildHutWall(hut.transform, new Vector3(-1.2f, 1.1f, -2f), new Vector3(1.6f, 2.2f, 0.3f), wood);
            BuildHutWall(hut.transform, new Vector3(1.2f, 1.1f, -2f), new Vector3(1.6f, 2.2f, 0.3f), wood);

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Dach";
            roof.transform.SetParent(hut.transform);
            roof.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            roof.transform.localScale = new Vector3(4.6f, 0.25f, 4.6f);
            ApplyMaterial(roof, wood);
            MarkStatic(roof);

            BuildSign(hut.transform, ForestHutPoint + new Vector2(0f, -3.5f),
                "Durchs Fenster spaehen",
                "Die Tuer ist von innen verriegelt. Durch das truebe Fenster: " +
                "ein aufgeschlagenes Tagebuch und ein Zahnrad, wie es in der " +
                "Memory Watch sitzt. Wer verriegelt eine Tuer von innen — " +
                "und ist dann nicht mehr da?");

            // Baum ohne Schatten: die Watch tickt hier unruhig.
            GameObject shadowless = new GameObject("Baum ohne Schatten");
            shadowless.transform.SetParent(gates.transform);

            string treeModel = "DeadTree_3";
            float treeScale = NormalizedScale(treeModel, 10f);
            GameObject tree = PlaceModel(treeModel, shadowless.transform, ShadowlessTreePoint, 85f, treeScale);
            AddTrunkCollider(tree, treeScale);
            accentTreePositions.Add(tree.transform.position);

            foreach (Renderer treeRenderer in tree.GetComponentsInChildren<Renderer>())
            {
                treeRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            BuildSign(shadowless.transform, ShadowlessTreePoint + new Vector2(2.5f, -2.5f),
                "Rinde untersuchen",
                "Die Sonne steht hoch, doch der Baum wirft keinen Schatten. In " +
                "der Rinde ist eine Vertiefung — genau in der Form der Memory " +
                "Watch. Sie tickt schneller, je naeher Aren kommt. Noch passt " +
                "sie nicht hinein. Noch nicht.");
        }

        // Die vier Nebenquest-NPCs aus Kapitel 1 (Platzhalter-Figuren;
        // Modelle und echtes Dialogsystem folgen mit M7).
        private static void BuildSideQuests(Transform parent)
        {
            GameObject questRoot = new GameObject("Nebenquests");
            questRoot.transform.SetParent(parent);

            // Maela, Kraeutersammlerin — der Korb am alten Pfad.
            BuildQuestNpc(questRoot.transform, new Vector2(6f, -40f), "Maela",
                new Color(0.35f, 0.5f, 0.35f), "maela_korb", "maela_korb_ziel",
                "Maela: „Ich sammle hier seit dreissig Jahren Kraeuter — und heute " +
                "weiss ich ploetzlich nicht mehr, wo mein Korb steht. Oder wie ich " +
                "zurueckkam. Wuerdest du... die Augen offen halten?“",
                "Maela: „Der Korb muss westlich liegen, beim alten Pfad. Ich traue " +
                "mich nicht — der Weg fuehlt sich fremd an.“",
                "Maela: „Mein Korb! Und... dieser Pfad. Ich bin ihn frueher taeglich " +
                "gegangen, mit meiner Schwester. Wie konnte ich das vergessen?“ " +
                "Sie sieht den Korb lange an. „Danke, Fremder.“",
                "Maela sortiert Kraeuter und summt eine Melodie, deren Namen sie " +
                "nicht mehr weiss.");
            BuildQuestObjective(questRoot.transform, new Vector2(-14f, -28f),
                "maela_korb", "maela_korb_ziel", "Korb aufheben",
                "Ein geflochtener Korb, halb unter Farnen. Daneben ein alter Pfad, " +
                "den das Moos fast verschluckt hat — aber die Trittsteine liegen in " +
                "vertrauter Reihenfolge, als haetten Fuesse sie tausendmal beruehrt.",
                CreateBasketVisual);

            // Borin, Holzfaeller — die Axt im gemiedenen Gebiet.
            BuildQuestNpc(questRoot.transform, new Vector2(-10f, -33f), "Borin",
                new Color(0.45f, 0.35f, 0.25f), "borin_axt", "borin_axt_ziel",
                "Borin: „Meine Axt liegt da drueben im Dickicht. Hol sie mir, ja? " +
                "Ich... gehe da nicht mehr rein. Frag nicht, warum. Ich weiss es " +
                "selbst nicht.“",
                "Borin: „Ostwaerts, beim grossen Fels. Ich kann dir nicht sagen, " +
                "wovor ich Angst habe. Das ist ja das Schlimmste.“",
                "Borin nimmt die Axt, wiegt sie in der Hand. „Nichts. Da war nichts " +
                "Boeses, oder? Nur Baeume.“ Er lacht unsicher. „Warum hatte ich dann " +
                "solche Angst?“",
                "Borin hackt Holz, bleibt aber stets mit dem Ruecken zum Osten.");
            BuildQuestObjective(questRoot.transform, new Vector2(26f, -38f),
                "borin_axt", "borin_axt_ziel", "Axt bergen",
                "Die Axt steckt in einem Stumpf. Fuer einen Herzschlag flackert die " +
                "Luft: Maenner, die hier einst lachten und arbeiteten — dann ist das " +
                "Bild fort. Kein Grund zur Furcht. Nur eine Erinnerung ohne Besitzer.",
                CreateAxeVisual);

            // Teren, fahrender Haendler — das panische Packtier.
            BuildQuestNpc(questRoot.transform, new Vector2(12f, -9f), "Teren",
                new Color(0.5f, 0.42f, 0.28f), "teren_packtier", "teren_packtier_ziel",
                "Teren: „Mein Packtier ist durchgegangen! Einfach so, mitten auf dem " +
                "Weg — als haette es etwas gesehen, das nicht da war. Es kann nicht " +
                "weit sein.“",
                "Teren: „Flussaufwaerts, Richtung Furt. Sei behutsam — es zittert " +
                "vor etwas, das keiner von uns sehen kann.“",
                "Das Tier folgt dir ruhig zurueck. Teren streicht ihm ueber die " +
                "Nuestern. „Was auch immer du gesehen hast, Alter — es ist fort.“ " +
                "Er sieht Aren nachdenklich an. „Oder es war nie fort. Nur vergessen.“",
                "Teren prueft seine Ladung und wirft dem Packtier besorgte Blicke zu.");
            BuildQuestObjective(questRoot.transform, new Vector2(24f, -5f),
                "teren_packtier", "teren_packtier_ziel", "Packtier beruhigen",
                "Das Tier steht zitternd vor einer leeren Wegstelle und starrt sie " +
                "an. Die Memory Watch tickt kurz — hier verlief einmal ein Zaun. " +
                "Aren spricht leise, legt die Hand auf den warmen Hals. Das Zittern " +
                "hoert auf.",
                CreatePackAnimalVisual);

            // Ilya, Waldhueterin — die beschaedigte Wegmarke.
            BuildQuestNpc(questRoot.transform, new Vector2(-4f, -19f), "Ilya",
                new Color(0.3f, 0.4f, 0.45f), "ilya_marken", "ilya_marken_ziel",
                "Ilya: „Ich richte die Wegmarken des Waldes. Eine ist umgestuerzt — " +
                "westlich von hier. Stell sie wieder auf, wenn du magst. Wanderer " +
                "verlaufen sich sonst.“",
                "Ilya: „Die Marke liegt westlich, am Lichtungsweg. Vorsicht: Mir war, " +
                "als zeigte sie kurz einen Weg, den es gar nicht gibt.“",
                "Ilya nickt langsam. „Du hast es auch gesehen, oder? Den Weg, den es " +
                "nicht gibt.“ Sie blickt nach Norden. „Vielleicht gab es ihn. " +
                "Vielleicht erinnert sich der Wald besser als wir.“",
                "Ilya prueft ihre Karten und vergleicht sie stirnrunzelnd mit den " +
                "Wegen.");
            BuildQuestObjective(questRoot.transform, new Vector2(-16f, -12f),
                "ilya_marken", "ilya_marken_ziel", "Wegmarke aufrichten",
                "Aren stemmt den Stein aufrecht. Fuer einen Augenblick gluehen die " +
                "verblassten Zeichen auf — und deuten auf einen Pfad, der mitten im " +
                "Unterholz endet. Als das Gluehen erlischt, zeigt die Marke wieder " +
                "brav zur Lichtung.",
                CreateWaymarkVisual);
        }

        // Zuordnung NPC -> Quaternius-Outfit (CC0, Universal Characters).
        private static readonly System.Collections.Generic.Dictionary<string, string> NpcOutfits =
            new System.Collections.Generic.Dictionary<string, string>
            {
                { "Maela", "Female_Peasant" },
                { "Borin", "Male_Peasant" },
                { "Ilya", "Female_Ranger" },
                { "Teren", "Male_Ranger" }
            };

        private static void BuildQuestNpc(
            Transform parent, Vector2 position, string npcName, Color clothColor,
            string questId, string objectiveId,
            string introText, string reminderText, string completionText, string idleText
        )
        {
            GameObject npc = new GameObject($"NPC_{npcName}");
            npc.transform.SetParent(parent);
            float groundY = GroundY(position.x, position.y);
            npc.transform.position = new Vector3(position.x, groundY, position.y);
            npc.transform.rotation = Quaternion.Euler(0f, position.x * 31f % 360f, 0f);

            if (!NpcOutfits.TryGetValue(npcName, out string outfitName) ||
                !BuildNpcCharacter(npc.transform, outfitName))
            {
                BuildNpcPlaceholder(npc.transform, npcName, clothColor);
            }

            SphereCollider trigger = npc.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.4f;
            trigger.center = new Vector3(0f, 1f, 0f);

            QuestGiver questGiver = npc.AddComponent<QuestGiver>();
            SerializedObject serialized = new SerializedObject(questGiver);
            serialized.FindProperty("interactionPrompt").stringValue = $"Mit {npcName} sprechen";
            serialized.FindProperty("questId").stringValue = questId;
            serialized.FindProperty("objectiveId").stringValue = objectiveId;
            serialized.FindProperty("introText").stringValue = introText;
            serialized.FindProperty("reminderText").stringValue = reminderText;
            serialized.FindProperty("completionText").stringValue = completionText;
            serialized.FindProperty("idleText").stringValue = idleText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Echter Charakter aus dem Universal-Characters-Pack: Humanoid-Import,
        // Idle ueber den PlayerAnimator (Speed=0), Materialien per Heuristik
        // (Outfit-Renderer vs. Haut). Liefert false, wenn das Modell fehlt.
        private static bool BuildNpcCharacter(Transform npcRoot, string outfitName)
        {
            string modelPath =
                $"Assets/ThirdParty/Quaternius UniversalCharacters/Outfits/{outfitName}.fbx";

            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;

            if (importer == null)
            {
                Debug.LogWarning($"NPC-Modell fehlt: {modelPath}");
                return false;
            }

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

            if (model == null)
            {
                return false;
            }

            GameObject character = (GameObject)PrefabUtility.InstantiatePrefab(model, npcRoot);
            character.name = "Charakter";
            character.transform.localPosition = Vector3.zero;
            character.transform.localRotation = Quaternion.identity;

            // Auf Spielwelt-Groesse normalisieren (Aren ist 0.75-skaliert).
            Bounds bounds = CalculateBounds(character);

            if (bounds.size.y > 0.1f)
            {
                character.transform.localScale *= 1.35f / bounds.size.y;
            }

            ApplyNpcMaterials(character, outfitName);

            // Idle-Animation ueber den vorhandenen PlayerAnimator-Controller.
            Animator animator = character.GetComponent<Animator>();

            if (animator == null)
            {
                animator = character.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/_Elyndor/Animation/Controllers/PlayerAnimator.controller");

            foreach (Object subAsset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (subAsset is Avatar avatar)
                {
                    animator.avatar = avatar;
                    break;
                }
            }

            return true;
        }

        private static void ApplyNpcMaterials(GameObject character, string outfitName)
        {
            string textureFolder = "Assets/ThirdParty/Quaternius UniversalCharacters/Textures";
            string outfitTexture = outfitName.Contains("Peasant") ? "T_Peasant" : "T_Ranger";
            string skinTexture = outfitName.StartsWith("Female")
                ? "T_Superhero_Female_Light_BaseColor"
                : "T_Superhero_Male_Ligh";

            Material outfitMaterial = GetOrCreateTexturedMaterial(
                $"Npc_{outfitTexture}", $"{textureFolder}/{outfitTexture}_BaseColor.png",
                $"{textureFolder}/{outfitTexture}_Normal.png");
            Material skinMaterial = GetOrCreateTexturedMaterial(
                $"Npc_Skin_{(outfitName.StartsWith("Female") ? "F" : "M")}",
                $"{textureFolder}/{skinTexture}.png", null);

            foreach (Renderer childRenderer in character.GetComponentsInChildren<Renderer>())
            {
                bool isOutfit =
                    childRenderer.name.Contains("Peasant") || childRenderer.name.Contains("Ranger") ||
                    childRenderer.name.Contains("Body") || childRenderer.name.Contains("Arms") ||
                    childRenderer.name.Contains("Legs") || childRenderer.name.Contains("Feet") ||
                    childRenderer.name.Contains("Hood") || childRenderer.name.Contains("Pauldron");

                Material[] materials = childRenderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = isOutfit ? outfitMaterial : skinMaterial;
                }

                childRenderer.sharedMaterials = materials;
            }
        }

        private static Material GetOrCreateTexturedMaterial(
            string name, string baseMapPath, string normalMapPath
        )
        {
            Material material = LoadOrCreateMaterialAsset(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.1f);

            Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(baseMapPath);

            if (baseMap != null)
            {
                material.SetTexture("_BaseMap", baseMap);
            }
            else
            {
                Debug.LogWarning($"NPC-Textur fehlt: {baseMapPath}");
            }

            if (normalMapPath != null)
            {
                Texture2D normalMap = LoadPackTexture(
                    System.IO.Path.GetDirectoryName(normalMapPath).Replace('\\', '/'),
                    System.IO.Path.GetFileNameWithoutExtension(normalMapPath),
                    asNormalMap: true);

                if (normalMap != null)
                {
                    material.SetTexture("_BumpMap", normalMap);
                    material.EnableKeyword("_NORMALMAP");
                }
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        // Fallback, falls das Charaktermodell nicht verfuegbar ist.
        private static void BuildNpcPlaceholder(Transform npcRoot, string npcName, Color clothColor)
        {
            Material cloth = GetOrCreateMaterial($"Proto_Npc_{npcName}", clothColor);
            Material skin = GetOrCreateMaterial("Proto_NpcSkin", new Color(0.8f, 0.65f, 0.52f));

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Koerper";
            body.transform.SetParent(npcRoot);
            body.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.85f, 0.55f);
            ApplyMaterial(body, cloth);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Kopf";
            head.transform.SetParent(npcRoot);
            head.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            head.transform.localScale = Vector3.one * 0.4f;
            Object.DestroyImmediate(head.GetComponent<Collider>());
            ApplyMaterial(head, skin);
        }

        private static void BuildQuestObjective(
            Transform parent, Vector2 position, string questId, string objectiveId,
            string prompt, string foundText, System.Action<Transform> buildVisual
        )
        {
            GameObject objective = new GameObject($"Ziel_{objectiveId}");
            objective.transform.SetParent(parent);
            objective.transform.position =
                new Vector3(position.x, GroundY(position.x, position.y), position.y);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(objective.transform);
            visual.transform.localPosition = Vector3.zero;
            buildVisual(visual.transform);

            SphereCollider trigger = objective.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.2f;
            trigger.center = new Vector3(0f, 0.8f, 0f);

            QuestObjective questObjective = objective.AddComponent<QuestObjective>();
            SerializedObject serialized = new SerializedObject(questObjective);
            serialized.FindProperty("interactionPrompt").stringValue = prompt;
            serialized.FindProperty("questId").stringValue = questId;
            serialized.FindProperty("objectiveId").stringValue = objectiveId;
            serialized.FindProperty("foundText").stringValue = foundText;
            serialized.FindProperty("visualRoot").objectReferenceValue = visual;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateBasketVisual(Transform parent)
        {
            Material wood = GetOrCreateMaterial("Proto_Wood", WoodColor);
            GameObject basket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basket.name = "Korb";
            basket.transform.SetParent(parent);
            basket.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            basket.transform.localScale = new Vector3(0.5f, 0.18f, 0.5f);
            Object.DestroyImmediate(basket.GetComponent<Collider>());
            ApplyMaterial(basket, wood);
        }

        private static void CreateAxeVisual(Transform parent)
        {
            Material wood = GetOrCreateMaterial("Proto_Wood", WoodColor);
            Material stone = GetOrCreateMaterial("Proto_Stone", StoneColor);

            GameObject stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stump.name = "Stumpf";
            stump.transform.SetParent(parent);
            stump.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            stump.transform.localScale = new Vector3(0.6f, 0.25f, 0.6f);
            Object.DestroyImmediate(stump.GetComponent<Collider>());
            ApplyMaterial(stump, wood);

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Axt";
            blade.transform.SetParent(parent);
            blade.transform.localPosition = new Vector3(0.1f, 0.7f, 0f);
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            blade.transform.localScale = new Vector3(0.08f, 0.7f, 0.2f);
            Object.DestroyImmediate(blade.GetComponent<Collider>());
            ApplyMaterial(blade, stone);
        }

        private static void CreatePackAnimalVisual(Transform parent)
        {
            Material hide = GetOrCreateMaterial("Proto_PackAnimal", new Color(0.42f, 0.33f, 0.24f));

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Packtier";
            body.transform.SetParent(parent);
            body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            body.transform.localRotation = Quaternion.Euler(90f, 20f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            ApplyMaterial(body, hide);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Kopf";
            head.transform.SetParent(parent);
            head.transform.localPosition = new Vector3(0.35f, 1.15f, 0.85f);
            head.transform.localScale = Vector3.one * 0.45f;
            Object.DestroyImmediate(head.GetComponent<Collider>());
            ApplyMaterial(head, hide);
        }

        private static void CreateWaymarkVisual(Transform parent)
        {
            Material stone = GetOrCreateMaterial("Proto_Stone", StoneColor);
            GameObject fallenMark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallenMark.name = "Umgestuerzte Wegmarke";
            fallenMark.transform.SetParent(parent);
            fallenMark.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            fallenMark.transform.localRotation = Quaternion.Euler(85f, 30f, 0f);
            fallenMark.transform.localScale = new Vector3(0.32f, 1.05f, 0.32f);
            Object.DestroyImmediate(fallenMark.GetComponent<Collider>());
            ApplyMaterial(fallenMark, stone);
        }

        // Der beschaedigte Rucksack neben der Aufwachstelle (Kapitel 1).
        private static void BuildBackpack(Transform parent)
        {
            GameObject backpack = new GameObject("Beschaedigter Rucksack");
            backpack.transform.SetParent(parent);
            Vector2 position = new Vector2(1.6f, -55.5f);
            backpack.transform.position =
                new Vector3(position.x, GroundY(position.x, position.y), position.y);

            Material leather = GetOrCreateMaterial("Proto_Leather", new Color(0.4f, 0.28f, 0.18f));

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(backpack.transform);
            visual.transform.localPosition = Vector3.zero;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Rucksackkorpus";
            body.transform.SetParent(visual.transform);
            body.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            body.transform.localRotation = Quaternion.Euler(-12f, 35f, 6f);
            body.transform.localScale = new Vector3(0.45f, 0.55f, 0.3f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            ApplyMaterial(body, leather);

            GameObject flap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flap.name = "Klappe";
            flap.transform.SetParent(visual.transform);
            flap.transform.localPosition = new Vector3(0.05f, 0.55f, 0.05f);
            flap.transform.localRotation = Quaternion.Euler(-45f, 35f, 6f);
            flap.transform.localScale = new Vector3(0.45f, 0.22f, 0.05f);
            Object.DestroyImmediate(flap.GetComponent<Collider>());
            ApplyMaterial(flap, leather);

            SphereCollider trigger = backpack.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2f;
            trigger.center = new Vector3(0f, 0.5f, 0f);

            Elyndor.Inventory.BackpackPickup pickup =
                backpack.AddComponent<Elyndor.Inventory.BackpackPickup>();
            SerializedObject serialized = new SerializedObject(pickup);
            serialized.FindProperty("interactionPrompt").stringValue = "Rucksack durchsuchen";
            serialized.FindProperty("visualRoot").objectReferenceValue = visual;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Strohpuppe beim Lager: Ziel des Kampf-Tutorials.
        private static void BuildTrainingDummy(Transform parent)
        {
            GameObject dummy = new GameObject("Uebungspuppe");
            dummy.transform.SetParent(parent);
            Vector2 position = new Vector2(4.5f, -52.5f);
            dummy.transform.position =
                new Vector3(position.x, GroundY(position.x, position.y), position.y);

            Material wood = GetOrCreateMaterial("Proto_Wood", WoodColor);
            Material straw = GetOrCreateMaterial("Proto_Straw", new Color(0.62f, 0.54f, 0.3f));

            GameObject shakeRoot = new GameObject("Puppe");
            shakeRoot.transform.SetParent(dummy.transform);
            shakeRoot.transform.localPosition = Vector3.zero;

            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Pfahl";
            post.transform.SetParent(shakeRoot.transform);
            post.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            post.transform.localScale = new Vector3(0.16f, 0.9f, 0.16f);
            ApplyMaterial(post, wood);

            GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossbar.name = "Querbalken";
            crossbar.transform.SetParent(shakeRoot.transform);
            crossbar.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            crossbar.transform.localScale = new Vector3(1.2f, 0.12f, 0.12f);
            ApplyMaterial(crossbar, wood);

            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Strohkoerper";
            torso.transform.SetParent(shakeRoot.transform);
            torso.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            torso.transform.localScale = new Vector3(0.55f, 0.45f, 0.4f);
            ApplyMaterial(torso, straw);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Strohkopf";
            head.transform.SetParent(shakeRoot.transform);
            head.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            head.transform.localScale = Vector3.one * 0.35f;
            Object.DestroyImmediate(head.GetComponent<Collider>());
            ApplyMaterial(head, straw);

            Elyndor.Combat.TrainingDummy trainingDummy =
                dummy.AddComponent<Elyndor.Combat.TrainingDummy>();
            SerializedObject serialized = new SerializedObject(trainingDummy);
            serialized.FindProperty("shakeTarget").objectReferenceValue = shakeRoot.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Inventar-Panel (Taste I): Ausruestung links, Tasche rechts.
        internal static void BuildInventoryUI(GameObject canvasObject, Font font)
        {
            GameObject panel = new GameObject("InventoryPanel");
            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(680f, 460f);

            UnityEngine.UI.Image background = panel.AddComponent<UnityEngine.UI.Image>();
            Sprite panelSprite = LoadUiSprite(
                "Assets/ThirdParty/Kenney/kenney_ui-pack-rpg-expansion/PNG/panel_brown.png");

            if (panelSprite != null)
            {
                background.sprite = panelSprite;
                background.type = UnityEngine.UI.Image.Type.Sliced;
                background.color = Color.white;
            }
            else
            {
                background.color = new Color(0.05f, 0.06f, 0.08f, 0.92f);
            }

            background.raycastTarget = false;

            // Dunkle Schrift auf dem hellen Kenney-Panel.
            Color inkColor = new Color(0.27f, 0.19f, 0.12f);

            Text title = CreatePanelText(panel.transform, font, "Inventar", 26,
                new Vector2(0f, 195f), new Vector2(640f, 40f), TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            title.color = inkColor;

            Text equipmentHeader = CreatePanelText(panel.transform, font, "Ausrüstung", 22,
                new Vector2(-170f, 150f), new Vector2(300f, 32f), TextAnchor.MiddleCenter);
            equipmentHeader.color = new Color(0.45f, 0.3f, 0.16f);

            Text bagHeader = CreatePanelText(panel.transform, font, "Tasche", 22,
                new Vector2(170f, 150f), new Vector2(300f, 32f), TextAnchor.MiddleCenter);
            bagHeader.color = new Color(0.45f, 0.3f, 0.16f);

            Text equipmentText = CreatePanelText(panel.transform, font, "", 20,
                new Vector2(-170f, -25f), new Vector2(300f, 300f), TextAnchor.UpperLeft);
            equipmentText.color = inkColor;

            Text bagText = CreatePanelText(panel.transform, font, "", 20,
                new Vector2(170f, -25f), new Vector2(300f, 300f), TextAnchor.UpperLeft);
            bagText.color = inkColor;

            panel.SetActive(false);

            InventoryUI inventoryUI = canvasObject.AddComponent<InventoryUI>();
            SerializedObject serialized = new SerializedObject(inventoryUI);
            serialized.FindProperty("panelRoot").objectReferenceValue = panel;
            serialized.FindProperty("equipmentText").objectReferenceValue = equipmentText;
            serialized.FindProperty("bagText").objectReferenceValue = bagText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text CreatePanelText(
            Transform parent, Font font, string content, int fontSize,
            Vector2 anchoredPosition, Vector2 size, TextAnchor alignment
        )
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.9f, 0.91f, 0.93f);
            text.raycastTarget = false;
            text.text = content;

            return text;
        }

        // Atmosphaeren-Partikel: Staub im Licht, Gluehwuermchen an stillen Orten.
        private static void BuildAtmosphereParticles(Transform parent)
        {
            GameObject particleRoot = new GameObject("Atmosphaere");
            particleRoot.transform.SetParent(parent);

            BuildDustParticles(particleRoot.transform,
                new Vector3(0f, 6f, 0f), new Vector3(180f, 14f, 180f),
                new Color(0.9f, 0.88f, 0.75f, 0.16f), 70f);

            BuildGlowParticles(particleRoot.transform, "Gluehwuermchen_MemorySite",
                new Vector3(MemorySitePoint.x, 1f, MemorySitePoint.y), 4f,
                new Color(0.65f, 0.9f, 0.6f, 0.85f), 8f);

            BuildGlowParticles(particleRoot.transform, "Gluehwuermchen_Lichtung",
                new Vector3(ClearingPoint.x, 1.2f, ClearingPoint.y), 8f,
                new Color(0.75f, 0.9f, 0.55f, 0.7f), 12f);
        }

        // Grossflaechiger, traeger Schwebstaub.
        internal static void BuildDustParticles(
            Transform parent, Vector3 center, Vector3 boxSize, Color color, float rate
        )
        {
            ParticleSystem system = CreateParticleObject(parent, "Schwebstaub", center);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1200;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            ConfigureDrift(system, 0.06f);
            ApplyGlowMaterial(system);
        }

        // Kleine leuchtende Punkte, die gemaechlich umherwandern.
        internal static void BuildGlowParticles(
            Transform parent, string name, Vector3 center, float radius, Color color, float rate
        )
        {
            ParticleSystem system = CreateParticleObject(parent, name, center);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            ConfigureDrift(system, 0.25f);
            ApplyGlowMaterial(system);
        }

        private static ParticleSystem CreateParticleObject(
            Transform parent, string name, Vector3 position
        )
        {
            GameObject particleObject = new GameObject(name);
            particleObject.transform.SetParent(parent);
            particleObject.transform.position = position;
            return particleObject.AddComponent<ParticleSystem>();
        }

        private static void ConfigureDrift(ParticleSystem system, float strength)
        {
            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = strength;
            noise.frequency = 0.15f;

            // Weiches Ein- und Ausblenden ueber die Lebenszeit.
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void ApplyGlowMaterial(ParticleSystem system)
        {
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetOrCreateGlowParticleMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        // Additives Punktlicht-Material mit radialer Falloff-Textur.
        private static Material GetOrCreateGlowParticleMaterial()
        {
            Material material = LoadOrCreateMaterialAsset(
                "Proto_ParticleGlow", "Universal Render Pipeline/Particles/Unlit");
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", GetOrCreateGlowTexture());
            material.SetFloat("_Surface", 1f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D GetOrCreateGlowTexture()
        {
            EnsureFolder("Assets/_Elyndor/Art", "Terrain");
            string texturePath = $"{TerrainFolder}/Particle_Glow.png";

            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(
                        new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float alpha = Mathf.Clamp01(1f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            texture.Apply();
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        private static void BuildHutWall(
            Transform parent,
            Vector3 localPosition,
            Vector3 scale,
            Material material
        )
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Huettenwand";
            wall.transform.SetParent(parent);
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, material);
            MarkStatic(wall);
        }

        // Drei neue Story-Orte im Aussenring der vergroesserten Karte.
        private static void BuildOuterRingZones(Transform parent)
        {
            GameObject outerRing = new GameObject("Aussenring");
            outerRing.transform.SetParent(parent);

            Material stone = GetOrCreateMaterial("Proto_Stone", StoneColor);
            Material wood = GetOrCreateMaterial("Proto_Wood", WoodColor);

            // Verfallener Wachturm auf der Nordost-Kuppe.
            GameObject tower = new GameObject("Verfallener Wachturm");
            tower.transform.SetParent(outerRing.transform);
            float towerY = GroundY(WatchtowerPoint.x, WatchtowerPoint.y);
            tower.transform.position = new Vector3(WatchtowerPoint.x, towerY, WatchtowerPoint.y);

            float[] ringScales = { 3.4f, 3f, 2.6f };
            for (int i = 0; i < ringScales.Length; i++)
            {
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Turmring";
                ring.transform.SetParent(tower.transform);
                ring.transform.localPosition = new Vector3(0f, 0.7f + i * 1.4f, 0f);
                ring.transform.localRotation = Quaternion.Euler(i * 2f, i * 30f, i * 3f);
                ring.transform.localScale = new Vector3(ringScales[i], 0.7f, ringScales[i]);
                ApplyMaterial(ring, stone);
                MarkStatic(ring);
            }

            for (int i = 0; i < 5; i++)
            {
                float angle = i * 1.9f;
                GameObject rubble = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rubble.name = "Truemmerstein";
                rubble.transform.SetParent(tower.transform);
                rubble.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * (2.6f + i * 0.4f), 0.25f, Mathf.Sin(angle) * (2.6f + i * 0.3f)
                );
                rubble.transform.localRotation = Quaternion.Euler(0f, i * 47f, 8f);
                rubble.transform.localScale = new Vector3(0.8f, 0.5f, 0.6f);
                ApplyMaterial(rubble, stone);
                MarkStatic(rubble);
            }

            BuildSign(tower.transform, WatchtowerPoint + new Vector2(-3.5f, -2f),
                "Wachturm untersuchen",
                "Von hier oben bewachte jemand den Wald — aber die Ostwand ist " +
                "nicht eingestuerzt, sie wurde abgetragen. Stein fuer Stein. " +
                "Wovor hatte man aufgehoert, sich zu fuerchten?");

            // Stiller Teich in der Westsenke.
            GameObject pond = new GameObject("Stiller Teich");
            pond.transform.SetParent(outerRing.transform);
            float pondBedY = GroundY(QuietPondPoint.x, QuietPondPoint.y);

            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Teichwasser";
            water.transform.SetParent(pond.transform);
            water.transform.position = new Vector3(QuietPondPoint.x, pondBedY + 0.4f, QuietPondPoint.y);
            water.transform.localScale = new Vector3(1.3f, 1f, 1.1f);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            ApplyMaterial(water, GetOrCreateWaterMaterial());
            MarkStatic(water);

            System.Random pondRng = new System.Random(83);
            for (int i = 0; i < 14; i++)
            {
                float angle = (float)pondRng.NextDouble() * Mathf.PI * 2f;
                Vector2 position = QuietPondPoint +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
                    Mathf.Lerp(6.5f, 9f, (float)pondRng.NextDouble());
                PlaceModel("Grass_Wispy_Tall", pond.transform, position,
                    (float)pondRng.NextDouble() * 360f,
                    NormalizedScale("Grass_Wispy_Tall", 0.7f));
            }

            BuildSign(pond.transform, QuietPondPoint + new Vector2(5f, 3f),
                "Ufer betrachten",
                "Das Wasser ist so still, dass es mehr Himmel zeigt als Grund. " +
                "Am Ufer liegen kleine Steine in einer Reihe — als haette hier " +
                "jemand oft gesessen und sie geordnet, um nachzudenken.");

            // Jaegerlager im Suedwesten.
            GameObject camp = new GameObject("Verlassenes Jaegerlager");
            camp.transform.SetParent(outerRing.transform);
            float campY = GroundY(HunterCampPoint.x, HunterCampPoint.y);
            camp.transform.position = new Vector3(HunterCampPoint.x, campY, HunterCampPoint.y);

            System.Random campRng = new System.Random(89);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7f;
                Vector2 position = HunterCampPoint +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.9f;
                PlaceModel($"Pebble_Square_{1 + i % 6}", camp.transform, position,
                    (float)campRng.NextDouble() * 360f,
                    NormalizedScale($"Pebble_Square_{1 + i % 6}", 0.3f));
            }

            for (int i = 0; i < 2; i++)
            {
                GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                log.name = "Sitzstamm";
                log.transform.SetParent(camp.transform);
                log.transform.localPosition = new Vector3(-1.8f + i * 3.6f, 0.3f, 1.6f - i * 0.6f);
                log.transform.localRotation = Quaternion.Euler(0f, 25f + i * 120f, 90f);
                log.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                ApplyMaterial(log, wood);
                MarkStatic(log);
            }

            BuildSign(camp.transform, HunterCampPoint + new Vector2(2.5f, -2f),
                "Lagerstelle untersuchen",
                "Kalte Asche, sauber aufgeschichtetes Holz daneben — bereit fuer " +
                "ein Feuer, das nie entzuendet wurde. Wer auch immer hier lagerte, " +
                "wollte wiederkommen. Und hat es dann wohl vergessen.");
        }

        // Oeffnet die Zielszene und erneuert nur die generierten Wurzeln.
        // Der "[Handarbeit]"-Knoten wird NIE angetastet — dort platzierte
        // Objekte ueberleben jede Regenerierung.
        private static Scene PrepareTargetScene()
        {
            Scene scene;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) == null)
            {
                CopyBootstrapScene();
                scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

                foreach (string generatedRootName in new[]
                         { "Environment", "Finsterwald_Terrain", "MemoryVisionVolume", "PrototypeHUD" })
                {
                    GameObject generatedRoot = GameObject.Find(generatedRootName);

                    if (generatedRoot != null)
                    {
                        Object.DestroyImmediate(generatedRoot);
                    }
                }
            }

            if (GameObject.Find("[Handarbeit]") == null)
            {
                new GameObject("[Handarbeit]");
            }

            return scene;
        }

        private static void CopyBootstrapScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) != null)
            {
                AssetDatabase.DeleteAsset(TargetScenePath);
            }

            if (!AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath))
            {
                throw new System.InvalidOperationException(
                    $"Bootstrap-Szene konnte nicht nach {TargetScenePath} kopiert werden."
                );
            }
        }

        // ------------------------------------------------------------------
        // Atmosphäre
        // ------------------------------------------------------------------

        // Render-Pipeline-Feinschliff: weitere Schatten, SSAO fuer Plastizitaet.
        private static void ConfigureRenderPipeline()
        {
            UniversalRenderPipelineAsset pipelineAsset =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                    "Assets/Settings/PC_RPAsset.asset");

            if (pipelineAsset != null)
            {
                pipelineAsset.shadowDistance = 90f;
                pipelineAsset.shadowCascadeCount = 3;

                // Kostenloses Schaerfe-Paket: Kantenglaettung, HDR (bessere
                // Bloom-Qualitaet), hochaufgeloeste weiche Schatten.
                pipelineAsset.msaaSampleCount = 4;
                pipelineAsset.supportsHDR = true;
                pipelineAsset.mainLightShadowmapResolution = 4096;

                SerializedObject serializedPipeline = new SerializedObject(pipelineAsset);
                SerializedProperty softShadows =
                    serializedPipeline.FindProperty("m_SoftShadowsSupported");

                if (softShadows != null)
                {
                    softShadows.boolValue = true;
                    serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorUtility.SetDirty(pipelineAsset);
            }

            ScriptableRendererData rendererData =
                AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                    "Assets/Settings/PC_Renderer.asset");

            if (rendererData == null)
            {
                Debug.LogWarning("PC_Renderer.asset nicht gefunden — SSAO uebersprungen.");
                return;
            }

            foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
            {
                if (feature is ScreenSpaceAmbientOcclusion)
                {
                    return;
                }
            }

            ScreenSpaceAmbientOcclusion ssao =
                ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            ssao.name = "SSAO";
            AssetDatabase.AddObjectToAsset(ssao, rendererData);

            // rendererFeatures + FeatureMap muessen synchron gepflegt werden.
            SerializedObject serializedRenderer = new SerializedObject(rendererData);
            SerializedProperty features = serializedRenderer.FindProperty("m_RendererFeatures");
            SerializedProperty featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");

            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = ssao;

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                ssao, out _, out long localId);
            featureMap.arraySize++;
            featureMap.GetArrayElementAtIndex(featureMap.arraySize - 1).longValue = localId;

            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
        }

        private static void ConfigureLightingAndAtmosphere()
        {
            Light sun = ConfigureSunLight();

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogDensity = 0.012f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.58f, 0.64f);
            RenderSettings.ambientEquatorColor = new Color(0.40f, 0.44f, 0.41f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.22f, 0.19f);

            RenderSettings.skybox = CreateSkyboxMaterial();
            RenderSettings.sun = sun;
        }

        private static Light ConfigureSunLight()
        {
            GameObject lightObject = GameObject.Find("Directional Light");

            if (lightObject == null)
            {
                lightObject = new GameObject("Directional Light");
                lightObject.AddComponent<Light>().type = LightType.Directional;
            }

            Light sun = lightObject.GetComponent<Light>();
            sun.color = SunColor;
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            lightObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

            return sun;
        }

        private static Material CreateSkyboxMaterial()
        {
            Material skybox = LoadOrCreateMaterialAsset("Proto_Skybox", "Skybox/Procedural");
            skybox.SetColor("_SkyTint", new Color(0.50f, 0.57f, 0.62f));
            skybox.SetColor("_GroundColor", new Color(0.35f, 0.38f, 0.35f));
            skybox.SetFloat("_Exposure", 1.0f);
            skybox.SetFloat("_AtmosphereThickness", 1.15f);
            skybox.SetFloat("_SunSize", 0.03f);
            EditorUtility.SetDirty(skybox);
            return skybox;
        }

        private static void ConfigureBaseVolume()
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

            volume.sharedProfile = CreateProfileAsset("Finsterwald_Base_Profile", profile =>
            {
                Vignette vignette = AddOverride<Vignette>(profile);
                vignette.intensity.Override(0.18f);
                vignette.smoothness.Override(0.5f);

                ColorAdjustments colorAdjustments = AddOverride<ColorAdjustments>(profile);
                colorAdjustments.saturation.Override(-8f);
                colorAdjustments.contrast.Override(6f);

                // Dezentes Bloom: laesst Geister-Echos und Glanzlichter atmen.
                Bloom bloom = AddOverride<Bloom>(profile);
                bloom.intensity.Override(0.35f);
                bloom.threshold.Override(0.95f);
                bloom.scatter.Override(0.6f);
            });
        }

        private static void BuildMemoryVisionVolume()
        {
            GameObject volumeObject = new GameObject("MemoryVisionVolume");

            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 0f;

            volume.sharedProfile = CreateProfileAsset("Finsterwald_MemoryVision_Profile", profile =>
            {
                Vignette vignette = AddOverride<Vignette>(profile);
                vignette.intensity.Override(0.38f);
                vignette.smoothness.Override(0.6f);

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

        private static VolumeProfile CreateProfileAsset(
            string name,
            System.Action<VolumeProfile> configure
        )
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

        private static void RemoveLegacyGround()
        {
            GameObject legacyGround = GameObject.Find("Ground");

            if (legacyGround != null)
            {
                Object.DestroyImmediate(legacyGround);
            }
        }

        private static void CreateForestTerrain()
        {
            EnsureFolder("Assets/_Elyndor/Art", "Terrain");

            string dataPath = $"{TerrainFolder}/Finsterwald_TerrainData.asset";
            AssetDatabase.DeleteAsset(dataPath);

            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = HeightmapResolution;
            terrainData.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
            terrainData.SetHeights(0, 0, GenerateHeights());
            terrainData.alphamapResolution = AlphamapResolution;
            terrainData.terrainLayers = CreateTerrainLayers();
            terrainData.SetAlphamaps(0, 0, GenerateAlphamaps());

            AssetDatabase.CreateAsset(terrainData, dataPath);

            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "Finsterwald_Terrain";
            terrainObject.transform.position =
                new Vector3(-TerrainSize / 2f, -BaseLevel, -TerrainSize / 2f);

            forestTerrain = terrainObject.GetComponent<Terrain>();
            forestTerrain.heightmapPixelError = 6f;
            forestTerrain.drawInstanced = true;
            forestTerrain.basemapDistance = 400f;

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

                    float height = RawHeight(wx, wz);
                    height -= BrookCarve(wx, wz);
                    height = ApplyPathSmoothing(height, wx, wz);

                    heights[zi, xi] = Mathf.Clamp01((BaseLevel + height) / TerrainHeight);
                }
            }

            return heights;
        }

        private static float RawHeight(float wx, float wz)
        {
            float rolling = (Mathf.PerlinNoise(wx * 0.03f + 37.7f, wz * 0.03f + 11.3f) - 0.5f) * 3.2f;
            float detail = (Mathf.PerlinNoise(wx * 0.12f + 7.3f, wz * 0.12f + 3.1f) - 0.5f) * 0.7f;
            float height = rolling + detail;

            height += Hill(wx, wz, ViewpointPoint, 20f, 7f);
            height += Hill(wx, wz, new Vector2(-54f, -48f), 18f, 2.6f);
            height += Hill(wx, wz, new Vector2(58f, -40f), 16f, 2.2f);
            height += Hill(wx, wz, StoneCirclePoint, 10f, 1.1f);

            height += Hill(wx, wz, WatchtowerPoint, 12f, 2.2f);
            height -= Hill(wx, wz, BlackLakePoint, 11f, 1.4f);
            height = FlattenTowards(height, wx, wz, ForestHutPoint, 7f, 0.3f, 0.8f);
            height -= Hill(wx, wz, QuietPondPoint, 8f, 1.1f);
            height = FlattenTowards(height, wx, wz, HunterCampPoint, 8f, 0.2f, 0.8f);

            height = FlattenTowards(height, wx, wz, ClearingPoint, 10f, 0.3f, 0.85f);
            height = FlattenTowards(height, wx, wz, StartPoint, 8f, 0.1f, 0.7f);
            height = FlattenTowards(height, wx, wz, RuinPoint, 7f, 1.0f, 0.8f);

            return height;
        }

        private static float Hill(float wx, float wz, Vector2 center, float radius, float height)
        {
            float distance = Vector2.Distance(new Vector2(wx, wz), center);

            if (distance >= radius)
            {
                return 0f;
            }

            return height * (0.5f + 0.5f * Mathf.Cos(Mathf.PI * distance / radius));
        }

        private static float FlattenTowards(
            float height,
            float wx,
            float wz,
            Vector2 center,
            float radius,
            float targetHeight,
            float strength
        )
        {
            float distance = Vector2.Distance(new Vector2(wx, wz), center);

            if (distance >= radius)
            {
                return height;
            }

            float weight = (0.5f + 0.5f * Mathf.Cos(Mathf.PI * distance / radius)) * strength;
            return Mathf.Lerp(height, targetHeight, weight);
        }

        private static float BrookCarve(float wx, float wz)
        {
            float distance = DistanceToPolyline(new Vector2(wx, wz), brookSamples);

            if (distance >= BrookHalfWidth)
            {
                return 0f;
            }

            float depth = BrookDepth * FordDepthFactor(new Vector2(wx, wz));
            return depth * (0.5f + 0.5f * Mathf.Cos(Mathf.PI * distance / BrookHalfWidth));
        }

        // An der Furt ist der Bach flach genug, um ihn zu Fuß zu queren.
        private static float FordDepthFactor(Vector2 position)
        {
            float distance = Vector2.Distance(position, FordPoint);

            if (distance < 4f)
            {
                return 0.2f;
            }

            if (distance < 9f)
            {
                return Mathf.Lerp(0.2f, 1f, (distance - 4f) / 5f);
            }

            return 1f;
        }

        private static float ApplyPathSmoothing(float height, float wx, float wz)
        {
            Vector2 position = new Vector2(wx, wz);
            float bestDistance = float.MaxValue;
            float bestTarget = height;
            bool bestHidden = false;

            foreach (PathSample sample in pathSamples)
            {
                float distance = Vector2.Distance(position, sample.Position);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = sample.TargetHeight;
                    bestHidden = sample.Hidden;
                }
            }

            float radius = bestHidden ? 1.8f : 2.6f;
            float strength = bestHidden ? 0.6f : 0.75f;

            if (bestDistance >= radius)
            {
                return height;
            }

            float weight = (1f - bestDistance / radius) * strength;
            return Mathf.Lerp(height, bestTarget, weight);
        }

        private static TerrainLayer[] CreateTerrainLayers()
        {
            return new[]
            {
                CreateTerrainLayer("Terrain_Wald", new Color(0.20f, 0.29f, 0.18f)),
                CreateTerrainLayer("Terrain_Pfad", new Color(0.42f, 0.35f, 0.25f)),
                CreateTerrainLayer("Terrain_Fels", new Color(0.36f, 0.37f, 0.38f)),
                CreateTerrainLayer("Terrain_Wiese", new Color(0.27f, 0.37f, 0.19f))
            };
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
            EnsureFolder("Assets/_Elyndor/Art", "Terrain");

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

        private static float[,,] GenerateAlphamaps()
        {
            float[,,] alphamaps = new float[AlphamapResolution, AlphamapResolution, 4];

            for (int zi = 0; zi < AlphamapResolution; zi++)
            {
                for (int xi = 0; xi < AlphamapResolution; xi++)
                {
                    float wx = -TerrainSize / 2f + xi / (float)(AlphamapResolution - 1) * TerrainSize;
                    float wz = -TerrainSize / 2f + zi / (float)(AlphamapResolution - 1) * TerrainSize;
                    Vector2 position = new Vector2(wx, wz);

                    float pathWeight = PathPaintWeight(position);
                    float rockWeight = RockPaintWeight(position);
                    float meadowWeight = MeadowPaintWeight(position);

                    float total = pathWeight + rockWeight + meadowWeight;

                    if (total > 1f)
                    {
                        pathWeight /= total;
                        rockWeight /= total;
                        meadowWeight /= total;
                        total = 1f;
                    }

                    alphamaps[zi, xi, 0] = 1f - total;
                    alphamaps[zi, xi, 1] = pathWeight;
                    alphamaps[zi, xi, 2] = rockWeight;
                    alphamaps[zi, xi, 3] = meadowWeight;
                }
            }

            return alphamaps;
        }

        private static float PathPaintWeight(Vector2 position)
        {
            float weight = 0f;

            foreach (PathSample sample in pathSamples)
            {
                float distance = Vector2.Distance(position, sample.Position);

                float sampleWeight;
                if (sample.Hidden)
                {
                    sampleWeight = distance < 0.8f
                        ? 0.7f
                        : Mathf.Clamp01((1.6f - distance) / 0.8f) * 0.7f;
                }
                else
                {
                    sampleWeight = distance < 1.2f
                        ? 1f
                        : Mathf.Clamp01((2.4f - distance) / 1.2f);
                }

                weight = Mathf.Max(weight, sampleWeight);

                if (weight >= 1f)
                {
                    break;
                }
            }

            return weight;
        }

        private static float RockPaintWeight(Vector2 position)
        {
            float weight = RadialWeight(position, RuinPoint, 7f) * 0.6f;
            weight = Mathf.Max(weight, RadialWeight(position, StoneCirclePoint, 6f) * 0.55f);
            weight = Mathf.Max(weight, RadialWeight(position, ViewpointPoint, 5f) * 0.35f);

            float brookDistance = DistanceToPolyline(position, brookSamples);
            if (brookDistance < 2.2f)
            {
                weight = Mathf.Max(weight, 0.8f * (1f - brookDistance / 2.2f) + 0.3f);
            }

            return Mathf.Clamp01(weight);
        }

        private static float MeadowPaintWeight(Vector2 position)
        {
            float weight = RadialWeight(position, ClearingPoint, 10f) * 0.9f;
            weight = Mathf.Max(weight, RadialWeight(position, ViewpointPoint, 6f) * 0.6f);
            weight = Mathf.Max(weight, RadialWeight(position, StartPoint, 7f) * 0.4f);
            return weight;
        }

        private static float RadialWeight(Vector2 position, Vector2 center, float radius)
        {
            float distance = Vector2.Distance(position, center);
            return distance >= radius ? 0f : 0.5f + 0.5f * Mathf.Cos(Mathf.PI * distance / radius);
        }

        // ------------------------------------------------------------------
        // Pfade und Bach (Geometrie-Vorberechnung)
        // ------------------------------------------------------------------

        // Aussenring-Zonen (Kartenerweiterung auf 200 x 200 m).
        private static readonly Vector2 WatchtowerPoint = new Vector2(72f, 62f);
        private static readonly Vector2 QuietPondPoint = new Vector2(-76f, -34f);
        private static readonly Vector2 HunterCampPoint = new Vector2(-42f, -74f);

        private static readonly Vector2[][] OuterPaths =
        {
            // Aussichtspunkt -> Wachturm
            new[]
            {
                new Vector2(48f, 42f), new Vector2(56f, 50f), new Vector2(64f, 57f),
                new Vector2(72f, 62f)
            },
            // Lichtung -> Stiller Teich
            new[]
            {
                new Vector2(-27f, -15f), new Vector2(-42f, -20f), new Vector2(-58f, -27f),
                new Vector2(-70f, -31f), new Vector2(-76f, -34f)
            },
            // Start -> Jaegerlager
            new[]
            {
                new Vector2(0f, -58f), new Vector2(-14f, -63f), new Vector2(-28f, -69f),
                new Vector2(-42f, -74f)
            }
        };

        private static void SamplePathsAndBrook()
        {
            brookSamples = SampleSpline(BrookPoints, 1f);
            pathSamples = new List<PathSample>();

            foreach (Vector2[] path in VisiblePaths)
            {
                AddPathSamples(path, hidden: false);
            }

            foreach (Vector2[] path in OuterPaths)
            {
                AddPathSamples(path, hidden: false);
            }

            foreach (Vector2[] path in HiddenPaths)
            {
                AddPathSamples(path, hidden: true);
            }
        }

        private static void AddPathSamples(Vector2[] points, bool hidden)
        {
            foreach (Vector2 samplePosition in SampleSpline(points, 0.75f))
            {
                pathSamples.Add(new PathSample
                {
                    Position = samplePosition,
                    TargetHeight = RawHeight(samplePosition.x, samplePosition.y),
                    Hidden = hidden
                });
            }
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
                    result.Add(CatmullRom(p0, p1, p2, p3, s / (float)steps));
                }
            }

            result.Add(points[points.Length - 1]);
            return result;
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                2f * p1 +
                (p2 - p0) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (3f * p1 - p0 - 3f * p2 + p3) * t3
            );
        }

        private static float DistanceToPolyline(Vector2 position, List<Vector2> samples)
        {
            float best = float.MaxValue;

            foreach (Vector2 samplePosition in samples)
            {
                float distance = Vector2.Distance(position, samplePosition);

                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private static float PathDistance(Vector2 position)
        {
            float best = float.MaxValue;

            foreach (PathSample sample in pathSamples)
            {
                float distance = Vector2.Distance(position, sample.Position);

                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private static Vector2 BrookTangentAt(Vector2 position)
        {
            int bestIndex = 0;
            float best = float.MaxValue;

            for (int i = 0; i < brookSamples.Count; i++)
            {
                float distance = Vector2.Distance(position, brookSamples[i]);

                if (distance < best)
                {
                    best = distance;
                    bestIndex = i;
                }
            }

            int nextIndex = Mathf.Min(bestIndex + 1, brookSamples.Count - 1);
            int previousIndex = Mathf.Max(bestIndex - 1, 0);
            return (brookSamples[nextIndex] - brookSamples[previousIndex]).normalized;
        }

        private static float GroundY(float x, float z)
        {
            return forestTerrain.SampleHeight(new Vector3(x, 0f, z)) +
                   forestTerrain.transform.position.y;
        }

        // ------------------------------------------------------------------
        // Natur-Pack: Modelle, Materialien, Platzierung
        // ------------------------------------------------------------------

        private static void PreparePackMaterials()
        {
            packMaterials = new Dictionary<string, Material>();

            AddPackMaterial("Bark_NormalTree", PackTextureFolder, "Bark_NormalTree", "Bark_NormalTree_Normal", cutout: false);
            AddPackMaterial("Bark_DeadTree", PackTextureFolder, "Bark_DeadTree", "Bark_DeadTree_Normal", cutout: false);
            AddPackMaterial("Bark_TwistedTree", PackTextureFolder, "Bark_TwistedTree", "Bark_TwistedTree_Normal", cutout: false);
            AddPackMaterial("Leaves_NormalTree", PackTextureFolder, "Leaves_NormalTree_C", null, cutout: true);
            AddPackMaterial("Leaves_TwistedTree", PackTextureFolder, "Leaves_TwistedTree_C", null, cutout: true);
            AddPackMaterial("Leaf_Pine", PackTextureFolder, "Leaf_Pine_C", null, cutout: true);
            AddPackMaterial("Leaves_Pine", PackTextureFolder, "Leaf_Pine_C", null, cutout: true);
            AddPackMaterial("Leaves", PackTextureFolder, "Leaves", null, cutout: true);
            AddPackMaterial("Grass", PackTextureFolder, "Grass", null, cutout: true);
            AddPackMaterial("Flowers", PackTextureFolder, "Flowers", null, cutout: true);
            AddPackMaterial("Mushrooms", PackTextureFolder, "Mushrooms", null, cutout: false);
            AddPackMaterial("Rocks", PackTextureFolder, "Rocks_Diffuse", null, cutout: false);
            AddPackMaterial("PathRocks", PackTextureFolder, "PathRocks_Diffuse", null, cutout: false);

            // Ultimate Stylized Nature: Praefix noetig, weil "Flowers" auch im
            // Natur Pack existiert, dort aber ein anderes UV-Layout hat.
            AddPackMaterial("USN:NormalTree_Bark", StylizedNatureTextureFolder, "NormalTree_Bark", "NormalTree_Bark_Normal", cutout: false);
            AddPackMaterial("USN:NormalTree_Leaves", StylizedNatureTextureFolder, "NormalTree_Leaves", null, cutout: true);
            AddPackMaterial("USN:BirchTree_Bark", StylizedNatureTextureFolder, "BirchTree_Bark", "BirchTree_Bark_Normal", cutout: false);
            AddPackMaterial("USN:BirchTree_Leaves", StylizedNatureTextureFolder, "BirchTree_Leaves", null, cutout: true);
            AddPackMaterial("USN:MapleTree_Bark", StylizedNatureTextureFolder, "MapleTree_Bark", "MapleTree_Bark_Normal", cutout: false);
            AddPackMaterial("USN:MapleTree_Leaves", StylizedNatureTextureFolder, "MapleTree_Leaves", null, cutout: true);
            AddPackMaterial("USN:PineTree_Bark", StylizedNatureTextureFolder, "PineTree_Bark", "PineTree_Bark_Normal", cutout: false);
            AddPackMaterial("USN:PineTree_Leaves", StylizedNatureTextureFolder, "PineTree_Leaves", null, cutout: true);
            AddPackMaterial("USN:Rock", StylizedNatureTextureFolder, "Rocks", null, cutout: false);
            AddPackMaterial("USN:Bush_Leaves", StylizedNatureTextureFolder, "Bush_Leaves", null, cutout: true);
            AddPackMaterial("USN:Flowers", StylizedNatureTextureFolder, "Flowers", null, cutout: true);

            // Textured Stylized Trees: "Bark" wird von Tree, Pine und DeadTree
            // gemeinsam genutzt; die Birken teilen sich "Birch_Bark".
            AddPackMaterial("TST:Bark", TexturedTreesTextureFolder, "Tree_Bark", null, cutout: false);
            AddPackMaterial("TST:Tree_Leaves", TexturedTreesTextureFolder, "Tree_Leaves", null, cutout: true);
            AddPackMaterial("TST:Birch_Bark", TexturedTreesTextureFolder, "Birch_Bark", null, cutout: false);
            AddPackMaterial("TST:Birch_Leaves", TexturedTreesTextureFolder, "Birch_Leaves_Green", null, cutout: true);
            AddPackMaterial("TST:Pine_Leaves", TexturedTreesTextureFolder, "Pine_Leaves", null, cutout: true);
        }

        private static void AddPackMaterial(
            string materialKey,
            string textureFolder,
            string textureName,
            string normalName,
            bool cutout
        )
        {
            Material material = LoadOrCreateMaterialAsset(
                $"Pack_{materialKey.Replace(":", "_")}", "Universal Render Pipeline/Lit"
            );
            material.SetColor("_BaseColor", Color.white);

            // Stylized-Flat-Look: praktisch kein Glanz, sonst wirken dunkle
            // Rinden im Sonnenlicht wie Chrom.
            material.SetFloat("_Smoothness", 0.05f);

            Texture2D baseTexture = LoadPackTexture(textureFolder, textureName, asNormalMap: false);
            if (baseTexture != null)
            {
                material.SetTexture("_BaseMap", baseTexture);

                if (cutout)
                {
                    EnsureAlphaIsTransparency(baseTexture);
                }
            }

            if (normalName != null)
            {
                Texture2D normalTexture = LoadPackTexture(textureFolder, normalName, asNormalMap: true);
                if (normalTexture != null)
                {
                    material.SetTexture("_BumpMap", normalTexture);
                    material.EnableKeyword("_NORMALMAP");
                }
            }

            if (cutout)
            {
                // Blattwerk bekommt den Wind-Shader (Cutout + Sway inklusive).
                material.shader = Shader.Find("Elyndor/FoliageWind");
                material.SetFloat("_Cutoff", 0.4f);
            }

            EditorUtility.SetDirty(material);
            packMaterials[materialKey] = material;
        }

        // Ohne alphaIsTransparency blutet der weisse Texturhintergrund als
        // heller Saum in die Blattkanten (Mipmap-Bleeding).
        private static void EnsureAlphaIsTransparency(Texture2D texture)
        {
            string texturePath = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);

            if (importer != null && !importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static Texture2D LoadPackTexture(string textureFolder, string textureName, bool asNormalMap)
        {
            string texturePath = null;

            foreach (string extension in new[] { "png", "jpg" })
            {
                string candidate = $"{textureFolder}/{textureName}.{extension}";

                if (AssetDatabase.LoadAssetAtPath<Texture2D>(candidate) != null)
                {
                    texturePath = candidate;
                    break;
                }
            }

            if (texturePath == null)
            {
                Debug.LogWarning($"Pack-Textur nicht gefunden: {textureFolder}/{textureName}");
                return null;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            if (asNormalMap)
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);

                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                }
            }

            return texture;
        }

        private static GameObject LoadModel(string modelName)
        {
            if (modelCache.TryGetValue(modelName, out GameObject cached))
            {
                return cached;
            }

            string modelPath = ResolveModelPath(modelName);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

            if (model == null)
            {
                throw new System.InvalidOperationException($"Modell nicht gefunden: {modelPath}");
            }

            modelCache[modelName] = model;
            return model;
        }

        private static string ResolveModelPath(string modelKey)
        {
            if (modelKey.StartsWith("USN:"))
            {
                return $"{StylizedNatureModelFolder}/{modelKey.Substring(4)}.fbx";
            }

            if (modelKey.StartsWith("TST:"))
            {
                return $"{TexturedTreesModelFolder}/{modelKey.Substring(4)}.fbx";
            }

            if (modelKey.StartsWith("TEMPLE:"))
            {
                return $"{TempleModelFolder}/{modelKey.Substring(7)}.obj";
            }

            return $"{PackModelFolder}/{modelKey}.fbx";
        }

        private static string PackPrefix(string modelKey)
        {
            int separatorIndex = modelKey.IndexOf(':');
            return separatorIndex < 0 ? "" : modelKey.Substring(0, separatorIndex + 1);
        }

        private static float GetModelHeight(string modelName)
        {
            if (modelHeightCache.TryGetValue(modelName, out float cachedHeight))
            {
                return cachedHeight;
            }

            GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel(modelName));
            float height = CalculateBounds(temp).size.y;
            Object.DestroyImmediate(temp);

            modelHeightCache[modelName] = height;
            return height;
        }

        // Normalisiert unbekannte Modellmaßstäbe auf eine Zielhöhe in Metern.
        private static float NormalizedScale(string modelName, float targetHeight)
        {
            float modelHeight = GetModelHeight(modelName);

            if (modelHeight < 0.01f)
            {
                return 1f;
            }

            return Mathf.Clamp(targetHeight / modelHeight, 0.05f, 50f);
        }

        // Fuer flache Teile (Bodenplatten, liegende Segmente): normalisiert
        // ueber die groesste Grundflaechen-Kante statt ueber die Hoehe.
        private static float NormalizedScaleByFootprint(string modelName, float targetFootprint)
        {
            GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel(modelName));
            Vector3 size = CalculateBounds(temp).size;
            Object.DestroyImmediate(temp);

            float footprint = Mathf.Max(size.x, size.z);

            if (footprint < 0.01f)
            {
                return 1f;
            }

            return Mathf.Clamp(targetFootprint / footprint, 0.05f, 50f);
        }

        private static void ApplyMaterialRecursively(GameObject root, Material material)
        {
            foreach (Renderer childRenderer in root.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = childRenderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                childRenderer.sharedMaterials = materials;
            }
        }

        // Weltachsen-orientierter Block-Collider ueber die Renderer-Bounds.
        private static void AddBoundsCollider(GameObject target)
        {
            Bounds bounds = CalculateBounds(target);

            GameObject blocker = new GameObject("Kollision");
            blocker.transform.SetParent(target.transform, true);
            blocker.transform.position = bounds.center;
            blocker.transform.rotation = Quaternion.identity;

            BoxCollider blockerCollider = blocker.AddComponent<BoxCollider>();
            blockerCollider.size = new Vector3(
                bounds.size.x / Mathf.Max(blocker.transform.lossyScale.x, 0.01f),
                bounds.size.y / Mathf.Max(blocker.transform.lossyScale.y, 0.01f),
                bounds.size.z / Mathf.Max(blocker.transform.lossyScale.z, 0.01f)
            );
        }

        private static void AddExaminable(
            GameObject host,
            string prompt,
            string examineText,
            float radius,
            float textDuration
        )
        {
            SphereCollider trigger = host.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
            trigger.center = new Vector3(0f, 1f, 0f);

            ExaminableObject examinable = host.AddComponent<ExaminableObject>();
            SerializedObject serializedExaminable = new SerializedObject(examinable);
            serializedExaminable.FindProperty("interactionPrompt").stringValue = prompt;
            serializedExaminable.FindProperty("examineText").stringValue = examineText;
            serializedExaminable.FindProperty("textDuration").floatValue = textDuration;
            serializedExaminable.ApplyModifiedPropertiesWithoutUndo();
        }

        // Welt-Bounds aus den Mesh-Eckpunkten. Renderer.bounds ist im Batch-Mode
        // direkt nach dem Instanziieren nicht zuverlaessig (liefert je nach FBX
        // lokale statt Welt-Bounds) — Mesh-Bounds + Transform sind es immer.
        private static Bounds CalculateBounds(GameObject target)
        {
            MeshFilter[] filters = target.GetComponentsInChildren<MeshFilter>();
            bool hasBounds = false;
            Bounds bounds = new Bounds(target.transform.position, Vector3.one);

            foreach (MeshFilter filter in filters)
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Bounds meshBounds = filter.sharedMesh.bounds;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = new Vector3(
                        (i & 1) == 0 ? meshBounds.min.x : meshBounds.max.x,
                        (i & 2) == 0 ? meshBounds.min.y : meshBounds.max.y,
                        (i & 4) == 0 ? meshBounds.min.z : meshBounds.max.z
                    );
                    Vector3 worldCorner = filter.transform.TransformPoint(corner);

                    if (!hasBounds)
                    {
                        bounds = new Bounds(worldCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(worldCorner);
                    }
                }
            }

            return bounds;
        }

        private static GameObject PlaceModel(
            string modelName,
            Transform parent,
            Vector2 worldXZ,
            float yRotation,
            float scale
        )
        {
            return PlaceModel(modelName, parent, worldXZ, Quaternion.Euler(0f, yRotation, 0f), scale);
        }

        private static GameObject PlaceModel(
            string modelName,
            Transform parent,
            Vector2 worldXZ,
            Quaternion rotation,
            float scale
        )
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel(modelName), parent);
            instance.transform.position = new Vector3(worldXZ.x, 0f, worldXZ.y);

            // Komponieren statt ueberschreiben: USN-/TST-FBX tragen auf dem
            // Prefab-Root ihre Achsen-Korrektur (-90 Grad X aus Blender) und
            // ihre Einheiten-Umrechnung als Scale — beides muss erhalten
            // bleiben, sonst liegen die Baeume flach bzw. schrumpfen.
            instance.transform.rotation = rotation * instance.transform.rotation;
            instance.transform.localScale *= scale;

            // Pivot-unabhängig auf den Boden setzen.
            Bounds bounds = CalculateBounds(instance);
            float groundY = GroundY(worldXZ.x, worldXZ.y);
            instance.transform.position += Vector3.up * (groundY - bounds.min.y);

            RemapMaterials(instance, PackPrefix(modelName));
            MarkStaticRecursively(instance);
            return instance;
        }

        private static void RemapMaterials(GameObject instance, string packPrefix)
        {
            // Tempel-Teile bekommen ihre Materialien explizit per
            // ApplyMaterialRecursively — Remap-Warnungen waeren nur Rauschen.
            if (packPrefix == "TEMPLE:")
            {
                return;
            }

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

                    // Erst pack-spezifisch suchen, dann global — so kollidieren
                    // gleichnamige Materialien verschiedener Packs nicht.
                    if (packMaterials.TryGetValue(packPrefix + materialName, out Material replacement) ||
                        packMaterials.TryGetValue(materialName, out replacement))
                    {
                        materials[i] = replacement;
                        changed = true;
                    }
                    else if (unmappedMaterialNames.Add(packPrefix + materialName))
                    {
                        Debug.LogWarning(
                            $"Kein Material-Mapping für '{materialName}' — importiertes Material bleibt aktiv."
                        );
                    }
                }

                if (changed)
                {
                    childRenderer.sharedMaterials = materials;
                }
            }
        }

        private static GameObject PlaceTree(
            string modelName,
            Transform parent,
            Vector2 position,
            float targetHeight,
            System.Random rng
        )
        {
            // Region "Uralte Baumriesen": rund um den Waechter waechst alles hoeher.
            float ancientDistance = Vector2.Distance(position, AncientTreePoint);
            if (ancientDistance < 13f)
            {
                targetHeight *= Mathf.Lerp(1.45f, 1.1f, ancientDistance / 13f);
            }

            float scale = NormalizedScale(modelName, targetHeight) *
                          Mathf.Lerp(0.85f, 1.25f, (float)rng.NextDouble());

            GameObject tree = PlaceModel(
                modelName, parent, position, (float)rng.NextDouble() * 360f, scale
            );

            AddTrunkCollider(tree, scale);
            placedTreePositions.Add(tree.transform.position);
            return tree;
        }

        private static void AddTrunkCollider(GameObject tree, float scale)
        {
            Bounds bounds = CalculateBounds(tree);

            // Eigenes, weltausgerichtetes Kind: Die FBX-Roots tragen Rotation
            // und grosse Scale-Faktoren — eine Kapsel direkt auf dem Root
            // waere gekippt und um den Root-Faktor zu gross.
            GameObject trunk = new GameObject("Stamm-Kollision");
            trunk.transform.SetParent(tree.transform, true);
            trunk.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            trunk.transform.rotation = Quaternion.identity;

            float worldScale = Mathf.Max(trunk.transform.lossyScale.y, 0.0001f);

            CapsuleCollider trunkCollider = trunk.AddComponent<CapsuleCollider>();
            trunkCollider.radius = 0.35f / worldScale;
            trunkCollider.height = bounds.size.y / worldScale;
            trunkCollider.center = new Vector3(0f, bounds.size.y * 0.5f / worldScale, 0f);
        }

        // ------------------------------------------------------------------
        // Bach, Querungen, Weltgrenzen
        // ------------------------------------------------------------------

        private static void BuildBrook(Transform parent)
        {
            GameObject brookRoot = new GameObject("Alter Bach");
            brookRoot.transform.SetParent(parent);

            Material waterMaterial = GetOrCreateWaterMaterial();

            for (int i = 0; i < brookSamples.Count; i += 3)
            {
                Vector2 samplePosition = brookSamples[i];

                if (Mathf.Abs(samplePosition.x) > PlayableRadius + 6f)
                {
                    continue;
                }

                Vector2 tangent = BrookTangentAt(samplePosition);
                float bedY = GroundY(samplePosition.x, samplePosition.y);

                GameObject waterSegment = GameObject.CreatePrimitive(PrimitiveType.Plane);
                waterSegment.name = "Wasser";
                waterSegment.transform.SetParent(brookRoot.transform);
                waterSegment.transform.position =
                    new Vector3(samplePosition.x, bedY + 0.35f, samplePosition.y);
                waterSegment.transform.rotation =
                    Quaternion.LookRotation(new Vector3(tangent.x, 0f, tangent.y));
                waterSegment.transform.localScale = new Vector3(0.55f, 1f, 0.45f);
                Object.DestroyImmediate(waterSegment.GetComponent<Collider>());
                waterSegment.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                ApplyMaterial(waterSegment, waterMaterial);
                MarkStatic(waterSegment);
            }

            BuildBrookBlockers(brookRoot.transform);
        }

        private static void BuildBrookBlockers(Transform parent)
        {
            GameObject blockerRoot = new GameObject("Bach-Sperren (temporaer)");
            blockerRoot.transform.SetParent(parent);

            for (int i = 0; i < brookSamples.Count; i += 4)
            {
                Vector2 samplePosition = brookSamples[i];

                if (Mathf.Abs(samplePosition.x) > PlayableRadius + 4f)
                {
                    continue;
                }

                // Querungen offen lassen: Baumstamm und Furt.
                if (Vector2.Distance(samplePosition, LogCrossingPoint) < 7f ||
                    Vector2.Distance(samplePosition, FordPoint) < 7f)
                {
                    continue;
                }

                Vector2 tangent = BrookTangentAt(samplePosition);
                float bedY = GroundY(samplePosition.x, samplePosition.y);

                GameObject blocker = new GameObject("Sperre");
                blocker.transform.SetParent(blockerRoot.transform);
                blocker.transform.position =
                    new Vector3(samplePosition.x, bedY + 1.4f, samplePosition.y);
                blocker.transform.rotation =
                    Quaternion.LookRotation(new Vector3(tangent.x, 0f, tangent.y));

                BoxCollider blockerCollider = blocker.AddComponent<BoxCollider>();
                blockerCollider.size = new Vector3(4.4f, 3f, 5f);
            }
        }

        private static void BuildLogCrossing(Transform parent)
        {
            GameObject crossing = new GameObject("Baumstamm-Querung");
            crossing.transform.SetParent(parent);

            Vector2 tangent = BrookTangentAt(LogCrossingPoint);
            Vector2 across = new Vector2(-tangent.y, tangent.x);
            Vector3 acrossDirection = new Vector3(across.x, 0f, across.y);

            float bankY = (
                GroundY(LogCrossingPoint.x, LogCrossingPoint.y - BrookHalfWidth - 1f) +
                GroundY(LogCrossingPoint.x, LogCrossingPoint.y + BrookHalfWidth + 1f)
            ) * 0.5f;

            Material woodMaterial = GetOrCreateMaterial("Proto_Wood", WoodColor);

            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "Umgestuerzter Stamm";
            log.transform.SetParent(crossing.transform);
            log.transform.position = new Vector3(LogCrossingPoint.x, bankY - 0.1f, LogCrossingPoint.y);
            log.transform.rotation = Quaternion.FromToRotation(Vector3.up, acrossDirection);
            log.transform.localScale = new Vector3(0.7f, 5.6f, 0.7f);
            ApplyMaterial(log, woodMaterial);
            MarkStatic(log);

            // Flacher Lauf-Collider auf dem Stamm, damit die Querung zuverlässig ist.
            GameObject walkway = new GameObject("Stamm-Laufflaeche");
            walkway.transform.SetParent(crossing.transform);
            walkway.transform.position = new Vector3(LogCrossingPoint.x, bankY + 0.1f, LogCrossingPoint.y);
            walkway.transform.rotation = Quaternion.LookRotation(acrossDirection);

            BoxCollider walkwayCollider = walkway.AddComponent<BoxCollider>();
            walkwayCollider.size = new Vector3(1.2f, 0.25f, 10.8f);
        }

        private static void BuildFord(Transform parent)
        {
            GameObject ford = new GameObject("Furt");
            ford.transform.SetParent(parent);

            System.Random rng = new System.Random(19);
            string[] plateModels =
            {
                "RockPath_Round_Small_1", "RockPath_Round_Small_2",
                "RockPath_Round_Small_3", "RockPath_Round_Wide"
            };

            for (int i = 0; i < 5; i++)
            {
                Vector2 position = FordPoint + new Vector2(
                    Mathf.Lerp(-1.4f, 1.4f, (float)rng.NextDouble()),
                    Mathf.Lerp(-2.4f, 2.4f, i / 4f)
                );

                string model = plateModels[rng.Next(plateModels.Length)];
                float scale = NormalizedScale(model, 0.18f);
                GameObject plate = PlaceModel(
                    model, ford.transform, position, (float)rng.NextDouble() * 360f, scale
                );

                // Trittsteine ragen aus dem flachen Wasser heraus.
                plate.transform.position += Vector3.up * 0.25f;
            }
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

            BoxCollider wallCollider = wall.AddComponent<BoxCollider>();
            wallCollider.size = size;
        }

        // ------------------------------------------------------------------
        // Zonen
        // ------------------------------------------------------------------

        private static GameObject PreparePlayer()
        {
            PlayerMovement playerMovement = Object.FindAnyObjectByType<PlayerMovement>();

            if (playerMovement == null)
            {
                throw new System.InvalidOperationException(
                    "Kein PlayerMovement in der Szene gefunden — Testszene braucht den Player aus Bootstrap."
                );
            }

            GameObject player = playerMovement.gameObject;
            player.transform.localScale = Vector3.one * PlayerScale;

            if (player.GetComponent<InteractionDetector>() == null)
            {
                player.AddComponent<InteractionDetector>();
            }

            player.transform.position = new Vector3(
                StartPoint.x, GroundY(StartPoint.x, StartPoint.y - 2f) + 1.2f, StartPoint.y - 2f
            );

            AttachPlayerSilhouette(player);

            if (player.GetComponent<Elyndor.Combat.PlayerCombat>() == null)
            {
                player.AddComponent<Elyndor.Combat.PlayerCombat>();
            }

            ConfigureMainCamera();

            return player;
        }

        // SMAA glaettet die Blattkanten, die MSAA allein nicht erwischt.
        internal static void ConfigureMainCamera()
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                return;
            }

            UniversalAdditionalCameraData cameraData =
                mainCamera.GetUniversalAdditionalCameraData();
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            cameraData.renderPostProcessing = true;
        }

        // Silhouetten-Material, damit Aren unter Baumkronen sichtbar bleibt.
        internal static void AttachPlayerSilhouette(GameObject player)
        {
            Material silhouette = LoadOrCreateMaterialAsset(
                "Elyndor_PlayerSilhouette", "Elyndor/OccludedSilhouette"
            );
            silhouette.SetColor("_SilhouetteColor", new Color(0.45f, 0.75f, 0.95f, 0.4f));
            EditorUtility.SetDirty(silhouette);

            PlayerSilhouette playerSilhouette = player.GetComponent<PlayerSilhouette>();

            if (playerSilhouette == null)
            {
                playerSilhouette = player.AddComponent<PlayerSilhouette>();
            }

            SerializedObject serializedSilhouette = new SerializedObject(playerSilhouette);
            serializedSilhouette.FindProperty("silhouetteMaterial").objectReferenceValue = silhouette;
            serializedSilhouette.ApplyModifiedPropertiesWithoutUndo();
        }

        // Übergang zu den Sonnenfeldern: östlich der Felsformation öffnet
        // sich der Wald. Rückkehr-Spawn direkt am Portalpfad.
        private static void BuildRegionConnections(Transform parent)
        {
            GameObject connections = new GameObject("Regions-Uebergaenge");
            connections.transform.SetParent(parent);

            Vector2 portalPoint = new Vector2(58f, -26f);
            float portalY = GroundY(portalPoint.x, portalPoint.y);

            GameObject portal = new GameObject("Portal_Sonnenfelder");
            portal.transform.SetParent(connections.transform);
            portal.transform.position = new Vector3(portalPoint.x, portalY, portalPoint.y);

            BoxCollider trigger = portal.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3f, 4f, 8f);
            trigger.center = new Vector3(1.5f, 2f, 0f);

            Elyndor.World.RegionPortal regionPortal = portal.AddComponent<Elyndor.World.RegionPortal>();
            SerializedObject serializedPortal = new SerializedObject(regionPortal);
            serializedPortal.FindProperty("targetSceneName").stringValue = "Sonnenfelder";
            serializedPortal.FindProperty("targetSpawnId").stringValue = "von_wald";
            serializedPortal.ApplyModifiedPropertiesWithoutUndo();

            GameObject spawn = new GameObject("Spawn_VonSonnenfeldern");
            spawn.transform.SetParent(connections.transform);
            spawn.transform.position = new Vector3(portalPoint.x - 3f, portalY + 1.1f, portalPoint.y);

            Elyndor.World.RegionSpawnPoint spawnPoint = spawn.AddComponent<Elyndor.World.RegionSpawnPoint>();
            SerializedObject serializedSpawn = new SerializedObject(spawnPoint);
            serializedSpawn.FindProperty("spawnId").stringValue = "von_sonnenfeldern";
            serializedSpawn.ApplyModifiedPropertiesWithoutUndo();

            BuildSign(
                connections.transform,
                portalPoint + new Vector2(-2.5f, 4f),
                "Wegweiser lesen",
                "„Nach Osten: Die Sonnenfelder.“\n" +
                "Jemand hat mit Kohle daruntergeschrieben: „Die Muehle steht " +
                "nicht mehr. Aber die Felder erinnern sich an den Wind.“"
            );
        }

        private static void BuildStartArea(Transform parent)
        {
            GameObject startArea = new GameObject("Startbereich");
            startArea.transform.SetParent(parent);

            BuildSign(
                startArea.transform,
                new Vector2(2f, -50f),
                "Schild lesen",
                "„Finsterwald — Noerdlicher Pfad.“\n" +
                "Darunter, kaum lesbar: „Zur alten Bruecke. " +
                "Moege der Fluss sich erinnern.“"
            );
        }

        private static void BuildClearing(Transform parent)
        {
            GameObject clearing = new GameObject("Kleine Lichtung");
            clearing.transform.SetParent(parent);

            System.Random rng = new System.Random(23);

            // Landmarke: der grosse alte Baum in der Mitte der Lichtung.
            string bigTreeModel = "TwistedTree_1";
            float bigTreeScale = NormalizedScale(bigTreeModel, 13f);
            GameObject bigTree = PlaceModel(bigTreeModel, clearing.transform, ClearingPoint, 140f, bigTreeScale);
            AddTrunkCollider(bigTree, bigTreeScale);
            accentTreePositions.Add(bigTree.transform.position);

            // Blumen und Klee sammeln sich im Licht.
            for (int i = 0; i < 10; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(2.5f, 7.5f, (float)rng.NextDouble());
                Vector2 position = ClearingPoint + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                string model = rng.Next(2) == 0 ? "Flower_3_Group" : "Flower_4_Group";
                PlaceModel(model, clearing.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, 0.45f));
            }

            for (int i = 0; i < 8; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(2f, 8f, (float)rng.NextDouble());
                Vector2 position = ClearingPoint + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                string model = rng.Next(2) == 0 ? "Clover_1" : "Clover_2";
                PlaceModel(model, clearing.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, 0.2f));
            }

            // Bunte Blumeninseln aus dem Stylized-Nature-Pack.
            for (int i = 0; i < 6; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(3f, 8f, (float)rng.NextDouble());
                Vector2 position = ClearingPoint + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                string model = $"USN:Flower_{1 + rng.Next(5)}_Clump";
                PlaceModel(model, clearing.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, 0.5f));
            }
        }

        private static MemoryEcho BuildBridgeArea(Transform parent)
        {
            GameObject bridgeArea = new GameObject("Zerstoerte Bruecke");
            bridgeArea.transform.SetParent(parent);

            Vector2 tangent = BrookTangentAt(BridgePoint);
            Vector2 across = new Vector2(-tangent.y, tangent.x);
            Quaternion bridgeRotation =
                Quaternion.LookRotation(new Vector3(across.x, 0f, across.y));

            float southBankY = GroundY(BridgePoint.x, BridgePoint.y - BrookHalfWidth - 1.5f);
            float northBankY = GroundY(BridgePoint.x, BridgePoint.y + BrookHalfWidth + 1.5f);
            float bankY = (southBankY + northBankY) * 0.5f;

            Material woodMaterial = GetOrCreateMaterial("Proto_Wood", WoodColor);

            // Brueckenreste an beiden Ufern.
            BuildBridgeStump(bridgeArea.transform,
                BridgePoint - across * (BrookHalfWidth + 0.6f), bridgeRotation, southBankY, woodMaterial);
            BuildBridgeStump(bridgeArea.transform,
                BridgePoint + across * (BrookHalfWidth + 0.6f), bridgeRotation, northBankY, woodMaterial);

            // Geister-Bruecke (Erinnerung).
            GameObject ghostBridge = new GameObject("GhostBridge");
            ghostBridge.transform.SetParent(bridgeArea.transform);
            ghostBridge.transform.position = new Vector3(BridgePoint.x, bankY, BridgePoint.y);
            ghostBridge.transform.rotation = bridgeRotation;

            GameObject visualRoot = new GameObject("GhostBridge_Visual");
            visualRoot.transform.SetParent(ghostBridge.transform);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;

            Material ghostMaterial = GetOrCreateGhostMaterial();
            float deckLength = BrookHalfWidth * 2f + 2.5f;

            BuildGhostPart(visualRoot.transform, "Brueckendeck",
                new Vector3(0f, 0.55f, 0f), new Vector3(2.2f, 0.15f, deckLength), ghostMaterial);
            BuildGhostPart(visualRoot.transform, "Gelaender_Links",
                new Vector3(-1f, 1.0f, 0f), new Vector3(0.12f, 0.8f, deckLength), ghostMaterial);
            BuildGhostPart(visualRoot.transform, "Gelaender_Rechts",
                new Vector3(1f, 1.0f, 0f), new Vector3(0.12f, 0.8f, deckLength), ghostMaterial);

            MemoryEcho echo = ghostBridge.AddComponent<MemoryEcho>();
            SerializedObject serializedEcho = new SerializedObject(echo);
            serializedEcho.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            serializedEcho.FindProperty("fadeDuration").floatValue = 2.5f;
            serializedEcho.ApplyModifiedPropertiesWithoutUndo();

            BuildMemorySite(bridgeArea.transform, echo);
            return echo;
        }

        private static void BuildBridgeStump(
            Transform parent,
            Vector2 position,
            Quaternion rotation,
            float bankY,
            Material material
        )
        {
            GameObject stump = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stump.name = "Brueckenrest";
            stump.transform.SetParent(parent);
            stump.transform.position = new Vector3(position.x, bankY + 0.25f, position.y);
            stump.transform.rotation = rotation;
            stump.transform.localScale = new Vector3(2.4f, 0.5f, 0.8f);
            ApplyMaterial(stump, material);
            MarkStatic(stump);
        }

        private static void BuildMemorySite(Transform parent, MemoryEcho ghostBridgeEcho)
        {
            GameObject site = new GameObject("MemorySite_AlteBruecke");
            site.transform.SetParent(parent);
            site.transform.position = new Vector3(
                MemorySitePoint.x, GroundY(MemorySitePoint.x, MemorySitePoint.y), MemorySitePoint.y
            );

            System.Random rng = new System.Random(11);

            // Ring aus alten Steinen.
            const int stoneCount = 6;
            for (int i = 0; i < stoneCount; i++)
            {
                float angle = i * Mathf.PI * 2f / stoneCount;
                Vector2 position = MemorySitePoint +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.5f;

                string model = $"Pebble_Round_{1 + i % 5}";
                PlaceModel(model, site.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, 0.4f));
            }

            // Stille Blumen um den Ort — als haette die Natur ihn nie vergessen.
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f + 0.5f;
                Vector2 position = MemorySitePoint +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2.4f;

                string model = i % 2 == 0 ? "Flower_3_Single" : "Flower_4_Single";
                PlaceModel(model, site.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, 0.35f));
            }

            SphereCollider trigger = site.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 3f;
            trigger.center = new Vector3(0f, 1f, 0f);

            MemorySite memorySite = site.AddComponent<MemorySite>();

            SerializedObject serializedSite = new SerializedObject(memorySite);
            serializedSite.FindProperty("interactionPrompt").stringValue = "Memory Watch verwenden";
            serializedSite.FindProperty("siteId").stringValue = "finsterwald_bruecke_01";
            serializedSite.FindProperty("activationDuration").floatValue = 2.5f;
            serializedSite.FindProperty("memoryText").stringValue =
                "Die Memory Watch surrt leise. Fuer einen Moment traegt der Fluss " +
                "wieder eine Bruecke — Bohlen, Gelaender, Schritte von Menschen, " +
                "die niemand mehr kennt. Die Erinnerung bleibt, auch wenn die Welt " +
                "sie vergessen hat.";
            serializedSite.FindProperty("memoryTextDuration").floatValue = 8f;

            SerializedProperty echoesProperty = serializedSite.FindProperty("echoes");
            echoesProperty.arraySize = 1;
            echoesProperty.GetArrayElementAtIndex(0).objectReferenceValue = ghostBridgeEcho;

            serializedSite.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildStoneCircle(Transform parent)
        {
            GameObject circle = new GameObject("Steinkreis");
            circle.transform.SetParent(parent);
            circle.transform.position = new Vector3(
                StoneCirclePoint.x, GroundY(StoneCirclePoint.x, StoneCirclePoint.y), StoneCirclePoint.y
            );

            System.Random rng = new System.Random(29);

            const int stoneCount = 7;
            for (int i = 0; i < stoneCount; i++)
            {
                float angle = i * Mathf.PI * 2f / stoneCount;
                Vector2 position = StoneCirclePoint +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3.3f;

                string model = $"Rock_Medium_{1 + i % 3}";
                float scale = NormalizedScale(model, Mathf.Lerp(1.6f, 2.2f, (float)rng.NextDouble()));
                PlaceModel(model, circle.transform, position, (float)rng.NextDouble() * 360f, scale);
            }

            string plateModel = "RockPath_Square_Wide";
            PlaceModel(plateModel, circle.transform, StoneCirclePoint, 15f,
                NormalizedScale(plateModel, 0.22f));

            SphereCollider trigger = circle.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 4f;
            trigger.center = new Vector3(0f, 1f, 0f);

            ExaminableObject examinable = circle.AddComponent<ExaminableObject>();
            SerializedObject serializedExaminable = new SerializedObject(examinable);
            serializedExaminable.FindProperty("interactionPrompt").stringValue = "Steinkreis betrachten";
            serializedExaminable.FindProperty("examineText").stringValue =
                "Sieben Steine, aelter als jeder Weg. In den Flechten glaubt man " +
                "Muster zu erkennen — als haette hier jemand etwas festhalten wollen, " +
                "lange bevor es Worte dafuer gab.";
            serializedExaminable.FindProperty("textDuration").floatValue = 6f;
            serializedExaminable.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildRuin(Transform parent)
        {
            GameObject ruin = new GameObject("Kleine Ruine");
            ruin.transform.SetParent(parent);
            float ruinY = GroundY(RuinPoint.x, RuinPoint.y);
            ruin.transform.position = new Vector3(RuinPoint.x, ruinY, RuinPoint.y);

            Material stoneMaterial = GetOrCreateMaterial("Proto_Stone", StoneColor);

            // Halb eingestuerzte Mauerzuege aus dem Tempel-Kit — der Wald
            // waechst mitten hindurch.
            PlaceRuinPiece("TEMPLE:Wall_Ruined_Full_1", ruin.transform,
                RuinPoint + new Vector2(-1.2f, 3.2f), 0f, 2.6f, MossyStoneMaterial, withCollider: true);
            PlaceRuinPiece("TEMPLE:Wall_Ruined_Full_Cracked_1", ruin.transform,
                RuinPoint + new Vector2(1.5f, 3.1f), 4f, 2.6f, stoneMaterial, withCollider: true);
            PlaceRuinPiece("TEMPLE:Wall_Ruined_Half_1", ruin.transform,
                RuinPoint + new Vector2(3.1f, 1.2f), 92f, 2.4f, MossyStoneMaterial, withCollider: true);
            PlaceRuinPiece("TEMPLE:Wall_Ruined_Full_2", ruin.transform,
                RuinPoint + new Vector2(-2.9f, 1.4f), 88f, 2.5f, stoneMaterial, withCollider: true);
            PlaceRuinPiece("TEMPLE:Wall_Ruined_Half_2", ruin.transform,
                RuinPoint + new Vector2(-1.8f, -2.6f), 12f, 2.2f, MossyStoneMaterial, withCollider: true);

            // Halb versunkene Bodenplatten im Inneren.
            for (int i = 0; i < 3; i++)
            {
                Vector2 platePosition = RuinPoint + new Vector2(-1f + i * 1.1f, 0.4f + (i % 2) * 0.9f);
                GameObject plate = PlaceRuinPiece(
                    i == 1 ? "TEMPLE:Floor_Ruined_Corner_Outer_1" : $"TEMPLE:Floor_Ruined_Straight_{1 + i % 2}",
                    ruin.transform, platePosition, 7f + i * 84f, 2.1f, stoneMaterial, withCollider: false
                );
                plate.transform.position += Vector3.down * 0.1f;
            }

            // Ein stehender und ein gestuerzter Pfeiler.
            GameObject standingPillar = PlaceModel("TEMPLE:Pillar_Large_Base", ruin.transform,
                RuinPoint + new Vector2(2.2f, -1.6f), 24f, NormalizedScale("TEMPLE:Pillar_Large_Base", 1.9f));
            ApplyMaterialRecursively(standingPillar, MossyStoneMaterial);
            AddBoundsCollider(standingPillar);

            GameObject fallenPillar = PlaceModel("TEMPLE:Pillar_Large_Middle", ruin.transform,
                RuinPoint + new Vector2(0.4f, -1.5f), Quaternion.Euler(88f, 140f, 0f),
                NormalizedScale("TEMPLE:Pillar_Large_Middle", 2.3f));
            ApplyMaterialRecursively(fallenPillar, stoneMaterial);

            System.Random rng = new System.Random(31);

            // Die Natur holt sich den Ort zurueck.
            GameObject ruinTree = PlaceModel("TwistedTree_3", ruin.transform,
                RuinPoint + new Vector2(1.6f, 1.2f), 40f, NormalizedScale("TwistedTree_3", 6f));
            AddTrunkCollider(ruinTree, NormalizedScale("TwistedTree_3", 6f));

            for (int i = 0; i < 4; i++)
            {
                Vector2 position = RuinPoint + new Vector2(
                    Mathf.Lerp(-3f, 3f, (float)rng.NextDouble()),
                    Mathf.Lerp(-3f, 3f, (float)rng.NextDouble())
                );
                PlaceModel("Fern_1", ruin.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale("Fern_1", 0.7f));
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 position = RuinPoint + new Vector2(
                    Mathf.Lerp(-2.5f, 2.5f, (float)rng.NextDouble()),
                    Mathf.Lerp(-2.5f, 2.5f, (float)rng.NextDouble())
                );
                string model = rng.Next(2) == 0 ? "Mushroom_Common" : "Mushroom_Laetiporus";
                PlaceModel(model, ruin.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, 0.3f));
            }

            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2.2f;
                Vector2 position = RuinPoint +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 5f;
                string model = $"Rock_Medium_{1 + i}";
                PlaceModel(model, ruin.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, 1.2f));
            }

            // Verwitterte Inschrift als untersuchbares Detail.
            GameObject tablet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tablet.name = "Inschriftstein";
            tablet.transform.SetParent(ruin.transform);
            tablet.transform.localPosition = new Vector3(0.4f, 0.45f, 2.6f);
            tablet.transform.localRotation = Quaternion.Euler(-8f, 6f, 2f);
            tablet.transform.localScale = new Vector3(0.7f, 0.9f, 0.18f);
            ApplyMaterial(tablet, stoneMaterial);
            MarkStatic(tablet);

            GameObject inscription = new GameObject("Verwitterte Inschrift");
            inscription.transform.SetParent(ruin.transform);
            inscription.transform.localPosition = tablet.transform.localPosition;

            SphereCollider trigger = inscription.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.2f;

            ExaminableObject examinable = inscription.AddComponent<ExaminableObject>();
            SerializedObject serializedExaminable = new SerializedObject(examinable);
            serializedExaminable.FindProperty("interactionPrompt").stringValue = "Inschrift entziffern";
            serializedExaminable.FindProperty("examineText").stringValue =
                "Der Stein ist verwittert, nur Bruchstuecke bleiben: „...als der Fluss " +
                "noch einen Namen trug...“ — „...wer vergisst, verliert mehr als " +
                "Worte...“ Der Rest ist fort. Wie so vieles hier.";
            serializedExaminable.FindProperty("textDuration").floatValue = 7f;
            serializedExaminable.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildViewpoint(Transform parent)
        {
            GameObject viewpoint = new GameObject("Aussichtspunkt");
            viewpoint.transform.SetParent(parent);
            viewpoint.transform.position = new Vector3(
                ViewpointPoint.x, GroundY(ViewpointPoint.x, ViewpointPoint.y), ViewpointPoint.y
            );

            System.Random rng = new System.Random(37);

            // Ein toter Baum als Silhouette gegen den Himmel — von weitem sichtbar.
            string silhouetteModel = "DeadTree_1";
            float silhouetteScale = NormalizedScale(silhouetteModel, 9f);
            Vector2 silhouettePosition = ViewpointPoint + new Vector2(3.5f, 2f);
            GameObject silhouette = PlaceModel(
                silhouetteModel, viewpoint.transform, silhouettePosition, 210f, silhouetteScale
            );
            AddTrunkCollider(silhouette, silhouetteScale);
            accentTreePositions.Add(silhouette.transform.position);

            for (int i = 0; i < 2; i++)
            {
                Vector2 position = ViewpointPoint + new Vector2(-2.5f + i * 1.5f, -2f - i);
                string model = $"Rock_Medium_{1 + i}";
                PlaceModel(model, viewpoint.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, 1.1f));
            }

            BuildSign(
                viewpoint.transform,
                ViewpointPoint + new Vector2(-1f, 1f),
                "Aussicht betrachten",
                "Von hier oben liegt der Finsterwald still unter dem Nebel. " +
                "Der Bach zieht silbern nach Westen — und irgendwo dort unten " +
                "wartet eine Bruecke darauf, erinnert zu werden."
            );
        }

        private static void BuildSign(
            Transform parent,
            Vector2 position,
            string prompt,
            string examineText
        )
        {
            GameObject sign = new GameObject("Wegschild");
            sign.transform.SetParent(parent);
            sign.transform.position = new Vector3(position.x, GroundY(position.x, position.y), position.y);

            Material woodMaterial = GetOrCreateMaterial("Proto_Wood", WoodColor);

            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Pfosten";
            post.transform.SetParent(sign.transform);
            post.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            post.transform.localScale = new Vector3(0.12f, 0.6f, 0.12f);
            ApplyMaterial(post, woodMaterial);
            MarkStatic(post);

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Tafel";
            board.transform.SetParent(sign.transform);
            board.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            board.transform.localScale = new Vector3(1.1f, 0.5f, 0.08f);
            board.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            ApplyMaterial(board, woodMaterial);
            MarkStatic(board);

            SphereCollider trigger = sign.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.2f;
            trigger.center = new Vector3(0f, 1f, 0f);

            ExaminableObject examinable = sign.AddComponent<ExaminableObject>();
            SerializedObject serializedExaminable = new SerializedObject(examinable);
            serializedExaminable.FindProperty("interactionPrompt").stringValue = prompt;
            serializedExaminable.FindProperty("examineText").stringValue = examineText;
            serializedExaminable.FindProperty("textDuration").floatValue = 6f;
            serializedExaminable.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildWaymarkers(Transform parent)
        {
            GameObject waymarkers = new GameObject("Wegmarkierungen");
            waymarkers.transform.SetParent(parent);

            BuildWaymarker(waymarkers.transform, new Vector2(2f, -20.5f));
            BuildWaymarker(waymarkers.transform, new Vector2(8f, -9.5f));
            BuildWaymarker(waymarkers.transform, new Vector2(31f, -3.5f));
            BuildWaymarker(waymarkers.transform, new Vector2(35f, 9f));

            // Landmarke: markanter Fels an der Weggabelung.
            string rockModel = "Rock_Medium_2";
            PlaceModel(rockModel, waymarkers.transform, ForkRockPoint, 75f,
                NormalizedScale(rockModel, 3.6f));
        }

        private static void BuildWaymarker(Transform parent, Vector2 position)
        {
            GameObject waymarker = new GameObject("Alte Wegmarkierung");
            waymarker.transform.SetParent(parent);
            waymarker.transform.position = new Vector3(position.x, GroundY(position.x, position.y), position.y);
            waymarker.transform.rotation = Quaternion.Euler(3f, position.x * 13f % 360f, -5f);

            Material stoneMaterial = GetOrCreateMaterial("Proto_Stone", StoneColor);
            Material markMaterial = GetOrCreateMaterial("Proto_FadedMark", FadedMarkColor);

            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Steinpfeiler";
            pillar.transform.SetParent(waymarker.transform);
            pillar.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            pillar.transform.localScale = new Vector3(0.32f, 1.05f, 0.32f);
            ApplyMaterial(pillar, stoneMaterial);
            MarkStatic(pillar);

            GameObject fadedBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fadedBand.name = "Verblasste Markierung";
            fadedBand.transform.SetParent(waymarker.transform);
            fadedBand.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            fadedBand.transform.localScale = new Vector3(0.34f, 0.1f, 0.34f);
            Object.DestroyImmediate(fadedBand.GetComponent<Collider>());
            ApplyMaterial(fadedBand, markMaterial);
            MarkStatic(fadedBand);
        }

        private static void BuildGhostPart(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 scale,
            Material material
        )
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = scale;

            // Erinnerungen sind immateriell: keine Kollision, keine Schatten.
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            ApplyMaterial(part, material);
        }

        // ------------------------------------------------------------------
        // Environmental Storytelling (M2.9)
        // ------------------------------------------------------------------

        private static Material MossyStoneMaterial =>
            GetOrCreateMaterial("Proto_MossyStone", new Color(0.36f, 0.42f, 0.34f));

        private static GameObject PlaceRuinPiece(
            string model,
            Transform parent,
            Vector2 position,
            float yRotation,
            float footprint,
            Material material,
            bool withCollider
        )
        {
            GameObject piece = PlaceModel(
                model, parent, position, yRotation, NormalizedScaleByFootprint(model, footprint)
            );
            ApplyMaterialRecursively(piece, material);

            if (withCollider)
            {
                AddBoundsCollider(piece);
            }

            return piece;
        }

        // Region "Uralte Baumriesen": ein Waechterbaum, um den herum der ganze
        // Bestand hoeher waechst (siehe PlaceTree).
        private static void BuildAncientTree(Transform parent)
        {
            GameObject ancient = new GameObject("Uralter Waechter");
            ancient.transform.SetParent(parent);
            ancient.transform.position = new Vector3(
                AncientTreePoint.x, GroundY(AncientTreePoint.x, AncientTreePoint.y), AncientTreePoint.y
            );

            string model = "TwistedTree_2";
            float scale = NormalizedScale(model, 19f);
            GameObject tree = PlaceModel(model, ancient.transform, AncientTreePoint, 250f, scale);
            AddTrunkCollider(tree, scale);
            accentTreePositions.Add(tree.transform.position);

            // Freiliegendes Wurzelwerk: flache Steinplatten und Farne am Fuss.
            System.Random rng = new System.Random(41);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f + 0.4f;
                Vector2 position = AncientTreePoint +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Lerp(2.2f, 3.8f, (float)rng.NextDouble());

                string detail = i % 2 == 0 ? "Fern_1" : $"Pebble_Square_{1 + i % 6}";
                PlaceModel(detail, ancient.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(detail, i % 2 == 0 ? 0.8f : 0.35f));
            }

            AddExaminable(
                ancient,
                "Den alten Baum betrachten",
                "Sein Stamm ist breiter als jedes Tor, die Rinde voller Narben, " +
                "die niemand mehr deuten kann. Die Baeume ringsum wachsen ihm " +
                "entgegen, als suchten sie seine Naehe. Wer unter ihm steht, " +
                "spricht unwillkuerlich leiser.",
                4f, 8f
            );
        }

        // Verlassener Rastplatz nahe des Hauptwegs: kalte Feuerstelle,
        // Sitzstaemme, bereitgelegtes Holz — aber niemand kam zurueck.
        private static void BuildRestSite(Transform parent)
        {
            GameObject site = new GameObject("Verlassener Rastplatz");
            site.transform.SetParent(parent);
            site.transform.position = new Vector3(
                RestSitePoint.x, GroundY(RestSitePoint.x, RestSitePoint.y), RestSitePoint.y
            );

            System.Random rng = new System.Random(53);
            Material charred = GetOrCreateMaterial("Proto_Charred", new Color(0.13f, 0.12f, 0.11f));
            Material wood = GetOrCreateMaterial("Proto_Wood", WoodColor);

            // Steinring der Feuerstelle.
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7f;
                Vector2 position = RestSitePoint + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.8f;

                string model = $"Pebble_Round_{1 + i % 5}";
                PlaceModel(model, site.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, 0.28f));
            }

            // Verkohlter Rest in der Mitte.
            GameObject ember = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ember.name = "Kalte Asche";
            ember.transform.SetParent(site.transform);
            ember.transform.position = site.transform.position + Vector3.up * 0.06f;
            ember.transform.localScale = new Vector3(0.55f, 0.06f, 0.55f);
            Object.DestroyImmediate(ember.GetComponent<Collider>());
            ApplyMaterial(ember, charred);
            MarkStatic(ember);

            // Zwei Sitzstaemme am Feuer.
            for (int i = 0; i < 2; i++)
            {
                GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                log.name = "Sitzstamm";
                log.transform.SetParent(site.transform);

                float angle = 1.1f + i * 2.4f;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.7f;
                log.transform.position = new Vector3(
                    RestSitePoint.x + offset.x,
                    GroundY(RestSitePoint.x + offset.x, RestSitePoint.y + offset.y) + 0.22f,
                    RestSitePoint.y + offset.y
                );
                log.transform.rotation = Quaternion.Euler(90f, angle * Mathf.Rad2Deg + 90f, 0f);
                log.transform.localScale = new Vector3(0.42f, 0.8f, 0.42f);
                ApplyMaterial(log, wood);
                MarkStatic(log);
            }

            // Bereitgelegtes, nie verbranntes Feuerholz.
            for (int i = 0; i < 3; i++)
            {
                GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = "Feuerholz";
                branch.transform.SetParent(site.transform);
                branch.transform.position = new Vector3(
                    RestSitePoint.x - 1.3f + i * 0.14f,
                    GroundY(RestSitePoint.x - 1.3f, RestSitePoint.y + 1.5f) + 0.1f + i * 0.13f,
                    RestSitePoint.y + 1.5f
                );
                branch.transform.rotation = Quaternion.Euler(90f, 12f + i * 9f, 0f);
                branch.transform.localScale = new Vector3(0.12f, 0.5f, 0.12f);
                Object.DestroyImmediate(branch.GetComponent<Collider>());
                ApplyMaterial(branch, wood);
                MarkStatic(branch);
            }

            accentTreePositions.Add(site.transform.position);

            AddExaminable(
                site,
                "Rastplatz untersuchen",
                "Kalte Asche, ordentlich geschichtetes Feuerholz, zwei Sitzstaemme. " +
                "Wer hier rastete, wollte wiederkommen — das Holz liegt noch bereit. " +
                "Es wurde nie verbrannt.",
                3f, 8f
            );
        }

        // Kleiner Schrein am versteckten Pfad: eine Nische im Stein.
        private static void BuildShrine(Transform parent)
        {
            GameObject shrine = new GameObject("Vergessener Schrein");
            shrine.transform.SetParent(parent);
            shrine.transform.position = new Vector3(
                ShrinePoint.x, GroundY(ShrinePoint.x, ShrinePoint.y), ShrinePoint.y
            );

            // 330°: die Nische zeigt zum versteckten Pfad im Westen.
            GameObject wall = PlaceRuinPiece(
                "TEMPLE:Wall_Straight_Cubby", shrine.transform, ShrinePoint,
                330f, 1.7f, MossyStoneMaterial, withCollider: true
            );
            wall.name = "Schrein-Nische";

            System.Random rng = new System.Random(59);

            // Halbkreis aus Kieseln davor, dazu Blumen — manche frischer, als sie sein duerften.
            for (int i = 0; i < 5; i++)
            {
                float angle = 2.1f + i * 0.45f;
                Vector2 position = ShrinePoint + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.2f;

                string model = $"Pebble_Round_{1 + i % 5}";
                PlaceModel(model, shrine.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, 0.22f));
            }

            for (int i = 0; i < 3; i++)
            {
                float angle = 2.3f + i * 0.5f;
                Vector2 position = ShrinePoint + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.8f;

                string model = i % 2 == 0 ? "Flower_3_Single" : "Flower_4_Single";
                PlaceModel(model, shrine.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, 0.32f));
            }

            accentTreePositions.Add(shrine.transform.position);

            AddExaminable(
                shrine,
                "Schrein betrachten",
                "Eine Nische im Stein, glattgerieben von unzaehligen Haenden. " +
                "Wem hier einst gedacht wurde, ist vergessen — und doch liegen " +
                "Blumen darin. Manche sind frischer, als sie sein duerften.",
                2.6f, 8f
            );
        }

        // Ueberwucherte Steinmauer am Uferweg: eine Mauer ohne Haus.
        private static void BuildOvergrownWall(Transform parent)
        {
            GameObject wallRow = new GameObject("Ueberwucherte Mauer");
            wallRow.transform.SetParent(parent);
            wallRow.transform.position = new Vector3(
                OvergrownWallPoint.x, GroundY(OvergrownWallPoint.x, OvergrownWallPoint.y), OvergrownWallPoint.y
            );

            Material stone = GetOrCreateMaterial("Proto_Stone", StoneColor);
            System.Random rng = new System.Random(61);

            (string model, float offset, float rotation, bool mossy)[] pieces =
            {
                ("TEMPLE:Wall_Ruined_Full_1", -2.4f, 91f, true),
                ("TEMPLE:Wall_Ruined_Half_1", 0f, 88f, false),
                ("TEMPLE:Wall_Ruined_Full_Cracked_2", 2.5f, 94f, true)
            };

            foreach ((string model, float offset, float rotation, bool mossy) piece in pieces)
            {
                Vector2 position = OvergrownWallPoint + new Vector2(piece.offset, (float)rng.NextDouble() * 0.4f - 0.2f);
                PlaceRuinPiece(piece.model, wallRow.transform, position, piece.rotation, 2.5f,
                    piece.mossy ? MossyStoneMaterial : stone, withCollider: true);
            }

            // Der Wald holt sich die Mauer zurueck.
            for (int i = 0; i < 5; i++)
            {
                Vector2 position = OvergrownWallPoint + new Vector2(
                    Mathf.Lerp(-3f, 3f, (float)rng.NextDouble()),
                    Mathf.Lerp(0.7f, 1.6f, (float)rng.NextDouble()) * (rng.Next(2) == 0 ? 1f : -1f)
                );

                string model = i < 2 ? "Bush_Common" : "Fern_1";
                PlaceModel(model, wallRow.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, i < 2 ? 1.0f : 0.7f));
            }

            accentTreePositions.Add(new Vector3(OvergrownWallPoint.x - 2f, 0f, OvergrownWallPoint.y));
            accentTreePositions.Add(new Vector3(OvergrownWallPoint.x + 2f, 0f, OvergrownWallPoint.y));

            AddExaminable(
                wallRow,
                "Mauerreste untersuchen",
                "Eine Mauer ohne Haus, ohne Tor, ohne Strasse. Sie schuetzte einmal " +
                "etwas — der Wald hat es sich laengst zurueckgeholt und die Steine " +
                "gleich mit.",
                2.8f, 7f
            );
        }

        // Region "Moosiger Felsenbereich" im Suedosten: gestapelte Felsen.
        private static void BuildRockFormation(Transform parent)
        {
            GameObject formation = new GameObject("Moosige Felsen");
            formation.transform.SetParent(parent);
            formation.transform.position = new Vector3(
                RockFormationPoint.x, GroundY(RockFormationPoint.x, RockFormationPoint.y), RockFormationPoint.y
            );

            System.Random rng = new System.Random(47);

            GameObject baseRock = PlaceModel("USN:Rock_3", formation.transform,
                RockFormationPoint, 20f, NormalizedScale("USN:Rock_3", 2.6f));
            AddBoundsCollider(baseRock);

            // Zweiter Fels, wie von Riesenhand daraufgelegt.
            GameObject topRock = PlaceModel("USN:Rock_1", formation.transform,
                RockFormationPoint + new Vector2(0.3f, 0.2f), 70f, NormalizedScale("USN:Rock_1", 1.5f));
            Bounds baseBounds = CalculateBounds(baseRock);
            Bounds topBounds = CalculateBounds(topRock);
            topRock.transform.position += Vector3.up * (baseBounds.max.y - topBounds.min.y - 0.3f);

            // Ring kleinerer Felsen, Farne und Pilze.
            for (int i = 0; i < 5; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                Vector2 position = RockFormationPoint +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Lerp(2.4f, 4.2f, (float)rng.NextDouble());

                string model = $"USN:Rock_{1 + rng.Next(5)}";
                PlaceModel(model, formation.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, Mathf.Lerp(0.7f, 1.4f, (float)rng.NextDouble())));
            }

            for (int i = 0; i < 7; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                Vector2 position = RockFormationPoint +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Lerp(3f, 5.5f, (float)rng.NextDouble());

                string model = i < 4 ? "Fern_1" : "Mushroom_Common";
                PlaceModel(model, formation.transform, position,
                    (float)rng.NextDouble() * 360f, NormalizedScale(model, i < 4 ? 0.75f : 0.3f));
            }

            accentTreePositions.Add(formation.transform.position);
            accentTreePositions.Add(new Vector3(RockFormationPoint.x + 3f, 0f, RockFormationPoint.y - 2f));

            AddExaminable(
                formation,
                "Felsen betrachten",
                "Die Felsen liegen aufeinander, als haette eine Riesenhand sie " +
                "gestapelt. Unter dem Moos schimmern Rillen im Stein — zu " +
                "regelmaessig, um Zufall zu sein.",
                3.5f, 8f
            );
        }

        // Verstreute Pfeilerfragmente: der Wald war einmal mehr als Wald.
        private static void BuildScatteredRuins(Transform parent)
        {
            GameObject root = new GameObject("Verstreute Ruinen");
            root.transform.SetParent(parent);

            System.Random rng = new System.Random(43);
            Material stone = GetOrCreateMaterial("Proto_Stone", StoneColor);

            foreach (Vector2 center in RuinFragmentPoints)
            {
                // Stehender Pfeilerstumpf.
                GameObject pillar = PlaceModel("TEMPLE:Pillar_Large_Base", root.transform, center,
                    (float)rng.NextDouble() * 360f,
                    NormalizedScale("TEMPLE:Pillar_Large_Base", Mathf.Lerp(1.5f, 2.1f, (float)rng.NextDouble())));
                ApplyMaterialRecursively(pillar, rng.Next(2) == 0 ? stone : MossyStoneMaterial);
                AddBoundsCollider(pillar);

                // Gestuerztes Segment daneben.
                float fallAngle = (float)rng.NextDouble() * Mathf.PI * 2f;
                Vector2 fallOffset = new Vector2(Mathf.Cos(fallAngle), Mathf.Sin(fallAngle)) * 1.9f;
                GameObject fallen = PlaceModel("TEMPLE:Pillar_Large_Middle", root.transform, center + fallOffset,
                    Quaternion.Euler(88f, (float)rng.NextDouble() * 360f, 0f),
                    NormalizedScale("TEMPLE:Pillar_Large_Middle", 2.4f));
                ApplyMaterialRecursively(fallen, MossyStoneMaterial);

                // Halb versunkene Bodenplatte.
                Vector2 plateOffset = new Vector2(-fallOffset.y, fallOffset.x) * 0.9f;
                GameObject plate = PlaceModel("TEMPLE:Floor_Ruined_Straight_1", root.transform, center + plateOffset,
                    (float)rng.NextDouble() * 360f,
                    NormalizedScaleByFootprint("TEMPLE:Floor_Ruined_Straight_1", 2.2f));
                plate.transform.position += Vector3.down * 0.12f;
                ApplyMaterialRecursively(plate, stone);

                // Bewuchs am Fragment.
                PlaceModel("Fern_1", root.transform, center + new Vector2(1f, -1.1f),
                    (float)rng.NextDouble() * 360f, NormalizedScale("Fern_1", 0.7f));
                PlaceModel("Mushroom_Common", root.transform, center + new Vector2(-0.9f, 0.8f),
                    (float)rng.NextDouble() * 360f, NormalizedScale("Mushroom_Common", 0.28f));

                accentTreePositions.Add(new Vector3(center.x, 0f, center.y));
            }
        }

        // Umgestuerzte Baumriesen mit Pilzreihen — seit Jahrzehnten Teil des Bodens.
        private static void BuildFallenGiants(Transform parent)
        {
            GameObject root = new GameObject("Gestuerzte Baumriesen");
            root.transform.SetParent(parent);

            System.Random rng = new System.Random(67);

            foreach (Vector2 point in FallenGiantPoints)
            {
                float yaw = (float)rng.NextDouble() * 360f;
                string model = $"TST:DeadTree_{1 + rng.Next(10)}";

                GameObject giant = PlaceModel(model, root.transform, point,
                    Quaternion.Euler(86f, yaw, 0f), NormalizedScale(model, 13f));
                AddBoundsCollider(giant);

                // Der Stamm zeigt in Fallrichtung — Baeume dort fernhalten.
                Vector2 lieDirection = new Vector2(
                    Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad)
                );
                accentTreePositions.Add(new Vector3(point.x, 0f, point.y));
                accentTreePositions.Add(new Vector3(
                    point.x + lieDirection.x * 6f, 0f, point.y + lieDirection.y * 6f
                ));

                // Pilze und Farn am modernden Holz.
                for (int i = 0; i < 5; i++)
                {
                    Vector2 along = point + lieDirection * Mathf.Lerp(0.5f, 5.5f, (float)rng.NextDouble()) +
                        new Vector2(-lieDirection.y, lieDirection.x) * ((float)rng.NextDouble() * 1.6f - 0.8f);

                    string detail = i < 3 ? "Mushroom_Common" : "Fern_1";
                    PlaceModel(detail, root.transform, along,
                        (float)rng.NextDouble() * 360f, NormalizedScale(detail, i < 3 ? 0.3f : 0.7f));
                }
            }
        }

        // Engstellen: Felsen ruecken an den Weg heran, der Wald wird kurz eng.
        private static void BuildPathNarrows(Transform parent)
        {
            GameObject root = new GameObject("Weg-Engstellen");
            root.transform.SetParent(parent);

            System.Random rng = new System.Random(71);

            Vector2[] narrowRocks =
            {
                new Vector2(-10.6f, -31f), new Vector2(-5.4f, -33f),
                new Vector2(26f, -8.8f), new Vector2(26f, -3.2f),
                new Vector2(33.6f, 12.4f), new Vector2(38.4f, 11.6f)
            };

            foreach (Vector2 position in narrowRocks)
            {
                string model = $"Rock_Medium_{1 + rng.Next(3)}";
                GameObject rock = PlaceModel(model, root.transform, position,
                    (float)rng.NextDouble() * 360f,
                    NormalizedScale(model, Mathf.Lerp(1.7f, 2.3f, (float)rng.NextDouble())));
                AddBoundsCollider(rock);
            }
        }

        // ------------------------------------------------------------------
        // Vegetation
        // ------------------------------------------------------------------

        private static void ScatterVegetation(Transform parent, System.Random rng)
        {
            GameObject vegetationRoot = new GameObject("Vegetation");
            vegetationRoot.transform.SetParent(parent);

            Transform trees = CreateGroup(vegetationRoot.transform, "Baeume");
            Transform border = CreateGroup(vegetationRoot.transform, "Waldrand");
            Transform bushes = CreateGroup(vegetationRoot.transform, "Buesche");
            Transform grasses = CreateGroup(vegetationRoot.transform, "Gras");
            Transform ferns = CreateGroup(vegetationRoot.transform, "Farne");
            Transform floorPlants = CreateGroup(vegetationRoot.transform, "Waldboden");
            Transform rocks = CreateGroup(vegetationRoot.transform, "Felsen");
            Transform pebbles = CreateGroup(vegetationRoot.transform, "Kiesel");
            Transform mushrooms = CreateGroup(vegetationRoot.transform, "Pilze");

            BuildBirchGrove(trees, rng);
            ScatterTrees(trees, rng);
            BuildForestBorder(border, rng);
            ScatterBushes(bushes, rng);
            ScatterGrass(grasses, rng);
            ScatterFerns(ferns, rng);
            ScatterForestFloorPlants(floorPlants, rng);
            ScatterForestRocks(rocks, rng);
            ScatterPebbles(pebbles, rng);
            ScatterMushrooms(mushrooms, rng);
        }

        // Ein stiller Birkenhain westlich des Startwegs — heller Kontrast im Nebel.
        private static readonly Vector2 BirchGrovePoint = new Vector2(-16f, -31f);

        private static void BuildBirchGrove(Transform parent, System.Random rng)
        {
            int placed = 0;
            int attempts = 0;

            while (placed < 12 && attempts < 120)
            {
                attempts++;
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(1.5f, 7.5f, (float)rng.NextDouble());
                Vector2 position = BirchGrovePoint +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                if (!IsFreeForTree(position))
                {
                    continue;
                }

                PlaceTree($"TST:Birch_{1 + rng.Next(10)}", parent, position,
                    Mathf.Lerp(7.5f, 10f, (float)rng.NextDouble()), rng);
                placed++;
            }
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent);
            return group.transform;
        }

        private static void ScatterTrees(Transform parent, System.Random rng)
        {
            // Baumgruppen fuer organische Verteilung.
            int placedClusters = 0;
            int attempts = 0;

            while (placedClusters < 190 && attempts < 11000)
            {
                attempts++;
                Vector2 clusterCenter = RandomPlayablePosition(rng);

                if (!IsFreeForTree(clusterCenter))
                {
                    continue;
                }

                int clusterSize = 4 + rng.Next(4);

                for (int i = 0; i < clusterSize; i++)
                {
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float radius = Mathf.Lerp(1.5f, 4.5f, (float)rng.NextDouble());
                    Vector2 position = clusterCenter +
                        new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                    TryPlaceForestTree(parent, position, rng);
                }

                placedClusters++;
            }

            // Einzelbaeume zum Auffuellen.
            int singles = 0;
            attempts = 0;

            while (singles < 430 && attempts < 13000)
            {
                attempts++;

                if (TryPlaceForestTree(parent, RandomPlayablePosition(rng), rng))
                {
                    singles++;
                }
            }

            // Region "Dichter Urwald" im Suedwesten: knorrig, dunkel, eng.
            int jungleTrees = 0;
            attempts = 0;

            while (jungleTrees < 40 && attempts < 1500)
            {
                attempts++;

                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = (float)rng.NextDouble() * 18f;
                Vector2 position = JungleRegionPoint +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                if (Mathf.Abs(position.x) > PlayableRadius || Mathf.Abs(position.y) > PlayableRadius)
                {
                    continue;
                }

                if (!IsFreeForTree(position))
                {
                    continue;
                }

                double roll = rng.NextDouble();
                int variant = 1 + rng.Next(5);
                string model = roll < 0.45
                    ? $"TwistedTree_{variant}"
                    : roll < 0.8 ? $"Pine_{variant}" : $"USN:PineTree_{(variant == 4 ? 5 : variant)}";

                PlaceTree(model, parent, position,
                    Mathf.Lerp(8f, 12f, (float)rng.NextDouble()), rng);
                jungleTrees++;
            }
        }

        private static bool TryPlaceForestTree(Transform parent, Vector2 position, System.Random rng)
        {
            if (!IsFreeForTree(position))
            {
                return false;
            }

            (string model, float targetHeight) = SelectTreeType(position, rng);
            PlaceTree(model, parent, position, targetHeight, rng);
            return true;
        }

        private static (string model, float targetHeight) SelectTreeType(Vector2 position, System.Random rng)
        {
            float brookDistance = DistanceToPolyline(position, brookSamples);
            float elevation = GroundY(position.x, position.y);
            double roll = rng.NextDouble();
            int variant = 1 + rng.Next(5);

            // Variante 10 der TST-Baeume lehnt stark (Audit 25.07.) — auslassen.
            int variant10 = 1 + rng.Next(9);

            // USN-Kiefer 4 lehnt ebenfalls stark — auf gerade Varianten begrenzen.
            int pineVariant = variant == 4 ? 5 : variant;

            // Ufer: knorrige Baeume und einzelne Birken.
            if (brookDistance < 10f && roll < 0.3)
            {
                return roll < 0.2
                    ? ($"TwistedTree_{variant}", 7f)
                    : ($"TST:Birch_{variant10}", 8.5f);
            }

            // Hoehen und Nordosten: Nadelwald aus beiden Packs gemischt.
            if ((elevation > 2.5f || (position.x > 25f && position.y > 8f)) && roll < 0.6)
            {
                return rng.Next(2) == 0
                    ? ($"Pine_{variant}", 11f)
                    : ($"USN:PineTree_{pineVariant}", 11f);
            }

            if (roll < 0.03)
            {
                return ($"DeadTree_{variant}", 6.5f);
            }

            if (roll < 0.05)
            {
                return ($"TST:DeadTree_{variant10}", 7f);
            }

            if (roll < 0.07)
            {
                return ($"TST:DeadBirch_{variant10}", 7.5f);
            }

            if (roll < 0.26)
            {
                return rng.Next(2) == 0
                    ? ($"Pine_{variant}", 10f)
                    : ($"USN:PineTree_{pineVariant}", 10f);
            }

            if (roll < 0.38)
            {
                return ($"USN:NormalTree_{variant}", 8.5f);
            }

            if (roll < 0.50)
            {
                return ($"TST:Tree_{variant10}", 8.5f);
            }

            // Ahorn sparsam als herbstlicher Farbakzent.
            if (roll < 0.57)
            {
                return ($"USN:MapleTree_{variant}", 8f);
            }

            if (roll < 0.65)
            {
                return ($"TST:Birch_{variant10}", 9f);
            }

            return ($"CommonTree_{variant}", 8f);
        }

        private static Vector2 RandomPlayablePosition(System.Random rng)
        {
            return new Vector2(
                Mathf.Lerp(-PlayableRadius, PlayableRadius, (float)rng.NextDouble()),
                Mathf.Lerp(-PlayableRadius, PlayableRadius, (float)rng.NextDouble())
            );
        }

        private static bool IsFreeForTree(Vector2 position)
        {
            if (PathDistance(position) < 2.8f) return false;
            if (DistanceToPolyline(position, brookSamples) < 4.5f) return false;
            if (Vector2.Distance(position, ClearingPoint) < 6f) return false;
            if (Vector2.Distance(position, RuinPoint) < 5.5f) return false;
            if (Vector2.Distance(position, StoneCirclePoint) < 6f) return false;
            if (Vector2.Distance(position, ViewpointPoint) < 5.5f) return false;
            if (Vector2.Distance(position, StartPoint) < 6f) return false;
            if (Vector2.Distance(position, MemorySitePoint) < 4.5f) return false;
            if (Vector2.Distance(position, LogCrossingPoint) < 4.5f) return false;
            if (Vector2.Distance(position, FordPoint) < 4.5f) return false;
            if (Vector2.Distance(position, ForkRockPoint) < 3f) return false;
            if (Vector2.Distance(position, BridgePoint) < 5f) return false;
            if (Vector2.Distance(position, WatchtowerPoint) < 6f) return false;
            if (Vector2.Distance(position, BlackLakePoint) < 12f) return false;
            if (Vector2.Distance(position, ForestHutPoint) < 6f) return false;
            if (Vector2.Distance(position, ShadowlessTreePoint) < 7f) return false;
            if (Vector2.Distance(position, QuietPondPoint) < 7.5f) return false;
            if (Vector2.Distance(position, HunterCampPoint) < 6f) return false;

            foreach (Vector3 treePosition in placedTreePositions)
            {
                if (Vector2.Distance(position, new Vector2(treePosition.x, treePosition.z)) < 1.8f)
                {
                    return false;
                }
            }

            foreach (Vector3 accentPosition in accentTreePositions)
            {
                if (Vector2.Distance(position, new Vector2(accentPosition.x, accentPosition.z)) < 5f)
                {
                    return false;
                }
            }

            return true;
        }

        // Dichter Baumrand: Der Spieler sieht nie ueber den Wald hinaus.
        private static void BuildForestBorder(Transform parent, System.Random rng)
        {
            float inner = PlayableRadius + 1f;
            float outer = PlayableRadius + 6f;
            int count = 230;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count * Mathf.PI * 2f;
                float ring = Mathf.Lerp(inner, outer, (float)rng.NextDouble());

                // Quadratischer Ring statt Kreis, damit die Ecken gefuellt sind.
                Vector2 direction = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
                float scaleToEdge = ring / Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
                Vector2 position = direction * scaleToEdge;

                position += new Vector2(
                    (float)(rng.NextDouble() * 2.0 - 1.0),
                    (float)(rng.NextDouble() * 2.0 - 1.0)
                );

                int variant = 1 + rng.Next(5);
                double treeRoll = rng.NextDouble();

                string model;
                if (treeRoll < 0.35)
                {
                    model = $"Pine_{variant}";
                }
                else if (treeRoll < 0.55)
                {
                    model = $"USN:PineTree_{(variant == 4 ? 5 : variant)}";
                }
                else if (treeRoll < 0.8)
                {
                    model = $"CommonTree_{variant}";
                }
                else
                {
                    model = $"TST:Tree_{1 + rng.Next(10)}";
                }

                float targetHeight = Mathf.Lerp(9f, 12f, (float)rng.NextDouble());

                float scale = NormalizedScale(model, targetHeight);
                GameObject tree = PlaceModel(model, parent, position,
                    (float)rng.NextDouble() * 360f, scale);
                AddTrunkCollider(tree, scale);
            }
        }

        private static void ScatterBushes(Transform parent, System.Random rng)
        {
            int placed = 0;
            int attempts = 0;

            while (placed < 620 && attempts < 13000)
            {
                attempts++;
                Vector2 position = RandomPlayablePosition(rng);
                float pathDistance = PathDistance(position);

                // Buesche saeumen bevorzugt die Wege, fuellen aber auch den Wald.
                bool nearPath = pathDistance > 2.4f && pathDistance < 7f;
                if (!nearPath && rng.NextDouble() < 0.3)
                {
                    continue;
                }

                if (pathDistance < 2.4f) continue;
                if (DistanceToPolyline(position, brookSamples) < 3f) continue;
                if (Vector2.Distance(position, StartPoint) < 4f) continue;
                if (Vector2.Distance(position, MemorySitePoint) < 3.5f) continue;
                if (Vector2.Distance(position, RestSitePoint) < 3f) continue;
                if (Vector2.Distance(position, ShrinePoint) < 2.5f) continue;

                (string model, float targetHeight) = SelectBushType(rng);
                float scale = NormalizedScale(model, targetHeight) *
                              Mathf.Lerp(0.8f, 1.35f, (float)rng.NextDouble());
                PlaceModel(model, parent, position, (float)rng.NextDouble() * 360f, scale);
                placed++;
            }
        }

        private static (string model, float targetHeight) SelectBushType(System.Random rng)
        {
            double roll = rng.NextDouble();

            if (roll < 0.30) return ("Bush_Common", 1.1f);
            if (roll < 0.40) return ("Bush_Common_Flowers", 1.1f);
            if (roll < 0.60) return ("USN:Bush", 1.1f);
            if (roll < 0.72) return ("USN:Bush_Large", 1.5f);
            if (roll < 0.88) return ("USN:Bush_Small", 0.7f);
            if (roll < 0.94) return ("USN:Bush_Flowers", 1.1f);
            return ("USN:Bush_Large_Flowers", 1.5f);
        }

        private static void ScatterGrass(Transform parent, System.Random rng)
        {
            string[] grassModels =
            {
                "Grass_Common_Short", "Grass_Common_Tall",
                "Grass_Wispy_Short", "Grass_Wispy_Tall"
            };

            int placed = 0;
            int attempts = 0;

            while (placed < 1300 && attempts < 19000)
            {
                attempts++;

                Vector2 position;
                double zone = rng.NextDouble();

                if (zone < 0.45)
                {
                    // Entlang der Wege.
                    PathSample sample = pathSamples[rng.Next(pathSamples.Count)];
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float radius = Mathf.Lerp(1.6f, 4.5f, (float)rng.NextDouble());
                    position = sample.Position +
                        new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
                else if (zone < 0.7)
                {
                    // Auf der Lichtung.
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float radius = Mathf.Lerp(2f, 9f, (float)rng.NextDouble());
                    position = ClearingPoint +
                        new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
                else
                {
                    position = RandomPlayablePosition(rng);
                }

                if (Mathf.Abs(position.x) > PlayableRadius || Mathf.Abs(position.y) > PlayableRadius) continue;
                if (PathDistance(position) < 1.5f) continue;
                if (DistanceToPolyline(position, brookSamples) < 2.5f) continue;

                string model = grassModels[rng.Next(grassModels.Length)];
                float scale = NormalizedScale(model, 0.45f) *
                              Mathf.Lerp(0.75f, 1.4f, (float)rng.NextDouble());
                PlaceModel(model, parent, position, (float)rng.NextDouble() * 360f, scale);
                placed++;
            }
        }

        private static void ScatterFerns(Transform parent, System.Random rng)
        {
            int placed = 0;
            int attempts = 0;

            while (placed < 250 && attempts < 6500)
            {
                attempts++;
                Vector2 position = RandomPlayablePosition(rng);

                float brookDistance = DistanceToPolyline(position, brookSamples);
                bool nearBrook = brookDistance > 4f && brookDistance < 9f;
                bool nearRuin = Vector2.Distance(position, RuinPoint) < 9f &&
                                Vector2.Distance(position, RuinPoint) > 4f;

                if (!nearBrook && !nearRuin)
                {
                    continue;
                }

                if (PathDistance(position) < 1.8f) continue;

                float scale = NormalizedScale("Fern_1", 0.7f) *
                              Mathf.Lerp(0.8f, 1.3f, (float)rng.NextDouble());
                PlaceModel("Fern_1", parent, position, (float)rng.NextDouble() * 360f, scale);
                placed++;
            }
        }

        // Stauden und Blumeninseln aus dem Stylized-Nature-Pack fuellen das
        // Unterholz zwischen den Baeumen.
        private static void ScatterForestFloorPlants(Transform parent, System.Random rng)
        {
            string[] plantModels = { "USN:Plant_1", "USN:Plant_2", "USN:Plant_Flowers" };
            string[] flowerModels =
            {
                "USN:Flower_1_Clump", "USN:Flower_2_Clump", "USN:Flower_3_Clump",
                "USN:Flower_4_Clump", "USN:Flower_5_Clump"
            };

            int placed = 0;
            int attempts = 0;

            while (placed < 290 && attempts < 7500)
            {
                attempts++;
                Vector2 position = RandomPlayablePosition(rng);
                float pathDistance = PathDistance(position);

                // Stauden im Unterholz, Blumen eher an lichten Stellen nahe der Wege.
                bool underwood = pathDistance > 4f;
                bool lightSpot = pathDistance > 1.8f && pathDistance < 5f;

                if (!underwood && !lightSpot) continue;
                if (DistanceToPolyline(position, brookSamples) < 2.5f) continue;
                if (Vector2.Distance(position, MemorySitePoint) < 3.5f) continue;

                string model = lightSpot && rng.NextDouble() < 0.55
                    ? flowerModels[rng.Next(flowerModels.Length)]
                    : plantModels[rng.Next(plantModels.Length)];

                float scale = NormalizedScale(model, 0.5f) *
                              Mathf.Lerp(0.8f, 1.3f, (float)rng.NextDouble());
                PlaceModel(model, parent, position, (float)rng.NextDouble() * 360f, scale);
                placed++;
            }
        }

        // Groessere Einzelfelsen tief im Wald — brechen die Baumflaechen auf.
        private static void ScatterForestRocks(Transform parent, System.Random rng)
        {
            int placed = 0;
            int attempts = 0;

            while (placed < 105 && attempts < 4500)
            {
                attempts++;
                Vector2 position = RandomPlayablePosition(rng);

                if (PathDistance(position) < 2.6f) continue;
                if (DistanceToPolyline(position, brookSamples) < 5f) continue;
                if (Vector2.Distance(position, ClearingPoint) < 8f) continue;
                if (Vector2.Distance(position, StoneCirclePoint) < 7f) continue;
                if (Vector2.Distance(position, MemorySitePoint) < 4.5f) continue;

                string model = $"USN:Rock_{1 + rng.Next(5)}";
                float scale = NormalizedScale(model, Mathf.Lerp(0.6f, 1.8f, (float)rng.NextDouble()));
                PlaceModel(model, parent, position, (float)rng.NextDouble() * 360f, scale);
                placed++;
            }
        }

        private static void ScatterPebbles(Transform parent, System.Random rng)
        {
            int placed = 0;
            int attempts = 0;

            while (placed < 210 && attempts < 5200)
            {
                attempts++;

                Vector2 position;

                if (rng.NextDouble() < 0.5)
                {
                    PathSample sample = pathSamples[rng.Next(pathSamples.Count)];
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float radius = Mathf.Lerp(1.6f, 3.5f, (float)rng.NextDouble());
                    position = sample.Position +
                        new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
                else
                {
                    Vector2 brookSample = brookSamples[rng.Next(brookSamples.Count)];
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float radius = Mathf.Lerp(4.6f, 6.5f, (float)rng.NextDouble());
                    position = brookSample +
                        new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }

                if (Mathf.Abs(position.x) > PlayableRadius || Mathf.Abs(position.y) > PlayableRadius) continue;
                if (PathDistance(position) < 1.4f) continue;
                if (DistanceToPolyline(position, brookSamples) < 4.2f) continue;

                bool round = rng.NextDouble() < 0.5;
                string model = round
                    ? $"Pebble_Round_{1 + rng.Next(5)}"
                    : $"Pebble_Square_{1 + rng.Next(6)}";

                float scale = NormalizedScale(model, 0.3f) *
                              Mathf.Lerp(0.7f, 1.5f, (float)rng.NextDouble());
                PlaceModel(model, parent, position, (float)rng.NextDouble() * 360f, scale);
                placed++;
            }
        }

        private static void ScatterMushrooms(Transform parent, System.Random rng)
        {
            int count = Mathf.Min(50, placedTreePositions.Count);

            for (int i = 0; i < count; i++)
            {
                Vector3 treePosition = placedTreePositions[rng.Next(placedTreePositions.Count)];
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(0.8f, 2f, (float)rng.NextDouble());
                Vector2 position = new Vector2(treePosition.x, treePosition.z) +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                if (PathDistance(position) < 1.5f)
                {
                    continue;
                }

                string model = rng.NextDouble() < 0.7 ? "Mushroom_Common" : "Mushroom_Laetiporus";
                float scale = NormalizedScale(model, 0.28f) *
                              Mathf.Lerp(0.8f, 1.4f, (float)rng.NextDouble());
                PlaceModel(model, parent, position, (float)rng.NextDouble() * 360f, scale);
            }
        }

        // ------------------------------------------------------------------
        // UI
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
                new Vector2(0.5f, 0.12f), new Vector2(560f, 56f),
                font, 26, out Text promptText
            );

            GameObject narrationRoot = BuildTextPanel(
                canvasObject.transform, "NarrationPanel",
                new Vector2(0.5f, 0.85f), new Vector2(900f, 140f),
                font, 24, out Text narrationText
            );

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

            BuildIntroUI(canvasObject, font);
            BuildCompassUI(canvasObject, font);
            BuildTutorialUI(canvasObject, font);
            BuildInventoryUI(canvasObject, font);
            BuildCombatTutorialUI(canvasObject, font, detector);
            WireSfxLibrary(canvasObject, detector.transform);
        }

        // Soundeffekt-Bibliothek: Kenney-CC0-Clips an die Spiel-Events binden.
        internal static void WireSfxLibrary(GameObject canvasObject, Transform player)
        {
            const string rpgAudio = "Assets/ThirdParty/Kenney/kenney_rpg-audio/Audio";
            const string impactAudio = "Assets/ThirdParty/Kenney/kenney_impact-sounds/Audio";

            canvasObject.AddComponent<AudioSource>();
            Elyndor.Core.SfxLibrary sfx = canvasObject.AddComponent<Elyndor.Core.SfxLibrary>();

            SerializedObject serialized = new SerializedObject(sfx);
            serialized.FindProperty("narrationClip").objectReferenceValue = LoadClip($"{rpgAudio}/bookFlip1.ogg");
            serialized.FindProperty("memoryStartClip").objectReferenceValue = LoadClip($"{rpgAudio}/metalLatch.ogg");
            serialized.FindProperty("memoryCompleteClip").objectReferenceValue = LoadClip($"{rpgAudio}/bookOpen.ogg");
            serialized.FindProperty("attackLightClip").objectReferenceValue = LoadClip($"{rpgAudio}/knifeSlice.ogg");
            serialized.FindProperty("attackHeavyClip").objectReferenceValue = LoadClip($"{rpgAudio}/chop.ogg");
            serialized.FindProperty("hitLightClip").objectReferenceValue = LoadClip($"{impactAudio}/impactPunch_medium_000.ogg");
            serialized.FindProperty("hitHeavyClip").objectReferenceValue = LoadClip($"{impactAudio}/impactPunch_heavy_000.ogg");
            serialized.FindProperty("inventoryOpenClip").objectReferenceValue = LoadClip($"{rpgAudio}/clothBelt.ogg");
            serialized.FindProperty("inventoryCloseClip").objectReferenceValue = LoadClip($"{rpgAudio}/cloth1.ogg");
            serialized.FindProperty("footstepSource").objectReferenceValue = player;

            SerializedProperty footsteps = serialized.FindProperty("footstepClips");
            footsteps.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                footsteps.GetArrayElementAtIndex(i).objectReferenceValue =
                    LoadClip($"{impactAudio}/footstep_grass_00{i}.ogg");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioClip LoadClip(string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (clip == null)
            {
                Debug.LogWarning($"Audio-Clip nicht gefunden: {path}");
            }

            return clip;
        }

        // Kenney-UI-Sprite mit 9-Slice-Rand laden (Importer wird angepasst).
        internal static Sprite LoadUiSprite(string path)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

            if (importer == null)
            {
                Debug.LogWarning($"UI-Sprite nicht gefunden: {path}");
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteBorder == Vector4.zero)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteBorder = new Vector4(16f, 16f, 16f, 16f);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Kampf-Tutorial: startet, sobald das Schwert in der Haupthand liegt.
        private static void BuildCombatTutorialUI(
            GameObject canvasObject, Font font, InteractionDetector detector
        )
        {
            GameObject promptRoot = BuildTextPanel(
                canvasObject.transform, "CombatTutorialPrompt",
                new Vector2(0.16f, 0.22f), new Vector2(480f, 76f),
                font, 22, out Text promptText
            );
            promptRoot.SetActive(false);

            Elyndor.Core.CombatTutorial combatTutorial =
                canvasObject.AddComponent<Elyndor.Core.CombatTutorial>();
            SerializedObject serialized = new SerializedObject(combatTutorial);
            serialized.FindProperty("promptRoot").objectReferenceValue = promptRoot;
            serialized.FindProperty("promptText").objectReferenceValue = promptText;
            serialized.FindProperty("playerCombat").objectReferenceValue =
                detector.GetComponent<Elyndor.Combat.PlayerCombat>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Bewegungs-Tutorial: schlanker Hinweis unten links, schaltet erst
        // nach ausgefuehrter Aktion weiter.
        private static void BuildTutorialUI(GameObject canvasObject, Font font)
        {
            GameObject promptRoot = BuildTextPanel(
                canvasObject.transform, "TutorialPrompt",
                new Vector2(0.16f, 0.12f), new Vector2(480f, 64f),
                font, 22, out Text promptText
            );
            promptRoot.SetActive(false);

            Elyndor.Core.TutorialSequence tutorial =
                canvasObject.AddComponent<Elyndor.Core.TutorialSequence>();
            SerializedObject serializedTutorial = new SerializedObject(tutorial);
            serializedTutorial.FindProperty("intro").objectReferenceValue =
                canvasObject.GetComponent<Elyndor.Core.IntroSequence>();
            serializedTutorial.FindProperty("promptRoot").objectReferenceValue = promptRoot;
            serializedTutorial.FindProperty("promptText").objectReferenceValue = promptText;
            serializedTutorial.ApplyModifiedPropertiesWithoutUndo();
        }

        // Kompass oben rechts: Windrose dreht sich mit der Kamera.
        internal static void BuildCompassUI(GameObject canvasObject, Font font)
        {
            GameObject compass = new GameObject("Kompass");
            compass.transform.SetParent(canvasObject.transform, false);

            RectTransform compassRect = compass.AddComponent<RectTransform>();
            compassRect.anchorMin = new Vector2(0.94f, 0.9f);
            compassRect.anchorMax = new Vector2(0.94f, 0.9f);
            compassRect.sizeDelta = new Vector2(92f, 92f);

            UnityEngine.UI.Image background = compass.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0f, 0f, 0f, 0.35f);
            background.raycastTarget = false;

            GameObject needle = new GameObject("Windrose");
            needle.transform.SetParent(compass.transform, false);
            RectTransform needleRect = needle.AddComponent<RectTransform>();
            needleRect.sizeDelta = Vector2.zero;

            (string letter, Vector2 offset, Color color)[] directions =
            {
                ("N", new Vector2(0f, 30f), new Color(0.9f, 0.5f, 0.4f)),
                ("O", new Vector2(30f, 0f), new Color(0.85f, 0.87f, 0.9f)),
                ("S", new Vector2(0f, -30f), new Color(0.85f, 0.87f, 0.9f)),
                ("W", new Vector2(-30f, 0f), new Color(0.85f, 0.87f, 0.9f))
            };

            foreach ((string letter, Vector2 offset, Color color) in directions)
            {
                GameObject letterObject = new GameObject(letter);
                letterObject.transform.SetParent(needle.transform, false);

                RectTransform letterRect = letterObject.AddComponent<RectTransform>();
                letterRect.anchoredPosition = offset;
                letterRect.sizeDelta = new Vector2(24f, 24f);

                UnityEngine.UI.Text letterText = letterObject.AddComponent<UnityEngine.UI.Text>();
                letterText.font = font;
                letterText.fontSize = 18;
                letterText.alignment = TextAnchor.MiddleCenter;
                letterText.color = color;
                letterText.raycastTarget = false;
                letterText.text = letter;
            }

            CompassUI compassUI = canvasObject.AddComponent<CompassUI>();
            SerializedObject serializedCompass = new SerializedObject(compassUI);
            serializedCompass.FindProperty("needle").objectReferenceValue = needleRect;
            serializedCompass.ApplyModifiedPropertiesWithoutUndo();
        }

        // Erwachens-Sequenz (Kapitel 1): Schwarzblende mit Erinnerungszeilen.
        private static void BuildIntroUI(GameObject canvasObject, Font font)
        {
            GameObject blackScreen = new GameObject("IntroBlende");
            blackScreen.transform.SetParent(canvasObject.transform, false);

            RectTransform blackRect = blackScreen.AddComponent<RectTransform>();
            blackRect.anchorMin = Vector2.zero;
            blackRect.anchorMax = Vector2.one;
            blackRect.offsetMin = Vector2.zero;
            blackRect.offsetMax = Vector2.zero;

            Image blackImage = blackScreen.AddComponent<Image>();
            blackImage.color = Color.black;
            blackImage.raycastTarget = false;

            CanvasGroup blackGroup = blackScreen.AddComponent<CanvasGroup>();
            blackGroup.alpha = 1f;
            blackGroup.blocksRaycasts = false;
            blackGroup.interactable = false;

            GameObject lineObject = new GameObject("IntroText");
            lineObject.transform.SetParent(blackScreen.transform, false);

            RectTransform lineRect = lineObject.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(1000f, 80f);

            Text lineText = lineObject.AddComponent<Text>();
            lineText.font = font;
            lineText.fontSize = 28;
            lineText.alignment = TextAnchor.MiddleCenter;
            lineText.color = new Color(0.85f, 0.87f, 0.9f, 0.95f);
            lineText.horizontalOverflow = HorizontalWrapMode.Wrap;

            Elyndor.Core.IntroSequence intro =
                canvasObject.AddComponent<Elyndor.Core.IntroSequence>();
            SerializedObject serializedIntro = new SerializedObject(intro);
            serializedIntro.FindProperty("blackScreen").objectReferenceValue = blackGroup;
            serializedIntro.FindProperty("lineText").objectReferenceValue = lineText;
            serializedIntro.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildTextPanel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 size,
            Font font,
            int fontSize,
            out Text text
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

            // Leichte Abdunklung statt Farb-Overlay: ruhig, nicht spektakulär.
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

        // ------------------------------------------------------------------
        // Materialien und Hilfsfunktionen
        // ------------------------------------------------------------------

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            Material material = LoadOrCreateMaterialAsset(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateWaterMaterial()
        {
            Material material = LoadOrCreateMaterialAsset("Proto_Water", "Universal Render Pipeline/Lit");
            material.shader = Shader.Find("Elyndor/StylizedWater");
            material.SetColor("_BaseColor", WaterColor);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateGhostMaterial()
        {
            Material material = LoadOrCreateMaterialAsset("Proto_MemoryGhost", "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", GhostColor);
            ConfigureTransparent(material);

            // Sanftes Eigenleuchten: „Hier war einmal etwas."
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.35f, 0.55f, 0.7f) * 0.5f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

            EditorUtility.SetDirty(material);
            return material;
        }

        // Standard-Rezept für transparente URP-Lit-Materialien.
        private static void ConfigureTransparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Material LoadOrCreateMaterialAsset(string name, string shaderName)
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

        private static void AddSceneToBuildSettings()
        {
            foreach (EditorBuildSettingsScene existingScene in EditorBuildSettings.scenes)
            {
                if (existingScene.path == TargetScenePath)
                {
                    return;
                }
            }

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            System.Array.Resize(ref scenes, scenes.Length + 1);
            scenes[scenes.Length - 1] = new EditorBuildSettingsScene(TargetScenePath, true);
            EditorBuildSettings.scenes = scenes;
        }
    }
}










