using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Relic-ore runes bought at the Sanctum. Runes tilt the forge's core numbers, and the
    /// Sanctum's level caps how far each can go.
    /// </summary>
    public class RuneManager : MonoBehaviour, IProductionModifier
    {
        public event Action OnChanged;

        GameConfig config;
        BuildingManager buildings;
        readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        public void Init(GameConfig cfg, BuildingManager buildingManager, SaveData data)
        {
            config = cfg;
            buildings = buildingManager;
            levels.Clear();

            if (data != null && data.runes != null)
                foreach (RuneState s in data.runes)
                    if (s != null && !string.IsNullOrEmpty(s.id))
                        levels[s.id] = Mathf.Max(0, s.level);

            if (cfg != null && cfg.runes != null)
                foreach (RuneDef def in cfg.runes)
                    if (def != null && !levels.ContainsKey(def.id))
                        levels[def.id] = 0;
        }

        public int GetLevel(string id) => levels.TryGetValue(id, out int l) ? l : 0;

        /// <summary>Level cap across all runes, driven by the Sanctum building.</summary>
        public int MaxLevel => buildings != null ? buildings.RuneMaxLevel : 0;

        public bool IsMaxed(RuneDef def) => def == null || GetLevel(def.id) >= MaxLevel;

        /// <summary>Cost in relic ore, shaped by the rune and the Sanctum's discount.</summary>
        public int GetCost(RuneDef def)
        {
            if (def == null) return 0;
            float raw = def.baseCost * Mathf.Pow(def.costGrowth, GetLevel(def.id));
            return Mathf.Max(1, Mathf.RoundToInt(raw * Production.RuneCostMult));
        }

        public bool CanBuy(RuneDef def, int relicOre) => !IsMaxed(def) && relicOre >= GetCost(def);

        public bool TryBuy(RuneDef def, GameManager gm)
        {
            if (def == null || gm == null || !CanBuy(def, gm.RelicOre)) return false;
            gm.SpendRelicOre(GetCost(def));
            levels[def.id] = GetLevel(def.id) + 1;
            OnChanged?.Invoke();
            if (gm.production != null) gm.production.Recalculate();
            gm.Save();
            return true;
        }

        public int TotalLevels
        {
            get
            {
                int n = 0;
                foreach (KeyValuePair<string, int> kv in levels) n += kv.Value;
                return n;
            }
        }

        public void ResetAll()
        {
            foreach (string id in new List<string>(levels.Keys)) levels[id] = 0;
            OnChanged?.Invoke();
        }

        public void Contribute(GameConfig c)
        {
            float power = Mathf.Max(0.01f, Production.RunePower);

            float flame = Scaled(RuneId.Flame, power);
            if (flame > 0f) Production.PriceMult *= 1f + flame;

            float haste = Scaled(RuneId.Haste, power);
            if (haste > 0f) Production.CraftSpeedMult *= 1f / (1f + haste);

            float fortune = Scaled(RuneId.Fortune, power);
            if (fortune > 0f) Production.LuckBonus += fortune;

            float wealth = Scaled(RuneId.Wealth, power);
            if (wealth > 0f) Production.OreRateMult *= 1f + wealth;
        }

        /// <summary>Effect total for one rune, already multiplied by rune power.</summary>
        float Scaled(string id, float power)
        {
            RuneDef def = config != null ? config.GetRune(id) : null;
            if (def == null) return 0f;
            int lvl = GetLevel(id);
            if (lvl <= 0) return 0f;
            return def.effectPerLevel * lvl * power;
        }

        public void WriteTo(SaveData d)
        {
            d.runes = new List<RuneState>();
            foreach (KeyValuePair<string, int> kv in levels)
                d.runes.Add(new RuneState { id = kv.Key, level = kv.Value });
        }
    }
}
