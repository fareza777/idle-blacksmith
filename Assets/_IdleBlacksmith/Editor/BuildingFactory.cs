using IdleBlacksmith.Core;
using UnityEditor;
using UnityEngine;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// The non-smithy complex buildings: Ore Mine, Trading Post, Dungeon Gate and
    /// Enchanter's Sanctum. One prefab per building per level, built from the same palette
    /// and mesh helpers as the rest of the game so everything reads as one family.
    ///
    /// Every builder works in a local space where +Z faces the shop and y=0 is the ground.
    /// </summary>
    public static partial class ModelFactory
    {
        public static string BuildingPrefabPath(string id, int level)
            => $"{Paths.Prefabs}/Building_{id}_L{level}.prefab";

        public static GameObject BuildingPrefab(string id, int level)
            => AssetDatabase.LoadAssetAtPath<GameObject>(BuildingPrefabPath(id, level));

        public const string EmptyPlotPrefab = Paths.Prefabs + "/BuildingPlot_Empty.prefab";

        /// <summary>
        /// Marker standing on a plot that has not been built yet: a surveyor's stake with a sign,
        /// plus a chalk ring on the ground, so the yard reads as "buildable land" rather than
        /// decoration the player cannot touch.
        /// </summary>
        static void BuildEmptyPlotMarker()
        {
            var b = new MeshBuilder();

            // chalk ring on the ground
            const int seg = 20;
            for (int i = 0; i < seg; i++)
            {
                float a = Mathf.PI * 2f * i / seg;
                b.Box(new Vector3(Mathf.Cos(a) * 1.5f, 0.05f, Mathf.Sin(a) * 1.5f),
                    new Vector3(0.22f, 0.06f, 0.22f), Palette.WoodPale);
            }

            // surveyor's stake
            b.Box(new Vector3(0f, 0.65f, 0f), new Vector3(0.12f, 1.30f, 0.12f), Palette.WoodMid);
            b.Box(new Vector3(0f, 1.42f, 0f), new Vector3(0.95f, 0.62f, 0.08f), Palette.WoodPale);
            b.Box(new Vector3(0f, 1.42f, 0.05f), new Vector3(0.72f, 0.40f, 0.03f), Palette.ClothCream);
            // a small hammer mark on the board, matching the build button
            b.Box(new Vector3(0f, 1.46f, 0.08f), new Vector3(0.34f, 0.09f, 0.02f), Palette.MetalDark);
            b.Box(new Vector3(0.10f, 1.38f, 0.08f), new Vector3(0.08f, 0.22f, 0.02f), Palette.WoodDark);

            var root = new GameObject("BuildingPlot_Empty");
            Part("Mesh", root.transform, SaveMesh(b, "BuildingPlot_Empty"), PM, Vector3.zero);
            SavePrefab(root, EmptyPlotPrefab);
        }

        /// <summary>Highest level that has a prefab, used by the scene builder to populate arrays.</summary>
        public const int BuildingMaxLevel = 5;

        static void BuildBuildings()
        {
            BuildEmptyPlotMarker();
            for (int level = 1; level <= BuildingMaxLevel; level++)
            {
                BuildMine(level);
                BuildMarket(level);
                BuildGate(level);
                BuildSanctum(level);
                BuildFurnace(level);
            }
        }

        // ------------------------------------------------------------ mine

        static void BuildMine(int level)
        {
            var b = new MeshBuilder();
            var floaters = new System.Collections.Generic.List<Transform>();

            float s = 1f + 0.09f * (level - 1);

            // hillside
            b.Rock(new Vector3(0f, 0.35f * s, 0.55f * s), new Vector3(2.5f, 1.5f, 2.2f) * s, Palette.Stone, 2.4f);
            b.Rock(new Vector3(-1.7f * s, 0.25f * s, 1.15f * s), new Vector3(1.5f, 1.0f, 1.4f) * s, Palette.StoneDark, 6.1f);
            b.Rock(new Vector3(1.6f * s, 0.22f * s, 1.3f * s), new Vector3(1.4f, 0.9f, 1.3f) * s, Palette.StoneDark, 9.3f);

            // timber-framed adit
            float mouthZ = -0.55f * s;
            b.Box(new Vector3(-0.78f * s, 0.72f * s, mouthZ), new Vector3(0.20f, 1.45f, 0.30f) * s, Palette.WoodDark);
            b.Box(new Vector3(0.78f * s, 0.72f * s, mouthZ), new Vector3(0.20f, 1.45f, 0.30f) * s, Palette.WoodDark);
            b.Box(new Vector3(0f, 1.47f * s, mouthZ), new Vector3(1.85f, 0.22f, 0.32f) * s, Palette.WoodDark);
            b.Box(new Vector3(0f, 1.62f * s, mouthZ - 0.02f), new Vector3(2.10f, 0.12f, 0.36f) * s, Palette.WoodMid);
            // dark opening
            b.Box(new Vector3(0f, 0.62f * s, mouthZ + 0.04f), new Vector3(1.36f, 1.24f, 0.10f) * s, Palette.Coal);

            // glowing crystals down the shaft
            int crystals = 2 + level;
            for (int i = 0; i < crystals; i++)
            {
                float cx = crystals == 1 ? 0f : -0.5f * s + i * (1.0f * s) / (crystals - 1);
                float h = 0.28f + 0.09f * (i % 3);
                b.Box(new Vector3(cx, 0.20f * s + h * 0.5f, mouthZ + 0.06f), new Vector3(0.13f, h, 0.13f), Palette.OreCrystal, 1);
            }

            // headframe over the shaft
            float towerH = 1.5f + 0.24f * level;
            float towerX = -1.55f * s;
            b.Box(new Vector3(towerX - 0.42f, towerH * 0.5f, 1.55f * s), new Vector3(0.16f, towerH, 0.16f), Palette.WoodMid);
            b.Box(new Vector3(towerX + 0.42f, towerH * 0.5f, 1.55f * s), new Vector3(0.16f, towerH, 0.16f), Palette.WoodMid);
            b.Box(new Vector3(towerX, towerH - 0.12f, 1.55f * s), new Vector3(1.05f, 0.14f, 0.18f), Palette.WoodDark);
            b.Box(new Vector3(towerX, towerH * 0.62f, 1.55f * s), new Vector3(0.95f, 0.10f, 0.14f), Palette.WoodDark);
            b.Cylinder(new Vector3(towerX, towerH + 0.06f, 1.55f * s), 0.20f, 0.16f, 8, Palette.MetalDark);
            b.Box(new Vector3(towerX, towerH - 0.45f, 1.55f * s), new Vector3(0.07f, 0.75f, 0.07f), Palette.MetalLight);

            // ore cart on rails
            float railZ = -1.35f * s;
            b.Box(new Vector3(0f, 0.05f, railZ), new Vector3(2.6f, 0.09f, 0.14f), Palette.WoodPale);
            b.Box(new Vector3(0f, 0.05f, railZ - 0.55f), new Vector3(2.6f, 0.09f, 0.14f), Palette.WoodPale);
            float cartX = -0.15f + 0.28f * level;
            b.Box(new Vector3(cartX, 0.34f, railZ - 0.28f), new Vector3(0.72f, 0.42f, 0.60f), Palette.MetalDark);
            b.Box(new Vector3(cartX, 0.50f, railZ - 0.28f), new Vector3(0.78f, 0.09f, 0.66f), Palette.MetalLight);
            b.Rock(new Vector3(cartX - 0.08f, 0.60f, railZ - 0.30f), new Vector3(0.20f, 0.16f, 0.20f), Palette.OreRock, 3.3f);
            b.Rock(new Vector3(cartX + 0.14f, 0.58f, railZ - 0.22f), new Vector3(0.15f, 0.13f, 0.15f), Palette.OreRock, 7.9f);
            for (int i = 0; i < 4; i++)
            {
                float wx = cartX + (i % 2 == 0 ? -0.24f : 0.24f);
                float wz = railZ - 0.28f + (i < 2 ? -0.19f : 0.19f);
                b.Cylinder(new Vector3(wx, 0.14f, wz), 0.11f, 0.08f, 7, Palette.Coal, 0,
                    Palette.Coal);
            }

            if (level >= 2)
            {
                // second seam with its own glow
                b.Box(new Vector3(1.85f * s, 0.42f * s, -0.15f * s), new Vector3(1.1f, 0.85f, 0.75f), Palette.StoneDark);
                b.Box(new Vector3(1.85f * s, 0.40f * s, -0.50f * s), new Vector3(0.72f, 0.62f, 0.10f), Palette.Coal);
                b.Box(new Vector3(1.85f * s, 0.38f * s, -0.48f * s), new Vector3(0.40f, 0.38f, 0.08f), Palette.OreCrystal, 1);
            }
            if (level >= 3)
            {
                // wash-house and stacked crates
                b.Box(new Vector3(-2.35f * s, 0.55f, 0.05f), new Vector3(1.25f, 1.10f, 1.05f), Palette.WoodLight);
                b.Box(new Vector3(-2.35f * s, 1.16f, 0.05f), new Vector3(1.40f, 0.14f, 1.20f), Palette.WoodDark);
                b.Box(new Vector3(-2.30f * s, 0.20f, -1.15f), new Vector3(0.55f, 0.40f, 0.55f), Palette.WoodMid);
                b.Box(new Vector3(-1.75f * s, 0.18f, -1.30f), new Vector3(0.45f, 0.36f, 0.45f), Palette.WoodMid);
            }
            if (level >= 4)
            {
                // conveyor ramp up to the headframe
                var rot = Quaternion.Euler(-22f, 0f, 0f);
                b.Box(new Vector3(towerX, 0.78f, -0.10f), new Vector3(0.62f, 0.10f, 3.3f), rot, Palette.WoodPale);
                for (int i = 0; i < 4; i++)
                    b.Box(new Vector3(towerX, 0.30f + i * 0.28f, -1.35f + i * 0.72f), new Vector3(0.14f, 0.55f, 0.14f), Palette.WoodDark);
            }
            if (level >= 5)
            {
                // gilded supports marking a masterwork mine
                b.Box(new Vector3(-0.78f * s, 1.55f * s, mouthZ), new Vector3(0.30f, 0.10f, 0.36f) * s, Palette.Gold);
                b.Box(new Vector3(0.78f * s, 1.55f * s, mouthZ), new Vector3(0.30f, 0.10f, 0.36f) * s, Palette.Gold);
                b.Box(new Vector3(towerX, towerH + 0.02f, 1.55f * s), new Vector3(1.12f, 0.10f, 0.22f), Palette.Gold);
            }

            var root = new GameObject("Building_" + BuildingId.Mine + "_L" + level);
            Part("Mesh", root.transform, SaveMesh(b, "Building_Mine_L" + level), PME, Vector3.zero);

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.65f * s, mouthZ - 0.25f);
            var light = glowGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.45f, 0.82f, 0.95f);
            light.intensity = 0.9f + 0.16f * level;
            light.range = 3.4f;
            light.shadows = LightShadows.None;

            var dust = CreateDust(root.transform, new Vector3(0f, 0.75f, mouthZ - 0.5f), level);

            var flourish = root.AddComponent<IdleBlacksmith.Gameplay.BuildingFlourish>();
            flourish.glow = light;
            flourish.glowBase = light.intensity;
            flourish.glowSwing = 0.22f;
            flourish.bursts = new[] { dust };
            flourish.burstInterval = Mathf.Max(1.6f, 5.2f - 0.6f * level);
            flourish.floaters = floaters.ToArray();

            SavePrefab(root, BuildingPrefabPath(BuildingId.Mine, level));
        }

        // ------------------------------------------------------------ market

        static void BuildMarket(int level)
        {
            var b = new MeshBuilder();
            var floaters = new System.Collections.Generic.List<Transform>();

            int stalls = 1 + (level - 1) / 2;                 // 1,1,2,2,3
            float span = 1.7f;
            float x0 = -(stalls - 1) * span * 0.5f;

            for (int i = 0; i < stalls; i++)
            {
                float cx = x0 + i * span;
                float depth = 0.75f + 0.05f * level;

                // counter and posts
                b.Box(new Vector3(cx, 0.82f, 0f), new Vector3(1.55f, 0.10f, depth), Palette.WoodLight);
                b.Box(new Vector3(cx - 0.66f, 0.41f, -0.18f), new Vector3(0.11f, 0.82f, 0.11f), Palette.WoodDark);
                b.Box(new Vector3(cx + 0.66f, 0.41f, -0.18f), new Vector3(0.11f, 0.82f, 0.11f), Palette.WoodDark);
                b.Box(new Vector3(cx - 0.66f, 0.41f, 0.22f), new Vector3(0.11f, 0.82f, 0.11f), Palette.WoodDark);
                b.Box(new Vector3(cx + 0.66f, 0.41f, 0.22f), new Vector3(0.11f, 0.82f, 0.11f), Palette.WoodDark);
                b.Box(new Vector3(cx, 0.30f, 0.02f), new Vector3(1.40f, 0.06f, depth * 0.7f), Palette.WoodMid);
                // goods on the counter
                for (int g = 0; g < 3; g++)
                {
                    float gx = cx - 0.42f + g * 0.42f;
                    b.Box(new Vector3(gx, 0.90f, 0.02f), new Vector3(0.22f, 0.10f, 0.18f), Palette.WoodLight);
                    if (g == 1) b.Cylinder(new Vector3(gx, 0.97f, 0.02f), 0.09f, 0.06f, 8, Palette.Gold);
                }

                // striped awning, tilted toward the customer
                var tilt = Quaternion.Euler(20f, 0f, 0f);
                for (int s = 0; s < 4; s++)
                    b.Box(new Vector3(cx - 0.57f + s * 0.38f, 1.62f, -0.34f), new Vector3(0.36f, 0.045f, 1.15f), tilt,
                        s % 2 == 0 ? Palette.RedAccent : Palette.ClothCream);
                b.Box(new Vector3(cx - 0.74f, 1.05f, -0.78f), new Vector3(0.07f, 2.1f, 0.07f), Palette.WoodDark);
                b.Box(new Vector3(cx + 0.74f, 1.05f, -0.78f), new Vector3(0.07f, 2.1f, 0.07f), Palette.WoodDark);
            }

            // price board
            if (level >= 2)
            {
                b.Box(new Vector3(x0 - 1.15f, 1.05f, 0.05f), new Vector3(0.07f, 2.1f, 0.07f), Palette.WoodDark);
                b.Box(new Vector3(x0 - 1.15f, 1.85f, 0.05f), new Vector3(0.92f, 0.62f, 0.06f), Palette.WoodPale);
                b.Box(new Vector3(x0 - 1.15f, 1.95f, 0.10f), new Vector3(0.68f, 0.06f, 0.02f), Palette.StoneDark);
                b.Box(new Vector3(x0 - 1.15f, 1.78f, 0.10f), new Vector3(0.52f, 0.06f, 0.02f), Palette.StoneDark);
            }
            // barrels and crates behind
            if (level >= 3)
            {
                b.Cylinder(new Vector3(x0 + stalls * span - 0.6f, 0.30f, 0.75f), 0.27f, 0.60f, 10, Palette.WoodMid);
                b.Box(new Vector3(x0 + stalls * span - 1.35f, 0.24f, 0.70f), new Vector3(0.48f, 0.48f, 0.48f), Palette.WoodLight);
                b.Box(new Vector3(x0 + stalls * span - 1.30f, 0.52f, 0.72f), new Vector3(0.42f, 0.10f, 0.42f), Palette.WoodDark);
            }
            // hanging lanterns + banners
            if (level >= 4)
            {
                for (int i = 0; i < stalls; i++)
                {
                    float cx = x0 + i * span;
                    b.Box(new Vector3(cx + 0.70f, 1.62f, -0.30f), new Vector3(0.16f, 0.20f, 0.16f), Palette.Ember, 1);
                    b.Box(new Vector3(cx + 0.70f, 1.75f, -0.30f), new Vector3(0.20f, 0.05f, 0.20f), Palette.MetalDark);
                }
            }
            if (level >= 5)
            {
                for (int i = 0; i < stalls; i++)
                {
                    float cx = x0 + i * span;
                    b.Box(new Vector3(cx, 2.10f, -0.78f), new Vector3(1.60f, 0.09f, 0.18f), Palette.Gold);
                    b.Box(new Vector3(cx, 1.28f, 0.42f), new Vector3(0.42f, 0.62f, 0.04f), Palette.Banner);
                    b.Box(new Vector3(cx, 0.96f, 0.42f), new Vector3(0.42f, 0.07f, 0.05f), Palette.Gold);
                }
            }

            var root = new GameObject("Building_" + BuildingId.Market + "_L" + level);
            Part("Mesh", root.transform, SaveMesh(b, "Building_Market_L" + level), PME, Vector3.zero);

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 1.25f, 0.1f);
            var light = glowGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.80f, 0.55f);
            light.intensity = 0.7f + 0.14f * level;
            light.range = 3.6f;
            light.shadows = LightShadows.None;

            var flourish = root.AddComponent<IdleBlacksmith.Gameplay.BuildingFlourish>();
            flourish.glow = light;
            flourish.glowBase = light.intensity;
            flourish.glowSwing = 0.14f;
            flourish.floaters = floaters.ToArray();

            SavePrefab(root, BuildingPrefabPath(BuildingId.Market, level));
        }

        // ------------------------------------------------------------ gate

        static void BuildGate(int level)
        {
            var b = new MeshBuilder();
            var floaters = new System.Collections.Generic.List<Transform>();

            float h = 2.2f + 0.22f * level;
            float w = 1.15f + 0.06f * level;

            // steps up to the arch
            for (int i = 0; i < 2 + level / 2; i++)
                b.Box(new Vector3(0f, 0.06f + i * 0.09f, -0.95f - i * 0.30f),
                    new Vector3(w * 2.5f - i * 0.22f, 0.12f, 0.34f), i % 2 == 0 ? Palette.Stone : Palette.StoneDark);

            // pillars
            b.Box(new Vector3(-w, h * 0.5f, 0f), new Vector3(0.55f, h, 0.70f), Palette.Stone);
            b.Box(new Vector3(w, h * 0.5f, 0f), new Vector3(0.55f, h, 0.70f), Palette.Stone);
            b.Box(new Vector3(-w, h + 0.10f, 0f), new Vector3(0.72f, 0.20f, 0.86f), Palette.StoneDark);
            b.Box(new Vector3(w, h + 0.10f, 0f), new Vector3(0.72f, 0.20f, 0.86f), Palette.StoneDark);
            b.Box(new Vector3(0f, h * 0.5f, 0f), new Vector3(0.42f, h, 0.46f), Palette.StoneDark);

            // lintel + arch teeth
            b.Box(new Vector3(0f, h + 0.24f, 0f), new Vector3(w * 2f + 0.9f, 0.34f, 0.80f), Palette.Stone);
            b.Box(new Vector3(0f, h + 0.48f, 0f), new Vector3(w * 2f + 1.1f, 0.14f, 0.94f), Palette.StoneDark);
            int teeth = 5 + level;
            for (int i = 0; i < teeth; i++)
            {
                float tx = -w * 0.9f + i * (w * 1.8f) / Mathf.Max(1, teeth - 1);
                b.Box(new Vector3(tx, h + 0.06f, -0.44f), new Vector3(0.16f, 0.24f, 0.14f), Palette.StoneDark);
            }

            // the portal itself — a tall dark opening with a glowing core in slot 1
            b.Box(new Vector3(0f, h * 0.44f, 0.12f), new Vector3(w * 1.55f, h * 0.86f, 0.10f), Palette.Coal);
            b.Box(new Vector3(0f, h * 0.42f, 0.18f), new Vector3(w * 0.95f, h * 0.62f, 0.08f), Palette.OreCrystal, 1);
            b.Box(new Vector3(0f, h * 0.42f, 0.22f), new Vector3(w * 0.42f, h * 0.30f, 0.06f), Palette.White, 1);

            // torches on the pillars
            for (int i = 0; i < 2; i++)
            {
                float px = i == 0 ? -w : w;
                b.Box(new Vector3(px, h * 0.72f, -0.42f), new Vector3(0.09f, 0.42f, 0.09f), Palette.WoodDark);
                b.Box(new Vector3(px, h * 0.95f, -0.42f), new Vector3(0.14f, 0.20f, 0.14f), Palette.Ember, 1);
            }
            if (level >= 3)
                for (int i = 0; i < 2; i++)
                {
                    float px = i == 0 ? -w - 1.55f : w + 1.55f;
                    b.Box(new Vector3(px, 0.55f, 0.4f), new Vector3(0.14f, 1.1f, 0.14f), Palette.StoneDark);
                    b.Box(new Vector3(px, 1.20f, 0.4f), new Vector3(0.22f, 0.28f, 0.22f), Palette.Ember, 1);
                    b.Box(new Vector3(px, 1.38f, 0.4f), new Vector3(0.30f, 0.08f, 0.30f), Palette.StoneDark);
                }
            if (level >= 4)
            {
                // carved runes and a flanking rubble wall
                for (int i = 0; i < 4; i++)
                    b.Box(new Vector3(-w + 0.05f + i * 0.01f, 0.9f + i * 0.34f, 0.36f), new Vector3(0.16f, 0.16f, 0.05f), Palette.OreCrystal, 1);
                for (int i = 0; i < 4; i++)
                    b.Box(new Vector3(w - 0.05f, 0.9f + i * 0.34f, 0.36f), new Vector3(0.16f, 0.16f, 0.05f), Palette.OreCrystal, 1);
                b.Rock(new Vector3(-w - 1.9f, 0.25f, -0.3f), new Vector3(0.9f, 0.55f, 0.8f), Palette.StoneDark, 4.2f);
                b.Rock(new Vector3(w + 1.9f, 0.22f, -0.2f), new Vector3(0.85f, 0.50f, 0.75f), Palette.StoneDark, 8.6f);
            }
            if (level >= 5)
            {
                b.Box(new Vector3(0f, h + 0.62f, 0f), new Vector3(w * 2f + 1.1f, 0.10f, 0.98f), Palette.Gold);
                b.Box(new Vector3(-w, h * 0.5f, -0.40f), new Vector3(0.10f, h * 0.9f, 0.10f), Palette.Gold);
                b.Box(new Vector3(w, h * 0.5f, -0.40f), new Vector3(0.10f, h * 0.9f, 0.10f), Palette.Gold);
            }

            var root = new GameObject("Building_" + BuildingId.Gate + "_L" + level);
            Part("Mesh", root.transform, SaveMesh(b, "Building_Gate_L" + level), PME, Vector3.zero);

            var glowGo = new GameObject("PortalLight");
            glowGo.transform.SetParent(root.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, h * 0.45f, -0.6f);
            var light = glowGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.55f, 0.80f, 1f);
            light.intensity = 1.2f + 0.30f * level;
            light.range = 4.6f;
            light.shadows = LightShadows.None;

            var flourish = root.AddComponent<IdleBlacksmith.Gameplay.BuildingFlourish>();
            flourish.glow = light;
            flourish.glowBase = light.intensity;
            flourish.glowSwing = 0.45f;
            flourish.glowSpeed = 1.7f;
            flourish.floaters = floaters.ToArray();

            SavePrefab(root, BuildingPrefabPath(BuildingId.Gate, level));
        }

        // ------------------------------------------------------------ sanctum

        static void BuildSanctum(int level)
        {
            var b = new MeshBuilder();

            float h = 2.1f + 0.42f * level;
            float w = 0.95f + 0.07f * level;
            var lean = Quaternion.Euler(0f, 0f, 3.5f);

            // crooked tower base and shaft
            b.Box(new Vector3(0f, 0.22f, 0f), new Vector3(w * 2.5f, 0.44f, w * 2.5f), Palette.StoneDark);
            b.Box(new Vector3(0f, 0.52f, 0f), new Vector3(w * 2.1f, 0.22f, w * 2.1f), Palette.Stone);
            b.Box(new Vector3(0f, h * 0.5f + 0.6f, 0f), new Vector3(w * 1.8f, h, w * 1.8f), Palette.Stone, 0, -1);
            b.Box(new Vector3(0f, h * 0.5f + 0.6f, 0f), new Vector3(w * 1.95f, h * 0.16f, w * 1.95f), Palette.StoneDark);

            // conical roof
            float roofY = h + 0.6f;
            b.Cylinder(new Vector3(0f, roofY + 0.10f, 0f), w * 1.30f, 0.20f, 8, Palette.PlumDark);
            b.Cylinder(new Vector3(0f, roofY + 0.55f, 0f), w * 1.00f, 0.70f, 8, Palette.PlumDark);
            b.Cylinder(new Vector3(0f, roofY + 1.05f, 0f), w * 0.52f, 0.60f, 8, Palette.ShirtPurple);
            b.Cylinder(new Vector3(0f, roofY + 1.42f, 0f), w * 0.10f, 0.34f, 6, Palette.Gold);

            // glowing round window
            b.Cylinder(new Vector3(0f, h * 0.62f + 0.55f, -w * 0.92f), w * 0.42f, 0.10f, 10, Palette.WoodDark);
            b.Cylinder(new Vector3(0f, h * 0.62f + 0.55f, -w * 0.96f), w * 0.32f, 0.08f, 10, Palette.Ember, 1);

            // door
            b.Box(new Vector3(0f, 0.62f, -w * 0.94f), new Vector3(w * 0.62f, 1.10f, 0.10f), Palette.WoodDark);

            // floating crystal ring — more shards as it grows
            int shards = 2 + level;
            float ringR = w * 1.9f + 0.25f * level;
            for (int i = 0; i < shards; i++)
            {
                float a = Mathf.PI * 2f * i / shards + level * 0.4f;
                float sx = Mathf.Cos(a) * ringR;
                float sz = Mathf.Sin(a) * ringR;
                float sy = 0.85f + 0.34f * i;
                b.Box(new Vector3(sx, sy, sz), new Vector3(0.13f, 0.30f, 0.13f), Palette.OreCrystal, 1);
            }

            if (level >= 3)
            {
                // side annex
                b.Box(new Vector3(w * 1.85f, 0.60f, 0.25f), new Vector3(1.15f, 1.20f, 1.15f), Palette.Stone);
                b.Cylinder(new Vector3(w * 1.85f, 1.48f, 0.25f), 0.92f, 0.30f, 8, Palette.PlumDark);
                b.Cylinder(new Vector3(w * 1.85f, 1.90f, 0.25f), 0.52f, 0.55f, 8, Palette.ShirtPurple);
            }
            if (level >= 4)
            {
                // floating rune stones around the base
                for (int i = 0; i < 5; i++)
                {
                    float a = Mathf.PI * 2f * i / 5f + 0.5f;
                    b.Box(new Vector3(Mathf.Cos(a) * (ringR + 0.75f), 0.34f, Mathf.Sin(a) * (ringR + 0.75f)),
                        new Vector3(0.28f, 0.44f, 0.14f), Palette.StoneDark);
                }
                b.Box(new Vector3(-w * 1.85f, 0.75f, -0.45f), new Vector3(1.05f, 1.50f, 1.05f), Palette.StoneDark);
                b.Box(new Vector3(-w * 1.85f, 1.45f, -0.45f), new Vector3(0.72f, 0.42f, 0.72f), Palette.Ember, 1);
            }
            if (level >= 5)
            {
                // master spire and a big caged heart-stone
                b.Box(new Vector3(0f, roofY + 1.95f, 0f), new Vector3(0.90f, 0.34f, 0.90f), Palette.Gold);
                b.Box(new Vector3(0f, roofY + 2.30f, 0f), new Vector3(0.32f, 0.55f, 0.32f), Palette.OreCrystal, 1);
                b.Box(new Vector3(0f, 1.10f, -w * 1.15f), new Vector3(0.85f, 0.85f, 0.20f), Palette.WoodDark);
                b.Box(new Vector3(0f, 1.10f, -w * 1.24f), new Vector3(0.62f, 0.62f, 0.14f), Palette.OreCrystal, 1);
            }

            var root = new GameObject("Building_" + BuildingId.Sanctum + "_L" + level);
            Part("Mesh", root.transform, SaveMesh(b, "Building_Sanctum_L" + level), PME, Vector3.zero);

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, h * 0.62f + 0.7f, -w * 1.2f);
            var light = glowGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.72f, 0.55f, 1f);
            light.intensity = 1.0f + 0.26f * level;
            light.range = 4.4f;
            light.shadows = LightShadows.None;

            var enchant = CreateSparkle(root.transform, new Vector3(0f, 1.2f, 0f), 0.9f * level);

            var flourish = root.AddComponent<IdleBlacksmith.Gameplay.BuildingFlourish>();
            flourish.glow = light;
            flourish.glowBase = light.intensity;
            flourish.glowSwing = 0.30f;
            flourish.glowSpeed = 0.9f;
            flourish.bursts = new[] { enchant };
            flourish.burstInterval = 6.5f;
            flourish.floaters = new Transform[0];

            SavePrefab(root, BuildingPrefabPath(BuildingId.Sanctum, level));
        }

        // ------------------------------------------------------------ blast furnace

        /// <summary>
        /// Brick furnace with an arched glowing mouth: bellows appear at level 2, a coal
        /// heap at 3, and one more ember seam climbs the stack per level.
        /// </summary>
        static void BuildFurnace(int level)
        {
            var b = new MeshBuilder();
            float s = 1f + 0.07f * (level - 1);

            // stone footing
            b.Box(new Vector3(0f, 0.14f * s, 0f), new Vector3(2.2f, 0.28f, 1.9f) * s, Palette.Stone);

            // tapered brick body and the chimney behind it
            float bodyH = (1.5f + 0.35f * level) * s;
            b.Box(new Vector3(0f, 0.28f * s + bodyH * 0.5f, 0.15f * s), new Vector3(1.7f, bodyH, 1.5f) * s, Palette.Terracotta);
            b.Box(new Vector3(0f, 0.28f * s + bodyH + 0.05f, 0.15f * s), new Vector3(1.85f, 0.10f, 1.65f) * s, Palette.StoneDark);
            float chimH = (0.9f + 0.30f * level) * s;
            b.Box(new Vector3(0.4f * s, 0.28f * s + bodyH + chimH * 0.5f, 0.4f * s), new Vector3(0.55f, chimH, 0.55f), Palette.Terracotta);
            b.Box(new Vector3(0.4f * s, 0.28f * s + bodyH + chimH + 0.07f, 0.4f * s), new Vector3(0.75f, 0.14f, 0.75f), Palette.StoneDark);

            // arched mouth: dark archway around a hot glowing throat
            b.Box(new Vector3(0f, 0.82f * s, -0.62f * s), new Vector3(0.95f, 1.15f, 0.12f), Palette.Coal);
            b.Box(new Vector3(0f, 0.68f * s, -0.635f * s), new Vector3(0.55f, 0.52f, 0.13f), Palette.Ember, 1);
            b.Box(new Vector3(0f, 1.46f * s, -0.62f * s), new Vector3(1.15f, 0.22f, 0.2f), Palette.StoneDark);

            // bellows bolted to the flank once the furnace is serious
            if (level >= 2)
            {
                b.Box(new Vector3(-1.15f * s, 0.55f * s, -0.2f), new Vector3(0.70f, 0.50f, 0.55f), Palette.WoodDark);
                b.Box(new Vector3(-1.15f * s, 0.88f * s, -0.2f), new Vector3(0.50f, 0.18f, 0.40f), Palette.Grip);
                b.Box(new Vector3(-0.82f * s, 0.55f * s, -0.35f), new Vector3(0.28f, 0.12f, 0.12f), Palette.MetalDark);
            }

            // coal heap beside the mouth
            if (level >= 3)
                b.Rock(new Vector3(1.05f * s, 0.28f * s, -0.55f), new Vector3(0.70f, 0.48f, 0.60f), Palette.Coal, 5.2f);

            // ember seams climbing the facade, one per level
            for (int i = 0; i < level; i++)
                b.Box(new Vector3(-0.80f * s + i * (0.40f * s), 0.35f * s + 0.20f * i, -0.615f * s),
                      new Vector3(0.07f, 0.45f + 0.18f * i, 0.05f), Palette.Ember, 1);

            var root = new GameObject("Building_" + BuildingId.Furnace + "_L" + level);
            Part("Mesh", root.transform, SaveMesh(b, "Building_Furnace_L" + level), PME, Vector3.zero);
            SavePrefab(root, BuildingPrefabPath(BuildingId.Furnace, level));
        }

        // ------------------------------------------------------------ helpers

        static ParticleSystem CreateDust(Transform parent, Vector3 localPos, int level)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.9f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.42f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
            main.gravityModifier = -0.03f;
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.78f, 0.74f, 0.66f, 0.55f), new Color(0.62f, 0.58f, 0.52f, 0.30f));

            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(6 + level * 2)) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.2f, 0.3f, 0.4f);

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = sparkMat;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        static ParticleSystem CreateSparkle(Transform parent, Vector3 localPos, float radius)
        {
            var go = new GameObject("Sparkle");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.10f, 0.40f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.gravityModifier = -0.10f;
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)18) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(0.3f, radius);

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.75f, 0.85f, 1f), 0f), new GradientColorKey(new Color(0.85f, 0.65f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = gradient;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = sparkMat;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }
    }
}
