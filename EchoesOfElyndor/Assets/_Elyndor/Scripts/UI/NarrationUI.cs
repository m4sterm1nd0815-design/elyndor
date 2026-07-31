using System.Collections;
using Elyndor.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Elyndor.UI
{
    /// <summary>
    /// Zeigt erzählende Texte (Untersuchungen, Erinnerungen) für eine begrenzte
    /// Dauer an. Abonniert den Narration-Kanal und kennt keine Weltobjekte.
    /// </summary>
    public class NarrationUI : MonoBehaviour
    {
        [SerializeField] private GameObject narrationRoot;
        [SerializeField] private Text narrationText;

        private Coroutine hideCoroutine;

        private void OnEnable()
        {
            NarrationEvents.MessageRequested += HandleMessageRequested;
        }

        private void OnDisable()
        {
            NarrationEvents.MessageRequested -= HandleMessageRequested;
        }

        private void HandleMessageRequested(string text, float duration)
        {
            narrationText.text = text;
            narrationRoot.SetActive(true);

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }

            hideCoroutine = StartCoroutine(HideAfterDelay(duration));
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            narrationRoot.SetActive(false);
            hideCoroutine = null;
        }
    }
}
