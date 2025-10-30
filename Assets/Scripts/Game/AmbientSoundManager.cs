using UnityEngine;
using System.Collections;

namespace SneakyGame.Game
{
    /// <summary>
    /// Manages ambient horror sounds for atmosphere
    /// </summary>
    public class AmbientSoundManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource heartbeatSource;
        [SerializeField] private AudioSource randomEffectsSource;

        [Header("Heartbeat Settings")]
        [SerializeField] private float baseHeartbeatPitch = 0.8f;
        [SerializeField] private float panicHeartbeatPitch = 1.5f;
        [SerializeField] private float heartbeatVolume = 0.3f;

        [Header("Random Effects")]
        [SerializeField] private float minEffectInterval = 10f;
        [SerializeField] private float maxEffectInterval = 30f;

        private Player.PlayerMovement localPlayer;
        private bool isInDanger = false;

        private void Start()
        {
            CreateAudioSourcesIfNeeded();
            SetupAudioSources();
            StartCoroutine(RandomEffectsCoroutine());
        }

        private void CreateAudioSourcesIfNeeded()
        {
            // CRITICAL: Ensure AudioListener exists (required for ANY audio in Unity)
            AudioListener existingListener = FindObjectOfType<AudioListener>();
            if (existingListener == null)
            {
                gameObject.AddComponent<AudioListener>();
                Debug.Log("[AmbientSoundManager] ⚠️ No AudioListener found - created fallback AudioListener");
                Debug.Log("[AmbientSoundManager] Note: Player camera will destroy this when player spawns");
            }
            else
            {
                Debug.Log("[AmbientSoundManager] ✓ AudioListener found on: " + existingListener.gameObject.name);
            }

            // Auto-create AudioSources if not assigned in Inspector
            if (ambientSource == null)
            {
                GameObject ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
                Debug.Log("[AmbientSoundManager] Created AmbientSource");
            }

            if (heartbeatSource == null)
            {
                GameObject heartbeatObj = new GameObject("HeartbeatSource");
                heartbeatObj.transform.SetParent(transform);
                heartbeatSource = heartbeatObj.AddComponent<AudioSource>();
                Debug.Log("[AmbientSoundManager] Created HeartbeatSource");
            }

            if (randomEffectsSource == null)
            {
                GameObject effectsObj = new GameObject("RandomEffectsSource");
                effectsObj.transform.SetParent(transform);
                randomEffectsSource = effectsObj.AddComponent<AudioSource>();
                Debug.Log("[AmbientSoundManager] Created RandomEffectsSource");
            }
        }

        private void SetupAudioSources()
        {
            // Setup ambient wind/atmosphere
            if (ambientSource != null)
            {
                // Load windmill ambience from Resources
                ambientSource.clip = Resources.Load<AudioClip>("Audio/Ambient/windmill_ambience");

                if (ambientSource.clip == null)
                {
                    Debug.Log("[AmbientSoundManager] Creating procedural ambient wind...");
                    ambientSource.clip = ProceduralAudioGenerator.CreateAmbientWind();

                    if (ambientSource.clip != null)
                    {
                        Debug.Log($"[AmbientSoundManager] ✓ Created wind clip: {ambientSource.clip.length}s, {ambientSource.clip.samples} samples");
                    }
                    else
                    {
                        Debug.LogError("[AmbientSoundManager] ❌ Failed to create wind clip!");
                        return;
                    }
                }

                ambientSource.loop = true;
                ambientSource.volume = 0.2f;
                ambientSource.pitch = 0.9f;
                ambientSource.spatialBlend = 0f; // 2D sound
                ambientSource.Play();

                Debug.Log($"[AmbientSoundManager] Ambient config: Volume={ambientSource.volume}, Loop={ambientSource.loop}, Clip={ambientSource.clip.name}");
                Debug.Log($"[AmbientSoundManager] Is Playing: {ambientSource.isPlaying}");

                if (!ambientSource.isPlaying)
                {
                    Debug.LogError("[AmbientSoundManager] ❌ AudioSource.Play() called but isPlaying=false!");
                }
                else
                {
                    Debug.Log("[AmbientSoundManager] ✓ Ambient wind CONFIRMED PLAYING");
                }
            }

            // Setup heartbeat
            if (heartbeatSource != null)
            {
                // Load slow heartbeat from Resources
                heartbeatSource.clip = Resources.Load<AudioClip>("Audio/Player/heartbeat_slow");

                if (heartbeatSource.clip == null)
                {
                    Debug.Log("[AmbientSoundManager] Using procedural heartbeat");
                    heartbeatSource.clip = ProceduralAudioGenerator.CreateHeartbeat();
                }

                heartbeatSource.loop = true;
                heartbeatSource.volume = 0f; // Start silent
                heartbeatSource.pitch = baseHeartbeatPitch;
                heartbeatSource.spatialBlend = 0f; // 2D sound
                heartbeatSource.Play();
                Debug.Log("[AmbientSoundManager] ✓ Heartbeat ready (silent until danger)");
            }

            // Setup random effects
            if (randomEffectsSource != null)
            {
                randomEffectsSource.loop = false;
                randomEffectsSource.volume = 0.4f;
                randomEffectsSource.spatialBlend = 0.5f; // Semi-3D
                Debug.Log("[AmbientSoundManager] ✓ Random effects ready");
            }

            Debug.Log("[AmbientSoundManager] All audio systems initialized!");
        }

