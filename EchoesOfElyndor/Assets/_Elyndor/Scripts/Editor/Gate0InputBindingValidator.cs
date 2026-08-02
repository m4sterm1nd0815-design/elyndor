using System;
using System.Linq;
using Elyndor.Interaction;
using Elyndor.Player;
using Elyndor.UIFoundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Elyndor.EditorTools
{
    public static class Gate0InputBindingValidator
    {
        private const string ScenePath =
            "Assets/_Elyndor/Scenes/Finsterwald.unity";
        private const string InputAssetPath =
            "Assets/_Elyndor/Input/InputSystem_Actions.inputactions";

        [MenuItem("Elyndor/Input/Apply Gate 0 Input Bindings")]
        public static void ApplyFinsterwaldBindings()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlayerInputReader[] readers = ActiveSceneComponents<PlayerInputReader>();

            RequireCount(readers, 1, nameof(PlayerInputReader));

            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                InputAssetPath);

            if (asset == null)
                throw new InvalidOperationException($"Missing input asset: {InputAssetPath}");

            SerializedObject readerObject = new SerializedObject(readers[0]);
            readerObject.FindProperty("inputActions").objectReferenceValue = asset;
            readerObject.FindProperty("gameplayMapName").stringValue = "Player";
            readerObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(readers[0]);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            ValidateFinsterwald();
        }

        [MenuItem("Elyndor/QA/Validate Gate 0 Input")]
        public static void ValidateFinsterwald()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                InputAssetPath);

            if (asset == null)
                throw new InvalidOperationException($"Missing input asset: {InputAssetPath}");

            InputActionMap playerMap = asset.FindActionMap("Player", true);
            ValidateBinding(playerMap.FindAction("Interact", true), "<Keyboard>/e");

            if (!string.IsNullOrWhiteSpace(playerMap.FindAction("Interact", true).interactions))
                throw new InvalidOperationException("Interact must not require a Hold interaction.");

            for (int i = 1; i <= 8; i++)
                ValidateBinding(playerMap.FindAction($"Quickslot{i}", true), $"<Keyboard>/digit{i}");

            ValidateBinding(playerMap.FindAction("QuickslotUse", true), "<Keyboard>/r");

            PlayerInputReader[] readers = ActiveSceneComponents<PlayerInputReader>();
            QuickslotInputController[] controllers =
                ActiveSceneComponents<QuickslotInputController>();
            RequireCount(readers, 1, nameof(PlayerInputReader));
            RequireCount(controllers, 1, nameof(QuickslotInputController));

            SerializedObject readerObject = new SerializedObject(readers[0]);
            if (readerObject.FindProperty("inputActions").objectReferenceValue != asset)
                throw new InvalidOperationException("PlayerInputReader is not bound to the canonical input asset.");

            InteractionDetector[] detectors = ActiveSceneComponents<InteractionDetector>();
            RequireCount(detectors, 1, nameof(InteractionDetector));

            InteractableBase[] interactables = ActiveSceneComponents<InteractableBase>();
            if (interactables.Length == 0)
                throw new InvalidOperationException("Finsterwald contains no active interactables.");

            foreach (InteractableBase interactable in interactables)
            {
                bool hasTrigger = interactable.GetComponentsInChildren<Collider>(true)
                    .Any(collider => collider.enabled && collider.isTrigger &&
                        collider.gameObject.activeInHierarchy);

                if (!hasTrigger)
                    throw new InvalidOperationException(
                        $"Interactable '{interactable.name}' has no active trigger collider.");
            }

            HudGameplayBindingInstaller.Validate();
            Debug.Log(
                $"GATE0_INPUT_VALIDATION_OK: one reader, one controller, " +
                $"E and 1-8 valid, {interactables.Length} interactables valid.");
        }

        private static void ValidateBinding(InputAction action, string path)
        {
            int count = action.bindings.Count(binding =>
                string.Equals(binding.path, path, StringComparison.OrdinalIgnoreCase));

            if (count != 1)
                throw new InvalidOperationException(
                    $"Action '{action.name}' expected binding '{path}' exactly once, found {count}.");
        }

        private static T[] ActiveSceneComponents<T>() where T : Behaviour
        {
            return Resources.FindObjectsOfTypeAll<T>()
                .Where(component => component != null &&
                    component.gameObject.scene == EditorSceneManager.GetActiveScene() &&
                    component.isActiveAndEnabled)
                .ToArray();
        }

        private static void RequireCount<T>(T[] components, int expected, string name)
        {
            if (components.Length != expected)
                throw new InvalidOperationException(
                    $"Expected {expected} active {name}, found {components.Length}.");
        }
    }
}
