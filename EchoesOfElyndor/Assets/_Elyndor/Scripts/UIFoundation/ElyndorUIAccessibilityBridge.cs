using UnityEngine;

namespace Elyndor.UIFoundation
{
    public sealed class ElyndorUIAccessibilityBridge : MonoBehaviour
    {
        [SerializeField] private RectTransform uiRoot;
        [SerializeField] private QuickslotFocusVisual[] focusVisuals;
        [Range(0.75f, 1.75f)]
        [SerializeField] private float uiScale = 1f;
        [SerializeField] private bool reduceMotion;

        public void Apply()
        {
            if (uiRoot != null)
                uiRoot.localScale = Vector3.one * Mathf.Clamp(uiScale, 0.75f, 1.75f);

            if (focusVisuals == null)
                return;

            foreach (QuickslotFocusVisual visual in focusVisuals)
            {
                if (visual != null)
                    visual.SetReduceMotion(reduceMotion);
            }
        }
    }
}
