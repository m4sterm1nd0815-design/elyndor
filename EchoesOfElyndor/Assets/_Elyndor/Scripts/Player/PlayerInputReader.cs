using System;
using System.Linq;
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
        [SerializeField] private string lookActionName = "Look";
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
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private InputAction rollAction;
        private InputAction interactAction;
        private InputAction quickslotPreviousAction;
        private InputAction quickslotNextAction;
        private InputAction quickslotUseAction;
        private readonly InputAction[] directQuickslotActions =
            new InputAction[8];
        private static readonly Key[] NumberRowQuickslotKeys =
        {
            Key.Digit1,
            Key.Digit2,
            Key.Digit3,
            Key.Digit4,
            Key.Digit5,
            Key.Digit6,
            Key.Digit7,
            Key.Digit8
        };
        private static readonly Key[] NumpadQuickslotKeys =
        {
            Key.Numpad1,
            Key.Numpad2,
            Key.Numpad3,
            Key.Numpad4,
            Key.Numpad5,
            Key.Numpad6,
            Key.Numpad7,
            Key.Numpad8
        };
        private InputActionAsset runtimeInputActions;
        private int pendingDirectQuickslotIndex = -1;
        private int lastDirectQuickslotSelectionFrame = -1;
        private bool directQuickslotCallbacksRegistered;
        private bool gameplayInputEnabled;
        private bool initialized;

        public Vector2 Move =>
            moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        public Vector2 CameraLook
        {
            get
            {
                if (!gameplayInputEnabled || lookAction == null)
                    return Vector2.zero;

                bool usesMouse = lookAction.activeControl?.device is Mouse;
                if (usesMouse &&
                    (Mouse.current == null ||
                     !Mouse.current.rightButton.isPressed))
                {
                    return Vector2.zero;
                }

                return lookAction.ReadValue<Vector2>();
            }
        }

        public bool CameraLookUsesPointer =>
            lookAction?.activeControl?.device is Pointer;

        public float CameraZoom =>
            gameplayInputEnabled && Mouse.current != null
                ? Mouse.current.scroll.ReadValue().y
                : 0f;

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

        public bool TryConsumeDirectQuickslotSelection(out int index)
        {
            if (!gameplayInputEnabled ||
                lastDirectQuickslotSelectionFrame == Time.frameCount)
            {
                index = -1;
                return false;
            }

            int actionIndex = pendingDirectQuickslotIndex;
            pendingDirectQuickslotIndex = -1;

            int keyboardIndex = GetKeyboardDirectQuickslotPressedThisFrame();
            index = actionIndex >= 0 ? actionIndex : keyboardIndex;

            if (index >= 0)
                lastDirectQuickslotSelectionFrame = Time.frameCount;

            return index >= 0;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            RegisterDirectQuickslotCallbacks();
            gameplayMap?.Enable();
            gameplayInputEnabled = true;
        }

        private void OnDisable()
        {
            gameplayInputEnabled = false;
            UnregisterDirectQuickslotCallbacks();
            gameplayMap?.Disable();
            pendingDirectQuickslotIndex = -1;
        }

        private void OnDestroy()
        {
            UnregisterDirectQuickslotCallbacks();
            gameplayMap?.Disable();

            if (runtimeInputActions != null)
            {
                Destroy(runtimeInputActions);
                runtimeInputActions = null;
            }
        }

        /// <summary>
        /// Schaltet die komplette Gameplay-Map um. Das ist der spaetere
        /// Anschlusspunkt fuer Dialoge, Pause und Memory-Vision.
        /// </summary>
        public void SetGameplayInputEnabled(bool enabled)
        {
            Initialize();
            gameplayInputEnabled = enabled;

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

            if (resolvedAsset != null)
            {
                runtimeInputActions = Instantiate(resolvedAsset);
                runtimeInputActions.name = $"{resolvedAsset.name} (Runtime)";

                if (TryBindActions(runtimeInputActions))
                {
                    return;
                }

                Destroy(runtimeInputActions);
                runtimeInputActions = null;
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
            InputAction resolvedLook = map.FindAction(lookActionName, false);
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
                resolvedLook == null ||
                resolvedSprint == null ||
                resolvedJump == null ||
                resolvedRoll == null ||
                resolvedInteract == null ||
                resolvedQuickslotPrevious == null ||
                resolvedQuickslotNext == null ||
                resolvedQuickslotUse == null ||
                Array.Exists(
                    resolvedDirectQuickslots,
                    action => action == null) ||
                !HasBinding(resolvedInteract, "<Keyboard>/e") ||
                resolvedDirectQuickslots.Where((action, index) =>
                    !HasBinding(action, $"<Keyboard>/digit{index + 1}"))
                    .Any()
            )
            {
                return false;
            }

            gameplayMap = map;
            moveAction = resolvedMove;
            lookAction = resolvedLook;
            sprintAction = resolvedSprint;
            jumpAction = resolvedJump;
            rollAction = resolvedRoll;
            interactAction = resolvedInteract;
            quickslotPreviousAction = resolvedQuickslotPrevious;
            quickslotNextAction = resolvedQuickslotNext;
            quickslotUseAction = resolvedQuickslotUse;
            Array.Copy(
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

            lookAction = gameplayMap.AddAction(
                lookActionName,
                InputActionType.Value,
                expectedControlLayout: "Vector2"
            );
            lookAction.AddBinding("<Pointer>/delta");
            lookAction.AddBinding("<Gamepad>/rightStick");

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
                action.AddBinding($"<Keyboard>/digit{i + 1}");
                action.AddBinding($"<Keyboard>/numpad{i + 1}");
                directQuickslotActions[i] = action;
            }
        }

        private static bool HasBinding(InputAction action, string path)
        {
            return action != null && action.bindings.Any(binding =>
                string.Equals(
                    binding.path,
                    path,
                    StringComparison.OrdinalIgnoreCase));
        }

        private void RegisterDirectQuickslotCallbacks()
        {
            if (directQuickslotCallbacksRegistered)
                return;

            foreach (InputAction action in directQuickslotActions)
            {
                if (action != null)
                    action.performed += HandleDirectQuickslotPerformed;
            }

            directQuickslotCallbacksRegistered = true;
        }

        private void UnregisterDirectQuickslotCallbacks()
        {
            if (!directQuickslotCallbacksRegistered)
                return;

            foreach (InputAction action in directQuickslotActions)
            {
                if (action != null)
                    action.performed -= HandleDirectQuickslotPerformed;
            }

            directQuickslotCallbacksRegistered = false;
        }

        private void HandleDirectQuickslotPerformed(
            InputAction.CallbackContext context)
        {
            int index = Array.IndexOf(directQuickslotActions, context.action);

            if (index >= 0 &&
                lastDirectQuickslotSelectionFrame != Time.frameCount)
            {
                pendingDirectQuickslotIndex = index;
            }
        }

        private static int GetKeyboardDirectQuickslotPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return -1;

            for (int index = 0; index < NumberRowQuickslotKeys.Length; index++)
            {
                if (keyboard[NumberRowQuickslotKeys[index]].wasPressedThisFrame ||
                    keyboard[NumpadQuickslotKeys[index]].wasPressedThisFrame)
                {
                    return index;
                }
            }

            return -1;
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

