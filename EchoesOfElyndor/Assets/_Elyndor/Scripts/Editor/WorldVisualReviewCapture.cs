using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Produces deterministic review images for the existing world regions.
    /// The capture camera is temporary and never becomes part of a scene.
    /// </summary>
    public static class WorldVisualReviewCapture
    {
        private const int Width = 1920;
        private const int Height = 1080;
        private const string SceneFolder = "Assets/_Elyndor/Scenes";

        private readonly struct Shot
        {
            public Shot(string name, Vector3 position, Vector3 target, float fieldOfView = 60f)
            {
                Name = name;
                Position = position;
                Target = target;
                FieldOfView = fieldOfView;
            }

            public string Name { get; }
            public Vector3 Position { get; }
            public Vector3 Target { get; }
            public float FieldOfView { get; }
        }

        [MenuItem("Elyndor/World Visuals/Build And Capture Review")]
        public static void BuildAndCaptureAll()
        {
            WorldVisualOverhaulBuilder.BuildCurrentRegions();
            CaptureAll();
        }

        [MenuItem("Elyndor/World Visuals/Capture Review Screenshots")]
        public static void CaptureAll()
        {
            EditorSettings.asyncShaderCompilation = false;

            CaptureRegion("Finsterwald", () => new[]
            {
                Ground("01_arrival", 0f, -48f, 0f, -31f),
                Ground("02_main_vista", 1f, -25f, -3f, -7f, 64f),
                Ground("03_landmark", -22f, 7f, -14f, 16f),
                Ground("04_side_path", -19f, -22f, -35f, -13f),
                Ground("05_memory_site", -1f, -10f, 2f, 2f),
                Ground("06_region_exit", 33f, 23f, 46f, 36f),
                Sky("07_overview", new Vector3(4f, 82f, -88f), new Vector3(0f, 0f, 0f), 52f),
                Ground("08_player_perspective", 4f, -39f, -2f, -28f, 68f, 2.1f),
                Ground("09_difficult_light", -45f, 26f, -38f, 35f, 55f)
            });

            CaptureRegion("Sonnenfelder", () => new[]
            {
                Ground("01_arrival", -45f, -7f, -30f, -3f),
                Ground("02_main_vista", -20f, -5f, 13f, 2f, 67f),
                Ground("03_landmark", 17f, -18f, 34f, -28f),
                Ground("04_side_path", 4f, 15f, -10f, 24f),
                Ground("05_memory_site", 2f, 9f, 13f, 15f),
                Ground("06_region_exit", 35f, 1f, 48f, 0f),
                Sky("07_overview", new Vector3(-12f, 75f, -80f), new Vector3(0f, 0f, 3f), 54f),
                Ground("08_player_perspective", -38f, -6f, -25f, -3f, 68f, 2.1f),
                Ground("09_difficult_light", 14f, -23f, 31f, -29f, 55f)
            });

            CaptureRegion("Nebelmoor", () => new[]
            {
                Ground("01_arrival", 0f, -39f, 6f, -25f),
                Ground("02_main_vista", 5f, -25f, -2f, -10f, 64f),
                Ground("03_landmark", -19f, 17f, -31f, 24f),
                Ground("04_side_path", 11f, -9f, 21f, -1f),
                Ground("05_memory_site", 3f, -3f, 7f, 7f),
                Ground("06_region_exit", 23f, 26f, 31f, 36f),
                Sky("07_overview", new Vector3(-4f, 68f, -74f), new Vector3(0f, 0f, 1f), 52f),
                Ground("08_player_perspective", 1f, -34f, 7f, -23f, 68f, 2.1f),
                Ground("09_difficult_light", -25f, 5f, -16f, 13f, 54f)
            });

            AssetDatabase.Refresh();
            Debug.Log("World visual review screenshots captured successfully.");
        }

        private static Shot Ground(
            string name, float fromX, float fromZ, float toX, float toZ,
            float fieldOfView = 60f, float eyeHeight = 3.2f)
        {
            return new Shot(
                name,
                new Vector3(fromX, GroundHeight(fromX, fromZ) + eyeHeight, fromZ),
                new Vector3(toX, GroundHeight(toX, toZ) + 1.3f, toZ),
                fieldOfView);
        }

        private static Shot Sky(string name, Vector3 position, Vector3 target, float fieldOfView)
        {
            return new Shot(name, position, target, fieldOfView);
        }

        private static float GroundHeight(float x, float z)
        {
            Terrain activeTerrain = Terrain.activeTerrain;
            return activeTerrain == null
                ? 0f
                : activeTerrain.SampleHeight(new Vector3(x, 0f, z)) +
                  activeTerrain.transform.position.y;
        }

        private static void CaptureRegion(
            string sceneName, Func<IReadOnlyList<Shot>> createShots)
        {
            EditorSceneManager.OpenScene($"{SceneFolder}/{sceneName}.unity", OpenSceneMode.Single);
            IReadOnlyList<Shot> shots = createShots();
            string outputFolder = Path.Combine(
                Directory.GetParent(Application.dataPath)!.FullName,
                "docs", "screenshots", "world-overhaul", sceneName.ToLowerInvariant());
            Directory.CreateDirectory(outputFolder);

            foreach (Shot shot in shots)
                RenderShot(Path.Combine(outputFolder, $"{shot.Name}.png"), shot);

            Debug.Log($"Captured {shots.Count} review screenshots for {sceneName}: {outputFolder}");
        }

        private static void RenderShot(string path, Shot shot)
        {
            GameObject cameraObject = new GameObject("[Temporary] World Review Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = shot.FieldOfView;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 500f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            cameraObject.transform.SetPositionAndRotation(
                shot.Position,
                Quaternion.LookRotation((shot.Target - shot.Position).normalized, Vector3.up));

            RenderTexture renderTexture = new RenderTexture(
                Width, Height, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();

            if (!renderTexture.IsCreated())
                throw new InvalidOperationException(
                    $"Could not create the review render texture for {shot.Name}.");
            RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest
            {
                destination = renderTexture
            };

            try
            {
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new InvalidOperationException("The active render pipeline does not support camera render requests.");

                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTexture;
                Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                texture.Apply(false);

                if (!ContainsRenderableDetail(texture))
                    throw new InvalidOperationException(
                        $"Review capture {shot.Name} contains no renderable scene detail. " +
                        "Run the capture with graphics enabled (without -nographics).");

                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                RenderTexture.active = previous;
            }
            finally
            {
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static bool ContainsRenderableDetail(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            byte minimum = byte.MaxValue;
            byte maximum = byte.MinValue;

            for (int i = 0; i < pixels.Length; i += 4096)
            {
                Color32 pixel = pixels[i];
                minimum = Math.Min(minimum, Math.Min(pixel.r, Math.Min(pixel.g, pixel.b)));
                maximum = Math.Max(maximum, Math.Max(pixel.r, Math.Max(pixel.g, pixel.b)));
            }

            return maximum - minimum >= 12;
        }
    }
}
