using Elyndor.Interaction;
using UnityEngine;

namespace Elyndor.Puzzles
{
    /// <summary>Wozu ein Teil des Brückenrätsels dient.</summary>
    public enum BridgePartRole
    {
        /// <summary>Der Seilbock: meldet, ob die Spannung hält.</summary>
        TensionCheck = 0,

        /// <summary>Die Stammfreigabe: prüft die Stellung, indem sie sie ausprobiert.</summary>
        Release = 1,

        /// <summary>Eine der beiden Bohlen, die den Übergang sichern.</summary>
        Plank = 2
    }

    /// <summary>
    /// Ein bedienbares Teil des Brückenrätsels. Alle drei Rollen tun dasselbe:
    /// sie geben eine Absicht an den <see cref="BridgePuzzle"/> weiter und
    /// entscheiden nichts selbst.
    ///
    /// Bewusst eine Klasse mit einer Rolle statt dreier fast gleicher Klassen —
    /// der einzige Unterschied wäre die Zeile gewesen, die den Controller
    /// aufruft.
    /// </summary>
    public sealed class BridgePuzzlePart : InteractableBase
    {
        [Header("Rätselteil")]
        [SerializeField] private BridgePartRole role = BridgePartRole.TensionCheck;
        [SerializeField] private BridgePuzzle puzzle;

        [Header("Beschriftung")]
        [SerializeField] private string tensionPrompt = "Seilbock prüfen";
        [SerializeField] private string releasePrompt = "Stamm freigeben";
        [SerializeField] private string plankPrompt = "Bohle legen";

        public BridgePartRole Role => role;

        public override string InteractionPrompt => role switch
        {
            BridgePartRole.Release => releasePrompt,
            BridgePartRole.Plank => plankPrompt,
            _ => tensionPrompt
        };

        protected override void Awake()
        {
            base.Awake();

            if (puzzle == null)
            {
                puzzle = GetComponentInParent<BridgePuzzle>();
            }
        }

        public override bool CanInteract(GameObject interactor)
        {
            if (puzzle == null)
            {
                return false;
            }

            return role switch
            {
                // Der Seilbock darf immer befragt werden, solange überhaupt
                // konfiguriert wird. Er verrät nichts, was das Rätsel löst.
                BridgePartRole.TensionCheck => puzzle.CanRelease ||
                                               puzzle.CanTurnAnchors,
                BridgePartRole.Release => puzzle.CanRelease,
                BridgePartRole.Plank => puzzle.CanPlacePlanks,
                _ => false
            };
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            switch (role)
            {
                case BridgePartRole.TensionCheck:
                    puzzle.CheckTension();
                    break;

                case BridgePartRole.Release:
                    puzzle.TryRelease();
                    break;

                case BridgePartRole.Plank:
                    puzzle.TryPlacePlank();
                    break;
            }
        }
    }
}
