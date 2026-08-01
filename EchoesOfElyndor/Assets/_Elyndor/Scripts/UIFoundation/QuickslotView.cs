using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Elyndor.UIFoundation
{
    public sealed class QuickslotView : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text glyphLabel;
        [SerializeField] private Text amountLabel;
        [SerializeField] private GameObject focusFrame;

        private bool isRuntimeSelected;
        private bool hasUiFocus;

        public void SetVisual(Sprite sprite, string glyph, int amount)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }

            if (glyphLabel != null)
                glyphLabel.text = glyph ?? string.Empty;

            if (amountLabel != null)
                amountLabel.text = amount > 1 ? amount.ToString() : string.Empty;
        }

        public void OnSelect(BaseEventData eventData)
        {
            hasUiFocus = true;
            RefreshFocusFrame();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            hasUiFocus = false;
            RefreshFocusFrame();
        }

        public void SetSelected(bool selected)
        {
            isRuntimeSelected = selected;
            RefreshFocusFrame();
        }

        private void RefreshFocusFrame()
        {
            if (focusFrame != null)
                focusFrame.SetActive(isRuntimeSelected || hasUiFocus);
        }
    }
}
