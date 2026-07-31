using System;
using UnityEngine;

namespace Elyndor.Core
{
    /// <summary>
    /// Loser Event-Kanal für erzählende Texte (Untersuchungen, Erinnerungen).
    /// Weltobjekte senden Texte, ohne die UI zu kennen; die UI abonniert,
    /// ohne einzelne Weltobjekte zu kennen.
    /// </summary>
    public static class NarrationEvents
    {
        /// <summary>Parameter: Text und Anzeigedauer in Sekunden.</summary>
        public static event Action<string, float> MessageRequested;

        public static void RaiseMessage(string text, float duration)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            MessageRequested?.Invoke(text, duration);
        }

        // Statische Events überleben das Deaktivieren des Domain Reloads
        // (Enter Play Mode Options); deshalb beim Spielstart zurücksetzen.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            MessageRequested = null;
        }
    }
}
