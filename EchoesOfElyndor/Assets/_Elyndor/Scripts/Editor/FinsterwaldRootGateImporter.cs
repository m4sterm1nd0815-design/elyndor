using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Elyndor.EditorTools
{
    public static class FinsterwaldRootGateImporter
    {
        public const string ModelPath = "Assets/_Elyndor/Art/Environment/Finsterwald/RootGate/Finsterwald_RootGate_Meshy.fbx";
        public const string PrefabPath = "Assets/_Elyndor/Art/Environment/Finsterwald/RootGate/Finsterwald_RootGate.prefab";

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
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();

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
                    $"rootRotation={rootRotation} prefab={PrefabPath}");
            }
            finally
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
