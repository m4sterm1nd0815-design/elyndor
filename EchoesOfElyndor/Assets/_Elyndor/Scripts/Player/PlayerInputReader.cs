using UnityEngine;
using UnityEngine.InputSystem;

namespace Elyndor.Player
{
    /// <summary>
    /// Zentrale Eingabeschicht fuer das Gameplay. Bevorzugt das vorhandene
    /// Input-Actions-Asset und faellt nur dann auf eine zur Laufzeit erzeugte
    /// Standardbelegung zurueck, wenn noch kein Asset verbunden ist.
    /// Gameplay-Komponenten greifen dadurch nicht mehr direkt auf Keyboard
    /// oder Gamepad zu.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [Header("Input Actions (optional)")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string gameplayMapName = "Player";

        [Header("Action Names")]
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string sprintActionName = "Sprint";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string rollActionName = "Roll";
        [Tooltip("Das aktuelle Actions-Asset besitzt noch Crouch statt Roll.")]
        [SerializeField] private string legacyRollActionName = "Crouch";
        [SerializeField] private string interactActionName = "Interact";

        [Header("Quickslots")]
        [SerializeField] private string quickslotPreviousActionName =
            "QuickslotPrevious";
        [SerializeField] private string quickslotNextActionName =
            "QuickslotNext";
        [SerializeField] private string quickslotUseActionName =
            "QuickslotUse";
        [SerializeField] private string[] directQuickslotActionNames =
        {
            "Quickslot1",
            "Quickslot2",
            "Quickslot3",
            "Quickslot4",
            "Quickslot5",
            "Quickslot6",
            "Quickslot7",
            "Quickslot8"
        };

        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private InputAction rollAction;
        private InputAction interactAction;
        private InputAction quickslotPreviousAction;
        private InputAction quickslotNextAction;
        private InputAction quickslotUseAction;
        private readonly InputAction[] directQuickslotActions =
            new InputAction[8];
        private bool initialized;

        public Vector2 Move =>
            moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        public bool SprintHeld =>
            sprintAction != null && sprintAction.IsPressed();

        public bool JumpPressedThisFrame =>
            jumpAction != null && jumpAction.WasPressedThisFrame();

        public bool RollPressedThisFrame =>
            rollAction != null && rollAction.WasPressedThisFrame();

        public bool InteractPressedThisFrame =>
            interactAction != null && interactAction.WasPressedThisFrame();

        public bool QuickslotPreviousPressedThisFrame =>
            quickslotPreviousAction != null &&
            quickslotPreviousAction.WasPressedThisFrame();

        public bool QuickslotNextPressedThisFrame =>
            quickslotNextAction != null &&
            quickslotNextAction.WasPressedThisFrame();

        public bool QuickslotUsePressedThisFrame =>
            quickslotUseAction != null &&
            quickslotUseAction.WasPressedThisFrame();

        public int GetDirectQuickslotPressedThisFrame()
        {
            for (int i = 0; i < directQuickslotActions.Length; i++)
            {
                if (directQuickslotActions[i] != null &&
                    directQuickslotActions[i].WasPressedThisFrame())
                {
                    return i;
                }
            }

            return -1;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            gameplayMap?.Enable();
        }

        private void OnDisable()
        {
            gameplayMap?.Disable();
        }

        /// <summary>
        /// Schaltet die komplette Gameplay-Map um. Das ist der spaetere
        /// Anschlusspunkt fuer Dialoge, Pause und Memory-Vision.
        /// </summary>
        public void SetGameplayInputEnabled(bool enabled)
        {
            Initialize();

            if (enabled)
            {
                gameplayMap?.Enable();
            }
            else
            {
                gameplayMap?.Disable();
            }
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            InputActionAsset resolvedAsset = ResolveInputAsset();

            if (resolvedAsset != null && TryBindActions(resolvedAsset))
            {
                return;
            }

            CreateRuntimeFallbackMap();

            Debug.LogWarning(
                "PlayerInputReader: Kein vollstaendiges Player-Action-Map-Asset " +
                "gefunden. Standardbelegung wird zur Laufzeit verwendet. " +
                "Das InputSystem_Actions-Asset kann spaeter im Inspector " +
                "zugewiesen werden.",
                this
            );
        }

        private InputActionAsset ResolveInputAsset()
        {
            if (inputActions != null)
            {
                return inputActions;
            }

            PlayerInput playerInput = GetComponent<PlayerInput>();

            if (playerInput != null && playerInput.actions != null)
            {
                return playerInput.actions;
            }

#if UNITY_6000_0_OR_NEWER
            if (InputSystem.actions != null)
            {
                return InputSystem.actions;
            }
#endif

            return null;
        }

