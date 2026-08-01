using System;
using UnityEngine;

namespace Elyndor.UIFoundation
{
    public sealed class QuickslotBarPresenter : MonoBehaviour
    {
        [SerializeField] private QuickslotRuntimeInventory inventory;
        [SerializeField] private PlayerVitals playerVitals;
        [SerializeField] private QuickslotView[] views = Array.Empty<QuickslotView>();

        public int SelectedIndex => inventory != null
            ? inventory.SelectedIndex
            : 0;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
            ApplySelection(inventory != null ? inventory.SelectedIndex : 0);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            QuickslotRuntimeInventory runtimeInventory,
            PlayerVitals vitals,
            QuickslotView[] quickslotViews)
        {
            Unsubscribe();

            inventory = runtimeInventory;
            playerVitals = vitals;
            views = quickslotViews ?? Array.Empty<QuickslotView>();

            if (isActiveAndEnabled)
                Subscribe();

            Refresh();
            ApplySelection(inventory != null ? inventory.SelectedIndex : 0);
        }

        public void SelectSlot(int index)
        {
            inventory?.SetSelectedIndex(index);
        }

        public void FocusSlot(int index)
        {
            SelectSlot(index);
            ApplySelection(index);
        }

        public void UseSlot(int index)
        {
            inventory?.UseSlot(index, playerVitals);
        }

        public void UseSelected()
        {
            inventory?.UseSelected(playerVitals);
        }

        public void Refresh()
        {
            for (int i = 0; i < views.Length; i++)
            {
                QuickslotRuntimeEntry entry = inventory?.GetEntry(i);

                if (entry == null || entry.IsEmpty)
                {
                    views[i].SetVisual(null, string.Empty, 0);
                    continue;
                }

                views[i].SetVisual(entry.icon, entry.glyph, entry.amount);
            }
        }

        private void Subscribe()
        {
            if (inventory == null)
                return;

            inventory.Changed -= Refresh;
            inventory.SelectionChanged -= ApplySelection;
            inventory.Changed += Refresh;
            inventory.SelectionChanged += ApplySelection;
        }

        private void Unsubscribe()
        {
            if (inventory == null)
                return;

            inventory.Changed -= Refresh;
            inventory.SelectionChanged -= ApplySelection;
        }

        private void ApplySelection(int index)
        {
            if (views == null)
                return;

            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] != null)
                    views[i].SetSelected(i == index);
            }
        }
    }
}
