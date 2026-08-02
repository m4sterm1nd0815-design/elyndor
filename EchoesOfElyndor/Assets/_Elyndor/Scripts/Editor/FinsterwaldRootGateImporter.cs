using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Elyndor.EditorTools
{
    public static class FinsterwaldRootGateImporter
    {
        public const string ModelPath = "Assets/_Elyndor/Art/Environment/Finsterwald/RootGate/Finsterwald_RootGate_Meshy.fbx";
        public const string PrefabPath = "Assets/_Elyndor/Art/Environment/Finsterwald/RootGate/Finsterwald_RootGate.prefab";
        public const string MaterialPath = "Assets/_Elyndor/Art/Environment/Finsterwald/RootGate/Materials/Finsterwald_RootGate_Stylized.mat";
        public const string AlbedoPath = "Assets/_Elyndor/Art/Environment/Finsterwald/RootGate/Textures/Finsterwald_RootGate_Albedo.png";

        private const int AlbedoResolution = 512;

        [MenuItem("Elyndor/World Visuals/Import Finsterwald Root Gate")]
        public static void ImportAndCreatePrefab()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(ModelPath) is not ModelImporter importer)
                throw new InvalidOperationException($"Root Gate is not importable as a model: {ModelPath}");

            importer.globalScale = 1f;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            // The source FBX only provides an unusable internal fallback material.
            // Keep the model import independent from embedded material extraction and
            // assign the reviewed URP material explicitly to the production prefab.
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();

            Material rootGateMaterial = CreateOrUpdateMaterial();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject instance = model == null ? null : PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Unity could not instantiate the Root Gate model.");

            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                Mesh[] meshes = instance.GetComponentsInChildren<MeshFilter>(true)
                    .Select(filter => filter.sharedMesh)
                    .Concat(instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                        .Select(renderer => renderer.sharedMesh))
                    .Where(mesh => mesh != null).Distinct().ToArray();
                if (renderers.Length == 0 || meshes.Length == 0)
                    throw new InvalidOperationException("Root Gate contains no renderable meshes.");

                foreach (Renderer renderer in renderers)
                {
                    int slotCount = Math.Max(1, renderer.sharedMaterials.Length);
                    renderer.sharedMaterials = Enumerable.Repeat(rootGateMaterial, slotCount).ToArray();
                }

                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers.Skip(1))
                    bounds.Encapsulate(renderer.bounds);
                if (bounds.size.x <= 0.01f || bounds.size.y <= 0.01f || bounds.size.z <= 0.01f)
                    throw new InvalidOperationException($"Root Gate bounds are invalid: {bounds.size}");

                int vertices = meshes.Sum(mesh => mesh.vertexCount);
                int subMeshes = meshes.Sum(mesh => mesh.subMeshCount);
                int materialSlots = renderers.Sum(renderer => renderer.sharedMaterials.Length);
                int missingMaterials = renderers.Sum(renderer =>
                    renderer.sharedMaterials.Count(material => material == null));
                float bottomOffset = bounds.min.y - instance.transform.position.y;
                Vector3 rootRotation = instance.transform.rotation.eulerAngles;

                GameObject root = new GameObject("Finsterwald Root Gate");
                try
                {
                    instance.name = "Root Gate Mesh";
                    instance.transform.SetParent(root.transform, false);
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }

                Debug.Log($"ROOT_GATE_IMPORT_OK path={ModelPath} meshes={meshes.Length} " +
                    $"vertices={vertices} subMeshes={subMeshes} renderers={renderers.Length} " +
                    $"materials={materialSlots} missingMaterials={missingMaterials} " +
                    $"bounds={bounds.size} center={bounds.center} bottomOffset={bottomOffset:F3} " +
                    $"rootRotation={rootRotation} material={MaterialPath} prefab={PrefabPath}");
            }
            finally
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }

            AssetDatabase.SaveAssets();
        }

        private static Material CreateOrUpdateMaterial()
        {
            EnsureAssetFolder("Assets/_Elyndor/Art/Environment/Finsterwald/RootGate/Materials");
            EnsureAssetFolder("Assets/_Elyndor/Art/Environment/Finsterwald/RootGate/Textures");

            Texture2D albedo = CreateOrUpdateAlbedo();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP/Lit shader is unavailable; Root Gate material cannot be created safely.");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Finsterwald_RootGate_Stylized" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", new Color(0.82f, 0.84f, 0.76f, 1f));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.18f);
            material.SetFloat("_OcclusionStrength", 0.8f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D CreateOrUpdateAlbedo()
        {
            Texture2D generated = new Texture2D(AlbedoResolution, AlbedoResolution, TextureFormat.RGBA32, false, false)
            {
                name = "Finsterwald_RootGate_Albedo",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color bark = new Color(0.24f, 0.17f, 0.11f, 1f);
            Color weatheredStone = new Color(0.31f, 0.32f, 0.28f, 1f);
            Color moss = new Color(0.20f, 0.31f, 0.14f, 1f);
            Color[] pixels = new Color[AlbedoResolution * AlbedoResolution];

            for (int y = 0; y < AlbedoResolution; y++)
            {
                for (int x = 0; x < AlbedoResolution; x++)
                {
                    float u = x / (float)AlbedoResolution;
                    float v = y / (float)AlbedoResolution;
                    float broad = Mathf.PerlinNoise(u * 3.2f + 7.1f, v * 3.2f + 2.8f);
                    float detail = Mathf.PerlinNoise(u * 13.7f + 19.3f, v * 13.7f + 5.4f);
                    float mossMask = Mathf.SmoothStep(0.54f, 0.78f, broad * 0.72f + detail * 0.28f);
                    Color mineralBase = Color.Lerp(bark, weatheredStone, Mathf.SmoothStep(0.24f, 0.76f, detail));
                    Color color = Color.Lerp(mineralBase, moss, mossMask * 0.86f);
                    float valueVariation = Mathf.Lerp(0.83f, 1.08f, Mathf.PerlinNoise(u * 31f, v * 31f));
                    pixels[y * AlbedoResolution + x] = new Color(
                        color.r * valueVariation,
                        color.g * valueVariation,
                        color.b * valueVariation,
                        1f);
                }
            }

            generated.SetPixels(pixels);
            generated.Apply(false, false);
            File.WriteAllBytes(AlbedoPath, generated.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(AlbedoPath, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(AlbedoPath) is TextureImporter textureImporter)
            {
                textureImporter.textureType = TextureImporterType.Default;
                textureImporter.sRGBTexture = true;
                textureImporter.alphaSource = TextureImporterAlphaSource.None;
                textureImporter.wrapMode = TextureWrapMode.Repeat;
                textureImporter.filterMode = FilterMode.Bilinear;
                textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                textureImporter.maxTextureSize = AlbedoResolution;
                textureImporter.SaveAndReimport();
            }

            Texture2D imported = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
            if (imported == null)
                throw new InvalidOperationException($"Root Gate albedo could not be imported: {AlbedoPath}");

            return imported;
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
                throw new InvalidOperationException($"Invalid Unity asset folder: {path}");

            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
