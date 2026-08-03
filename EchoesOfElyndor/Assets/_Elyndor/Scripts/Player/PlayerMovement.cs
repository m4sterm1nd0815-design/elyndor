using System.Collections;
using Elyndor.UIFoundation;
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

        [Tooltip("Beschleunigung bei Bewegungsbeginn und Richtungswechseln.")]
        [SerializeField] private float acceleration = 28f;

        [Tooltip("Abbremsung ohne Bewegungseingabe.")]
        [SerializeField] private float deceleration = 36f;

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

        [Tooltip("Kleine Abwaertsgeschwindigkeit am Boden.")]
        [SerializeField] private float groundedStickVelocity = -2f;

        [Header("Stamina")]
        [SerializeField] private PlayerVitals playerVitals;

        [Tooltip("Ausdauerverbrauch pro Sekunde beim Sprinten.")]
        [SerializeField] private float sprintStaminaPerSecond = 18f;

        [Tooltip("Regeneration pro Sekunde.")]
        [SerializeField] private float staminaRegenerationPerSecond = 22f;

        [Tooltip("Wartezeit nach dem letzten Verbrauch.")]
        [SerializeField] private float staminaRegenerationDelay = 1f;

        private static readonly int SpeedHash =
            Animator.StringToHash("Speed");

        private CharacterController characterController;
        private PlayerInputReader inputReader;
        private Transform cameraTransform;

        private PlayerState currentState = PlayerState.Normal;

        private Vector3 lastMoveDirection = Vector3.forward;
        private Vector3 planarVelocity;

        private float nextRollTime;
        private float verticalVelocity;
        private float lastStaminaUseTime = float.NegativeInfinity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputReader = GetComponent<PlayerInputReader>();

            if (playerVitals == null)
            {
                playerVitals = GetComponent<PlayerVitals>();
            }

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

            Vector2 movementInput =
                Vector2.ClampMagnitude(inputReader.Move, 1f);

            Vector3 moveDirection =
                ToCameraRelativeDirection(movementInput);

            bool hasMovementInput =
                moveDirection.sqrMagnitude > 0.01f;

            if (hasMovementInput)
            {
                lastMoveDirection = moveDirection.normalized;
                RotateTowards(lastMoveDirection);
            }

            if (inputReader.RollPressedThisFrame &&
                Time.time >= nextRollTime)
            {
                StartRoll(moveDirection);
                return;
            }

            bool wantsToSprint =
                inputReader.SprintHeld && hasMovementInput;

            bool isSprinting =
                TryMaintainSprint(wantsToSprint);

            float targetSpeed =
                isSprinting ? sprintSpeed : walkSpeed;

            Vector3 targetPlanarVelocity =
                moveDirection * targetSpeed;

            float velocityChangeRate =
                hasMovementInput ? acceleration : deceleration;

            planarVelocity = Vector3.MoveTowards(
                planarVelocity,
                targetPlanarVelocity,
                velocityChangeRate * Time.deltaTime);

            ApplyGravity();

            if (characterController.isGrounded &&
                inputReader.JumpPressedThisFrame)
            {
                verticalVelocity = jumpVelocity;
            }

            Vector3 velocity =
                planarVelocity +
                Vector3.up * verticalVelocity;

            characterController.Move(
                velocity * Time.deltaTime);

            float animationSpeed =
                CalculateAnimationSpeed(planarVelocity.magnitude);

            UpdateAnimationSpeed(animationSpeed);
            RegenerateStamina(isSprinting);
        }

        private bool TryMaintainSprint(bool wantsToSprint)
        {
            if (!wantsToSprint)
            {
                return false;
            }

            if (playerVitals == null)
            {
                return true;
            }

            float frameCost =
                sprintStaminaPerSecond * Time.deltaTime;

            if (!playerVitals.TrySpendStamina(frameCost))
            {
                return false;
            }

            lastStaminaUseTime = Time.time;
            return true;
        }

        private void RegenerateStamina(bool isSprinting)
        {
            if (playerVitals == null || isSprinting)
            {
                return;
            }

            if (Time.time <
                lastStaminaUseTime + staminaRegenerationDelay)
            {
                return;
            }

            playerVitals.ApplyStamina(
                staminaRegenerationPerSecond * Time.deltaTime);
        }

        private void StartRoll(Vector3 moveDirection)
        {
            Vector3 rollDirection =
                GetRollDirection(moveDirection);

            StartCoroutine(PerformRoll(rollDirection));
        }

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

            return cameraRight * input.x +
                   cameraForward * input.y;
        }

        private Vector3 GetRollDirection(
            Vector3 currentMoveDirection)
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
            float movementSpeed)
        {
            if (movementSpeed <= 0.01f)
            {
                return 0f;
            }

            if (movementSpeed <= walkSpeed)
            {
                return Mathf.InverseLerp(
                    0f,
                    walkSpeed,
                    movementSpeed) * 0.5f;
            }

            return Mathf.Lerp(
                0.5f,
                1f,
                Mathf.InverseLerp(
                    walkSpeed,
                    sprintSpeed,
                    movementSpeed));
        }

        private void ApplyGravity()
        {
            if (characterController.isGrounded &&
                verticalVelocity < 0f)
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
                Time.deltaTime);
        }

        private IEnumerator PerformRoll(
            Vector3 rollDirection)
        {
            currentState = PlayerState.Rolling;
            nextRollTime = Time.time + rollCooldown;
            planarVelocity = Vector3.zero;

            UpdateAnimationSpeed(0f);
            RotateImmediatelyTowards(rollDirection);

            float elapsedTime = 0f;

            while (elapsedTime < rollDuration)
            {
                ApplyGravity();

                Vector3 rollVelocity =
                    rollDirection * rollSpeed +
                    Vector3.up * verticalVelocity;

                characterController.Move(
                    rollVelocity * Time.deltaTime);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            currentState = PlayerState.Normal;
        }

        private void RotateTowards(Vector3 direction)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            float rotationBlend =
                1f - Mathf.Exp(
                    -rotationSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationBlend);
        }

        private void RotateImmediatelyTowards(
            Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.01f)
            {
                return;
            }

            transform.rotation =
                Quaternion.LookRotation(direction);
        }

        private void OnValidate()
        {
            walkSpeed = Mathf.Max(0.1f, walkSpeed);

            sprintSpeed = Mathf.Max(
                walkSpeed + 0.1f,
                sprintSpeed);

            rotationSpeed = Mathf.Max(0.1f, rotationSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            deceleration = Mathf.Max(0.1f, deceleration);

            rollSpeed = Mathf.Max(0.1f, rollSpeed);
            rollDuration = Mathf.Max(0.01f, rollDuration);
            rollCooldown = Mathf.Max(0f, rollCooldown);

            jumpVelocity = Mathf.Max(0.1f, jumpVelocity);

            sprintStaminaPerSecond =
                Mathf.Max(0f, sprintStaminaPerSecond);

            staminaRegenerationPerSecond =
                Mathf.Max(0f, staminaRegenerationPerSecond);

            staminaRegenerationDelay =
                Mathf.Max(0f, staminaRegenerationDelay);
        }
    }
}