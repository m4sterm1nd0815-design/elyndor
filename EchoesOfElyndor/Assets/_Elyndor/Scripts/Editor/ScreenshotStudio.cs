using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Elyndor.EditorTools
{
    public static class ScreenshotStudio
    {
        [MenuItem("Elyndor/Presentation/Capture HUD Baselines")]
        public static void CaptureHudBaselines()
        {
            Camera camera = Camera.main;

            if (camera == null)
            {
                Debug.LogError("No Main Camera found.");
                return;
            }

            string folder = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    "Screenshots/Elyndor/UI"));

            Directory.CreateDirectory(folder);

            Capture(camera, Path.Combine(
                folder,
                $"ui_{DateTime.Now:yyyyMMdd_HHmmss}_1920x1080.png"),
                1920,
                1080);

            Capture(camera, Path.Combine(
                folder,
                $"ui_{DateTime.Now:yyyyMMdd_HHmmss}_3840x2160.png"),
                3840,
                2160);

            Debug.Log($"UI screenshots created in {folder}");
        }

        private static void Capture(Camera camera, string path, int width, int height)
        {
            RenderTexture texture = RenderTexture.GetTemporary(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;

            camera.targetTexture = texture;
            RenderTexture.active = texture;
            camera.Render();

            var image = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);

            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(texture);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}

