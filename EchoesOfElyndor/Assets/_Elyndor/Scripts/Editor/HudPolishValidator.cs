using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Elyndor.UIFoundation;

namespace Elyndor.EditorTools
{
    public static class HudPolishValidator
    {
        [MenuItem("Elyndor/QA/Validate Polished HUD")]
        public static void Validate()
        {
            int errors = 0;

            HudFoundationMarker[] huds = Object.FindObjectsByType<HudFoundationMarker>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (huds.Length == 0)
            {
                Debug.LogError("No Elyndor HUD foundation exists in the active scene.");
                errors++;
            }

            QuickslotFocusVisual[] focusVisuals =
                Object.FindObjectsByType<QuickslotFocusVisual>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (focusVisuals.Length != 8)
                Debug.LogWarning($"Expected 8 quickslot focus visuals, found {focusVisuals.Length}.");

            if (EventSystem.current == null)
            {
                Debug.LogError("No EventSystem exists in the active scene.");
                errors++;
            }

            Debug.Log(
                $"Polished HUD QA: {huds.Length} HUDs, " +
                $"{focusVisuals.Length} quickslots, {errors} blocking errors.");
        }
    }
}
