using IdleBlacksmith.Gameplay;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Central hub: owns save data, wires systems, spawns the helper, handles shop
    /// expansion (environment tier swap), relic ore, expeditions and onboarding state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        static GameManager instance;

        /// <summary>Edit-mode safe: falls back to a scene search so editor tooling can read refs.</summary>
        public static GameManager Instance
        {
            get
            {
                if (instance == null && !Application.isPlaying)
                    instance = FindFirstObjectByType<GameManager>();
                return instance;
            }
            private set => instance = value;
        }

        public const int MaxShopTier = 3;

        [Header("Config")]
        public GameConfig config;

        [Header("Systems (auto-grabbed if on same object)")]
        public EconomyManager economy;
        public UpgradeManager upgrades;
        public ExpeditionManager expeditions;

        [Header("Scene stations")]
        public OrePile orePile;
        public AnvilStation anvil;
        public SwordRack rack;
        public CustomerSpawner customerSpawner;

        [Header("Helper")]
        public Transform helperSpawnPoint;
        public GameObject apprenticeAnvilRoot;

        [Header("Environment (shop growth stages, tier 1..3)")]
        public Transform environmentRoot;
        public GameObject[] environmentPrefabs;

        public SaveData Data { get; private set; }
        public bool HelperUnlocked { get; private set; }
        public int ShopTier { get; private set; } = 1;
        public int RelicOre { get; private set; }

        public event System.Action<bool> OnHelperUnlockedChanged;
        public event System.Action<int> OnShopTierChanged;
        public event System.Action<int> OnRelicOreChanged;

        GameObject helperInstance;
        float autosaveTimer;

        /// <summary>Sale price of one sword, including relic-ore and shop-tier bonuses.</summary>
        public int CurrentSwordPrice
        {
            get
            {
                if (config == null) return 10;
                float oreMult = 1f + config.relicOrePriceBonus * Mathf.Min(RelicOre, config.relicOreMaxBonusCount);
                float tierMult = 1f + config.tierPriceBonus * (ShopTier - 1);
                return Mathf.Max(1, Mathf.RoundToInt(config.swordPrice * oreMult * tierMult));
            }
        }

        public int RackCapacityTotal =>
            config == null ? 4 : upgrades.RackCapacity(config) + config.tierRackBonus * (ShopTier - 1);

        public bool CanExpand => config != null && ShopTier < MaxShopTier
            && config.expandCosts != null && ShopTier - 1 < config.expandCosts.Length;

        public int ExpandCost => CanExpand ? config.expandCosts[ShopTier - 1] : 0;

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep; // idle game: keep the screen on
            if (economy == null) economy = GetComponent<EconomyManager>();
            if (economy == null) economy = gameObject.AddComponent<EconomyManager>();
            if (upgrades == null) upgrades = GetComponent<UpgradeManager>();
            if (upgrades == null) upgrades = gameObject.AddComponent<UpgradeManager>();
            if (expeditions == null) expeditions = GetComponent<ExpeditionManager>();
            if (expeditions == null) expeditions = gameObject.AddComponent<ExpeditionManager>();

            Data = SaveSystem.Load();
            economy.Init(Data.gold, Data.totalEarned);
            upgrades.Init(config, Data);
            expeditions.Init(config, Data);
            HelperUnlocked = Data.helperUnlocked;
            ShopTier = Mathf.Clamp(Data.shopTier < 1 ? 1 : Data.shopTier, 1, MaxShopTier);
            RelicOre = Mathf.Max(0, Data.relicOre);
        }

        void Start()
        {
            SpawnEnvironment(ShopTier);
            if (rack != null)
            {
                rack.SetCapacity(RackCapacityTotal);
                rack.RestoreStock(Data.rackStoredSwords);
            }
            if (apprenticeAnvilRoot != null && HelperUnlocked)
                apprenticeAnvilRoot.SetActive(true);
            if (HelperUnlocked) SpawnHelper(false);
            upgrades.OnUpgradeChanged += HandleUpgradeChanged;
            Save();
        }

        void HandleUpgradeChanged(string id, int level)
        {
            if (id == UpgradeManager.RackId && rack != null)
                rack.SetCapacity(RackCapacityTotal);
            Save();
        }

        // ------------------------------------------------------------ helper

        public bool TryUnlockHelper()
        {
            if (HelperUnlocked || economy == null || !economy.Spend(config.helperCost)) return false;
            HelperUnlocked = true;
            if (apprenticeAnvilRoot != null) apprenticeAnvilRoot.SetActive(true);
            SpawnHelper(true);
            OnHelperUnlockedChanged?.Invoke(true);
            Save();
            return true;
        }

        void SpawnHelper(bool withEffect)
        {
            if (helperInstance != null || config.helperPrefab == null) return;
            Vector3 pos = helperSpawnPoint != null
                ? helperSpawnPoint.position
                : (anvil != null ? anvil.transform.position + Vector3.right * 1.2f : Vector3.zero);
            helperInstance = Instantiate(config.helperPrefab, pos, Quaternion.identity, transform);
            var w = helperInstance.GetComponent<WorkerController>();
            if (w != null)
            {
                AnvilStation helperAnvil = apprenticeAnvilRoot != null
                    ? apprenticeAnvilRoot.GetComponentInChildren<AnvilStation>(true)
                    : anvil;
                w.Init(orePile, helperAnvil != null ? helperAnvil : anvil, rack, 1);
                if (withEffect) w.PlaySpawnEffect();
            }
        }

        // ------------------------------------------------------------ shop expansion

        public bool TryExpandShop()
        {
            if (!CanExpand || economy == null || !economy.Spend(ExpandCost)) return false;
            ShopTier++;
            SpawnEnvironment(ShopTier);
            if (rack != null) rack.SetCapacity(RackCapacityTotal);
            OnShopTierChanged?.Invoke(ShopTier);
            Save();
            return true;
        }

        void SpawnEnvironment(int tier)
        {
            if (environmentRoot == null || environmentPrefabs == null || environmentPrefabs.Length == 0) return;
            int idx = Mathf.Clamp(tier - 1, 0, environmentPrefabs.Length - 1);
            GameObject prefab = environmentPrefabs[idx];
            if (prefab == null) return;
            for (int i = environmentRoot.childCount - 1; i >= 0; i--)
                Destroy(environmentRoot.GetChild(i).gameObject);
            Instantiate(prefab, Vector3.zero, Quaternion.identity, environmentRoot);
        }

        // ------------------------------------------------------------ relic ore

        public void AddRelicOre(int amount)
        {
            if (amount == 0) return;
            RelicOre = Mathf.Max(0, RelicOre + amount);
            OnRelicOreChanged?.Invoke(RelicOre);
        }

        // ------------------------------------------------------------ expeditions

        public bool StartExpedition(string id)
        {
            if (expeditions == null || !expeditions.StartExpedition(id)) return false;
            Save();
            return true;
        }

        /// <summary>Claims the finished expedition: gold + relic ore. False while it runs.</summary>
        public bool TryClaimExpedition(out ExpeditionDef def)
        {
            def = null;
            if (expeditions == null || !expeditions.TryClaim(out def)) return false;
            economy.AddGold(def.goldReward);
            AddRelicOre(def.relicOreReward);
            Save();
            return true;
        }

        // ------------------------------------------------------------ onboarding

        public bool HasSeenOnboarding => Data != null && Data.onboardingSeen;

        public void MarkOnboardingSeen()
        {
            if (Data == null) return;
            Data.onboardingSeen = true;
            Save();
        }

        // ------------------------------------------------------------ save loop

        void Update()
        {
            autosaveTimer += Time.deltaTime;
            if (autosaveTimer >= 10f)
            {
                autosaveTimer = 0f;
                Save();
            }
        }

        public void Save()
        {
            if (Data == null || economy == null || upgrades == null) return;
            Data.gold = economy.Gold;
            Data.totalEarned = economy.TotalEarned;
            Data.helperUnlocked = HelperUnlocked;
            Data.shopTier = ShopTier;
            Data.relicOre = RelicOre;
            Data.version = 2;
            if (rack != null) Data.rackStoredSwords = rack.Stock;
            upgrades.WriteTo(Data);
            if (expeditions != null) expeditions.WriteTo(Data);
            SaveSystem.Save(Data);
        }

        void OnApplicationPause(bool pause) { if (pause) Save(); }
        void OnApplicationQuit() { Save(); }
    }
}