        private void Update()
        {
            // Find local player
            if (localPlayer == null)
            {
                var players = FindObjectsOfType<Player.PlayerMovement>();
                foreach (var player in players)
                {
                    if (player.IsOwner)
                    {
                        localPlayer = player;
                        break;
                    }
                }
            }

            if (localPlayer != null)
            {
                UpdateHeartbeat();
            }
        }

        private void UpdateHeartbeat()
        {
            // Check if player is in danger (low stamina or zombies nearby)
            float stamina = localPlayer.GetStamina();
            float maxStamina = localPlayer.GetMaxStamina();
            float staminaPercent = stamina / maxStamina;

            // Check for nearby zombies
            var zombies = FindObjectsOfType<AI.ZombieAI>();
            float closestZombieDistance = float.MaxValue;

            foreach (var zombie in zombies)
            {
                float distance = Vector3.Distance(localPlayer.transform.position, zombie.transform.position);
                if (distance < closestZombieDistance)
                {
                    closestZombieDistance = distance;
                }
            }

            // Determine danger level
            bool lowStamina = staminaPercent < 0.3f;
            bool zombieClose = closestZombieDistance < 15f;

            isInDanger = lowStamina || zombieClose;

            // Adjust heartbeat
            if (heartbeatSource != null)
            {
                float targetVolume = 0f;
                float targetPitch = baseHeartbeatPitch;

                if (isInDanger)
                {
                    // More intense heartbeat when in danger
                    float dangerLevel = 1f - (closestZombieDistance / 15f);
                    dangerLevel = Mathf.Clamp01(dangerLevel);

                    // Also factor in low stamina
                    if (lowStamina)
                    {
                        dangerLevel = Mathf.Max(dangerLevel, 1f - staminaPercent);
                    }

                    targetVolume = Mathf.Lerp(0.1f, heartbeatVolume, dangerLevel);
                    targetPitch = Mathf.Lerp(baseHeartbeatPitch, panicHeartbeatPitch, dangerLevel);
                }

                heartbeatSource.volume = Mathf.Lerp(heartbeatSource.volume, targetVolume, Time.deltaTime * 2f);
                heartbeatSource.pitch = Mathf.Lerp(heartbeatSource.pitch, targetPitch, Time.deltaTime * 3f);
            }
        }

        private IEnumerator RandomEffectsCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minEffectInterval, maxEffectInterval));

                if (randomEffectsSource != null)
                {
                    // Load random creepy effect from Resources
                    int randomEffect = Random.Range(1, 3); // Choose between wind howl or creepy laugh
                    AudioClip effectClip = null;

                    if (randomEffect == 1)
                    {
                        int howlNum = Random.Range(1, 3);
                        effectClip = Resources.Load<AudioClip>($"Audio/Ambient/wind_howl_{howlNum}");
                    }
                    else
                    {
                        effectClip = Resources.Load<AudioClip>("Audio/Ambient/creepy_laugh");
                    }

                    if (effectClip == null)
                    {
                        Debug.LogWarning("Failed to load creepy effect - using procedural fallback");
                        effectClip = ProceduralAudioGenerator.CreateCreepyEffect();
                    }

                    randomEffectsSource.clip = effectClip;
                    randomEffectsSource.pitch = Random.Range(0.7f, 1.3f);
                    randomEffectsSource.Play();
                }
            }
        }
    }
}
