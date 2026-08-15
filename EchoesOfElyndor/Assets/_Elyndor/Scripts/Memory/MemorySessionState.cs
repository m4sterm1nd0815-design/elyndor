using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elyndor.Memory
{
    /// <summary>
    /// Hält für die laufende Spielsitzung fest, welche Memory Sites bereits
    /// aktiviert wurden — auch über Szenenwechsel hinweg.
    /// Bewusst reiner Zustand ohne MonoBehaviour; wird mit dem geplanten
    /// Save-System (Roadmap M10) über ISaveable persistiert.
    /// </summary>
    public static class MemorySessionState
    {
        private static readonly HashSet<string> activatedSiteIds = new HashSet<string>();

        /// <summary>
        /// Meldet jede Aenderung an diesem Zustand.
        ///
        /// Das Speichersystem haengt sich hier ein, statt einzelne Aufrufer zu
        /// kennen: wer auch immer einen Ort als gesehen eintraegt, loest damit
        /// dieselbe Meldung aus. Das Ereignis sagt bewusst nicht, <em>was</em>
        /// sich geaendert hat — der Zustand ist klein genug, um ihn ganz zu
        /// lesen, und ein Delta waere eine Fehlerquelle ohne Gegenwert.
        /// </summary>
        public static event Action Changed;

        /// <summary>
        /// Alle bisher aktivierten Orte. Nur lesbar; zum Eintragen gibt es
        /// <see cref="MarkActivated"/>.
        /// </summary>
        public static IReadOnlyCollection<string> ActivatedSiteIds => activatedSiteIds;

        public static bool IsActivated(string siteId)
        {
            return !string.IsNullOrEmpty(siteId) && activatedSiteIds.Contains(siteId);
        }

        public static void MarkActivated(string siteId)
        {
            if (string.IsNullOrEmpty(siteId))
            {
                Debug.LogWarning("MemorySessionState: Leere Site-ID kann nicht gespeichert werden.");
                return;
            }

            if (activatedSiteIds.Add(siteId))
            {
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Vergisst einen Ort wieder.
        ///
        /// Gegenstueck zu <c>PuzzleSessionState.Forget</c> und
        /// <c>RegionRegenerationState.Forget</c>, die es beide schon
        /// gab. Im Spiel wird es nicht aufgerufen: der Integrationstest
        /// braucht es, um den Zustand „Erinnerung noch nicht gesehen"
        /// herzustellen, ohne den die Bedingung der Regeneration nicht
        /// pruefbar waere.
        /// </summary>
        public static void Forget(string siteId)
        {
            if (string.IsNullOrEmpty(siteId))
            {
                return;
            }

            if (activatedSiteIds.Remove(siteId))
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Vergisst alle Orte. Fuer „Neues Spiel" und fuer Tests.</summary>
        public static void ForgetAll()
        {
            if (activatedSiteIds.Count == 0)
            {
                return;
            }

            activatedSiteIds.Clear();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            activatedSiteIds.Clear();

            // Die Abonnenten der letzten Sitzung sind nach einem Domain-Reload
            // ungueltig. Wird das nicht geleert, haelt das Ereignis tote
            // Empfaenger am Leben.
            Changed = null;
        }
    }
}
