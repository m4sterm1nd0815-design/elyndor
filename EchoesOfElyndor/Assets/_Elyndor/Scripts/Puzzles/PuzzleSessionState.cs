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

        private static readonly Dictionary<string, int[]> anchorSettings =
            new Dictionary<string, int[]>();

        /// <summary>
        /// Meldet jede Aenderung. Das Speichersystem haengt sich hier ein,
        /// statt einzelne Aufrufer zu kennen.
        /// </summary>
        public static event Action Changed;

        /// <summary>Alle gemerkten Raetselstaende. Nur lesbar.</summary>
        public static IReadOnlyDictionary<string, BridgePuzzleState> BridgeStates =>
            bridgeStates;

        /// <summary>
        /// Die Ankerstellungen je Raetsel-ID. Nur lesbar.
        ///
        /// Der Zustandsname allein genuegt nicht: <c>ReadyToRelease</c>
        /// <em>bedeutet</em> „die Anker stehen richtig". Wird nur der Name
        /// wiederhergestellt und stehen die Steine danach wieder in ihrer
        /// Ausgangsstellung, behauptet das Raetsel eine Spannung, die es nicht
        /// gibt — und der Stamm verkantet ausgerechnet bei dem Spieler, dem
        /// gerade gesagt wurde, er koenne tragen.
        /// </summary>
        public static IReadOnlyDictionary<string, int[]> AnchorSettings =>
            anchorSettings;

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

        /// <summary>
        /// Die gemerkten Ankerstellungen, oder <c>null</c>, wenn zu diesem
        /// Raetsel noch keine gemerkt wurden. <c>null</c> heisst ausdruecklich
        /// „nichts bekannt" und nicht „alle auf null": ein Spielstand aus einer
        /// Fassung ohne dieses Feld darf die Steine nicht heimlich verstellen.
        /// </summary>
        public static int[] GetAnchorSettings(string puzzleId)
        {
            if (string.IsNullOrEmpty(puzzleId) ||
                !anchorSettings.TryGetValue(puzzleId, out int[] stored))
            {
                return null;
            }

            return (int[])stored.Clone();
        }

        /// <summary>Schreibt die Ankerstellungen eines Raetsels.</summary>
        public static void SetAnchorSettings(
            string puzzleId, IReadOnlyList<int> settings)
        {
            if (string.IsNullOrEmpty(puzzleId))
            {
                Debug.LogWarning(
                    "PuzzleSessionState: Leere Raetsel-ID kann keine " +
                    "Ankerstellungen speichern.");
                return;
            }

            if (settings == null)
            {
                return;
            }

            if (anchorSettings.TryGetValue(puzzleId, out int[] previous) &&
                SameSettings(previous, settings))
            {
                return;
            }

            int[] copy = new int[settings.Count];

            for (int i = 0; i < settings.Count; i++)
            {
                copy[i] = settings[i];
            }

            anchorSettings[puzzleId] = copy;
            Changed?.Invoke();
        }

        private static bool SameSettings(
            int[] previous, IReadOnlyList<int> settings)
        {
            if (previous.Length != settings.Count)
            {
                return false;
            }

            for (int i = 0; i < previous.Length; i++)
            {
                if (previous[i] != settings[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Vergisst den Stand eines Rätsels; vor allem für Tests.</summary>
        public static void Forget(string puzzleId)
        {
            if (string.IsNullOrEmpty(puzzleId))
            {
                return;
            }

            bool removed = bridgeStates.Remove(puzzleId);
            removed |= anchorSettings.Remove(puzzleId);

            if (removed)
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Vergisst alle Raetsel. Fuer „Neues Spiel" und fuer Tests.</summary>
        public static void ForgetAll()
        {
            if (bridgeStates.Count == 0 && anchorSettings.Count == 0)
            {
                return;
            }

            bridgeStates.Clear();
            anchorSettings.Clear();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            bridgeStates.Clear();
            anchorSettings.Clear();

            // Abonnenten der letzten Sitzung sind nach einem Domain-Reload
            // ungueltig.
            Changed = null;
        }
    }
}
