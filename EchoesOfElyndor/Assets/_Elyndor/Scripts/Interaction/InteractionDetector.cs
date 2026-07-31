using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Elyndor.Interaction
{
    /// <summary>
    /// Sitzt am Spieler und verwaltet alle Interaktionsobjekte in Reichweite.
    /// Die Reichweite wird von den Trigger-Collidern der Objekte definiert;
    /// der CharacterController des Spielers löst deren Trigger aus.
    /// Es gibt immer höchstens ein aktives Ziel (das nächste interagierbare).
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        private readonly List<IInteractable> interactablesInRange = new List<IInteractable>();

        private IInteractable currentTarget;

        /// <summary>Das aktuell aktive Interaktionsziel, null wenn keines in Reichweite.</summary>
        public IInteractable CurrentTarget => currentTarget;

        /// <summary>Wird ausgelöst, wenn sich das aktive Ziel ändert (auch auf null).</summary>
        public event Action<IInteractable> TargetChanged;

        private void Update()
        {
            RefreshTarget();

            if (currentTarget != null && ReadInteractInput())
            {
                currentTarget.Interact(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            IInteractable interactable = other.GetComponentInParent<IInteractable>();

            if (interactable != null && !interactablesInRange.Contains(interactable))
            {
                interactablesInRange.Add(interactable);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            IInteractable interactable = other.GetComponentInParent<IInteractable>();

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
            // MonoBehaviour-Implementierungen melden Zerstörung über den
            // überladenen Unity-Null-Vergleich.
            return interactable is Component component && component == null;
        }

        // Direkter Device-Zugriff wie in PlayerMovement; wird mit dem geplanten
        // Input-Refactor (Roadmap M3) auf das Input-Actions-Asset umgestellt.
        private bool ReadInteractInput()
        {
            bool keyboardInteract =
                Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame;

            bool gamepadInteract =
                Gamepad.current != null &&
                Gamepad.current.buttonWest.wasPressedThisFrame;

            return keyboardInteract || gamepadInteract;
        }
    }
}
