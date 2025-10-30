using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
namespace SneakyGame.AI
{
    public class ZombieHealth : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float headshotMultiplier = 2f;
        [Header("Death Settings")]
        [SerializeField] private float deathAnimationLength = 3f;
        [SerializeField] private GameObject bloodEffectPrefab;
        private NetworkVariable<float> health = new NetworkVariable<float>();
        private bool isDead = false;
        private Renderer bodyRenderer;
        private NavMeshAgent agent;
        private ZombieAI zombieAI;
        private Animator animator;
        private void Awake()
        {
            bodyRenderer = GetComponentInChildren<Renderer>();
            agent = GetComponent<NavMeshAgent>();
            zombieAI = GetComponent<ZombieAI>();
            animator = GetComponentInChildren<Animator>();
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) health.Value = maxHealth;
            health.OnValueChanged += OnHealthChanged;
        }
        public void SetRoundHealth(int round)
        {
            if (!IsServer) return;
            maxHealth = 150 + (round - 1) * 100;
            health.Value = maxHealth;
        }
        public void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (isDead) { Debug.Log($"{name} already dead, ignoring damage"); return; }
            if (!IsServer) { Debug.LogWarning($"{name} TakeDamage called on client, should only be server!"); return; }
            bool isHeadshot = hitPoint.y > (transform.position.y + 1.5f);
            if (isHeadshot)
            {
                damage *= headshotMultiplier;
                Debug.Log($"<color=red>{name} took HEADSHOT damage: {damage} (Health: {health.Value} -> {health.Value - damage})</color>");
            }
            else Debug.Log($"<color=orange>{name} took body damage: {damage} (Health: {health.Value} -> {health.Value - damage})</color>");
            float oldHealth = health.Value;
            health.Value = Mathf.Max(0, health.Value - damage);
            Debug.Log($"{name} health updated: {oldHealth} -> {health.Value}");
            ShowHitEffectClientRpc(hitPoint, isHeadshot);
            PlayHitSoundClientRpc();
            if (health.Value <= 0) { Debug.Log($"<color=yellow>{name} health reached 0, calling Die()</color>"); Die(); }
        }
        private void Die()
        {
            if (isDead) { Debug.LogWarning($"{name} Die() called but already dead!"); return; }
            isDead = true;
            Debug.Log($"<color=green>*** {name} DIED! ***</color>");
            if (Game.RoundManager.Instance != null) Game.RoundManager.Instance.OnZombieDied();
            if (zombieAI != null) { zombieAI.enabled = false; Debug.Log($"{name}: Disabled ZombieAI"); }
            var animController = GetComponent<AI.ZombieAnimationController>();
            if (animController != null) { animController.enabled = false; Debug.Log($"{name}: Disabled ZombieAnimationController"); }
            if (agent != null) { agent.enabled = false; Debug.Log($"{name}: Disabled NavMeshAgent"); }
            bool playedDeathAnimation = false;
            if (animator != null)
            {
                int randomDeath = Random.Range(1, 3);
                string deathAnimName = $"death{randomDeath}";
                animator.Play(deathAnimName, 0, 0f);
                Debug.Log($"<color=purple>{name}: Playing death animation '{deathAnimName}'</color>");
                playedDeathAnimation = true;
                foreach (var param in animator.parameters)
                {
                    if (param.name.ToLower().Contains("death") || param.name.ToLower().Contains("die"))
                    {
                        if (param.type == AnimatorControllerParameterType.Trigger) animator.SetTrigger(param.name);
                        else if (param.type == AnimatorControllerParameterType.Bool) animator.SetBool(param.name, true);
                    }
                }
            }
            DieClientRpc();
            float despawnDelay = playedDeathAnimation ? deathAnimationLength : 0.5f;
            Invoke(nameof(DespawnZombie), despawnDelay);
            Debug.Log($"{name}: Will despawn in {despawnDelay} seconds");
        }
        private void DespawnZombie()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                Debug.Log($"{name}: Despawning zombie from network");
                NetworkObject.Despawn(true);
            }
        }
        [ClientRpc]
        private void DieClientRpc()
        {
            Collider[] colliders = GetComponents<Collider>();
            foreach (var col in colliders) if (!col.isTrigger) col.enabled = false;
            if (bodyRenderer != null)
            {
                Color darkColor = bodyRenderer.material.color * 0.5f;
                darkColor.a = 1f;
                bodyRenderer.material.color = darkColor;
            }
        }
        [ClientRpc]
        private void ShowHitEffectClientRpc(Vector3 hitPoint, bool isHeadshot)
        {
            if (bloodEffectPrefab != null)
            {
                GameObject blood = Instantiate(bloodEffectPrefab, hitPoint, Quaternion.identity);
                Destroy(blood, 2f);
            }
            if (bodyRenderer != null && !isDead) StartCoroutine(FlashRed());
        }
        private System.Collections.IEnumerator FlashRed()
        {
            Color originalColor = bodyRenderer.material.color;
            bodyRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            bodyRenderer.material.color = originalColor;
        }
        [ClientRpc]
        private void PlayHitSoundClientRpc()
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                AudioClip hitClip = Resources.Load<AudioClip>("Audio/Zombies/zombie_hit");
                if (hitClip == null) hitClip = Game.ProceduralAudioGenerator.CreateZombieHit();
                if (hitClip != null) audioSource.PlayOneShot(hitClip, 0.5f);
            }
        }
        private void OnHealthChanged(float previousValue, float newValue) { }
        public float GetHealth() => health.Value;
        public float GetMaxHealth() => maxHealth;
        public bool IsDead() => isDead;
    }
}
