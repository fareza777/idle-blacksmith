using System;
using System.Collections.Generic;
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
            Try("fanfare", Fanfare());
            Try("mine_pick", MinePick());
            Try("market_chime", MarketChime());
            Try("enchant", Enchant());
            Try("quest_done", QuestDone());
            Try("achievement", Achievement());
            Try("prestige", Prestige());
            Try("unlock", Unlock());
            Try("levelup", LevelUp());
            Try("whoosh", Whoosh());
            Try("blip", Blip());
            Try("ember_whoosh", EmberWhoosh());
            Try("amb_fire", AmbFire());
            Try("music_deep", MusicDeep());
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

        // ------------------------------------------------------------ content sfx

        static float[] Fanfare() => Render(1.6f, t =>
        {
            float[] notes = { 392f, 523.25f, 659.25f, 783.99f, 1046.5f };
            float sum = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float st = i * 0.13f;
                if (t < st) continue;
                float lt = t - st;
                float env = Mathf.Exp(-lt * 3.4f);
                sum += (Sin(notes[i], lt) + Sin(notes[i] * 2f, lt) * 0.34f + Sin(notes[i] * 3f, lt) * 0.16f) * env;
            }
            return sum * 0.3f + Mathf.Sin(2f * Mathf.PI * 4f * t) * Mathf.Exp(-t * 6f) * 0.02f;
        });

        static float[] MinePick() => Render(0.4f, t =>
        {
            float env = Mathf.Exp(-t * 26f);
            float clink = Sin(1180f, t) * 0.4f + Sin(1760f, t) * 0.24f + Sin(2410f, t) * 0.14f;
            float thud = Sin(Mathf.Lerp(180f, 84f, Mathf.Clamp01(t * 12f)), t) * 0.5f;
            float grit = (Mathf.PerlinNoise(t * 5200f, 0.7f) - 0.5f) * 2f * Mathf.Exp(-t * 120f);
            return (clink * 0.7f + thud) * env + grit * 0.7f;
        });

        static float[] MarketChime() => Render(0.9f, t =>
        {
            float a = Sin(1046.5f, t) * Mathf.Exp(-t * 5f);
            float b = t >= 0.18f ? Sin(1396.9f, t - 0.18f) * Mathf.Exp(-(t - 0.18f) * 5f) : 0f;
            float shimmer = Sin(2093f, t) * Mathf.Exp(-t * 11f) * 0.22f;
            return (a + b) * 0.55f + shimmer;
        });

        static float[] Enchant() => Render(1.3f, t =>
        {
            // two detuned sine sweeps rising a fifth apart
            float f1 = Mathf.Lerp(300f, 1250f, Mathf.Sqrt(Mathf.Clamp01(t / 1.3f)));
            float f2 = f1 * 1.5f;
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.3f));
            float body = Sin(f1, t) * 0.45f + Sin(f2, t) * 0.3f + Sin(f1 * 2.01f, t) * 0.16f;
            return body * env;
        });

        static float[] QuestDone() => Render(1.15f, t =>
        {
            float[] notes = { 587.33f, 739.99f, 880f, 1174.66f };
            float sum = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float st = i * 0.11f;
                if (t < st) continue;
                float lt = t - st;
                sum += (Sin(notes[i], lt) + Sin(notes[i] * 2f, lt) * 0.28f) * Mathf.Exp(-lt * 5.5f);
            }
            return sum * 0.42f;
        });

        static float[] Achievement() => Render(1.5f, t =>
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
            float sum = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float st = i * 0.10f;
                if (t < st) continue;
                float lt = t - st;
                sum += (Sin(notes[i], lt) + Sin(notes[i] * 1.5f, lt) * 0.3f + Sin(notes[i] * 2f, lt) * 0.2f)
                       * Mathf.Exp(-lt * 3.6f);
            }
            return sum * 0.34f;
        });

        static float[] Prestige() => Render(2.6f, t =>
        {
            float ramp = Mathf.Clamp01(t / 2.6f);
            float f = Mathf.Lerp(160f, 1400f, ramp * ramp);
            float swell = Mathf.Sin(Mathf.PI * ramp);
            float chord = Sin(f, t) * 0.4f + Sin(f * 1.5f, t) * 0.3f + Sin(f * 2f, t) * 0.2f + Sin(f * 3f, t) * 0.12f;
            float air = (Mathf.PerlinNoise(t * 900f, 1.3f) - 0.5f) * 2f * 0.25f * swell;
            return chord * swell * 0.8f + air;
        });

        static float[] Unlock() => Render(0.42f, t =>
        {
            float click = (Mathf.PerlinNoise(t * 6000f, 2.1f) - 0.5f) * 2f * Mathf.Exp(-t * 90f);
            float f = Mathf.Lerp(420f, 940f, Mathf.Clamp01(t / 0.42f));
            float tone = Sin(f, t) * Mathf.Exp(-t * 9f);
            return click * 0.55f + tone * 0.7f;
        });

        static float[] LevelUp() => Render(0.95f, t =>
        {
            float[] notes = { 659.25f, 830.61f, 987.77f };
            float sum = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float st = i * 0.085f;
                if (t < st) continue;
                float lt = t - st;
                sum += (Sin(notes[i], lt) + Sin(notes[i] * 2f, lt) * 0.25f) * Mathf.Exp(-lt * 8f);
            }
            return sum * 0.5f;
        });

        static float[] Whoosh() => Render(0.55f, t =>
        {
            float p = t / 0.55f;
            float env = Mathf.Sin(Mathf.PI * p);
            float body = Mathf.PerlinNoise(t * (400f + 2600f * p), 3.7f) - 0.5f;
            // a little low-end so it reads as movement rather than hiss
            return body * 2f * env * 0.55f + Sin(Mathf.Lerp(320f, 90f, p), t) * env * 0.16f;
        });

        static float[] Blip() => Render(0.07f, t =>
        {
            // a short rounded tick — the typewriter's voice
            float f = Mathf.Lerp(760f, 540f, Mathf.Clamp01(t / 0.07f));
            float env = Mathf.Exp(-t * 55f);
            return Sin(f, t) * env * 0.5f + Sin(f * 2f, t) * env * 0.14f;
        });

        static float[] EmberWhoosh() => Render(1.4f, t =>
        {
            // opening-cinematic sweep: rising warm rush with a soft crackle tail
            float p = Mathf.Clamp01(t / 1.4f);
            float env = Mathf.Sin(Mathf.PI * p);
            float rush = (Mathf.PerlinNoise(t * (300f + 2400f * p), 5.1f) - 0.5f) * 2f;
            float low = Sin(Mathf.Lerp(140f, 520f, p * p), t) * 0.3f;
            float spark = (Mathf.PerlinNoise(t * 7000f, 8.3f) - 0.5f) * 2f * Mathf.Exp(-t * 8f);
            return (rush * 0.5f + low) * env * 0.8f + spark * 0.4f;
        });

        /// <summary>Seamless forge-bed loop: warm rumble under sparse wrap-around pops.</summary>
        static float[] AmbFire()
        {
            const float seconds = 3.2f;
            var rng = new System.Random(777);
            var pops = new List<(float at, float amp, float decay)>();
            float pt = 0.05f;
            while (pt < seconds)
            {
                pops.Add((pt, 0.10f + (float)rng.NextDouble() * 0.32f, 60f + (float)rng.NextDouble() * 90f));
                pt += 0.05f + (float)rng.NextDouble() * 0.18f;
            }

            int n = (int)(SR * seconds);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float x = i / (float)SR;
                float v = (Mathf.PerlinNoise(x * 11f, 5.1f) - 0.5f) * 0.22f
                        + (Mathf.PerlinNoise(x * 3.1f, 2.3f) - 0.5f) * 0.18f;
                foreach (var p in pops)
                {
                    float d = x - p.at;
                    if (d < 0f) d += seconds;   // tail pops ring into the head
                    if (d < 0.25f)
                        v += p.amp * Mathf.Exp(-d * p.decay)
                           * (Mathf.PerlinNoise(d * 1000f, p.at * 7.3f) - 0.5f) * 2.4f;
                }
                s[i] = Mathf.Clamp(v, -1f, 1f) * 0.8f;
            }

            // Fold the last quarter second into the first so AudioSource.loop wraps clean.
            int xn = (int)(SR * 0.25f);
            for (int i = 0; i < xn; i++)
                s[i] = Mathf.Lerp(s[n - xn + i], s[i], i / (float)xn);
            return s;
        }

        /// <summary>
        /// Deep-forge theme for high smithy tiers: a slow drone under a sparse bell phrase
        /// over a heartbeat pulse. Written as a seamless 16s loop.
        /// </summary>
        static float[] MusicDeep()
        {
            const float seconds = 16f;
            float[] bells = { 220f, 261.63f, 329.63f, 293.66f, 261.63f, 220f, 196f, 164.81f }; // A C E D C A G E
            int n = (int)(SR * seconds);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float x = i / (float)SR;
                // Drone: root + fifth, breathing on a 16s swell so the loop turns imperceptibly.
                float swell = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * x / seconds);
                float v = Sin(110f, x) * 0.14f + Sin(164.81f, x) * 0.10f + Sin(220f, x) * 0.05f;
                v *= swell;
                // Sparse bells, one every two seconds.
                int note = (int)(x / 2f) % bells.Length;
                float lt = x % 2f;
                v += (Sin(bells[note], lt) + Sin(bells[note] * 2f, lt) * 0.35f)
                     * 0.12f * Mathf.Exp(-lt * 2.2f);
                // Heartbeat thump on each second.
                float ht = x % 1f;
                v += Sin(55f + 18f * Mathf.Exp(-ht * 30f), ht) * 0.22f * Mathf.Exp(-ht * 14f);
                s[i] = Mathf.Clamp(v * 0.85f, -1f, 1f);
            }

            // Fold the tail into the head so the loop wraps without a click.
            int xn = (int)(SR * 0.4f);
            for (int i = 0; i < xn; i++)
                s[i] = Mathf.Lerp(s[n - xn + i], s[i], i / (float)xn);
            return s;
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
