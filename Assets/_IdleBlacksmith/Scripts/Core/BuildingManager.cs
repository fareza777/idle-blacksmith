using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// The forge complex: five buildings, each upgradable from 0 (unbuilt) to its max level.
    /// Levels are keyed by BuildingDef.id so shipping a new building is a config-only change.
    ///
    /// Cost indexing: levelCosts[level - startLevel], i.e. the cost of the *next* level.
    /// The Smithy starts at level 1, so its first entry is the 1 -> 2 price.
    /// </summary>
    public class BuildingManager : MonoBehaviour, IProductionModifier
    {
        /// <summary>(buildingId, newLevel)</summary>
        public event Action<string, int> OnBuildingChanged;

        GameConfig config;
        readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        /// <summary>
        /// Config is normally injected by Init, but editor tooling reads these accessors before
        /// Awake has run, so fall back to the GameManager's config when ours is still unset.
        /// </summary>
        GameConfig Config
        {
            get
            {
                if (config != null) return config;
                GameManager gm = GameManager.Instance;
                if (gm != null && gm.config != null) config = gm.config;
                return config;
            }
        }

        public void Init(GameConfig cfg, SaveData data)
        {
            config = cfg;
            levels.Clear();

            if (data != null && data.buildings != null)
                foreach (BuildingState s in data.buildings)
                    if (s != null && !string.IsNullOrEmpty(s.id))
                        levels[s.id] = Mathf.Max(0, s.level);

            if (cfg != null && cfg.buildings != null)
                foreach (BuildingDef def in cfg.buildings)
                    if (def != null && !string.IsNullOrEmpty(def.id) && !levels.ContainsKey(def.id))
                        levels[def.id] = Mathf.Max(0, def.startLevel);
        }

        public int GetLevel(string id) => levels.TryGetValue(id, out int l) ? l : 0;

        public bool IsBuilt(string id) => GetLevel(id) > 0;

        public BuildingDef Def(string id) => Config != null ? Config.GetBuilding(id) : null;

        /// <summary>Cost of the next level, or 0 when the building is at its cap.</summary>
        public int GetCost(string id)
        {
            BuildingDef def = Def(id);
            if (def == null || def.levelCosts == null) return 0;
            int idx = GetLevel(id) - def.startLevel;
            if (idx < 0 || idx >= def.levelCosts.Length) return 0;
            return def.levelCosts[idx];
        }

        public bool IsMaxed(string id)
        {
            BuildingDef def = Def(id);
            if (def == null) return true;
            return GetLevel(id) >= def.startLevel + (def.levelCosts != null ? def.levelCosts.Length : 0);
        }

        public bool CanUpgrade(string id, int gold) => !IsMaxed(id) && gold >= GetCost(id);

        public bool TryUpgrade(string id, EconomyManager econ)
        {
            if (econ == null || IsMaxed(id)) return false;
            int cost = GetCost(id);
            if (!econ.Spend(cost)) return false;

            int newLevel = GetLevel(id) + 1;
            levels[id] = newLevel;
            OnBuildingChanged?.Invoke(id, newLevel);
            return true;
        }

        /// <summary>Puts every building back to its starting level — used by prestige.</summary>
        public void ResetToStart()
        {
            if (Config == null || Config.buildings == null) return;
            foreach (BuildingDef def in Config.buildings)
            {
                if (def == null) continue;
                levels[def.id] = Mathf.Max(0, def.startLevel);
                OnBuildingChanged?.Invoke(def.id, levels[def.id]);
            }
        }

        /// <summary>Applies a level directly. Only for editor tooling and tests.</summary>
        public void ForceLevel(string id, int level)
        {
            levels[id] = Mathf.Max(0, level);
            OnBuildingChanged?.Invoke(id, levels[id]);
        }

        // ------------------------------------------------------------ effects

        /// <summary>Sale-price multiplier contributed by the Smithy and Trading Post.</summary>
        public float PriceMult
        {
            get
            {
                float mult = 1f;
                if (Config == null || Config.buildings == null) return mult;
                foreach (BuildingDef def in config.buildings)
                    if (def != null && def.priceBonus != 0f)
                        mult *= 1f + def.priceBonus * Mathf.Max(0, GetLevel(def.id) - Mathf.Max(1, def.startLevel));
                return mult;
            }
        }

        /// <summary>Rack slots contributed by the Smithy.</summary>
        public int RackBonus
        {
            get
            {
                BuildingDef def = Def(BuildingId.Smithy);
                if (def == null || def.rackBonus == 0) return 0;
                return def.rackBonus * Mathf.Max(0, GetLevel(BuildingId.Smithy) - Mathf.Max(1, def.startLevel));
            }
        }

        public float OrePerSecond
        {
            get
            {
                BuildingDef def = Def(BuildingId.Mine);
                if (def == null) return 0f;
                return def.orePerSecond * GetLevel(BuildingId.Mine);
            }
        }

        public int OreCapacity
        {
            get
            {
                BuildingDef def = Def(BuildingId.Mine);
                if (def == null) return 0;
                return def.oreCapacity * GetLevel(BuildingId.Mine);
            }
        }

        /// <summary>Multiplier on the gap between customers (below 1 = busier).</summary>
        public float CustomerIntervalMult
        {
            get
            {
                float mult = 1f;
                if (Config == null || Config.buildings == null) return mult;
                foreach (BuildingDef def in config.buildings)
                {
                    if (def == null || def.customerIntervalCut == 0f) continue;
                    int tiers = Mathf.Max(0, GetLevel(def.id) - Mathf.Max(1, def.startLevel));
                    mult *= Mathf.Max(0.15f, 1f - def.customerIntervalCut * tiers);
                }
                return mult;
            }
        }

        public int ExpeditionSlots
        {
            get
            {
                BuildingDef def = Def(BuildingId.Gate);
                if (def == null) return 1;
                int tiers = Mathf.Max(0, GetLevel(BuildingId.Gate) - Mathf.Max(1, def.startLevel));
                return 1 + def.expeditionSlots * tiers;
            }
        }

        public float DungeonRewardMult
        {
            get
            {
                BuildingDef def = Def(BuildingId.Gate);
                if (def == null) return 1f;
                int tiers = Mathf.Max(0, GetLevel(BuildingId.Gate) - Mathf.Max(1, def.startLevel));
                return 1f + def.dungeonRewardBonus * tiers;
            }
        }

        public int RuneMaxLevel
        {
            get
            {
                BuildingDef def = Def(BuildingId.Sanctum);
                if (def == null) return 4;
                return 4 + def.runeLevelsPerTier * GetLevel(BuildingId.Sanctum);
            }
        }

        public float RuneCostMult
        {
            get
            {
                BuildingDef def = Def(BuildingId.Sanctum);
                if (def == null) return 1f;
                int tiers = Mathf.Max(0, GetLevel(BuildingId.Sanctum) - Mathf.Max(1, def.startLevel));
                return Mathf.Max(0.25f, 1f - def.runeCostCut * tiers);
            }
        }

        public void Contribute(GameConfig c)
        {
            Production.PriceMult *= PriceMult;
            Production.CustomerIntervalMult *= CustomerIntervalMult;
            Production.OrePerSecondFlat += OrePerSecond;
            Production.OreCapacityBonus += OreCapacity;
            Production.ExpeditionSlots = Mathf.Max(Production.ExpeditionSlots, ExpeditionSlots);
            Production.DungeonRewardMult *= DungeonRewardMult;
            Production.RuneMaxLevel = Mathf.Max(Production.RuneMaxLevel, RuneMaxLevel);
            Production.RuneCostMult *= RuneCostMult;
        }

        public void WriteTo(SaveData d)
        {
            d.buildings = new List<BuildingState>();
            if (config != null && config.buildings != null)
            {
                foreach (BuildingDef def in config.buildings)
                    if (def != null)
                        d.buildings.Add(new BuildingState { id = def.id, level = GetLevel(def.id) });
            }
            else
            {
                foreach (KeyValuePair<string, int> kv in levels)
                    d.buildings.Add(new BuildingState { id = kv.Key, level = kv.Value });
            }
        }
    }
}
