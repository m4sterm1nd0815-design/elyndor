using Elyndor.Player;
using UnityEngine;

namespace Elyndor.World
{
    /// <summary>
    /// Startposition nach einem Regionswechsel. Stimmt die eigene Spawn-ID
    /// mit dem angeforderten Spawn überein, wird der Spieler beim Laden
    /// hierher versetzt (CharacterController kurz deaktiviert, damit die
    /// Teleportation nicht von der Kollisionsauflösung geschluckt wird).
    /// </summary>
    public class RegionSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId;

        private void Start()
        {
            if (string.IsNullOrEmpty(RegionTravel.PendingSpawnId) ||
                RegionTravel.PendingSpawnId != spawnId)
            {
                return;
            }

            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();

            if (player == null)
            {
                return;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = transform.position;
            controller.enabled = true;

            RegionTravel.ClearPendingSpawn();
        }
    }
}
