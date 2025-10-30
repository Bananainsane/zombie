using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Game
{
    /// <summary>
    /// Spawns freeze powerups at random locations in the arena
    /// </summary>
    public class PowerupSpawner : MonoBehaviour
    {
        [Header("Powerup Settings")]
        [SerializeField] private GameObject powerupPrefab;
        [SerializeField] private float spawnInterval = 20f;
        [SerializeField] private float spawnRadius = 35f;
        [SerializeField] private int maxPowerups = 2;

        [Header("Auto-spawn")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private float initialDelay = 10f;

        private NetworkManager networkManager;

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
            if (networkManager != null)
            {
                networkManager.OnServerStarted += OnServerStarted;
            }
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnServerStarted -= OnServerStarted;
            }
        }

        private void OnServerStarted()
        {
            if (spawnOnStart)
            {
                InvokeRepeating(nameof(TrySpawnPowerup), initialDelay, spawnInterval);
            }
        }

        private void TrySpawnPowerup()
        {
            if (networkManager == null || !networkManager.IsServer) return;

            // Count existing powerups in scene
            int currentCount = FindObjectsOfType<FreezePowerup>().Length;

            if (currentCount >= maxPowerups)
            {
                Debug.Log("Max powerups reached, skipping spawn");
                return;
            }

            if (powerupPrefab == null)
            {
                Debug.LogError("Powerup prefab not assigned!");
                return;
            }

            SpawnPowerup();
        }

        private void SpawnPowerup()
        {
            // Random position in arena
            Vector3 randomPos = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                1f,
                Random.Range(-spawnRadius, spawnRadius)
            );

            GameObject powerup = Instantiate(powerupPrefab, randomPos, Quaternion.identity);
            NetworkObject netObj = powerup.GetComponent<NetworkObject>();
            netObj.Spawn();

            Debug.Log($"Spawned freeze powerup at {randomPos}");
        }
    }
}
