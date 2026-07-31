using System.Collections.Generic;
using UnityEngine;

namespace Elyndor.Interaction
{
    /// <summary>
    /// Minimaler Sitzungszustand für Nebenquests: Stufe pro Quest-ID und
    /// erledigte Ziele. Gleiches Muster wie MemorySessionState; wird mit
    /// dem Save-System (Roadmap M10) persistiert und kann später vom
    /// vollwertigen Questsystem abgelöst werden.
    /// </summary>
    public static class QuestState
    {
        private static readonly Dictionary<string, int> stages = new Dictionary<string, int>();
        private static readonly HashSet<string> completedObjectives = new HashSet<string>();

        public static int GetStage(string questId)
        {
            return stages.TryGetValue(questId, out int stage) ? stage : 0;
        }

        public static void SetStage(string questId, int stage)
        {
            if (string.IsNullOrEmpty(questId))
            {
                Debug.LogWarning("QuestState: Leere Quest-ID.");
                return;
            }

            stages[questId] = stage;
        }

        public static bool IsObjectiveCompleted(string objectiveId)
        {
            return completedObjectives.Contains(objectiveId);
        }

        public static void CompleteObjective(string objectiveId)
        {
            if (!string.IsNullOrEmpty(objectiveId))
            {
                completedObjectives.Add(objectiveId);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            stages.Clear();
            completedObjectives.Clear();
        }
    }
}
