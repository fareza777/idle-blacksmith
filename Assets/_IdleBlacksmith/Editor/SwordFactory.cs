using System.Collections.Generic;
using IdleBlacksmith.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// Recipe swords and rarity gems. Every sword prefab is "Blade" + "Gem" children: the
    /// blade carries the recipe's metal colour on the shared palette material, and the gem
    /// lives in material slot 1 so the rarity material can be swapped per instance at runtime.
    /// Declared as a partial of ModelFactory so it reuses the mesh/prefab save helpers.
    /// </summary>
    public static partial class ModelFactory
    {
        public const string GemFolder = Paths.Materials + "/Gems";

        public static string SwordPrefabPath(string recipeId) => $"{Paths.Prefabs}/Sword_{recipeId}.prefab";

        public static string GemMaterialPath(Rarity rarity) => $"{GemFolder}/M_Gem_{rarity}.mat";

        /// <summary>Blade/guard/grip palette indices per recipe. Blades read as their metal.</summary>
        static readonly Dictionary<string, int[]> RecipeVisuals = new Dictionary<string, int[]>
        {
            { RecipeId.Copper,      new[] { Palette.Terracotta,  Palette.MetalDark,  Palette.Grip } },
            { RecipeId.Iron,        new[] { Palette.MetalDark,   Palette.MetalDark,  Palette.Grip } },
            { RecipeId.Steel,       new[] { Palette.MetalLight,  Palette.SwordGuard, Palette.Grip } },
            { RecipeId.EmberAxe,    new[] { Palette.Ember,       Palette.SwordGuard, Palette.Grip } },
            { RecipeId.Silver,      new[] { Palette.SwordBlade,  Palette.Straw,      Palette.Grip } },
            { RecipeId.Mithril,     new[] { Palette.OreCrystal,  Palette.Teal,       Palette.Grip } },
            { RecipeId.Dragonsteel, new[] { Palette.RugRed,      Palette.Ember,      Palette.Grip } },
            { RecipeId.Frostbrand,  new[] { Palette.OreCrystal,  Palette.SwordBlade, Palette.Grip } },
            { RecipeId.Voidreaver,  new[] { Palette.PlumDark,    Palette.MetalDark,  Palette.Grip } },
        };

        public static GameObject SwordPrefabFor(string recipeId)
            => AssetDatabase.LoadAssetAtPath<GameObject>(SwordPrefabPath(recipeId));

        public static void BuildRecipeSwords()
        {
            EnsureFolder(GemFolder);
            Material[] gems = BuildRarityGems();
            BuildSparklePrefab();
            foreach (string id in RecipeId.All)
                BuildOneSword(id, gems);
        }

        // ------------------------------------------------------------ gems

        static Material[] BuildRarityGems()
        {
            var gems = new List<Material>();
            for (int i = 0; i < RarityInfo.Count; i++)
            {
                var rarity = (Rarity)i;
                Color c = RarityInfo.GemColor(rarity);
                string path = GemMaterialPath(rarity);

                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(mat, path);
                }
                mat.SetFloat("_Smoothness", 0.8f);
                mat.SetFloat("_Metallic", 0f);
                mat.SetColor("_BaseColor", c * 0.55f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * (rarity >= Rarity.Epic ? 3.2f : 1.7f));
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                EditorUtility.SetDirty(mat);
                gems.Add(mat);
            }
            return gems.ToArray();
        }

        // ------------------------------------------------------------ swords

        static void BuildOneSword(string recipeId, Material[] gems)
        {
            if (!RecipeVisuals.TryGetValue(recipeId, out int[] colors))
                colors = new[] { Palette.SwordBlade, Palette.SwordGuard, Palette.Grip };

            var root = new GameObject("Sword_" + recipeId);

            var blade = new GameObject("Blade");
            blade.transform.SetParent(root.transform, false);
            blade.AddComponent<MeshFilter>().sharedMesh = BuildSwordBladeMesh(recipeId, colors);
            blade.AddComponent<MeshRenderer>().sharedMaterials = PM;

            var gem = new GameObject(GemChildName);
            gem.transform.SetParent(root.transform, false);
            gem.AddComponent<MeshFilter>().sharedMesh = BuildSwordGemMesh(recipeId);
            gem.AddComponent<MeshRenderer>().sharedMaterials = new[] { paletteMat, gems[(int)Rarity.Common] };

            SavePrefab(root, SwordPrefabPath(recipeId));
        }

        /// <summary>Must match Gameplay.SwordVisuals.GemChild.</summary>
        const string GemChildName = "Gem";

        /// <summary>
        /// Each recipe gets its own silhouette so the rack reads as six different weapons, not
        /// one recoloured sword. The blade runs +Z from the crossguard; the grip runs -Z.
        /// </summary>
        static Mesh BuildSwordBladeMesh(string recipeId, int[] colors)
        {
            var b = new MeshBuilder();
            int blade = colors[0], guard = colors[1], grip = colors[2];
            switch (recipeId)
            {
                case RecipeId.Copper: // squat dagger
                    b.Box(new Vector3(0, 0, 0.115f), new Vector3(0.055f, 0.014f, 0.19f), blade);
                    b.Box(new Vector3(0, 0, 0.215f), new Vector3(0.020f, 0.014f, 0.045f), blade);
                    b.Box(Vector3.zero, new Vector3(0.10f, 0.028f, 0.030f), guard);
                    b.Box(new Vector3(0, 0, -0.055f), new Vector3(0.038f, 0.038f, 0.09f), grip);
                    b.Box(new Vector3(0, 0, -0.115f), new Vector3(0.055f, 0.055f, 0.035f), guard);
                    break;
                case RecipeId.Steel: // curved sabre, three swept segments
                    b.Box(new Vector3(0, 0, 0.15f), new Vector3(0.050f, 0.014f, 0.26f), blade);
                    b.Box(new Vector3(0, 0.012f, 0.36f), new Vector3(0.042f, 0.014f, 0.20f), Quaternion.Euler(9f, 0f, 0f), blade);
                    b.Box(new Vector3(0, 0.045f, 0.50f), new Vector3(0.024f, 0.014f, 0.10f), Quaternion.Euler(18f, 0f, 0f), blade);
                    b.Box(Vector3.zero, new Vector3(0.14f, 0.028f, 0.034f), guard);
                    b.Box(new Vector3(0, 0.005f, -0.05f), new Vector3(0.11f, 0.020f, 0.05f), Quaternion.Euler(-20f, 0f, 0f), guard); // knuckle bow stub
                    b.Box(new Vector3(0, 0, -0.075f), new Vector3(0.040f, 0.040f, 0.12f), grip);
                    b.Box(new Vector3(0, 0, -0.15f), new Vector3(0.058f, 0.058f, 0.038f), guard);
                    break;
                case RecipeId.EmberAxe: // war axe: long haft, crescent head, back spike
                    b.Box(new Vector3(0, 0, 0.06f), new Vector3(0.042f, 0.042f, 0.52f), grip);   // haft
                    b.Box(new Vector3(0.085f, 0, 0.30f), new Vector3(0.16f, 0.016f, 0.11f), blade); // crescent, wide
                    b.Box(new Vector3(0.165f, 0, 0.335f), new Vector3(0.085f, 0.016f, 0.065f), Quaternion.Euler(0f, -18f, 0f), blade); // swept edge
                    b.Box(new Vector3(-0.075f, 0, 0.30f), new Vector3(0.10f, 0.014f, 0.035f), blade); // back spike
                    b.Box(new Vector3(0, 0, 0.30f), new Vector3(0.06f, 0.05f, 0.05f), guard);       // collar
                    b.Box(new Vector3(0, 0, -0.215f), new Vector3(0.05f, 0.05f, 0.045f), guard);    // butt cap
                    break;
                case RecipeId.Silver: // slim rapier with ring guard
                    b.Box(new Vector3(0, 0, 0.28f), new Vector3(0.028f, 0.012f, 0.50f), blade);
                    b.Box(new Vector3(0, 0, 0.545f), new Vector3(0.012f, 0.012f, 0.05f), blade);
                    b.Box(new Vector3(0, 0, 0.02f), new Vector3(0.19f, 0.014f, 0.030f), guard);   // ring: horizontal
                    b.Box(new Vector3(0, 0, 0.02f), new Vector3(0.014f, 0.19f, 0.030f), guard);   // ring: vertical
                    b.Box(new Vector3(0, 0, -0.06f), new Vector3(0.034f, 0.034f, 0.10f), grip);
                    b.Box(new Vector3(0, 0, -0.13f), new Vector3(0.052f, 0.052f, 0.034f), guard);
                    break;
                case RecipeId.Mithril: // katana: long slim single edge, small disc guard
                    b.Box(new Vector3(0, 0, 0.30f), new Vector3(0.038f, 0.013f, 0.54f), blade);
                    b.Box(new Vector3(0, 0.010f, 0.585f), new Vector3(0.030f, 0.013f, 0.055f), Quaternion.Euler(14f, 0f, 0f), blade);
                    b.Box(new Vector3(0, 0, 0.015f), new Vector3(0.11f, 0.050f, 0.022f), guard);   // tsuba
                    b.Box(new Vector3(0, 0, -0.09f), new Vector3(0.034f, 0.034f, 0.16f), grip);   // long wrap
                    b.Box(new Vector3(0, 0, -0.18f), new Vector3(0.046f, 0.046f, 0.030f), guard);
                    break;
                case RecipeId.Frostbrand: // glacier longsword: straight blade with rime barbs on one edge
                    b.Box(new Vector3(0, 0, 0.29f), new Vector3(0.048f, 0.013f, 0.50f), blade);
                    b.Box(new Vector3(0, 0, 0.565f), new Vector3(0.016f, 0.013f, 0.065f), blade);
                    b.Box(new Vector3(0.045f, 0, 0.36f), new Vector3(0.05f, 0.012f, 0.045f), Quaternion.Euler(0f, -22f, 0f), blade); // barb 1
                    b.Box(new Vector3(0.045f, 0, 0.46f), new Vector3(0.05f, 0.012f, 0.045f), Quaternion.Euler(0f, -22f, 0f), blade); // barb 2
                    b.Box(new Vector3(0.045f, 0, 0.52f), new Vector3(0.04f, 0.012f, 0.035f), Quaternion.Euler(0f, -22f, 0f), blade);  // barb 3
                    b.Box(Vector3.zero, new Vector3(0.15f, 0.028f, 0.034f), guard);
                    b.Box(new Vector3(0, 0, -0.065f), new Vector3(0.036f, 0.036f, 0.11f), grip);
                    b.Box(new Vector3(0, 0, -0.135f), new Vector3(0.06f, 0.06f, 0.045f), blade);   // rime crystal pommel
                    break;
                case RecipeId.Voidreaver: // void greatblade: split twin prong tip, dark steel
                    b.Box(new Vector3(0, 0, 0.30f), new Vector3(0.085f, 0.016f, 0.48f), blade);
                    b.Box(new Vector3(-0.028f, 0, 0.575f), new Vector3(0.038f, 0.016f, 0.09f), blade); // left prong
                    b.Box(new Vector3(0.028f, 0, 0.575f), new Vector3(0.038f, 0.016f, 0.09f), blade);  // right prong
                    b.Box(new Vector3(0, 0, 0.545f), new Vector3(0.018f, 0.016f, 0.05f), guard);        // void gap notch
                    b.Box(Vector3.zero, new Vector3(0.20f, 0.036f, 0.040f), guard);
                    b.Box(new Vector3(0, 0, -0.115f), new Vector3(0.046f, 0.046f, 0.19f), grip);
                    b.Box(new Vector3(0, 0, -0.235f), new Vector3(0.08f, 0.08f, 0.05f), guard);        // heavy dark pommel
                    break;
                case RecipeId.Dragonsteel: // greatsword: wide blade, parry hooks, two-hand grip
                    b.Box(new Vector3(0, 0, 0.30f), new Vector3(0.075f, 0.016f, 0.52f), blade);
                    b.Box(new Vector3(0, 0, 0.575f), new Vector3(0.030f, 0.016f, 0.07f), blade);
                    b.Box(new Vector3(-0.055f, 0, 0.09f), new Vector3(0.030f, 0.016f, 0.10f), guard); // left hook
                    b.Box(new Vector3(0.055f, 0, 0.09f), new Vector3(0.030f, 0.016f, 0.10f), guard);  // right hook
                    b.Box(Vector3.zero, new Vector3(0.19f, 0.034f, 0.038f), guard);
                    b.Box(new Vector3(0, 0, -0.115f), new Vector3(0.045f, 0.045f, 0.19f), grip);
                    b.Box(new Vector3(0, 0, -0.225f), new Vector3(0.075f, 0.075f, 0.045f), guard);   // heavy pommel
                    break;
                default: // Iron arming sword — the baseline shape
                    b.Box(new Vector3(0, 0, 0.21f), new Vector3(0.055f, 0.014f, 0.36f), blade);
                    b.Box(new Vector3(0, 0, 0.40f), new Vector3(0.026f, 0.014f, 0.05f), blade);
                    b.Box(Vector3.zero, new Vector3(0.16f, 0.03f, 0.035f), guard);
                    b.Box(new Vector3(0, 0, -0.07f), new Vector3(0.04f, 0.04f, 0.11f), grip);
                    b.Box(new Vector3(0, 0, -0.145f), new Vector3(0.06f, 0.06f, 0.04f), guard);
                    break;
            }
            return SaveMesh(b, "Sword_" + recipeId + "_Blade");
        }

        static Mesh BuildSwordGemMesh(string recipeId)
        {
            var b = new MeshBuilder();
            // Faceted stone set into the guard; emitted into material slot 1 (the gem slot).
            b.Rock(new Vector3(0f, 0.036f, 0.012f), new Vector3(0.024f, 0.021f, 0.024f), Palette.OreCrystal, 11.3f, 1);
            return SaveMesh(b, "Sword_" + recipeId + "_Gem");
        }

        // ------------------------------------------------------------ rarity sparkle

        public const string SparklePrefab = Paths.Prefabs + "/FX_RaritySparkle.prefab";

        static void BuildSparklePrefab()
        {
            var root = new GameObject("FX_RaritySparkle");
            var ps = root.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.055f);
            main.gravityModifier = -0.15f;   // drifts upward, like sparkles
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)16) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.09f;

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.85f, 0.45f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = gradient;

            var psr = root.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = sparkMat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.shadowCastingMode = ShadowCastingMode.Off;

            SavePrefab(root, SparklePrefab);
        }

        // ------------------------------------------------------------ folders

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
