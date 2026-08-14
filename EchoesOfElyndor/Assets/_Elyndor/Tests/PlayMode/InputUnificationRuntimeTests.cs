using System.Collections;
using System.Linq;
using System.Reflection;
using Elyndor.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Regressionstests fuer die vereinheitlichte Eingabeschicht.
    ///
    /// Hintergrund: Gameplay-Code las Maus, Tastatur und Gamepad teilweise
    /// direkt. Dabei lagen <c>Interact</c> und das Inventar am Controller beide
    /// auf <c>buttonNorth</c> — ein Tastendruck oeffnete das Inventar und
    /// untersuchte gleichzeitig das Objekt davor. Solche Kollisionen sind nur
    /// sichtbar, wenn die Eingabe ueber eine gemeinsame Schicht laeuft und
    /// jede Aktion einzeln geprueft werden kann.
    ///
    /// Nach jedem <c>Press</c> bzw. <c>Release</c> folgt ein
    /// <c>yield return null</c>. Grund: <see cref="InputTestFixture"/> erzwingt
    /// in einem <c>[UnityTest]</c> intern <c>queueEventOnly</c> — das Ereignis
    /// wird nur eingereiht und erst vom naechsten Input-Update des Player-Loops
    /// verarbeitet. Ohne diesen Frame liest die Zusicherung den Zustand davor.
    /// </summary>
    public sealed class InputUnificationRuntimeTests : InputTestFixture
    {
        private GameObject playerObject;
        private PlayerInputReader reader;
        private InputActionAsset sourceAsset;
        private Keyboard keyboard;
        private Gamepad gamepad;
        private Mouse mouse;

        public override void Setup()
        {
            base.Setup();

            keyboard = InputSystem.AddDevice<Keyboard>();
            gamepad = InputSystem.AddDevice<Gamepad>();
            mouse = InputSystem.AddDevice<Mouse>();
            sourceAsset = CreateGameplayAsset();
        }

        public override void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
                playerObject = null;
            }

            if (sourceAsset != null)
            {
                sourceAsset.Disable();
                Object.DestroyImmediate(sourceAsset);
                sourceAsset = null;
            }

            base.TearDown();
        }

        // ------------------------------------------------------------------
        // Einzelne Aktionen, jeweils Tastatur und Gamepad
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Attack_ReagiertAufTastaturUndGamepad()
        {
            yield return CreatePlayer();

            Press(mouse.leftButton);
            yield return null;
            Assert.That(reader.AttackPressedThisFrame, Is.True, "Maustaste");

            Release(mouse.leftButton);
            yield return null;
            Assert.That(reader.AttackReleasedThisFrame, Is.True,
                "Loslassen muss eine eigene Flanke liefern — PlayerCombat " +
                "unterscheidet darueber leichten und schweren Schlag.");

            Press(gamepad.rightTrigger);
            yield return null;
            Assert.That(reader.AttackPressedThisFrame, Is.True, "Rechter Trigger");
            Release(gamepad.rightTrigger);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Interact_ReagiertAufTastaturUndGamepad()
        {
            yield return CreatePlayer();

            Press(keyboard.eKey);
            yield return null;
            Assert.That(reader.InteractPressedThisFrame, Is.True, "Taste E");
            Release(keyboard.eKey);
            yield return null;

            Press(gamepad.buttonNorth);
            yield return null;
            Assert.That(reader.InteractPressedThisFrame, Is.True, "buttonNorth");
            Release(gamepad.buttonNorth);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InventoryToggle_ReagiertAufTastaturUndGamepad()
        {
            yield return CreatePlayer();

            Press(keyboard.iKey);
            yield return null;
            Assert.That(reader.InventoryTogglePressedThisFrame, Is.True, "Taste I");
            Release(keyboard.iKey);
            yield return null;

            Press(gamepad.selectButton);
            yield return null;
            Assert.That(reader.InventoryTogglePressedThisFrame, Is.True, "Select");
            Release(gamepad.selectButton);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RollPressedThisFrame_ReagiertAufTastaturUndGamepad()
        {
            yield return CreatePlayer();

            Press(keyboard.cKey);
            yield return null;
            Assert.That(reader.RollPressedThisFrame, Is.True, "Taste C");
            Release(keyboard.cKey);
            yield return null;

            Press(gamepad.buttonEast);
            yield return null;
            Assert.That(reader.RollPressedThisFrame, Is.True, "buttonEast");
            Release(gamepad.buttonEast);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Block_ReagiertAufTastaturUndGamepad()
        {
            yield return CreatePlayer();

            Press(keyboard.qKey);
            yield return null;
            Assert.That(reader.BlockHeld, Is.True, "Taste Q");
            Release(keyboard.qKey);
            yield return null;
            Assert.That(reader.BlockHeld, Is.False, "Nach dem Loslassen");

            Press(gamepad.leftTrigger);
            yield return null;
            Assert.That(reader.BlockHeld, Is.True, "Linker Trigger");
            Release(gamepad.leftTrigger);
            yield return null;
        }

        // ------------------------------------------------------------------
        // Der eigentliche Defekt: gegenseitige Auslösung
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Interact_LoestNiemalsInventoryAus()
        {
            yield return CreatePlayer();

            Press(keyboard.eKey);
            yield return null;
            Assert.That(reader.InteractPressedThisFrame, Is.True);
            Assert.That(reader.InventoryTogglePressedThisFrame, Is.False,
                "Interact darf das Inventar nicht mit oeffnen.");
            Release(keyboard.eKey);
            yield return null;

            Press(gamepad.buttonNorth);
            yield return null;
            Assert.That(reader.InteractPressedThisFrame, Is.True);
            Assert.That(reader.InventoryTogglePressedThisFrame, Is.False,
                "Am Controller lagen beide frueher auf buttonNorth.");
            Release(gamepad.buttonNorth);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Inventory_LoestNiemalsInteractAus()
        {
            yield return CreatePlayer();

            Press(keyboard.iKey);
            yield return null;
            Assert.That(reader.InventoryTogglePressedThisFrame, Is.True);
            Assert.That(reader.InteractPressedThisFrame, Is.False,
                "Das Inventar darf nicht zusaetzlich interagieren.");
            Release(keyboard.iKey);
            yield return null;

            Press(gamepad.selectButton);
            yield return null;
            Assert.That(reader.InventoryTogglePressedThisFrame, Is.True);
            Assert.That(reader.InteractPressedThisFrame, Is.False);
            Release(gamepad.selectButton);
            yield return null;
        }

        // ------------------------------------------------------------------
        // Umschaltbarkeit der Gameplay-Eingabe
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator GameplayInput_DeaktivierenUndWiederAktivieren()
        {
            yield return CreatePlayer();

            reader.SetGameplayInputEnabled(false);
            Assert.That(reader.GameplayMapEnabled, Is.False);

            Press(keyboard.eKey);
            yield return null;
            Assert.That(reader.InteractPressedThisFrame, Is.False,
                "Bei abgeschalteter Gameplay-Eingabe darf nichts durchkommen.");
            Release(keyboard.eKey);
            yield return null;

            Press(keyboard.iKey);
            yield return null;
            Assert.That(reader.InventoryTogglePressedThisFrame, Is.False);
            Release(keyboard.iKey);
            yield return null;

            reader.SetGameplayInputEnabled(true);
            Assert.That(reader.GameplayMapEnabled, Is.True);

            Press(keyboard.eKey);
            yield return null;
            Assert.That(reader.InteractPressedThisFrame, Is.True,
                "Nach dem Wiedereinschalten muss die Eingabe wieder ankommen.");
            Release(keyboard.eKey);
            yield return null;
        }

        // ------------------------------------------------------------------

        private IEnumerator CreatePlayer()
        {
            playerObject = new GameObject("InputUnificationTestPlayer");
            playerObject.SetActive(false);

            reader = playerObject.AddComponent<PlayerInputReader>();
            SetPrivateField(reader, "inputActions", sourceAsset);

            playerObject.SetActive(true);
            yield return null;

            Assert.That(
                reader.GameplayMapEnabled,
                Is.True,
                "Vorbedingung: Die Gameplay-Map muss aktiv sein.");
        }

        /// <summary>
        /// Spiegelt die reale Belegung des Projekt-Assets, damit die Tests
        /// dieselbe Struktur pruefen, die auch ausgeliefert wird.
        /// </summary>
        private static InputActionAsset CreateGameplayAsset()
        {
            InputActionAsset asset =
                ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "InputUnificationTestActions";

            InputActionMap map = new InputActionMap("Player");
            asset.AddActionMap(map);

            map.AddAction("Move", InputActionType.Value)
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            map.AddAction("Look", InputActionType.Value)
                .AddBinding("<Gamepad>/rightStick");

            InputAction sprint = map.AddAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<Keyboard>/leftShift");
            sprint.AddBinding("<Gamepad>/leftStickPress");

            InputAction jump = map.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            // Wie im Projekt-Asset heisst die Rolle noch "Crouch".
            InputAction roll = map.AddAction("Crouch", InputActionType.Button);
            roll.AddBinding("<Keyboard>/c");
            roll.AddBinding("<Gamepad>/buttonEast");

            InputAction interact = map.AddAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonNorth");

            InputAction attack = map.AddAction("Attack", InputActionType.Button);
            attack.AddBinding("<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/rightTrigger");

            InputAction block = map.AddAction("Block", InputActionType.Button);
            block.AddBinding("<Keyboard>/q");
            block.AddBinding("<Gamepad>/leftTrigger");

            InputAction inventory = map.AddAction("Inventory", InputActionType.Button);
            inventory.AddBinding("<Keyboard>/i");
            inventory.AddBinding("<Gamepad>/select");

            map.AddAction("QuickslotPrevious", InputActionType.Button)
                .AddBinding("<Gamepad>/leftShoulder");
            map.AddAction("QuickslotNext", InputActionType.Button)
                .AddBinding("<Gamepad>/rightShoulder");
            map.AddAction("QuickslotUse", InputActionType.Button)
                .AddBinding("<Keyboard>/r");

            for (int index = 0; index < 8; index++)
            {
                map.AddAction($"Quickslot{index + 1}", InputActionType.Button)
                    .AddBinding($"<Keyboard>/digit{index + 1}");
            }

            return asset;
        }

        private static void SetPrivateField<T>(
            object target,
            string fieldName,
            T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Feld '{fieldName}' fehlt.");
            field.SetValue(target, value);
        }
    }
}
