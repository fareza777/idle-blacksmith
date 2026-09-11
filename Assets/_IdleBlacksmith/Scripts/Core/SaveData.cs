using System;

namespace IdleBlacksmith.Core
{
    [Serializable]
    public class SaveData
    {
        public int version = 2;
        public int gold;
        public int totalEarned;
        public int craftLevel;
        public int carryLevel;
        public int rackLevel;
        public bool helperUnlocked;
        public int rackStoredSwords;

        // ---- v2: RPG rework ----
        public int shopTier = 1;                 // 1..3, drives the environment prefab + bonuses
        public int relicOre;                     // dungeon currency, boosts sword prices
        public string expeditionId = "";         // active dungeon expedition ("", none)
        public long expeditionEndUtcMs;          // wall-clock end so it resolves while offline
        public bool onboardingSeen;
    }
}
