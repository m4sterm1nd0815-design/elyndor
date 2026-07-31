using System.Collections;
using Elyndor.Combat;
using Elyndor.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Elyndor.Core
{
    /// <summary>
    /// Kampf-Tutorial: startet, sobald eine Waffe in der Haupthand liegt.
    /// Leichter Angriff, schwerer Angriff und Blocken werden an der
    /// Übungspuppe geübt; jeder Schritt schaltet erst nach Ausführung
    /// weiter. Einmal pro Sitzung.
    /// </summary>
    public class CombatTutorial : MonoBehaviour
    {
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private float requiredBlockSeconds = 1.5f;
        [SerializeField] private float stepCompletedPause = 1.2f;

        private static bool hasPlayedThisSession;
        private bool lightHitLanded;
        private bool heavyHitLanded;

        private void OnEnable()
        {
            PlayerInventory.Changed += HandleInventoryChanged;
            TrainingDummy.HitTaken += HandleDummyHit;
        }

        private void OnDisable()
        {
            PlayerInventory.Changed -= HandleInventoryChanged;
            TrainingDummy.HitTaken -= HandleDummyHit;
        }

        private void HandleInventoryChanged()
        {
            if (hasPlayedThisSession ||
                PlayerInventory.GetEquipped(EquipmentSlot.MainHand) == null ||
                promptRoot == null || promptText == null)
            {
                return;
            }

            hasPlayedThisSession = true;
            StartCoroutine(RunTutorial());
        }

        private void HandleDummyHit(AttackType attackType)
        {
            if (attackType == AttackType.Light)
            {
                lightHitLanded = true;
            }
            else
            {
                heavyHitLanded = true;
            }
        }

        private IEnumerator RunTutorial()
        {
            yield return new WaitForSeconds(2.5f);

            yield return RunStep(
                "Ein Schwert. Übe an der Strohpuppe beim Lager: " +
                "Linksklick für einen leichten Angriff.",
                WaitUntilFlag(() => lightHitLanded)
            );

            yield return RunStep(
                "Halte die Angriffstaste gedrückt und lass los — " +
                "ein schwerer Angriff.",
                WaitUntilFlag(() => heavyHitLanded)
            );

            yield return RunStep(
                "Halte Q, um zu blocken.",
                BlockHeldCondition()
            );

            promptText.text = "Die Hände wissen es noch. Wer hat es ihnen beigebracht?";
            yield return new WaitForSeconds(4.5f);
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

        private IEnumerator WaitUntilFlag(System.Func<bool> flag)
        {
            while (!flag())
            {
                yield return null;
            }
        }

        private IEnumerator BlockHeldCondition()
        {
            float accumulated = 0f;

            while (accumulated < requiredBlockSeconds)
            {
                if (playerCombat != null && playerCombat.IsBlocking)
                {
                    accumulated += Time.deltaTime;
                }

                yield return null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            hasPlayedThisSession = false;
        }
    }
}
