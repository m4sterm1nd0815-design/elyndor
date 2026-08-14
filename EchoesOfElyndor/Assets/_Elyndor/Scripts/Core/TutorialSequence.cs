using System.Collections;
using Elyndor.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Elyndor.Core
{
    /// <summary>
    /// Bewegungs-Tutorial nach dem Erwachen: Gehen, Sprinten, Rolle,
    /// Springen. Jeder Schritt blendet einen knappen Hinweis ein und
    /// schaltet erst weiter, wenn der Spieler ihn ausgeführt hat
    /// (Lore-Regel: Tutorials knapp und nur, wenn die Handlung sie braucht).
    /// Läuft einmal pro Sitzung, nur im Finsterwald.
    ///
    /// Die Fortschrittserkennung liest die Action-Schicht, nicht mehr einzelne
    /// Tasten. Vorher pruefte der Rollen-Schritt <c>leftCtrl</c>, waehrend das
    /// Actions-Asset die Rolle auf <c>C</c> legt — wer die Rolle wie vorgesehen
    /// ausloeste, kam im Tutorial nicht weiter.
    /// </summary>
    public class TutorialSequence : MonoBehaviour
    {
        [SerializeField] private IntroSequence intro;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private float requiredMoveSeconds = 1.5f;
        [SerializeField] private float stepCompletedPause = 1.2f;

        [Tooltip("Optional. Bleibt das Feld leer, wird der Reader zur Laufzeit " +
                 "in der geladenen Szene gesucht.")]
        [SerializeField] private PlayerInputReader inputReader;

        private static bool hasPlayedThisSession;

        private void Start()
        {
            if (hasPlayedThisSession || promptRoot == null || promptText == null)
            {
                if (promptRoot != null)
                {
                    promptRoot.SetActive(false);
                }

                return;
            }

            hasPlayedThisSession = true;
            StartCoroutine(RunTutorial());
        }

        private IEnumerator RunTutorial()
        {
            promptRoot.SetActive(false);

            // Erst erwachen, dann lernen.
            while (intro != null && intro.IsPlaying)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            yield return RunStep(
                "Bewege dich mit W, A, S, D (oder dem linken Stick).",
                TimedCondition(IsMoving, requiredMoveSeconds)
            );

            yield return RunStep(
                "Halte Shift gedrückt, um zu sprinten.",
                TimedCondition(IsSprinting, requiredMoveSeconds)
            );

            yield return RunStep(
                "Drücke Strg für eine Ausweichrolle.",
                PressedCondition(IsRollPressed)
            );

            yield return RunStep(
                "Drücke die Leertaste, um zu springen.",
                PressedCondition(IsJumpPressed)
            );

            promptText.text = "Gut. Der Körper erinnert sich — auch wenn der Kopf es nicht tut.";
            yield return new WaitForSeconds(4f);
            promptRoot.SetActive(false);
        }

        private IEnumerator RunStep(string prompt, IEnumerator condition)
        {
            promptText.text = prompt;
            promptRoot.SetActive(true);

            yield return condition;

            promptText.text = "✔";
            yield return new WaitForSeconds(stepCompletedPause);
        }

        private IEnumerator TimedCondition(System.Func<bool> check, float requiredSeconds)
        {
            float accumulated = 0f;

            while (accumulated < requiredSeconds)
            {
                if (check())
                {
                    accumulated += Time.deltaTime;
                }

                yield return null;
            }
        }

        private IEnumerator PressedCondition(System.Func<bool> check)
        {
            while (!check())
            {
                yield return null;
            }
        }

        private PlayerInputReader Reader
        {
            get
            {
                if (inputReader == null)
                {
                    inputReader = PlayerInputReader.FindInLoadedScenes();
                }

                return inputReader;
            }
        }

        private bool IsMoving() =>
            Reader != null && Reader.Move.sqrMagnitude > 0.04f;

        private bool IsSprinting() =>
            IsMoving() && Reader != null && Reader.SprintHeld;

        private bool IsRollPressed() =>
            Reader != null && Reader.RollPressedThisFrame;

        private bool IsJumpPressed() =>
            Reader != null && Reader.JumpPressedThisFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            hasPlayedThisSession = false;
        }
    }
}
