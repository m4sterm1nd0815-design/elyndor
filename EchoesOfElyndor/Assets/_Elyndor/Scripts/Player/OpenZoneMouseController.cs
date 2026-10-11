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
        [SerializeField, Min(0.1f)] private float attackDistance = 2f;
        [SerializeField, Min(1f)] private float rayDistance = 500f;

        private PlayerCombat combat;
        private PlayerInputReader inputReader;
        private Vector3 destination;
        private Transform attackTarget;
        private bool hasDestination;

        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            inputReader = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            if (inputReader == null || !inputReader.GameplayMapEnabled ||
                Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame ||
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
                return;
            }

            // A terrain click is a movement order. Props and UI never create
            // an arbitrary destination behind a visible obstacle.
            if (hit.collider is TerrainCollider)
            {
                attackTarget = null;
                destination = hit.point;
                hasDestination = true;
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
            if (offset.magnitude <= stoppingDistance)
            {
                if (attackTarget == null) Cancel();
                return Vector3.zero;
            }
            return offset.normalized;
        }

        public void Cancel()
        {
            hasDestination = false;
            attackTarget = null;
        }

        private void OnDisable() => Cancel();

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}
