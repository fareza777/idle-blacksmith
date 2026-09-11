using IdleBlacksmith.Gameplay;
using UnityEditor;
using UnityEngine;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>Stations, props and the merged shop environment mesh.</summary>
    public static partial class ModelFactory
    {
        public const string AnvilPrefab = Paths.Prefabs + "/Anvil.prefab";
        public const string ApprenticeAnvilPrefab = Paths.Prefabs + "/ApprenticeAnvil.prefab";
        public const string OrePilePrefab = Paths.Prefabs + "/OrePile.prefab";
        public const string SwordRackPrefab = Paths.Prefabs + "/SwordRack.prefab";
        public const string ForgePrefab = Paths.Prefabs + "/Forge.prefab";
        public const string EnvironmentPrefabT1 = Paths.Prefabs + "/Environment_T1.prefab";
        public const string EnvironmentPrefabT2 = Paths.Prefabs + "/Environment_T2.prefab";
        public const string EnvironmentPrefabT3 = Paths.Prefabs + "/Environment_T3.prefab";
        public const string EnvironmentPrefabT4 = Paths.Prefabs + "/Environment_T4.prefab";
        public const string EnvironmentPrefabT5 = Paths.Prefabs + "/Environment_T5.prefab";
        /// <summary>Smithy growth stages, index 0 = level 1 (smallest).</summary>
        public static readonly string[] EnvironmentTierPrefabs =
        {
            EnvironmentPrefabT1, EnvironmentPrefabT2, EnvironmentPrefabT3, EnvironmentPrefabT4, EnvironmentPrefabT5,
        };
        public const string CounterPrefab = Paths.Prefabs + "/Counter.prefab";
        public const string BarrelPrefab = Paths.Prefabs + "/Barrel.prefab";
        public const string CratePrefab = Paths.Prefabs + "/Crate.prefab";
        public const string StoolPrefab = Paths.Prefabs + "/Stool.prefab";
        public const string PlantPrefab = Paths.Prefabs + "/Plant.prefab";
        public const string ShelfPrefab = Paths.Prefabs + "/Shelf.prefab";
        public const string BannerPrefab = Paths.Prefabs + "/Banner.prefab";
        public const string RugPrefab = Paths.Prefabs + "/Rug.prefab";
        public const string SignPostPrefab = Paths.Prefabs + "/SignPost.prefab";

        // ------------------------------------------------------------ stations

        static void BuildStations()
        {
            BuildAnvil();
            BuildOrePile();
            BuildSwordRack();
            BuildForge();
            BuildProps();
        }

        static void AddAnvilGeometry(MeshBuilder b, float s)
        {
            b.Cylinder(new Vector3(0, 0.175f * s, 0), 0.26f * s, 0.35f * s, 8, Palette.WoodMid);
            b.Box(new Vector3(0, 0.40f * s, 0), new Vector3(0.36f, 0.10f, 0.26f) * s, Palette.MetalDark);
            b.Box(new Vector3(0, 0.51f * s, 0), new Vector3(0.22f, 0.12f, 0.20f) * s, Palette.MetalDark);
            b.Box(new Vector3(0, 0.62f * s, 0), new Vector3(0.55f, 0.10f, 0.22f) * s, Palette.MetalLight);
            // stepped horn
            b.Box(new Vector3(0.30f * s, 0.615f * s, 0), new Vector3(0.10f, 0.09f, 0.14f) * s, Palette.MetalLight);
            b.Box(new Vector3(0.385f * s, 0.60f * s, 0), new Vector3(0.09f, 0.07f, 0.11f) * s, Palette.MetalLight);
            b.Box(new Vector3(0.46f * s, 0.585f * s, 0), new Vector3(0.07f, 0.05f, 0.085f) * s, Palette.MetalLight);
            // heel
            b.Box(new Vector3(-0.30f * s, 0.615f * s, 0), new Vector3(0.12f, 0.09f, 0.20f) * s, Palette.MetalDark);
        }

        static void BuildAnvilVariant(string name, string path, float s, float craftY)
        {
            var b = new MeshBuilder();
            AddAnvilGeometry(b, s);
            Mesh mesh = SaveMesh(b, name);

            var root = new GameObject(name);
            Part("Body", root.transform, mesh, PM, Vector3.zero);

            var craftPoint = new GameObject("CraftPoint");
            craftPoint.transform.SetParent(root.transform, false);
            craftPoint.transform.localPosition = new Vector3(0, craftY, 0);

            Mesh hot = AssetReplace.SaveMesh(
                BuildSwordMesh(1), name + "_HotSword", $"{Paths.Models}/{name}_HotSword.asset");
            GameObject hotGo = Part("HotSword", root.transform, hot, new[] { hotSwordMat }, new Vector3(0, craftY - 0.045f, 0.03f));
            hotGo.transform.localRotation = Quaternion.Euler(0, 25f, 0);

            ParticleSystem sparks = CreateSparks(root.transform, new Vector3(0, craftY + 0.05f, 0));

            var wp0 = new GameObject("WorkPoint0");
            wp0.transform.SetParent(root.transform, false);
            wp0.transform.localPosition = new Vector3(0, 0, -0.75f * s);
            var wp1 = new GameObject("WorkPoint1");
            wp1.transform.SetParent(root.transform, false);
            wp1.transform.localPosition = new Vector3(-0.7f * s, 0, -0.35f * s);

            var station = root.AddComponent<AnvilStation>();
            station.workPoints = new[] { wp0.transform, wp1.transform };
            station.craftPoint = craftPoint.transform;
            station.hotSwordVisual = hotGo;
            station.sparks = sparks;

            SavePrefab(root, path);
        }

        static void BuildAnvil()
        {
            BuildAnvilVariant("Anvil", AnvilPrefab, 1f, 0.70f);
            BuildAnvilVariant("ApprenticeAnvil", ApprenticeAnvilPrefab, 0.8f, 0.58f);
        }

        static void BuildOrePile()
        {
            var root = new GameObject("OrePile");

            var b = new MeshBuilder();
            // wooden bin
            b.Box(new Vector3(0, 0.05f, 0), new Vector3(1.0f, 0.10f, 0.8f), Palette.WoodDark);
            b.Box(new Vector3(0, 0.22f, 0.37f), new Vector3(1.0f, 0.30f, 0.06f), Palette.WoodMid);
            b.Box(new Vector3(0, 0.22f, -0.37f), new Vector3(1.0f, 0.30f, 0.06f), Palette.WoodMid);
            b.Box(new Vector3(0.47f, 0.22f, 0), new Vector3(0.06f, 0.30f, 0.8f), Palette.WoodMid);
            b.Box(new Vector3(-0.47f, 0.22f, 0), new Vector3(0.06f, 0.30f, 0.8f), Palette.WoodMid);
            Part("Bin", root.transform, SaveMesh(b, "OrePile_Bin"), PM, Vector3.zero);

            // rocks (separate so they can bounce when picked)
            var rockTransforms = new System.Collections.Generic.List<Transform>();
            Vector3[] poses =
            {
                new Vector3(-0.22f, 0.30f, -0.10f), new Vector3(0.15f, 0.32f, 0.12f),
                new Vector3(-0.02f, 0.42f, -0.02f), new Vector3(0.26f, 0.28f, -0.18f),
                new Vector3(-0.30f, 0.28f, 0.18f),
            };
            for (int i = 0; i < poses.Length; i++)
            {
                b = new MeshBuilder();
                float sz = 0.13f + 0.05f * (i % 3);
                b.Rock(Vector3.zero, new Vector3(sz, sz * 0.9f, sz), Palette.OreRock, i * 13.7f);
                if (i % 2 == 0)
                    b.Box(new Vector3(0.05f, sz * 0.55f, 0.02f), new Vector3(0.05f, 0.07f, 0.05f), Palette.OreCrystal, 1);
                GameObject rock = Part("Rock" + i, root.transform, SaveMesh(b, "OrePile_Rock" + i), PMC, poses[i]);
                rockTransforms.Add(rock.transform);
            }

            var p0 = new GameObject("Pickup0");
            p0.transform.SetParent(root.transform, false);
            p0.transform.localPosition = new Vector3(0.75f, 0, 0.1f);
            var p1 = new GameObject("Pickup1");
            p1.transform.SetParent(root.transform, false);
            p1.transform.localPosition = new Vector3(0.15f, 0, 0.75f);

            var pile = root.AddComponent<OrePile>();
            pile.pickupPoints = new[] { p0.transform, p1.transform };
            pile.rockVisuals = rockTransforms.ToArray();

            SavePrefab(root, OrePilePrefab);
        }

        static void BuildSwordRack()
        {
            var root = new GameObject("SwordRack");

            var b = new MeshBuilder();
            b.Box(new Vector3(-0.95f, 0.65f, 0), new Vector3(0.10f, 1.30f, 0.12f), Palette.WoodDark);
            b.Box(new Vector3(0.95f, 0.65f, 0), new Vector3(0.10f, 1.30f, 0.12f), Palette.WoodDark);
            b.Box(new Vector3(0, 0.62f, 0), new Vector3(2.0f, 0.08f, 0.08f), Palette.WoodMid);
            b.Box(new Vector3(0, 1.08f, 0), new Vector3(2.0f, 0.08f, 0.08f), Palette.WoodMid);
            b.Box(new Vector3(-0.95f, 0.04f, 0.05f), new Vector3(0.30f, 0.08f, 0.40f), Palette.WoodDark);
            b.Box(new Vector3(0.95f, 0.04f, 0.05f), new Vector3(0.30f, 0.08f, 0.40f), Palette.WoodDark);
            Part("Frame", root.transform, SaveMesh(b, "SwordRack_Frame"), PM, Vector3.zero);

            // shared peg mesh (horizontal square peg pointing toward the room, -Z)
            b = new MeshBuilder();
            b.Box(new Vector3(0, 0, -0.055f), new Vector3(0.05f, 0.05f, 0.15f), Palette.WoodLight);
            Mesh pegMesh = SaveMesh(b, "SwordRack_Peg");

            var pegs = new Transform[16];
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 8; i++)
                {
                    int idx = row * 8 + i;
                    var peg = new GameObject("Peg" + idx);
                    peg.transform.SetParent(root.transform, false);
                    peg.transform.localPosition = new Vector3(-0.84f + i * 0.24f, row == 0 ? 0.62f : 1.08f, 0f);
                    peg.AddComponent<MeshFilter>().sharedMesh = pegMesh;
                    peg.AddComponent<MeshRenderer>().sharedMaterials = PM;
                    pegs[idx] = peg.transform;
                }

            Transform Marker(string n, Vector3 p)
            {
                var m = new GameObject(n);
                m.transform.SetParent(root.transform, false);
                m.transform.localPosition = p;
                return m.transform;
            }

            var rack = root.AddComponent<SwordRack>();
            rack.slotPoints = pegs;
            // The rack stands behind the shop counter: pegs/swords face the customers (-Z),
            // workers restock from the shop side (+Z).
            rack.depositPoints = new[] { Marker("Deposit0", new Vector3(-0.45f, 0, 0.95f)), Marker("Deposit1", new Vector3(0.45f, 0, 0.95f)) };
            rack.waitPoints = new[] { Marker("Wait0", new Vector3(1.5f, 0, 1.35f)), Marker("Wait1", new Vector3(1.5f, 0, 0.75f)) };
            rack.customerPoint = Marker("CustomerPoint", new Vector3(0f, 0, -1.6f));

            SavePrefab(root, SwordRackPrefab);
        }

        static void BuildForge()
        {
            var root = new GameObject("Forge");

            var b = new MeshBuilder();
            b.Box(new Vector3(0, 0.25f, 0), new Vector3(1.35f, 0.50f, 0.95f), Palette.StoneDark);
            b.Box(new Vector3(0, 0.87f, 0), new Vector3(1.15f, 0.75f, 0.85f), Palette.Stone);
            b.Box(new Vector3(0, 0.75f, 0.40f), new Vector3(0.70f, 0.40f, 0.10f), Palette.Coal);
            b.Box(new Vector3(0, 0.62f, 0.32f), new Vector3(0.66f, 0.10f, 0.42f), Palette.Ember, 1);
            b.Rock(new Vector3(-0.15f, 0.68f, 0.34f), new Vector3(0.09f, 0.07f, 0.09f), Palette.Ember, 5.1f, 1);
            b.Rock(new Vector3(0.12f, 0.69f, 0.30f), new Vector3(0.08f, 0.06f, 0.08f), Palette.Ember, 9.3f, 1);
            b.Box(new Vector3(0, 1.40f, 0), new Vector3(1.00f, 0.35f, 0.75f), Palette.StoneDark);
            b.Box(new Vector3(0, 2.05f, -0.05f), new Vector3(0.42f, 1.05f, 0.42f), Palette.Stone);
            b.Box(new Vector3(0, 2.60f, -0.05f), new Vector3(0.52f, 0.10f, 0.52f), Palette.StoneDark);
            // bellows
            b.Box(new Vector3(-0.85f, 0.30f, 0.15f), new Vector3(0.34f, 0.10f, 0.45f), Palette.WoodMid);
            b.Box(new Vector3(-0.85f, 0.40f, 0.15f), new Vector3(0.30f, 0.10f, 0.40f), Palette.Apron);
            b.Box(new Vector3(-0.85f, 0.45f, 0.42f), new Vector3(0.06f, 0.06f, 0.22f), Palette.WoodDark);
            Part("Body", root.transform, SaveMesh(b, "Forge_Body"), PME, Vector3.zero);

            var lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1.0f, 0.55f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.55f, 0.25f);
            l.intensity = 1.5f;
            l.range = 3.2f;
            l.shadows = LightShadows.None;
            lightGo.AddComponent<LightFlicker>();

            var audioGo = new GameObject("FireAudio");
            audioGo.transform.SetParent(root.transform, false);
            audioGo.transform.localPosition = new Vector3(0, 0.8f, 0.4f);
            var src = audioGo.AddComponent<AudioSource>();
            src.playOnAwake = true;
            src.loop = true;
            src.spatialBlend = 1f;
            src.minDistance = 1.2f;
            src.maxDistance = 7f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.volume = 0.45f;
            src.dopplerLevel = 0f;

            SavePrefab(root, ForgePrefab);
        }

        // ------------------------------------------------------------ props

        static void BuildProps()
        {
            // Shop counter: the customer-facing sales station with coin bowl, lantern
            // and one display sword laid out on the countertop.
            {
                var b = new MeshBuilder();
                b.Box(new Vector3(0, 0.78f, 0), new Vector3(1.9f, 0.09f, 0.62f), Palette.WoodLight);
                b.Box(new Vector3(-0.82f, 0.39f, -0.20f), new Vector3(0.10f, 0.78f, 0.10f), Palette.WoodDark);
                b.Box(new Vector3(0.82f, 0.39f, -0.20f), new Vector3(0.10f, 0.78f, 0.10f), Palette.WoodDark);
                b.Box(new Vector3(-0.82f, 0.39f, 0.20f), new Vector3(0.10f, 0.78f, 0.10f), Palette.WoodDark);
                b.Box(new Vector3(0.82f, 0.39f, 0.20f), new Vector3(0.10f, 0.78f, 0.10f), Palette.WoodDark);
                b.Box(new Vector3(0, 0.25f, 0), new Vector3(1.72f, 0.05f, 0.50f), Palette.WoodMid);
                b.Box(new Vector3(0, 0.55f, -0.28f), new Vector3(1.9f, 0.42f, 0.06f), Palette.WoodMid); // front board
                // coin bowl
                b.Cylinder(new Vector3(-0.55f, 0.86f, 0.05f), 0.14f, 0.08f, 8, Palette.WoodDark);
                b.Cylinder(new Vector3(-0.55f, 0.90f, 0.05f), 0.10f, 0.03f, 8, Palette.Gold);
                // lantern
                b.Box(new Vector3(0.62f, 0.86f, 0.02f), new Vector3(0.13f, 0.05f, 0.13f), Palette.MetalDark);
                b.Box(new Vector3(0.62f, 0.94f, 0.02f), new Vector3(0.08f, 0.11f, 0.08f), Palette.Ember, 1);
                b.Box(new Vector3(0.62f, 1.02f, 0.02f), new Vector3(0.13f, 0.05f, 0.13f), Palette.MetalDark);
                var root = new GameObject("Counter");
                Part("Mesh", root.transform, SaveMesh(b, "Counter"), PME, Vector3.zero);
                if (carriedSwordMesh != null)
                {
                    GameObject ds = Part("DisplaySword", root.transform, carriedSwordMesh, PM, new Vector3(0.05f, 0.845f, 0.08f));
                    ds.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
                }
                SavePrefab(root, CounterPrefab);
            }
            // Barrel.
            {
                var b = new MeshBuilder();
                b.Cylinder(new Vector3(0, 0.28f, 0), 0.26f, 0.55f, 10, Palette.WoodMid);
                b.Cylinder(new Vector3(0, 0.15f, 0), 0.275f, 0.05f, 10, Palette.MetalDark);
                b.Cylinder(new Vector3(0, 0.42f, 0), 0.275f, 0.05f, 10, Palette.MetalDark);
                var root = new GameObject("Barrel");
                Part("Mesh", root.transform, SaveMesh(b, "Barrel"), PM, Vector3.zero);
                SavePrefab(root, BarrelPrefab);
            }
            // Crate.
            {
                var b = new MeshBuilder();
                b.Box(new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.5f, 0.5f), Palette.WoodLight);
                b.Box(new Vector3(0, 0.47f, 0), new Vector3(0.54f, 0.06f, 0.54f), Palette.WoodDark);
                b.Box(new Vector3(0, 0.04f, 0), new Vector3(0.54f, 0.08f, 0.54f), Palette.WoodDark);
                var root = new GameObject("Crate");
                Part("Mesh", root.transform, SaveMesh(b, "Crate"), PM, Vector3.zero);
                SavePrefab(root, CratePrefab);
            }
            // Stool.
            {
                var b = new MeshBuilder();
                b.Cylinder(new Vector3(0, 0.34f, 0), 0.19f, 0.08f, 8, Palette.WoodLight);
                b.Box(new Vector3(-0.10f, 0.15f, -0.10f), new Vector3(0.06f, 0.30f, 0.06f), Palette.WoodDark);
                b.Box(new Vector3(0.10f, 0.15f, -0.10f), new Vector3(0.06f, 0.30f, 0.06f), Palette.WoodDark);
                b.Box(new Vector3(0, 0.15f, 0.11f), new Vector3(0.06f, 0.30f, 0.06f), Palette.WoodDark);
                var root = new GameObject("Stool");
                Part("Mesh", root.transform, SaveMesh(b, "Stool"), PM, Vector3.zero);
                SavePrefab(root, StoolPrefab);
            }
            // Potted plant.
            {
                var b = new MeshBuilder();
                b.Cylinder(new Vector3(0, 0.12f, 0), 0.16f, 0.24f, 8, Palette.Terracotta);
                b.Rock(new Vector3(0, 0.38f, 0), new Vector3(0.18f, 0.16f, 0.18f), Palette.PlantGreen, 2.2f);
                b.Rock(new Vector3(0.08f, 0.52f, 0.04f), new Vector3(0.12f, 0.11f, 0.12f), Palette.PlantGreen, 7.7f);
                b.Rock(new Vector3(-0.07f, 0.50f, -0.05f), new Vector3(0.10f, 0.10f, 0.10f), Palette.PlantGreen, 4.9f);
                var root = new GameObject("Plant");
                Part("Mesh", root.transform, SaveMesh(b, "Plant"), PM, Vector3.zero);
                SavePrefab(root, PlantPrefab);
            }
            // Wall shelf with two display swords and a candle.
            {
                var b = new MeshBuilder();
                b.Box(new Vector3(0, 0, 0), new Vector3(1.0f, 0.05f, 0.28f), Palette.WoodMid);
                b.Box(new Vector3(-0.35f, -0.10f, 0.05f), new Vector3(0.06f, 0.16f, 0.18f), Palette.WoodDark);
                b.Box(new Vector3(0.35f, -0.10f, 0.05f), new Vector3(0.06f, 0.16f, 0.18f), Palette.WoodDark);
                b.Cylinder(new Vector3(0.40f, 0.08f, 0.02f), 0.035f, 0.10f, 6, Palette.ClothCream);
                b.Box(new Vector3(0.40f, 0.15f, 0.02f), new Vector3(0.03f, 0.05f, 0.03f), Palette.Ember, 1);
                var root = new GameObject("Shelf");
                Part("Mesh", root.transform, SaveMesh(b, "Shelf"), PME, Vector3.zero);
                if (carriedSwordMesh != null)
                {
                    GameObject s1 = Part("DisplaySword1", root.transform, carriedSwordMesh, PM, new Vector3(-0.25f, 0.05f, 0));
                    s1.transform.localRotation = Quaternion.Euler(90f, 8f, 0);
                    GameObject s2 = Part("DisplaySword2", root.transform, carriedSwordMesh, PM, new Vector3(0.05f, 0.05f, 0.04f));
                    s2.transform.localRotation = Quaternion.Euler(90f, -5f, 0);
                }
                SavePrefab(root, ShelfPrefab);
            }
            // Banner.
            {
                var b = new MeshBuilder();
                b.Cylinder(new Vector3(0, 0.02f, 0), 0.03f, 0.9f, 6, Palette.WoodDark);
                b.Box(new Vector3(0, -0.45f, 0.02f), new Vector3(0.62f, 0.85f, 0.03f), Palette.Banner);
                b.Box(new Vector3(0, -0.85f, 0.02f), new Vector3(0.62f, 0.08f, 0.035f), Palette.Gold);
                var root = new GameObject("Banner");
                Part("Mesh", root.transform, SaveMesh(b, "Banner"), PM, Vector3.zero);
                SavePrefab(root, BannerPrefab);
            }
            // Rug.
            {
                var b = new MeshBuilder();
                b.Box(new Vector3(0, 0.012f, 0), new Vector3(1.9f, 0.024f, 1.45f), Palette.RugRed);
                b.Box(new Vector3(0, 0.016f, 0), new Vector3(1.6f, 0.026f, 1.15f), Palette.RugCream);
                b.Box(new Vector3(0, 0.020f, 0), new Vector3(1.2f, 0.028f, 0.8f), Palette.RugRed);
                var root = new GameObject("Rug");
                Part("Mesh", root.transform, SaveMesh(b, "Rug"), PM, Vector3.zero);
                SavePrefab(root, RugPrefab);
            }
            // Sign post with a tiny anvil emblem.
            {
                var b = new MeshBuilder();
                b.Box(new Vector3(0, 0.7f, 0), new Vector3(0.10f, 1.4f, 0.10f), Palette.WoodDark);
                b.Box(new Vector3(0, 1.45f, 0), new Vector3(0.55f, 0.06f, 0.06f), Palette.WoodDark);
                b.Box(new Vector3(0.18f, 1.22f, 0), new Vector3(0.44f, 0.34f, 0.05f), Palette.WoodPale);
                b.Box(new Vector3(0.18f, 1.24f, 0.035f), new Vector3(0.20f, 0.05f, 0.02f), Palette.MetalDark);
                b.Box(new Vector3(0.18f, 1.30f, 0.035f), new Vector3(0.08f, 0.08f, 0.02f), Palette.MetalDark);
                var root = new GameObject("SignPost");
                Part("Mesh", root.transform, SaveMesh(b, "SignPost"), PM, Vector3.zero);
                SavePrefab(root, SignPostPrefab);
            }
        }

        // ------------------------------------------------------------ environment

        /// <summary>
        /// One merged environment per growth tier. The gameplay footprint (front wall with
        /// the door, station area) is identical across tiers; bigger tiers extend the floor
        /// outward/backward, raise the walls and add awning / windows / banners / lanterns /
        /// denser vegetation.
        /// </summary>
        /// <summary>
        /// Footprint of one smithy growth tier. Exposed so the scene builder can hand the same
        /// numbers to the camera, which cannot measure them itself: the environment is a single
        /// merged mesh, so its renderer bounds span the 46x46 grass plate as well as the building.
        /// </summary>
        public struct TierDims
        {
            public float halfWidth;
            public float frontZ;
            public float backZ;
            public float height;
        }

        public static TierDims EnvironmentDims(int tier)
        {
            // Width grows slowly and depth grows fast: the camera looks down a portrait screen,
            // where horizontal spread is what forces it to pull back. Growth along Z is nearly free.
            return new TierDims
            {
                halfWidth = 2.5f + 0.45f * (tier - 1),
                frontZ = -3.5f,
                backZ = 3.5f + 0.6f * (tier - 1),
                height = 2.4f + 0.3f * (tier - 1),
            };
        }

        static void BuildEnvironment()
        {
            for (int tier = 1; tier <= EnvironmentTierPrefabs.Length; tier++) BuildEnvironmentTier(tier);
        }

        static void BuildEnvironmentTier(int tier)
        {
            TierDims d = EnvironmentDims(tier);
            float halfW = d.halfWidth;
            float backZ = d.backZ;
            const float frontZ = -3.5f;
            float tallH = d.height;
            const float lowH = 1.15f;
            float midZ = (frontZ + backZ) * 0.5f;
            float depth = backZ - frontZ;
            var rng = new System.Random(1234 + tier * 77);

            var b = new MeshBuilder();

            // ---------------- outside: grass, path, vegetation
            b.Box(new Vector3(0, -0.11f, 0), new Vector3(46f, 0.10f, 46f), Palette.Grass, 0, Palette.Grass);

            // stone path from the door to the customer approach (front-right)
            Vector3[] path =
            {
                new Vector3(0.80f, 0, -4.00f), new Vector3(1.35f, 0, -4.35f), new Vector3(2.05f, 0, -4.62f),
                new Vector3(2.90f, 0, -4.88f), new Vector3(3.85f, 0, -5.12f), new Vector3(4.90f, 0, -5.38f),
                new Vector3(6.00f, 0, -5.58f),
            };
            int stones = Mathf.Min(7, 3 + tier);
            for (int i = 0; i < stones; i++)
                b.Cylinder(new Vector3(path[i].x, -0.03f, path[i].z), 0.26f + 0.03f * (i % 2), 0.06f, 7, Palette.PathStone);

            // welcome mat inside the door
            b.Box(new Vector3(0.8f, 0.012f, -2.85f), new Vector3(1.0f, 0.024f, 0.72f), Palette.RugRed);

            // scattered greenery (denser + fancier as the shop grows)
            int tufts = 6 + tier * 6;
            for (int i = 0; i < tufts; i++)
                Tuft(b, Scatter(rng, halfW, backZ), 0.85f + (float)rng.NextDouble() * 0.7f, i * 3 + tier);
            int flowers = tier <= 1 ? 0 : 3 + tier * 2;
            for (int i = 0; i < flowers; i++)
                Flower(b, Scatter(rng, halfW, backZ), i + tier);
            Bush(b, new Vector3(-halfW - 1.1f, 0, 1.6f), 1f, 3.3f);
            Bush(b, new Vector3(halfW + 1.3f, 0, 2.4f), 0.85f, 8.1f);
            if (tier >= 2)
            {
                Bush(b, new Vector3(-halfW - 1.6f, 0, -2.2f), 1.15f, 5.7f);
                Tree(b, new Vector3(-4.9f, 0, -2.6f), 1f, 2.9f);
                FenceRun(b, new Vector3(-6.2f, 0, -5.3f), new Vector3(-1.2f, 0, -5.3f), 6);
            }
            if (tier >= 3)
            {
                Tree(b, new Vector3(5.4f, 0, 0.6f), 1.2f, 7.4f);
                FenceRun(b, new Vector3(-6.2f, 0, -5.3f + 5.3f * 2f), new Vector3(-6.2f, 0, -5.3f), 5); // left side run
                LanternPost(b, new Vector3(2.55f, 0, -4.15f));
                LanternPost(b, new Vector3(5.35f, 0, -5.95f));
            }

            // ---------------- building
            // floor planks (top surface at y=0)
            int planks = Mathf.Max(6, Mathf.RoundToInt(halfW * 2f / 0.62f));
            float plankW = halfW * 2f / planks;
            for (int i = 0; i < planks; i++)
            {
                float x = -halfW + plankW * (i + 0.5f);
                int col = i % 2 == 0 ? Palette.FloorA : Palette.FloorB;
                b.Box(new Vector3(x, -0.06f, midZ), new Vector3(plankW - 0.02f, 0.12f, depth), col, 0, col);
            }

            // front wall (low dollhouse cutaway) with a door gap x in [0.3, 1.3]
            FrontWallSeg(b, -halfW, 0.3f, frontZ, lowH);
            FrontWallSeg(b, 1.3f, halfW, frontZ, lowH);
            // door frame + lintel
            b.Box(new Vector3(0.3f, 0.85f, frontZ), new Vector3(0.16f, 1.70f, 0.24f), Palette.BeamDark);
            b.Box(new Vector3(1.3f, 0.85f, frontZ), new Vector3(0.16f, 1.70f, 0.24f), Palette.BeamDark);
            b.Box(new Vector3(0.8f, 1.76f, frontZ), new Vector3(1.35f, 0.16f, 0.26f), Palette.BeamDark);

            // back wall (tall, timber framed)
            b.Box(new Vector3(0, tallH * 0.5f, backZ), new Vector3(halfW * 2f + 0.24f, tallH, 0.24f), Palette.WallCream);
            int beams = Mathf.Max(3, Mathf.RoundToInt(halfW * 2f / 1.5f) + 1);
            for (int i = 0; i < beams; i++)
            {
                float x = -halfW + (halfW * 2f) * i / (beams - 1);
                b.Box(new Vector3(x, tallH * 0.5f, backZ - 0.04f), new Vector3(0.18f, tallH, 0.30f), Palette.BeamDark);
            }
            b.Box(new Vector3(0, tallH - 0.10f, backZ - 0.04f), new Vector3(halfW * 2f + 0.24f, 0.20f, 0.30f), Palette.BeamDark);
            b.Box(new Vector3(0, 0.09f, backZ - 0.04f), new Vector3(halfW * 2f + 0.24f, 0.18f, 0.28f), Palette.BeamDark);

            // left wall (tall, timber framed)
            b.Box(new Vector3(-halfW, tallH * 0.5f, midZ), new Vector3(0.24f, tallH, depth), Palette.WallCream);
            int zBeams = Mathf.Max(3, Mathf.RoundToInt(depth / 1.5f) + 1);
            for (int i = 0; i < zBeams; i++)
            {
                float z = frontZ + depth * i / (zBeams - 1);
                b.Box(new Vector3(-halfW + 0.04f, tallH * 0.5f, z), new Vector3(0.30f, tallH, 0.18f), Palette.BeamDark);
            }
            b.Box(new Vector3(-halfW + 0.04f, tallH - 0.10f, midZ), new Vector3(0.30f, 0.20f, depth), Palette.BeamDark);
            b.Box(new Vector3(-halfW + 0.04f, 0.09f, midZ), new Vector3(0.28f, 0.18f, depth), Palette.BeamDark);

            // right wall (low dollhouse cutaway so the camera sees in)
            b.Box(new Vector3(halfW, lowH * 0.5f, midZ), new Vector3(0.22f, lowH, depth), Palette.WallCream);
            b.Box(new Vector3(halfW, lowH + 0.05f, midZ), new Vector3(0.28f, 0.10f, depth), Palette.WoodMid);

            // baked interior wall decor (moves with the walls as the shop grows)
            WallBanner(b, new Vector3(-1.5f, tallH - 0.65f, backZ - 0.16f));
            WallShelf(b, new Vector3(1.15f, 1.35f, backZ - 0.20f));

            if (tier >= 2)
            {
                // window with warm glowing pane on the back wall + flower box
                b.Box(new Vector3(-0.2f, 1.45f, backZ - 0.10f), new Vector3(0.85f, 0.85f, 0.10f), Palette.BeamDark);
                b.Box(new Vector3(-0.2f, 1.45f, backZ - 0.13f), new Vector3(0.65f, 0.65f, 0.08f), Palette.Ember, 1);
                b.Box(new Vector3(-0.2f, 1.45f, backZ - 0.16f), new Vector3(0.10f, 0.65f, 0.06f), Palette.BeamDark);
                b.Box(new Vector3(-0.2f, 1.45f, backZ - 0.16f), new Vector3(0.65f, 0.10f, 0.06f), Palette.BeamDark);
                b.Box(new Vector3(-0.2f, 0.95f, backZ - 0.22f), new Vector3(0.95f, 0.16f, 0.22f), Palette.WoodMid);
                Flower(b, new Vector3(-0.5f, 1.02f, backZ - 0.24f), 0);
                Flower(b, new Vector3(-0.05f, 1.02f, backZ - 0.26f), 1);
                Flower(b, new Vector3(0.15f, 1.02f, backZ - 0.22f), 2);
                // striped awning over the storefront
                var tilt = Quaternion.Euler(24f, 0f, 0f);
                for (int i = 0; i < 5; i++)
                    b.Box(new Vector3(-0.24f + i * 0.52f, 2.02f, frontZ - 0.55f), new Vector3(0.50f, 0.045f, 1.15f), tilt,
                        i % 2 == 0 ? Palette.RedAccent : Palette.ClothCream);
                b.Box(new Vector3(0.25f - 0.52f, 1.05f, frontZ - 1.0f), new Vector3(0.07f, 2.1f, 0.07f), Palette.WoodDark);
                b.Box(new Vector3(1.35f + 0.52f, 1.05f, frontZ - 1.0f), new Vector3(0.07f, 2.1f, 0.07f), Palette.WoodDark);
                // chimney peeking over the back wall
                b.Box(new Vector3(-1.9f, tallH + 0.45f, backZ - 0.35f), new Vector3(0.55f, 1.2f, 0.55f), Palette.Stone);
                b.Box(new Vector3(-1.9f, tallH + 1.08f, backZ - 0.35f), new Vector3(0.68f, 0.14f, 0.68f), Palette.StoneDark);
                // eave boards on the tall walls
                b.Box(new Vector3(0, tallH + 0.02f, backZ - 0.25f), new Vector3(halfW * 2f + 0.6f, 0.09f, 0.75f), Palette.WoodMid);
                b.Box(new Vector3(-halfW - 0.25f, tallH + 0.02f, midZ), new Vector3(0.75f, 0.09f, depth + 0.5f), Palette.WoodMid);
            }
            if (tier >= 3)
            {
                // golden trim under the beams + storefront flags
                b.Box(new Vector3(0, tallH - 0.32f, backZ - 0.05f), new Vector3(halfW * 2f + 0.24f, 0.07f, 0.31f), Palette.Gold);
                b.Box(new Vector3(-halfW + 0.05f, tallH - 0.32f, midZ), new Vector3(0.31f, 0.07f, depth), Palette.Gold);
                StorefrontFlag(b, new Vector3(-0.7f, 0, frontZ - 0.1f));
                StorefrontFlag(b, new Vector3(2.4f, 0, frontZ - 0.1f));
            }
            if (tier >= 4)
            {
                // upper storey with a railed gallery over the storefront
                b.Box(new Vector3(0, tallH + 0.45f, backZ - 0.55f), new Vector3(halfW * 2f, 0.85f, 1.10f), Palette.WallCream);
                b.Box(new Vector3(0, tallH + 0.92f, backZ - 0.55f), new Vector3(halfW * 2f + 0.3f, 0.12f, 1.30f), Palette.WoodMid);
                for (int i = 0; i < beams; i++)
                {
                    float x = -halfW + (halfW * 2f) * i / (beams - 1);
                    b.Box(new Vector3(x, tallH + 0.45f, backZ - 1.12f), new Vector3(0.16f, 0.85f, 0.14f), Palette.BeamDark);
                }
                b.Box(new Vector3(0, tallH + 0.80f, backZ - 1.14f), new Vector3(halfW * 2f, 0.09f, 0.12f), Palette.WoodMid);
                // a second window lights up the upper floor
                b.Box(new Vector3(1.15f, tallH + 0.45f, backZ - 1.16f), new Vector3(0.70f, 0.55f, 0.08f), Palette.BeamDark);
                b.Box(new Vector3(1.15f, tallH + 0.45f, backZ - 1.19f), new Vector3(0.52f, 0.38f, 0.06f), Palette.Ember, 1);
                // lantern pair flanking the entrance
                LanternPost(b, new Vector3(-1.45f, 0, frontZ - 0.85f));
                LanternPost(b, new Vector3(3.05f, 0, frontZ - 0.85f));
                Tree(b, new Vector3(-6.4f, 0, 2.4f), 1.1f, 11.2f);
            }
            if (tier >= 5)
            {
                // the Dragonforge: gilded ridge, banners and a grand gated entrance
                b.Box(new Vector3(0, tallH + 0.98f, backZ - 0.55f), new Vector3(halfW * 2f + 0.36f, 0.12f, 1.34f), Palette.Gold);
                b.Box(new Vector3(0, tallH + 0.34f, backZ - 0.06f), new Vector3(halfW * 2f + 0.24f, 0.09f, 0.33f), Palette.Gold);
                b.Box(new Vector3(-halfW + 0.06f, tallH + 0.34f, midZ), new Vector3(0.33f, 0.09f, depth), Palette.Gold);
                // twin flag poles proud of the roofline
                StorefrontFlag(b, new Vector3(-halfW + 0.5f, 0, backZ - 1.3f));
                StorefrontFlag(b, new Vector3(halfW - 0.5f, 0, backZ - 1.3f));
                // grand entrance: bigger lintel and stone jambs
                b.Box(new Vector3(0.3f, 0.95f, frontZ), new Vector3(0.26f, 1.90f, 0.34f), Palette.Stone);
                b.Box(new Vector3(1.3f, 0.95f, frontZ), new Vector3(0.26f, 1.90f, 0.34f), Palette.Stone);
                b.Box(new Vector3(0.8f, 2.00f, frontZ), new Vector3(1.65f, 0.26f, 0.40f), Palette.Stone);
                b.Box(new Vector3(0.8f, 2.18f, frontZ), new Vector3(1.95f, 0.12f, 0.48f), Palette.Gold);
                // two more lanterns along the front wall
                LanternPost(b, new Vector3(-halfW + 0.4f, 0, frontZ - 0.7f));
                LanternPost(b, new Vector3(halfW - 0.4f, 0, frontZ - 0.7f));
                Tree(b, new Vector3(6.6f, 0, 3.1f), 1.25f, 13.7f);
            }

            var root = new GameObject("Environment_T" + tier);
            Part("Mesh", root.transform, SaveMesh(b, "Environment_T" + tier), PME, Vector3.zero);
            SavePrefab(root, EnvironmentTierPrefabs[tier - 1]);
        }

        static void FrontWallSeg(MeshBuilder b, float x0, float x1, float z, float h)
        {
            float cx = (x0 + x1) * 0.5f, w = x1 - x0;
            if (w <= 0.01f) return;
            b.Box(new Vector3(cx, h * 0.5f, z), new Vector3(w, h, 0.22f), Palette.WallCream);
            b.Box(new Vector3(cx, h + 0.05f, z), new Vector3(w, 0.10f, 0.28f), Palette.WoodMid);
        }

        static void WallBanner(MeshBuilder b, Vector3 p)
        {
            b.Cylinder(p, 0.03f, 0.8f, 6, Palette.WoodDark); // rod (vertical handled below)
            b.Box(p + new Vector3(0, -0.42f, 0.03f), new Vector3(0.58f, 0.78f, 0.035f), Palette.Banner);
            b.Box(p + new Vector3(0, -0.79f, 0.03f), new Vector3(0.58f, 0.07f, 0.04f), Palette.Gold);
            b.Box(p + new Vector3(0, -0.20f, 0.045f), new Vector3(0.16f, 0.16f, 0.02f), Palette.Gold); // tiny anvil mark
        }

        static void WallShelf(MeshBuilder b, Vector3 p)
        {
            b.Box(p, new Vector3(0.95f, 0.05f, 0.26f), Palette.WoodMid);
            b.Box(p + new Vector3(-0.33f, -0.09f, 0.04f), new Vector3(0.06f, 0.15f, 0.16f), Palette.WoodDark);
            b.Box(p + new Vector3(0.33f, -0.09f, 0.04f), new Vector3(0.06f, 0.15f, 0.16f), Palette.WoodDark);
            // bottles + candle
            b.Cylinder(p + new Vector3(-0.22f, 0.10f, 0), 0.05f, 0.14f, 6, Palette.Teal);
            b.Cylinder(p + new Vector3(-0.05f, 0.09f, 0.02f), 0.045f, 0.12f, 6, Palette.Terracotta);
            b.Cylinder(p + new Vector3(0.28f, 0.08f, 0), 0.035f, 0.10f, 6, Palette.ClothCream);
            b.Box(p + new Vector3(0.28f, 0.15f, 0), new Vector3(0.03f, 0.05f, 0.03f), Palette.Ember, 1);
        }

        static void StorefrontFlag(MeshBuilder b, Vector3 basePos)
        {
            b.Box(basePos + new Vector3(0, 1.45f, 0), new Vector3(0.07f, 1.4f, 0.07f), Palette.WoodDark);
            b.Box(basePos + new Vector3(0.28f, 1.95f, 0), new Vector3(0.5f, 0.05f, 0.05f), Palette.WoodDark);
            b.Box(basePos + new Vector3(0.28f, 1.68f, 0), new Vector3(0.42f, 0.5f, 0.03f), Palette.Banner);
            b.Box(basePos + new Vector3(0.28f, 1.45f, 0), new Vector3(0.42f, 0.06f, 0.035f), Palette.Gold);
        }

        static void Tuft(MeshBuilder b, Vector3 p, float s, int seed)
        {
            int blades = 3 + seed % 2;
            for (int i = 0; i < blades; i++)
            {
                float a = seed * 2.39f + i * 2.1f;
                var off = new Vector3(Mathf.Cos(a) * 0.07f * s, 0, Mathf.Sin(a) * 0.07f * s);
                float h = (0.20f + 0.07f * ((seed + i) % 3)) * s;
                int col = (seed + i) % 3 == 0 ? Palette.GrassDark : (seed + i) % 3 == 1 ? Palette.Grass : Palette.GrassLight;
                b.Box(p + off + new Vector3(0, h * 0.5f, 0), new Vector3(0.06f * s, h, 0.06f * s), col);
            }
        }

        static void Flower(MeshBuilder b, Vector3 p, int seed)
        {
            int head = seed % 3 == 0 ? Palette.FlowerRed : seed % 3 == 1 ? Palette.FlowerYellow : Palette.FlowerPink;
            b.Box(p + new Vector3(0, 0.09f, 0), new Vector3(0.03f, 0.18f, 0.03f), Palette.PlantGreen);
            b.Box(p + new Vector3(0, 0.20f, 0), new Vector3(0.10f, 0.08f, 0.10f), head);
            b.Box(p + new Vector3(0, 0.245f, 0), new Vector3(0.045f, 0.03f, 0.045f), Palette.Straw);
        }

        static void Bush(MeshBuilder b, Vector3 p, float s, float seed)
        {
            b.Rock(p + new Vector3(0, 0.16f * s, 0), new Vector3(0.34f, 0.26f, 0.34f) * s, Palette.PlantGreen, seed);
            b.Rock(p + new Vector3(0.17f * s, 0.12f * s, 0.05f * s), new Vector3(0.22f, 0.18f, 0.22f) * s, Palette.GrassDark, seed + 1.7f);
        }

        static void Tree(MeshBuilder b, Vector3 p, float s, float seed)
        {
            b.Cylinder(p + new Vector3(0, 0.45f * s, 0), 0.13f * s, 0.9f * s, 7, Palette.WoodMid);
            b.Rock(p + new Vector3(0, 1.15f * s, 0), new Vector3(0.62f, 0.55f, 0.62f) * s, Palette.PlantGreen, seed);
            b.Rock(p + new Vector3(0.28f * s, 0.95f * s, 0.10f * s), new Vector3(0.38f, 0.32f, 0.38f) * s, Palette.GrassDark, seed + 3.1f);
            b.Rock(p + new Vector3(-0.24f * s, 1.00f * s, -0.12f * s), new Vector3(0.34f, 0.30f, 0.34f) * s, Palette.GrassLight, seed + 6.7f);
        }

        static void LanternPost(MeshBuilder b, Vector3 p)
        {
            b.Box(p + new Vector3(0, 0.55f, 0), new Vector3(0.08f, 1.1f, 0.08f), Palette.WoodDark);
            b.Box(p + new Vector3(0, 1.12f, 0), new Vector3(0.16f, 0.05f, 0.16f), Palette.MetalDark);
            b.Box(p + new Vector3(0, 1.21f, 0), new Vector3(0.11f, 0.14f, 0.11f), Palette.Ember, 1);
            b.Box(p + new Vector3(0, 1.31f, 0), new Vector3(0.16f, 0.05f, 0.16f), Palette.MetalDark);
        }

        static void FenceRun(MeshBuilder b, Vector3 from, Vector3 to, int posts)
        {
            for (int i = 0; i < posts; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, posts == 1 ? 0f : (float)i / (posts - 1));
                b.Box(p + new Vector3(0, 0.34f, 0), new Vector3(0.09f, 0.68f, 0.09f), Palette.WoodPale);
                b.Box(p + new Vector3(0, 0.70f, 0), new Vector3(0.12f, 0.06f, 0.12f), Palette.WoodMid);
            }
            Vector3 mid = (from + to) * 0.5f;
            Vector3 d = to - from;
            float len = d.magnitude;
            float ang = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            var rot = Quaternion.Euler(0, ang, 0);
            b.Box(mid + new Vector3(0, 0.28f, 0), new Vector3(0.05f, 0.07f, len), rot, Palette.WoodPale);
            b.Box(mid + new Vector3(0, 0.52f, 0), new Vector3(0.05f, 0.07f, len), rot, Palette.WoodPale);
        }

        static Vector3 Scatter(System.Random rng, float halfW, float backZ)
        {
            for (int tries = 0; tries < 24; tries++)
            {
                var p = new Vector3(
                    Mathf.Lerp(-7f, 7f, (float)rng.NextDouble()),
                    0f,
                    Mathf.Lerp(-7.5f, backZ + 1.6f, (float)rng.NextDouble()));
                bool insideShop = Mathf.Abs(p.x) < halfW + 0.45f && p.z > -3.95f && p.z < backZ + 0.45f;
                bool onPath = p.x > 0.2f && p.x < 6.6f && p.z < -3.6f && p.z > -6.3f;
                if (!insideShop && !onPath) return p;
            }
            return new Vector3(-halfW - 1.2f, 0, -4.6f);
        }

        // ------------------------------------------------------------ particles

        static ParticleSystem CreateSparks(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.gravityModifier = 1.2f;
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)14) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.06f;

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.7f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = gradient;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = sparkMat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }
    }
}
