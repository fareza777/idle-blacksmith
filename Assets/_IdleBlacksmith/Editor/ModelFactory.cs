using IdleBlacksmith.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// Builds the character + item prefabs. Chibi proportions, segmented limbs on
    /// pivots so the Animator can swing arms/legs (no skinning needed).
    /// WorldFactory (same partial class) builds stations, props and environment.
    /// </summary>
    public static partial class ModelFactory
    {
        public const string WorkerPrefab = Paths.Prefabs + "/Worker.prefab";
        public const string HelperPrefab = Paths.Prefabs + "/Helper.prefab";
        public const string CustomerAPrefab = Paths.Prefabs + "/CustomerA.prefab";
        public const string CustomerBPrefab = Paths.Prefabs + "/CustomerB.prefab";
        public const string SwordPrefab = Paths.Prefabs + "/Sword.prefab";
        public const string OreChunkPrefab = Paths.Prefabs + "/OreChunk.prefab";

        static Material paletteMat;
        static Material emissiveMat;
        static Material crystalMat;
        static Material hotSwordMat;
        static Material blobMat;
        static Material sparkMat;
        static Material smokeMat;

        static Material[] PM => new[] { paletteMat };
        static Material[] PME => new[] { paletteMat, emissiveMat };
        static Material[] PMC => new[] { paletteMat, crystalMat };

        static void LoadMaterials()
        {
            paletteMat = AssetDatabase.LoadAssetAtPath<Material>(AssetFactory.MatPalette);
            emissiveMat = AssetDatabase.LoadAssetAtPath<Material>(AssetFactory.MatEmissive);
            crystalMat = AssetDatabase.LoadAssetAtPath<Material>(AssetFactory.MatCrystal);
            hotSwordMat = AssetDatabase.LoadAssetAtPath<Material>(AssetFactory.MatHotSword);
            blobMat = AssetDatabase.LoadAssetAtPath<Material>(AssetFactory.MatBlob);
            sparkMat = AssetDatabase.LoadAssetAtPath<Material>(AssetFactory.MatParticle);
            smokeMat = AssetDatabase.LoadAssetAtPath<Material>(AssetFactory.MatSmoke);
        }

        public static void BuildAll()
        {
            LoadMaterials();
            BuildItems();
            BuildRecipeSwords();
            BuildCharacters();
            BuildStations();
            BuildEnvironment();
            BuildBuildings();
            AssetDatabase.SaveAssets();
        }

        static Mesh SaveMesh(MeshBuilder b, string name)
            => AssetReplace.SaveMesh(b.Build(name), name, $"{Paths.Models}/{name}.asset");

        static GameObject Part(string name, Transform parent, Mesh mesh, Material[] mats, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
            return go;
        }

        static Mesh blobQuadMesh; // cached: SaveMesh deletes+recreates, so re-saving per character would dangle earlier prefab refs

        static void AddBlobShadow(Transform root, float radius)
        {
            if (blobQuadMesh == null)
            {
                // Solid dark diamond on the shared palette material: guaranteed to render,
                // and a stylized solid blob suits the low-poly look better than a soft sprite.
                var b = new MeshBuilder();
                b.Face(Vector3.zero, new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f), Palette.Coal);
                blobQuadMesh = SaveMesh(b, "BlobShadowQuad");
            }
            GameObject shadow = Part("Shadow", root, blobQuadMesh, PM, new Vector3(0, 0.02f, 0));
            shadow.transform.localScale = Vector3.one * radius;
            shadow.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------ items

        static Mesh BuildSwordMesh(int mat)
        {
            var b = new MeshBuilder();
            b.Box(new Vector3(0, 0, 0.21f), new Vector3(0.055f, 0.014f, 0.36f), Palette.SwordBlade, mat);
            b.Box(new Vector3(0, 0, 0.40f), new Vector3(0.026f, 0.014f, 0.05f), Palette.SwordBlade, mat);
            b.Box(Vector3.zero, new Vector3(0.16f, 0.03f, 0.035f), Palette.SwordGuard, mat);
            b.Box(new Vector3(0, 0, -0.07f), new Vector3(0.04f, 0.04f, 0.11f), Palette.Grip, mat);
            b.Box(new Vector3(0, 0, -0.145f), new Vector3(0.06f, 0.06f, 0.04f), Palette.SwordGuard, mat);
            return b.Build("SwordMesh");
        }

        static void BuildItems()
        {
            // Sword (blade along +Z, pivot at guard).
            {
                carriedSwordMesh = AssetReplace.SaveMesh(
                    BuildSwordMesh(0), "Sword", $"{Paths.Models}/Sword.asset");
                var root = new GameObject("Sword");
                Part("Blade", root.transform, carriedSwordMesh, PM, Vector3.zero);
                SavePrefab(root, SwordPrefab);
            }
            // Ore chunk (rock + crystal).
            {
                var b = new MeshBuilder();
                b.Rock(Vector3.zero, new Vector3(0.085f, 0.075f, 0.085f), Palette.OreRock, 3.7f);
                b.Box(new Vector3(0.04f, 0.05f, 0.03f), new Vector3(0.045f, 0.06f, 0.045f), Palette.OreCrystal, 1);
                Mesh mesh = SaveMesh(b, "OreChunk");
                var root = new GameObject("OreChunk");
                Part("Rock", root.transform, mesh, PMC, Vector3.zero);
                SavePrefab(root, OreChunkPrefab);
            }
            // Hand hammer: grip at the anchor, head forward along +Z (same convention as
            // the carried sword prop).
            {
                var b = new MeshBuilder();
                b.Box(new Vector3(0, 0, 0.09f), new Vector3(0.034f, 0.034f, 0.19f), Palette.Grip);
                b.Box(new Vector3(0, 0.005f, 0.20f), new Vector3(0.075f, 0.075f, 0.12f), Palette.MetalLight);
                b.Box(new Vector3(0, 0.005f, 0.265f), new Vector3(0.052f, 0.052f, 0.02f), Palette.MetalDark);
                b.Box(new Vector3(0, 0, -0.015f), new Vector3(0.05f, 0.05f, 0.035f), Palette.MetalDark);
                carriedHammerMesh = AssetReplace.SaveMesh(
                    b.Build("HammerMesh"), "CarriedHammer", $"{Paths.Models}/CarriedHammer.asset");
            }
        }

        // ------------------------------------------------------------ characters

        static void BuildCharacters()
        {
            BuildCharacter("Worker", Palette.ShirtBlue, Palette.HairBrown, true, Hat.Cap, Palette.Pants);
            BuildCharacter("Helper", Palette.ShirtGreen, Palette.HairBlond, true, Hat.Headband, Palette.Pants);
            BuildCharacter("CustomerA", Palette.ShirtOrange, Palette.HairBrown, false, Hat.Straw, Palette.PlumDark);
            BuildCharacter("CustomerB", Palette.Teal, Palette.HairBlack, false, Hat.Buns, Palette.PlumDark);
        }

        enum Hat { Cap, Straw, Buns, Headband }

        static void BuildCharacter(string name, int shirt, int hair, bool apron, Hat hat, int pants)
        {
            var root = new GameObject(name);
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);

            // Body (pivot at hip, y=0.34).
            var b = new MeshBuilder();
            b.Box(new Vector3(0, 0.26f, 0), new Vector3(0.44f, 0.52f, 0.30f), shirt);
            if (apron)
            {
                b.Box(new Vector3(0, 0.22f, 0.165f), new Vector3(0.32f, 0.40f, 0.045f), Palette.Apron);
                b.Box(new Vector3(0, 0.47f, 0.16f), new Vector3(0.20f, 0.10f, 0.04f), Palette.Apron);
            }
            b.Box(new Vector3(0, 0.03f, 0), new Vector3(0.46f, 0.08f, 0.32f), Palette.Boots); // belt
            Mesh bodyMesh = SaveMesh(b, name + "_Body");
            Part("Body", model.transform, bodyMesh, PM, new Vector3(0, 0.34f, 0));

            // Head (pivot at neck, y=0.88).
            b = new MeshBuilder();
            b.Box(new Vector3(0, 0.21f, 0), new Vector3(0.46f, 0.42f, 0.42f), Palette.Skin);
            b.Box(new Vector3(0.115f, 0.24f, 0.212f), new Vector3(0.055f, 0.09f, 0.02f), Palette.EyeDark);
            b.Box(new Vector3(-0.115f, 0.24f, 0.212f), new Vector3(0.055f, 0.09f, 0.02f), Palette.EyeDark);
            b.Box(new Vector3(0.175f, 0.13f, 0.208f), new Vector3(0.06f, 0.04f, 0.02f), Palette.Cheek);
            b.Box(new Vector3(-0.175f, 0.13f, 0.208f), new Vector3(0.06f, 0.04f, 0.02f), Palette.Cheek);
            switch (hat)
            {
                case Hat.Cap:
                    b.Box(new Vector3(0, 0.44f, -0.02f), new Vector3(0.49f, 0.12f, 0.44f), Palette.Apron);
                    b.Box(new Vector3(0, 0.40f, 0.26f), new Vector3(0.30f, 0.05f, 0.16f), Palette.Apron);
                    b.Box(new Vector3(0, 0.30f, -0.20f), new Vector3(0.46f, 0.20f, 0.05f), hair);
                    break;
                case Hat.Straw:
                    b.Cylinder(new Vector3(0, 0.455f, 0), 0.36f, 0.05f, 10, Palette.Straw);
                    b.Cylinder(new Vector3(0, 0.53f, 0), 0.17f, 0.13f, 8, Palette.Straw);
                    b.Box(new Vector3(0, 0.30f, -0.20f), new Vector3(0.46f, 0.18f, 0.05f), hair);
                    break;
                case Hat.Buns:
                    b.Box(new Vector3(0, 0.44f, -0.02f), new Vector3(0.48f, 0.12f, 0.44f), hair);
                    b.Box(new Vector3(0, 0.30f, -0.20f), new Vector3(0.46f, 0.22f, 0.05f), hair);
                    b.Box(new Vector3(0.27f, 0.36f, -0.05f), new Vector3(0.13f, 0.13f, 0.13f), hair);
                    b.Box(new Vector3(-0.27f, 0.36f, -0.05f), new Vector3(0.13f, 0.13f, 0.13f), hair);
                    break;
                case Hat.Headband:
                    b.Box(new Vector3(0, 0.44f, -0.02f), new Vector3(0.48f, 0.12f, 0.44f), hair);
                    b.Box(new Vector3(0, 0.31f, 0.01f), new Vector3(0.49f, 0.07f, 0.45f), Palette.RedAccent);
                    break;
            }
            Mesh headMesh = SaveMesh(b, name + "_Head");
            Part("Head", model.transform, headMesh, PM, new Vector3(0, 0.88f, 0));

            // Arms (pivot at shoulder, mesh hangs down).
            b = new MeshBuilder();
            b.Box(new Vector3(0, -0.11f, 0), new Vector3(0.17f, 0.24f, 0.19f), shirt);
            b.Box(new Vector3(0, -0.29f, 0.01f), new Vector3(0.15f, 0.15f, 0.16f), Palette.Skin);
            Mesh armMesh = SaveMesh(b, name + "_Arm");
            Part("ArmL", model.transform, armMesh, PM, new Vector3(-0.295f, 0.80f, 0));
            GameObject armR = Part("ArmR", model.transform, armMesh, PM, new Vector3(0.295f, 0.80f, 0));

            var handAnchor = new GameObject("HandAnchor");
            handAnchor.transform.SetParent(armR.transform, false);
            handAnchor.transform.localPosition = new Vector3(0, -0.36f, 0.05f);

            // Legs (pivot at hip).
            b = new MeshBuilder();
            b.Box(new Vector3(0, -0.15f, 0), new Vector3(0.17f, 0.30f, 0.20f), pants);
            b.Box(new Vector3(0, -0.30f, 0.04f), new Vector3(0.18f, 0.10f, 0.27f), Palette.Boots);
            Mesh legMesh = SaveMesh(b, name + "_Leg");
            Part("LegL", model.transform, legMesh, PM, new Vector3(-0.12f, 0.34f, 0));
            Part("LegR", model.transform, legMesh, PM, new Vector3(0.12f, 0.34f, 0));

            AddBlobShadow(root.transform, 0.38f);

            // Components.
            var animator = root.AddComponent<Animator>();
            animator.applyRootMotion = false;
            root.AddComponent<SimpleWalker>();

            bool isCustomer = name.StartsWith("Customer");
            if (isCustomer)
            {
                var cc = root.AddComponent<CustomerController>();
                cc.model = model.transform;
                if (carriedSwordMesh != null)
                {
                    GameObject prop = Part("CarriedSword", handAnchor.transform, carriedSwordMesh, PM,
                        new Vector3(0, 0.02f, 0.05f));
                    prop.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
                    prop.SetActive(false);
                    cc.carriedSwordProp = prop;
                }
                SavePrefab(root, Paths.Prefabs + "/" + name + ".prefab");
            }
            else
            {
                var wc = root.AddComponent<WorkerController>();
                wc.model = model.transform;
                wc.handAnchor = handAnchor.transform;
                if (carriedHammerMesh != null)
                {
                    GameObject hammer = Part("CarriedHammer", handAnchor.transform, carriedHammerMesh, PM,
                        new Vector3(0, 0.02f, 0.04f));
                    hammer.transform.localRotation = Quaternion.Euler(32f, 0f, 0f);
                    wc.hammerProp = hammer;
                }
                SavePrefab(root, Paths.Prefabs + "/" + name + ".prefab");
            }
        }

        static Mesh carriedSwordMesh;
        static Mesh carriedHammerMesh;

        static void SavePrefab(GameObject root, string path)
            => AssetReplace.SavePrefab(root, path);
    }
}
