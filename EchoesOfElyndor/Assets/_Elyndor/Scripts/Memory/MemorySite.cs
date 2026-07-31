using System;
using System.Collections;
using Elyndor.Core;
using Elyndor.Interaction;
using UnityEngine;

namespace Elyndor.Memory
{
    /// <summary>
    /// Ein Erinnerungsort in der Welt. Aren aktiviert hier die Memory Watch,
    /// wodurch zugewiesene <see cref="MemoryEcho"/>-Objekte sichtbar werden.
    /// Zuständig nur für Ablauf und Zustand — Darstellung liegt in MemoryEcho,
    /// Text-Ausgabe im Narration-Kanal, Sitzungszustand in MemorySessionState.
    /// </summary>
    public class MemorySite : InteractableBase
    {
        private enum SiteState
        {
            Idle,
            Activating,
            Activated
        }

        [Header("Memory Site")]
        [Tooltip("Eindeutige, stabile ID für Sitzungszustand und späteres Speichern.")]
        [SerializeField] private string siteId;
        [SerializeField] private MemoryEcho[] echoes;

        [Header("Aktivierung")]
        [Tooltip("Dauer der Aktivierungssequenz, bevor die Erinnerung erscheint.")]
        [SerializeField] private float activationDuration = 2f;

        [Header("Erinnerungstext")]
        [SerializeField, TextArea(2, 6)] private string memoryText;
        [SerializeField] private float memoryTextDuration = 6f;

        private SiteState state = SiteState.Idle;

        /// <summary>Wird ausgelöst, wenn irgendeine Memory Site ihre Aktivierung beginnt.</summary>
        public static event Action<MemorySite> AnyActivationStarted;

        /// <summary>Wird ausgelöst, wenn irgendeine Memory Site ihre Aktivierung abschließt.</summary>
        public static event Action<MemorySite> AnyActivationCompleted;

        public string SiteId => siteId;

        public bool IsActivated => state == SiteState.Activated;

        public override bool CanInteract(GameObject interactor) => state == SiteState.Idle;

        protected override void Awake()
        {
            base.Awake();

            if (string.IsNullOrEmpty(siteId))
            {
                Debug.LogError($"{name}: MemorySite benötigt eine eindeutige Site-ID.", this);
            }
        }

        private void Start()
        {
            // Bereits in dieser Sitzung aktivierte Orte sofort wiederherstellen,
            // damit der Zustand Szenenwechsel übersteht.
            if (MemorySessionState.IsActivated(siteId))
            {
                RestoreActivatedState();
            }
        }

        public override void Interact(GameObject interactor)
        {
            if (state != SiteState.Idle)
            {
                return;
            }

            StartCoroutine(RunActivationSequence());
        }

        private IEnumerator RunActivationSequence()
        {
            state = SiteState.Activating;
            AnyActivationStarted?.Invoke(this);

            // Platzhalter für die spätere audiovisuelle Aktivierung
            // (Post-Processing, Sound); aktuell nur eine Wartezeit.
            yield return new WaitForSeconds(activationDuration);

            RevealEchoes(instant: false);

            MemorySessionState.MarkActivated(siteId);
            state = SiteState.Activated;

            AnyActivationCompleted?.Invoke(this);
            NarrationEvents.RaiseMessage(memoryText, memoryTextDuration);
        }

        private void RestoreActivatedState()
        {
            state = SiteState.Activated;
            RevealEchoes(instant: true);
        }

        private void RevealEchoes(bool instant)
        {
            foreach (MemoryEcho echo in echoes)
            {
                if (echo == null)
                {
                    continue;
                }

                if (instant)
                {
                    echo.RevealInstant();
                }
                else
                {
                    echo.Reveal();
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            AnyActivationStarted = null;
            AnyActivationCompleted = null;
        }
    }
}
