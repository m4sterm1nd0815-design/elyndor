using Elyndor.Core;
using Elyndor.Interaction;
using UnityEngine;

namespace Elyndor.Inventory
{
    /// <summary>
    /// Der beschädigte Rucksack neben Arens Aufwachstelle (Lore-Bibel:
    /// Brot, Feldflasche, Messer, leere Seiten — und das Schwert).
    /// Einmalig durchsuchbar; das Schwert wird direkt ausgerüstet.
    /// </summary>
    public class BackpackPickup : InteractableBase
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private float textDuration = 9f;

        private const string PickupFlag = "rucksack_durchsucht";

        public override bool CanInteract(GameObject interactor)
        {
            return !QuestState.IsObjectiveCompleted(PickupFlag);
        }

        public override void Interact(GameObject interactor)
        {
            QuestState.CompleteObjective(PickupFlag);

            PlayerInventory.AddToBag(new InventoryItem("brot", "Brot", EquipmentSlot.None));
            PlayerInventory.AddToBag(new InventoryItem("feldflasche", "Leere Feldflasche", EquipmentSlot.None));
            PlayerInventory.AddToBag(new InventoryItem("messer", "Kleines Messer", EquipmentSlot.None));
            PlayerInventory.AddToBag(new InventoryItem("leere_seiten", "Unbeschriebene Seiten", EquipmentSlot.None));

            InventoryItem sword = new InventoryItem("altes_schwert", "Altes Schwert", EquipmentSlot.MainHand);
            PlayerInventory.AddToBag(sword);
            PlayerInventory.Equip(sword);

            NarrationEvents.RaiseMessage(
                "Der Rucksack ist beschädigt, aber gepackt: Brot, eine leere " +
                "Feldflasche, ein kleines Messer, unbeschriebene Seiten — und " +
                "ein Schwert. Arens Hand schließt sich um den Griff, als hätte " +
                "sie es schon tausendmal getan. [I] öffnet das Inventar.",
                textDuration
            );

            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }
        }
    }
}
