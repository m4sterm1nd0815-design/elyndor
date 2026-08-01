using System.Collections.Generic;
using Elyndor.Player;
using UnityEngine;

namespace Elyndor.UIFoundation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class QuickslotInputController : MonoBehaviour
    {
        private static readonly HashSet<QuickslotInputController>
            ActiveControllers = new HashSet<QuickslotInputController>();

        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private QuickslotBarPresenter presenter;

        public void Configure(
            PlayerInputReader playerInputReader,
            QuickslotBarPresenter quickslotPresenter)
        {
            ActiveControllers.Remove(this);
            inputReader = playerInputReader;
            presenter = quickslotPresenter;

            if (isActiveAndEnabled)
                RegisterOrDisableDuplicate();
        }

        private void Awake()
        {
            if (inputReader == null)
                inputReader = GetComponent<PlayerInputReader>();
        }

        private void OnEnable()
        {
            RegisterOrDisableDuplicate();
        }

        private void OnDisable()
        {
            ActiveControllers.Remove(this);
        }

        private void Update()
        {
            if (inputReader == null || presenter == null)
                return;

            int directSelection =
                inputReader.GetDirectQuickslotPressedThisFrame();

            if (directSelection >= 0)
            {
                presenter.SelectSlot(directSelection);
            }
            else if (inputReader.QuickslotPreviousPressedThisFrame)
            {
                presenter.SelectSlot(WrapIndex(presenter.SelectedIndex - 1));
            }
            else if (inputReader.QuickslotNextPressedThisFrame)
            {
                presenter.SelectSlot(WrapIndex(presenter.SelectedIndex + 1));
            }

            if (inputReader.QuickslotUsePressedThisFrame)
                presenter.UseSelected();
        }

        private static int WrapIndex(int index)
        {
            int slotCount = QuickslotRuntimeInventory.SlotCount;
            return (index % slotCount + slotCount) % slotCount;
        }

        private void RegisterOrDisableDuplicate()
        {
            ActiveControllers.RemoveWhere(controller => controller == null);

            foreach (QuickslotInputController controller in ActiveControllers)
            {
                if (!ControlsSameQuickslots(controller))
                    continue;

                Debug.LogWarning(
                    "QuickslotInputController: Ein anderer aktiver " +
                    "Controller steuert bereits dieselben Quickslots. " +
                    $"'{name}' wird deaktiviert.",
                    this);

                enabled = false;
                return;
            }

            ActiveControllers.Add(this);
        }

        private bool ControlsSameQuickslots(
            QuickslotInputController otherController)
        {
            if (otherController == null ||
                otherController == this ||
                otherController.gameObject.scene != gameObject.scene)
            {
                return false;
            }

            bool sharesInputReader =
                inputReader != null &&
                otherController.inputReader == inputReader;

            bool sharesPresenter =
                presenter != null &&
                otherController.presenter == presenter;

            return sharesInputReader || sharesPresenter;
        }
    }
}
