using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

namespace SneakyGame.AI
{
    public class ZombieAI : NetworkBehaviour
    {
        [Header("Detection")][SerializeField] private float visionRadius = 30f;
        [SerializeField] private float hearingRadius = 40f;
        [SerializeField] private float loseTargetRadius = 50f;
        [SerializeField] private float visionAngle = 180f;
        [SerializeField] private LayerMask obstacleMask;
        [Header("References")][SerializeField] private Renderer bodyRenderer;
        [SerializeField] private AudioSource audioSource;
        [Header("Speed Settings")][SerializeField] private float normalSpeed = 2f;
        [SerializeField] private float alertedSpeed = 3.5f;
        [SerializeField] private float frozenSpeed = 0.5f;
        [Header("Search Pattern Settings")][SerializeField] private float searchRadius = 15f;
        [SerializeField] private float searchWaitTime = 3f;
        [SerializeField] private float spreadDistance = 20f;
        private Vector3 searchPosition;
        private float searchTimer = 0f;
        [Header("Flanking Settings")][SerializeField] private float flankRadius = 8f;
        [SerializeField] private float flankUpdateInterval = 2f;
        private float flankTimer = 0f;
        private Vector3 flankPosition;
        [Header("Flocking Settings")][SerializeField] private float separationRadius = 2f;
        [SerializeField] private float alignmentRadius = 5f;
        [SerializeField] private float cohesionRadius = 5f;
        [SerializeField] private float flockingWeight = 0.5f;
        private Vector3 velocity;
        [Header("Slowdown Effect")]private NetworkVariable<bool> isFrozen = new NetworkVariable<bool>(false);
        private float freezeTimer = 0f;
        [Header("Audio Timers")]
        private float idleSoundTimer = 0f;
        private float chaseSoundTimer = 0f;
        private const float IDLE_SOUND_INTERVAL_MIN = 8f;
        private const float IDLE_SOUND_INTERVAL_MAX = 15f;
        private const float CHASE_SOUND_INTERVAL_MIN = 4f;
        private const float CHASE_SOUND_INTERVAL_MAX = 7f;
        private enum ZombieState { Searching, Alerted, Chasing }
        private ZombieState currentState = ZombieState.Searching;
        private NavMeshAgent agent;
        private Transform targetPlayer;
        private Vector3 lastKnownPlayerPosition;
        private bool hasSeenPlayerBefore = false;
        private static System.Collections.Generic.List<ZombieAI> allZombies = new System.Collections.Generic.List<ZombieAI>();

        private void Awake() { agent = GetComponent<NavMeshAgent>(); }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { agent.speed = normalSpeed; allZombies.Add(this); velocity = Vector3.zero; currentState = ZombieState.Searching; PickNewSearchPosition(); RandomizeZombieSkin(); idleSoundTimer = Random.Range(IDLE_SOUND_INTERVAL_MIN, IDLE_SOUND_INTERVAL_MAX); chaseSoundTimer = Random.Range(CHASE_SOUND_INTERVAL_MIN, CHASE_SOUND_INTERVAL_MAX); Debug.Log($"{name} spawned on server! Starting search mode. Agent speed: {agent.speed}"); }
            SetupAudio();
        }

        private void RandomizeZombieSkin()
        {
            if (bodyRenderer == null) return;
            float scaleVariation = Random.Range(0.9f, 1.15f);
            bodyRenderer.transform.localScale = Vector3.one * scaleVariation;
            if (bodyRenderer.material != null) Debug.Log($"{name} using original model materials");
        }

        private void SetupAudio()
        {
            if (audioSource != null)
            {
                int randomGroan = Random.Range(1, 3);
                audioSource.clip = Resources.Load<AudioClip>($"Audio/Zombies/zombie_groan_{randomGroan}");
                if (audioSource.clip == null) { Debug.LogWarning($"{name} failed to load zombie groan audio - using fallback"); audioSource.clip = Game.ProceduralAudioGenerator.CreateZombieGroan(); }
                else Debug.Log($"{name} audio setup complete with groan {randomGroan}");
            }
        }

        public override void OnNetworkDespawn() { if (IsServer) allZombies.Remove(this); base.OnNetworkDespawn(); }

