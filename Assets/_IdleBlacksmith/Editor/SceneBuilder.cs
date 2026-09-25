using IdleBlacksmith.Core;
using IdleBlacksmith.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// Builds the playable Shop scene: lighting, portrait camera, post volume,
    /// shop layout (counter-based sales flow), gameplay systems, UI, and wiring.
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = Paths.Scenes + "/Shop.unity";
        public const string ConfigPath = Paths.Settings + "/GameConfig.asset";
        public const string UrpAssetPath = Paths.Settings + "/URP.asset";
        public const string UrpRendererPath = Paths.Settings + "/URP_Renderer.asset";
        public const string PostProfilePath = Paths.Settings + "/PostProfile.asset";

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        public static void Build()
        {
            GameConfig config = EnsureConfig();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLighting();
            CameraDirector cameraDirector = SetupCamera();
            SetupPost();

            // ------------------------------------------------ world layout
            // Environment root: GameManager swaps the tier prefab in here as the shop grows.
            var envRoot = new GameObject("EnvironmentRoot");
            var envPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.EnvironmentTierPrefabs[0]);
            if (envPrefab != null)
            {
                var env = (GameObject)PrefabUtility.InstantiatePrefab(envPrefab);
                env.transform.SetParent(envRoot.transform, false);
            }

            GameObject forge = Spawn(ModelFactory.ForgePrefab, V(-1.85f, 0, 2.35f), 135f); // hearth toward camera
            GameObject anvil = Spawn(ModelFactory.AnvilPrefab, V(-0.45f, 0, 1.3f), 15f);
            GameObject apprentice = Spawn(ModelFactory.ApprenticeAnvilPrefab, V(1.3f, 0, 1.6f), -12f);
            apprentice.SetActive(false);
            GameObject ore = Spawn(ModelFactory.OrePilePrefab, V(-2.0f, 0, 0.85f), 0f);
            // sales area: rack stocked from the shop side, counter facing the front door
            GameObject rack = Spawn(ModelFactory.SwordRackPrefab, V(0.8f, 0, -0.95f), 0f);
            Spawn(ModelFactory.CounterPrefab, V(0.8f, 0, -2.15f), 180f);
            Spawn(ModelFactory.BarrelPrefab, V(2.0f, 0, 2.8f), 15f);
            Spawn(ModelFactory.CratePrefab, V(2.05f, 0, 1.95f), -8f);
            Spawn(ModelFactory.StoolPrefab, V(-1.35f, 0, 0.5f), 30f);
            Spawn(ModelFactory.PlantPrefab, V(-2.0f, 0, -2.75f), 0f);
            // Tappable workbench tools: grindstone sharpens the next blade, the quench
            // trough finishes the current craft, and the bellows on the hearth stokes it.
            Spawn(ModelFactory.GrindstonePrefab, V(1.95f, 0, 0.55f), 100f);
            Spawn(ModelFactory.QuenchTroughPrefab, V(-0.55f, 0, -0.85f), 8f);
            Spawn(ModelFactory.RugPrefab, V(0.1f, 0.005f, 0.55f), 8f);
            Spawn(ModelFactory.SignPostPrefab, V(3.3f, 0, -4.6f), 160f);

            // Drifting clouds and the forge cat — ambient life that survives tier rebuilds.
            var cloudRng = new System.Random(4242);
            var clouds = new GameObject("Clouds");
            for (int c = 0; c < 5; c++)
            {
                var cloud = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.CloudPrefabPath(c % 3)));
                cloud.transform.SetParent(clouds.transform, false);
                cloud.transform.position = new Vector3(
                    Mathf.Lerp(-12f, 12f, (float)cloudRng.NextDouble()),
                    Mathf.Lerp(9.5f, 13.5f, (float)cloudRng.NextDouble()),
                    Mathf.Lerp(-6f, 8f, (float)cloudRng.NextDouble()));
                float cs = Mathf.Lerp(0.85f, 1.5f, (float)cloudRng.NextDouble());
                cloud.transform.localScale = Vector3.one * cs;
                cloud.transform.rotation = Quaternion.Euler(0f, (float)cloudRng.NextDouble() * 360f, 0f);
                var drift = cloud.GetComponent<CloudDrift>();
                if (drift != null) drift.speed = Mathf.Lerp(0.16f, 0.42f, (float)cloudRng.NextDouble());
            }
            GameObject cat = Spawn(ModelFactory.CatPrefab, V(-0.95f, 0, 1.95f), 205f);

            // Small birds cross the sky every few seconds — ambient life.
            var flock = new GameObject("BirdFlock");
            var bf = flock.AddComponent<BirdFlock>();
            bf.birdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.BirdPrefab);

            // ------------------------------------------------ the rest of the complex
            // Each plot faces the forge, so every building reads as part of one yard. The plots sit
            // clear of the Smithy's *widest* growth stage (level 5 reaches x ±5.3, z -3.5..5.9),
            // otherwise the fully grown forge swallows the buildings around it.
            // The front-right is deliberately left clear: that is the customer approach path
            // (spawn -> yard -> door -> counter), so nothing may block it.
            var plotsRoot = new GameObject("Buildings");
            Vector3 shopCentre = V(0.15f, 0f, -0.55f);
            BuildingVisuals[] plots =
            {
                MakePlot(plotsRoot, BuildingId.Mine, V(-6.0f, 0, 1.4f), shopCentre, "ORE MINE"),
                MakePlot(plotsRoot, BuildingId.Market, V(6.0f, 0, 1.4f), shopCentre, "TRADING POST"),
                MakePlot(plotsRoot, BuildingId.Gate, V(-3.4f, 0, -5.4f), shopCentre, "DUNGEON GATE"),
                MakePlot(plotsRoot, BuildingId.Sanctum, V(5.4f, 0, 6.6f), shopCentre, "SANCTUM"),
                MakePlot(plotsRoot, BuildingId.Furnace, V(-5.6f, 0, 6.4f), shopCentre, "BLAST FURNACE"),
                MakePlot(plotsRoot, BuildingId.Storehouse, V(-6.3f, 0, -2.3f), shopCentre, "STOREHOUSE"),
            };

            Transform helperSpawn = Marker("HelperSpawn", V(1.3f, 0, 0.85f));
            // Customer flow: along the yard path -> front door -> the counter.
            // The yard waypoints sit on the grass (the ground slab's top face), not on the shop
            // floor, otherwise the customer hovers a few centimetres above the grass on the way in.
            Transform wpYard = Marker("WpYard", V(2.4f, ModelFactory.OutdoorGroundY, -4.55f));
            Transform wpDoor = Marker("WpDoor", V(0.8f, 0f, -4.1f));
            Transform wpCounter = Marker("WpCounter", V(0.8f, 0f, -2.8f));
            Transform customerSpawn = Marker("CustomerSpawn", V(6.4f, ModelFactory.OutdoorGroundY, -5.7f));

            // The storehouse porter: hauls crates smithy -> depot once the building stands.
            var porterGo = Spawn(ModelFactory.CustomerBPrefab, V(-6.3f, 0, -1.0f), 140f);
            Object.DestroyImmediate(porterGo.GetComponent<CustomerController>());
            var porter = porterGo.AddComponent<PorterController>();
            porter.shopPoint = wpDoor;

            // ------------------------------------------------ systems
            var gameGo = new GameObject("Game");
            var economy = gameGo.AddComponent<EconomyManager>();
            var upgrades = gameGo.AddComponent<UpgradeManager>();
            var expeditions = gameGo.AddComponent<ExpeditionManager>();
            var resources = gameGo.AddComponent<ResourceManager>();
            var buildings = gameGo.AddComponent<BuildingManager>();
            var recipes = gameGo.AddComponent<RecipeManager>();
            var production = gameGo.AddComponent<ProductionManager>();
            var runes = gameGo.AddComponent<RuneManager>();
            var talents = gameGo.AddComponent<TalentManager>();
            var prestige = gameGo.AddComponent<PrestigeManager>();
            var questManager = gameGo.AddComponent<QuestManager>();
            var achievements = gameGo.AddComponent<AchievementManager>();
            var orders = gameGo.AddComponent<OrderManager>();
            var rush = gameGo.AddComponent<RushHourManager>();
            gameGo.AddComponent<ChatterManager>();
            var daily = gameGo.AddComponent<DailyRewardManager>();
            var gm = gameGo.AddComponent<GameManager>();

            var audioGo = new GameObject("Audio");
            var audio = audioGo.AddComponent<AudioManager>();
            audio.clips = new[]
            {
                Clip("hammer", 0.9f), Clip("coin", 0.9f), Clip("pop", 0.8f),
                Clip("upgrade", 0.9f), Clip("hire", 1f), Clip("denied", 0.7f),
                Clip("fanfare", 0.9f),
                Clip("mine_pick", 0.8f), Clip("market_chime", 0.9f), Clip("enchant", 0.9f),
                Clip("quest_done", 0.9f), Clip("achievement", 0.9f), Clip("prestige", 1f),
                Clip("unlock", 0.9f), Clip("levelup", 0.9f), Clip("whoosh", 0.7f),
                Clip("blip", 0.9f), Clip("ember_whoosh", 0.9f), Clip("amb_fire", 0.4f),
                Clip("amb_night", 0.45f), Clip("amb_rain", 0.55f), Clip("thunder", 0.7f),
            };
            audio.musicClips = new[]
            {
                Clip("music_forge", 1f), Clip("music_intro", 1f), Clip("music_deep", 1f),
                Clip("music_fair", 1f),
            };

            var dialogue = gameGo.AddComponent<DialogueManager>();
            dialogue.portraits = new[]
            {
                new DialogueManager.NamedSprite { key = "bram", sprite = AssetFactory.LoadMenuArt("portrait_bram") },
                new DialogueManager.NamedSprite { key = "petra", sprite = AssetFactory.LoadMenuArt("portrait_petra") },
                new DialogueManager.NamedSprite { key = "sable", sprite = AssetFactory.LoadMenuArt("portrait_sable") },
                new DialogueManager.NamedSprite { key = "aldric", sprite = AssetFactory.LoadMenuArt("portrait_aldric") },
                new DialogueManager.NamedSprite { key = "nyx", sprite = AssetFactory.LoadMenuArt("portrait_nyx") },
                new DialogueManager.NamedSprite { key = "cole", sprite = AssetFactory.LoadMenuArt("portrait_cole") },
            };

            var spawnerGo = new GameObject("CustomerSpawner");
            var spawner = spawnerGo.AddComponent<CustomerSpawner>();
            spawner.spawnPoint = customerSpawn;
            spawner.enterWaypoints = new[] { wpYard, wpDoor, wpCounter };
            spawner.exitWaypoints = new[] { wpDoor, wpYard };

            Spawn(ModelFactory.WorkerPrefab, V(-1.5f, 0, -0.6f), 90f);

            // ------------------------------------------------ UI
            UiRefs ui = UiBuilder.Build(config, anvil.transform, apprentice.transform, rack.transform);

            // ------------------------------------------------ wiring
            gm.config = config;
            gm.economy = economy;
            gm.upgrades = upgrades;
            gm.expeditions = expeditions;
            gm.resources = resources;
            gm.buildings = buildings;
            gm.recipes = recipes;
            gm.production = production;
            gm.runes = runes;
            gm.talents = talents;
            gm.prestige = prestige;
            gm.quests = questManager;
            gm.achievements = achievements;
            gm.orders = orders;
            gm.rush = rush;
            gm.daily = daily;
            production.config = config;
            gm.orePile = ore.GetComponent<OrePile>();
            gm.anvil = anvil.GetComponent<AnvilStation>();
            gm.rack = rack.GetComponent<SwordRack>();
            gm.customerSpawner = spawner;
            orders.customerSpawner = spawner;
            gm.helperSpawnPoint = helperSpawn;
            gm.apprenticeAnvilRoot = apprentice;
            gm.environmentRoot = envRoot.transform;
            gm.environmentPrefabs = new GameObject[ModelFactory.EnvironmentTierPrefabs.Length];
            for (int i = 0; i < ModelFactory.EnvironmentTierPrefabs.Length; i++)
                gm.environmentPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.EnvironmentTierPrefabs[i]);
            gm.signFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(AssetFactory.FontTitlePath);

            anvil.GetComponent<AnvilStation>().progressBar = ui.anvilBar;
            var apprenticeStation = apprentice.GetComponent<AnvilStation>();
            apprenticeStation.progressBar = ui.apprenticeBar;
            rack.GetComponent<SwordRack>().stockBar = ui.rackBar;

            dialogue.ui = ui.uiManager;

            AudioSource fireAudio = forge.GetComponentInChildren<AudioSource>(true);
            if (fireAudio != null) fireAudio.clip = LoadClip("amb_fire");

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // Tapping a building in the world opens its row in the complex sheet.
            var pickerGo = new GameObject("BuildingPicker");
            var picker = pickerGo.AddComponent<BuildingPicker>();
            picker.buildings = plots;
            picker.anvil = anvil.GetComponent<AnvilStation>();
            picker.orePile = ore.GetComponent<OrePile>();
            picker.cat = cat.GetComponent<CatAmbient>();

            // The bellows is baked into the hearth mesh, so its ToolStation lives on the
            // forge root with an anchor over the bellows — every tool drives the main anvil.
            var bellows = forge.AddComponent<ToolStation>();
            bellows.kind = ToolStation.Kind.Bellows;
            bellows.cooldown = 45f;
            bellows.anchorLocal = new Vector3(-0.85f, 0.45f, 0.25f);
            bellows.scaleOnUse = false;
            bellows.breatheOnReady = false;
            var stations = Object.FindObjectsByType<ToolStation>(FindObjectsSortMode.None);
            foreach (ToolStation t in stations) t.anvil = picker.anvil;
            picker.tools = stations;

            // Camera frames the smithy plus every building that actually exists.
            cameraDirector.staticAnchors = new Transform[0];
            cameraDirector.buildings = plots;
            cameraDirector.smithyTiers = new CameraDirector.BuildingFootprint[ModelFactory.EnvironmentTierPrefabs.Length];
            for (int i = 0; i < cameraDirector.smithyTiers.Length; i++)
            {
                ModelFactory.TierDims d = ModelFactory.EnvironmentDims(i + 1);
                cameraDirector.smithyTiers[i] = new CameraDirector.BuildingFootprint
                {
                    halfWidth = d.halfWidth, frontZ = d.frontZ, backZ = d.backZ, height = d.height,
                };
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("[SceneBuilder] Shop scene saved to " + ScenePath);
        }

        static GameObject Spawn(string prefabPath, Vector3 pos, float rotY)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[SceneBuilder] Missing prefab " + prefabPath);
                return new GameObject("MISSING " + System.IO.Path.GetFileName(prefabPath));
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, rotY, 0);
            return go;
        }

        static Transform Marker(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            return go.transform;
        }

        /// <summary>A building plot: owns the id, the per-level prefabs, the facing and the sign text.</summary>
        static BuildingVisuals MakePlot(GameObject parent, string id, Vector3 pos, Vector3 lookAt, string signText)
        {
            var go = new GameObject("Plot_" + id);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;
            Vector3 dir = lookAt - pos;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                go.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            var bv = go.AddComponent<BuildingVisuals>();
            bv.buildingId = id;
            bv.levelPrefabs = new GameObject[ModelFactory.BuildingMaxLevel];
            for (int i = 0; i < bv.levelPrefabs.Length; i++)
                bv.levelPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(
                    ModelFactory.BuildingPrefabPath(id, i + 1));

            // The Smithy is the one building that always exists, so it gets no "build here" sign.
            if (id != BuildingId.Smithy)
            {
                GameObject marker = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.EmptyPlotPrefab);
                if (marker != null)
                {
                    bv.emptyMarker = (GameObject)PrefabUtility.InstantiatePrefab(marker);
                    bv.emptyMarker.transform.SetParent(go.transform, false);
                    bv.emptyMarker.transform.localPosition = Vector3.zero;
                    LabelPlotSign(bv.emptyMarker.transform, signText);
                }
            }
            return bv;
        }

        /// <summary>Paints the building's name on the survey sign, so the plot tells you what it wants to be.</summary>
        static void LabelPlotSign(Transform marker, string signText)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(AssetFactory.FontTitlePath);
            if (font == null || string.IsNullOrEmpty(signText)) return;
            var go = new GameObject("PlotName");
            go.transform.SetParent(marker, false);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = font;
            tmp.text = signText;
            tmp.fontSize = 1.35f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.55f;
            tmp.fontSizeMax = 1.35f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.28f, 0.18f, 0.10f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0.70f, 0.38f);
            go.transform.localPosition = new Vector3(0f, 1.42f, 0.10f);
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        static AudioManager.NamedClip Clip(string id, float volume)
        {
            return new AudioManager.NamedClip { id = id, clip = LoadClip(id), volume = volume };
        }

        static AudioClip LoadClip(string id)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Paths.Audio}/{id}.mp3");
            if (clip == null) clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Paths.Audio}/{id}.wav");
            return clip;
        }

        // ------------------------------------------------------------ lighting / camera / post

        static void SetupLighting()
        {
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.90f, 0.78f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.82f;
            sunGo.transform.rotation = Quaternion.LookRotation(new Vector3(0.45f, -1f, -0.35f));
            sunGo.AddComponent<Gameplay.DayCycle>();

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.65f, 0.55f);
            RenderSettings.fog = false;
        }

        static CameraDirector SetupCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.7f; // portrait: vertical half-extent, width follows aspect
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 80f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.92f, 0.85f, 0.71f);
            camGo.transform.position = new Vector3(5.5f, 10.1f, -8.9f);
            camGo.transform.LookAt(new Vector3(0.15f, 0f, -0.55f));

            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.volumeLayerMask = 1;

            camGo.AddComponent<AudioListener>();
            return camGo.AddComponent<CameraDirector>();
        }

        static void SetupPost()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, PostProfilePath);
            }
            profile.isDirty = true;

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = 0.55f;
            bloom.threshold.value = 1.05f;
            bloom.scatter.value = 0.65f;

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.value = TonemappingMode.Neutral;

            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.value = 8f;
            color.contrast.value = 6f;

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.18f;
            vignette.smoothness.value = 0.4f;

            var whiteBalance = profile.Add<WhiteBalance>(true);
            whiteBalance.temperature.value = 8f;

            EditorUtility.SetDirty(profile);

            var volGo = new GameObject("PostProcessing");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 0f;
            vol.sharedProfile = profile;
        }

        // ------------------------------------------------------------ content tables

        static QuestDef Q(string id, string title, string body, QuestGoal goal, int target,
            int gold = 0, int ore = 0, int relic = 0, int shard = 0, string targetId = "")
            => new QuestDef
            {
                id = id, title = title, body = body, goal = goal, target = target, targetId = targetId,
                goldReward = gold, oreReward = ore, relicReward = relic, shardReward = shard,
            };

        /// <summary>
        /// The spine of the game: one quest at a time, each one teaching the next system and
        /// ending at the first rekindle. Order matters — it is walked front to back.
        /// </summary>
        static QuestDef[] BuildQuests() => new[]
        {
            Q("q_forge5", "Fire and Iron", "Hammer out five swords. The anvil bar shows how far along you are.",
                QuestGoal.ForgeSwords, 5, gold: 60, ore: 5),
            Q("q_sell5", "Open for Business", "Your smith stocks the rack; customers walking the path buy from it.",
                QuestGoal.SellSwords, 5, gold: 80),
            Q("q_earn250", "First Purse", "Pile up 250 gold from sales.",
                QuestGoal.EarnGoldRun, 250, gold: 100, ore: 10),
            Q("q_mine1", "Dig Deeper", "Build the Ore Mine so the forge never runs dry while you are away.",
                QuestGoal.UpgradeBuilding, 1, gold: 150, targetId: BuildingId.Mine),
            Q("q_ore60", "Stockpile", "Hold 60 ore at once. Upgrade Ore Store if the pile keeps filling.",
                QuestGoal.ReachOre, 60, gold: 120),
            Q("q_forge25", "Production Line", "Twenty-five swords forged. The rack is the bottleneck, not the hammer.",
                QuestGoal.ForgeSwords, 25, gold: 200, ore: 20),
            Q("q_smithy2", "Room to Grow", "Grow the Smithy to level 2 and watch the shop widen.",
                QuestGoal.UpgradeBuilding, 2, gold: 250, targetId: BuildingId.Smithy),
            Q("q_iron", "Better Metal", "Unlock the Iron Sword recipe — the Smithy's second level opens it.",
                QuestGoal.UnlockRecipe, 2, gold: 200, ore: 15),
            Q("q_helper", "An Extra Pair of Hands", "Hire the apprentice and let two smiths share the work.",
                QuestGoal.HireHelper, 1, gold: 200),
            Q("q_tools", "The Smith's Tools", "The bellows stoke the hearth, the grindstone sharpens the next blade, the quench trough finishes a craft. Put them to work.",
                QuestGoal.UseTools, 5, gold: 220, ore: 10),
            Q("q_order1", "A Patron's Request", "Merchants post orders at the gate arch — deliver the swords they ask for and they pay over the counter price.",
                QuestGoal.ServeOrders, 1, gold: 250),
            Q("q_gate1", "The Way Down", "Build the Dungeon Gate. Expeditions pay out even while the app is closed.",
                QuestGoal.UpgradeBuilding, 1, gold: 300, ore: 25, targetId: BuildingId.Gate),
            Q("q_exped1", "First Blood", "Send a party through the gate and claim what they bring back.",
                QuestGoal.ClaimExpedition, 1, gold: 300, relic: 5),
            Q("q_smithy3", "The Grand Forge", "Grow the Smithy to level 3.",
                QuestGoal.UpgradeBuilding, 3, gold: 600, targetId: BuildingId.Smithy),
            Q("q_forge100", "A Hundred Blades", "Forge one hundred swords.",
                QuestGoal.ForgeSwords, 100, gold: 800, ore: 40),
            Q("q_market1", "Word of Mouth", "Open the Trading Post to pull in more customers.",
                QuestGoal.UpgradeBuilding, 1, gold: 700, targetId: BuildingId.Market),
            Q("q_rush1", "Rush Hour", "When the forge bell rings, customers flood in — finish an order while the rush lasts.",
                QuestGoal.RushOrders, 1, gold: 900),
            Q("q_mine2", "Timbered Shaft", "Take the mine to level 2 for more ore and more storage.",
                QuestGoal.UpgradeBuilding, 2, gold: 800, targetId: BuildingId.Mine),
            Q("q_silver", "Silver and Steel", "Unlock four recipes. Silver needs a deeper mine than the rest.",
                QuestGoal.UnlockRecipe, 4, gold: 1200, relic: 10),
            Q("q_gate2", "Two Parties", "Upgrade the gate so two expeditions run at the same time.",
                QuestGoal.UpgradeBuilding, 2, gold: 1500, targetId: BuildingId.Gate),
            Q("q_runes3", "Enchanted", "Raise three rune levels at the Sanctum — relic ore is for spending, not hoarding.",
                QuestGoal.BuildRunes, 3, gold: 1500, relic: 15),
            Q("q_daily3", "The Ember Tithe", "The forge pays out once a day — claim the daily ember three days running.",
                QuestGoal.ClaimDailies, 3, gold: 1600, relic: 10),
            Q("q_forge250", "Master Smith", "Forge two hundred and fifty swords.",
                QuestGoal.ForgeSwords, 250, gold: 4000, ore: 100),
            Q("q_sanctum1", "The Enchanter Arrives", "Build the Enchanter's Sanctum.",
                QuestGoal.UpgradeBuilding, 1, gold: 5000, relic: 25, targetId: BuildingId.Sanctum),
            Q("q_furnace1", "Brick and Blast", "Raise the Blast Furnace — forced air means faster forging.",
                QuestGoal.UpgradeBuilding, 1, gold: 6000, targetId: BuildingId.Furnace),
            Q("q_store", "Lock the Goods", "Raise a Storehouse — what the forge earns while you sleep should still be there when you wake.",
                QuestGoal.UpgradeBuilding, 1, gold: 2500, targetId: BuildingId.Storehouse),
            Q("q_cat5", "Nine Lives", "The forge cat keeps the shop's luck — scratch her until she purrs.",
                QuestGoal.PetCat, 5, gold: 400),
            Q("q_ember", "Lucky Catch", "A wandering ember drifts over the village now and then. Snatch it before it fades — fortune favours the quick.",
                QuestGoal.CatchEmber, 3, gold: 500),
            Q("q_mastery", "A Smith's Signature", "Master a recipe — forge it until your hands know it by heart and buyers pay a premium.",
                QuestGoal.MasterRecipe, 2, gold: 1500, shard: 1),
            Q("q_dawns", "Many Mornings", "Five dawns over the forge. The village wakes to the smell of fresh steel now.",
                QuestGoal.DaysPassed, 5, gold: 1800),
            Q("q_fair1", "Market Day", "Every fifth dawn the village holds a market fair — bunting up, crowds in, prices up too. Sell swords while the fair runs.",
                QuestGoal.FairSales, 8, gold: 2500, relic: 8),
            Q("q_smithy4", "Mithril Works", "Grow the Smithy to level 4 and unlock mithril.",
                QuestGoal.UpgradeBuilding, 4, gold: 8000, targetId: BuildingId.Smithy),
            Q("q_rare", "Something Rare", "Forge a Rare sword. Luck, the Sanctum and the Lucky Anvil all help.",
                QuestGoal.OwnRarity, 2, gold: 6000, shard: 1),
            Q("q_forge500", "Five Hundred", "Forge five hundred swords.",
                QuestGoal.ForgeSwords, 500, gold: 12000, shard: 2),
            Q("q_starforged", "Reach the Stars", "Unlock every blade the forge knows — including the Starforged, hammered from a fallen star.",
                QuestGoal.UnlockRecipe, 10, gold: 20000, shard: 3),
            Q("q_orders10", "The Guild Ledger", "Ten merchant orders fulfilled — patrons remember a smith who delivers.",
                QuestGoal.ServeOrders, 10, gold: 9000, shard: 2),
            Q("q_prestige", "Rekindle the Forge", "Burn this run for ember shards and start again, permanently stronger.",
                QuestGoal.Prestige, 1, shard: 5),
            Q("q_smithy5", "Dragonforge", "Take the Smithy all the way to level 5.",
                QuestGoal.UpgradeBuilding, 5, gold: 20000, shard: 5, targetId: BuildingId.Smithy),
            Q("q_legendary", "Legend of the Forge", "Forge a Legendary sword.",
                QuestGoal.OwnRarity, 4, shard: 12),
            Q("q_forge2000", "The Endless Anvil", "Two thousand swords. The forge never sleeps.",
                QuestGoal.ForgeSwords, 2000, shard: 30),
        };

        static AchievementDef A(string id, string name, string desc, QuestGoal goal, int target,
            AchBonus kind, float bonus, string icon, string targetId = "")
            => new AchievementDef
            {
                id = id, displayName = name, description = desc, goal = goal, target = target,
                targetId = targetId, bonusKind = kind, bonus = bonus, icon = AssetFactory.LoadIcon(icon),
            };

        /// <summary>Lifetime milestones. Each one leaves a small permanent bonus on the forge.</summary>
        static AchievementDef[] BuildAchievements() => new[]
        {
            A("a_first_sword", "First Strike", "Forge your first sword", QuestGoal.ForgeSwords, 1, AchBonus.Gold, 0.02f, "craft"),
            A("a_forge50", "Working Smithy", "Forge 50 swords", QuestGoal.ForgeSwords, 50, AchBonus.Gold, 0.03f, "craft"),
            A("a_forge500", "Veteran Smith", "Forge 500 swords", QuestGoal.ForgeSwords, 500, AchBonus.Gold, 0.04f, "craft"),
            A("a_sell100", "Shopkeeper", "Sell 100 swords", QuestGoal.SellSwords, 100, AchBonus.Price, 0.03f, "coin"),
            A("a_sell1000", "Guild Merchant", "Sell 1000 swords", QuestGoal.SellSwords, 1000, AchBonus.Price, 0.05f, "coin"),
            A("a_gold10k", "Ten Thousand", "Earn 10,000 gold in one run", QuestGoal.EarnGoldRun, 10000, AchBonus.Gold, 0.03f, "coin"),
            A("a_gold1m", "Gold Hoard", "Earn 1,000,000 gold in one run", QuestGoal.EarnGoldRun, 1000000, AchBonus.Gold, 0.06f, "chest"),
            A("a_ore250", "Stockpiled", "Hold 250 ore at once", QuestGoal.ReachOre, 250, AchBonus.Ore, 0.04f, "ore"),
            A("a_mine3", "Deep Miner", "Raise the Ore Mine to level 3", QuestGoal.UpgradeBuilding, 3, AchBonus.Ore, 0.05f, "mine", BuildingId.Mine),
            A("a_smithy5", "Dragonforge Built", "Raise the Smithy to level 5", QuestGoal.UpgradeBuilding, 5, AchBonus.Price, 0.05f, "smithy", BuildingId.Smithy),
            A("a_market5", "Grand Bazaar", "Raise the Trading Post to level 5", QuestGoal.UpgradeBuilding, 5, AchBonus.Gold, 0.05f, "market", BuildingId.Market),
            A("a_gate5", "Abyssal Gate", "Raise the Dungeon Gate to level 5", QuestGoal.UpgradeBuilding, 5, AchBonus.Offline, 0.05f, "gate", BuildingId.Gate),
            A("a_exp25", "Seasoned Party", "Complete 25 expeditions", QuestGoal.ClaimExpedition, 25, AchBonus.Offline, 0.05f, "dungeon"),
            A("a_exp100", "Dungeon Delver", "Complete 100 expeditions", QuestGoal.ClaimExpedition, 100, AchBonus.Offline, 0.05f, "dungeon"),
            A("a_rare", "Rare Find", "Forge a Rare sword", QuestGoal.OwnRarity, 2, AchBonus.Luck, 1f, "gem"),
            A("a_epic", "Epic Forge", "Forge an Epic sword", QuestGoal.OwnRarity, 3, AchBonus.Luck, 1.5f, "gem"),
            A("a_legendary", "Legendary", "Forge a Legendary sword", QuestGoal.OwnRarity, 4, AchBonus.Luck, 2f, "trophy"),
            A("a_runes20", "Rune Carver", "Raise 20 rune levels", QuestGoal.BuildRunes, 20, AchBonus.Craft, 0.05f, "rune"),
            A("a_runes60", "Rune Master", "Raise 60 rune levels", QuestGoal.BuildRunes, 60, AchBonus.Craft, 0.06f, "rune"),
            A("a_helper", "Not Alone", "Hire the apprentice", QuestGoal.HireHelper, 1, AchBonus.Craft, 0.03f, "helper"),
            A("a_prestige1", "Reborn in Ember", "Rekindle the forge once", QuestGoal.Prestige, 1, AchBonus.Gold, 0.05f, "ember"),
            A("a_prestige5", "Phoenix Smith", "Rekindle the forge five times", QuestGoal.Prestige, 5, AchBonus.Gold, 0.08f, "ember"),
            A("a_play60", "Dedicated", "Play for an hour", QuestGoal.PlayMinutes, 60, AchBonus.Offline, 0.03f, "offline"),
            A("a_play600", "Forge Never Sleeps", "Play for ten hours", QuestGoal.PlayMinutes, 600, AchBonus.Gold, 0.04f, "offline"),
            A("a_orders10", "Guild Contractor", "Complete 10 merchant orders", QuestGoal.ServeOrders, 10, AchBonus.Price, 0.03f, "scroll"),
            A("a_orders50", "Quartermaster", "Complete 50 merchant orders", QuestGoal.ServeOrders, 50, AchBonus.Gold, 0.05f, "scroll"),
            A("a_rush10", "Rush Master", "Complete 10 orders during Rush Hour", QuestGoal.RushOrders, 10, AchBonus.Craft, 0.04f, "star"),
            A("a_daily7", "Faithful", "Claim the daily ember 7 times", QuestGoal.ClaimDailies, 7, AchBonus.Offline, 0.03f, "star"),
            A("a_daily30", "Ember Devout", "Claim the daily ember 30 times", QuestGoal.ClaimDailies, 30, AchBonus.Gold, 0.05f, "ember"),
            A("a_furnace5", "Volcanic Heart", "Raise the Blast Furnace to level 5", QuestGoal.UpgradeBuilding, 5, AchBonus.Craft, 0.05f, "furnace", BuildingId.Furnace),
            A("a_store3", "Hoard Master", "Raise the Storehouse to level 3", QuestGoal.UpgradeBuilding, 3, AchBonus.Offline, 0.05f, "chest", BuildingId.Storehouse),
            A("a_cat10", "Cat Person", "Pet the forge cat 10 times", QuestGoal.PetCat, 10, AchBonus.Luck, 0.5f, "cat"),
            A("a_tools", "Tool Time", "Use the bellows, grindstone and quench trough 25 times", QuestGoal.UseTools, 25, AchBonus.Craft, 0.03f, "craft"),
            A("a_tools100", "Hand and Hammer", "Use the workbench tools 100 times", QuestGoal.UseTools, 100, AchBonus.Luck, 0.5f, "gem"),
            A("a_ember10", "Ember Hunter", "Catch 10 lucky embers", QuestGoal.CatchEmber, 10, AchBonus.Luck, 0.5f, "ember"),
            A("a_ember50", "Sprite Whisperer", "Catch 50 lucky embers", QuestGoal.CatchEmber, 50, AchBonus.Luck, 1f, "star"),
            A("a_master3", "Signature Blade", "Reach mastery tier 3 on any recipe", QuestGoal.MasterRecipe, 3, AchBonus.Price, 0.03f, "craft"),
            A("a_master5", "Grandmaster Smith", "Reach mastery tier 5 on any recipe", QuestGoal.MasterRecipe, 5, AchBonus.Price, 0.05f, "trophy"),
            A("a_dawns15", "Seasoned Hearth", "See 15 dawns over the forge", QuestGoal.DaysPassed, 15, AchBonus.Offline, 0.05f, "star"),
            A("a_fair25", "Fair Favorite", "Sell 25 swords on market-fair days", QuestGoal.FairSales, 25, AchBonus.Gold, 0.05f, "coin"),
        };

        // ------------------------------------------------------------ config asset

        static GameConfig EnsureConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(cfg, ConfigPath);
            }

            cfg.swordPrice = 10;
            cfg.helperCost = 300;
            cfg.baseMoveSpeed = 2.3f;
            cfg.pickupDuration = 0.7f;
            cfg.depositDuration = 0.35f;
            cfg.baseCraftDuration = 3.2f;
            cfg.minCraftDuration = 0.8f;
            cfg.customerMoveSpeed = 1.8f;
            cfg.customerLeaveSpeedMultiplier = 1.15f;
            cfg.minCustomerInterval = 4.5f;
            cfg.maxCustomerInterval = 9f;
            cfg.baseRackCapacity = 4;

            cfg.baseOrePerSecond = 0.45f;
            cfg.baseOreCapacity = 40;
            cfg.offlineRate = 0.5f;
            cfg.offlineCapHours = 8f;

            cfg.prestigeGoldDivisor = 5000f;
            cfg.prestigeShardScale = 6f;
            cfg.prestigeExponent = 0.6f;

            cfg.relicOrePriceBonus = 0.05f;
            cfg.relicOreMaxBonusCount = 40;

            cfg.expeditions = new[]
            {
                new ExpeditionDef
                {
                    id = "goblin_caves", displayName = "Goblin Caves",
                    description = "A quick delve for shiny scraps",
                    durationSeconds = 120, goldReward = 60, relicOreReward = 2, requiredGateLevel = 1,
                },
                new ExpeditionDef
                {
                    id = "ember_depths", displayName = "Ember Depths",
                    description = "Warm tunnels, richer seams",
                    durationSeconds = 480, goldReward = 220, relicOreReward = 6, requiredGateLevel = 2,
                },
                new ExpeditionDef
                {
                    id = "dragons_hoard", displayName = "Dragon's Hoard",
                    description = "A long raid beneath the mountain",
                    durationSeconds = 1800, goldReward = 900, relicOreReward = 15, requiredGateLevel = 3,
                },
                new ExpeditionDef
                {
                    id = "frozen_deep", displayName = "The Frozen Deep",
                    description = "Ice-bound halls where old kings sleep",
                    durationSeconds = 3600, goldReward = 2600, relicOreReward = 30, requiredGateLevel = 4,
                },
                new ExpeditionDef
                {
                    id = "abyssal_throne", displayName = "Abyssal Throne",
                    description = "The bottom of the world. Few return, all return rich",
                    durationSeconds = 7200, goldReward = 8000, relicOreReward = 70, requiredGateLevel = 5,
                },
            };

            cfg.splashArt = AssetFactory.LoadMenuArt("splash");
            cfg.emblemArt = AssetFactory.LoadMenuArt("emblem");
            cfg.dungeonArt = AssetFactory.LoadMenuArt("dungeon_bg");
            cfg.onboardingArt = new[]
            {
                AssetFactory.LoadMenuArt("onboard_forge"),
                AssetFactory.LoadMenuArt("onboard_shop"),
                AssetFactory.LoadMenuArt("onboard_dungeon"),
            };

            cfg.upgrades = new[]
            {
                new UpgradeDef
                {
                    id = UpgradeId.Craft, displayName = "Forge Mastery",
                    description = "Craft swords faster", icon = AssetFactory.LoadIcon("craft"),
                    maxLevel = 10, baseCost = 50, costGrowth = 1.8f,
                },
                new UpgradeDef
                {
                    id = UpgradeId.Carry, displayName = "Swift Boots",
                    description = "Workers move faster", icon = AssetFactory.LoadIcon("carry"),
                    maxLevel = 10, baseCost = 45, costGrowth = 1.75f,
                },
                new UpgradeDef
                {
                    id = UpgradeId.Rack, displayName = "Weapon Rack",
                    description = "+2 sword slots per level", icon = AssetFactory.LoadIcon("rack"),
                    maxLevel = 6, baseCost = 75, costGrowth = 2.0f,
                },
                new UpgradeDef
                {
                    id = UpgradeId.OreCap, displayName = "Ore Store",
                    description = "Hold more ore so the forge never stalls", icon = AssetFactory.LoadIcon("ore"),
                    maxLevel = 8, baseCost = 60, costGrowth = 1.9f,
                },
                new UpgradeDef
                {
                    id = UpgradeId.Luck, displayName = "Lucky Anvil",
                    description = "Better odds of forging rare swords", icon = AssetFactory.LoadIcon("star"),
                    maxLevel = 8, baseCost = 150, costGrowth = 2.05f,
                },
                new UpgradeDef
                {
                    id = UpgradeId.Charm, displayName = "Shop Charm",
                    description = "Customers arrive more often", icon = AssetFactory.LoadIcon("market"),
                    maxLevel = 8, baseCost = 120, costGrowth = 2.0f,
                },
            };

            cfg.buildings = new[]
            {
                new BuildingDef
                {
                    id = BuildingId.Smithy, displayName = "The Smithy", startLevel = 1, maxLevel = 5,
                    description = "Your forge. Each level widens the shop, adds rack space and raises every price.",
                    icon = AssetFactory.LoadIcon("smithy"),
                    levelCosts = new[] { 250, 1500, 8000, 40000 },
                    levelPerks = new[]
                    {
                        "Roadside Stall — +2 rack, +15% prices",
                        "Village Smithy — +2 rack, +15%, Iron",
                        "Grand Forge — +2 rack, +15%, Steel",
                        "Mithril Works — +2 rack, +15%, Mithril",
                        "Dragonforge — +2 rack, +15%, Dragonsteel",
                    },
                    priceBonus = 0.15f, rackBonus = 2,
                },
                new BuildingDef
                {
                    id = BuildingId.Mine, displayName = "Ore Mine", startLevel = 0, maxLevel = 5,
                    description = "Miners feed the forge while you are away. No ore, no swords.",
                    icon = AssetFactory.LoadIcon("mine"),
                    levelCosts = new[] { 180, 900, 4200, 20000, 90000 },
                    levelPerks = new[]
                    {
                        "Prospector's Dig — +0.6 ore/s",
                        "Timbered Shaft — +0.6 ore/s, +25 ore storage",
                        "Deep Seam — +0.6 ore/s, +25 storage, Silver unlocked",
                        "Crystal Vein — +0.6 ore/s, +25 storage, Mithril unlocked",
                        "Rich Core — +0.6 ore/s, +25 storage, Dragonsteel unlocked",
                    },
                    orePerSecond = 0.6f, oreCapacity = 25,
                },
                new BuildingDef
                {
                    id = BuildingId.Market, displayName = "Trading Post", startLevel = 0, maxLevel = 5,
                    description = "A busy storefront pulls in more customers and better offers.",
                    icon = AssetFactory.LoadIcon("market"),
                    levelCosts = new[] { 220, 1100, 5200, 26000, 110000 },
                    levelPerks = new[]
                    {
                        "Market Stall — customers arrive 7% faster",
                        "Open Front — 14% faster, +6% prices",
                        "Trade Sign — 21% faster, +12% prices",
                        "Merchant Guild — 28% faster, +18% prices",
                        "Grand Bazaar — 35% faster, +24% prices",
                    },
                    customerIntervalCut = 0.07f, priceBonus = 0.06f,
                },
                new BuildingDef
                {
                    id = BuildingId.Gate, displayName = "Dungeon Gate", startLevel = 0, maxLevel = 5,
                    description = "Opens the way below — parties return with gold and relic ore.",
                    icon = AssetFactory.LoadIcon("gate"),
                    levelCosts = new[] { 400, 2200, 11000, 55000, 240000 },
                    levelPerks = new[]
                    {
                        "Sealed Arch — adventurers can delve",
                        "Warded Gate — +1 expedition at a time",
                        "Deep Passage — +1 expedition, +15% rewards",
                        "Rift Mouth — +1 expedition, +30% rewards",
                        "Abyssal Gate — +1 expedition, +45% rewards",
                    },
                    expeditionSlots = 1, dungeonRewardBonus = 0.15f,
                },
                new BuildingDef
                {
                    id = BuildingId.Sanctum, displayName = "Enchanter's Sanctum", startLevel = 0, maxLevel = 5,
                    description = "Spend relic ore on runes that sharpen every part of the forge.",
                    icon = AssetFactory.LoadIcon("sanctum"),
                    levelCosts = new[] { 900, 5000, 24000, 120000, 500000 },
                    levelPerks = new[]
                    {
                        "Rune Circle — runes up to level 4",
                        "Stone Altar — runes up to level 8, costs -8%",
                        "Crystal Font — runes up to level 12, costs -16%",
                        "Astral Chamber — runes up to level 16, costs -24%",
                        "Worldheart — runes up to level 20, costs -32%",
                    },
                    runeLevelsPerTier = 4, runeCostCut = 0.08f,
                },
                new BuildingDef
                {
                    id = BuildingId.Furnace, displayName = "Blast Furnace", startLevel = 0, maxLevel = 5,
                    description = "Roaring forced-draft heat. Every level hammers craft time down.",
                    icon = AssetFactory.LoadIcon("furnace"),
                    levelCosts = new[] { 300, 1600, 7000, 32000, 140000 },
                    levelPerks = new[]
                    {
                        "Brick Stack — forging 8% faster",
                        "Twin Bellows — forging 16% faster",
                        "Coke Furnace — forging 24% faster",
                        "Blast Chamber — forging 32% faster",
                        "Volcanic Heart — forging 40% faster",
                    },
                    craftSpeedCut = 0.08f,
                },
                new BuildingDef
                {
                    id = BuildingId.Storehouse, displayName = "Storehouse", startLevel = 0, maxLevel = 5,
                    description = "Guarded cargo — every level banks more of your offline earnings.",
                    icon = AssetFactory.LoadIcon("chest"),
                    levelCosts = new[] { 450, 2400, 12000, 60000, 260000 },
                    levelPerks = new[]
                    {
                        "Lean-To — offline earnings +20%",
                        "Timber Shed — offline earnings +40%",
                        "Cargo Shed — offline earnings +60%",
                        "Warehouse — offline earnings +80%",
                        "Grand Depot — offline earnings +100%",
                    },
                    offlineBonus = 0.20f,
                },
            };

            cfg.recipes = new[]
            {
                new RecipeDef
                {
                    id = RecipeId.Copper, displayName = "Copper Dagger", oreCost = 1, baseValue = 10,
                    craftDuration = 3.2f, requiredSmithyLevel = 1, requiredMineLevel = 0,
                    description = "Cheap, quick and always in demand.",
                    icon = AssetFactory.LoadIcon("copper"),
                    swordPrefab = ModelFactory.SwordPrefabFor(RecipeId.Copper),
                },
                new RecipeDef
                {
                    id = RecipeId.Iron, displayName = "Iron Sword", oreCost = 2, baseValue = 26,
                    craftDuration = 4.0f, requiredSmithyLevel = 2, requiredMineLevel = 0,
                    description = "A soldier's honest blade.",
                    icon = AssetFactory.LoadIcon("iron"),
                    swordPrefab = ModelFactory.SwordPrefabFor(RecipeId.Iron),
                },
                new RecipeDef
                {
                    id = RecipeId.Steel, displayName = "Steel Sabre", oreCost = 4, baseValue = 70,
                    craftDuration = 5.0f, requiredSmithyLevel = 3, requiredMineLevel = 0,
                    description = "Folded steel, keen and bright.",
                    icon = AssetFactory.LoadIcon("steel"),
                    swordPrefab = ModelFactory.SwordPrefabFor(RecipeId.Steel),
                },
                new RecipeDef
                {
                    id = RecipeId.EmberAxe, displayName = "Ember Waraxe", oreCost = 6, baseValue = 120,
                    craftDuration = 5.8f, requiredSmithyLevel = 3, requiredMineLevel = 1,
                    description = "An ember-forged crescent that never cools.",
                    icon = AssetFactory.LoadIcon("emberaxe"),
                    swordPrefab = ModelFactory.SwordPrefabFor(RecipeId.EmberAxe),
                },
                new RecipeDef
                {
                    id = RecipeId.Silver, displayName = "Silver Rapier", oreCost = 7, baseValue = 190,
                    craftDuration = 6.0f, requiredSmithyLevel = 3, requiredMineLevel = 2,
                    description = "Bites deep into things that haunt the dark.",
                    icon = AssetFactory.LoadIcon("silver"),
                    swordPrefab = ModelFactory.SwordPrefabFor(RecipeId.Silver),
                },
                new RecipeDef
                {
                    id = RecipeId.Frostbrand, displayName = "Frostbrand", oreCost = 10, baseValue = 330,
                    craftDuration = 8.2f, requiredSmithyLevel = 4, requiredMineLevel = 3,
                    description = "Forged cold and quenched in glacier water.",
                    icon = AssetFactory.LoadIcon("frostbrand"),
                    swordPrefab = ModelFactory.SwordPrefabFor(RecipeId.Frostbrand),
                },
                new RecipeDef
                {
                    id = RecipeId.Mithril, displayName = "Mithril Katana", oreCost = 12, baseValue = 520,
                    craftDuration = 7.5f, requiredSmithyLevel = 4, requiredMineLevel = 3,
                    description = "Light as air, hard as dawn.",
                    icon = AssetFactory.LoadIcon("mithril"),
                    swordPrefab = ModelFactory.SwordPrefabFor(RecipeId.Mithril),
                },
                new RecipeDef
                {
                    id = RecipeId.Dragonsteel, displayName = "Dragonsteel Greatsword", oreCost = 20, baseValue = 1500,
                    craftDuration = 9.0f, requiredSmithyLevel = 5, requiredMineLevel = 4,
                    description = "Quenched in dragonfire. Nothing survives it.",
                    icon = AssetFactory.LoadIcon("dragonsteel"),
                    swordPrefab = ModelFactory.SwordPrefabFor(RecipeId.Dragonsteel),
                },
                new RecipeDef
                {
                    id = RecipeId.Voidreaver, displayName = "Voidreaver", oreCost = 30, baseValue = 2600,
                    craftDuration = 10.5f, requiredSmithyLevel = 5, requiredMineLevel = 5,
                    description = "A blade that drinks the light around it.",
                    icon = AssetFactory.LoadIcon("voidreaver"),
                    swordPrefab = ModelFactory.SwordPrefabFor(RecipeId.Voidreaver),
                },
                new RecipeDef
                {
                    id = RecipeId.Starforged, displayName = "Starforged", oreCost = 45, baseValue = 5200,
                    craftDuration = 12f, requiredSmithyLevel = 5, requiredMineLevel = 5,
                    description = "Hammered from a fallen star — the last word in blades.",
                    icon = AssetFactory.LoadIcon("starforged"),
                    swordPrefab = ModelFactory.SwordPrefabFor(RecipeId.Starforged),
                },
            };

            cfg.runes = new[]
            {
                new RuneDef
                {
                    id = RuneId.Flame, displayName = "Rune of Flame", baseCost = 8, costGrowth = 1.55f,
                    effectPerLevel = 0.06f, baseMaxLevel = 20,
                    description = "+6% to every sword price per level",
                    icon = AssetFactory.LoadIcon("rune"),
                },
                new RuneDef
                {
                    id = RuneId.Haste, displayName = "Rune of Haste", baseCost = 10, costGrowth = 1.6f,
                    effectPerLevel = 0.03f, baseMaxLevel = 20,
                    description = "-3% forging time per level",
                    icon = AssetFactory.LoadIcon("craft"),
                },
                new RuneDef
                {
                    id = RuneId.Fortune, displayName = "Rune of Fortune", baseCost = 14, costGrowth = 1.65f,
                    effectPerLevel = 1f, baseMaxLevel = 20,
                    description = "+1 luck per level, tilting the odds toward rare swords",
                    icon = AssetFactory.LoadIcon("star"),
                },
                new RuneDef
                {
                    id = RuneId.Wealth, displayName = "Rune of Wealth", baseCost = 9, costGrowth = 1.55f,
                    effectPerLevel = 0.10f, baseMaxLevel = 20,
                    description = "+10% ore mined per level",
                    icon = AssetFactory.LoadIcon("ore"),
                },
            };

            cfg.talents = new[]
            {
                new TalentDef
                {
                    id = TalentId.EmberHeat, displayName = "Ember Heat", kind = TalentKind.GoldBonus,
                    effectPerLevel = 0.10f, baseCost = 1, costGrowth = 2f, maxLevel = 5,
                    description = "+10% gold from every sale",
                    icon = AssetFactory.LoadIcon("ember"),
                },
                new TalentDef
                {
                    id = TalentId.DeepVeins, displayName = "Deep Veins", kind = TalentKind.OreRate,
                    effectPerLevel = 0.15f, baseCost = 1, costGrowth = 2f, maxLevel = 5,
                    description = "+15% ore mined",
                    icon = AssetFactory.LoadIcon("mine"),
                },
                new TalentDef
                {
                    id = TalentId.QuickHands, displayName = "Quick Hands", kind = TalentKind.CraftSpeed,
                    effectPerLevel = 0.05f, baseCost = 2, costGrowth = 2.1f, maxLevel = 5,
                    description = "-5% forging time",
                    icon = AssetFactory.LoadIcon("craft"),
                },
                new TalentDef
                {
                    id = TalentId.SilverTongue, displayName = "Silver Tongue", kind = TalentKind.PriceBonus,
                    effectPerLevel = 0.08f, baseCost = 2, costGrowth = 2.1f, maxLevel = 5,
                    description = "+8% to every sword price",
                    icon = AssetFactory.LoadIcon("silver"),
                },
                new TalentDef
                {
                    id = TalentId.LuckyStrike, displayName = "Lucky Strike", kind = TalentKind.Luck,
                    effectPerLevel = 1.5f, baseCost = 3, costGrowth = 2.2f, maxLevel = 5,
                    description = "+1.5 luck on every forge",
                    icon = AssetFactory.LoadIcon("star"),
                },
                new TalentDef
                {
                    id = TalentId.NightShift, displayName = "Night Shift", kind = TalentKind.OfflineRate,
                    effectPerLevel = 0.07f, baseCost = 3, costGrowth = 2.2f, maxLevel = 5,
                    description = "+7% offline production rate",
                    icon = AssetFactory.LoadIcon("offline"),
                },
                new TalentDef
                {
                    id = TalentId.MasterMerchant, displayName = "Master Merchant", kind = TalentKind.CustomerRate,
                    effectPerLevel = 0.10f, baseCost = 2, costGrowth = 2f, maxLevel = 5,
                    description = "+10% customer frequency",
                    icon = AssetFactory.LoadIcon("market"),
                },
                new TalentDef
                {
                    id = TalentId.RichVeins, displayName = "Rich Veins", kind = TalentKind.OreCapacity,
                    effectPerLevel = 20f, baseCost = 2, costGrowth = 2f, maxLevel = 5,
                    description = "+20 ore storage",
                    icon = AssetFactory.LoadIcon("ore"),
                },
                new TalentDef
                {
                    id = TalentId.GuildContacts, displayName = "Guild Contacts", kind = TalentKind.ExpeditionSpeed,
                    effectPerLevel = 0.08f, baseCost = 3, costGrowth = 2.1f, maxLevel = 5,
                    description = "-8% expedition duration",
                    icon = AssetFactory.LoadIcon("gate"),
                },
                new TalentDef
                {
                    id = TalentId.RuneMastery, displayName = "Rune Mastery", kind = TalentKind.RunePower,
                    effectPerLevel = 0.12f, baseCost = 4, costGrowth = 2.3f, maxLevel = 5,
                    description = "+12% to every rune effect",
                    icon = AssetFactory.LoadIcon("rune"),
                },
            };

            cfg.quests = BuildQuests();
            cfg.achievements = BuildAchievements();

            cfg.rarityGems = new Material[RarityInfo.Count];
            for (int i = 0; i < RarityInfo.Count; i++)
                cfg.rarityGems[i] = AssetDatabase.LoadAssetAtPath<Material>(
                    ModelFactory.GemMaterialPath((Rarity)i));
            cfg.raritySparklePrefab = AssetDatabase.LoadAssetAtPath<ParticleSystem>(ModelFactory.SparklePrefab);

            cfg.workerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.WorkerPrefab);
            cfg.helperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.HelperPrefab);
            cfg.customerPrefabA = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.CustomerAPrefab);
            cfg.customerPrefabB = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.CustomerBPrefab);
            cfg.customerPrefabC = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.CustomerCPrefab);
            cfg.customerPrefabD = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.CustomerDPrefab);
            cfg.vendorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.VendorPrefab);
            cfg.mysticPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.MysticPrefab);
            cfg.stokerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.StokerPrefab);
            cfg.swordPrefab = ModelFactory.SwordPrefabFor(RecipeId.Copper) ?? AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.SwordPrefab);
            cfg.oreChunkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.OreChunkPrefab);

            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            return cfg;
        }
    }
}
