using UnityEngine;

namespace IdleBlacksmith.Core
{
    [CreateAssetMenu(menuName = "Idle Blacksmith/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Economy")]
        public int swordPrice = 10;
        public int helperCost = 300;

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

        [Header("Shop Expansion")]
        [Tooltip("Cost to go tier 1->2, then 2->3")]
        public int[] expandCosts = { 250, 1500 };
        [Tooltip("Extra sale price per shop tier above 1")]
        public float tierPriceBonus = 0.15f;
        [Tooltip("Extra rack slots per shop tier above 1")]
        public int tierRackBonus = 2;

        [Header("Dungeon")]
        public ExpeditionDef[] expeditions;
        [Tooltip("Sale price bonus per relic ore held")]
        public float relicOrePriceBonus = 0.05f;
        [Tooltip("Relic ore beyond this count gives no further price bonus")]
        public int relicOreMaxBonusCount = 40;

        [Header("Menu Art (imported from Art/Menu)")]
        public Sprite splashArt;
        public Sprite emblemArt;
        public Sprite dungeonArt;
        public Sprite[] onboardingArt;

        [Header("Upgrades")]
        public UpgradeDef[] upgrades;

        [Header("Prefabs")]
        public GameObject workerPrefab;
        public GameObject helperPrefab;
        public GameObject customerPrefabA;
        public GameObject customerPrefabB;
        public GameObject swordPrefab;
        public GameObject oreChunkPrefab;

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
    }
}
