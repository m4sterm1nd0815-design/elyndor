using System.Collections;
using UnityEngine;

namespace Elyndor.Memory
{
    /// <summary>
    /// Rein visuelle Darstellung einer Erinnerung (z. B. die Geister-Brücke).
    /// Ist zu Spielbeginn verborgen und wird von einer <see cref="MemorySite"/>
    /// eingeblendet. Enthält bewusst keine Aktivierungs- oder Zustandslogik.
    /// </summary>
    public class MemoryEcho : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Darstellung")]
        [Tooltip("Wurzelobjekt der sichtbaren Erinnerung; wird ein- und ausgeblendet.")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private float fadeDuration = 1.5f;

        private Renderer[] visualRenderers;
        private Color[] authoredBaseColors;
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            if (visualRoot == null)
            {
                Debug.LogWarning($"{name}: MemoryEcho hat kein visualRoot zugewiesen.", this);
                return;
            }

            CacheRenderers();
            visualRoot.SetActive(false);
        }

        /// <summary>Blendet die Erinnerung über die Fade-Dauer ein.</summary>
        public void Reveal()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.SetActive(true);

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeIn());
        }

        /// <summary>Zeigt die Erinnerung sofort, ohne Überblendung.</summary>
        public void RevealInstant()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.SetActive(true);
            ApplyAlphaFactor(1f);
        }

        private void CacheRenderers()
        {
            visualRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            authoredBaseColors = new Color[visualRenderers.Length];

            for (int i = 0; i < visualRenderers.Length; i++)
            {
                Material material = visualRenderers[i].sharedMaterial;

                authoredBaseColors[i] =
                    material != null && material.HasColor(BaseColorId)
                        ? material.GetColor(BaseColorId)
                        : Color.white;
            }
        }

        private IEnumerator FadeIn()
        {
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                ApplyAlphaFactor(elapsedTime / fadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            ApplyAlphaFactor(1f);
            fadeCoroutine = null;
        }

        private void ApplyAlphaFactor(float alphaFactor)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

            for (int i = 0; i < visualRenderers.Length; i++)
            {
                Color baseColor = authoredBaseColors[i];
                baseColor.a *= Mathf.Clamp01(alphaFactor);

                propertyBlock.SetColor(BaseColorId, baseColor);
                visualRenderers[i].SetPropertyBlock(propertyBlock);
            }
        }
    }
}
