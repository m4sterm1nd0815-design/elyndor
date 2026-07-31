using UnityEngine;

namespace Elyndor.Interaction
{
    /// <summary>
    /// Basisklasse für Interaktionsobjekte in der Welt.
    /// Erwartet einen Trigger-Collider am selben GameObject (oder einem Kind),
    /// über den der <see cref="InteractionDetector"/> das Objekt erkennt.
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [Header("Interaktion")]
        [SerializeField] private string interactionPrompt = "Untersuchen";

        public virtual string InteractionPrompt => interactionPrompt;

        public Vector3 Position => transform.position;

        public virtual bool CanInteract(GameObject interactor) => true;

        public abstract void Interact(GameObject interactor);

        protected virtual void Awake()
        {
            if (!HasTriggerCollider())
            {
                Debug.LogWarning(
                    $"{name}: InteractableBase benötigt einen Trigger-Collider, " +
                    "sonst kann der InteractionDetector das Objekt nicht finden.",
                    this
                );
            }
        }

        private bool HasTriggerCollider()
        {
            foreach (Collider ownCollider in GetComponentsInChildren<Collider>())
            {
                if (ownCollider.isTrigger)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
