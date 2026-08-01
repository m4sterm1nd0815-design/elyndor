using UnityEngine;
using UnityEngine.EventSystems;

namespace Elyndor.UIFoundation
{
    public sealed class QuickslotSlotRelay :
        MonoBehaviour,
        ISelectHandler,
        ISubmitHandler,
        IPointerClickHandler
    {
        [Range(0, QuickslotRuntimeInventory.SlotCount - 1)]
        [SerializeField] private int slotIndex;

        [SerializeField] private QuickslotBarPresenter presenter;

        public void Configure(int index, QuickslotBarPresenter barPresenter)
        {
            slotIndex = Mathf.Clamp(
                index,
                0,
                QuickslotRuntimeInventory.SlotCount - 1);

            presenter = barPresenter;
        }

        public void OnSelect(BaseEventData eventData)
        {
            presenter?.SelectSlot(slotIndex);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            presenter?.UseSlot(slotIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            presenter?.SelectSlot(slotIndex);
            presenter?.UseSlot(slotIndex);
        }
    }
}
