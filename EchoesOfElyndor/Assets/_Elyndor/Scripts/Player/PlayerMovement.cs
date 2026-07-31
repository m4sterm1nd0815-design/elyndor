using System.Collections;
using UnityEngine;

namespace Elyndor.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float rotationSpeed = 10f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private float animationDampTime = 0.1f;

        [Header("Dodge Roll")]
        [SerializeField] private float rollSpeed = 12f;
        [SerializeField] private float rollDuration = 0.35f;
        [SerializeField] private float rollCooldown = 0.5f;

        [Header("Jump")]
        [SerializeField] private float jumpVelocity = 8f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -25f;
        [Tooltip("Kleine Abwaertsgeschwindigkeit am Boden, damit der Controller auf Gefaellen Bodenkontakt haelt.")]
        [SerializeField] private float groundedStickVelocity = -2f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        private CharacterController characterController;
        private PlayerInputReader inputReader;
        private Transform cameraTransform;
        private PlayerState currentState = PlayerState.Normal;

        private Vector3 lastMoveDirection = Vector3.forward;
        private float nextRollTime;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputReader = GetComponent<PlayerInputReader>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void Update()
        {
            if (currentState != PlayerState.Normal)
            {
                UpdateAnimationSpeed(0f);
                return;
            }

            Vector2 movementInput = Vector2.ClampMagnitude(inputReader.Move, 1f);
            Vector3 moveDirection = ToCameraRelativeDirection(movementInput);

            bool isMoving = moveDirection.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                lastMoveDirection = moveDirection.normalized;
                RotateTowards(lastMoveDirection);
            }

            if (inputReader.RollPressedThisFrame && Time.time >= nextRollTime)
            {
                Vector3 rollDirection = GetRollDirection(moveDirection);
                StartCoroutine(PerformRoll(rollDirection));
                return;
            }

            bool isSprinting = inputReader.SprintHeld;
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            ApplyGravity();

            if (characterController.isGrounded && inputReader.JumpPressedThisFrame)
            {
                verticalVelocity = jumpVelocity;
            }

            Vector3 velocity =
                moveDirection * currentSpeed +
                Vector3.up * verticalVelocity;

            characterController.Move(velocity * Time.deltaTime);

            float animationSpeed = CalculateAnimationSpeed(
                isMoving,
                isSprinting
            );

            UpdateAnimationSpeed(animationSpeed);
        }

        // Bewegung ist kamerarelativ: "vor" ist immer die Blickrichtung der
        // Kamera, auf die XZ-Ebene projiziert. Ohne Kamera: Weltachsen.
        private Vector3 ToCameraRelativeDirection(Vector2 input)
        {
            if (cameraTransform == null)
            {
                Camera mainCamera = Camera.main;

                if (mainCamera == null)
                {
                    return new Vector3(input.x, 0f, input.y);
                }

                cameraTransform = mainCamera.transform;
            }

            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            return cameraRight * input.x + cameraForward * input.y;
        }

        private Vector3 GetRollDirection(Vector3 currentMoveDirection)
        {
            if (currentMoveDirection.sqrMagnitude > 0.01f)
            {
                return currentMoveDirection.normalized;
            }

            if (lastMoveDirection.sqrMagnitude > 0.01f)
            {
                return lastMoveDirection.normalized;
            }

            return transform.forward;
        }

        private float CalculateAnimationSpeed(
            bool isMoving,
            bool isSprinting
        )
        {
            if (!isMoving)
            {
                return 0f;
            }

            return isSprinting ? 1f : 0.5f;
        }

        private void ApplyGravity()
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickVelocity;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
        }

        private void UpdateAnimationSpeed(float speed)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(
                SpeedHash,
                speed,
                animationDampTime,
                Time.deltaTime
            );
        }

        private IEnumerator PerformRoll(Vector3 rollDirection)
        {
            currentState = PlayerState.Rolling;
            nextRollTime = Time.time + rollCooldown;

            UpdateAnimationSpeed(0f);
            RotateImmediatelyTowards(rollDirection);

            float elapsedTime = 0f;

            while (elapsedTime < rollDuration)
            {
                ApplyGravity();

                Vector3 rollVelocity =
                    rollDirection * rollSpeed +
                    Vector3.up * verticalVelocity;

                characterController.Move(rollVelocity * Time.deltaTime);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            currentState = PlayerState.Normal;
        }

        private void RotateTowards(Vector3 direction)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        private void RotateImmediatelyTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.01f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
