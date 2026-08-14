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

            bridgeStates[puzzleId] = BridgePuzzleRules.StableFallback(state);
        }

        /// <summary>Vergisst den Stand eines Rätsels; vor allem für Tests.</summary>
        public static void Forget(string puzzleId)
        {
            if (!string.IsNullOrEmpty(puzzleId))
            {
                bridgeStates.Remove(puzzleId);
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            bridgeStates.Clear();
        }
    }
}
