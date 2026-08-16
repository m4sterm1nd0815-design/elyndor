using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Prueft importierte Modelle gegen das, was in
    /// <c>docs/Technical/BLENDER_ASSET_PIPELINE.md</c> als Standard steht:
    /// Skalierung, Wurzeltransform, Pivot, Dreiecke, Normalen, UVs,
    /// Materialslots, fehlende Verweise, Rig, Dateigroesse und die
    /// maschinenlesbare Herkunfts-/Lizenzangabe neben dem Asset.
    ///
    /// Der Validator prueft ausdruecklich <b>nicht</b> jedes Modell im Projekt,
    /// sondern nur die unten eingetragenen. Der Bestand aus der Meshy-Zeit ist
    /// vor diesem Standard entstanden; ihn hier pauschal einzusammeln haette
    /// als Erstes das Gate rot gefaerbt, ohne dass jemand die Assets angefasst
    /// haette. Ein Gate, das vom ersten Tag an rot ist, wird ignoriert.
    /// Die Liste waechst deshalb pro Asset, das den Standard nachweislich
    /// erfuellt.
    ///
    /// Der Validator oeffnet keine Szene und schreibt kein Asset. Er
    /// instanziiert das Modell, misst, und raeumt die Instanz wieder weg.
    /// </summary>
    public static class ModelImportValidator
    {
        /// <summary>Toleranz fuer Werte, die exakt sein sollen (Meter bzw. Einheiten).</summary>
        private const float Epsilon = 0.001f;

        /// <summary>
        /// Ein Modell, das den Standard erfuellen muss, mit seinen Grenzen.
        /// Die Grenzen stehen bewusst am Asset und nicht global: ein Findling
        /// und eine Landmarke haben nichts gemeinsam ausser dem Dateiformat.
        /// </summary>
        private readonly struct RegisteredModel
        {
            public readonly string AssetPath;
            public readonly string PrefabPath;
            public readonly int MaxTriangles;
            public readonly int MaxMaterialSlots;
            public readonly long MaxFileBytes;
            public readonly float MinHeightMeters;
            public readonly float MaxHeightMeters;
            public readonly bool StaticProp;

            public RegisteredModel(
                string assetPath,
                string prefabPath,
                int maxTriangles,
                int maxMaterialSlots,
                long maxFileBytes,
                float minHeightMeters,
                float maxHeightMeters,
                bool staticProp)
            {
                AssetPath = assetPath;
                PrefabPath = prefabPath;
                MaxTriangles = maxTriangles;
                MaxMaterialSlots = maxMaterialSlots;
                MaxFileBytes = maxFileBytes;
                MinHeightMeters = minHeightMeters;
                MaxHeightMeters = maxHeightMeters;
                StaticProp = staticProp;
            }
        }

        private static IReadOnlyList<RegisteredModel> Registered => new[]
        {
            new RegisteredModel(
                "Assets/_Elyndor/Art/_PipelineTest/ELY_Test_Rock_A.fbx",
                "Assets/_Elyndor/Art/_PipelineTest/ELY_Test_Rock_A.prefab",
                maxTriangles: 500,
                maxMaterialSlots: 1,
                maxFileBytes: 256 * 1024,
                minHeightMeters: 0.5f,
                maxHeightMeters: 1.5f,
                staticProp: true),
        };

        [MenuItem("Elyndor/QA/Validate Model Imports")]
        public static void Validate()
        {
            string report = Run(out int errors);

            if (errors > 0)
            {
                Debug.LogError(report);
                throw new InvalidOperationException(
                    $"Model import validation failed with {errors} errors.");
            }

            Debug.Log($"MODEL_IMPORT_VALIDATION_OK\n{report}");
        }

        private static string Run(out int errors)
        {
            StringBuilder report = new StringBuilder();
            errors = 0;

            foreach (RegisteredModel model in Registered)
            {
                report.AppendLine($"--- {model.AssetPath}");
                errors += Check(model, report);
            }

            return report.ToString();
        }

        private static int Check(RegisteredModel model, StringBuilder report)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(model.AssetPath) == null)
            {
                report.AppendLine("  FEHLER: Modell nicht gefunden.");
                return 1;
            }

            // Die Herkunft wird vor allem anderen geprueft und ausdruecklich
            // auch dann, wenn spaeter etwas anderes fehlt: Ob wir ein Asset
            // benutzen duerfen, haengt nicht daran, ob sein Prefab schon
            // gebaut ist.
            int errors = CheckProvenance(model, report);

            // Geprueft wird das Prefab, nicht die rohe Datei. Ausgeliefert wird
            // das Prefab; alles, was erst dort entsteht — Material, Collider —
            // waere an der Datei gemessen unsichtbar.
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(model.PrefabPath);

            if (prefab == null)
            {
                report.AppendLine(
                    $"  FEHLER: Prefab {model.PrefabPath} nicht gefunden.");
                return errors + 1;
            }

            errors += CheckImporter(model, report);
            errors += CheckFileSize(model, report);

            ReportImportedModelRoot(model, report);

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                // Nur die Position wird gesetzt. Rotation und Skalierung bleiben
                // stehen, wie das Prefab sie mitbringt — genau die will der
                // Validator ja messen. Eine erste Fassung hat sie hier auf
                // Identitaet gezwungen und damit die Achsdrehung aus dem Import
                // ueberschrieben, bevor irgendetwas sie pruefen konnte. Der
                // Bericht war danach sauber und der Fels lag auf dem Ruecken.
                instance.transform.position = Vector3.zero;

                errors += CheckHierarchy(instance, report);
                errors += CheckMesh(model, instance, report);
                errors += CheckBounds(model, instance, report);
                errors += CheckMaterials(model, instance, report);
                errors += CheckRig(model, instance, report);
                ReportColliderSuitability(instance, report);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            return errors;
        }

        /// <summary>
        /// Was Unity aus der Datei gemacht hat, bevor irgendein Prefab-Schritt
        /// daran war. Ohne diese Zeile laesst sich hinterher nicht sagen, ob
        /// eine schiefe Achse aus dem Export oder aus dem Prefab-Bau stammt.
        /// </summary>
        private static void ReportImportedModelRoot(
            RegisteredModel model, StringBuilder report)
        {
            GameObject asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(model.AssetPath);

            if (asset == null)
            {
                return;
            }

            Transform root = asset.transform;
            report.AppendLine(
                $"  Importwurzel roh: rot={root.localRotation.eulerAngles} " +
                $"scale={root.localScale} kinder={root.childCount}");

            foreach (Transform child in root)
            {
                report.AppendLine(
                    $"    '{child.name}': rot={child.localRotation.eulerAngles} " +
                    $"scale={child.localScale}");
            }
        }

        /// <summary>
        /// Herkunft und Lizenz aus der Sidecar-Datei neben dem Asset. Der
        /// Scope bleibt bewusst die Liste oben: Der Bestand aus der Meshy- und
        /// ThirdParty-Zeit hat diese Datei nicht, und ihn hier einzusammeln
        /// haette dasselbe rote Gate am ersten Tag ergeben, das der Validator
        /// von Anfang an vermeidet. Wer ein Altasset auf den neuen Standard
        /// hebt, traegt es oben ein und liefert die Datei mit.
        /// </summary>
        private static int CheckProvenance(RegisteredModel model, StringBuilder report)
        {
            IReadOnlyList<string> problems =
                AssetProvenance.ValidateFile(model.AssetPath, out string summary);

            report.AppendLine(
                $"  Herkunft ({AssetProvenance.SidecarPathFor(model.AssetPath)}): " +
                summary);

            foreach (string problem in problems)
            {
                report.AppendLine($"  FEHLER: Herkunft — {problem}");
            }

            return problems.Count;
        }

        private static int CheckImporter(RegisteredModel model, StringBuilder report)
        {
            if (AssetImporter.GetAtPath(model.AssetPath) is not ModelImporter importer)
            {
                report.AppendLine("  FEHLER: kein ModelImporter am Pfad.");
                return 1;
            }

            int errors = 0;

            report.AppendLine(
                $"  Importer: globalScale={importer.globalScale} " +
                $"useFileScale={importer.useFileScale} " +
                $"normals={importer.importNormals} " +
                $"tangents={importer.importTangents} " +
                $"animationType={importer.animationType}");

            if (Mathf.Abs(importer.globalScale - 1f) > Epsilon)
            {
                report.AppendLine(
                    $"  FEHLER: Scale Factor ist {importer.globalScale}, " +
                    "erwartet 1. Ein abweichender Faktor verschiebt jede " +
                    "spaetere Groessenaussage.");
                errors++;
            }

            if (!importer.useFileScale)
            {
                report.AppendLine(
                    "  FEHLER: Convert Units ist aus. Blender exportiert in " +
                    "Metern; ohne Umrechnung stimmt die Groesse nur zufaellig.");
                errors++;
            }

            // Ein leerer Remap-Eintrag ist der Fall, den Unity im Inspector
            // stumm als graues Feld zeigt: der Import erwartet ein Material
            // oder eine Textur, findet sie aber nicht.
            foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> remap
                     in importer.GetExternalObjectMap())
            {
                if (remap.Value != null)
                {
                    continue;
                }

                report.AppendLine(
                    $"  FEHLER: unaufgeloester Verweis {remap.Key.type.Name} " +
                    $"'{remap.Key.name}'.");
                errors++;
            }

            return errors;
        }

        private static int CheckFileSize(RegisteredModel model, StringBuilder report)
        {
            FileInfo file = new FileInfo(model.AssetPath);
            if (!file.Exists)
            {
                report.AppendLine("  FEHLER: Quelldatei fehlt auf der Platte.");
                return 1;
            }

            report.AppendLine($"  Dateigroesse: {file.Length} Bytes");

            if (file.Length <= model.MaxFileBytes)
            {
                return 0;
            }

            report.AppendLine(
                $"  FEHLER: Dateigroesse ueber Budget " +
                $"({file.Length} > {model.MaxFileBytes}).");
            return 1;
        }

        private static int CheckHierarchy(GameObject instance, StringBuilder report)
        {
            int errors = 0;

            // Die Wurzel wird mitgeprueft. Sie ist der Ort, an dem eine nicht
            // umgerechnete Achse landet.
            foreach (Transform transform in instance.GetComponentsInChildren<Transform>(true))
            {
                if (Quaternion.Angle(transform.localRotation, Quaternion.identity) > 0.01f)
                {
                    report.AppendLine(
                        $"  FEHLER: '{transform.name}' bringt eine Rotation " +
                        $"{transform.localRotation.eulerAngles} mit. Eine " +
                        "eingebackene Achsdrehung faellt erst auf, wenn jemand " +
                        "das Prefab dreht.");
                    errors++;
                }

                if ((transform.localScale - Vector3.one).sqrMagnitude > Epsilon * Epsilon)
                {
                    report.AppendLine(
                        $"  FEHLER: '{transform.name}' hat Scale " +
                        $"{transform.localScale}, erwartet 1.");
                    errors++;
                }
            }

            foreach (Component component in instance.GetComponentsInChildren<Component>(true))
            {
                if (component != null)
                {
                    continue;
                }

                report.AppendLine("  FEHLER: fehlendes Script in der Hierarchie.");
                errors++;
            }

            return errors;
        }

        private static int CheckMesh(
            RegisteredModel model, GameObject instance, StringBuilder report)
        {
            MeshFilter[] filters = instance.GetComponentsInChildren<MeshFilter>(true);

            if (filters.Length == 0)
            {
                report.AppendLine("  FEHLER: kein Mesh im Modell.");
                return 1;
            }

            int errors = 0;
            int triangles = 0;
            int vertices = 0;

            foreach (MeshFilter filter in filters)
            {
                Mesh mesh = filter.sharedMesh;

                if (mesh == null)
                {
                    report.AppendLine($"  FEHLER: '{filter.name}' ohne Mesh.");
                    errors++;
                    continue;
                }

                triangles += mesh.triangles.Length / 3;
                vertices += mesh.vertexCount;

                errors += CheckNormals(mesh, report);
                errors += CheckUv(mesh, report);
            }

            report.AppendLine($"  Dreiecke: {triangles} | Vertices: {vertices}");

            if (triangles > model.MaxTriangles)
            {
                report.AppendLine(
                    $"  FEHLER: {triangles} Dreiecke ueber Budget " +
                    $"({model.MaxTriangles}).");
                errors++;
            }

            return errors;
        }

        private static int CheckNormals(Mesh mesh, StringBuilder report)
        {
            Vector3[] normals = mesh.normals;

            if (normals == null || normals.Length != mesh.vertexCount)
            {
                report.AppendLine(
                    $"  FEHLER: '{mesh.name}' hat keine Normalen. Unity " +
                    "beleuchtet das Modell dann nach geratenen Werten.");
                return 1;
            }

            int broken = 0;
            foreach (Vector3 normal in normals)
            {
                if (Mathf.Abs(normal.sqrMagnitude - 1f) > 0.01f)
                {
                    broken++;
                }
            }

            if (broken == 0)
            {
                report.AppendLine($"  Normalen: {normals.Length}, alle normiert");
                return 0;
            }

            report.AppendLine(
                $"  FEHLER: {broken} Normalen sind nicht auf Laenge 1.");
            return 1;
        }

        private static int CheckUv(Mesh mesh, StringBuilder report)
        {
            Vector2[] uv = mesh.uv;

            if (uv == null || uv.Length == 0)
            {
                report.AppendLine(
                    $"  FEHLER: '{mesh.name}' hat keine UV0. Jedes spaetere " +
                    "Material mit Textur waere damit blind.");
                return 1;
            }

            float minU = float.MaxValue, maxU = float.MinValue;
            float minV = float.MaxValue, maxV = float.MinValue;

            foreach (Vector2 point in uv)
            {
                minU = Mathf.Min(minU, point.x);
                maxU = Mathf.Max(maxU, point.x);
                minV = Mathf.Min(minV, point.y);
                maxV = Mathf.Max(maxV, point.y);
            }

            report.AppendLine(
                $"  UV0: {uv.Length} Punkte, u [{minU:F3}..{maxU:F3}] " +
                $"v [{minV:F3}..{maxV:F3}]");
            return 0;
        }

        private static int CheckBounds(
            RegisteredModel model, GameObject instance, StringBuilder report)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                report.AppendLine("  FEHLER: kein Renderer im Modell.");
                return 1;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            report.AppendLine(
                $"  Weltmasse bei Identitaet: {bounds.size} " +
                $"| min {bounds.min} | max {bounds.max}");

            int errors = 0;

            // Pivot am Boden: das Objekt darf beim Absetzen auf y=0 weder
            // schweben noch im Boden stecken.
            if (Mathf.Abs(bounds.min.y) > 0.01f)
            {
                report.AppendLine(
                    $"  FEHLER: Pivot nicht am Boden — Unterkante liegt bei " +
                    $"y={bounds.min.y:F4} statt 0.");
                errors++;
            }

            if (bounds.size.y < model.MinHeightMeters ||
                bounds.size.y > model.MaxHeightMeters)
            {
                report.AppendLine(
                    $"  FEHLER: Hoehe {bounds.size.y:F3} m ausserhalb " +
                    $"[{model.MinHeightMeters}..{model.MaxHeightMeters}] m.");
                errors++;
            }

            return errors;
        }

        private static int CheckMaterials(
            RegisteredModel model, GameObject instance, StringBuilder report)
        {
            int errors = 0;
            int slots = 0;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                slots += materials.Length;

                foreach (Material material in materials)
                {
                    if (material == null)
                    {
                        report.AppendLine(
                            $"  FEHLER: '{renderer.name}' hat einen leeren " +
                            "Materialslot.");
                        errors++;
                        continue;
                    }

                    report.AppendLine(
                        $"  Material: '{material.name}' " +
                        $"Shader '{material.shader.name}'");

                    if (material.shader == null ||
                        material.shader.name == "Hidden/InternalErrorShader")
                    {
                        report.AppendLine(
                            $"  FEHLER: '{material.name}' hat keinen gueltigen " +
                            "Shader.");
                        errors++;
                        continue;
                    }

                    // Das Projekt rendert mit URP. Ein Material des eingebauten
                    // Standard-Shaders faellt beim Import nicht auf — es
                    // erscheint erst in der Szene, und dort magenta.
                    if (!material.shader.name.StartsWith(
                            "Universal Render Pipeline/", StringComparison.Ordinal))
                    {
                        report.AppendLine(
                            $"  FEHLER: '{material.name}' benutzt " +
                            $"'{material.shader.name}' statt eines " +
                            "URP-Shaders.");
                        errors++;
                    }
                }
            }

            if (slots > model.MaxMaterialSlots)
            {
                report.AppendLine(
                    $"  FEHLER: {slots} Materialslots ueber Budget " +
                    $"({model.MaxMaterialSlots}).");
                errors++;
            }

            return errors;
        }

        private static int CheckRig(
            RegisteredModel model, GameObject instance, StringBuilder report)
        {
            var clips = new List<string>();

            foreach (UnityEngine.Object sub in AssetDatabase.LoadAllAssetsAtPath(model.AssetPath))
            {
                if (sub is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    clips.Add(clip.name);
                }
            }

            bool skinned =
                instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0;
            bool animator =
                instance.GetComponentsInChildren<Animator>(true).Length > 0;

            report.AppendLine(
                $"  Rig: skinned={skinned} animator={animator} " +
                $"clips={clips.Count}");

            if (!model.StaticProp)
            {
                return 0;
            }

            int errors = 0;

            if (skinned || animator)
            {
                report.AppendLine(
                    "  FEHLER: als statisches Objekt eingetragen, bringt aber " +
                    "ein Rig mit.");
                errors++;
            }

            if (clips.Count > 0)
            {
                report.AppendLine(
                    $"  FEHLER: als statisches Objekt eingetragen, bringt aber " +
                    $"{clips.Count} Animationsclips mit.");
                errors++;
            }

            return errors;
        }

        /// <summary>
        /// Reine Auskunft, kein Gate. Ob ein Objekt einen Collider braucht und
        /// welchen, ist eine Gameplay-Entscheidung — der Validator darf sie
        /// nicht treffen, aber er kann sagen, was moeglich ist.
        /// </summary>
        private static void ReportColliderSuitability(
            GameObject instance, StringBuilder report)
        {
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            int triangles = 0;

            foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    triangles += filter.sharedMesh.triangles.Length / 3;
                }
            }

            // Ein konvexer MeshCollider ist in Unity auf 255 Dreiecke begrenzt.
            report.AppendLine(
                $"  Collider im Modell: {colliders.Length} | " +
                $"konvexer MeshCollider moeglich: {triangles <= 255} " +
                $"({triangles} Dreiecke, Grenze 255)");
        }
    }
}
