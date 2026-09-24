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

        /// <summary>How many times the shop can grow. Mirrors the Smithy building's levels.</summary>
        public const int MaxShopTier = 5;

        [Header("Config")]
        public GameConfig config;

        [Header("Systems (auto-grabbed if on same object)")]
        public EconomyManager economy;
        public UpgradeManager upgrades;
        public ExpeditionManager expeditions;
        public ResourceManager resources;
        public BuildingManager buildings;
        public RecipeManager recipes;
        public ProductionManager production;
        public RuneManager runes;
        public TalentManager talents;
        public PrestigeManager prestige;
        public QuestManager quests;
        public AchievementManager achievements;
        public OrderManager orders;
        public RushHourManager rush;
        public MarketFairManager marketFair;
        public DailyRewardManager daily;

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
        public int RelicOre { get; private set; }

        /// <summary>Smithy level. The whole shop layout and price curve follows this.</summary>
        public int ShopTier => buildings != null ? Mathf.Max(1, buildings.GetLevel(BuildingId.Smithy)) : 1;

        public event System.Action<bool> OnHelperUnlockedChanged;
        public event System.Action<int> OnShopTierChanged;
        public event System.Action<int> OnRelicOreChanged;

        GameObject helperInstance;
        float autosaveTimer;
        float progressTimer;

        /// <summary>What a customer pays for one specific sword.</summary>
        public int PriceOf(SwordItem item)
        {
            if (config == null) return 10;
            RecipeDef recipe = item != null ? config.GetRecipe(item.recipeId) : null;
            int baseValue = recipe != null ? recipe.baseValue : config.swordPrice;
            float oreMult = 1f + config.relicOrePriceBonus * Mathf.Min(RelicOre, config.relicOreMaxBonusCount);
            float mult = RarityInfo.MultiplierOf(item != null ? item.rarity : Rarity.Common)
                       * oreMult * Production.PriceMult * Production.GoldMult
                       * (rush != null ? rush.PriceMult : 1f)
                       * (marketFair != null ? marketFair.PriceMult : 1f)
                       * DailyBonusMult(recipe)
                       * MasteryMultOf(recipe != null ? recipe.id : null);
            return Mathf.Max(1, Mathf.RoundToInt(baseValue * mult));
        }

        /// <summary>Today's featured recipe — rotates through the catalog one slot per dawn.</summary>
        public RecipeDef RecipeOfTheDay()
        {
            var all = recipes != null ? recipes.All : null;
            if (all == null || all.Count == 0) return null;
            int days = Data != null && Data.stats != null ? Data.stats.dayCycles : 0;
            return all[Mathf.Abs(days) % all.Count];
        }

        /// <summary>+30% sale bonus while a recipe is today's feature.</summary>
        public float DailyBonusMult(RecipeDef r)
        {
            return r != null && r == RecipeOfTheDay() ? 1.3f : 1f;
        }

        static readonly int[] masterySteps = { 10, 25, 60, 120, 250 };

        /// <summary>Mastery tier 0..5 for a recipe — each tier is a permanent +4% sell price.</summary>
        public int MasteryTierOf(string recipeId)
        {
            if (Data == null || Data.stats == null || string.IsNullOrEmpty(recipeId)) return 0;
            int forged = Data.stats.ForgedCount(recipeId);
            int tier = 0;
            foreach (int step in masterySteps)
                if (forged >= step) tier++;
            return tier;
        }

        public float MasteryMultOf(string recipeId)
        {
            return 1f + MasteryTierOf(recipeId) * 0.04f;
        }

        /// <summary>Records a forge and tracks the best rarity seen. Called by the worker.</summary>
        public void RegisterForged(SwordItem item)
        {
            if (item == null || Data == null || Data.stats == null) return;
            Data.stats.swordsForged++;
            Data.stats.bestRarity = Mathf.Max(Data.stats.bestRarity, (int)item.rarity);
            Data.stats.NoteForged(item.recipeId, item.rarity);
        }

        /// <summary>Records a sale and adds the takings to the prestige run total.</summary>
        public void RegisterSale(SwordItem item)
        {
            if (item == null || Data == null || Data.stats == null) return;
            int price = PriceOf(item);
            Data.stats.swordsSold++;
            Data.stats.customersServed++;
            if (marketFair != null && marketFair.Active) Data.stats.fairSales++;
            Data.stats.goldEarned += price;
            Data.runEarned += price;
        }

        /// <summary>The recipe the smith is currently forging, falling back to copper.</summary>
        public RecipeDef ActiveRecipe
        {
            get
            {
                if (config == null || Data == null) return null;
                return config.GetRecipe(Data.activeRecipeId) ?? config.GetRecipe(RecipeId.Copper);
            }
        }

        /// <summary>Ore spent on a single forged sword.</summary>
        public int ActiveOreCost
        {
            get
            {
                RecipeDef r = ActiveRecipe;
                return r != null ? r.oreCost : 1;
            }
        }

        public int RackCapacityTotal =>
            config == null ? 4 : upgrades.RackCapacity(config) + (buildings != null ? buildings.RackBonus : 0);

        /// <summary>True while the Smithy can still grow.</summary>
        public bool CanExpand => buildings != null && !buildings.IsMaxed(BuildingId.Smithy);

        public int ExpandCost => buildings != null ? buildings.GetCost(BuildingId.Smithy) : 0;

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
            if (resources == null) resources = GetComponent<ResourceManager>();
            if (resources == null) resources = gameObject.AddComponent<ResourceManager>();
            if (buildings == null) buildings = GetComponent<BuildingManager>();
            if (buildings == null) buildings = gameObject.AddComponent<BuildingManager>();
            if (recipes == null) recipes = GetComponent<RecipeManager>();
            if (recipes == null) recipes = gameObject.AddComponent<RecipeManager>();
            if (production == null) production = GetComponent<ProductionManager>();
            if (production == null) production = gameObject.AddComponent<ProductionManager>();
            production.config = config;
            runes = Ensure<RuneManager>(runes);
            talents = Ensure<TalentManager>(talents);
            prestige = Ensure<PrestigeManager>(prestige);
            quests = Ensure<QuestManager>(quests);
            achievements = Ensure<AchievementManager>(achievements);
            marketFair = Ensure<MarketFairManager>(marketFair);

            Data = SaveSystem.Load();
            economy.Init(Data.gold, Data.totalEarned);
            upgrades.Init(config, Data);
            buildings.Init(config, Data);
            recipes.Init(config, buildings, Data);
            runes.Init(config, buildings, Data);
            talents.Init(config, Data);
            prestige.Init(config);
            resources.Upgrades = upgrades;
            resources.Init(config, Data);
            expeditions.Init(config, Data);
            achievements.Init(config, Data);

            HelperUnlocked = Data.helperUnlocked;
            RelicOre = Mathf.Max(0, Data.relicOre);
            production.InvalidateModifiers();
            production.Recalculate();

            LastOffline = OfflineProgress.Apply();
            quests.Init(config, Data);
        }

        T Ensure<T>(T current) where T : Component
        {
            if (current != null) return current;
            T found = GetComponent<T>();
            return found != null ? found : gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Builds a throwaway live state without touching the save file. Editor tooling calls
        /// this so previews and screenshots show real numbers and real building levels; the
        /// game itself never needs it because Awake runs the same path.
        /// </summary>
        public void EnsureInitializedForPreview(int smithyLevel = 0)
        {
            if (Data != null) return;

            economy = Ensure<EconomyManager>(economy);
            upgrades = Ensure<UpgradeManager>(upgrades);
            expeditions = Ensure<ExpeditionManager>(expeditions);
            resources = Ensure<ResourceManager>(resources);
            buildings = Ensure<BuildingManager>(buildings);
            recipes = Ensure<RecipeManager>(recipes);
            production = Ensure<ProductionManager>(production);
            runes = Ensure<RuneManager>(runes);
            talents = Ensure<TalentManager>(talents);
            prestige = Ensure<PrestigeManager>(prestige);
            quests = Ensure<QuestManager>(quests);
            achievements = Ensure<AchievementManager>(achievements);
            marketFair = Ensure<MarketFairManager>(marketFair);
            production.config = config;

            Data = new SaveData();
            Data.everSaved = true;
            Data.Normalize();

            economy.Init(5000, 0);
            upgrades.Init(config, Data);
            buildings.Init(config, Data);
            if (smithyLevel > 0) buildings.ForceLevel(BuildingId.Smithy, smithyLevel);
            recipes.Init(config, buildings, Data);
            runes.Init(config, buildings, Data);
            talents.Init(config, Data);
            prestige.Init(config);
            resources.Upgrades = upgrades;
            resources.Init(config, Data);
            resources.ResetTo(30);
            expeditions.Init(config, Data);
            achievements.Init(config, Data);
            production.InvalidateModifiers();
            production.Recalculate();
            quests.Init(config, Data);
        }

        /// <summary>What the complex produced while the app was closed. Shown once on boot.</summary>
        public OfflineProgress.Report LastOffline { get; private set; }

        void Start()
        {
            SpawnEnvironment(ShopTier);
            if (rack != null)
            {
                rack.SetCapacity(RackCapacityTotal);
                rack.RestoreStock(Data.rackItems);
            }
            if (apprenticeAnvilRoot != null && HelperUnlocked)
                apprenticeAnvilRoot.SetActive(true);
            if (HelperUnlocked) SpawnHelper(false);
            SpawnAmbientNpcs();
            upgrades.OnUpgradeChanged += HandleUpgradeChanged;
            if (buildings != null) buildings.OnBuildingChanged += (id, _) => HandleBuildingChanged(id);
            Save();
        }

        void HandleUpgradeChanged(string id, int level)
        {
            if (production != null) production.Recalculate();
            if (id == UpgradeId.Rack && rack != null)
                rack.SetCapacity(RackCapacityTotal);
            if (resources != null) resources.Refresh();
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

        /// <summary>
        /// Ambient villager hands spawned at runtime. Their controller types must never
        /// appear on serialized scene objects — a scene-level MonoBehaviour of a script
        /// compiled this way corrupts level0 on device (editor tolerates it).
        /// </summary>
        void SpawnAmbientNpcs()
        {
            var minerGo = new GameObject("MinerHand");
            minerGo.transform.position = new Vector3(-6.0f, 0f, 0.1f);
            var miner = minerGo.AddComponent<MinerController>();
            miner.characterPrefab = config != null ? config.customerPrefabA : null;
            miner.pile = orePile != null ? orePile.transform : null;

            var delverGo = new GameObject("Delver");
            delverGo.transform.position = new Vector3(-3.4f, 0f, -4.0f);
            var adv = delverGo.AddComponent<AdventurerController>();
            adv.characterPrefab = config != null ? config.customerPrefabB : null;
            adv.armSword = true;

            // The stall keeper paces her counter once the Trading Post stands.
            var vendorGo = new GameObject("Vendor");
            vendorGo.transform.position = new Vector3(6f, 0f, 3.0f);
            var vendor = vendorGo.AddComponent<AmbientHand>();
            vendor.characterPrefab = config != null ? config.vendorPrefab : null;
            vendor.requiresBuilding = BuildingId.Market;
            vendor.patrolOffset = 1.4f;

            // The hooded mystic stands vigil in front of the Sanctum.
            var mysticGo = new GameObject("Mystic");
            mysticGo.transform.position = new Vector3(5.4f, 0f, 5.0f);
            var mystic = mysticGo.AddComponent<AmbientHand>();
            mystic.characterPrefab = config != null ? config.mysticPrefab : null;
            mystic.requiresBuilding = BuildingId.Sanctum;
            mystic.patrolOffset = 0f;
            mystic.idleMin = 6f;
            mystic.idleMax = 12f;

            // The stoker shovels at the Blast Furnace mouth once it stands.
            var stokerGo = new GameObject("Stoker");
            stokerGo.transform.position = new Vector3(-5.6f, 0f, 5.0f);
            var stoker = stokerGo.AddComponent<AmbientHand>();
            stoker.characterPrefab = config != null ? config.stokerPrefab : null;
            stoker.requiresBuilding = BuildingId.Furnace;
            stoker.patrolOffset = 0.9f;
            stoker.idleMin = 2.5f;
            stoker.idleMax = 5f;

            // Fireflies over the village once dusk settles.
            var fireflyGo = new GameObject("Fireflies");
            fireflyGo.transform.position = new Vector3(0f, 0f, 1f);
            fireflyGo.AddComponent<FireflyDrift>();

            // Rune shards orbit the Sanctum — one per rune level owned.
            var orbitGo = new GameObject("RuneOrbit");
            orbitGo.AddComponent<RuneOrbit>();

            // The lucky ember — a wandering tap-for-gold bonus over the village.
            var emberGo = new GameObject("EmberSprite");
            var sprite = emberGo.AddComponent<EmberSprite>();
            var picker = FindFirstObjectByType<BuildingPicker>();
            if (picker != null) picker.emberSprite = sprite;

            // Stars fade in once true night settles over the village.
            var starGo = new GameObject("StarField");
            starGo.AddComponent<StarField>();

            // The moon climbs the sky through the night half of the day cycle.
            var moonGo = new GameObject("MoonDrift");
            moonGo.AddComponent<MoonDrift>();

            // Rain showers roll over the village every few minutes.
            var rainGo = new GameObject("RainWeather");
            rainGo.AddComponent<RainWeather>();

            // Bunting over the forecourt, raised only on market-fair days.
            var buntingGo = new GameObject("FairBunting");
            buntingGo.AddComponent<FairBunting>();

            // Strolling villagers who only show up for the fair.
            var crowdGo = new GameObject("FairCrowd");
            crowdGo.AddComponent<FairCrowd>();

            // Butterflies over the front garden while the sun is up.
            var flyGo = new GameObject("ButterflyDrift");
            flyGo.AddComponent<ButterflyDrift>();

            // The sun crossing the sky through the daylight stretch.
            var sunGo = new GameObject("SunDrift");
            sunGo.AddComponent<SunDrift>();
        }

        // ------------------------------------------------------------ shop expansion

        /// <summary>Buys the next Smithy level. The side effects run from the building-changed handler.</summary>
        public bool TryExpandShop()
            => buildings != null && economy != null && buildings.TryUpgrade(BuildingId.Smithy, economy);

        /// <summary>Called after any building level changes: re-applies global bonuses and saves.</summary>
        public void HandleBuildingChanged(string id)
        {
            if (id == BuildingId.Smithy)
            {
                OnShopTierChanged?.Invoke(ShopTier);
                SpawnEnvironment(ShopTier);
                // The rebuilt environment brings fresh decorative emitters.
                UI.SettingsPanel.ApplyFxSetting(UI.SettingsPanel.ReduceFX);
                UI.UIManager.Instance?.SpawnFloatingText(
                    new Vector3(0f, 2.8f, 0f), "SMITHY LEVEL " + ShopTier + "!", new Color(1f, 0.72f, 0.3f));
                AudioManager.Play("fanfare", 0.04f, 0.85f);
                // From tier three the workshop theme gives way to the deep-forge drone.
                AudioManager.PlayMusic(ShopTier >= 3 ? "music_deep" : "music_forge", 3f);
            }
            if (production != null) production.Recalculate();
            if (rack != null) rack.SetCapacity(RackCapacityTotal);
            if (recipes != null) recipes.RefreshUnlocks();
            if (resources != null) resources.Refresh();
            Save();
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

            float mult = Mathf.Max(0.01f, Production.DungeonRewardMult);
            int gold = Mathf.Max(1, Mathf.RoundToInt(def.goldReward * mult));
            int relic = Mathf.Max(1, Mathf.RoundToInt(def.relicOreReward * mult));

            economy.AddGold(gold);
            AddRelicOre(relic);
            if (Data.stats != null)
            {
                Data.stats.expeditionsClaimed++;
                Data.stats.relicsEarned += relic;
                Data.stats.goldEarned += gold;
                Data.runEarned += gold;
            }
            Save();
            return true;
        }

        /// <summary>Relic ore is also the Sanctum's currency, so it can be spent as well as earned.</summary>
        public bool SpendRelicOre(int amount)
        {
            if (amount <= 0) return true;
            if (RelicOre < amount) return false;
            RelicOre -= amount;
            OnRelicOreChanged?.Invoke(RelicOre);
            return true;
        }

        /// <summary>
        /// Wipes everything that belongs to the current run — gold, ore, buildings, upgrades,
        /// runes, the rack and any expeditions. Talents, shards, quests, achievements and
        /// lifetime stats are deliberately left alone.
        /// </summary>
        public void ResetRun()
        {
            economy.Init(0, Data.totalEarned);
            buildings.ResetToStart();
            upgrades.ResetAll();
            runes.ResetAll();
            if (rack != null) rack.ClearAll();
            if (expeditions != null) expeditions.ClearAll();
            resources.ResetTo(0);

            OnShopTierChanged?.Invoke(ShopTier);
            SpawnEnvironment(ShopTier);
            production.Recalculate();
            if (rack != null) rack.SetCapacity(RackCapacityTotal);
            recipes.RefreshUnlocks(false);
            resources.Refresh();
            Save();
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
            float dt = Time.deltaTime;
            if (resources != null) resources.Tick(dt);
            if (Data != null && Data.stats != null) Data.stats.playSeconds += dt;

            // Quest and achievement checks are cheap and idempotent, so a steady poll keeps
            // every system honest without wiring an event for each one.
            progressTimer += dt;
            if (progressTimer >= 0.25f)
            {
                progressTimer = 0f;
                if (quests != null) quests.Refresh();
                if (achievements != null) achievements.Refresh();
            }

            autosaveTimer += dt;
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
            Data.relicOre = RelicOre;
            Data.lastSaveUtcMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Data.rackStoredSwords = rack != null ? rack.Stock : 0;
            upgrades.WriteTo(Data);
            if (buildings != null) buildings.WriteTo(Data);
            if (runes != null) runes.WriteTo(Data);
            if (talents != null) talents.WriteTo(Data);
            if (rack != null) Data.rackItems = new System.Collections.Generic.List<SwordItem>(rack.Items);
            if (resources != null) resources.WriteTo(Data);
            if (expeditions != null) expeditions.WriteTo(Data);
            SaveSystem.Save(Data);
        }

        void OnApplicationPause(bool pause) { if (pause) Save(); }
        void OnApplicationQuit() { Save(); }
    }
}
