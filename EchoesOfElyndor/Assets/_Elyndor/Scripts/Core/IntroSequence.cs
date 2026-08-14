using System.Collections;
using Elyndor.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Elyndor.Core
{
    /// <summary>
    /// Erwachens-Sequenz aus Kapitel 1 der Lore-Bibel: Schwarzblende,
    /// fragmentierte Erinnerungszeilen, ein beginnendes Ticken — dann
    /// öffnet Aren die Augen zwischen den Farnen. Läuft einmal pro
    /// Sitzung, ist mit jeder Taste überspringbar und besteht nur aus
    /// UI-Einblendungen (Audio folgt, sobald Assets existieren).
    ///
    /// Uebersprungen wird ueber die Action-Schicht (Sprung, Interagieren oder
    /// Angriff), nicht mehr ueber <c>Keyboard.anyKey</c>. "Beliebige Taste"
    /// reagierte auch auf Tasten, die im Spiel etwas voellig anderes tun.
    /// </summary>
    public class IntroSequence : MonoBehaviour
    {
        [SerializeField] private CanvasGroup blackScreen;
        [SerializeField] private Text lineText;
        [SerializeField] private float lineDuration = 3.2f;
        [SerializeField] private float fadeOutDuration = 3f;

        [Tooltip("Optional. Bleibt das Feld leer, wird der Reader zur Laufzeit " +
                 "in der geladenen Szene gesucht.")]
        [SerializeField] private PlayerInputReader inputReader;

        [SerializeField, TextArea(1, 3)]
        private string[] lines =
        {
            "Wind.",
            "Ein fernes Kinderlachen.",
            "Eine Frauenstimme — die Worte sind nicht zu verstehen.",
            "„Versprich mir nur eines ...“",
            "Stille.",
            "Dann: ein einzelnes Ticken."
        };

        private static bool hasPlayedThisSession;
        private bool skipRequested;

        /// <summary>Läuft die Sequenz gerade (für nachgelagerte Abläufe wie das Tutorial)?</summary>
        public bool IsPlaying { get; private set; }

        private void Start()
        {
            if (hasPlayedThisSession || blackScreen == null || lineText == null)
            {
                if (blackScreen != null)
                {
                    blackScreen.gameObject.SetActive(false);
                }

                return;
            }

            hasPlayedThisSession = true;
            IsPlaying = true;
            StartCoroutine(RunSequence());
        }

        private void Update()
        {
            if (blackScreen != null && blackScreen.gameObject.activeSelf && SkipRequested())
            {
                skipRequested = true;
            }
        }

        private bool SkipRequested()
        {
            if (inputReader == null)
            {
                inputReader = PlayerInputReader.FindInLoadedScenes();
            }

            return inputReader != null && inputReader.SkipRequestedThisFrame;
        }

        private IEnumerator RunSequence()
        {
            blackScreen.gameObject.SetActive(true);
            blackScreen.alpha = 1f;

            foreach (string line in lines)
            {
                if (skipRequested)
                {
                    break;
                }

                lineText.text = line;

                float elapsedTime = 0f;
                while (elapsedTime < lineDuration && !skipRequested)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }

            lineText.text = string.Empty;

            // Aren öffnet die Augen: langsame Blende in den Finsterwald.
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeOutDuration)
            {
                blackScreen.alpha = 1f - fadeElapsed / fadeOutDuration;
                fadeElapsed += Time.deltaTime;
                yield return null;
            }

            blackScreen.alpha = 0f;
            blackScreen.gameObject.SetActive(false);
            IsPlaying = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            hasPlayedThisSession = false;
        }
    }
}