        private void Update()
        {
            if (!IsServer) return;
            if (isFrozen.Value) { freezeTimer -= Time.deltaTime; if (freezeTimer <= 0) { isFrozen.Value = false; UpdateSpeed(); UpdateColor(); } }
            Transform detected = DetectPlayerWithSenses();
            switch (currentState) { case ZombieState.Searching: SearchBehavior(detected); break; case ZombieState.Alerted: AlertedBehavior(detected); break; case ZombieState.Chasing: ChasingBehavior(detected); break; }
            ApplyFlocking();
            UpdateColorBasedOnState();
            UpdateAudioBasedOnState();
        }

        private void UpdateAudioBasedOnState()
        {
            // Idle sounds while searching
            if (currentState == ZombieState.Searching)
            {
                idleSoundTimer -= Time.deltaTime;
                if (idleSoundTimer <= 0f)
                {
                    PlayIdleGroanClientRpc();
                    idleSoundTimer = Random.Range(IDLE_SOUND_INTERVAL_MIN, IDLE_SOUND_INTERVAL_MAX);
                }
            }
            // Chase sounds while chasing
            else if (currentState == ZombieState.Chasing)
            {
                chaseSoundTimer -= Time.deltaTime;
                if (chaseSoundTimer <= 0f)
                {
                    PlayChaseGroanClientRpc();
                    chaseSoundTimer = Random.Range(CHASE_SOUND_INTERVAL_MIN, CHASE_SOUND_INTERVAL_MAX);
                }
            }
        }

        private void ApplyFlocking()
        {
            if (agent.hasPath && agent.velocity.magnitude > 0.1f) velocity = agent.velocity;
            Vector3 flockingForce = CalculateFlockingForce();
            if (flockingForce.magnitude > 0.1f && agent.hasPath) { Vector3 adjustedDestination = agent.destination + flockingForce * Time.deltaTime; NavMeshHit hit; if (NavMesh.SamplePosition(adjustedDestination, out hit, 5f, NavMesh.AllAreas)) agent.SetDestination(hit.position); }
        }

        private void SearchBehavior(Transform detected)
        {
            if (detected != null) { currentState = ZombieState.Chasing; targetPlayer = detected; lastKnownPlayerPosition = detected.position; if (!hasSeenPlayerBefore) { hasSeenPlayerBefore = true; PlayZombieScreamClientRpc(); } AlertAllZombies(detected.position); UpdateSpeed(); Debug.Log($"{name} FOUND PLAYER! Switching to CHASE mode"); return; }
            if (!agent.hasPath || agent.remainingDistance < 2f) { searchTimer -= Time.deltaTime; if (searchTimer <= 0f) PickNewSearchPosition(); }
        }

