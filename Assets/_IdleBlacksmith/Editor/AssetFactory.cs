using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace IdleBlacksmith.EditorTools
{
    public static class Paths
    {
        public const string Root = "Assets/_IdleBlacksmith";
        public const string Art = Root + "/Art";
        public const string Textures = Art + "/Textures";
        public const string UiSprites = Art + "/UI";
        public const string IconRaw = Art + "/Icons/Raw";
        public const string Models = Art + "/Models";
        public const string Materials = Art + "/Materials";
        public const string Prefabs = Root + "/Prefabs";
        public const string Animations = Root + "/Animations";
        public const string Audio = Root + "/Audio";
        public const string Fonts = Root + "/Fonts";
        public const string Resources = Root + "/Resources";
        public const string Scenes = Root + "/Scenes";
        public const string Settings = Root + "/Settings";

        public static void EnsureAll()
        {
            EnsureFolder("Assets", "_IdleBlacksmith");
            EnsureFolder(Root, "Art"); EnsureFolder(Root, "Prefabs"); EnsureFolder(Root, "Animations");
            EnsureFolder(Root, "Audio"); EnsureFolder(Root, "Fonts"); EnsureFolder(Root, "Resources");
            EnsureFolder(Root, "Scenes"); EnsureFolder(Root, "Settings");
            EnsureFolder(Art, "Textures"); EnsureFolder(Art, "UI"); EnsureFolder(Art, "Icons");
            EnsureFolder(Art, "Models"); EnsureFolder(Art, "Materials");
            EnsureFolder(Art + "/Icons", "Raw");
        }

        static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }

    /// <summary>Textures, materials, UI sprites, icon import, fonts/TMP setup.</summary>
    public static class AssetFactory
    {
        public const string PaletteTexPath = Paths.Textures + "/Palette.png";
        public const string BlobTexPath = Paths.UiSprites + "/blob.png";

        // ------------------------------------------------------------ palette

        public static void EnsurePalette()
        {
            var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var px = new Color32[128 * 128];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            for (int cell = 0; cell < Palette.Colors.Length; cell++)
            {
                int cx = (cell % 8) * 16, cy = (cell / 8) * 16;
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        // Unity texture origin = bottom-left; palette UV uses top-left cells first.
                        int texY = 128 - 16 - cy + y;
                        px[texY * 128 + cx + x] = Palette.Colors[cell];
                    }
            }
            tex.SetPixels32(px);
            tex.Apply(false);
            SavePng(tex, PaletteTexPath);
            Object.DestroyImmediate(tex);

            var imp = (TextureImporter)AssetImporter.GetAtPath(PaletteTexPath);
            imp.textureType = TextureImporterType.Default;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 128;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        // ------------------------------------------------------------ materials

        public const string MatPalette = Paths.Materials + "/M_Palette.mat";
        public const string MatEmissive = Paths.Materials + "/M_Emissive.mat";
        public const string MatCrystal = Paths.Materials + "/M_Crystal.mat";
        public const string MatHotSword = Paths.Materials + "/M_HotSword.mat";
        public const string MatBlob = Paths.Materials + "/M_BlobShadow.mat";
        public const string MatParticle = Paths.Materials + "/M_Spark.mat";

        public static void EnsureMaterials()
        {
            Texture palette = AssetDatabase.LoadAssetAtPath<Texture2D>(PaletteTexPath);
            CreateLit(MatPalette, palette, 0.12f);
            CreateEmissive(MatEmissive, new Color(1f, 0.45f, 0.1f), 2.6f);   // forge coals, lantern
            CreateEmissive(MatCrystal, new Color(0.35f, 0.85f, 1f), 0.9f);    // ore crystals
            CreateEmissive(MatHotSword, new Color(1f, 0.36f, 0.08f), 3.2f);   // sword on anvil
            CreateBlobShadow();
            CreateSpark();
        }

        static Material BaseLit(string path, float smooth)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_Smoothness", smooth);
            mat.SetFloat("_Metallic", 0f);
            return mat;
        }

        static void CreateLit(string path, Texture tex, float smooth)
        {
            Material mat = BaseLit(path, smooth);
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);
        }

        static void CreateEmissive(string path, Color color, float intensity)
        {
            Material mat = BaseLit(path, 0.35f);
            mat.SetColor("_BaseColor", color * 0.35f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
        }

        static void MakeTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        static void CreateBlobShadow()
        {
            EnsureBlobTexture();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatBlob);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, MatBlob);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BlobTexPath));
            mat.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0.32f));
            MakeTransparent(mat);
            EditorUtility.SetDirty(mat);
        }

        static void CreateSpark()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatParticle);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, MatParticle);
            }
            mat.SetColor("_BaseColor", new Color(2.2f, 1.15f, 0.35f));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f); // additive
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_BLENDMODE_ADDITIVE");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
        }

        // ------------------------------------------------------------ textures / sprites

        public static void SavePng(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }

        static void EnsureBlobTexture()
        {
            if (File.Exists(BlobTexPath)) return;
            const int s = 128;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            SavePng(tex, BlobTexPath);
            Object.DestroyImmediate(tex);
            var imp = (TextureImporter)AssetImporter.GetAtPath(BlobTexPath);
            imp.textureType = TextureImporterType.Default;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }

        /// <summary>White rounded-rect / circle sprites with 9-slice borders for tinting.</summary>
        public static void EnsureUiSprites()
        {
            MakeSprite("rounded", 132, 132, 34, new Vector4(34, 34, 34, 34));
            MakeSprite("rounded_small", 68, 68, 18, new Vector4(18, 18, 18, 18));
            MakeSprite("pill", 200, 100, 50, new Vector4(60, 60, 49, 49));
            MakeSprite("circle", 64, 64, 32, new Vector4(24, 24, 24, 24));
            MakeSprite("bar", 48, 20, 10, new Vector4(12, 12, 9, 9));
        }

        static void MakeSprite(string name, int w, int h, float radius, Vector4 border)
        {
            string path = $"{Paths.UiSprites}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = RoundedRectSdf(x + 0.5f, y + 0.5f, w, h, radius);
                        float a = Mathf.Clamp01(0.5f - d);
                        px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                tex.SetPixels32(px);
                tex.Apply(false);
                SavePng(tex, path);
                Object.DestroyImmediate(tex);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.spriteBorder = border;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.maxTextureSize = 256;
            imp.SaveAndReimport();
        }

        static float RoundedRectSdf(float x, float y, float w, float h, float r)
        {
            float cx = w * 0.5f, cy = h * 0.5f;
            float qx = Mathf.Abs(x - cx) - (cx - r);
            float qy = Mathf.Abs(y - cy) - (cy - r);
            float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        // ------------------------------------------------------------ icons

        /// <summary>Sets sprite import settings on every icon in Art/Icons/Raw.</summary>
        public static void ProcessIcons()
        {
            if (!Directory.Exists(Paths.IconRaw)) return;
            foreach (string file in Directory.GetFiles(Paths.IconRaw, "*.png"))
            {
                // AssetDatabase paths require forward slashes.
                string assetPath = file.Replace('\\', '/');

                // A PNG written by IconFallback may not be imported yet, in which case there is no
                // importer to configure and the texture would stay a plain Texture (not a Sprite).
                AssetImporter imp = AssetImporter.GetAtPath(assetPath);
                if (imp == null)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                    imp = AssetImporter.GetAtPath(assetPath);
                }
                if (!(imp is TextureImporter tex)) continue;

                tex.textureType = TextureImporterType.Sprite;
                tex.spriteImportMode = SpriteImportMode.Single;
                tex.alphaIsTransparency = true;
                tex.mipmapEnabled = false;
                tex.filterMode = FilterMode.Bilinear;
                tex.maxTextureSize = 512;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.SaveAndReimport();
            }
        }

        public static Sprite LoadIcon(string name)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Paths.IconRaw}/{name}.png");
            if (s == null)
                Debug.LogWarning($"[AssetFactory] icon '{name}' did not import as a Sprite — the UI will show an empty box");
            return s;
        }

        // ------------------------------------------------------------ menu art (Replicate)

        public const string MenuArtDir = Paths.Art + "/Menu";

        /// <summary>Imports every generated menu artwork (Art/Menu) as a sprite.</summary>
        public static void ProcessMenuArt()
        {
            if (!Directory.Exists(MenuArtDir)) return;
            foreach (string file in Directory.GetFiles(MenuArtDir, "*.png"))
            {
                var imp = AssetImporter.GetAtPath(file.Replace('\\', '/')) as TextureImporter;
                if (imp == null) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Bilinear;
                imp.maxTextureSize = 2048;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
        }

        public static Sprite LoadMenuArt(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{MenuArtDir}/{name}.png");
        }

        // ------------------------------------------------------------ app icon

        public const string AppIconPath = Paths.Art + "/App/app_icon.png";

        /// <summary>
        /// Imports Art/App/app_icon.png as a plain texture and registers it as the app icon
        /// (legacy + Android round/adaptive slots). Missing file → icon stays as-is.
        /// </summary>
        public static void ApplyAppIcon()
        {
            if (!File.Exists(AppIconPath))
            {
                Debug.LogWarning($"[AssetFactory] {AppIconPath} missing — app icon unchanged");
                return;
            }
            AssetDatabase.ImportAsset(AppIconPath, ImportAssetOptions.ForceSynchronousImport);
            var imp = AssetImporter.GetAtPath(AppIconPath) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Default;
                imp.alphaIsTransparency = false;
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Bilinear;
                imp.maxTextureSize = 1024;
                imp.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (tex == null)
            {
                Debug.LogWarning("[AssetFactory] app icon failed to import — app icon unchanged");
                return;
            }
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.BuiltIn, new[] { tex });
            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { tex }, IconKind.Any);
            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { tex }, IconKind.Round);
            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { tex }, IconKind.Adaptive);
            Debug.Log("[AssetFactory] app icon applied from " + AppIconPath);
        }

        // ------------------------------------------------------------ fonts / TMP

        public const string FontBodyPath = Paths.Fonts + "/Baloo2-500.asset";
        public const string FontTitlePath = Paths.Fonts + "/Baloo2-800.asset";
        public const string TmpSettingsPath = Paths.Resources + "/TMP Settings.asset";

        public static TMP_FontAsset EnsureFontAsset(string ttfName, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            Font font = AssetDatabase.LoadAssetAtPath<Font>($"{Paths.Fonts}/{ttfName}");
            if (font == null)
            {
                Debug.LogWarning($"[AssetFactory] {ttfName} not found, using built-in font.");
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font);
            fa.name = Path.GetFileNameWithoutExtension(assetPath);
            fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(fa, assetPath);
            if (fa.material != null) AssetDatabase.AddObjectToAsset(fa.material, assetPath);
            if (fa.atlasTextures != null)
                foreach (Texture2D t in fa.atlasTextures)
                    if (t != null) AssetDatabase.AddObjectToAsset(t, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        }

        /// <summary>Creates the TMP Settings asset early (empty) so TMP_Settings.instance
        /// resolves while font assets are being created.</summary>
        public static void EnsureTmpSettingsAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) != null) return;
            var settings = ScriptableObject.CreateInstance<TMP_Settings>();
            AssetDatabase.CreateAsset(settings, TmpSettingsPath);
            AssetDatabase.SaveAssets();
        }

        public static void EnsureTmpSettings(TMP_FontAsset defaultFont)
        {
            if (defaultFont == null) return;
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<TMP_Settings>();
                AssetDatabase.CreateAsset(settings, TmpSettingsPath);
            }
            var so = new SerializedObject(settings);
            SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
            if (prop != null)
            {
                prop.objectReferenceValue = defaultFont;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}
