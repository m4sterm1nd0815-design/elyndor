using UnityEngine;

namespace Elyndor.Interaction
{
    /// <summary>
    /// Gemeinsamer Vertrag für alles, womit Aren interagieren kann:
    /// Untersuchen, Aufheben, Gespräche, Türen, Hebel, Memory Sites, NPCs.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Kontextabhängiger Hinweistext, z. B. "Untersuchen".</summary>
        string InteractionPrompt { get; }

        /// <summary>Weltposition des Objekts (für die Zielauswahl nach Distanz).</summary>
        Vector3 Position { get; }

        /// <summary>Ob das Objekt aktuell interagierbar ist.</summary>
        bool CanInteract(GameObject interactor);

        /// <summary>Führt die Interaktion aus.</summary>
        void Interact(GameObject interactor);
    }
}