        private void AlertedBehavior(Transform detected)
        {
            if (detected != null) { currentState = ZombieState.Chasing; targetPlayer = detected; lastKnownPlayerPosition = detected.position; UpdateSpeed(); Debug.Log($"{name} spotted player while alerted! Switching to CHASE"); return; }
            if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 3f) { currentState = ZombieState.Searching; PickNewSearchPosition(); UpdateSpeed(); Debug.Log($"{name} reached alert position but found nothing. Back to SEARCH"); }
            else agent.SetDestination(lastKnownPlayerPosition);
        }

        private void ChasingBehavior(Transform detected)
        {
            if (detected != null) { targetPlayer = detected; lastKnownPlayerPosition = detected.position; flankTimer -= Time.deltaTime; if (flankTimer <= 0f) { CalculateFlankPosition(detected.position); flankTimer = flankUpdateInterval; } float distance = Vector3.Distance(transform.position, detected.position); if (distance > 5f && Random.value > 0.7f) agent.SetDestination(flankPosition); else agent.SetDestination(PredictPlayerPosition(detected)); }
            else { float distance = Vector3.Distance(transform.position, lastKnownPlayerPosition); if (distance > loseTargetRadius) { currentState = ZombieState.Searching; targetPlayer = null; PickNewSearchPosition(); UpdateSpeed(); Debug.Log($"{name} lost player completely. Returning to SEARCH mode"); } else agent.SetDestination(lastKnownPlayerPosition); }
        }

        private void PickNewSearchPosition()
        {
            int zombieIndex = allZombies.IndexOf(this);
            int totalZombies = allZombies.Count;
            float angleStep = 360f / Mathf.Max(1, totalZombies);
            float angle = angleStep * zombieIndex + Random.Range(-angleStep * 0.3f, angleStep * 0.3f);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 targetPos = transform.position + direction * Random.Range(searchRadius * 0.5f, searchRadius);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, searchRadius, NavMesh.AllAreas)) { searchPosition = hit.position; agent.SetDestination(searchPosition); searchTimer = searchWaitTime; Debug.Log($"{name} picked new search position in sector {zombieIndex}"); }
        }

        private void UpdateSpeed() { if (isFrozen.Value) agent.speed = frozenSpeed; else if (currentState == ZombieState.Chasing || currentState == ZombieState.Alerted) agent.speed = alertedSpeed; else agent.speed = normalSpeed; }
        private void UpdateColorBasedOnState() { return; }

        private Transform DetectPlayerWithSenses()
        {
            var players = FindObjectsOfType<Game.PlayerState>().Where(p => p.CompareTag("Player") && !p.IsInfected.Value).ToArray();
            if (players.Length == 0) { var allPlayers = FindObjectsOfType<Game.PlayerState>(); if (allPlayers.Length == 0) Debug.LogWarning($"{name}: No PlayerState objects found in scene!"); return null; }
            Transform bestTarget = null;
            float bestPriority = float.MaxValue;
            foreach (var playerState in players)
            {
                Transform player = playerState.transform;
                float distance = Vector3.Distance(transform.position, player.position);
                bool canSee = CanSeePlayer(player, distance);
                float noiseLevel = 0f;
                if (player.TryGetComponent<Player.PlayerMovement>(out var movement)) noiseLevel = movement.GetNoiseLevel();
                float soundDetectionRadius = hearingRadius * noiseLevel;
                bool canHear = distance < soundDetectionRadius;
                if (canSee || canHear) { float priority = distance - (noiseLevel * 10f); if (priority < bestPriority) { bestPriority = priority; bestTarget = player; } }
            }
            return bestTarget;
        }

        private bool CanSeePlayer(Transform player, float distance)
        {
            if (distance > visionRadius) return false;
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            if (angle > visionAngle / 2f) return false;
            Vector3 rayStart = transform.position + Vector3.up * 1f;
            Vector3 rayEnd = player.position + Vector3.up * 1f;
            if (Physics.Raycast(rayStart, (rayEnd - rayStart).normalized, out RaycastHit hit, distance)) return hit.transform == player || hit.transform.IsChildOf(player);
            return true;
        }

        private Vector3 PredictPlayerPosition(Transform player) { if (player.TryGetComponent<Rigidbody>(out var rb) && rb.linearVelocity.magnitude > 0.1f) return player.position + rb.linearVelocity * 1f; return player.position; }

        private void CalculateFlankPosition(Vector3 playerPos)
        {
            var nearbyZombies = allZombies.Where(z => z != this && Vector3.Distance(z.transform.position, playerPos) < 20f).ToList();
            if (nearbyZombies.Count == 0) { Vector3 toPlayer = (playerPos - transform.position).normalized; Vector3 perpendicular = Vector3.Cross(toPlayer, Vector3.up).normalized; flankPosition = playerPos + perpendicular * flankRadius; }
            else { int zombieIndex = allZombies.IndexOf(this); float angleStep = 360f / (nearbyZombies.Count + 1); float angle = angleStep * zombieIndex; Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * flankRadius; flankPosition = playerPos + offset; }
            NavMeshHit hit;
            if (NavMesh.SamplePosition(flankPosition, out hit, 10f, NavMesh.AllAreas)) flankPosition = hit.position;
        }

        private void AlertAllZombies(Vector3 playerPosition) { foreach (var zombie in allZombies) if (zombie != this && zombie.currentState == ZombieState.Searching) zombie.AlertToPosition(playerPosition); Debug.Log($"Alerted zombies to player at position {playerPosition}"); }

        public void AlertToPosition(Vector3 position) { if (!IsServer) return; currentState = ZombieState.Alerted; lastKnownPlayerPosition = position; UpdateSpeed(); PlayZombieResponseClientRpc(); Debug.Log($"{name} was alerted to position {position}"); }

        [ClientRpc]
        private void PlayZombieScreamClientRpc()
        {
            if (audioSource != null)
            {
                int randomScream = Random.Range(1, 3);
                AudioClip screamClip = Resources.Load<AudioClip>($"Audio/Zombies/zombie_scream_{randomScream}");
                if (screamClip == null) { Debug.LogWarning("Failed to load zombie scream - using fallback"); screamClip = Game.ProceduralAudioGenerator.CreateZombieScream(); }
                audioSource.clip = screamClip;
                audioSource.pitch = Random.Range(1.0f, 1.3f);
                audioSource.volume = 1f;
                audioSource.Play();
            }
        }

        [ClientRpc]
        private void PlayZombieResponseClientRpc()
        {
            if (audioSource != null)
            {
                int randomGroan = Random.Range(1, 3);
                AudioClip groanClip = Resources.Load<AudioClip>($"Audio/Zombies/zombie_groan_{randomGroan}");
                if (groanClip == null) { Debug.LogWarning("Failed to load zombie groan - using fallback"); groanClip = Game.ProceduralAudioGenerator.CreateZombieGroan(); }
                audioSource.clip = groanClip;
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.volume = 0.7f;
                audioSource.Play();
            }
        }

        [ClientRpc]
        private void PlayIdleGroanClientRpc()
        {
            if (audioSource != null && !audioSource.isPlaying)
            {
                AudioClip groanClip = Game.ProceduralAudioGenerator.CreateZombieGroan();
                audioSource.clip = groanClip;
                audioSource.pitch = Random.Range(0.8f, 1.0f);
                audioSource.volume = 0.4f;  // Quieter for idle
                audioSource.Play();
            }
        }

        [ClientRpc]
        private void PlayChaseGroanClientRpc()
        {
            if (audioSource != null && !audioSource.isPlaying)
            {
                // More aggressive sound for chasing
                AudioClip aggressiveSound = Game.ProceduralAudioGenerator.CreateZombieScream();
                audioSource.clip = aggressiveSound;
                audioSource.pitch = Random.Range(1.1f, 1.4f);  // Higher pitch for urgency
                audioSource.volume = 0.8f;  // Louder for chase
                audioSource.Play();
            }
        }

        public void FreezeZombie(float duration) { if (!IsServer) return; isFrozen.Value = true; freezeTimer = duration; agent.speed = frozenSpeed; UpdateColor(); }
        private void UpdateColor() { return; }

        private Vector3 CalculateFlockingForce() { if (allZombies.Count <= 1) return Vector3.zero; Vector3 separation = Separation(); Vector3 alignment = Alignment(); Vector3 cohesion = Cohesion(); return (separation + alignment + cohesion) * flockingWeight; }

        private Vector3 Separation()
        {
            Vector3 steer = Vector3.zero;
            int count = 0;
            foreach (var other in allZombies) { if (other == this || other == null) continue; float d = Vector3.Distance(transform.position, other.transform.position); if (d < separationRadius && d > 0) { steer += (transform.position - other.transform.position).normalized / d; count++; } }
            if (count > 0) { steer /= count; steer = steer.normalized * agent.speed; }
            return steer;
        }

        private Vector3 Alignment()
        {
            Vector3 avg = Vector3.zero;
            int count = 0;
            foreach (var other in allZombies) { if (other == this || other == null) continue; float d = Vector3.Distance(transform.position, other.transform.position); if (d < alignmentRadius) { avg += other.velocity; count++; } }
            if (count > 0) { avg /= count; avg = avg.normalized * agent.speed; return avg - velocity; }
            return Vector3.zero;
        }

        private Vector3 Cohesion()
        {
            Vector3 center = Vector3.zero;
            int count = 0;
            foreach (var other in allZombies) { if (other == this || other == null) continue; float d = Vector3.Distance(transform.position, other.transform.position); if (d < cohesionRadius) { center += other.transform.position; count++; } }
            if (count > 0) { center /= count; Vector3 desired = (center - transform.position).normalized * agent.speed; return desired - velocity; }
            return Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, visionRadius);
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, hearingRadius);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, loseTargetRadius);
            Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position, separationRadius);
            Gizmos.color = Color.green; Gizmos.DrawWireSphere(transform.position, alignmentRadius);
        }
    }
}
