using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Temporaeres Werkzeug: rendert Kontroll-Screenshots der Finsterwald-Szene
    /// im Batch-Mode. Kann nach Gebrauch geloescht werden.
    /// </summary>
    public static class SceneScreenshotter
    {
        private const string ScenePath = "Assets/_Elyndor/Scenes/Finsterwald.unity";
        private const string OutputFolder =
            @"C:\Users\Lars\AppData\Local\Temp\claude\C--Users-Lars-Desktop-Echos-of-Elyndor-EchoesOfElyndor\234220f0-d3fd-4883-acb3-27198b1307e3\scratchpad\shots";

        private const int Width = 1600;
        private const int Height = 900;

        /// <summary>
        /// Listet alle Renderer auf, deren Materialien nicht aus dem
        /// Pack_/Proto_-Satz stammen oder deren Basistextur fehlt.
        /// </summary>
        public static void Audit()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var reported = new System.Collections.Generic.HashSet<string>();

            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>())
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        Debug.Log($"AUDIT|NULL-Material|{FullPath(renderer.transform)}");
                        continue;
                    }

                    bool isOwn = material.name.StartsWith("Pack_") || material.name.StartsWith("Proto_");
                    bool missingTexture = material.HasProperty("_BaseMap") &&
                                          material.GetTexture("_BaseMap") == null &&
                                          material.name.StartsWith("Pack_") &&
                                          !material.name.Contains("Mushrooms");

                    string key = $"{material.name}|{material.shader.name}|{isOwn}|{missingTexture}";

                    if (!isOwn || missingTexture)
                    {
                        if (reported.Add(key))
                        {
                            Debug.Log($"AUDIT|{(isOwn ? "OHNE-TEXTUR" : "FREMD")}|Material={material.name}|Shader={material.shader.name}|Beispiel={FullPath(renderer.transform)}");
                        }
                    }
                }
            }

            Debug.Log("AUDIT|FERTIG");
        }

        private static string FullPath(Transform transform)
        {
            string path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }

        /// <summary>Misst Referenzmodelle auf mehreren Wegen (Debug).</summary>
        public static void AuditModels()
        {
            string[] paths =
            {
                "Assets/ThirdParty/Natur Pack/FBX (Unity)/Pine_1.fbx",
                "Assets/ThirdParty/Natur Pack/FBX (Unity)/CommonTree_1.fbx",
                "Assets/ThirdParty/Natur Pack/FBX (Unity)/Clover_1.fbx",
                "Assets/ThirdParty/Natur Pack/FBX (Unity)/Bush_Common.fbx",
                "Assets/ThirdParty/Ultimate Stylized Nature - May 2022/FBX/Rock_3.fbx",
                "Assets/ThirdParty/Ultimate Stylized Nature - May 2022/FBX/PineTree_1.fbx",
                "Assets/ThirdParty/Textured Stylized Trees - May 2020/FBX/Birch_1.fbx"
            };

            foreach (string path in paths)
            {
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                {
                    Debug.Log($"MODELL|{path}|NULL");
                    continue;
                }

                GameObject instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);

                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                Bounds rendererBounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds();
                foreach (Renderer r in renderers) { rendererBounds.Encapsulate(r.bounds); }

                Bounds cornerBounds = new Bounds(instance.transform.position, Vector3.zero);
                bool has = false;
                foreach (MeshFilter f in instance.GetComponentsInChildren<MeshFilter>())
                {
                    if (f.sharedMesh == null) continue;
                    Bounds mb = f.sharedMesh.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 c = new Vector3(
                            (i & 1) == 0 ? mb.min.x : mb.max.x,
                            (i & 2) == 0 ? mb.min.y : mb.max.y,
                            (i & 4) == 0 ? mb.min.z : mb.max.z);
                        Vector3 w = f.transform.TransformPoint(c);
                        if (!has) { cornerBounds = new Bounds(w, Vector3.zero); has = true; }
                        else cornerBounds.Encapsulate(w);
                    }
                }

                string scales = $"root={instance.transform.localScale.x:F4}";
                foreach (Transform t in instance.GetComponentsInChildren<Transform>())
                {
                    if (t != instance.transform)
                    {
                        scales += $";{t.name}={t.localScale.x:F4}";
                    }
                }

                Debug.Log($"MODELL|{System.IO.Path.GetFileName(path)}|corner={cornerBounds.size}|renderer={rendererBounds.size}|{scales}");
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>Findet uebergrosse Objekte in der gespeicherten Szene (Debug).</summary>
        /// <summary>
        /// Stellt alle Baumvarianten der Streu-Pools in Reihen auf und
        /// fotografiert sie — zum Aussortieren schiefer/liegender Modelle.
        /// </summary>
        public static void AuditTreeLineup()
        {
            UnityEditor.EditorSettings.asyncShaderCompilation = false;
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            GameObject lightObject = new GameObject("Sun");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);

            Directory.CreateDirectory(OutputFolder);

            string tstFolder = "Assets/ThirdParty/Textured Stylized Trees - May 2020/FBX";
            string usnFolder = "Assets/ThirdParty/Ultimate Stylized Nature - May 2022/FBX";
            (string label, string prefix, string folder, int count)[] rows =
            {
                ("birch", "Birch", tstFolder, 10), ("deadbirch", "DeadBirch", tstFolder, 10),
                ("deadtree", "DeadTree", tstFolder, 10), ("tree", "Tree", tstFolder, 10),
                ("usn_normal", "NormalTree", usnFolder, 5), ("usn_pine", "PineTree", usnFolder, 5),
                ("usn_maple", "MapleTree", usnFolder, 5)
            };

            System.Collections.Generic.List<GameObject> rowInstances =
                new System.Collections.Generic.List<GameObject>();

            foreach ((string label, string prefix, string folder, int count) in rows)
            {
                foreach (GameObject old in rowInstances)
                {
                    Object.DestroyImmediate(old);
                }

                rowInstances.Clear();

                for (int i = 1; i <= count; i++)
                {
                    string path = $"{folder}/{prefix}_{i}.fbx";
                    GameObject model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (model == null)
                    {
                        continue;
                    }

                    GameObject instance = (GameObject)Object.Instantiate(model);
                    rowInstances.Add(instance);

                    Bounds bounds = CalculateWorldBounds(instance);
                    float scale = bounds.size.y > 0.01f ? 8f / bounds.size.y : 1f;

                    // FBX-Root-Scale multiplizieren, nie ueberschreiben!
                    instance.transform.localScale *= scale;

                    bounds = CalculateWorldBounds(instance);
                    instance.transform.position += new Vector3(
                        (i - 1) * 7f - bounds.center.x, -bounds.min.y, -bounds.center.z
                    );

                    // Schraeglage messbar machen: Breite/Hoehe und horizontaler
                    // Versatz der oberen Haelfte gegenueber dem Fusspunkt.
                    bounds = CalculateWorldBounds(instance);
                    float widthRatio =
                        Mathf.Max(bounds.size.x, bounds.size.z) / Mathf.Max(bounds.size.y, 0.01f);

                    Vector3 basePoint = instance.transform.position;
                    float centerOffset = Vector2.Distance(
                        new Vector2(bounds.center.x, bounds.center.z),
                        new Vector2(basePoint.x, basePoint.z)
                    ) / Mathf.Max(bounds.size.y, 0.01f);

                    Debug.Log($"BAUM|{prefix}_{i}|ratio={widthRatio:F2}|offset={centerOffset:F2}");
                }

                RenderShot($"audit_{label}",
                    new Vector3(31f, 7f, -24f), new Vector3(31f, 3.5f, 0f));
            }

            Debug.Log("Baum-Parade gerendert.");
        }

        private static Bounds CalculateWorldBounds(GameObject target)
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

        public static void AuditGiants()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int reported = 0;

            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>())
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();

                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                Vector3 worldSize = Vector3.Scale(filter.sharedMesh.bounds.size, renderer.transform.lossyScale);

                if (worldSize.y > 25f || worldSize.x > 25f)
                {
                    Transform root = renderer.transform;
                    string chain = $"{root.name}({root.localScale.x:F2})";
                    while (root.parent != null)
                    {
                        root = root.parent;
                        chain = $"{root.name}({root.localScale.x:F2})/" + chain;
                    }

                    Debug.Log($"RIESE|{worldSize}|lossy={renderer.transform.lossyScale.x:F2}|{chain}");

                    if (++reported >= 15)
                    {
                        break;
                    }
                }
            }

            Debug.Log($"RIESE|FERTIG|{reported}");

            // Uebergrosse Collider (unsichtbare Physik-Fallen) finden.
            int colliderCount = 0;

            foreach (Collider collider in Object.FindObjectsByType<Collider>())
            {
                Vector3 size = collider.bounds.size;

                if (size.x > 30f || size.y > 30f || size.z > 30f)
                {
                    Transform root = collider.transform;
                    string chain = root.name;
                    while (root.parent != null) { root = root.parent; chain = $"{root.name}/{chain}"; }
                    Debug.Log($"KOLLIDER|{size}|{chain}");

                    if (++colliderCount >= 15) break;
                }
            }

            Debug.Log($"KOLLIDER|FERTIG|{colliderCount}");
        }

        /// <summary>Gibt Weltpositionen der Kinder eines Objekts aus (Debug).</summary>
        public static void AuditFormation()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject formation = GameObject.Find("Moosige Felsen");

            if (formation == null)
            {
                Debug.Log("FORMATION|nicht gefunden");
                return;
            }

            Debug.Log($"FORMATION|Root|{formation.transform.position}");

            foreach (Transform child in formation.transform)
            {
                Renderer childRenderer = child.GetComponentInChildren<Renderer>(true);
                MeshFilter filter = child.GetComponentInChildren<MeshFilter>(true);
                string meshInfo = filter == null || filter.sharedMesh == null
                    ? "KEIN-MESH"
                    : $"{filter.sharedMesh.name}/v={filter.sharedMesh.vertexCount}";
                string rendererInfo = childRenderer == null
                    ? "KEIN-RENDERER"
                    : $"enabled={childRenderer.enabled}|aktiv={childRenderer.gameObject.activeInHierarchy}|bounds={childRenderer.bounds.size}|mat={(childRenderer.sharedMaterial == null ? "NULL" : childRenderer.sharedMaterial.name)}";
                Debug.Log($"FORMATION|{child.name}|{meshInfo}|{rendererInfo}");
            }

            foreach (string assetPath in new[]
            {
                "Assets/ThirdParty/Ultimate Stylized Nature - May 2022/FBX/Rock_3.fbx",
                "Assets/ThirdParty/Ultimate Stylized Nature - May 2022/FBX/NormalTree_1.fbx"
            })
            {
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                Debug.Log(prefab == null
                    ? $"FORMATION|PREFAB|{assetPath}|NULL"
                    : $"FORMATION|PREFAB|{assetPath}|renderer={prefab.GetComponentsInChildren<Renderer>(true).Length}|kinder={prefab.transform.childCount}");
            }
        }

        public static void Capture()
        {
            // Ohne das rendert der Batch-Mode fehlende Shader-Varianten mit
            // einem Fallback ohne Alpha-Test (Blaetter als schwarze Quads).
            UnityEditor.EditorSettings.asyncShaderCompilation = false;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(OutputFolder);

            GroundShot("01_start_richtung_norden", new Vector2(2f, -47f), new Vector2(0f, -28f));
            GroundShot("02_hauptweg_gabelung", new Vector2(1f, -16f), new Vector2(-8f, -19f));
            GroundShot("03_birkenhain", new Vector2(-4f, -26f), new Vector2(-16f, -31f));
            GroundShot("04_lichtung", new Vector2(-14f, -19f), new Vector2(-27f, -15f));
            GroundShot("05_bruecke_und_bach", new Vector2(2f, -13f), new Vector2(2f, 1f));
            GroundShot("06_nordufer", new Vector2(14f, 9f), new Vector2(34f, 4f));
            SkyShot("07_uebersicht", new Vector3(0f, 70f, -95f), new Vector3(0f, 0f, 0f));
            GroundShot("08_uralter_waechter", new Vector2(-13f, 8f), new Vector2(-14f, 16f));
            GroundShot("09_tempelruine", new Vector2(-33f, 22f), new Vector2(-40f, 28f));
            SkyShot("10_felsformation", new Vector3(58f, 14f, -14f), new Vector3(50f, 1f, -24f));
            GroundShot("11_rastplatz", new Vector2(0f, -21f), new Vector2(5f, -25f));
            GroundShot("12_ueberwucherte_mauer", new Vector2(13f, -7f), new Vector2(17f, -11.5f));
            GroundShot("13_schrein", new Vector2(-36f, 13f), new Vector2(-34.3f, 16.5f));

            Debug.Log($"Screenshots gespeichert nach: {OutputFolder}");
        }

        private static void GroundShot(string name, Vector2 fromXZ, Vector2 toXZ)
        {
            // 3.4 m Augenhoehe: ueber Busch- und Farnhoehe, damit nahe
            // Vegetation nicht die Linse blockiert.
            Vector3 from = new Vector3(fromXZ.x, GroundY(fromXZ) + 3.4f, fromXZ.y);
            Vector3 to = new Vector3(toXZ.x, GroundY(toXZ) + 1.2f, toXZ.y);
            RenderShot(name, from, to);
        }

        private static void SkyShot(string name, Vector3 from, Vector3 to)
        {
            RenderShot(name, from, to);
        }

        private static float GroundY(Vector2 xz)
        {
            Terrain terrain = Terrain.activeTerrain;

            if (terrain == null)
            {
                return 0f;
            }

            return terrain.SampleHeight(new Vector3(xz.x, 0f, xz.y)) +
                   terrain.transform.position.y;
        }

        private static void RenderShot(string name, Vector3 position, Vector3 lookAt)
        {
            GameObject cameraObject = new GameObject("ScreenshotCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 400f;
            cameraObject.transform.position = position;
            cameraObject.transform.LookAt(lookAt);

            RenderTexture renderTexture = new RenderTexture(Width, Height, 24);
            RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();

            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                request.destination = renderTexture;
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            else
            {
                Debug.LogWarning("RenderRequest wird nicht unterstuetzt — Screenshot uebersprungen.");
            }

            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(OutputFolder, $"{name}.png"), texture.EncodeToPNG());

            RenderTexture.active = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraObject);
        }
    }
}



