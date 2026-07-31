using UnityEngine;
using UnityEngine.UI;

namespace Elyndor.UIFoundation
{
    public sealed class HudVitalsPresenter : MonoBehaviour
    {
        [SerializeField] private HudVitalsSource source;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image staminaFill;
        [SerializeField] private Image memoryFill;
        [SerializeField] private bool reduceMotion;
        [Min(0.01f)] [SerializeField] private float smoothing = 12f;

        private Vector3 target = Vector3.one;

        private void OnEnable()
        {
            if (source != null)
                source.Changed += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (source != null)
                source.Changed -= Refresh;
        }

        private void Update()
        {
            if (reduceMotion)
            {
                Apply(target);
                return;
            }

            Vector3 current = new(
                healthFill != null ? healthFill.fillAmount : 0f,
                staminaFill != null ? staminaFill.fillAmount : 0f,
                memoryFill != null ? memoryFill.fillAmount : 0f);

            float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            Apply(Vector3.Lerp(current, target, t));
        }

        public void SetReduceMotion(bool enabled)
        {
            reduceMotion = enabled;
            if (enabled)
                Apply(target);
        }

        private void Refresh()
        {
            if (source == null)
                return;

            target = new Vector3(
                source.Health01,
                source.Stamina01,
                source.Memory01);
        }

        private void Apply(Vector3 values)
        {
            if (healthFill != null)
                healthFill.fillAmount = Mathf.Clamp01(values.x);

            if (staminaFill != null)
                staminaFill.fillAmount = Mathf.Clamp01(values.y);

            if (memoryFill != null)
                memoryFill.fillAmount = Mathf.Clamp01(values.z);
        }
    }
}
