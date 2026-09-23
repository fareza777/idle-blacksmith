using UnityEngine;

namespace IdleBlacksmith.Core
{
    [CreateAssetMenu(menuName = "Idle Blacksmith/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Economy")]
        [Tooltip("Base price of a copper sword; recipes override this per item")]
        public int swordPrice = 10;
        public int helperCost = 300;

        [Header("Ore")]
        [Tooltip("Ore mined per second by the blacksmith himself — guarantees the loop never deadlocks")]
        public float baseOrePerSecond = 0.45f;
        [Tooltip("Ore the shop can hold before the mine level and upgrades add more")]
        public int baseOreCapacity = 40;
        [Tooltip("Fraction of income earned while the app is closed")]
        public float offlineRate = 0.5f;
        [Tooltip("Offline payouts are capped at this many hours")]
        public float offlineCapHours = 8f;
        [Tooltip("Offline gold per second per Smithy/Trading Post level")]
        public float offlineGoldPerBuildingLevel = 1.2f;

        [Header("Workers")]
        public float baseMoveSpeed = 2.3f;
        public float pickupDuration = 0.7f;
        public float depositDuration = 0.35f;
        public float baseCraftDuration = 3.2f;
        public float minCraftDuration = 0.8f;

        [Header("Customers")]
        public float customerMoveSpeed = 1.8f;
        public float customerLeaveSpeedMultiplier = 1.15f;
        public float minCustomerInterval = 4.5f;
        public float maxCustomerInterval = 9f;

        [Header("Rack")]
        public int baseRackCapacity = 4;

        [Header("Dungeon")]
        public ExpeditionDef[] expeditions;
        [Tooltip("Sale price bonus per relic ore held")]
        public float relicOrePriceBonus = 0.05f;
        [Tooltip("Relic ore beyond this count gives no further price bonus")]
        public int relicOreMaxBonusCount = 40;

        [Header("Prestige")]
        [Tooltip("Gold earned in a run needed per 1 ember shard, before the exponent")]
        public float prestigeGoldDivisor = 5000f;
        [Tooltip("Scales the whole shard payout")]
        public float prestigeShardScale = 6f;
        public float prestigeExponent = 0.6f;

        [Header("Menu Art (imported from Art/Menu)")]
        public Sprite splashArt;
        public Sprite emblemArt;
        public Sprite dungeonArt;
        public Sprite[] onboardingArt;

        [Header("Content")]
        public UpgradeDef[] upgrades;
        public BuildingDef[] buildings;
        public RecipeDef[] recipes;
        public RuneDef[] runes;
        public TalentDef[] talents;
        public QuestDef[] quests;
        public AchievementDef[] achievements;

        [Header("Prefabs")]
        public GameObject workerPrefab;
        public GameObject helperPrefab;
        public GameObject customerPrefabA;
        public GameObject customerPrefabB;
        [Tooltip("Distinct noble look used for VIP customers (high pay multiplier)")]
        public GameObject customerPrefabC;
        public GameObject vendorPrefab;
        public GameObject mysticPrefab;
        public GameObject stokerPrefab;
        public GameObject swordPrefab;
        public GameObject oreChunkPrefab;

        [Header("Rarity")]
        [Tooltip("Gem material per Rarity index (Common..Legendary), swapped onto forged sword gems")]
        public Material[] rarityGems;
        [Tooltip("Sparkle burst played on swords of Rare quality or better")]
        public ParticleSystem raritySparklePrefab;

        public Material GemMaterial(Rarity rarity)
        {
            if (rarityGems == null || rarityGems.Length == 0) return null;
            return rarityGems[Mathf.Clamp((int)rarity, 0, rarityGems.Length - 1)];
        }

        public UpgradeDef GetUpgrade(string id)
        {
            if (upgrades == null) return null;
            foreach (UpgradeDef u in upgrades)
                if (u != null && u.id == id) return u;
            return null;
        }

        public ExpeditionDef GetExpedition(string id)
        {
            if (expeditions == null || string.IsNullOrEmpty(id)) return null;
            foreach (ExpeditionDef e in expeditions)
                if (e != null && e.id == id) return e;
            return null;
        }

        public BuildingDef GetBuilding(string id)
        {
            if (buildings == null) return null;
            foreach (BuildingDef b in buildings)
                if (b != null && b.id == id) return b;
            return null;
        }

        public RecipeDef GetRecipe(string id)
        {
            if (recipes == null || string.IsNullOrEmpty(id)) return null;
            foreach (RecipeDef r in recipes)
                if (r != null && r.id == id) return r;
            return null;
        }

        public RuneDef GetRune(string id)
        {
            if (runes == null) return null;
            foreach (RuneDef r in runes)
                if (r != null && r.id == id) return r;
            return null;
        }

