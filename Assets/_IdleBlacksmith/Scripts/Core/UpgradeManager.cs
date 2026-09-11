using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    public class UpgradeManager : MonoBehaviour
    {
        public const string CraftId = "craft";
        public const string CarryId = "carry";
        public const string RackId = "rack";

        /// <summary>(upgradeId, newLevel)</summary>
        public event Action<string, int> OnUpgradeChanged;

        GameConfig config;
        readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        public void Init(GameConfig cfg, SaveData data)
        {
            config = cfg;
            levels[CraftId] = Mathf.Max(0, data.craftLevel);
            levels[CarryId] = Mathf.Max(0, data.carryLevel);
            levels[RackId] = Mathf.Max(0, data.rackLevel);
        }

        public int GetLevel(string id) => levels.TryGetValue(id, out int l) ? l : 0;

        public int GetCost(UpgradeDef def)
        {
            int lvl = GetLevel(def.id);
            float raw = def.baseCost * Mathf.Pow(def.costGrowth, lvl);
            return Mathf.Max(5, Mathf.RoundToInt(raw / 5f) * 5);
        }

        public bool IsMaxed(UpgradeDef def) => GetLevel(def.id) >= def.maxLevel;

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

        public float CraftDuration(GameConfig c)
            => Mathf.Max(c.minCraftDuration, c.baseCraftDuration * Mathf.Pow(0.90f, GetLevel(CraftId)));

        public float MoveSpeed(GameConfig c)
            => c.baseMoveSpeed * (1f + 0.12f * GetLevel(CarryId));

        public int RackCapacity(GameConfig c)
            => c.baseRackCapacity + 2 * GetLevel(RackId);

        public void WriteTo(SaveData d)
        {
            d.craftLevel = GetLevel(CraftId);
            d.carryLevel = GetLevel(CarryId);
            d.rackLevel = GetLevel(RackId);
        }
    }
}
