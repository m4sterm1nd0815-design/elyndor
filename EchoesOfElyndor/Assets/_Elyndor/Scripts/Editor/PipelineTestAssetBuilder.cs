using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Baut das Testasset der Blender-Pipeline aus dem exportierten FBX zu
    /// einem Prefab auf — Importeinstellungen, URP-Material, Collider.
    ///
    /// Das Asset ist ausschliesslich ein Nachweis fuer die Kette
    /// Blender → Export → Unity → QA. Es gehoert in keine Produktionsszene,
    /// und der Builder setzt es auch in keine: er erzeugt ein Prefab und
    /// sonst nichts.
    ///
    /// Warum das Material hier neu gebaut und gar nicht erst aus dem FBX
    /// uebernommen wird: Blender schreibt sein Material in die Datei, aber
    /// ohne Shader. Unity legt daraus ein Material des eingebauten
    /// Standard-Shaders an — und der rendert unter URP magenta. Das faellt
    /// beim Import niemandem auf, sondern erst in der Szene.
    ///
    /// Der erste Versuch hat das FBX-Material ueber
    /// <c>materialLocation = External</c> auf ein URP-Material umgebogen.
    /// Unity 6000.4 meldet dazu „External Material Location is no longer
    /// supported" und legt daneben stillschweigend einen zweiten
    /// Materialordner an. Der Import zieht deshalb jetzt gar kein Material
    /// mehr aus der Datei; das Prefab bekommt sein URP-Material zugewiesen.
    /// Das Blender-Material bleibt Vorlage fuer den Farbwert und sonst nichts.
    /// </summary>
    public static class PipelineTestAssetBuilder
    {
        private const string Folder = "Assets/_Elyndor/Art/_PipelineTest";
        private const string ModelPath = Folder + "/ELY_Test_Rock_A.fbx";
        private const string MaterialPath = Folder + "/M_ELY_Test_Rock_A.mat";
        private const string PrefabPath = Folder + "/ELY_Test_Rock_A.prefab";

        private const string UrpLitShader = "Universal Render Pipeline/Lit";

        [MenuItem("Elyndor/Art/Pipeline-Testasset aufbauen")]
        public static void Build()
        {
            if (!File.Exists(ModelPath))
            {
                throw new FileNotFoundException(
                    $"Pipeline-Testasset: {ModelPath} fehlt. Zuerst aus " +
                    "Blender exportieren.", ModelPath);
            }

            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);

            Material material = BuildMaterial();
            ConfigureImporter();
            BuildPrefab(material);

            AssetDatabase.SaveAssets();
            Debug.Log($"PIPELINE_TEST_ASSET_BUILT: {PrefabPath}");
        }

        /// <summary>Einstiegspunkt fuer den Batchmode: aufbauen und pruefen.</summary>
        public static void BuildAndValidateBatch()
        {
            try
            {
                Build();
                ModelImportValidator.Validate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"PIPELINE_TEST_ASSET fehlgeschlagen: {exception}");
                EditorApplication.Exit(1);
            }
        }

        private static Material BuildMaterial()
        {
            Shader shader = Shader.Find(UrpLitShader);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"Shader '{UrpLitShader}' nicht gefunden. Ohne URP-Shader " +
                    "waere das Material im Spiel magenta.");
            }

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            // Die Werte spiegeln den Principled BSDF aus der .blend:
            // Base Color 0.30/0.31/0.28, Roughness 0.88, Metallic 0.
            material.SetColor("_BaseColor", new Color(0.30f, 0.31f, 0.28f, 1f));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 1f - 0.88f);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureImporter()
        {
            if (AssetImporter.GetAtPath(ModelPath) is not ModelImporter importer)
            {
                throw new InvalidOperationException(
                    $"Kein ModelImporter an {ModelPath}.");
            }

            // 1 Blender-Meter = 1 Unity-Meter. Jede Abweichung hier macht jede
            // spaetere Groessenaussage im Projekt wertlos.
            importer.globalScale = 1f;
            importer.useFileScale = true;

            // Normalen kommen aus der Datei: Blender hat mit
            // mesh_smooth_type='FACE' bereits Glaettungsgruppen geschrieben.
            // Unity nachrechnen zu lassen wuerde die facettierte Optik
            // stillschweigend wegglaetten.
            importer.importNormals = ModelImporterNormals.Import;

            // Bleibt aus — gemessen, nicht vermutet.
            //
            // Blender ist Z-oben, Unity Y-oben. Steht die Umrechnung nicht
            // schon in den Meshdaten, traegt die Importwurzel sie als Rotation:
            // ohne diese Option (270,02, 0, 0), mit ihr (89,98, 0, 0). Unity
            // dreht Mesh und Wurzel um dieselben 180° weiter — das Objekt steht
            // in beiden Faellen richtig, die Wurzel ist in beiden Faellen
            // schief. Die Option loest das Problem also nicht, sie verschiebt es.
            //
            // Geloest wird es beim Export mit 'bake_space_transform=True'.
            // Siehe docs/Technical/BLENDER_ASSET_PIPELINE.md.
            importer.bakeAxisConversion = false;

            // Ohne Normal Map braucht das Material keine Tangenten.
            importer.importTangents = ModelImporterTangents.None;

            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;

            // Kein Lightmap-UV: der Testfels wird nicht gebacken. Fuer ein
            // produktives, statisches Objekt in einer gebackenen Szene waere
            // das eine bewusste Gegenentscheidung.
            importer.generateSecondaryUV = false;

            // Kein Material aus der Datei. Was Blender mitschickt, hat keinen
            // Shader, den URP kennt — es waere ein magentafarbenes Objekt mit
            // einem plausiblen Namen.
            importer.materialImportMode = ModelImporterMaterialImportMode.None;

            // Zuordnungen aus frueheren Importeinstellungen wuerden sonst
            // stehenbleiben und im Inspector als Rest auftauchen.
            foreach (AssetImporter.SourceAssetIdentifier remap
                     in new List<AssetImporter.SourceAssetIdentifier>(
                         importer.GetExternalObjectMap().Keys))
            {
                importer.RemoveRemap(remap);
            }

            importer.SaveAndReimport();
        }

        private static void BuildPrefab(Material material)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"{ModelPath} liess sich nicht laden.");
            }

            GameObject instance = UnityEngine.Object.Instantiate(asset);

            try
            {
                instance.name = "ELY_Test_Rock_A";

                // Nur die Position. Rotation und Skalierung kommen aus dem
                // Import und bleiben stehen: haette der Import sie schief
                // gesetzt, waere genau das der Befund. Wer sie hier
                // geradezieht, versteckt ihn im Prefab.
                instance.transform.position = Vector3.zero;

                foreach (Renderer renderer in
                         instance.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        slots[i] = material;
                    }

                    renderer.sharedMaterials = slots;
                }

                AddFittedBoxCollider(instance);

                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out bool saved);

                if (!saved)
                {
                    throw new InvalidOperationException(
                        $"Prefab {PrefabPath} wurde nicht geschrieben.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// Ein Primitiv statt eines MeshColliders — so steht es im
        /// Asset-Standard. Fuer einen Findling, gegen den man laeuft, ist die
        /// Box die billigste Form, die sich richtig anfuehlt.
        /// </summary>
        private static void AddFittedBoxCollider(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Kein Renderer im Testasset — Collider nicht bestimmbar.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            BoxCollider collider = instance.AddComponent<BoxCollider>();
            collider.center = bounds.center - instance.transform.position;
            collider.size = bounds.size;
        }
    }
}
