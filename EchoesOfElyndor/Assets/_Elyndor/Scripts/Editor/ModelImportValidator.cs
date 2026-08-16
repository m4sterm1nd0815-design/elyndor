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
        /// Was ein gerigtes Asset zusaetzlich erfuellen muss.
        ///
        /// Statische Objekte und Figuren teilen sich fast alle Pruefungen —
        /// Skalierung, Pivot, Normalen, UVs, Material. Was sie nicht teilen,
        /// steht hier: ein Skelett hat eine erwartete Groesse, Clips haben
        /// gemessene Dauern, und eine Figur hat eine Blickrichtung. Gerade die
        /// Blickrichtung ist der Fehler, der beim Import nicht auffaellt und
        /// im Spiel als rueckwaerts laufender Gegner endet.
        /// </summary>
        private sealed class RigExpectation
        {
            /// <summary>Erwartete Knochenzahl.</summary>
            public int Bones;

            /// <summary>Name des Wurzelknochens am SkinnedMeshRenderer.</summary>
            public string RootBone;

            /// <summary>Clipname ohne Praefix auf die Dauer in Sekunden.</summary>
            public IReadOnlyDictionary<string, float> Clips;

            /// <summary>
            /// Zwei Knochen, deren Verbindung nach vorn zeigen muss. In Unity
            /// ist vorn +Z.
            /// </summary>
            public string ForwardFromBone;
            public string ForwardToBone;
        }

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

            /// <summary>Null bei statischen Objekten.</summary>
            public readonly RigExpectation Rig;

            public RegisteredModel(
                string assetPath,
                string prefabPath,
                int maxTriangles,
                int maxMaterialSlots,
                long maxFileBytes,
                float minHeightMeters,
                float maxHeightMeters,
                bool staticProp,
                RigExpectation rig = null)
            {
                AssetPath = assetPath;
                PrefabPath = prefabPath;
                MaxTriangles = maxTriangles;
                MaxMaterialSlots = maxMaterialSlots;
                MaxFileBytes = maxFileBytes;
                MinHeightMeters = minHeightMeters;
                MaxHeightMeters = maxHeightMeters;
                StaticProp = staticProp;
                Rig = rig;
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

            // Der Wurzelstreifer ist das erste gerigte Asset nach diesem
            // Standard. Die Hoehengrenzen sind die Schulterhoehe aus dem
            // freigegebenen Konzeptentwurf; der hoechste Punkt des Modells ist
            // die Schulter. Die Clipdauern stehen ebenfalls dort und sind der
            // Grund, warum der Blender-Aufbau mit 50 Bildern je Sekunde
            // arbeitet: 0,7 s, 0,18 s und 0,8 s gehen damit glatt auf.
            //
            // Das Dreiecksbudget ist die dokumentierte Obergrenze aus
            // FINSTERWALD_ASSET_REQUIREMENTS.md. Das Modell liegt weit
            // darunter — die Pipeline verlangt die niedrigste Zahl, bei der
            // die Silhouette liest, nicht die hoechste, die das Budget
            // hergibt.
            new RegisteredModel(
                "Assets/_Elyndor/Art/Enemy/Finsterwald/ELY_Enemy_Wurzelstreifer/ELY_Enemy_Wurzelstreifer.fbx",
                "Assets/_Elyndor/Prefabs/Enemies/Wurzelstreifer.prefab",
                maxTriangles: 20000,
                maxMaterialSlots: 2,
                maxFileBytes: 2 * 1024 * 1024,
                minHeightMeters: 0.85f,
                maxHeightMeters: 0.95f,
                staticProp: false,
                rig: new RigExpectation
                {
                    Bones = 24,
                    RootBone = "Wurzel",
                    ForwardFromBone = "Becken",
                    ForwardToBone = "Kopf",
                    Clips = new Dictionary<string, float>(StringComparer.Ordinal)
                    {
                        { "Idle", 2.40f },
                        { "Lauschen", 2.00f },
                        { "Schritt", 1.00f },
                        { "Trab", 1.00f },
                        { "Lauf", 0.60f },
                        { "Telegraph", 0.70f },
                        { "Sprungbiss", 0.50f },
                        { "Flinch", 0.18f },
                        { "Stagger", 0.80f },
                        { "Flucht", 0.70f },
                        { "Niederlage", 1.40f },
                    },
                }),
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

                errors += CheckHierarchy(model, instance, report);
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

        private static int CheckHierarchy(
            RegisteredModel model, GameObject instance, StringBuilder report)
        {
            int errors = 0;

            // Knochen duerfen Rotationen tragen — eine Ruhepose besteht daraus.
            // Das Armature-Objekt selbst ebenfalls: Unity importiert ein
            // Blender-Skelett grundsaetzlich mit der Achsumrechnung als
            // Rotation, gemessen (270.02, 0, 0), und zwar unabhaengig davon,
            // ob beim Export bake_space_transform gesetzt war.
            //
            // Was dadurch nicht ungeprueft bleiben darf, ist das Mesh: genau
            // dort entscheidet sich, ob die Achsumrechnung in den Meshdaten
            // steckt oder als schiefe Wurzel mitgeschleppt wird. Der Unter-
            // schied zwischen beiden Exportvarianten war am 16.08.2026 genau
            // diese eine Rotation.
            HashSet<Transform> rigTransforms = CollectRigTransforms(instance);

            foreach (Transform transform in instance.GetComponentsInChildren<Transform>(true))
            {
                bool isRig = rigTransforms.Contains(transform);

                if (Quaternion.Angle(transform.localRotation, Quaternion.identity) > 0.01f)
                {
                    if (isRig)
                    {
                        report.AppendLine(
                            $"  Rig-Transform '{transform.name}': rot=" +
                            $"{transform.localRotation.eulerAngles} (zulaessig)");
                    }
                    else
                    {
                        report.AppendLine(
                            $"  FEHLER: '{transform.name}' bringt eine Rotation " +
                            $"{transform.localRotation.eulerAngles} mit. Eine " +
                            "eingebackene Achsdrehung faellt erst auf, wenn jemand " +
                            "das Prefab dreht.");
                        errors++;
                    }
                }

                // Die Skalierung wird ueberall geprueft, auch an Knochen. Ein
                // Knochen mit Skalierung ungleich 1 ist kein Gestaltungsmittel,
                // sondern ein nicht angewendeter Transform aus der Quelldatei.
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

        /// <summary>
        /// Alle Meshes der Instanz mit dem Namen des Objekts, an dem sie
        /// haengen — gehaeutete wie starre.
        ///
        /// Ein gehaeutetes Mesh haengt an einem <see cref="SkinnedMeshRenderer"/>
        /// und hat keinen <see cref="MeshFilter"/>. Eine Pruefung, die nur
        /// nach MeshFilter sucht, meldet bei einer Figur "kein Mesh im Modell"
        /// — und das ist die eine Fehlermeldung, die niemand ernst nimmt, weil
        /// das Modell sichtbar da ist.
        /// </summary>
        private static List<(string Name, Mesh Mesh)> CollectMeshes(
            GameObject instance)
        {
            var meshes = new List<(string, Mesh)>();

            foreach (MeshFilter filter in
                     instance.GetComponentsInChildren<MeshFilter>(true))
            {
                meshes.Add((filter.name, filter.sharedMesh));
            }

            foreach (SkinnedMeshRenderer skin in
                     instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                meshes.Add((skin.name, skin.sharedMesh));
            }

            return meshes;
        }

        /// <summary>
        /// Das Armature-Objekt und alle Knochen darunter.
        ///
        /// Ermittelt ueber die Knochenliste der gehaeuteten Renderer und deren
        /// Elternkette. Ueber Namen zu gehen waere die naheliegende Abkuerzung
        /// und die falsche: dann haette jedes Objekt, das jemand "Rig" nennt,
        /// eine Ausnahme von der Rotationspruefung.
        /// </summary>
        private static HashSet<Transform> CollectRigTransforms(GameObject instance)
        {
            var rig = new HashSet<Transform>();
            Transform root = instance.transform;

            foreach (SkinnedMeshRenderer skin in
                     instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                foreach (Transform bone in skin.bones)
                {
                    for (Transform current = bone;
                         current != null && current != root;
                         current = current.parent)
                    {
                        rig.Add(current);
                    }
                }
            }

            return rig;
        }

        private static int CheckMesh(
            RegisteredModel model, GameObject instance, StringBuilder report)
        {
            List<(string Name, Mesh Mesh)> meshes = CollectMeshes(instance);

            if (meshes.Count == 0)
            {
                report.AppendLine("  FEHLER: kein Mesh im Modell.");
                return 1;
            }

            int errors = 0;
            int triangles = 0;
            int vertices = 0;

            foreach ((string name, Mesh mesh) in meshes)
            {
                if (mesh == null)
                {
                    report.AppendLine($"  FEHLER: '{name}' ohne Mesh.");
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

            // Bei gehaeuteten Meshes wird die Ruhepose gemessen, nicht
            // Renderer.bounds.
            //
            // Unity legt die Grenzen eines SkinnedMeshRenderer bewusst
            // grosszuegig aus, damit ein animiertes Modell nicht aus seinem
            // eigenen Culling faellt. Beim Wurzelstreifer sind das 1,21 m Hoehe
            // statt 0,93 m, und die Unterkante liegt 10 cm unter dem Boden.
            // Gegen diese Zahlen geprueft, meldete die Pivotpruefung ein
            // schwebendes Objekt und die Hoehenpruefung ein zu grosses — beides
            // falsch. Das Mesh selbst kennt seine Ruhepose genau.
            bool skinned = false;
            Bounds bounds = default;

            foreach (SkinnedMeshRenderer skin in
                     instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skin.sharedMesh == null)
                {
                    continue;
                }

                Bounds local = skin.sharedMesh.bounds;
                Bounds world = new Bounds(
                    skin.transform.TransformPoint(local.center),
                    Vector3.Scale(local.size, skin.transform.lossyScale));

                if (!skinned)
                {
                    bounds = world;
                    skinned = true;
                }
                else
                {
                    bounds.Encapsulate(world);
                }
            }

            if (!skinned)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            report.AppendLine(
                $"  {(skinned ? "Ruhepose" : "Weltmasse bei Identitaet")}: " +
                $"{bounds.size} | min {bounds.min} | max {bounds.max}");

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

                // Ins Budget zaehlen nur die Renderer des Modells. Ein
                // Partikelsystem im Prefab — beim Wurzelstreifer der
                // Rindenstaub beim Treffer — hat sein eigenes Material und
                // gehoert nicht zum Materialbudget der Silhouette; das Budget
                // begrenzt Drawcalls des Modells, nicht die Gameplay-VFX
                // daneben.
                //
                // Auf leere Slots und Nicht-URP-Shader wird trotzdem jeder
                // Renderer geprueft. Ein leerer Slot rendert magenta,
                // gleichgueltig woran er haengt.
                bool countsTowardBudget =
                    renderer is MeshRenderer or SkinnedMeshRenderer;

                if (countsTowardBudget)
                {
                    slots += materials.Length;
                }

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
                return CheckRiggedAsset(model, instance, clips, report);
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
        /// Was eine Figur mitbringen muss: ein Skelett der erwarteten Groesse,
        /// einen gueltigen Avatar, die Clips mit ihren gemessenen Dauern — und
        /// eine Blickrichtung nach vorn.
        /// </summary>
        private static int CheckRiggedAsset(
            RegisteredModel model,
            GameObject instance,
            IReadOnlyList<string> clipNames,
            StringBuilder report)
        {
            RigExpectation expected = model.Rig;

            if (expected == null)
            {
                report.AppendLine(
                    "  FEHLER: als Figur eingetragen, aber ohne Rig-Erwartung. " +
                    "Ein Asset, dessen Sollwerte niemand aufgeschrieben hat, " +
                    "laesst sich nicht pruefen.");
                return 1;
            }

            int errors = 0;

            SkinnedMeshRenderer[] skins =
                instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (skins.Length == 0)
            {
                report.AppendLine(
                    "  FEHLER: als Figur eingetragen, aber ohne gehaeutetes " +
                    "Mesh.");
                return errors + 1;
            }

            foreach (SkinnedMeshRenderer skin in skins)
            {
                report.AppendLine(
                    $"  Skin '{skin.name}': bones={skin.bones.Length} " +
                    $"rootBone={(skin.rootBone != null ? skin.rootBone.name : "<fehlt>")}");

                if (skin.bones.Length != expected.Bones)
                {
                    report.AppendLine(
                        $"  FEHLER: {skin.bones.Length} Knochen, erwartet " +
                        $"{expected.Bones}. Eine abweichende Knochenzahl heisst, " +
                        "dass Rig und Clips nicht mehr zueinander passen.");
                    errors++;
                }

                if (skin.rootBone == null ||
                    skin.rootBone.name != expected.RootBone)
                {
                    report.AppendLine(
                        $"  FEHLER: Wurzelknochen ist " +
                        $"'{(skin.rootBone != null ? skin.rootBone.name : "<fehlt>")}', " +
                        $"erwartet '{expected.RootBone}'.");
                    errors++;
                }

                foreach (Transform bone in skin.bones)
                {
                    if (bone == null)
                    {
                        report.AppendLine(
                            "  FEHLER: ein Knochen des Skins fehlt. Das Mesh " +
                            "wuerde an dieser Stelle zusammenfallen.");
                        errors++;
                    }
                }
            }

            errors += CheckAvatar(model, report);
            errors += CheckClips(expected, clipNames, model, report);
            errors += CheckForward(expected, instance, report);

            return errors;
        }

        private static int CheckAvatar(RegisteredModel model, StringBuilder report)
        {
            foreach (UnityEngine.Object sub in
                     AssetDatabase.LoadAllAssetsAtPath(model.AssetPath))
            {
                if (sub is not Avatar avatar)
                {
                    continue;
                }

                report.AppendLine(
                    $"  Avatar: '{avatar.name}' isValid={avatar.isValid}");

                if (avatar.isValid)
                {
                    return 0;
                }

                report.AppendLine(
                    "  FEHLER: Avatar ist ungueltig. Ohne ihn spielt der " +
                    "Animator keinen einzigen Clip ab.");
                return 1;
            }

            report.AppendLine(
                "  FEHLER: kein Avatar im Modell. Die Clips waeren nicht " +
                "abspielbar.");
            return 1;
        }

        private static int CheckClips(
            RigExpectation expected,
            IReadOnlyList<string> clipNames,
            RegisteredModel model,
            StringBuilder report)
        {
            var lengths = new Dictionary<string, float>(StringComparer.Ordinal);

            foreach (UnityEngine.Object sub in
                     AssetDatabase.LoadAllAssetsAtPath(model.AssetPath))
            {
                if (sub is AnimationClip clip &&
                    !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    int separator = clip.name.LastIndexOf('|');
                    string shortName = separator >= 0
                        ? clip.name.Substring(separator + 1)
                        : clip.name;

                    lengths[shortName] = clip.length;
                }
            }

            int errors = 0;

            foreach (KeyValuePair<string, float> wanted in expected.Clips)
            {
                if (!lengths.TryGetValue(wanted.Key, out float length))
                {
                    report.AppendLine(
                        $"  FEHLER: Clip '{wanted.Key}' fehlt im Modell.");
                    errors++;
                    continue;
                }

                // Eine halbe Bildlaenge bei 50 Bildern je Sekunde. Die Dauern
                // stehen im Konzeptentwurf auf die Hundertstelsekunde; eine
                // groessere Toleranz wuerde genau den Unterschied durchlassen,
                // wegen dem die Bildrate auf 50 gesetzt wurde.
                if (Mathf.Abs(length - wanted.Value) > 0.011f)
                {
                    report.AppendLine(
                        $"  FEHLER: Clip '{wanted.Key}' dauert " +
                        $"{length:F3} s, erwartet {wanted.Value:F3} s.");
                    errors++;
                }
            }

            foreach (string name in lengths.Keys)
            {
                if (!expected.Clips.ContainsKey(name))
                {
                    report.AppendLine(
                        $"  FEHLER: unerwarteter Clip '{name}' im Modell. " +
                        "Ein Clip, den niemand eingetragen hat, ist entweder " +
                        "ueberfluessig oder die Liste ist veraltet.");
                    errors++;
                }
            }

            report.AppendLine(
                $"  Clips: {lengths.Count} gefunden, {expected.Clips.Count} erwartet");

            return errors;
        }

        /// <summary>
        /// Zeigt die Figur nach vorn?
        ///
        /// In Unity ist vorn +Z. Blender ist Z-oben mit -Y nach vorn, und der
        /// Standardexport dreht Blenders +Y auf Unitys -Z. Ein Modell, das in
        /// Blender nach +Y schaut, kommt damit rueckwaerts an: es steht
        /// richtig da, hat die richtige Groesse, den richtigen Pivot und
        /// gueltige Clips — und laeuft im Spiel rueckwaerts. Genau das ist am
        /// 16.08.2026 beim ersten Export dieses Assets passiert und keiner der
        /// anderen Pruefungen aufgefallen.
        /// </summary>
        private static int CheckForward(
            RigExpectation expected, GameObject instance, StringBuilder report)
        {
            Transform from = FindBone(instance.transform, expected.ForwardFromBone);
            Transform to = FindBone(instance.transform, expected.ForwardToBone);

            if (from == null || to == null)
            {
                report.AppendLine(
                    $"  FEHLER: Knochen '{expected.ForwardFromBone}' oder " +
                    $"'{expected.ForwardToBone}' fehlt; Blickrichtung nicht " +
                    "pruefbar.");
                return 1;
            }

            Vector3 spine = to.position - from.position;

            report.AppendLine(
                $"  Blickrichtung {expected.ForwardFromBone}->" +
                $"{expected.ForwardToBone}: {spine.ToString("F3")}");

            if (spine.z > 0.05f && Mathf.Abs(spine.z) > Mathf.Abs(spine.x))
            {
                return 0;
            }

            report.AppendLine(
                "  FEHLER: die Figur schaut nicht nach +Z. In Unity ist das " +
                "hinten oder zur Seite — der Gegner wuerde sich falsch herum " +
                "bewegen.");
            return 1;
        }

        private static Transform FindBone(Transform root, string name)
        {
            foreach (Transform transform in
                     root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == name)
                {
                    return transform;
                }
            }

            return null;
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

            foreach ((string _, Mesh mesh) in CollectMeshes(instance))
            {
                if (mesh != null)
                {
                    triangles += mesh.triangles.Length / 3;
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
