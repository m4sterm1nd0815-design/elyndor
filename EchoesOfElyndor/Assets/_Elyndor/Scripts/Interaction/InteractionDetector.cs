using System;
using System.Collections.Generic;
using Elyndor.Player;
using UnityEngine;

namespace Elyndor.Interaction
{
    /// <summary>
    /// Sitzt am Spieler und verwaltet alle Interaktionsobjekte in Reichweite.
    /// Die Reichweite wird von den Trigger-Collidern der Objekte definiert;
    /// der CharacterController des Spielers loest deren Trigger aus.
    /// Es gibt immer hoechstens ein aktives Ziel (das naechste interagierbare).
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class InteractionDetector : MonoBehaviour
    {
        private readonly List<IInteractable> interactablesInRange =
            new List<IInteractable>();

        private IInteractable currentTarget;
        private PlayerInputReader inputReader;

        /// <summary>Das aktuell aktive Interaktionsziel, null wenn keines in Reichweite.</summary>
        public IInteractable CurrentTarget => currentTarget;

        /// <summary>Wird ausgeloest, wenn sich das aktive Ziel aendert (auch auf null).</summary>
        public event Action<IInteractable> TargetChanged;

        private void Awake()
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            RefreshTarget();

            if (
                currentTarget != null &&
                inputReader.InteractPressedThisFrame
            )
            {
                currentTarget.Interact(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            IInteractable interactable =
                other.GetComponentInParent<IInteractable>();

            if (
                interactable != null &&
                !interactablesInRange.Contains(interactable)
            )
            {
                interactablesInRange.Add(interactable);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            IInteractable interactable =
                other.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                interactablesInRange.Remove(interactable);
            }
        }

        private void RefreshTarget()
        {
            IInteractable bestTarget = FindBestTarget();

            if (ReferenceEquals(bestTarget, currentTarget))
            {
                return;
            }

            currentTarget = bestTarget;
            TargetChanged?.Invoke(currentTarget);
        }

        private IInteractable FindBestTarget()
        {
            IInteractable bestTarget = null;
            float bestSquaredDistance = float.MaxValue;

            for (int i = interactablesInRange.Count - 1; i >= 0; i--)
            {
                IInteractable candidate = interactablesInRange[i];

                if (IsDestroyed(candidate))
                {
                    interactablesInRange.RemoveAt(i);
                    continue;
                }

                if (!candidate.CanInteract(gameObject))
                {
                    continue;
                }

                float squaredDistance =
                    (candidate.Position - transform.position).sqrMagnitude;

                if (squaredDistance < bestSquaredDistance)
                {
                    bestSquaredDistance = squaredDistance;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private static bool IsDestroyed(IInteractable interactable)
        {
            // MonoBehaviour-Implementierungen melden Zerstoerung ueber den
            // ueberladenen Unity-Null-Vergleich.
            return interactable is Component component && component == null;
        }
    }
}
