using System.Collections;
using System.Reflection;
using Elyndor.Player;
using Elyndor.UIFoundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Elyndor.Tests
{
    public sealed class QuickslotDirectSelectionRuntimeTests :
        InputTestFixture
    {
        private GameObject testRoot;
        private InputActionAsset inputAsset;
        private Keyboard keyboard;
        private PlayerInputReader inputReader;
        private QuickslotRuntimeInventory inventory;
        private QuickslotInputController controller;
        private InputAction[] directActions;
        private QuickslotView[] views;

        public override void Setup()
        {
            base.Setup();

            keyboard = InputSystem.AddDevice<Keyboard>();
            inputAsset = CreateInputAsset();
            testRoot = new GameObject("QuickslotRuntimeTest");
            testRoot.SetActive(false);

            inputReader = testRoot.AddComponent<PlayerInputReader>();
            SetPrivateField(inputReader, "inputActions", inputAsset);

            inventory = testRoot.AddComponent<QuickslotRuntimeInventory>();
            QuickslotBarPresenter presenter =
                testRoot.AddComponent<QuickslotBarPresenter>();
            controller =
                testRoot.AddComponent<QuickslotInputController>();

            views = CreateViews(testRoot.transform);
            presenter.Configure(inventory, null, views);
            controller.Configure(inputReader, presenter);
            testRoot.SetActive(true);

            directActions = GetPrivateField<InputAction[]>(
                inputReader,
                "directQuickslotActions");

            Assert.That(controller.isActiveAndEnabled, Is.True);
            Assert.That(inputReader.isActiveAndEnabled, Is.True);

            foreach (InputAction action in directActions)
                Assert.That(action.enabled, Is.True);
        }

        public override void TearDown()
        {
            if (testRoot != null)
                Object.DestroyImmediate(testRoot);

            if (inputAsset != null)
                Object.DestroyImmediate(inputAsset);

            base.TearDown();
        }

        [UnityTest]
        public IEnumerator NumberRowSelectsEachSlotExactlyOnce()
        {
            KeyControl[] keys =
            {
                keyboard.digit1Key,
                keyboard.digit2Key,
                keyboard.digit3Key,
                keyboard.digit4Key,
                keyboard.digit5Key,
                keyboard.digit6Key,
                keyboard.digit7Key,
                keyboard.digit8Key
            };

            yield return VerifyKeys(keys);
        }

        [UnityTest]
        public IEnumerator NumpadSelectsEachSlotExactlyOnce()
        {
            KeyControl[] keys =
            {
                keyboard.numpad1Key,
                keyboard.numpad2Key,
                keyboard.numpad3Key,
                keyboard.numpad4Key,
                keyboard.numpad5Key,
                keyboard.numpad6Key,
                keyboard.numpad7Key,
                keyboard.numpad8Key
            };

            yield return VerifyKeys(keys);
        }

        private IEnumerator VerifyKeys(KeyControl[] keys)
        {
            int[] actionCallbackCounts =
                new int[directActions.Length];

            for (int actionIndex = 0;
                actionIndex < directActions.Length;
                actionIndex++)
            {
                int capturedIndex = actionIndex;
                directActions[actionIndex].performed += _ =>
                    actionCallbackCounts[capturedIndex]++;
            }

            for (int targetIndex = 0; targetIndex < keys.Length; targetIndex++)
            {
                int differentIndex =
                    targetIndex == 0 ? 1 : 0;
                inventory.SetSelectedIndex(differentIndex);

                int selectionEventCount = 0;
                inventory.SelectionChanged += HandleSelectionChanged;

                Press(keys[targetIndex]);
                yield return null;

                Assert.That(
                    inventory.SelectedIndex,
                    Is.EqualTo(targetIndex),
                    $"Slot {targetIndex + 1} did not reach the inventory.");
                Assert.That(
                    selectionEventCount,
                    Is.EqualTo(1),
                    $"Slot {targetIndex + 1} emitted more than one selection.");
                Assert.That(
                    actionCallbackCounts[targetIndex],
                    Is.LessThanOrEqualTo(1),
                    $"Slot {targetIndex + 1} emitted duplicate action callbacks.");
                Assert.That(
                    views[targetIndex].IsSelectionVisible,
                    Is.True,
                    $"Slot {targetIndex + 1} was not updated by the presenter.");
                Assert.That(
                    inputReader.TryConsumeDirectQuickslotSelection(out _),
                    Is.False,
                    "A single key press remained queued for a second selection.");

                Release(keys[targetIndex]);
                yield return null;

                inventory.SelectionChanged -= HandleSelectionChanged;

                void HandleSelectionChanged(int selectedIndex)
                {
                    selectionEventCount++;
                }
            }
        }

        private static QuickslotView[] CreateViews(Transform parent)
        {
            QuickslotView[] result =
                new QuickslotView[QuickslotRuntimeInventory.SlotCount];

            for (int index = 0; index < result.Length; index++)
            {
                GameObject slot = new GameObject(
                    $"Quickslot {index + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(QuickslotFocusVisual),
                    typeof(QuickslotView));
                slot.transform.SetParent(parent, false);

                Image focusFrame = slot.GetComponent<Image>();
                QuickslotFocusVisual focusVisual =
                    slot.GetComponent<QuickslotFocusVisual>();
                QuickslotView view = slot.GetComponent<QuickslotView>();

                SetPrivateField(focusVisual, "focusFrame", focusFrame);
                SetPrivateField(view, "focusVisual", focusVisual);
                result[index] = view;
            }

            return result;
        }

        private static InputActionAsset CreateInputAsset()
        {
            InputActionAsset asset =
                ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = new InputActionMap("Player");
            asset.AddActionMap(map);

            map.AddAction("Move", InputActionType.Value);
            map.AddAction("Sprint", InputActionType.Button);
            map.AddAction("Jump", InputActionType.Button);
            map.AddAction("Roll", InputActionType.Button);
            map.AddAction("Interact", InputActionType.Button)
                .AddBinding("<Keyboard>/e");
            map.AddAction("QuickslotPrevious", InputActionType.Button);
            map.AddAction("QuickslotNext", InputActionType.Button);
            map.AddAction("QuickslotUse", InputActionType.Button)
                .AddBinding("<Keyboard>/r");

            for (int index = 0;
                index < QuickslotRuntimeInventory.SlotCount;
                index++)
            {
                map.AddAction(
                        $"Quickslot{index + 1}",
                        InputActionType.Button)
                    .AddBinding($"<Keyboard>/digit{index + 1}");
            }

            return asset;
        }

        private static T GetPrivateField<T>(
            object target,
            string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Missing private field {target.GetType().Name}.{fieldName}.");
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField<T>(
            object target,
            string fieldName,
            T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Missing private field {target.GetType().Name}.{fieldName}.");
            field.SetValue(target, value);
        }
    }
}
