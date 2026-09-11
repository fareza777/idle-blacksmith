using IdleBlacksmith.Core;
using IdleBlacksmith.Gameplay;
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
            SetupCamera();
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
            Spawn(ModelFactory.RugPrefab, V(0.1f, 0.005f, 0.55f), 8f);
            Spawn(ModelFactory.SignPostPrefab, V(3.3f, 0, -4.6f), 160f);

            Transform helperSpawn = Marker("HelperSpawn", V(1.3f, 0, 0.85f));
            // customer flow: along the yard path -> front door -> the counter
            Transform wpYard = Marker("WpYard", V(2.4f, 0, -4.55f));
            Transform wpDoor = Marker("WpDoor", V(0.8f, 0, -4.1f));
            Transform wpCounter = Marker("WpCounter", V(0.8f, 0, -2.8f));
            Transform customerSpawn = Marker("CustomerSpawn", V(6.4f, 0, -5.7f));

            // ------------------------------------------------ systems
            var gameGo = new GameObject("Game");
            var economy = gameGo.AddComponent<EconomyManager>();
            var upgrades = gameGo.AddComponent<UpgradeManager>();
            var expeditions = gameGo.AddComponent<ExpeditionManager>();
            var gm = gameGo.AddComponent<GameManager>();

            var audioGo = new GameObject("Audio");
            var audio = audioGo.AddComponent<AudioManager>();
            audio.clips = new[]
            {
                Clip("hammer", 0.9f), Clip("coin", 0.9f), Clip("pop", 0.8f),
                Clip("upgrade", 0.9f), Clip("hire", 1f), Clip("denied", 0.7f),
                Clip("fanfare", 0.9f),
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
            gm.orePile = ore.GetComponent<OrePile>();
            gm.anvil = anvil.GetComponent<AnvilStation>();
            gm.rack = rack.GetComponent<SwordRack>();
            gm.customerSpawner = spawner;
            gm.helperSpawnPoint = helperSpawn;
            gm.apprenticeAnvilRoot = apprentice;
            gm.environmentRoot = envRoot.transform;
            gm.environmentPrefabs = new GameObject[ModelFactory.EnvironmentTierPrefabs.Length];
            for (int i = 0; i < ModelFactory.EnvironmentTierPrefabs.Length; i++)
                gm.environmentPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.EnvironmentTierPrefabs[i]);

            anvil.GetComponent<AnvilStation>().progressBar = ui.anvilBar;
            var apprenticeStation = apprentice.GetComponent<AnvilStation>();
            apprenticeStation.progressBar = ui.apprenticeBar;
            rack.GetComponent<SwordRack>().stockBar = ui.rackBar;

            AudioSource fireAudio = forge.GetComponentInChildren<AudioSource>(true);
            if (fireAudio != null) fireAudio.clip = LoadClip("crackle");

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

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

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.65f, 0.55f);
            RenderSettings.fog = false;
        }

        static void SetupCamera()
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
            camGo.AddComponent<CameraBreath>();
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

            cfg.expandCosts = new[] { 250, 1500 };
            cfg.tierPriceBonus = 0.15f;
            cfg.tierRackBonus = 2;
            cfg.relicOrePriceBonus = 0.05f;
            cfg.relicOreMaxBonusCount = 40;

            cfg.expeditions = new[]
            {
                new ExpeditionDef
                {
                    id = "goblin_caves", displayName = "Goblin Caves",
                    description = "A quick delve for shiny scraps",
                    durationSeconds = 120, goldReward = 60, relicOreReward = 2,
                },
                new ExpeditionDef
                {
                    id = "ember_depths", displayName = "Ember Depths",
                    description = "Warm tunnels, richer seams",
                    durationSeconds = 480, goldReward = 220, relicOreReward = 6,
                },
                new ExpeditionDef
                {
                    id = "dragons_hoard", displayName = "Dragon's Hoard",
                    description = "A long raid beneath the mountain",
                    durationSeconds = 1800, goldReward = 900, relicOreReward = 15,
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
                    id = UpgradeManager.CraftId, displayName = "Forge Mastery",
                    description = "Craft swords faster", icon = AssetFactory.LoadIcon("craft"),
                    maxLevel = 10, baseCost = 50, costGrowth = 1.8f,
                },
                new UpgradeDef
                {
                    id = UpgradeManager.CarryId, displayName = "Swift Boots",
                    description = "Workers move faster", icon = AssetFactory.LoadIcon("carry"),
                    maxLevel = 10, baseCost = 45, costGrowth = 1.75f,
                },
                new UpgradeDef
                {
                    id = UpgradeManager.RackId, displayName = "Weapon Rack",
                    description = "+2 sword slots per level", icon = AssetFactory.LoadIcon("rack"),
                    maxLevel = 6, baseCost = 75, costGrowth = 2.0f,
                },
            };

            cfg.workerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.WorkerPrefab);
            cfg.helperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.HelperPrefab);
            cfg.customerPrefabA = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.CustomerAPrefab);
            cfg.customerPrefabB = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.CustomerBPrefab);
            cfg.swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.SwordPrefab);
            cfg.oreChunkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFactory.OreChunkPrefab);

            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            return cfg;
        }
    }
}
