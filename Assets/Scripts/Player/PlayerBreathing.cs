using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Player
{
    /// <summary>
    /// Handles breathing sound effects that increase when in danger or low stamina
    /// </summary>
    public class PlayerBreathing : NetworkBehaviour
    {
        [Header("Breathing Settings")]
        [SerializeField] private float normalBreathingInterval = 4f;
        [SerializeField] private float panicBreathingInterval = 1.5f;
        [SerializeField] private float breathVolume = 0.3f;

        [Header("References")]
        [SerializeField] private AudioSource breathingSource;

        private PlayerMovement playerMovement;
        private float breathTimer = 0f;
        private float currentBreathInterval;

        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner) return;

            // Setup breathing audio source
            if (breathingSource == null)
            {
                breathingSource = gameObject.AddComponent<AudioSource>();
            }

            breathingSource.spatialBlend = 0f; // 2D sound (only owner hears it)
            breathingSource.volume = breathVolume;
            breathingSource.playOnAwake = false;

            currentBreathInterval = normalBreathingInterval;
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Check stamina and nearby zombies to determine breathing rate
            float stamina = playerMovement != null ? playerMovement.GetStamina() : 100f;
            float maxStamina = playerMovement != null ? playerMovement.GetMaxStamina() : 100f;
            float staminaPercent = stamina / maxStamina;

            // Check for nearby zombies
            var zombies = FindObjectsOfType<AI.ZombieAI>();
            float closestZombieDistance = float.MaxValue;

            foreach (var zombie in zombies)
            {
                float distance = Vector3.Distance(transform.position, zombie.transform.position);
                if (distance < closestZombieDistance)
                {
                    closestZombieDistance = distance;
                }
            }

            // Determine breathing rate based on danger
            bool inDanger = staminaPercent < 0.4f || closestZombieDistance < 15f;

            if (inDanger)
            {
                float dangerLevel = 1f - (closestZombieDistance / 15f);
                if (staminaPercent < 0.4f)
                {
                    dangerLevel = Mathf.Max(dangerLevel, 1f - staminaPercent);
                }

                currentBreathInterval = Mathf.Lerp(normalBreathingInterval, panicBreathingInterval, dangerLevel);
            }
            else
            {
                currentBreathInterval = normalBreathingInterval;
            }

            // Play breathing sounds
            breathTimer -= Time.deltaTime;
            if (breathTimer <= 0f)
            {
                PlayBreathSound();
                breathTimer = currentBreathInterval;
            }
        }

        private void PlayBreathSound()
        {
            if (breathingSource == null) return;

            // Simple breathing simulation using pitch variation
            breathingSource.pitch = Random.Range(0.9f, 1.1f);
            breathingSource.PlayOneShot(CreateBreathSound());
        }

        private AudioClip CreateBreathSound()
        {
            // Generate simple breath sound (exhale)
            float duration = 0.6f;
            int sampleRate = 44100;
            int samples = (int)(duration * sampleRate);
            AudioClip clip = AudioClip.Create("Breath", samples, 1, sampleRate, false);

            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;

                // Envelope (fade in and out)
                float envelope = Mathf.Sin(t / duration * Mathf.PI);

                // White noise for breath texture
                float noise = (Random.value * 2f - 1f) * 0.15f;

                // Low frequency rumble
                float lowFreq = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.1f;

                data[i] = (noise + lowFreq) * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
