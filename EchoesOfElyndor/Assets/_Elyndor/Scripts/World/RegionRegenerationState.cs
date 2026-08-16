using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elyndor.World
{
    /// <summary>
    /// Der kleine Vertrag, über den P1.13 den Regionszustand später speichern
    /// kann, ohne dass P1.10 dafür neu gebaut werden muss.
    ///
    /// Absichtlich zwei Methoden und ein Schlüsselraum — mehr braucht eine
    /// Region nicht, und mehr würde das noch offene Speichersystem hier
    /// vorwegnehmen. Wer P1.13 baut, implementiert das und hängt es über
    /// <see cref="RegionRegenerationState.Attach"/> ein; der Rest des Codes
    /// merkt nichts davon.
    /// </summary>
    public interface IRegionStateStore
    {
        bool IsRegenerated(string regionStateId);
        void SetRegenerated(string regionStateId, bool value);
    }

    /// <summary>
    /// Hält fest, welche Regionsabschnitte bereits regeneriert sind.
    ///
    /// Für die laufende Sitzung reicht ein Satz im Speicher — dasselbe Muster
    /// wie <c>MemorySessionState</c> und <c>PuzzleSessionState</c>. Über das
    /// Programmende hinaus hält das nichts; genau dafür ist
    /// <see cref="IRegionStateStore"/> da.
    /// </summary>
    public static class RegionRegenerationState
    {
        private static readonly HashSet<string> regenerated =
            new HashSet<string>();

        private static IRegionStateStore store;

        /// <summary>Meldet jede Aenderung an diesem Zustand.</summary>
        public static event Action Changed;

        /// <summary>Alle regenerierten Abschnitte dieser Sitzung. Nur lesbar.</summary>
        public static IReadOnlyCollection<string> RegeneratedRegionIds => regenerated;

        /// <summary>
        /// Hängt einen dauerhaften Speicher ein. Bis P1.13 existiert, bleibt
        /// er null und alles läuft über die Sitzung.
        /// </summary>
        public static void Attach(IRegionStateStore newStore)
        {
            store = newStore;
        }

        public static bool IsRegenerated(string regionStateId)
        {
            if (string.IsNullOrEmpty(regionStateId))
            {
                return false;
            }

            return store != null
                ? store.IsRegenerated(regionStateId)
                : regenerated.Contains(regionStateId);
        }

        public static void MarkRegenerated(string regionStateId)
        {
            if (string.IsNullOrEmpty(regionStateId))
            {
                Debug.LogWarning(
                    "RegionRegenerationState: Leere Regions-ID.");
                return;
            }

            bool isNew = regenerated.Add(regionStateId);
            store?.SetRegenerated(regionStateId, true);

            if (isNew)
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Vergisst einen Abschnitt; vor allem für Tests.</summary>
        public static void Forget(string regionStateId)
        {
            bool removed = regenerated.Remove(regionStateId);
            store?.SetRegenerated(regionStateId, false);

            if (removed)
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Vergisst alle Abschnitte. Für „Neues Spiel" und für Tests.</summary>
        public static void ForgetAll()
        {
            if (regenerated.Count == 0)
            {
                return;
            }

            foreach (string regionStateId in regenerated)
            {
                store?.SetRegenerated(regionStateId, false);
            }

            regenerated.Clear();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            regenerated.Clear();
            store = null;

            // Abonnenten der letzten Sitzung sind nach einem Domain-Reload
            // ungültig.
            Changed = null;
        }
    }
}
