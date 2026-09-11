using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>Tiny named-clip player with pitch jitter and a persisted mute toggle.</summary>
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
        public AudioSource sfxSource;

        const string MuteKey = "IB_Muted";
        readonly Dictionary<string, NamedClip> map = new Dictionary<string, NamedClip>();

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

        void Awake()
        {
            Instance = this;
            map.Clear();
            if (clips != null)
                foreach (NamedClip c in clips)
                    if (c != null && !string.IsNullOrEmpty(c.id) && c.clip != null && !map.ContainsKey(c.id))
                        map.Add(c.id, c);
            AudioListener.volume = Muted ? 0f : 1f;
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
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
    }
}
