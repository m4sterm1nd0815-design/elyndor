using Elyndor.Interaction;
using UnityEngine;

namespace Elyndor.World
{
    /// <summary>
    /// Interaktiver Übergang zwischen Regionen. Der vorhandene
    /// InteractionDetector erkennt das Portal über dessen Trigger-Collider;
    /// die Reise startet erst nach einer bewussten Spielerinteraktion.
    /// </summary>
    public sealed class RegionPortal : InteractableBase
    {
        [Header("Ziel")]
        [SerializeField] private string targetSceneName;
        [SerializeField] private string targetSpawnId;

        [Header("Darstellung")]
        [SerializeField] private string portalPrompt = "Weiterreisen";

        private bool traveling;

        public string TargetSceneName => targetSceneName;
        public string TargetSpawnId => targetSpawnId;

        public override string InteractionPrompt => portalPrompt;

        public override bool CanInteract(GameObject interactor)
        {
            return !traveling &&
                interactor != null &&
                interactor.GetComponent<CharacterController>() != null &&
                !string.IsNullOrWhiteSpace(targetSceneName);
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
                return;

            traveling = true;
            RegionTravel.TravelTo(targetSceneName, targetSpawnId);
        }
    }
}
