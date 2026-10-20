using System.Collections.Generic;
using UnityEngine;

namespace MistProtocol.Core
{
    /// <summary>
    /// Synthesises the game's sound effects at start-up so the project ships
    /// with zero audio files (keeps the repo small, no licensing issues).
    /// </summary>
    public static class ProceduralAudio
    {
        private const int Rate = 22050;
        private static readonly Dictionary<string, AudioClip> Cache = new();

        public static AudioClip Get(string name)
        {
            if (Cache.TryGetValue(name, out var c) && c != null) return c;
            c = name switch
            {
                "rifle" => Noise(0.18f, 18f, 0.9f, 900f),
                "shotgun" => Noise(0.32f, 9f, 1.3f, 480f),
                "revolver" => Noise(0.24f, 12f, 1.15f, 720f),
                "smg" => Noise(0.11f, 28f, 0.75f, 1400f),
                "enemyShot" => Noise(0.15f, 22f, 0.6f, 1600f),
                "explosion" => Noise(1.1f, 4f, 1f, 250f),
                "impact" => Noise(0.08f, 40f, 0.5f, 600f),
                "step" => Noise(0.05f, 60f, 0.25f, 300f),
                "reload" => Click(0.35f),
                "door" => Tone(0.6f, 70f, 0.4f, 6f),
                "zap" => Tone(0.25f, 900f, 0.4f, 12f, true),
                "beep" => Tone(0.12f, 1200f, 0.3f, 18f),
                "pickup" => Tone(0.25f, 660f, 0.35f, 8f),
                "hum" => Tone(1.0f, 110f, 0.2f, 0f),
                _ => Noise(0.1f, 30f, 0.3f, 800f),
            };
            c.name = name;
            Cache[name] = c;
            return c;
        }

        public static void PlayAt(string name, Vector3 pos, float volume = 1f, float pitch = 1f)
        {
            var go = new GameObject("SFX_" + name);
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = Get(name);
            src.volume = volume;
            src.pitch = pitch * Random.Range(0.93f, 1.07f);
            src.spatialBlend = 1f;
            src.minDistance = 2f;
            src.maxDistance = 45f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.Play();
            Object.Destroy(go, src.clip.length / Mathf.Max(0.1f, src.pitch) + 0.1f);
        }

        private static AudioClip Noise(float len, float decay, float amp, float lowpassHz)
        {
            int n = Mathf.CeilToInt(len * Rate);
            var data = new float[n];
            float a = Mathf.Clamp01(2f * Mathf.PI * lowpassHz / Rate);
            float y = 0f;
            var rng = new System.Random(n);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float x = (float)(rng.NextDouble() * 2 - 1);
                y += a * (x - y);
                data[i] = y * amp * Mathf.Exp(-t * decay) * 2.5f;
            }
            var clip = AudioClip.Create("noise", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Tone(float len, float freq, float amp, float decay, bool sweep = false)
        {
            int n = Mathf.CeilToInt(len * Rate);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float f = sweep ? freq * (1f - 0.6f * t / len) : freq;
                phase += 2f * Mathf.PI * f / Rate;
                float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(t * 200f) * Mathf.Clamp01((len - t) * 40f);
                data[i] = (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2.01f)) * amp * env;
            }
            var clip = AudioClip.Create("tone", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Click(float len)
        {
            int n = Mathf.CeilToInt(len * Rate);
            var data = new float[n];
            var rng = new System.Random(7);
            foreach (float at in new[] { 0.02f, 0.2f, 0.3f })
            {
                int s = (int)(at * Rate);
                for (int i = 0; i < 600 && s + i < n; i++)
                    data[s + i] += (float)(rng.NextDouble() * 2 - 1) * Mathf.Exp(-i / 90f) * 0.6f;
            }
            var clip = AudioClip.Create("click", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
