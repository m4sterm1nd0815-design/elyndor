using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.World
{
    /// <summary>
    /// Zentraler Ablauf für Regionswechsel: merkt sich den Ziel-Spawn über
    /// den Szenenwechsel hinweg. Bewusst reiner Zustand ohne MonoBehaviour;
    /// Memory-Site-Zustände überleben über MemorySessionState ohnehin.
    /// </summary>
    public static class RegionTravel
    {
        /// <summary>Spawn-ID, an der der Spieler nach dem Laden erscheinen soll.</summary>
        public static string PendingSpawnId { get; private set; }

        public static void TravelTo(string sceneName, string spawnId)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("RegionTravel: Leerer Szenenname — Wechsel abgebrochen.");
                return;
            }

            PendingSpawnId = spawnId;
            SceneManager.LoadScene(sceneName);
        }

        public static void ClearPendingSpawn()
        {
            PendingSpawnId = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            PendingSpawnId = null;
        }
    }
}