        private bool TryBindActions(InputActionAsset asset)
        {
            InputActionMap map = asset.FindActionMap(gameplayMapName, false);

            if (map == null)
            {
                return false;
            }

            InputAction resolvedMove = map.FindAction(moveActionName, false);
            InputAction resolvedSprint = map.FindAction(sprintActionName, false);
            InputAction resolvedJump = map.FindAction(jumpActionName, false);
            InputAction resolvedRoll =
                map.FindAction(rollActionName, false) ??
                map.FindAction(legacyRollActionName, false);
            InputAction resolvedInteract = map.FindAction(interactActionName, false);
            InputAction resolvedQuickslotPrevious =
                map.FindAction(quickslotPreviousActionName, false);
            InputAction resolvedQuickslotNext =
                map.FindAction(quickslotNextActionName, false);
            InputAction resolvedQuickslotUse =
                map.FindAction(quickslotUseActionName, false);

            InputAction[] resolvedDirectQuickslots =
                new InputAction[directQuickslotActions.Length];

            for (int i = 0; i < resolvedDirectQuickslots.Length; i++)
            {
                string actionName = GetDirectQuickslotActionName(i);

                resolvedDirectQuickslots[i] =
                    string.IsNullOrWhiteSpace(actionName)
                        ? null
                        : map.FindAction(actionName, false);
            }

            if (
                resolvedMove == null ||
                resolvedSprint == null ||
                resolvedJump == null ||
                resolvedRoll == null ||
                resolvedInteract == null ||
                resolvedQuickslotPrevious == null ||
                resolvedQuickslotNext == null ||
                resolvedQuickslotUse == null ||
                System.Array.Exists(
                    resolvedDirectQuickslots,
                    action => action == null)
            )
            {
                return false;
            }

            gameplayMap = map;
            moveAction = resolvedMove;
            sprintAction = resolvedSprint;
            jumpAction = resolvedJump;
            rollAction = resolvedRoll;
            interactAction = resolvedInteract;
            quickslotPreviousAction = resolvedQuickslotPrevious;
            quickslotNextAction = resolvedQuickslotNext;
            quickslotUseAction = resolvedQuickslotUse;
            System.Array.Copy(
                resolvedDirectQuickslots,
                directQuickslotActions,
                directQuickslotActions.Length);
            return true;
        }

        private void CreateRuntimeFallbackMap()
        {
            gameplayMap = new InputActionMap("Player_RuntimeFallback");

            moveAction = gameplayMap.AddAction(
                moveActionName,
                InputActionType.Value,
                expectedControlLayout: "Vector2"
            );

            moveAction
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            moveAction
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            moveAction.AddBinding("<Gamepad>/leftStick");

            sprintAction = gameplayMap.AddAction(
                sprintActionName,
                InputActionType.Button
            );
            sprintAction.AddBinding("<Keyboard>/leftShift");
            sprintAction.AddBinding("<Gamepad>/leftStickPress");

            jumpAction = gameplayMap.AddAction(
                jumpActionName,
                InputActionType.Button
            );
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            rollAction = gameplayMap.AddAction(
                rollActionName,
                InputActionType.Button
            );
            rollAction.AddBinding("<Keyboard>/leftCtrl");
            rollAction.AddBinding("<Gamepad>/buttonEast");

            interactAction = gameplayMap.AddAction(
                interactActionName,
                InputActionType.Button
            );
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonWest");

            quickslotPreviousAction = gameplayMap.AddAction(
                quickslotPreviousActionName,
                InputActionType.Button
            );
            quickslotPreviousAction.AddBinding("<Gamepad>/leftShoulder");

            quickslotNextAction = gameplayMap.AddAction(
                quickslotNextActionName,
                InputActionType.Button
            );
            quickslotNextAction.AddBinding("<Gamepad>/rightShoulder");

            quickslotUseAction = gameplayMap.AddAction(
                quickslotUseActionName,
                InputActionType.Button
            );
            quickslotUseAction.AddBinding("<Keyboard>/r");
            quickslotUseAction.AddBinding("<Gamepad>/rightStickPress");

            for (int i = 0; i < directQuickslotActions.Length; i++)
            {
                string actionName = GetDirectQuickslotActionName(i);

                InputAction action = gameplayMap.AddAction(
                    actionName,
                    InputActionType.Button
                );
                action.AddBinding($"<Keyboard>/{i + 1}");
                directQuickslotActions[i] = action;
            }
        }

        private string GetDirectQuickslotActionName(int index)
        {
            if (directQuickslotActionNames != null &&
                index >= 0 &&
                index < directQuickslotActionNames.Length &&
                !string.IsNullOrWhiteSpace(directQuickslotActionNames[index]))
            {
                return directQuickslotActionNames[index];
            }

            return $"Quickslot{index + 1}";
        }
    }
}

