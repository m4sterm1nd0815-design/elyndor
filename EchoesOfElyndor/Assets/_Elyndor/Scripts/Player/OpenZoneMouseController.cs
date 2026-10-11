using Elyndor.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Elyndor.Player
{
    /// <summary>Prototype-only contextual mouse control for the open-zone scene.</summary>
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class OpenZoneMouseController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float arrivalDistance = 0.7f;
        [SerializeField, Min(0.2f)] private float slowdownDistance = 3f;
        [SerializeField, Range(0.05f, 1f)] private float minimumMoveInput = 0.18f;
        [SerializeField, Min(0.1f)] private float attackDistance = 2f;
        [SerializeField, Min(1f)] private float rayDistance = 500f;
        [SerializeField, Min(0.1f)] private float obstacleProbeDistance = 1.4f;
        [SerializeField, Min(0.05f)] private float obstacleProbeRadius = 0.32f;
        [SerializeField, Min(0.1f)] private float obstacleProbeHeight = 0.65f;
        [SerializeField] private LayerMask obstacleMask = ~0;
        [SerializeField, Min(8)] private int markerSegments = 32;
        [SerializeField, Min(0.05f)] private float markerRadius = 0.42f;

        private static readonly float[] AvoidanceAngles = { 30f, 52f, 75f, 95f };

        private PlayerCombat combat;
        private PlayerInputReader inputReader;
        private LineRenderer destinationMarker;
        private Material markerMaterial;
        private readonly RaycastHit[] obstacleHits = new RaycastHit[16];
        private Vector3 destination;
        private Transform attackTarget;
        private bool hasDestination;
        private float preferredAvoidanceSide = 1f;

        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            inputReader = GetComponent<PlayerInputReader>();
            CreateDestinationMarker();
        }

        private void Update()
        {
            if (inputReader == null || !inputReader.GameplayMapEnabled ||
                Mouse.current == null || !Mouse.current.leftButton.isPressed ||
                (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
                return;

            Camera view = Camera.main;
            if (view == null) return;

            Ray ray = view.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance)) return;

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable is Component target && target.transform != transform)
            {
                attackTarget = target.transform;
                hasDestination = true;
                UpdateDestinationMarker(target.transform.position, true);
                return;
            }

            // While the button stays down, the terrain hit follows the cursor.
            // Props and UI never create an arbitrary destination behind a
            // visible obstacle.
            if (hit.collider is TerrainCollider)
            {
                attackTarget = null;
                destination = hit.point;
                hasDestination = true;
                UpdateDestinationMarker(destination, false);
            }
        }

        private void LateUpdate()
        {
            if (attackTarget == null || !hasDestination) return;
            if (PlanarDistance(transform.position, attackTarget.position) > attackDistance) return;

            if (combat != null && combat.TryContextAttack(attackTarget))
                Cancel();
        }

        public Vector3 GetMovement(Vector3 position)
        {
            if (!hasDestination) return Vector3.zero;
            if (attackTarget != null) destination = attackTarget.position;

            Vector3 offset = destination - position;
            offset.y = 0f;
            float stoppingDistance = attackTarget == null ? arrivalDistance : attackDistance;
            float distance = offset.magnitude;
            if (distance <= stoppingDistance)
            {
                if (attackTarget == null) Cancel();
                return Vector3.zero;
            }

            Vector3 desiredDirection = offset.normalized;
            Vector3 steeredDirection = SteerAroundObstacle(
                position,
                desiredDirection,
                Mathf.Max(0.2f, distance - stoppingDistance));
            if (steeredDirection.sqrMagnitude <= 0.01f) return Vector3.zero;

            if (attackTarget != null) return steeredDirection;

            float speedScale = Mathf.InverseLerp(
                arrivalDistance,
                Mathf.Max(slowdownDistance, arrivalDistance + 0.1f),
                distance);
            return steeredDirection * Mathf.Lerp(minimumMoveInput, 1f, speedScale);
        }

        public void Cancel()
        {
            hasDestination = false;
            attackTarget = null;
            if (destinationMarker != null) destinationMarker.enabled = false;
        }

        private void OnDisable() => Cancel();

        private Vector3 SteerAroundObstacle(
            Vector3 position,
            Vector3 desiredDirection,
            float remainingDistance)
        {
            float probeDistance = Mathf.Min(obstacleProbeDistance, remainingDistance);
            if (!IsBlocked(position, desiredDirection, probeDistance))
                return desiredDirection;

            Vector3 bestDirection = Vector3.zero;
            float bestScore = float.NegativeInfinity;
            foreach (float angle in AvoidanceAngles)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0
                        ? preferredAvoidanceSide
                        : -preferredAvoidanceSide;
                    Vector3 candidate =
                        Quaternion.AngleAxis(angle * side, Vector3.up) * desiredDirection;
                    if (IsBlocked(position, candidate, probeDistance)) continue;

                    float score = Vector3.Dot(candidate, desiredDirection) +
                                  Vector3.Dot(candidate, transform.forward) * 0.12f;
                    if (score <= bestScore) continue;
                    bestScore = score;
                    bestDirection = candidate;
                    preferredAvoidanceSide = side;
                }
            }
            return bestDirection;
        }

        private bool IsBlocked(Vector3 position, Vector3 direction, float distance)
        {
            if (distance <= 0.05f) return false;
            Vector3 origin = position + Vector3.up * obstacleProbeHeight;
            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                obstacleProbeRadius,
                direction,
                obstacleHits,
                distance,
                obstacleMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = obstacleHits[i].collider;
                if (collider == null || collider is TerrainCollider) continue;
                Transform candidate = collider.transform;
                if (candidate == transform || candidate.IsChildOf(transform)) continue;
                if (attackTarget != null &&
                    (candidate == attackTarget || candidate.IsChildOf(attackTarget) ||
                     attackTarget.IsChildOf(candidate)))
                    continue;
                return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (markerMaterial == null) return;
            if (Application.isPlaying) Destroy(markerMaterial);
            else DestroyImmediate(markerMaterial);
        }

        private void CreateDestinationMarker()
        {
            GameObject marker = new GameObject("Mouse Destination");
            marker.transform.SetParent(transform, false);
            destinationMarker = marker.AddComponent<LineRenderer>();
            destinationMarker.useWorldSpace = true;
            destinationMarker.loop = true;
            destinationMarker.positionCount = markerSegments;
            destinationMarker.startWidth = 0.055f;
            destinationMarker.endWidth = 0.055f;
            destinationMarker.numCornerVertices = 2;
            destinationMarker.numCapVertices = 2;
            destinationMarker.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            destinationMarker.receiveShadows = false;
            Shader markerShader = Shader.Find("Sprites/Default") ??
                                  Shader.Find("Universal Render Pipeline/Unlit");
            if (markerShader != null)
            {
                markerMaterial = new Material(markerShader);
                destinationMarker.sharedMaterial = markerMaterial;
            }
            destinationMarker.enabled = false;
        }

        private void UpdateDestinationMarker(Vector3 point, bool hostile)
        {
            if (destinationMarker == null) return;
            Color color = hostile
                ? new Color(1f, 0.28f, 0.12f, 0.9f)
                : new Color(0.25f, 0.9f, 0.75f, 0.85f);
            destinationMarker.startColor = color;
            destinationMarker.endColor = color;
            point.y += 0.08f;
            for (int i = 0; i < markerSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / markerSegments;
                destinationMarker.SetPosition(i, point + new Vector3(
                    Mathf.Cos(angle) * markerRadius,
                    0f,
                    Mathf.Sin(angle) * markerRadius));
            }
            destinationMarker.enabled = true;
        }

        private void OnValidate()
        {
            slowdownDistance = Mathf.Max(slowdownDistance, arrivalDistance + 0.1f);
            obstacleProbeDistance = Mathf.Max(0.1f, obstacleProbeDistance);
            obstacleProbeRadius = Mathf.Max(0.05f, obstacleProbeRadius);
            obstacleProbeHeight = Mathf.Max(0.1f, obstacleProbeHeight);
            markerSegments = Mathf.Max(8, markerSegments);
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}
