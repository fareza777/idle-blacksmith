using System;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// The ore economy. Ore accrues continuously from the blacksmith's own prospecting plus
    /// the mine, and every forge spends it. Because the base rate is always positive the
    /// loop can slow down but can never deadlock.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        /// <summary>(ore, capacity)</summary>
        public event Action<int, int> OnOreChanged;

        GameConfig config;
        UpgradeManager upgrades;

        /// <summary>Fractional ore carried between frames so slow rates still accrue.</summary>
        float pending;

        public int Ore { get; private set; }
        public UpgradeManager Upgrades { set => upgrades = value; }

        public int OreCapacity
        {
            get
            {
                int baseCap = config != null ? config.baseOreCapacity : 40;
                int fromUpgrades = (upgrades != null && config != null) ? upgrades.OreCapacity(config) : 0;
                return Mathf.Max(1, baseCap + fromUpgrades + Production.OreCapacityBonus);
            }
        }

        public float OrePerSecond
        {
            get
            {
                float baseRate = config != null ? config.baseOrePerSecond : 0.45f;
                return (baseRate + Production.OrePerSecondFlat) * Production.OreRateMult;
            }
        }

        public bool IsFull => Ore >= OreCapacity;

        public void Init(GameConfig cfg, SaveData data)
        {
            config = cfg;
            pending = 0f;
            Ore = data != null ? Mathf.Max(0, data.ore) : 0;
            Notify();
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            int cap = OreCapacity;
            if (Ore >= cap) return;

            pending += OrePerSecond * dt;
            if (pending < 1f) return;

            int gained = Mathf.FloorToInt(pending);
            pending -= gained;
            Add(gained);
        }

        public void Add(int amount)
        {
            if (amount == 0) return;
            Ore = Mathf.Clamp(Ore + amount, 0, OreCapacity);
            Notify();
        }

        public bool CanAfford(int amount) => Ore >= amount;

        public bool TryConsume(int amount)
        {
            if (amount <= 0) return true;
            if (Ore < amount) return false;
            Ore -= amount;
            Notify();
            return true;
        }

        /// <summary>Drops the stock back to zero — used by prestige, which also clears the mine.</summary>
        public void ResetTo(int amount)
        {
            pending = 0f;
            Ore = Mathf.Clamp(amount, 0, OreCapacity);
            Notify();
        }

        /// <summary>Offline payout: adds up to <paramref name="amount"/> ore, clamped to free
        /// stock space, and returns what actually fit. The caller computes the amount with
        /// every multiplier (offline rate, storehouse) already folded in.</summary>
        public int GrantOffline(int amount)
        {
            if (amount <= 0) return 0;
            int space = Mathf.Max(0, OreCapacity - Ore);
            int gained = Mathf.Min(amount, space);
            if (gained <= 0) return 0;
            Ore += gained;
            Notify();
            return gained;
        }

        public void WriteTo(SaveData d) => d.ore = Ore;

        void Notify() => OnOreChanged?.Invoke(Ore, OreCapacity);

        /// <summary>Call after anything changes the capacity so the HUD re-reads it.</summary>
        public void Refresh() => Notify();
    }
}
