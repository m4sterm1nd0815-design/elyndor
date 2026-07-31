using System.Text;
using Elyndor.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Elyndor.UI
{
    /// <summary>
    /// Inventar-Panel (Taste I / Gamepad Nord): zeigt die neun
    /// Körperplätze und den Tascheninhalt als Textliste. Bewusst
    /// minimal — Icons, Drag-und-Drop und Tooltips folgen mit dem
    /// UI-Ausbau; die Datenschicht (PlayerInventory) bleibt dieselbe.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text equipmentText;
        [SerializeField] private Text bagText;

        private static readonly (EquipmentSlot slot, string label)[] SlotLabels =
        {
            (EquipmentSlot.MainHand, "Haupthand"),
            (EquipmentSlot.Shield, "Schild"),
            (EquipmentSlot.Head, "Mütze"),
            (EquipmentSlot.Cape, "Cape"),
            (EquipmentSlot.Hands, "Handschuhe"),
            (EquipmentSlot.Chest, "Brust"),
            (EquipmentSlot.Belt, "Gürtel"),
            (EquipmentSlot.Legs, "Hose"),
            (EquipmentSlot.Feet, "Schuhe")
        };

        private void OnEnable()
        {
            PlayerInventory.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            PlayerInventory.Changed -= Refresh;
        }

        /// <summary>Wird beim Öffnen (true) / Schließen (false) ausgelöst.</summary>
        public static event System.Action<bool> Toggled;

        private void Update()
        {
            if (ToggleRequested())
            {
                panelRoot.SetActive(!panelRoot.activeSelf);
                Toggled?.Invoke(panelRoot.activeSelf);

                if (panelRoot.activeSelf)
                {
                    Refresh();
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            Toggled = null;
        }

        private static bool ToggleRequested()
        {
            bool keyboard = Keyboard.current != null &&
                Keyboard.current.iKey.wasPressedThisFrame;

            bool gamepad = Gamepad.current != null &&
                Gamepad.current.buttonNorth.wasPressedThisFrame;

            return keyboard || gamepad;
        }

        private void Refresh()
        {
            if (equipmentText == null || bagText == null)
            {
                return;
            }

            StringBuilder equipment = new StringBuilder();

            foreach ((EquipmentSlot slot, string label) in SlotLabels)
            {
                InventoryItem item = PlayerInventory.GetEquipped(slot);
                equipment.Append(label).Append(":  ")
                    .AppendLine(item != null ? item.DisplayName : "—");
            }

            equipmentText.text = equipment.ToString();

            StringBuilder bag = new StringBuilder();

            if (PlayerInventory.BagItems.Count == 0)
            {
                bag.Append("(leer)");
            }
            else
            {
                foreach (InventoryItem item in PlayerInventory.BagItems)
                {
                    bag.Append("· ").AppendLine(item.DisplayName);
                }
            }

            bagText.text = bag.ToString();
        }
    }
}
