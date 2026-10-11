using Elyndor.World;
using UnityEngine;

namespace Elyndor.Prototypes
{
    /// <summary>
    /// Activates a prototype encounter after the Finsterwald has answered.
    /// This keeps the northern fight behind the Memory Watch and bridge flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OpenZoneEncounterGate : MonoBehaviour
    {
        [SerializeField] private FinsterwaldRegeneration regeneration;
        [SerializeField] private GameObject encounterRoot;

        public FinsterwaldRegeneration Regeneration => regeneration;
        public GameObject EncounterRoot => encounterRoot;

        public void Configure(
            FinsterwaldRegeneration targetRegeneration,
            GameObject targetEncounterRoot)
        {
            regeneration = targetRegeneration;
            encounterRoot = targetEncounterRoot;
            Refresh();
        }

        private void OnEnable()
        {
            FinsterwaldRegeneration.AnyRegionRegenerated += HandleRegenerated;
        }

        private void Start()
        {
            // All Awake calls have completed here, including restoration of a
            // previously regenerated state.
            Refresh();
        }

        private void OnDisable()
        {
            FinsterwaldRegeneration.AnyRegionRegenerated -= HandleRegenerated;
        }

        private void HandleRegenerated()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (encounterRoot == null) return;
            bool shouldBeActive = regeneration != null && regeneration.IsRegenerated;
            if (encounterRoot.activeSelf != shouldBeActive)
                encounterRoot.SetActive(shouldBeActive);
        }
    }
}
