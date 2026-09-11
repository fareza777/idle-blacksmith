using System;
using System.Collections.Generic;

namespace IdleBlacksmith.Core
{
    [Serializable]
    public class BuildingState
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class UpgradeState
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class RuneState
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class TalentState
    {
        public string id;
        public int level;
    }

    /// <summary>A running dungeon expedition. Ends are wall-clock so they resolve offline.</summary>
    [Serializable]
    public class ExpeditionState
    {
        public string id = "";
        public long startUtcMs;
        public long endUtcMs;
    }

    /// <summary>Lifetime counters, kept across prestige. Drives achievements and the stats panel.</summary>
    [Serializable]
    public class StatBlock
    {
        public int swordsForged;
        public int swordsSold;
        public long goldEarned;
        public int customersServed;
        public int expeditionsClaimed;
        public int prestiges;
        public int buildingsUpgraded;
        public int recipesUnlocked = 1;
        public int relicsEarned;
        public int bestRarity;
        public float playSeconds;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 3;

        public int gold;
        public int totalEarned;
        public bool helperUnlocked;

        // ---- v2 fields, read once by SaveSystem.Migrate and rewritten in their v3 form ----
        public int craftLevel;
        public int carryLevel;
        public int rackLevel;
        public int rackStoredSwords;
        public int shopTier = 1;
        public int relicOre;
        public string expeditionId = "";
        public long expeditionEndUtcMs;
        public bool onboardingSeen;

        // ---- v3: data-driven progression ----
        /// <summary>Metal ore stock. Mined over time, consumed by every forge.</summary>
        public int ore;
        /// <summary>Wall-clock stamp of the last save, used to pay out offline progress.</summary>
        public long lastSaveUtcMs;
        /// <summary>False until the first real save, so a brand-new player skips the welcome-back modal.</summary>
        public bool everSaved;

        public string activeRecipeId = RecipeId.Copper;
        public List<string> unlockedRecipes = new List<string> { RecipeId.Copper };
        public List<BuildingState> buildings = new List<BuildingState>();
        public List<UpgradeState> upgradeList = new List<UpgradeState>();
        public List<RuneState> runes = new List<RuneState>();
        public List<TalentState> talents = new List<TalentState>();
        public List<SwordItem> rackItems = new List<SwordItem>();
        public List<ExpeditionState> expeditions = new List<ExpeditionState>();

        public int emberShards;
        public int prestigeCount;
        /// <summary>Gold earned since the last prestige — the prestige payout is based on this.</summary>
        public long runEarned;

        public string activeQuestId = "";
        public List<string> questsClaimed = new List<string>();
        public List<string> achievementsUnlocked = new List<string>();

        public StatBlock stats = new StatBlock();

        /// <summary>Guarantees no list is left null after a JsonUtility round-trip or a fresh load.</summary>
        public void Normalize()
        {
            if (unlockedRecipes == null) unlockedRecipes = new List<string>();
            if (buildings == null) buildings = new List<BuildingState>();
            if (upgradeList == null) upgradeList = new List<UpgradeState>();
            if (runes == null) runes = new List<RuneState>();
            if (talents == null) talents = new List<TalentState>();
            if (rackItems == null) rackItems = new List<SwordItem>();
            if (expeditions == null) expeditions = new List<ExpeditionState>();
            if (questsClaimed == null) questsClaimed = new List<string>();
            if (achievementsUnlocked == null) achievementsUnlocked = new List<string>();
            if (stats == null) stats = new StatBlock();
            if (string.IsNullOrEmpty(activeRecipeId)) activeRecipeId = RecipeId.Copper;
            if (unlockedRecipes.Count == 0) unlockedRecipes.Add(RecipeId.Copper);
        }
    }
}
