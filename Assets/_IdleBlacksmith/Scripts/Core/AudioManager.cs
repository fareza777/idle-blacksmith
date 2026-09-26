using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Named-clip player with pitch jitter, a persisted mute toggle, and a dedicated
    /// looping music channel that can crossfade between tracks.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [System.Serializable]
        public class NamedClip
        {
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        public NamedClip[] clips;
        public NamedClip[] musicClips;
        public AudioSource sfxSource;
        public AudioSource musicSource;
        public AudioSource ambSource;
        public float musicVolume = 0.55f;

        const string MuteKey = "IB_Muted";
        const string MusicMuteKey = "IB_MusicMuted";
        readonly Dictionary<string, NamedClip> map = new Dictionary<string, NamedClip>();
        string currentMusicId = "";
        string currentAmbId = "";
        Coroutine fader;
        Coroutine ambFader;
        /// <summary>0..1 music duck, decays over ~1.1s. Set by DuckMusic.</summary>
        float duck;

        /// <summary>
        /// Briefly dips the music under a loud moment — fanfares, thunder, prestige.
        /// Implemented as a cap in LateUpdate so it lowers whatever the crossfader wrote
        /// without fighting it, then releases smoothly as the duck decays.
        /// </summary>
        public static void DuckMusic(float amount = 0.5f)
        {
            if (Instance != null) Instance.duck = Mathf.Max(Instance.duck, amount);
        }

        void LateUpdate()
        {
            if (duck <= 0f || musicSource == null) return;
            duck = Mathf.Max(0f, duck - Time.unscaledDeltaTime * 0.9f);
            NamedClip c;
            float peak = map.TryGetValue(currentMusicId, out c) && c.clip != null
                ? musicVolume * c.volume : musicVolume;
            float cap = peak * (1f - duck * 0.6f);
            if (musicSource.volume > cap) musicSource.volume = cap;
        }

        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MuteKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MuteKey, value ? 1 : 0);
                PlayerPrefs.Save();
                AudioListener.volume = value ? 0f : 1f;
            }
        }

        /// <summary>Music channel only — lets the player kill the tune but keep the SFX.</summary>
        public static bool MusicMuted
        {
            get => PlayerPrefs.GetInt(MusicMuteKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MusicMuteKey, value ? 1 : 0);
                PlayerPrefs.Save();
                if (Instance != null && Instance.musicSource != null)
                    Instance.musicSource.mute = value;
            }
        }

        void Awake()
        {
            Instance = this;
            map.Clear();
            if (clips != null)
                foreach (NamedClip c in clips)
                    if (c != null && !string.IsNullOrEmpty(c.id) && c.clip != null && !map.ContainsKey(c.id))
                        map.Add(c.id, c);
            if (musicClips != null)
                foreach (NamedClip c in musicClips)
                    if (c != null && !string.IsNullOrEmpty(c.id) && c.clip != null && !map.ContainsKey(c.id))
                        map.Add(c.id, c);
            AudioListener.volume = Muted ? 0f : 1f;
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
            }
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f;
                musicSource.loop = true;
            }
            musicSource.mute = MusicMuted;
            if (ambSource == null)
            {
                ambSource = gameObject.AddComponent<AudioSource>();
                ambSource.playOnAwake = false;
                ambSource.spatialBlend = 0f;
                ambSource.loop = true;
            }
        }

        public static void Play(string id, float pitchJitter = 0.06f, float volumeScale = 1f)
        {
            if (Instance == null || Muted) return;
            if (!Instance.map.TryGetValue(id, out NamedClip c) || c.clip == null) return;
            AudioSource s = Instance.sfxSource;
            if (s == null) return;
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            s.PlayOneShot(c.clip, c.volume * volumeScale);
        }

        /// <summary>
        /// Switches the looping track. A no-op when the same track is already playing, so
        /// callers can spam "menu music" freely. Fade seconds = full crossfade time.
        /// </summary>
        public static void PlayMusic(string id, float fadeSeconds = 1.2f)
        {
            if (Instance == null || string.IsNullOrEmpty(id) || id == Instance.currentMusicId) return;
            if (!Instance.map.TryGetValue(id, out NamedClip c) || c.clip == null) return;
            Instance.currentMusicId = id;
            if (Instance.fader != null) Instance.StopCoroutine(Instance.fader);
            Instance.fader = Instance.StartCoroutine(Instance.FadeTo(c, fadeSeconds));
        }

        /// <summary>Loops an ambience bed (fire crackle etc.) under everything else.</summary>
        public static void PlayAmbience(string id, float fadeSeconds = 1.5f)
        {
            if (Instance == null || string.IsNullOrEmpty(id) || id == Instance.currentAmbId) return;
            if (!Instance.map.TryGetValue(id, out NamedClip c) || c.clip == null) return;
            Instance.currentAmbId = id;
            if (Instance.ambFader != null) Instance.StopCoroutine(Instance.ambFader);
            Instance.ambFader = Instance.StartCoroutine(Instance.FadeAmbTo(c, fadeSeconds));
        }

        public static void StopMusic(float fadeSeconds = 0.8f)
        {
            if (Instance == null) return;
            Instance.currentMusicId = "";
            if (Instance.fader != null) Instance.StopCoroutine(Instance.fader);
            Instance.fader = Instance.StartCoroutine(Instance.FadeOut(fadeSeconds));
        }

        System.Collections.IEnumerator FadeTo(NamedClip target, float seconds)
        {
            AudioSource src = musicSource;
            float vol = musicVolume * target.volume;
            if (!src.isPlaying || seconds <= 0.01f)
            {
                src.clip = target.clip;
                src.volume = vol;
                // AudioListener.volume handles the master mute — always play so the
                // track is already running when the player unmutes.
                src.Play();
                yield break;
            }
            float t = 0f;
            float startVol = src.volume;
            while (t < seconds * 0.5f)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(startVol, 0f, t / (seconds * 0.5f));
                yield return null;
            }
            src.clip = target.clip;
            src.Play();
            t = 0f;
            while (t < seconds * 0.5f)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(0f, vol, t / (seconds * 0.5f));
                yield return null;
            }
            src.volume = vol;
            fader = null;
        }

        System.Collections.IEnumerator FadeOut(float seconds)
        {
            AudioSource src = musicSource;
            float startVol = src.volume;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(startVol, 0f, t / seconds);
                yield return null;
            }
            src.Stop();
            src.volume = musicVolume;
            fader = null;
        }

        System.Collections.IEnumerator FadeAmbTo(NamedClip target, float seconds)
        {
            AudioSource src = ambSource;
            src.clip = target.clip;
            src.volume = 0f;
            src.Play();
            float t = 0f;
            float vol = target.volume;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(0f, vol, t / seconds);
                yield return null;
            }
            src.volume = vol;
            ambFader = null;
        }
    }
}
