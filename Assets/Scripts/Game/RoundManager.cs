using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Game
{
    public class RoundManager : NetworkBehaviour
    {
        public static RoundManager Instance;
        public NetworkVariable<int> CurrentRound = new NetworkVariable<int>(1);

        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private int zombiesPerRound = 12;
        [SerializeField] private int maxZombiesAlive = 8;
        [SerializeField] private float spawnRadius = 45f;
        [SerializeField] private float roundDelay = 10f;
        [SerializeField] private float spawnInterval = 2f;

        private int zombiesToSpawn = 0;
        private int zombiesAlive = 0;
        private bool roundActive = false;
        private float spawnTimer = 0f;
        private bool gameOver = false;

        private void Awake() => Instance = this;

        public override void OnNetworkSpawn()
        {
            if (IsServer) Invoke(nameof(StartRound), 3f);
        }

        private void Update()
        {
            if (!IsServer || !roundActive) return;

            if (zombiesToSpawn > 0 && zombiesAlive < maxZombiesAlive)
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f)
                {
                    SpawnZombie();
                    zombiesToSpawn--;
                    spawnTimer = spawnInterval;
                    Debug.Log($"<color=cyan>Round {CurrentRound.Value}: Spawned zombie. Remaining to spawn: {zombiesToSpawn}, Alive: {zombiesAlive}</color>");
                }
            }

            if (zombiesToSpawn <= 0 && zombiesAlive <= 0)
            {
                Debug.Log($"<color=green>Round {CurrentRound.Value} COMPLETE! All zombies dead. Starting next round in {roundDelay} seconds...</color>");
                EndRound();
            }
        }

        private void StartRound()
        {
            roundActive = true;
            zombiesToSpawn = zombiesPerRound + (CurrentRound.Value - 1) * 2;
            zombiesToSpawn = Mathf.Min(zombiesToSpawn, 24);
            zombiesAlive = 0;
            Debug.Log($"<color=yellow>========== ROUND {CurrentRound.Value} STARTING! Total zombies to spawn: {zombiesToSpawn} ==========</color>");
            ShowRoundStartClientRpc(CurrentRound.Value);
        }

        private void SpawnZombie()
        {
            if (zombiePrefab == null)
            {
                Debug.LogError("Zombie prefab is NULL! Select RoundManager and assign zombie prefab in Inspector.");
                return;
            }

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * spawnRadius, 0.5f, Mathf.Sin(angle) * spawnRadius);
            GameObject zombie = Instantiate(zombiePrefab, pos, Quaternion.identity);

            var netObj = zombie.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("Zombie prefab missing NetworkObject component!");
                Destroy(zombie);
                return;
            }

            netObj.Spawn();
            var health = zombie.GetComponent<AI.ZombieHealth>();
            if (health) health.SetRoundHealth(CurrentRound.Value);
            zombiesAlive++;

            Debug.Log($"Spawned zombie at {pos}. Components: ZombieAI={zombie.GetComponent<AI.ZombieAI>() != null}, NavMeshAgent={zombie.GetComponent<UnityEngine.AI.NavMeshAgent>() != null}");
        }

        public void OnZombieDied()
        {
            if (!IsServer) return;
            zombiesAlive--;
            Debug.Log($"<color=red>Zombie died! Zombies still alive: {zombiesAlive}, Still to spawn: {zombiesToSpawn}</color>");
        }

        private void EndRound()
        {
            roundActive = false;
            CurrentRound.Value++;
            Invoke(nameof(StartRound), roundDelay);
        }

        [ClientRpc]
        private void ShowRoundStartClientRpc(int round)
        {
            Debug.Log($"<color=yellow>ROUND {round}</color>");
        }

        public void OnPlayerDied()
        {
            if (!IsServer || gameOver) return;

            // Check if all players are dead
            if (AreAllPlayersDead())
            {
                TriggerGameOver();
            }
        }

        private bool AreAllPlayersDead()
        {
            // Find all PlayerState components in the scene
            PlayerState[] allPlayers = FindObjectsByType<PlayerState>(FindObjectsSortMode.None);

            if (allPlayers.Length == 0)
            {
                Debug.LogWarning("No players found in scene!");
                return false;
            }

            // Check if ALL players are dead
            foreach (PlayerState player in allPlayers)
            {
                if (!player.IsDead())
                {
                    return false; // At least one player is still alive
                }
            }

            // All players are dead
            return true;
        }

        private void TriggerGameOver()
        {
            gameOver = true;
            roundActive = false;
            Debug.Log($"<color=red>TEAM WIPE! All players dead on Round {CurrentRound.Value}. GAME OVER!</color>");

            // Notify all clients to show game over screen
            ShowGameOverClientRpc(CurrentRound.Value);
        }

        [ClientRpc]
        private void ShowGameOverClientRpc(int finalRound)
        {
            if (UI.GameOverUI.Instance != null)
            {
                UI.GameOverUI.Instance.ShowGameOver(finalRound);
            }
            else
            {
                Debug.LogError("GameOverUI.Instance is null! Make sure GameOverUI exists in the scene.");
            }
        }
    }
}
