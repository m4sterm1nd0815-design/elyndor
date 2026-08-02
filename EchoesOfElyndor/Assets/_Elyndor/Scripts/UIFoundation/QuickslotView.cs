using UnityEngine;
using UnityEngine.UI;

namespace Elyndor.UIFoundation
{
    public sealed class QuickslotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text glyphLabel;
        [SerializeField] private Text amountLabel;
        [SerializeField] private QuickslotFocusVisual focusVisual;

        public bool IsSelectionVisible =>
            ResolveFocusVisual()?.IsSelectionVisible ?? false;

        private void Awake()
        {
            ResolveFocusVisual();
        }

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

        private QuickslotFocusVisual ResolveFocusVisual()
        {
            if (focusVisual == null)
                focusVisual = GetComponent<QuickslotFocusVisual>();

            return focusVisual;
        }

        public void SetSelected(bool selected)
        {
            ResolveFocusVisual()?.SetRuntimeSelected(selected);
        }
    }
}
