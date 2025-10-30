using UnityEngine;
using System;

namespace SneakyGame.Game
{
    /// <summary>
    /// Generates procedural audio clips at runtime
    /// Creates horror game sounds without needing audio files
    /// </summary>
    public static class ProceduralAudioGenerator
    {
        private const int SampleRate = 44100;

        /// <summary>
        /// Create a heartbeat sound (low frequency thump)
        /// </summary>
        public static AudioClip CreateHeartbeat()
        {
            float duration = 0.8f;
            int samples = (int)(duration * SampleRate);
            AudioClip clip = AudioClip.Create("Heartbeat", samples, 1, SampleRate, false);

            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Two quick thumps (lub-dub)
                float envelope1 = Mathf.Exp(-t * 25f); // First thump
                float envelope2 = Mathf.Exp(-(t - 0.15f) * 30f) * (t > 0.15f ? 1f : 0f); // Second thump

                // Low frequency sine wave (bass thump)
                float freq1 = 60f; // Deep bass
                float freq2 = 45f;

                float wave1 = Mathf.Sin(2f * Mathf.PI * freq1 * t) * envelope1;
                float wave2 = Mathf.Sin(2f * Mathf.PI * freq2 * (t - 0.15f)) * envelope2;

                data[i] = (wave1 + wave2) * 0.5f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create a zombie scream/groan
        /// </summary>
        public static AudioClip CreateZombieScream()
        {
            float duration = 1.5f;
            int samples = (int)(duration * SampleRate);
            AudioClip clip = AudioClip.Create("ZombieScream", samples, 1, SampleRate, false);

            float[] data = new float[samples];
            System.Random random = new System.Random();

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Envelope (attack, sustain, decay)
                float envelope = 1f;
                if (t < 0.1f)
                    envelope = t / 0.1f; // Attack
                else if (t > duration - 0.3f)
                    envelope = (duration - t) / 0.3f; // Decay

                // Multiple frequencies for growl/scream effect
                float freq1 = 200f + Mathf.Sin(t * 3f) * 100f; // Varying pitch
                float freq2 = 450f + Mathf.Sin(t * 5f) * 50f;

                // Base tones
                float wave1 = Mathf.Sin(2f * Mathf.PI * freq1 * t);
                float wave2 = Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.5f;

                // Add noise for texture
                float noise = ((float)random.NextDouble() * 2f - 1f) * 0.3f;

                // Combine
                data[i] = (wave1 + wave2 + noise) * envelope * 0.5f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create a zombie response groan
        /// </summary>
        public static AudioClip CreateZombieGroan()
        {
            float duration = 1.0f;
            int samples = (int)(duration * SampleRate);
            AudioClip clip = AudioClip.Create("ZombieGroan", samples, 1, SampleRate, false);

            float[] data = new float[samples];
            System.Random random = new System.Random();

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Slower envelope
                float envelope = Mathf.Exp(-t * 2f);

                // Lower frequencies for groan
                float freq1 = 150f + Mathf.Sin(t * 2f) * 50f;
                float freq2 = 300f;

                float wave1 = Mathf.Sin(2f * Mathf.PI * freq1 * t);
                float wave2 = Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.3f;

                // Less noise than scream
                float noise = ((float)random.NextDouble() * 2f - 1f) * 0.2f;

                data[i] = (wave1 + wave2 + noise) * envelope * 0.4f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create ambient wind/atmosphere
        /// </summary>
        public static AudioClip CreateAmbientWind()
        {
            float duration = 10f; // Long loop
            int samples = (int)(duration * SampleRate);
            AudioClip clip = AudioClip.Create("AmbientWind", samples, 1, SampleRate, false);

            float[] data = new float[samples];
            System.Random random = new System.Random();

            // Generate pink noise (more natural than white noise)
            float b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Pink noise algorithm
                float white = (float)random.NextDouble() * 2f - 1f;
                b0 = 0.99886f * b0 + white * 0.0555179f;
                b1 = 0.99332f * b1 + white * 0.0750759f;
                b2 = 0.96900f * b2 + white * 0.1538520f;
                b3 = 0.86650f * b3 + white * 0.3104856f;
                b4 = 0.55000f * b4 + white * 0.5329522f;
                b5 = -0.7616f * b5 - white * 0.0168980f;
                float pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362f;
                b6 = white * 0.115926f;

                // Low pass filter for wind effect
                float wind = pink * 0.1f;

                // Add slow wave modulation
                float modulation = 1f + Mathf.Sin(t * 0.5f) * 0.3f;

                data[i] = wind * modulation * 0.15f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create random creepy effect (distant sound)
        /// </summary>
        public static AudioClip CreateCreepyEffect()
        {
            float duration = 2f;
            int samples = (int)(duration * SampleRate);
            AudioClip clip = AudioClip.Create("CreepyEffect", samples, 1, SampleRate, false);

            float[] data = new float[samples];
            System.Random random = new System.Random();

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Slow attack and decay
                float envelope = Mathf.Sin(t / duration * Mathf.PI);

                // Eerie frequency sweep
                float freq = 300f + Mathf.Sin(t * 2f) * 200f;

                // Multiple harmonics for unsettling effect
                float wave1 = Mathf.Sin(2f * Mathf.PI * freq * t);
                float wave2 = Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.5f;
                float wave3 = Mathf.Sin(2f * Mathf.PI * (freq * 2.1f) * t) * 0.3f;

                // Add some noise
                float noise = ((float)random.NextDouble() * 2f - 1f) * 0.1f;

                data[i] = (wave1 + wave2 + wave3 + noise) * envelope * 0.2f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create footstep sound
        /// </summary>
        public static AudioClip CreateFootstep()
        {
            float duration = 0.15f;
            int samples = (int)(duration * SampleRate);
            AudioClip clip = AudioClip.Create("Footstep", samples, 1, SampleRate, false);

            float[] data = new float[samples];
            System.Random random = new System.Random();

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Sharp attack, quick decay
                float envelope = Mathf.Exp(-t * 30f);

                // Low frequency thud
                float freq = 120f;
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);

                // Add noise for texture
                float noise = ((float)random.NextDouble() * 2f - 1f) * 0.5f;

                data[i] = (wave * 0.5f + noise) * envelope * 0.3f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create gunshot sound (COD BO2 style)
        /// </summary>
        public static AudioClip CreateGunshot()
        {
            float duration = 0.3f;
            int samples = (int)(duration * SampleRate);
            AudioClip clip = AudioClip.Create("Gunshot", samples, 1, SampleRate, false);

            float[] data = new float[samples];
            System.Random random = new System.Random();

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Very sharp attack, quick decay
                float envelope = Mathf.Exp(-t * 40f);

                // Low frequency explosion
                float freq1 = 80f;
                float freq2 = 200f;

                float wave1 = Mathf.Sin(2f * Mathf.PI * freq1 * t);
                float wave2 = Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.5f;

                // Lots of noise for the "crack"
                float noise = ((float)random.NextDouble() * 2f - 1f);

                // Combine with emphasis on noise
                data[i] = (wave1 * 0.3f + wave2 * 0.2f + noise * 0.8f) * envelope * 0.6f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create zombie hit/impact sound
        /// </summary>
        public static AudioClip CreateZombieHit()
        {
            float duration = 0.2f;
            int samples = (int)(duration * SampleRate);
            AudioClip clip = AudioClip.Create("ZombieHit", samples, 1, SampleRate, false);

            float[] data = new float[samples];
            System.Random random = new System.Random();

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Quick impact envelope
                float envelope = Mathf.Exp(-t * 25f);

                // Mid-range thud
                float freq = 250f + Mathf.Sin(t * 50f) * 100f;
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);

                // Some noise for wet impact
                float noise = ((float)random.NextDouble() * 2f - 1f) * 0.4f;

                data[i] = (wave * 0.6f + noise) * envelope * 0.4f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create ammo pickup sound (high-pitched beep)
        /// </summary>
        public static AudioClip CreateAmmoPickup()
        {
            float duration = 0.25f;
            int samples = (int)(duration * SampleRate);
            AudioClip clip = AudioClip.Create("AmmoPickup", samples, 1, SampleRate, false);

            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Quick attack, medium decay
                float envelope = Mathf.Exp(-t * 12f);

                // High-pitched beep (pleasant sound)
                float freq1 = 800f;  // Primary tone
                float freq2 = 1200f; // Harmonic for richness

                float wave1 = Mathf.Sin(2f * Mathf.PI * freq1 * t);
                float wave2 = Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.3f;

                // Combine waves
                data[i] = (wave1 + wave2) * envelope * 0.5f;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
