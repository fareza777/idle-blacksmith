using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Dungeon expeditions. Several can run at once once the Dungeon Gate is upgraded, and
    /// every end time is wall-clock UTC so runs finish while the app is closed.
    /// </summary>
    public class ExpeditionManager : MonoBehaviour
    {
        public event Action OnChanged;

        /// <summary>One running expedition.</summary>
        public class Slot
        {
            public string id;
            public long startUtcMs;
            public long endUtcMs;
        }

        GameConfig config;
        readonly List<Slot> slots = new List<Slot>();

        public IReadOnlyList<Slot> Slots => slots;

        /// <summary>How many expeditions can run at once — driven by the Dungeon Gate.</summary>
        public int Capacity => Mathf.Max(1, Production.ExpeditionSlots);

        public int RunningCount => slots.Count;
        public bool HasActive => slots.Count > 0;
        public bool HasFreeSlot => slots.Count < Capacity;

        public int ReadyCount
        {
            get
            {
                long now = NowMs();
                int n = 0;
                foreach (Slot s in slots) if (now >= s.endUtcMs) n++;
                return n;
            }
        }

        public bool ReadyToClaim => ReadyCount > 0;

        public Slot SlotOf(string id)
        {
            foreach (Slot s in slots) if (s.id == id) return s;
            return null;
        }

        public int CountOf(string id)
        {
            int n = 0;
            foreach (Slot s in slots) if (s.id == id) n++;
            return n;
        }

        public bool IsRunning(string id) => SlotOf(id) != null;

        public bool IsReady(string id)
        {
            Slot s = SlotOf(id);
            return s != null && NowMs() >= s.endUtcMs;
        }

        public ExpeditionDef DefinitionOf(string id) => config != null ? config.GetExpedition(id) : null;

        public float RemainingOf(string id)
        {
            Slot s = SlotOf(id);
            if (s == null) return 0f;
            return Mathf.Max(0f, (s.endUtcMs - NowMs()) / 1000f);
        }

        public float ProgressOf(string id)
        {
            Slot s = SlotOf(id);
            if (s == null) return 0f;
            long span = s.endUtcMs - s.startUtcMs;
            if (span <= 0) return 1f;
            return Mathf.Clamp01(1f - (s.endUtcMs - NowMs()) / (float)span);
        }

        /// <summary>True when the Dungeon Gate is high enough for this expedition.</summary>
        public bool IsUnlocked(ExpeditionDef def)
        {
            if (def == null) return false;
            GameManager gm = GameManager.Instance;
            int gate = gm != null && gm.buildings != null ? gm.buildings.GetLevel(BuildingId.Gate) : 0;
            return gate >= def.requiredGateLevel;
        }

        /// <summary>Wall-clock duration after talent and rune adjustments.</summary>
        public float EffectiveDuration(ExpeditionDef def)
            => def == null ? 0f : def.durationSeconds * Mathf.Max(0.1f, Production.ExpeditionSpeedMult);

        public void Init(GameConfig cfg, SaveData data)
        {
            config = cfg;
            slots.Clear();
            if (data == null || data.expeditions == null) return;

            foreach (ExpeditionState s in data.expeditions)
            {
                if (s == null || string.IsNullOrEmpty(s.id)) continue;
                if (config != null && config.GetExpedition(s.id) == null) continue;   // dropped from config
                slots.Add(new Slot
                {
                    id = s.id,
                    startUtcMs = s.startUtcMs > 0 ? s.startUtcMs : s.endUtcMs - 1000,
                    endUtcMs = s.endUtcMs,
                });
            }
        }

        public bool StartExpedition(string id)
        {
            ExpeditionDef def = DefinitionOf(id);
            if (def == null || !IsUnlocked(def) || !HasFreeSlot) return false;

            long now = NowMs();
            slots.Add(new Slot
            {
                id = id,
                startUtcMs = now,
                endUtcMs = now + (long)(EffectiveDuration(def) * 1000f),
            });
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Takes the finished expedition with the earliest end time.</summary>
        public bool TryClaim(out ExpeditionDef def)
        {
            def = null;
            long now = NowMs();
            Slot best = null;
            foreach (Slot s in slots)
            {
                if (now < s.endUtcMs) continue;
                if (best == null || s.endUtcMs < best.endUtcMs) best = s;
            }
            if (best == null) return false;

            def = DefinitionOf(best.id);
            slots.Remove(best);
            OnChanged?.Invoke();
            return def != null;
        }

        public void ClearAll()
        {
            if (slots.Count == 0) return;
            slots.Clear();
            OnChanged?.Invoke();
        }

        public void WriteTo(SaveData d)
        {
            d.expeditions = new List<ExpeditionState>();
            foreach (Slot s in slots)
                d.expeditions.Add(new ExpeditionState { id = s.id, startUtcMs = s.startUtcMs, endUtcMs = s.endUtcMs });
        }

        public static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