        public TalentDef GetTalent(string id)
        {
            if (talents == null) return null;
            foreach (TalentDef t in talents)
                if (t != null && t.id == id) return t;
            return null;
        }
    }

    [System.Serializable]
    public class UpgradeDef
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Min(1)] public int maxLevel = 10;
        [Min(1)] public int baseCost = 50;
        [Min(1f)] public float costGrowth = 1.85f;
    }

    [System.Serializable]
    public class ExpeditionDef
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Tooltip("Real-time seconds; resolves even while the app is closed")]
        public int durationSeconds = 120;
        public int goldReward = 60;
        public int relicOreReward = 2;
        [Tooltip("Dungeon Gate level required to unlock this expedition")]
        public int requiredGateLevel = 1;
    }

    /// <summary>
    /// One building in the forge complex. Level 0 means "not built yet"; the Smithy is the
    /// only one that starts above zero. Cost arrays are indexed by the level being bought
    /// (index 0 = cost to reach level 2).
    /// </summary>
    [System.Serializable]
    public class BuildingDef
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Tooltip("Level the building starts at on a fresh save (the Smithy is 1, everything else 0)")]
        public int startLevel;
        [Min(1)] public int maxLevel = 5;
        [Tooltip("Cost of the next level, indexed by (currentLevel - startLevel). Length caps how far it can grow.")]
        public int[] levelCosts;
        [Tooltip("One short line per level, index 0 = level 1")]
        public string[] levelPerks;

        [Header("Effects (applied per level)")]
        [Tooltip("Smithy/Market: extra sale price per level above 1")]
        public float priceBonus;
        [Tooltip("Smithy: extra rack slots per level above 1")]
        public int rackBonus;
        [Tooltip("Mine: ore mined per second per level")]
        public float orePerSecond;
        [Tooltip("Mine: extra ore capacity per level")]
        public int oreCapacity;
        [Tooltip("Market: fraction shaved off the customer interval per level above 1")]
        public float customerIntervalCut;
        [Tooltip("Gate: extra concurrent expeditions per level above 1")]
        public int expeditionSlots;
        [Tooltip("Gate: extra expedition reward fraction per level above 1")]
        public float dungeonRewardBonus;
        [Tooltip("Sanctum: extra rune levels per level of the sanctum")]
        public int runeLevelsPerTier;
        [Tooltip("Sanctum: fraction shaved off rune costs per level above 1")]
        public float runeCostCut;
        [Tooltip("Blast Furnace: fraction off craft duration per level")]
        public float craftSpeedCut;
        [Tooltip("Storehouse: extra offline earnings fraction per level")]
        public float offlineBonus;

        public int MaxBuyableLevel => Mathf.Min(maxLevel, (levelCosts != null ? levelCosts.Length : 0) + 1);
    }

    [System.Serializable]
    public class RecipeDef
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Min(1)] public int oreCost = 1;
        [Min(1)] public int baseValue = 10;
        [Min(0.1f)] public float craftDuration = 3.2f;
        [Tooltip("Smithy level needed before this recipe can be forged")]
        public int requiredSmithyLevel = 1;
        [Tooltip("Mine level needed before this recipe can be forged")]
        public int requiredMineLevel;
        public GameObject swordPrefab;
        [Tooltip("Blade colour index into the shared palette")]
        public int bladeColor = 40;
    }

    /// <summary>A permanent relic-ore upgrade bought at the Sanctum.</summary>
    [System.Serializable]
    public class RuneDef
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Min(1)] public int baseMaxLevel = 4;
        [Min(1)] public int baseCost = 8;
        [Min(1f)] public float costGrowth = 1.6f;
        [Tooltip("Effect per level, meaning depends on the rune id")]
        public float effectPerLevel = 0.06f;
    }

    public enum TalentKind
    {
        GoldBonus,
        OreRate,
        CraftSpeed,
        PriceBonus,
        Luck,
        OfflineRate,
        CustomerRate,
        OreCapacity,
        ExpeditionSpeed,
        RunePower,
    }

    /// <summary>A permanent ember-shard upgrade bought after prestiging.</summary>
    [System.Serializable]
    public class TalentDef
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public TalentKind kind;
        [Min(1)] public int maxLevel = 5;
        [Min(1)] public int baseCost = 1;
        [Min(1f)] public float costGrowth = 2f;
        public float effectPerLevel = 0.1f;
    }

    /// <summary>What a quest or achievement counts.</summary>
    public enum QuestGoal
    {
        ForgeSwords,
        SellSwords,
        EarnGoldRun,
        ReachGold,
        ReachOre,
        UpgradeBuilding,
        UnlockRecipe,
        ClaimExpedition,
        HireHelper,
        Prestige,
        BuildRunes,
        OwnRarity,
        PlayMinutes,
        ServeOrders,
        RushOrders,
        ClaimDailies,
        UseTools,
        PetCat,
    }

    [System.Serializable]
    public class QuestDef
    {
        public string id;
        public string title;
        [TextArea] public string body;
        public QuestGoal goal;
        [Tooltip("Building or recipe id, for the goals that need one")]
        public string targetId;
        public int target = 1;
        public int goldReward;
        public int oreReward;
        public int relicReward;
        public int shardReward;
    }

    public enum AchBonus
    {
        Gold,
        Ore,
        Price,
        Craft,
        Luck,
        Offline,
    }

    [System.Serializable]
    public class AchievementDef
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public QuestGoal goal;
        public string targetId;
        public int target = 1;
        public AchBonus bonusKind;
        [Tooltip("Permanent bonus granted while unlocked, e.g. 0.02 = +2%")]
        public float bonus;
    }
}
