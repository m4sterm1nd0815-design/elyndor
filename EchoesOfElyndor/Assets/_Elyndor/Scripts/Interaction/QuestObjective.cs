using Elyndor.Core;
using UnityEngine;

namespace Elyndor.Interaction
{
    /// <summary>
    /// Weltobjekt, das ein Nebenquest-Ziel erfüllt (Korb finden, Axt
    /// bergen ...). Erst interagierbar, wenn die zugehörige Quest aktiv
    /// ist; nach dem Fund verschwindet optional das sichtbare Objekt.
    /// </summary>
    public class QuestObjective : InteractableBase
    {
        [Header("Quest")]
        [SerializeField] private string questId;
        [SerializeField] private string objectiveId;

        [Header("Fund")]
        [SerializeField, TextArea(2, 5)] private string foundText;
        [SerializeField] private float textDuration = 7f;
        [Tooltip("Wird nach dem Fund deaktiviert (z. B. das sichtbare Objekt).")]
        [SerializeField] private GameObject visualRoot;

        public override bool CanInteract(GameObject interactor)
        {
            return QuestState.GetStage(questId) == 1 &&
                   !QuestState.IsObjectiveCompleted(objectiveId);
        }

        public override void Interact(GameObject interactor)
        {
            QuestState.CompleteObjective(objectiveId);
            NarrationEvents.RaiseMessage(foundText, textDuration);

            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }
        }
    }
}
