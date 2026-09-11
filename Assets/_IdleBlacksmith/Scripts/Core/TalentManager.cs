using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Permanent ember-shard upgrades. Talents survive prestige, which is what makes
    /// rekindling the forge worth doing.
    /// </summary>
    public class TalentManager : MonoBehaviour, IProductionModifier
    {
        public event Action OnChanged;

        GameConfig config;
        readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        public void Init(GameConfig cfg, SaveData data)
        {
            config = cfg;
            levels.Clear();

            if (data != null && data.talents != null)
                foreach (TalentState s in data.talents)
                    if (s != null && !string.IsNullOrEmpty(s.id))
                        levels[s.id] = Mathf.Max(0, s.level);

            if (cfg != null && cfg.talents != null)
                foreach (TalentDef def in cfg.talents)
                    if (def != null && !levels.ContainsKey(def.id))
                        levels[def.id] = 0;
        }

        public int GetLevel(string id) => levels.TryGetValue(id, out int l) ? l : 0;

        public bool IsMaxed(TalentDef def) => def == null || GetLevel(def.id) >= def.maxLevel;

        public int GetCost(TalentDef def)
        {
            if (def == null) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(def.baseCost * Mathf.Pow(def.costGrowth, GetLevel(def.id))));
        }

        public bool CanBuy(TalentDef def, int shards) => !IsMaxed(def) && shards >= GetCost(def);

        public bool TryBuy(TalentDef def, GameManager gm)
        {
            if (def == null || gm == null || gm.prestige == null) return false;
            if (!CanBuy(def, gm.prestige.Shards)) return false;
            if (!gm.prestige.SpendShards(GetCost(def))) return false;

            levels[def.id] = GetLevel(def.id) + 1;
            OnChanged?.Invoke();
            if (gm.production != null) gm.production.Recalculate();
            gm.Save();
            return true;
        }

        public void Contribute(GameConfig c)
        {
            if (c == null || c.talents == null) return;
            foreach (TalentDef def in c.talents)
            {
                if (def == null) continue;
                int lvl = GetLevel(def.id);
                if (lvl <= 0) continue;
                float amount = def.effectPerLevel * lvl;

                switch (def.kind)
                {
                    case TalentKind.GoldBonus:
                        Production.GoldMult *= 1f + amount;
                        break;
                    case TalentKind.OreRate:
                        Production.OreRateMult *= 1f + amount;
                        break;
                    case TalentKind.CraftSpeed:
                        Production.CraftSpeedMult *= 1f / (1f + amount);
                        break;
                    case TalentKind.PriceBonus:
                        Production.PriceMult *= 1f + amount;
                        break;
                    case TalentKind.Luck:
                        Production.LuckBonus += amount;
                        break;
                    case TalentKind.OfflineRate:
                        Production.OfflineRate += amount;
                        break;
                    case TalentKind.CustomerRate:
                        Production.CustomerIntervalMult *= 1f / (1f + amount);
                        break;
                    case TalentKind.OreCapacity:
                        Production.OreCapacityBonus += Mathf.RoundToInt(amount);
                        break;
                    case TalentKind.ExpeditionSpeed:
                        Production.ExpeditionSpeedMult *= 1f / (1f + amount);
                        break;
                    case TalentKind.RunePower:
                        Production.RunePower *= 1f + amount;
                        break;
                }
            }
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

        public void WriteTo(SaveData d)
        {
            d.talents = new List<TalentState>();
            foreach (KeyValuePair<string, int> kv in levels)
                d.talents.Add(new TalentState { id = kv.Key, level = kv.Value });
        }
    }
}
