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

            regenerated.Add(regionStateId);
            store?.SetRegenerated(regionStateId, true);
        }

        /// <summary>Vergisst einen Abschnitt; vor allem für Tests.</summary>
        public static void Forget(string regionStateId)
        {
            regenerated.Remove(regionStateId);
            store?.SetRegenerated(regionStateId, false);
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            regenerated.Clear();
            store = null;
        }
    }
}
