using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Elyndor.Core
{
    /// <summary>
    /// Bewegungs-Tutorial nach dem Erwachen: Gehen, Sprinten, Rolle,
    /// Springen. Jeder Schritt blendet einen knappen Hinweis ein und
    /// schaltet erst weiter, wenn der Spieler ihn ausgeführt hat
    /// (Lore-Regel: Tutorials knapp und nur, wenn die Handlung sie braucht).
    /// Läuft einmal pro Sitzung, nur im Finsterwald.
    /// </summary>
    public class TutorialSequence : MonoBehaviour
    {
        [SerializeField] private IntroSequence intro;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private float requiredMoveSeconds = 1.5f;
        [SerializeField] private float stepCompletedPause = 1.2f;

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

        private static bool IsMoving()
        {
            bool keyboard = Keyboard.current != null && (
                Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed ||
                Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed);

            bool gamepad = Gamepad.current != null &&
                Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.04f;

            return keyboard || gamepad;
        }

        private static bool IsSprinting()
        {
            bool keyboard = Keyboard.current != null &&
                Keyboard.current.leftShiftKey.isPressed;

            bool gamepad = Gamepad.current != null &&
                Gamepad.current.leftStickButton.isPressed;

            return IsMoving() && (keyboard || gamepad);
        }

        private static bool IsRollPressed()
        {
            bool keyboard = Keyboard.current != null &&
                Keyboard.current.leftCtrlKey.wasPressedThisFrame;

            bool gamepad = Gamepad.current != null &&
                Gamepad.current.buttonEast.wasPressedThisFrame;

            return keyboard || gamepad;
        }

        private static bool IsJumpPressed()
        {
            bool keyboard = Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame;

            bool gamepad = Gamepad.current != null &&
                Gamepad.current.buttonSouth.wasPressedThisFrame;

            return keyboard || gamepad;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            hasPlayedThisSession = false;
        }
    }
}
