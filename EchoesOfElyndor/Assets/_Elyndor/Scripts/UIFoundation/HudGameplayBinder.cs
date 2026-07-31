using UnityEngine;

namespace Elyndor.UIFoundation
{
    public sealed class HudGameplayBinder : MonoBehaviour
    {
        [SerializeField] private PlayerVitals playerVitals;
        [SerializeField] private HudVitalsSource hudSource;

        private void OnEnable()
        {
            if (playerVitals != null)
                playerVitals.Changed += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (playerVitals != null)
                playerVitals.Changed -= Refresh;
        }

        public void Configure(PlayerVitals vitals, HudVitalsSource source)
        {
            if (isActiveAndEnabled && playerVitals != null)
                playerVitals.Changed -= Refresh;

            playerVitals = vitals;
            hudSource = source;

            if (isActiveAndEnabled && playerVitals != null)
                playerVitals.Changed += Refresh;

            Refresh();
        }

        public void Refresh()
        {
            if (playerVitals == null || hudSource == null)
                return;

            hudSource.SetNormalized(
                playerVitals.Health01,
                playerVitals.Stamina01,
                playerVitals.Memory01);
        }
    }
}
