using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Gold-priced upgrade tracks. Levels live in a dictionary keyed by UpgradeDef.id, so
    /// adding a track to GameConfig.upgrades is all that is needed to ship a new upgrade.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        /// <summary>(upgradeId, newLevel)</summary>
        public event Action<string, int> OnUpgradeChanged;

        GameConfig config;
        readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        public void Init(GameConfig cfg, SaveData data)
        {
            config = cfg;
            levels.Clear();

            if (data != null && data.upgradeList != null)
                foreach (UpgradeState s in data.upgradeList)
                    if (s != null && !string.IsNullOrEmpty(s.id))
                        levels[s.id] = Mathf.Max(0, s.level);

            if (cfg != null && cfg.upgrades != null)
                foreach (UpgradeDef def in cfg.upgrades)
                    if (def != null && !string.IsNullOrEmpty(def.id) && !levels.ContainsKey(def.id))
                        levels[def.id] = 0;
        }

        public int GetLevel(string id) => levels.TryGetValue(id, out int l) ? l : 0;

        public int GetCost(UpgradeDef def)
        {
            if (def == null) return 0;
            int lvl = GetLevel(def.id);
            float raw = def.baseCost * Mathf.Pow(def.costGrowth, lvl);
            return Mathf.Max(5, Mathf.RoundToInt(raw / 5f) * 5);
        }

        public bool IsMaxed(UpgradeDef def) => def == null || GetLevel(def.id) >= def.maxLevel;

        public bool CanAfford(UpgradeDef def, int gold) => !IsMaxed(def) && gold >= GetCost(def);

        public bool TryBuy(UpgradeDef def, EconomyManager econ)
        {
            if (def == null || econ == null || !CanAfford(def, econ.Gold)) return false;
            econ.Spend(GetCost(def));
            int newLevel = GetLevel(def.id) + 1;
            levels[def.id] = newLevel;
            OnUpgradeChanged?.Invoke(def.id, newLevel);
            return true;
        }

        /// <summary>Drops every track back to zero — used by prestige.</summary>
        public void ResetAll()
        {
            if (config == null || config.upgrades == null) return;
            foreach (UpgradeDef def in config.upgrades)
                if (def != null && levels.ContainsKey(def.id))
                    levels[def.id] = 0;
            foreach (UpgradeDef def in config.upgrades)
                if (def != null) OnUpgradeChanged?.Invoke(def.id, 0);
        }

        // ------------------------------------------------------------ effects

        public float CraftDuration(GameConfig c, RecipeDef recipe = null)
        {
            float baseDuration = recipe != null ? recipe.craftDuration : c.baseCraftDuration;
            float t = baseDuration * Mathf.Pow(0.90f, GetLevel(UpgradeId.Craft)) * Production.CraftSpeedMult;
            return Mathf.Max(c.minCraftDuration * 0.5f, t);
        }

        public float MoveSpeed(GameConfig c)
            => c.baseMoveSpeed * (1f + 0.12f * GetLevel(UpgradeId.Carry));

        public int RackCapacity(GameConfig c)
            => c.baseRackCapacity + 2 * GetLevel(UpgradeId.Rack);

        /// <summary>Upgrade contribution only; buildings, runes and talents add through Production.</summary>
        public int OreCapacity(GameConfig c)
            => 12 * GetLevel(UpgradeId.OreCap);

        /// <summary>Luck drives the rarity roll: every point tilts the curve toward Legendary.</summary>
        public float Luck => 0.25f * GetLevel(UpgradeId.Luck) + Production.LuckBonus;

        /// <summary>Multiplier on the customer arrival interval (below 1 = busier shop).</summary>
        public float CustomerIntervalMult
            => 1f / (1f + 0.10f * GetLevel(UpgradeId.Charm)) * Production.CustomerIntervalMult;

        public void WriteTo(SaveData d)
        {
            d.upgradeList = new List<UpgradeState>();
            if (config != null && config.upgrades != null)
            {
                foreach (UpgradeDef def in config.upgrades)
                    if (def != null)
                        d.upgradeList.Add(new UpgradeState { id = def.id, level = GetLevel(def.id) });
            }
            else
            {
                foreach (KeyValuePair<string, int> kv in levels)
                    d.upgradeList.Add(new UpgradeState { id = kv.Key, level = kv.Value });
            }
        }
    }
}
