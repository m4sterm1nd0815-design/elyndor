using System.Collections;
using Elyndor.Memory;
using UnityEngine;

namespace Elyndor.UI
{
    /// <summary>
    /// Dezente Bildschirm-Anzeige, solange eine Memory-Watch-Aktivierung läuft.
    /// Blendet weich über eine CanvasGroup ein und aus. Abonniert die
    /// statischen MemorySite-Events und kennt keine konkreten Orte.
    /// </summary>
    public class MemoryWatchActivationUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private float fadeInDuration = 0.8f;
        [SerializeField] private float fadeOutDuration = 1.4f;

        private Coroutine fadeCoroutine;

        private void OnEnable()
        {
            MemorySite.AnyActivationStarted += HandleActivationStarted;
            MemorySite.AnyActivationCompleted += HandleActivationCompleted;

            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            MemorySite.AnyActivationStarted -= HandleActivationStarted;
            MemorySite.AnyActivationCompleted -= HandleActivationCompleted;
        }

        private void HandleActivationStarted(MemorySite site)
        {
            overlayGroup.gameObject.SetActive(true);
            StartFade(1f, fadeInDuration, deactivateWhenDone: false);
        }

        private void HandleActivationCompleted(MemorySite site)
        {
            StartFade(0f, fadeOutDuration, deactivateWhenDone: true);
        }

        private void StartFade(float targetAlpha, float duration, bool deactivateWhenDone)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeTo(targetAlpha, duration, deactivateWhenDone));
        }

        private IEnumerator FadeTo(float targetAlpha, float duration, bool deactivateWhenDone)
        {
            float startAlpha = overlayGroup.alpha;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                overlayGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            overlayGroup.alpha = targetAlpha;

            if (deactivateWhenDone)
            {
                overlayGroup.gameObject.SetActive(false);
            }

            fadeCoroutine = null;
        }
    }
}
