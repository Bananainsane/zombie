using UnityEngine;

namespace SneakyGame.Game
{
    /// <summary>
    /// DEAD SIMPLE audio test - add to any GameObject to test audio
    /// </summary>
    public class SimpleAudioTest : MonoBehaviour
    {
        private AudioSource testSource;

        private void Start()
        {
            Debug.Log("========== SIMPLE AUDIO TEST START ==========");

            // Check for AudioListener
            AudioListener listener = FindObjectOfType<AudioListener>();
            if (listener == null)
            {
                Debug.LogError("❌ NO AUDIOLISTENER! Adding one now...");
                gameObject.AddComponent<AudioListener>();
            }
            else
            {
                Debug.Log($"✓ AudioListener exists on: {listener.gameObject.name}");
            }

            // Create AudioSource
            testSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("✓ Created AudioSource");

            // Create a simple test clip
            Debug.Log("Creating test audio clip...");
            AudioClip testClip = CreateSimpleBeep();

            if (testClip == null)
            {
                Debug.LogError("❌ Failed to create audio clip!");
                return;
            }

            Debug.Log($"✓ Created clip: {testClip.name}, Length: {testClip.length}s, Samples: {testClip.samples}");

            // Configure AudioSource
            testSource.clip = testClip;
            testSource.volume = 1.0f;
            testSource.loop = true;
            testSource.spatialBlend = 0f; // 2D sound
            testSource.playOnAwake = false;

            Debug.Log($"AudioSource configured - Volume: {testSource.volume}, Clip: {testSource.clip.name}");

            // Play it
            testSource.Play();

            Debug.Log($"✓ testSource.Play() called");
            Debug.Log($"Is Playing: {testSource.isPlaying}");
            Debug.Log($"Time: {testSource.time}");

            Debug.Log("========== YOU SHOULD HEAR A BEEP NOW ==========");
            Debug.Log("If you hear nothing, check:");
            Debug.Log("1. Unity Editor volume slider (top right, speaker icon)");
            Debug.Log("2. Windows volume mixer - is Unity muted?");
            Debug.Log("3. Headphones/speakers connected?");
        }

        private void Update()
        {
            if (testSource != null && Time.frameCount % 60 == 0) // Every 60 frames
            {
                Debug.Log($"[Frame {Time.frameCount}] AudioSource status: " +
                    $"Playing={testSource.isPlaying}, " +
                    $"Time={testSource.time:F2}, " +
                    $"Volume={testSource.volume}");
            }
        }

        private AudioClip CreateSimpleBeep()
        {
            int sampleRate = 44100;
            float duration = 0.5f;
            int samples = (int)(duration * sampleRate);

            AudioClip clip = AudioClip.Create("TestBeep", samples, 1, sampleRate, false);

            float[] data = new float[samples];
            float frequency = 440f; // A4 note

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            Debug.Log("========== AUDIO TEST STOPPED ==========");
        }
    }
}
