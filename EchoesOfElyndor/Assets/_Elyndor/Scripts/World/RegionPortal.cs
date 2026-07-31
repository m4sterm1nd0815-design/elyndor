using UnityEngine;

namespace Elyndor.World
{
    /// <summary>
    /// Übergang zwischen Regionen. Erwartet einen Trigger-Collider;
    /// betritt der Spieler (CharacterController) den Trigger, wird die
    /// Zielszene geladen und dort am passenden Spawn gestartet.
    /// </summary>
    public class RegionPortal : MonoBehaviour
    {
        [Header("Ziel")]
        [SerializeField] private string targetSceneName;
        [SerializeField] private string targetSpawnId;

        private bool traveling;

        private void OnTriggerEnter(Collider other)
        {
            if (traveling || other.GetComponent<CharacterController>() == null)
            {
                return;
            }

            traveling = true;
            RegionTravel.TravelTo(targetSceneName, targetSpawnId);
        }
    }
}
