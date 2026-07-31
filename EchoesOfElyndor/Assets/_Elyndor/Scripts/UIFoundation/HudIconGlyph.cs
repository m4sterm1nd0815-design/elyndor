using UnityEngine;
using UnityEngine.UI;

namespace Elyndor.UIFoundation
{
    public enum HudGlyphType
    {
        Heart,
        Stamina,
        Memory,
        EmptySlot
    }

    public sealed class HudIconGlyph : MonoBehaviour
    {
        [SerializeField] private HudGlyphType glyphType;
        [SerializeField] private Text label;

        private void OnValidate()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (label == null)
                return;

            label.text = glyphType switch
            {
                HudGlyphType.Heart => "♥",
                HudGlyphType.Stamina => "◆",
                HudGlyphType.Memory => "✦",
                _ => "·"
            };
        }
    }
}
