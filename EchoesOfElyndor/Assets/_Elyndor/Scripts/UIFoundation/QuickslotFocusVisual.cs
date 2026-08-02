using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Elyndor.UIFoundation
{
    [RequireComponent(typeof(Selectable))]
    public sealed class QuickslotFocusVisual : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image focusFrame;
        [SerializeField] private bool reduceMotion;
        [SerializeField] private float selectedScale = 1.04f;
        [SerializeField] private float speed = 14f;

        private Vector3 targetScale = Vector3.one;
        private bool hasUiFocus;
        private bool isRuntimeSelected;

        public bool IsSelectionVisible =>
            focusFrame != null && focusFrame.enabled;

        private void Awake()
        {
            RefreshState();
        }

        private void Update()
        {
            if (reduceMotion)
            {
                transform.localScale = targetScale;
                return;
            }

            float t = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
        }

        public void OnSelect(BaseEventData eventData)
        {
            hasUiFocus = true;
            RefreshState();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            hasUiFocus = false;
            RefreshState();
        }

        public void SetReduceMotion(bool enabled)
        {
            reduceMotion = enabled;
            if (enabled)
                transform.localScale = targetScale;
        }

        public void SetRuntimeSelected(bool selected)
        {
            isRuntimeSelected = selected;
            RefreshState();
        }

        private void RefreshState()
        {
            bool focused = hasUiFocus || isRuntimeSelected;
            if (focusFrame != null)
                focusFrame.enabled = focused;

            targetScale = focused
                ? Vector3.one * selectedScale
                : Vector3.one;
        }
    }
}
