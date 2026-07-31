using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Bereitet die Mixamo-Kampfanimationen für Aren auf:
    /// 1. Stellt die Sword-And-Shield-Clips auf Humanoid um (retargetbar).
    /// 2. Verdrahtet Kampf-States im PlayerAnimator: leichter/schwerer
    ///    Angriff (Trigger) und Blocken (Bool), jeweils zurück in die
    ///    Locomotion. Wiederholbar; bestehende States werden ersetzt.
    /// </summary>
    public static class MixamoCombatSetup
    {
        private const string MixamoFolder = "Assets/_Elyndor/Animation/Mixamo";
        private const string ControllerPath =
            "Assets/_Elyndor/Animation/Controllers/PlayerAnimator.controller";

        private static readonly (string state, string fbx, string parameter, bool isTrigger, bool loop)[]
            CombatStates =
        {
            ("AttackLight", "Sword And Shield Slash.fbx", "AttackLight", true, false),
            ("AttackHeavy", "Sword And Shield Attack.fbx", "AttackHeavy", true, false),
            ("Block", "Sword And Shield Block.fbx", "Block", false, true)
        };

        [MenuItem("Elyndor/Setup/Kampf-Animationen einrichten")]
        public static void ConfigureCombatAnimations()
        {
            ReimportSwordClipsAsHumanoid();
            WireAnimatorStates();
            AssetDatabase.SaveAssets();
            Debug.Log("Kampf-Animationen eingerichtet.");
        }

        private static void ReimportSwordClipsAsHumanoid()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { MixamoFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!System.IO.Path.GetFileName(path).StartsWith("Sword And Shield"))
                {
                    continue;
                }

                ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);

                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void WireAnimatorStates()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (controller == null)
            {
                throw new System.InvalidOperationException(
                    $"PlayerAnimator nicht gefunden: {ControllerPath}"
                );
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            foreach ((string stateName, string fbxName, string parameter, bool isTrigger, bool loop)
                     in CombatStates)
            {
                AnimationClip clip = LoadClip($"{MixamoFolder}/{fbxName}");

                if (clip == null)
                {
                    Debug.LogWarning($"Kein Humanoid-Clip in {fbxName} — State {stateName} übersprungen.");
                    continue;
                }

                if (loop)
                {
                    SetClipLooping(clip);
                }

                EnsureParameter(controller, parameter, isTrigger);

                RemoveState(stateMachine, stateName);
                AnimatorState state = stateMachine.AddState(stateName);
                state.motion = clip;

                AnimatorStateTransition entry = stateMachine.AddAnyStateTransition(state);
                entry.hasExitTime = false;
                entry.duration = 0.08f;
                entry.canTransitionToSelf = false;
                entry.AddCondition(
                    isTrigger ? AnimatorConditionMode.If : AnimatorConditionMode.If,
                    0f, parameter
                );

                AnimatorStateTransition exit = state.AddTransition(stateMachine.defaultState);

                if (isTrigger)
                {
                    exit.hasExitTime = true;
                    exit.exitTime = 0.85f;
                    exit.duration = 0.15f;
                }
                else
                {
                    exit.hasExitTime = false;
                    exit.duration = 0.15f;
                    exit.AddCondition(AnimatorConditionMode.IfNot, 0f, parameter);
                }
            }

            EditorUtility.SetDirty(controller);
        }

        private static AnimationClip LoadClip(string fbxPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.Contains("__preview"));
        }

        private static void SetClipLooping(AnimationClip clip)
        {
            string path = AssetDatabase.GetAssetPath(clip);
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;

            bool changed = false;

            for (int i = 0; i < clips.Length; i++)
            {
                if (!clips[i].loopTime)
                {
                    clips[i].loopTime = true;
                    changed = true;
                }
            }

            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureParameter(
            AnimatorController controller, string name, bool isTrigger
        )
        {
            if (controller.parameters.All(parameter => parameter.name != name))
            {
                controller.AddParameter(name, isTrigger
                    ? AnimatorControllerParameterType.Trigger
                    : AnimatorControllerParameterType.Bool);
            }
        }

        private static void RemoveState(AnimatorStateMachine stateMachine, string stateName)
        {
            ChildAnimatorState existing = stateMachine.states
                .FirstOrDefault(child => child.state != null && child.state.name == stateName);

            if (existing.state != null)
            {
                stateMachine.RemoveState(existing.state);
            }
        }
    }
}
