using System.Linq;
using Elyndor.Persistence;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace Elyndor.EditorTools
{
    [InitializeOnLoad]
    public sealed class OpenZonePlayModeGuard : IPreprocessBuildWithReport
    {
        static OpenZonePlayModeGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static bool IsPrototype(string path) =>
            path == OpenZonePrototypeBuilder.ScenePath;

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SceneAsset start = EditorSceneManager.playModeStartScene;
                bool prototype = start != null
                    ? IsPrototype(AssetDatabase.GetAssetPath(start))
                    : EditorSceneManager.GetSceneManagerSetup()
                        .Any(scene => scene.isLoaded && IsPrototype(scene.path));
                SessionState.SetBool(SaveBootstrap.TransientSessionKey, prototype);
                if (prototype)
                    SaveService.Uninstall();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (SessionState.GetBool(SaveBootstrap.TransientSessionKey, false))
                    SaveService.Uninstall();
                SessionState.SetBool(SaveBootstrap.TransientSessionKey, false);
            }
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (EditorBuildSettings.scenes.Any(scene => scene.enabled && IsPrototype(scene.path)))
                throw new BuildFailedException(
                    "Finsterwald_OpenZone is an Editor-only experiment. Remove it from build scenes.");
        }
    }
}
