using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Game
{
    public class PlayerState : NetworkBehaviour
    {
        [Header("Player State")]
        public NetworkVariable<bool> IsInfected = new NetworkVariable<bool>();
        public NetworkVariable<float> Health = new NetworkVariable<float>(100f);

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float respawnDelay = 3f;

        private Renderer playerRenderer;
        private bool isDead = false;

        private void Awake() => playerRenderer = GetComponentInChildren<Renderer>();

        public override void OnNetworkSpawn()
        {
            IsInfected.OnValueChanged += OnInfectedChanged;
            Health.OnValueChanged += OnHealthChanged;

            OnInfectedChanged(false, IsInfected.Value);
            OnHealthChanged(0f, Health.Value);

            // Initialize health to max
            if (IsServer)
            {
                Health.Value = maxHealth;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void BecomeInfectedServerRpc() => IsInfected.Value = true;

        [ServerRpc(RequireOwnership = false)]
        public void ResetInfectionServerRpc() => IsInfected.Value = false;

        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage)
        {
            if (isDead) return;

            Health.Value = Mathf.Max(0, Health.Value - damage);

            if (Health.Value <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            Debug.Log($"{name} has died!");

            // Disable player controls
            if (TryGetComponent<Player.PlayerMovement>(out var movement))
            {
                movement.enabled = false;
            }

            // Disable weapon shooting
            if (TryGetComponent<Player.WeaponController>(out var weapon))
            {
                weapon.enabled = false;
            }

            // Notify clients
            DieClientRpc();

            // Notify RoundManager of player death for team wipe check
            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.OnPlayerDied();
            }
        }

        [ClientRpc]
        private void DieClientRpc()
        {
            Debug.Log($"<color=red>YOU DIED! Respawning in {respawnDelay} seconds...</color>");
        }

        private void Respawn()
        {
            isDead = false;
            Health.Value = maxHealth;

            // Re-enable controls
            if (TryGetComponent<Player.PlayerMovement>(out var movement))
            {
                movement.enabled = true;
            }

            Debug.Log($"<color=green>RESPAWNED! Health restored to {maxHealth}</color>");
        }

        private void OnHealthChanged(float previousValue, float newValue)
        {
            // Update UI or visual feedback
            if (newValue <= 0)
            {
                Debug.Log($"{name} health reached 0");
            }
        }

        private void OnInfectedChanged(bool was, bool now)
        {
            if (playerRenderer) playerRenderer.material.color = Color.blue;
            if (TryGetComponent<Player.PlayerMovement>(out var movement))
                movement.SetInfectedSpeed(false);
        }


        public float GetMaxHealth() => maxHealth;
        public bool IsDead() => isDead;
    }
}
