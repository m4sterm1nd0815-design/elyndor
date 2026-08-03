using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>
    /// Bewegung der Gegnergrundlage. Bewusst ohne NavMesh: im Projekt gibt es
    /// noch keine gebackene Navigation, und dieses Paket soll ohne
    /// Szenenaenderung testbar bleiben. Liegt ein
    /// <see cref="CharacterController"/> vor, wird ueber ihn bewegt,
    /// sonst direkt ueber den Transform.
    ///
    /// In Hurt, Attack und Dead findet keine Ortsveraenderung statt. Gedreht
    /// wird ausserdem nicht in Hurt und Dead.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyMovement : MonoBehaviour
    {
        [Header("Bewegung")]
        [Min(0f)] [SerializeField] private float moveSpeed = 2.6f;

        [Min(0f)]
        [Tooltip("Abstand, ab dem vor dem Ziel angehalten wird.")]
        [SerializeField] private float stopDistance = 1.6f;

        [Min(0f)] [SerializeField] private float turnSpeedDegrees = 540f;

        [Header("Schwerkraft")]
        [Tooltip("Wirkt nur, wenn ein CharacterController vorhanden ist.")]
        [SerializeField] private float gravity = -18f;

        private CharacterController characterController;
        private float verticalVelocity;

        /// <summary>Hat sich der Gegner im letzten Tick bewegt?</summary>
        public bool IsMoving { get; private set; }

        public float StopDistance => stopDistance;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        /// <summary>Setzt Geschwindigkeit und Halteabstand, etwa fuer einen spaeteren Spawner.</summary>
        public void Configure(float newMoveSpeed, float newStopDistance)
        {
            moveSpeed = Mathf.Max(0f, newMoveSpeed);
            stopDistance = Mathf.Max(0f, newStopDistance);
        }

        /// <summary>
        /// Bewegt den Gegner auf das Ziel zu. Wird vom
        /// <see cref="EnemyController"/> getaktet.
        /// </summary>
        public void Tick(
            EnemyFoundationState state, Transform target, float deltaTime)
        {
            IsMoving = false;

            bool mayTurn =
                state != EnemyFoundationState.Hurt &&
                state != EnemyFoundationState.Dead;

            bool mayMove =
                mayTurn && state != EnemyFoundationState.Attack;

            if (target == null || deltaTime <= 0f)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (mayTurn && distance > 0.0001f)
            {
                RotateTowards(toTarget / distance, deltaTime);
            }

            if (!mayMove || distance <= stopDistance || distance <= 0.0001f)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            Vector3 step = toTarget / distance * moveSpeed * deltaTime;

            // Nicht ueber den Halteabstand hinausschiessen.
            float maxStep = distance - stopDistance;

            if (step.magnitude > maxStep)
            {
                step = toTarget / distance * maxStep;
            }

            IsMoving = step.sqrMagnitude > 0f;
            Move(step, deltaTime);
        }

        private void RotateTowards(Vector3 direction, float deltaTime)
        {
            Quaternion desired = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, desired, turnSpeedDegrees * deltaTime);
        }

        private void ApplyGravityOnly(float deltaTime)
        {
            if (characterController == null || deltaTime <= 0f)
            {
                return;
            }

            Move(Vector3.zero, deltaTime);
        }

        private void Move(Vector3 horizontalStep, float deltaTime)
        {
            if (characterController == null)
            {
                transform.position += horizontalStep;
                return;
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }
            else
            {
                verticalVelocity += gravity * deltaTime;
            }

            characterController.Move(
                horizontalStep + Vector3.up * verticalVelocity * deltaTime);
        }
    }
}
