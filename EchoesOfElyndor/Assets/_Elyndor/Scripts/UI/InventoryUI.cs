using System.Text;
using Elyndor.Inventory;
using Elyndor.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Elyndor.UI
{
    /// <summary>
    /// Inventar-Panel (Taste I / Gamepad Select): zeigt die neun
    /// Körperplätze und den Tascheninhalt als Textliste. Bewusst
    /// minimal — Icons, Drag-und-Drop und Tooltips folgen mit dem
    /// UI-Ausbau; die Datenschicht (PlayerInventory) bleibt dieselbe.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text equipmentText;
        [SerializeField] private Text bagText;

        [Tooltip("Optional. Bleibt das Feld leer, wird der Reader zur Laufzeit " +
                 "in der geladenen Szene gesucht.")]
        [SerializeField] private PlayerInputReader inputReader;

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

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = PlayerInputReader.FindInLoadedScenes();
            }
        }

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

        /// <summary>
        /// Eingabe ueber die eigene Inventory-Action statt direkt am Geraet.
        ///
        /// Vorher las diese Klasse <c>Gamepad.buttonNorth</c> selbst — genau die
        /// Taste, auf der auch <c>Interact</c> liegt. Am Controller oeffnete
        /// sich dadurch das Inventar, waehrend gleichzeitig das Objekt vor dem
        /// Spieler untersucht wurde.
        /// </summary>
        private bool ToggleRequested()
        {
            return inputReader != null &&
                inputReader.InventoryTogglePressedThisFrame;
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
