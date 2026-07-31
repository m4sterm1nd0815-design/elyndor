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
            if (focusFrame != null)
                focusFrame.SetActive(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (focusFrame != null)
                focusFrame.SetActive(false);
        }
    }
}
