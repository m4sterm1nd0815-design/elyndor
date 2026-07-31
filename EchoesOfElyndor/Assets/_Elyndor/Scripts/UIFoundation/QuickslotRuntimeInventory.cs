using System;
using UnityEngine;

namespace Elyndor.UIFoundation
{
    [Serializable]
    public sealed class QuickslotRuntimeEntry
    {
        public string displayName;
        public Sprite icon;
        public string glyph;
        [Min(0)] public int amount;
        public bool consumeOnUse = true;
        public float healthDelta;
        public float staminaDelta;
        public float memoryDelta;

        public bool IsEmpty =>
            amount <= 0 ||
            (icon == null && string.IsNullOrWhiteSpace(glyph));

        public void Clear()
        {
            displayName = string.Empty;
            icon = null;
            glyph = string.Empty;
            amount = 0;
            consumeOnUse = true;
            healthDelta = 0f;
            staminaDelta = 0f;
            memoryDelta = 0f;
        }
    }

    public sealed class QuickslotRuntimeInventory : MonoBehaviour
    {
        public const int SlotCount = 8;

        [SerializeField] private QuickslotRuntimeEntry[] slots =
            new QuickslotRuntimeEntry[SlotCount];

        [Range(0, SlotCount - 1)]
        [SerializeField] private int selectedIndex;

        public event Action Changed;
        public event Action<int> SelectionChanged;

        public int SelectedIndex => selectedIndex;

        private void Awake()
        {
            EnsureSlots();
        }

        private void OnValidate()
        {
            EnsureSlots();
            selectedIndex = Mathf.Clamp(selectedIndex, 0, SlotCount - 1);
        }

        public QuickslotRuntimeEntry GetEntry(int index)
        {
            EnsureSlots();
            return index >= 0 && index < slots.Length ? slots[index] : null;
        }

        public void SetSelectedIndex(int index)
        {
            int clamped = Mathf.Clamp(index, 0, SlotCount - 1);
            if (selectedIndex == clamped)
                return;

            selectedIndex = clamped;
            SelectionChanged?.Invoke(selectedIndex);
        }

        public bool UseSelected(PlayerVitals vitals)
        {
            return UseSlot(selectedIndex, vitals);
        }

        public bool UseSlot(int index, PlayerVitals vitals)
        {
            QuickslotRuntimeEntry entry = GetEntry(index);
            if (entry == null || entry.IsEmpty || vitals == null)
                return false;

            vitals.ApplyHealth(entry.healthDelta);
            vitals.ApplyStamina(entry.staminaDelta);
            vitals.ApplyMemory(entry.memoryDelta);

            if (entry.consumeOnUse)
            {
                entry.amount = Mathf.Max(0, entry.amount - 1);
                if (entry.amount == 0)
                    entry.Clear();
            }

            Changed?.Invoke();
            return true;
        }

        public void ConfigurePrototypeLoadout()
        {
            EnsureSlots();

            ConfigureEntry(
                slots[0],
                "Heiltrank",
                "H",
                3,
                healthDelta: 30f);

            ConfigureEntry(
                slots[1],
                "Ausdauertrank",
                "A",
                2,
                staminaDelta: 35f);

            ConfigureEntry(
                slots[2],
                "Erinnerungssplitter",
                "E",
                2,
                memoryDelta: 25f);

            for (int i = 3; i < slots.Length; i++)
                slots[i].Clear();

            selectedIndex = 0;
            Changed?.Invoke();
            SelectionChanged?.Invoke(selectedIndex);
        }

        private static void ConfigureEntry(
            QuickslotRuntimeEntry entry,
            string displayName,
            string glyph,
            int amount,
            float healthDelta = 0f,
            float staminaDelta = 0f,
            float memoryDelta = 0f)
        {
            entry.displayName = displayName;
            entry.icon = null;
            entry.glyph = glyph;
            entry.amount = amount;
            entry.consumeOnUse = true;
            entry.healthDelta = healthDelta;
            entry.staminaDelta = staminaDelta;
            entry.memoryDelta = memoryDelta;
        }

        private void EnsureSlots()
        {
            if (slots == null || slots.Length != SlotCount)
            {
                QuickslotRuntimeEntry[] replacement =
                    new QuickslotRuntimeEntry[SlotCount];

                if (slots != null)
                {
                    int copyCount = Mathf.Min(slots.Length, replacement.Length);
                    Array.Copy(slots, replacement, copyCount);
                }

                slots = replacement;
            }

            for (int i = 0; i < slots.Length; i++)
                slots[i] ??= new QuickslotRuntimeEntry();
        }
    }
}
