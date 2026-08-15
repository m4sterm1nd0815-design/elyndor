using Elyndor.Core;
using Elyndor.Interaction;
using Elyndor.Memory;
using UnityEngine;

namespace Elyndor.Narration
{
    /// <summary>
    /// Spricht eine Katalogzeile, sobald eine bestimmte Memory Site
    /// abgeschlossen ist — genau einmal.
    ///
    /// So kommt die unbekannte Stimme an der Brücke ins Spiel: nicht als
    /// Dialog, nicht als Figur, sondern als etwas, das Aren hört, nachdem er
    /// die Erinnerung gesehen hat. Wer spricht, bleibt offen; der Sprecher
    /// steht als „Unbekannte Stimme" im Text selbst.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MemoryEchoNarration : MonoBehaviour
    {
        [SerializeField] private NarrationCatalog catalog;

        [Tooltip("Auf welche Memory Site gewartet wird.")]
        [SerializeField] private string memorySiteId = "finsterwald_bruecke_01";

        [Tooltip("Zeilen in dieser Reihenfolge, jeweils nacheinander.")]
        [SerializeField] private string[] narrationKeys = new string[0];

        [Tooltip("Pause zwischen zwei Zeilen.")]
        [Min(0f)] [SerializeField] private float gapBetweenLines = 1.5f;

        private int nextLine = -1;
        private float timer;
        private bool subscribed;

        /// <summary>Wurden alle Zeilen gesprochen?</summary>
        public bool Finished =>
            nextLine >= 0 && nextLine >= narrationKeys.Length;

        /// <summary>Wie viele Zeilen bereits gesprochen wurden.</summary>
        public int SpokenLines => Mathf.Max(0, nextLine);

        /// <summary>Setzt Katalog und Zeilenfolge; fuer Aufbauwerkzeuge.</summary>
        public void Configure(
            NarrationCatalog newCatalog, string siteId, string[] keys)
        {
            catalog = newCatalog;
            memorySiteId = siteId;
            narrationKeys = keys ?? new string[0];
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (subscribed)
            {
                MemorySite.AnyActivationCompleted -= HandleCompleted;
                subscribed = false;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Ein Schritt; oeffentlich fuer Tests.</summary>
        public void Tick(float deltaTime)
        {
            if (nextLine < 0 || Finished)
            {
                return;
            }

            timer -= Mathf.Max(0f, deltaTime);

            if (timer > 0f)
            {
                return;
            }

            SpeakNext();
        }

        /// <summary>Startet die Zeilenfolge; wirkt nur beim ersten Aufruf.</summary>
        public void Begin()
        {
            if (nextLine >= 0)
            {
                return;
            }

            nextLine = 0;
            timer = 0f;
        }

        private void SpeakNext()
        {
            if (catalog == null || nextLine >= narrationKeys.Length)
            {
                nextLine = narrationKeys.Length;
                return;
            }

            NarrationEntry entry = catalog.Find(narrationKeys[nextLine]);
            nextLine++;

            if (entry == null)
            {
                Debug.LogWarning(
                    $"{name}: Kein Katalogeintrag fuer " +
                    $"'{narrationKeys[nextLine - 1]}'.", this);
                return;
            }

            NarrationEvents.RaiseMessage(entry.Text, entry.Duration);
            timer = entry.Duration + gapBetweenLines;
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            MemorySite.AnyActivationCompleted += HandleCompleted;
            subscribed = true;
        }

        private void HandleCompleted(MemorySite site)
        {
            if (site != null && site.SiteId == memorySiteId)
            {
                Begin();
            }
        }
    }
}
