using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Lifetime milestones. Unlocking one grants a small permanent bonus that feeds straight
    /// into the Production bag, so achievements are part of the build rather than just badges.
    /// </summary>
    public class AchievementManager : MonoBehaviour, IProductionModifier
    {
        /// <summary>Fired once per achievement the moment it unlocks.</summary>
        public event Action<AchievementDef> OnUnlocked;

        public event Action OnChanged;

        GameConfig config;
        readonly List<AchievementDef> justUnlocked = new List<AchievementDef>();

        public void Init(GameConfig cfg, SaveData data)
        {
            config = cfg;
            Refresh();
        }

        public IReadOnlyList<AchievementDef> All
            => config != null && config.achievements != null ? config.achievements : Array.Empty<AchievementDef>();

        public bool IsUnlocked(string id)
        {
            GameManager gm = GameManager.Instance;
            return gm != null && gm.Data != null && gm.Data.achievementsUnlocked != null
                && gm.Data.achievementsUnlocked.Contains(id);
        }

        public int UnlockedCount
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return gm != null && gm.Data != null && gm.Data.achievementsUnlocked != null
                    ? gm.Data.achievementsUnlocked.Count : 0;
            }
        }

        public int TotalCount => All.Count;

        /// <summary>Checks every achievement and unlocks any whose goal is now met.</summary>
        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Data == null || config == null || config.achievements == null) return;
            if (gm.Data.achievementsUnlocked == null) gm.Data.achievementsUnlocked = new List<string>();

            justUnlocked.Clear();
            foreach (AchievementDef a in config.achievements)
            {
                if (a == null || IsUnlocked(a.id)) continue;
                if (Goals.Progress(a.goal, a.targetId) < a.target) continue;
                gm.Data.achievementsUnlocked.Add(a.id);
                justUnlocked.Add(a);
            }

            if (justUnlocked.Count == 0) return;

            // Bonuses change, so the Production bag has to be rebuilt before we announce.
            if (gm.production != null) gm.production.Recalculate();
            foreach (AchievementDef a in justUnlocked) OnUnlocked?.Invoke(a);
            OnChanged?.Invoke();
            gm.Save();
        }

        public void Contribute(GameConfig c)
        {
            if (c == null || c.achievements == null) return;
            foreach (AchievementDef a in c.achievements)
            {
                if (a == null || !IsUnlocked(a.id)) continue;
                switch (a.bonusKind)
                {
                    case AchBonus.Gold:
                        Production.GoldMult *= 1f + a.bonus;
                        break;
                    case AchBonus.Ore:
                        Production.OreRateMult *= 1f + a.bonus;
                        break;
                    case AchBonus.Price:
                        Production.PriceMult *= 1f + a.bonus;
                        break;
                    case AchBonus.Craft:
                        Production.CraftSpeedMult *= 1f / (1f + a.bonus);
                        break;
                    case AchBonus.Luck:
                        Production.LuckBonus += a.bonus;
                        break;
                    case AchBonus.Offline:
                        Production.OfflineRate += a.bonus;
                        break;
                }
            }
        }
    }
}
