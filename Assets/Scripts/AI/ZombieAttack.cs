using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.AI
{
    /// <summary>
    /// Zombie attack system - damages players on contact
    /// </summary>
    public class ZombieAttack : NetworkBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float attackRange = 2f;

        private float lastAttackTime = 0f;
        private ZombieAnimationController animController;

        private void Awake()
        {
            animController = GetComponent<ZombieAnimationController>();
        }

        private void OnTriggerStay(Collider other)
        {
            if (!IsServer) return;

            // Check if we hit a player
            if (other.CompareTag("Player"))
            {
                if (other.TryGetComponent<Game.PlayerState>(out var playerState))
                {
                    // Don't attack dead players
                    if (playerState.IsDead())
                    {
                        Debug.Log($"{name}: Player {playerState.name} is dead, not attacking");
                        return;
                    }

                    // Check cooldown
                    if (Time.time - lastAttackTime >= attackCooldown)
                    {
                        Debug.Log($"<color=red>{name} is ATTACKING {playerState.name}!</color>");
                        AttackPlayer(playerState);
                        lastAttackTime = Time.time;
                    }
                }
            }
        }

        private void AttackPlayer(Game.PlayerState playerState)
        {
            playerState.TakeDamageServerRpc(attackDamage);

            if (animController != null)
            {
                animController.PlayAttack();
            }
            else
            {
                Animator animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    int randomAttack = Random.Range(1, 5);
                    string attackAnim = $"atack{randomAttack}";
                    animator.Play(attackAnim, 0);
                    Debug.Log($"<color=red>{name}: Playing attack animation '{attackAnim}'</color>");
                }
            }

            PlayAttackSoundClientRpc();
            Debug.Log($"{name} attacked {playerState.name} for {attackDamage} damage");
        }

        [ClientRpc]
        private void PlayAttackSoundClientRpc()
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                // Load random attack sound
                int randomAttack = Random.Range(1, 3);
                AudioClip attackClip = Resources.Load<AudioClip>($"Audio/Zombies/zombie_attack_{randomAttack}");

                if (attackClip != null)
                {
                    audioSource.PlayOneShot(attackClip, 0.6f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
