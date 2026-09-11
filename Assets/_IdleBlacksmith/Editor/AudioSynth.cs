using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// Synthesizes simple WAV fallbacks for any SFX the ElevenLabs download missed.
    /// Deterministic, no dependencies.
    /// </summary>
    public static class AudioSynth
    {
        const int SR = 44100;

        public static void EnsureFallbacks()
        {
            Try("hammer", Hammer());
            Try("coin", Coin());
            Try("pop", Pop());
            Try("upgrade", Upgrade());
            Try("hire", Hire());
            Try("denied", Denied());
            Try("crackle", Crackle());
        }

        static void Try(string name, float[] samples)
        {
            string mp3 = $"{Paths.Audio}/{name}.mp3";
            string wav = $"{Paths.Audio}/{name}.wav";
            if (File.Exists(mp3) || File.Exists(wav)) return;
            WriteWav(wav, samples);
            AssetDatabase.ImportAsset(wav);
            Debug.Log($"[AudioSynth] synthesized fallback {name}.wav");
        }

        // ------------------------------------------------------------ synth

        static float[] Render(float seconds, Func<float, float> f)
        {
            int n = (int)(SR * seconds);
            var s = new float[n];
            for (int i = 0; i < n; i++)
                s[i] = Mathf.Clamp(f(i / (float)SR) * 0.8f, -1f, 1f);
            return s;
        }

        static float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);

        static float[] Hammer() => Render(0.35f, t =>
        {
            float env = Mathf.Exp(-t * 34f);
            float metal = Sin(812f, t) * 0.5f + Sin(1217f, t) * 0.34f + Sin(1879f, t) * 0.26f + Sin(2513f, t) * 0.15f;
            float noise = (Mathf.PerlinNoise(t * 4000f, 0.3f) - 0.5f) * 2f * Mathf.Exp(-t * 260f);
            return metal * env * 0.9f + noise * 0.8f;
        });

        static float[] Coin() => Render(0.32f, t =>
        {
            float a = t < 0.06f ? Sin(1568f, t) * Mathf.Exp(-t * 16f) : 0f;
            float b = t >= 0.06f ? Sin(2093f, t - 0.06f) * Mathf.Exp(-(t - 0.06f) * 14f) : 0f;
            return (a + b) * 0.8f;
        });

        static float[] Pop() => Render(0.09f, t =>
        {
            float f = Mathf.Lerp(480f, 170f, t / 0.09f);
            return Sin(f, t) * Mathf.Exp(-t * 30f) * 0.9f;
        });

        static float[] Upgrade()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            return Render(0.62f, t =>
            {
                float sum = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float st = i * 0.105f;
                    if (t >= st)
                    {
                        float lt = t - st;
                        sum += Sin(notes[i], lt) * Mathf.Exp(-lt * 9f);
                    }
                }
                return sum * 0.5f;
            });
        }

        static float[] Hire()
        {
            float[] notes = { 392f, 523.25f, 659.25f, 783.99f };
            return Render(0.95f, t =>
            {
                float sum = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float st = i * 0.12f;
                    if (t >= st)
                    {
                        float lt = t - st;
                        sum += Sin(notes[i], lt) * Mathf.Exp(-lt * 6f) + Sin(notes[i] * 2f, lt) * 0.3f * Mathf.Exp(-lt * 8f);
                    }
                }
                return sum * 0.4f;
            });
        }

        static float[] Denied() => Render(0.26f, t =>
        {
            float f = t < 0.12f ? 165f : 123f;
            float lt = t < 0.12f ? t : t - 0.12f;
            return (Sin(f, lt) + 0.3f * Sin(f * 3f, lt)) * Mathf.Exp(-lt * 12f) * 0.5f;
        });

        static float[] Crackle()
        {
            var rng = new System.Random(1234);
            var popTimes = new float[26];
            var popAmp = new float[26];
            for (int i = 0; i < popTimes.Length; i++)
            {
                popTimes[i] = (float)rng.NextDouble() * 2.3f;
                popAmp[i] = 0.25f + (float)rng.NextDouble() * 0.6f;
            }
            float last = 0f;
            return Render(2.4f, t =>
            {
                // brown-ish noise bed
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = (last + 0.03f * white) / 1.03f;
                float bed = last * 0.9f;
                float pops = 0f;
                for (int i = 0; i < popTimes.Length; i++)
                {
                    float dt = t - popTimes[i];
                    if (dt > 0f && dt < 0.03f)
                        pops += popAmp[i] * Mathf.Exp(-dt * 300f) * (white > 0 ? 1f : -1f);
                }
                return bed + pops * 0.6f;
            });
        }

        // ------------------------------------------------------------ WAV IO

        static void WriteWav(string path, float[] samples)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int dataSize = samples.Length * 2;
                bw.Write("RIFF".ToCharArray());
                bw.Write(36 + dataSize);
                bw.Write("WAVE".ToCharArray());
                bw.Write("fmt ".ToCharArray());
                bw.Write(16);
                bw.Write((short)1);          // PCM
                bw.Write((short)1);          // mono
                bw.Write(SR);
                bw.Write(SR * 2);            // byte rate
                bw.Write((short)2);          // block align
                bw.Write((short)16);         // bits
                bw.Write("data".ToCharArray());
                bw.Write(dataSize);
                foreach (float s in samples)
                    bw.Write((short)(s * 32760f));
            }
        }
    }
}
