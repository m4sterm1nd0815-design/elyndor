using UnityEngine;
using UnityEngine.InputSystem;

namespace Elyndor.Cameras
{
    /// <summary>
    /// Orbit-Verfolgerkamera: folgt dem Ziel weich (SmoothDamp) und lässt
    /// sich mit gehaltener rechter Maustaste bzw. rechtem Gamepad-Stick um
    /// den Spieler drehen; das Mausrad zoomt. Die Bewegung des Spielers ist
    /// kamerarelativ (siehe PlayerMovement).
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [Tooltip("Blickpunkt-Anhebung, damit Aren nicht am unteren Bildrand klebt.")]
        [SerializeField] private float focusHeight = 1.2f;

        [Header("Orbit")]
        [SerializeField] private float startYaw = 0f;
        [SerializeField] private float startPitch = 52f;
        [Tooltip("Fast bodennah: erlaubt den Blick in die Ferne statt nur von oben.")]
        [SerializeField] private float minPitch = 6f;
        [SerializeField] private float maxPitch = 72f;
        [Tooltip("Mindestabstand der Kamera ueber dem Terrain.")]
        [SerializeField] private float terrainClearance = 0.5f;
        [SerializeField] private float mouseSensitivity = 0.22f;
        [SerializeField] private float gamepadRotationSpeed = 140f;

        [Header("Zoom")]
        [SerializeField] private float distance = 12f;
        [SerializeField] private float minDistance = 6f;
        [SerializeField] private float maxDistance = 18f;
        [SerializeField] private float zoomSensitivity = 0.012f;

        [Header("Follow")]
        [SerializeField] private float followSmoothTime = 0.12f;

        private float yaw;
        private float pitch;
        private Vector3 followVelocity;

        /// <summary>Aktuelle Kamera-Drehung um die Hochachse (für kamerarelative Bewegung).</summary>
        public float YawDegrees => yaw;

        private void Awake()
        {
            yaw = startYaw;
            pitch = startPitch;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            ReadOrbitInput();

            Vector3 focusPoint = target.position + Vector3.up * focusHeight;
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = focusPoint + orbitRotation * new Vector3(0f, 0f, -distance);

            // Bei flachem Blickwinkel nicht ins Gelaende eintauchen.
            Terrain terrain = Terrain.activeTerrain;

            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(desiredPosition) +
                                 terrain.transform.position.y + terrainClearance;

                if (desiredPosition.y < terrainY)
                {
                    desiredPosition.y = terrainY;
                }
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref followVelocity,
                followSmoothTime
            );

            transform.LookAt(focusPoint);
        }

        // Direkter Device-Zugriff wie im restlichen Gameplay-Code; wird mit
        // dem Input-Refactor (Roadmap M3) auf das Actions-Asset umgestellt.
        private void ReadOrbitInput()
        {
            if (Mouse.current != null)
            {
                if (Mouse.current.rightButton.isPressed)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    yaw += mouseDelta.x * mouseSensitivity;
                    pitch -= mouseDelta.y * mouseSensitivity;
                }

                float scroll = Mouse.current.scroll.ReadValue().y;

                if (Mathf.Abs(scroll) > 0.01f)
                {
                    distance -= scroll * zoomSensitivity;
                }
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.rightStick.ReadValue();
                yaw += stick.x * gamepadRotationSpeed * Time.deltaTime;
                pitch -= stick.y * gamepadRotationSpeed * 0.6f * Time.deltaTime;
            }

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }
}
