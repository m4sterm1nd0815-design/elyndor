using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elyndor.Inventory
{
    /// <summary>Körperplätze der Ausrüstung.</summary>
    public enum EquipmentSlot
    {
        None,
        MainHand,
        Shield,
        Head,
        Cape,
        Hands,
        Chest,
        Belt,
        Legs,
        Feet
    }

    /// <summary>Ein Gegenstand im Inventar (bewusst schlank für den Prototyp).</summary>
    [Serializable]
    public class InventoryItem
    {
        public string Id;
        public string DisplayName;
        public EquipmentSlot Slot;

        public InventoryItem(string id, string displayName, EquipmentSlot slot)
        {
            Id = id;
            DisplayName = displayName;
            Slot = slot;
        }
    }

    /// <summary>
    /// Sitzungsweites Spieler-Inventar: Tascheninhalt plus ausgerüstete
    /// Gegenstände pro Körperplatz. Überlebt Regionswechsel; wird mit dem
    /// Save-System (Roadmap M10) persistiert.
    /// </summary>
    public static class PlayerInventory
    {
        private static readonly List<InventoryItem> bagItems = new List<InventoryItem>();
        private static readonly Dictionary<EquipmentSlot, InventoryItem> equippedItems =
            new Dictionary<EquipmentSlot, InventoryItem>();

        /// <summary>Wird bei jeder Änderung ausgelöst (UI-Aktualisierung).</summary>
        public static event Action Changed;

        public static IReadOnlyList<InventoryItem> BagItems => bagItems;

        public static InventoryItem GetEquipped(EquipmentSlot slot)
        {
            return equippedItems.TryGetValue(slot, out InventoryItem item) ? item : null;
        }

        public static void AddToBag(InventoryItem item)
        {
            if (item == null)
            {
                return;
            }

            bagItems.Add(item);
            Changed?.Invoke();
        }

        /// <summary>Rüstet den Gegenstand aus; ein vorhandener wandert in die Tasche.</summary>
        public static void Equip(InventoryItem item)
        {
            if (item == null || item.Slot == EquipmentSlot.None)
            {
                Debug.LogWarning("PlayerInventory: Gegenstand ist nicht ausrüstbar.");
                return;
            }

            if (equippedItems.TryGetValue(item.Slot, out InventoryItem previous) && previous != null)
            {
                bagItems.Add(previous);
            }

            bagItems.Remove(item);
            equippedItems[item.Slot] = item;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            bagItems.Clear();
            equippedItems.Clear();
            Changed = null;
        }
    }
}
