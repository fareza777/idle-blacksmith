using System;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Rekindling the forge: burns the current run for ember shards, which buy permanent
    /// talents. Buildings, gold, upgrades and runes reset; talents, quests, achievements and
    /// lifetime stats carry over.
    /// </summary>
    public class PrestigeManager : MonoBehaviour
    {
        public event Action OnChanged;

        GameConfig config;

        public void Init(GameConfig cfg) => config = cfg;

        public int Shards
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return gm != null && gm.Data != null ? gm.Data.emberShards : 0;
            }
        }

        public int Count
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return gm != null && gm.Data != null ? gm.Data.prestigeCount : 0;
            }
        }

        public long RunEarned
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return gm != null && gm.Data != null ? gm.Data.runEarned : 0L;
            }
        }

        /// <summary>Shards the next rekindle would award right now.</summary>
        public int PendingShards
        {
            get
            {
                if (config == null || config.prestigeGoldDivisor <= 0f) return 0;
                double runs = RunEarned / config.prestigeGoldDivisor;
                if (runs < 1.0) return 0;
                double shards = config.prestigeShardScale * Math.Pow(runs, config.prestigeExponent);
                return Mathf.Max(0, Mathf.FloorToInt((float)shards));
            }
        }

        public bool CanPrestige => PendingShards > 0;

        /// <summary>Gold still needed before the next shard is earned — drives the UI progress bar.</summary>
        public long GoldToNextShard
        {
            get
            {
                if (config == null || config.prestigeGoldDivisor <= 0f) return 0;
                long threshold = (long)config.prestigeGoldDivisor;
                if (RunEarned >= threshold) return 0;
                return threshold - RunEarned;
            }
        }

        public bool SpendShards(int amount)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Data == null || amount <= 0) return false;
            if (gm.Data.emberShards < amount) return false;
            gm.Data.emberShards -= amount;
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Burns the run. Returns the shards granted, or 0 when it could not happen.</summary>
        public int DoPrestige()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !CanPrestige) return 0;

            int gained = PendingShards;
            gm.Data.emberShards += gained;
            gm.Data.prestigeCount++;
            gm.Data.runEarned = 0;
            if (gm.Data.stats != null) gm.Data.stats.prestiges++;

            gm.ResetRun();
            OnChanged?.Invoke();
            return gained;
        }
    }
}
