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

            activatedSiteIds.Add(siteId);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            activatedSiteIds.Clear();
        }
    }
}
