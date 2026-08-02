using Elyndor.Player;
using UnityEngine;

namespace Elyndor.Cameras
{
    /// <summary>
    /// Kamera-relative Orbit-Kamera mit geglaettetem Fokuspunkt,
    /// Terrain-Schutz und kollisionsbasierter Sichtlinienkorrektur.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraFollow : MonoBehaviour
    {
        private const int OcclusionHitCapacity = 16;

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private PlayerInputReader inputReader;
        [Tooltip("Blickpunkt-Anhebung, damit Aren nicht am unteren Bildrand klebt.")]
        [SerializeField] private float focusHeight = 1.2f;

        [Header("Orbit")]
        [SerializeField] private float startYaw;
        [SerializeField] private float startPitch = 52f;
        [Tooltip("Fast bodennah: erlaubt den Blick in die Ferne statt nur von oben.")]
        [SerializeField] private float minPitch = 6f;
        [SerializeField] private float maxPitch = 72f;
        [SerializeField] private float mouseSensitivity = 0.22f;
        [SerializeField] private float gamepadRotationSpeed = 140f;

        [Header("Zoom")]
        [SerializeField] private float distance = 12f;
        [SerializeField] private float minDistance = 6f;
        [SerializeField] private float maxDistance = 18f;
        [SerializeField] private float zoomSensitivity = 0.012f;

        [Header("Follow")]
        [SerializeField] private float focusSmoothTime = 0.06f;
        [SerializeField] private float followSmoothTime = 0.12f;

        [Header("Occlusion")]
        [SerializeField] private LayerMask occlusionLayers =
            Physics.DefaultRaycastLayers;
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private float collisionPadding = 0.15f;
        [SerializeField] private float occlusionRecoverySmoothTime = 0.18f;
        [Tooltip("Mindestabstand der Kamera ueber dem Terrain.")]
        [SerializeField] private float terrainClearance = 0.5f;

        private readonly RaycastHit[] occlusionHits =
            new RaycastHit[OcclusionHitCapacity];

        private float yaw;
        private float pitch;
        private float currentDistance;
        private float distanceRecoveryVelocity;
        private Vector3 smoothedFocusPoint;
        private Vector3 focusVelocity;
        private Vector3 followVelocity;
        private bool initialized;

        public float YawDegrees => yaw;

        private void Awake()
        {
            yaw = startYaw;
            pitch = startPitch;
            currentDistance = distance;
            ResolveInputReader();
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            ResolveInputReader();
            ReadOrbitInput();

            Vector3 targetFocusPoint =
                target.position + Vector3.up * focusHeight;

            if (!initialized)
            {
                smoothedFocusPoint = targetFocusPoint;
                currentDistance = distance;
                initialized = true;
            }
            else
            {
                smoothedFocusPoint = Vector3.SmoothDamp(
                    smoothedFocusPoint,
                    targetFocusPoint,
                    ref focusVelocity,
                    focusSmoothTime);
            }

            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 orbitDirection =
                orbitRotation * Vector3.back;

            float unobstructedDistance = ResolveOcclusionDistance(
                smoothedFocusPoint,
                orbitDirection,
                distance);

            if (unobstructedDistance < currentDistance)
            {
                currentDistance = unobstructedDistance;
                distanceRecoveryVelocity = 0f;
            }
            else
            {
                currentDistance = Mathf.SmoothDamp(
                    currentDistance,
                    unobstructedDistance,
                    ref distanceRecoveryVelocity,
                    occlusionRecoverySmoothTime);
            }

            Vector3 desiredPosition =
                smoothedFocusPoint + orbitDirection * currentDistance;

            desiredPosition = KeepAboveTerrain(desiredPosition);

            Vector3 smoothedPosition = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref followVelocity,
                followSmoothTime);

            transform.position = ClampToClearSightLine(
                smoothedFocusPoint,
                smoothedPosition);
            transform.LookAt(smoothedFocusPoint);
        }

        private void ResolveInputReader()
        {
            if (inputReader == null && target != null)
                inputReader = target.GetComponent<PlayerInputReader>();
        }

        private void ReadOrbitInput()
        {
            if (inputReader == null)
                return;

            Vector2 lookInput = inputReader.CameraLook;

            if (inputReader.CameraLookUsesPointer)
            {
                yaw += lookInput.x * mouseSensitivity;
                pitch -= lookInput.y * mouseSensitivity;
            }
            else
            {
                yaw += lookInput.x * gamepadRotationSpeed * Time.deltaTime;
                pitch -= lookInput.y * gamepadRotationSpeed * 0.6f *
                         Time.deltaTime;
            }

            float scroll = inputReader.CameraZoom;
            if (Mathf.Abs(scroll) > 0.01f)
                distance -= scroll * zoomSensitivity;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        private float ResolveOcclusionDistance(
            Vector3 focusPoint,
            Vector3 direction,
            float requestedDistance)
        {
            int hitCount = Physics.SphereCastNonAlloc(
                focusPoint,
                collisionRadius,
                direction,
                occlusionHits,
                requestedDistance,
                occlusionLayers,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = requestedDistance;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = occlusionHits[i];

                if (hit.collider == null || IsTargetCollider(hit.collider))
                    continue;

                nearestDistance = Mathf.Min(
                    nearestDistance,
                    Mathf.Max(
                        0.05f,
                        hit.distance - collisionPadding));
            }

            return nearestDistance;
        }

        private Vector3 ClampToClearSightLine(
            Vector3 focusPoint,
            Vector3 candidatePosition)
        {
            Vector3 offset = candidatePosition - focusPoint;
            float candidateDistance = offset.magnitude;

            if (candidateDistance <= Mathf.Epsilon)
                return candidatePosition;

            float clearDistance = ResolveOcclusionDistance(
                focusPoint,
                offset / candidateDistance,
                candidateDistance);

            return clearDistance < candidateDistance
                ? focusPoint + offset.normalized * clearDistance
                : candidatePosition;
        }

        private bool IsTargetCollider(Collider candidate)
        {
            Transform candidateTransform = candidate.transform;

            return candidateTransform == target ||
                   candidateTransform.IsChildOf(target);
        }

        private Vector3 KeepAboveTerrain(Vector3 position)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                return position;

            float terrainY = terrain.SampleHeight(position) +
                             terrain.transform.position.y +
                             terrainClearance;

            if (position.y < terrainY)
                position.y = terrainY;

            return position;
        }

        private void OnValidate()
        {
            minPitch = Mathf.Clamp(minPitch, -5f, 85f);
            maxPitch = Mathf.Clamp(maxPitch, minPitch, 85f);
            minDistance = Mathf.Max(0.5f, minDistance);
            maxDistance = Mathf.Max(minDistance, maxDistance);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            focusSmoothTime = Mathf.Max(0.01f, focusSmoothTime);
            followSmoothTime = Mathf.Max(0.01f, followSmoothTime);
            collisionRadius = Mathf.Max(0.01f, collisionRadius);
            collisionPadding = Mathf.Max(0f, collisionPadding);
            occlusionRecoverySmoothTime = Mathf.Max(
                0.01f,
                occlusionRecoverySmoothTime);
        }
    }
}
