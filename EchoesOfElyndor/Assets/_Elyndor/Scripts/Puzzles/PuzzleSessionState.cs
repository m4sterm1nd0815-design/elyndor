using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elyndor.Puzzles
{
    /// <summary>
    /// Hält für die laufende Spielsitzung fest, wie weit ein Rätsel gelöst ist
    /// — auch über Szenenwechsel hinweg.
    ///
    /// Bewusst nach dem Vorbild von <c>MemorySessionState</c> gebaut und
    /// bewusst <em>kein</em> allgemeiner Save-Manager. Das globale
    /// Speicher- und Checkpoint-System ist noch nicht entschieden; es für ein
    /// einzelnes Rätsel vorwegzunehmen hiesse, diese Entscheidung nebenbei zu
    /// treffen. Was hier fehlt, ist deshalb genau eine Sache: die Persistenz
    /// über das Programmende hinaus. Sie kommt mit dem Save-System, und dann
    /// an einer Stelle für beide Zustände.
    /// </summary>
    public static class PuzzleSessionState
    {
        private static readonly Dictionary<string, BridgePuzzleState> bridgeStates =
            new Dictionary<string, BridgePuzzleState>();

        /// <summary>
        /// Meldet jede Aenderung. Das Speichersystem haengt sich hier ein,
        /// statt einzelne Aufrufer zu kennen.
        /// </summary>
        public static event Action Changed;

        /// <summary>Alle gemerkten Raetselstaende. Nur lesbar.</summary>
        public static IReadOnlyDictionary<string, BridgePuzzleState> BridgeStates =>
            bridgeStates;

        /// <summary>Der gespeicherte Stand, oder <see cref="BridgePuzzleState.Dormant"/>.</summary>
        public static BridgePuzzleState GetBridgeState(string puzzleId)
        {
            if (string.IsNullOrEmpty(puzzleId))
            {
                return BridgePuzzleState.Dormant;
            }

            return bridgeStates.TryGetValue(puzzleId, out BridgePuzzleState state)
                ? state
                : BridgePuzzleState.Dormant;
        }

        /// <summary>
        /// Schreibt den Stand. Übergangszustände werden auf ihren letzten
        /// stabilen Stand zurückgeführt — ein Szenenwechsel mitten in der
        /// Stammbewegung darf niemals Zwischengeometrie wiederherstellen.
        /// </summary>
        public static void SetBridgeState(
            string puzzleId, BridgePuzzleState state)
        {
            if (string.IsNullOrEmpty(puzzleId))
            {
                Debug.LogWarning(
                    "PuzzleSessionState: Leere Raetsel-ID kann nicht " +
                    "gespeichert werden.");
                return;
            }

            BridgePuzzleState stable = BridgePuzzleRules.StableFallback(state);

            if (bridgeStates.TryGetValue(puzzleId, out BridgePuzzleState previous) &&
                previous == stable)
            {
                return;
            }

            bridgeStates[puzzleId] = stable;
            Changed?.Invoke();
        }

        /// <summary>Vergisst den Stand eines Rätsels; vor allem für Tests.</summary>
        public static void Forget(string puzzleId)
        {
            if (!string.IsNullOrEmpty(puzzleId) && bridgeStates.Remove(puzzleId))
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Vergisst alle Raetsel. Fuer „Neues Spiel" und fuer Tests.</summary>
        public static void ForgetAll()
        {
            if (bridgeStates.Count == 0)
            {
                return;
            }

            bridgeStates.Clear();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            bridgeStates.Clear();

            // Abonnenten der letzten Sitzung sind nach einem Domain-Reload
            // ungueltig.
            Changed = null;
        }
    }
}
