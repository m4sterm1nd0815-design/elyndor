using Elyndor.UIFoundation;
using UnityEngine;

namespace Elyndor.Puzzles
{
    /// <summary>
    /// Der Ortskontext, in dem die Memory Watch ihre Bereitschaft meldet.
    ///
    /// Bewusst ein eigener Auslöser und keine Abstandsprüfung im Rätsel: die
    /// Zone ist eine gestalterische Größe, die in der Szene sichtbar
    /// eingestellt werden soll, nicht eine Zahl im Code.
    ///
    /// Erkannt wird der Spieler an seinen <see cref="PlayerVitals"/> — dasselbe
    /// Kennzeichen, das auch die Gegnerwahrnehmung benutzt.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class WatchResonanceZone : MonoBehaviour
    {
        [SerializeField] private BridgePuzzle puzzle;

        private int occupants;

        /// <summary>Steht der Spieler gerade in der Zone?</summary>
        public bool IsOccupied => occupants > 0;

        private void Awake()
        {
            if (puzzle == null)
            {
                puzzle = GetComponentInParent<BridgePuzzle>();
            }

            Collider zone = GetComponent<Collider>();

            if (zone != null && !zone.isTrigger)
            {
                Debug.LogWarning(
                    $"{name}: Die Resonanzzone braucht einen Trigger-Collider.",
                    this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            occupants++;

            if (occupants == 1)
            {
                puzzle?.SetPlayerInZone(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            // Nie unter null: ein Spieler mit mehreren Collidern soll die
            // Zaehlung nicht ins Negative ziehen.
            occupants = Mathf.Max(0, occupants - 1);

            if (occupants == 0)
            {
                puzzle?.SetPlayerInZone(false);
            }
        }

        private static bool IsPlayer(Collider other)
        {
            return other != null &&
                   other.GetComponentInParent<PlayerVitals>() != null;
        }
    }
}
