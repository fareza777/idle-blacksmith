using System;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Idle dungeon expeditions: pick a dungeon, wait real time (it keeps ticking while the
    /// app is closed thanks to UTC timestamps), then claim gold + relic ore.
    /// One expedition at a time.
    /// </summary>
    public class ExpeditionManager : MonoBehaviour
    {
        public event Action OnChanged;

        GameConfig config;

        public string ActiveId { get; private set; } = "";
        public long EndUtcMs { get; private set; }
        public long StartUtcMs { get; private set; }

        public bool HasActive => !string.IsNullOrEmpty(ActiveId) && Active != null;
        public ExpeditionDef Active => config != null ? config.GetExpedition(ActiveId) : null;
        public bool ReadyToClaim => HasActive && NowMs() >= EndUtcMs;
        public float RemainingSeconds => HasActive ? Mathf.Max(0f, (EndUtcMs - NowMs()) / 1000f) : 0f;

        public float Progress01
        {
            get
            {
                ExpeditionDef def = Active;
                if (def == null || def.durationSeconds <= 0) return 0f;
                return Mathf.Clamp01(1f - RemainingSeconds / def.durationSeconds);
            }
        }

        public void Init(GameConfig cfg, SaveData data)
        {
            config = cfg;
            ActiveId = data.expeditionId ?? "";
            EndUtcMs = data.expeditionEndUtcMs;
            // An expedition id saved by an older build that no longer exists is dropped.
            if (!string.IsNullOrEmpty(ActiveId) && config.GetExpedition(ActiveId) == null)
                ActiveId = "";
            StartUtcMs = EndUtcMs - (HasActive ? Active.durationSeconds * 1000L : 0L);
        }

        public bool StartExpedition(string id)
        {
            if (HasActive) return false;
            ExpeditionDef def = config != null ? config.GetExpedition(id) : null;
            if (def == null) return false;
            ActiveId = id;
            StartUtcMs = NowMs();
            EndUtcMs = StartUtcMs + def.durationSeconds * 1000L;
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Collects rewards of a finished expedition. Returns false while it still runs.</summary>
        public bool TryClaim(out ExpeditionDef def)
        {
            def = Active;
            if (def == null || !ReadyToClaim) return false;
            ActiveId = "";
            EndUtcMs = 0;
            OnChanged?.Invoke();
            return true;
        }

        public void WriteTo(SaveData d)
        {
            d.expeditionId = HasActive ? ActiveId : "";
            d.expeditionEndUtcMs = HasActive ? EndUtcMs : 0;
        }

        public static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
