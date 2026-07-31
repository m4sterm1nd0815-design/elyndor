using Elyndor.Core;
using UnityEngine;

namespace Elyndor.Interaction
{
    /// <summary>
    /// Einfaches untersuchbares Objekt (Schilder, Fundstücke, Spuren).
    /// Zeigt beim Interagieren einen Beschreibungstext über den Narration-Kanal.
    /// </summary>
    public class ExaminableObject : InteractableBase
    {
        [Header("Untersuchen")]
        [SerializeField, TextArea(2, 6)] private string examineText;
        [SerializeField] private float textDuration = 5f;

        public override void Interact(GameObject interactor)
        {
            NarrationEvents.RaiseMessage(examineText, textDuration);
        }
    }
}
