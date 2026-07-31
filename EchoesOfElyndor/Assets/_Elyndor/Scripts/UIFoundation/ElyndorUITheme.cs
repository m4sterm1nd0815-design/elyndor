using UnityEngine;

namespace Elyndor.UIFoundation
{
    [CreateAssetMenu(fileName = "ElyndorUITheme", menuName = "Elyndor/UI Theme")]
    public sealed class ElyndorUITheme : ScriptableObject
    {
        [Header("Surfaces")]
        public Color panel = new(0.035f, 0.04f, 0.055f, 0.88f);
        public Color panelSoft = new(0.05f, 0.06f, 0.075f, 0.72f);

        [Header("Accents")]
        public Color gold = new(0.78f, 0.62f, 0.24f, 1f);
        public Color goldSoft = new(0.55f, 0.42f, 0.16f, 0.9f);
        public Color memory = new(0.18f, 0.82f, 0.95f, 1f);

        [Header("Vitals")]
        public Color health = new(0.56f, 0.08f, 0.09f, 1f);
        public Color stamina = new(0.18f, 0.52f, 0.22f, 1f);

        [Header("Text")]
        public Color text = new(0.93f, 0.91f, 0.82f, 1f);
        public Color textMuted = new(0.68f, 0.68f, 0.68f, 1f);
    }
}
