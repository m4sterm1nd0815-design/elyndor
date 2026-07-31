using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Elyndor.Memory
{
    /// <summary>
    /// Rein visuelle Betonung der Memory-Watch-Aktivierung: blendet das
    /// Gewicht eines Post-Processing-Volumes (Abdunklung, Entsättigung,
    /// Vignette) weich ein und wieder aus. Hört auf die statischen
    /// MemorySite-Events und enthält keine Spiellogik.
    /// </summary>
    public class MemoryVisionEffect : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField] private Volume visionVolume;

        [Header("Zeitverlauf")]
        [SerializeField] private float fadeInDuration = 1.2f;
        [Tooltip("Wie lange die Sicht nach der Aktivierung gehalten wird, während die Erinnerung einblendet.")]
        [SerializeField] private float holdDuration = 2f;
        [SerializeField] private float fadeOutDuration = 2.5f;

        private Coroutine effectCoroutine;

        private void OnEnable()
        {
            MemorySite.AnyActivationStarted += HandleActivationStarted;
            MemorySite.AnyActivationCompleted += HandleActivationCompleted;

            if (visionVolume != null)
            {
                visionVolume.weight = 0f;
            }
        }

        private void OnDisable()
        {
            MemorySite.AnyActivationStarted -= HandleActivationStarted;
            MemorySite.AnyActivationCompleted -= HandleActivationCompleted;
        }

        private void HandleActivationStarted(MemorySite site)
        {
            StartEffectCoroutine(FadeVolumeWeight(1f, fadeInDuration));
        }

        private void HandleActivationCompleted(MemorySite site)
        {
            StartEffectCoroutine(HoldAndRelease());
        }

        private void StartEffectCoroutine(IEnumerator routine)
        {
            if (visionVolume == null)
            {
                return;
            }

            if (effectCoroutine != null)
            {
                StopCoroutine(effectCoroutine);
            }

            effectCoroutine = StartCoroutine(routine);
        }

        private IEnumerator HoldAndRelease()
        {
            yield return new WaitForSeconds(holdDuration);
            yield return FadeVolumeWeight(0f, fadeOutDuration);
        }

        private IEnumerator FadeVolumeWeight(float targetWeight, float duration)
        {
            float startWeight = visionVolume.weight;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float progress = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
                visionVolume.weight = Mathf.Lerp(startWeight, targetWeight, progress);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            visionVolume.weight = targetWeight;
            effectCoroutine = null;
        }
    }
}
