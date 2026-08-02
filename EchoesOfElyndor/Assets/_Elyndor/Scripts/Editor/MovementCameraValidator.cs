using System;
using System.Linq;
using Elyndor.Cameras;
using Elyndor.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools
{
    public static class MovementCameraValidator
    {
        private static readonly string[] RegionSceneNames =
        {
            "Finsterwald",
            "Sonnenfelder",
            "Nebelmoor"
        };

        [MenuItem("Elyndor/QA/Validate Movement And Camera")]
        public static void ValidateCurrentRegions()
        {
            foreach (string sceneName in RegionSceneNames)
                ValidateScene(sceneName);

            Debug.Log(
                "MOVEMENT_CAMERA_VALIDATION_OK: one active player/input/camera " +
                "chain per region, valid movement tuning, camera target and " +
                "occlusion configuration.");
        }

        private static void ValidateScene(string sceneName)
        {
            Scene scene = EditorSceneManager.OpenScene(
                $"Assets/_Elyndor/Scenes/{sceneName}.unity",
                OpenSceneMode.Single);

            PlayerMovement movement = FindSingleActive<PlayerMovement>(
                scene,
                sceneName);
            PlayerInputReader input = FindSingleActive<PlayerInputReader>(
                scene,
                sceneName);
            CameraFollow camera = FindSingleActive<CameraFollow>(
                scene,
                sceneName);
            CharacterController controller =
                movement.GetComponent<CharacterController>();

            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"{sceneName}: PlayerMovement requires a " +
                    "CharacterController.");
            }

            if (input.gameObject != movement.gameObject)
            {
                throw new InvalidOperationException(
                    $"{sceneName}: PlayerInputReader and PlayerMovement must " +
                    "share the player object.");
            }

            ValidatePositiveFloat(movement, "walkSpeed", sceneName);
            ValidatePositiveFloat(movement, "sprintSpeed", sceneName);
            ValidatePositiveFloat(movement, "acceleration", sceneName);
            ValidatePositiveFloat(movement, "deceleration", sceneName);
            ValidatePositiveFloat(movement, "rotationSpeed", sceneName);

            SerializedObject serializedMovement =
                new SerializedObject(movement);
            float walkSpeed = serializedMovement
                .FindProperty("walkSpeed").floatValue;
            float sprintSpeed = serializedMovement
                .FindProperty("sprintSpeed").floatValue;

            if (sprintSpeed <= walkSpeed)
            {
                throw new InvalidOperationException(
                    $"{sceneName}: sprintSpeed must exceed walkSpeed.");
            }

            SerializedObject serializedCamera = new SerializedObject(camera);
            Transform target = serializedCamera
                .FindProperty("target").objectReferenceValue as Transform;

            if (target != movement.transform)
            {
                throw new InvalidOperationException(
                    $"{sceneName}: CameraFollow target must be the active " +
                    "PlayerMovement transform.");
            }

            ValidatePositiveFloat(camera, "focusSmoothTime", sceneName);
            ValidatePositiveFloat(camera, "followSmoothTime", sceneName);
            ValidatePositiveFloat(camera, "collisionRadius", sceneName);
            ValidatePositiveFloat(
                camera,
                "occlusionRecoverySmoothTime",
                sceneName);

            int occlusionMask = serializedCamera
                .FindProperty("occlusionLayers").intValue;

            if (occlusionMask == 0)
            {
                throw new InvalidOperationException(
                    $"{sceneName}: camera occlusion layer mask is empty.");
            }
        }

        private static T FindSingleActive<T>(
            Scene scene,
            string sceneName)
            where T : Behaviour
        {
            T[] components = UnityEngine.Object
                .FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(component =>
                    component.gameObject.scene == scene &&
                    component.gameObject.activeInHierarchy &&
                    component.enabled)
                .ToArray();

            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{sceneName}: expected one active {typeof(T).Name}, " +
                    $"found {components.Length}.");
            }

            return components[0];
        }

        private static void ValidatePositiveFloat(
            UnityEngine.Object target,
            string propertyName,
            string sceneName)
        {
            SerializedProperty property = new SerializedObject(target)
                .FindProperty(propertyName);

            if (property == null || property.floatValue <= 0f)
            {
                throw new InvalidOperationException(
                    $"{sceneName}/{target.name}: {propertyName} must be " +
                    "greater than zero.");
            }
        }
    }
}
