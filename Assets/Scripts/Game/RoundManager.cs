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
    }
}
