using Elyndor.Core;
using UnityEngine;

namespace Elyndor.Interaction
{
    /// <summary>
    /// Einfacher Nebenquest-NPC: Ansprechen startet die Quest, ein
    /// <see cref="QuestObjective"/> in der Welt erfüllt sie, erneutes
    /// Ansprechen schließt sie ab. Texte laufen über den Narration-Kanal.
    /// </summary>
    public class QuestGiver : InteractableBase
    {
        [Header("Quest")]
        [SerializeField] private string questId;
        [SerializeField] private string objectiveId;

        [Header("Texte")]
        [SerializeField, TextArea(2, 5)] private string introText;
        [SerializeField, TextArea(2, 5)] private string reminderText;
        [SerializeField, TextArea(2, 5)] private string completionText;
        [SerializeField, TextArea(2, 5)] private string idleText;
        [SerializeField] private float textDuration = 7f;

        private const int StageNotStarted = 0;
        private const int StageActive = 1;
        private const int StageCompleted = 2;

        public override void Interact(GameObject interactor)
        {
            int stage = QuestState.GetStage(questId);

            if (stage == StageNotStarted)
            {
                QuestState.SetStage(questId, StageActive);
                NarrationEvents.RaiseMessage(introText, textDuration);
                return;
            }

            if (stage == StageActive)
            {
                if (QuestState.IsObjectiveCompleted(objectiveId))
                {
                    QuestState.SetStage(questId, StageCompleted);
                    NarrationEvents.RaiseMessage(completionText, textDuration);
                }
                else
                {
                    NarrationEvents.RaiseMessage(reminderText, textDuration);
                }

                return;
            }

            NarrationEvents.RaiseMessage(idleText, textDuration);
        }
    }
}
