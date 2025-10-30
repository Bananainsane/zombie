using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Game
{
    /// <summary>
    /// Freeze powerup that slows down all zombies when collected
    /// </summary>
    public class FreezePowerup : NetworkBehaviour
    {
        [SerializeField] private float freezeDuration = 5f;
        [SerializeField] private float rotationSpeed = 50f;

        private void Update()
        {
            // Rotate powerup for visibility
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            // Only players can collect powerups
            if (other.CompareTag("Player"))
            {
                CollectPowerup();
            }
        }

        private void CollectPowerup()
        {
            // Freeze all zombies
            var zombies = FindObjectsOfType<AI.ZombieAI>();
            foreach (var zombie in zombies)
            {
                zombie.FreezeZombie(freezeDuration);
            }

            Debug.Log($"Freeze powerup collected! Freezing {zombies.Length} zombies for {freezeDuration}s");

            // Despawn powerup
            GetComponent<NetworkObject>().Despawn();
            Destroy(gameObject);
        }
    }
}
