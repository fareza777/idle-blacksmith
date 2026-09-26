using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// The single place where every global bonus lives. Systems read these multipliers
    /// instead of asking each manager directly, which keeps buildings, runes, talents and
    /// achievements from having to know about each other.
    ///
    /// Nothing writes these fields except ProductionManager.Recalculate — everything else
    /// only reads them.
    /// </summary>
    public static class Production
    {
        /// <summary>Multiplies the gold a customer pays.</summary>
        public static float GoldMult = 1f;
        /// <summary>Multiplies sword sale prices (stacks with GoldMult).</summary>
        public static float PriceMult = 1f;
        /// <summary>Multiplies ore mined per second.</summary>
        public static float OreRateMult = 1f;
        /// <summary>Multiplies craft duration; below 1 means faster forging.</summary>
        public static float CraftSpeedMult = 1f;
        /// <summary>Multiplies the gap between customers; below 1 means a busier shop.</summary>
        public static float CustomerIntervalMult = 1f;
        /// <summary>Multiplies expedition duration; below 1 means shorter runs.</summary>
        public static float ExpeditionSpeedMult = 1f;
        /// <summary>Multiplies expedition gold and relic rewards.</summary>
        public static float DungeonRewardMult = 1f;
        /// <summary>Multiplies every rune effect.</summary>
        public static float RunePower = 1f;

        /// <summary>Flat ore storage added on top of the base capacity and upgrades.</summary>
        public static int OreCapacityBonus;
        /// <summary>Flat ore per second added on top of the base rate and upgrades.</summary>
        public static float OrePerSecondFlat;
        /// <summary>How many expeditions can run at once.</summary>
        public static int ExpeditionSlots = 1;
        /// <summary>Highest rune level reachable, driven by the Sanctum.</summary>
        public static int RuneMaxLevel = 4;
        /// <summary>Multiplies rune costs; below 1 means cheaper.</summary>
        public static float RuneCostMult = 1f;
        /// <summary>Flat luck added to the rarity roll.</summary>
        public static float LuckBonus;
        /// <summary>Fraction of income earned while the app was closed.</summary>
        public static float OfflineRate = 0.5f;

        /// <summary>Puts every multiplier back to its config baseline before contributions run.</summary>
        public static void Reset(GameConfig c)
        {
            GoldMult = 1f;
            PriceMult = 1f;
            OreRateMult = 1f;
            CraftSpeedMult = 1f;
            CustomerIntervalMult = 1f;
            ExpeditionSpeedMult = 1f;
            DungeonRewardMult = 1f;
            RunePower = 1f;

            OreCapacityBonus = 0;
            OrePerSecondFlat = 0f;
            ExpeditionSlots = 1;
            RuneMaxLevel = 0;
            RuneCostMult = 1f;
            LuckBonus = 0f;
            OfflineRate = c != null ? c.offlineRate : 0.5f;
        }
    }

    /// <summary>Implemented by any manager that contributes global bonuses.</summary>
    public interface IProductionModifier
    {
        void Contribute(GameConfig config);
    }
}
