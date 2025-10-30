using Unity.Netcode;
using UnityEngine;
using System.Linq;
using SneakyGame.UI;

namespace SneakyGame.Game
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance;

        [SerializeField] private float roundTime = 180f;
        private NetworkVariable<float> timeLeft = new NetworkVariable<float>(0f);
        private NetworkVariable<bool> roundActive = new NetworkVariable<bool>(false);

        private void Awake() => Instance = this;

        public override void OnNetworkSpawn()
        {
            if (IsServer) StartRound();
        }

        private void Update()
        {
            // Check if NetworkBehaviour is spawned before accessing NetworkVariables
            if (!IsSpawned || !IsServer || !roundActive.Value) return;

            timeLeft.Value -= Time.deltaTime;
            if (timeLeft.Value <= 0) EndRound("TIME UP! Survivors Win!");

            CheckWinCondition();
        }

        private void StartRound()
        {
            roundActive.Value = true;
            timeLeft.Value = roundTime;
        }

        private void CheckWinCondition()
        {
            var players = FindObjectsOfType<PlayerState>().Where(p => p.CompareTag("Player")).ToArray();
            var survivors = players.Count(p => !p.IsInfected.Value);

            if (survivors == 0 && players.Length > 0) EndRound("Zombies Win!");
        }

        private void EndRound(string message)
        {
            roundActive.Value = false;
            ShowWinnerClientRpc(message);
            Invoke(nameof(ResetAndStartRound), 5f);
        }

        private void ResetAndStartRound()
        {
            // Reset all player infection states
            var players = FindObjectsOfType<PlayerState>().Where(p => p.CompareTag("Player")).ToArray();
            foreach (var player in players)
            {
                player.ResetInfectionServerRpc();
            }

            // Respawn players at random positions
            foreach (var player in players)
            {
                if (player.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                {
                    // Players don't have NavMeshAgent, skip
                }
                else
                {
                    // Random spawn position
                    Vector3 spawnPos = new Vector3(
                        Random.Range(-35f, 35f),
                        1f,
                        Random.Range(-35f, 35f)
                    );
                    player.transform.position = spawnPos;
                }
            }

            StartRound();
        }

        [ClientRpc]
        private void ShowWinnerClientRpc(string message) => RoundEndUI.Instance?.ShowWinner(message);

        public float GetTimeLeft() => timeLeft.Value;
        public bool IsRoundActive() => roundActive.Value;
    }
}
