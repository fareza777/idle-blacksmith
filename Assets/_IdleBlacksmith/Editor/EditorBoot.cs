using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>URP pipeline asset + player/project settings.</summary>
    public static class ProjectSetup
    {
        public static void Apply()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.productName = "Idle Blacksmith RPG";
            PlayerSettings.companyName = "CozyForge";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.DeleteAsset(SceneBuilder.UrpRendererPath);
                AssetDatabase.CreateAsset(rendererData, SceneBuilder.UrpRendererPath);
                var urp = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.DeleteAsset(SceneBuilder.UrpAssetPath);
                AssetDatabase.CreateAsset(urp, SceneBuilder.UrpAssetPath);

                TrySetFloat(urp, "m_ShadowDistance", 30f);
                TrySetBool(urp, "m_SoftShadowsSupported", true);
                TrySetBool(urp, "m_SupportsHDR", true);
                TrySetInt(urp, "m_MSAA", 4);
                TrySetInt(urp, "m_MainLightShadowmapResolution", 2048);
                EditorUtility.SetDirty(urp);

                GraphicsSettings.defaultRenderPipeline = urp;
                AssetDatabase.SaveAssets();
                Debug.Log("[ProjectSetup] URP pipeline created and assigned.");
            }
        }

        static void TrySetFloat(Object o, string prop, float v)
        {
            var so = new SerializedObject(o);
            SerializedProperty p = so.FindProperty(prop);
            if (p != null && p.propertyType == SerializedPropertyType.Float)
            {
                p.floatValue = v;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void TrySetInt(Object o, string prop, int v)
        {
            var so = new SerializedObject(o);
            SerializedProperty p = so.FindProperty(prop);
            if (p != null && (p.propertyType == SerializedPropertyType.Integer || p.propertyType == SerializedPropertyType.Enum))
            {
                p.intValue = v;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void TrySetBool(Object o, string prop, bool v)
        {
            var so = new SerializedObject(o);
            SerializedProperty p = so.FindProperty(prop);
            if (p != null && p.propertyType == SerializedPropertyType.Boolean)
            {
                p.boolValue = v;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    /// <summary>
    /// One-shot project bootstrap. Auto-runs on first editor open (marker file in
    /// UserSettings); afterwards available from the menu for manual rebuilds.
    /// </summary>
    public static class EditorBoot
    {
        const string Marker = "UserSettings/ib_booted.marker";
        static bool building;

        [InitializeOnLoadMethod]
        static void Auto()
        {
            EditorApplication.delayCall += TryAuto;
        }

        static void TryAuto()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAuto;
                return;
            }
            if (building || File.Exists(Marker)) return;
            try
            {
                BuildAll();
                Directory.CreateDirectory("UserSettings");
                File.WriteAllText(Marker, System.DateTime.Now.ToString("s"));
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MenuItem("Tools/Idle Blacksmith/Rebuild Everything", priority = 0)]
        public static void BuildAllMenu()
        {
            try
            {
                BuildAll();
                Directory.CreateDirectory("UserSettings");
                File.WriteAllText(Marker, System.DateTime.Now.ToString("s"));
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MenuItem("Tools/Idle Blacksmith/Open Shop Scene", priority = 1)]
        public static void OpenScene()
        {
            if (File.Exists(SceneBuilder.ScenePath))
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneBuilder.ScenePath);
            else
                Debug.LogWarning("[IdleBlacksmith] Scene not built yet. Run Tools > Idle Blacksmith > Rebuild Everything.");
        }

        [MenuItem("Tools/Idle Blacksmith/Capture Previews", priority = 2)]
        public static void CaptureMenu() => PreviewBuilder.CaptureAll();

        public static void BuildAll()
        {
            if (building) return;
            building = true;
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                Log("folders", () => Paths.EnsureAll());
                Log("project setup", () => ProjectSetup.Apply());
                Log("fallback icons", () => IconFallback.Ensure());
                Log("icon import", () => AssetFactory.ProcessIcons());
                Log("menu art import", () => AssetFactory.ProcessMenuArt());
                Log("palette", () => AssetFactory.EnsurePalette());
                Log("ui sprites", () => AssetFactory.EnsureUiSprites());
                Log("materials", () => AssetFactory.EnsureMaterials());

                Log("tmp essentials", () => ImportTmpEssentials());
                Log("tmp settings asset", () => AssetFactory.EnsureTmpSettingsAsset());
                TMP_FontAsset body = null, title = null;
                Log("body font", () => body = AssetFactory.EnsureFontAsset("Baloo2-500.ttf", AssetFactory.FontBodyPath));
                Log("title font", () => title = AssetFactory.EnsureFontAsset("Baloo2-800.ttf", AssetFactory.FontTitlePath));
                Log("tmp settings", () => AssetFactory.EnsureTmpSettings(title != null ? title : body));

                Log("audio fallbacks", () => AudioSynth.EnsureFallbacks());
                Log("models", () => ModelFactory.BuildAll());
                Log("animations", () => AnimationFactory.BuildAll());
                Log("scene", () => SceneBuilder.Build());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[IdleBlacksmith] BUILD OK in {sw.ElapsedMilliseconds / 1000f:0.0}s");
                Directory.CreateDirectory("UserSettings");
                File.WriteAllText(Marker, System.DateTime.Now.ToString("s"));
            }
            finally
            {
                building = false;
            }
        }

        /// <summary>
        /// Unity 6.3 ships the TMP runtime shaders inside "TMP Essential Resources.unitypackage"
        /// (Packages/com.unity.ugui/Package Resources). Import them so Shader.Find works, then
        /// remove the package's own TMP Settings so OUR settings asset (in our Resources folder)
        /// is the only "TMP Settings" Resources.Load can resolve.
        /// </summary>
        static void ImportTmpEssentials()
        {
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
            {
                string cache = Path.GetFullPath("Library/PackageCache");
                foreach (string dir in Directory.GetDirectories(cache, "com.unity.ugui@*"))
                {
                    string pkg = Path.Combine(dir, "Package Resources/TMP Essential Resources.unitypackage");
                    if (File.Exists(pkg))
                    {
                        AssetDatabase.ImportPackage(pkg, false);
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        break;
                    }
                }
            }
            const string pkgSettings = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (File.Exists(pkgSettings))
                AssetDatabase.DeleteAsset(pkgSettings);
        }

        /// <summary>
        /// Headless play-mode smoke test: opens the Shop scene, enters play mode (no domain
        /// reload so our hooks survive), simulates ~4s, then reports system state and exits
        /// with code 0/1. Run WITHOUT -quit; this method exits the editor itself.
        /// </summary>
        [MenuItem("Tools/Idle Blacksmith/Play-Mode Smoke Test")]
        public static void SmokeTest()
        {
            if (!File.Exists(SceneBuilder.ScenePath))
            {
                Debug.LogWarning("[Smoke] Scene missing; build first.");
                EditorApplication.Exit(1);
                return;
            }
            EditorSceneManager.OpenScene(SceneBuilder.ScenePath, OpenSceneMode.Single);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

            int frames = 0, exceptions = 0;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();

            void OnLog(string msg, string stack, LogType type)
            {
                // Count only game exceptions; Unity 6.3 logs an unrelated editor-internal
                // UnityEditor.Search ArgumentOutOfRangeException during headless startup.
                if (type == LogType.Exception && (stack.Contains("IdleBlacksmith") || msg.Contains("IdleBlacksmith")))
                    exceptions++;
                else if (type == LogType.Exception)
                    Debug.Log("[Smoke] ignoring editor-internal exception: " + msg.Split('\n')[0]);
            }

            void Tick()
            {
                if (!EditorApplication.isPlaying) return;
                frames++;
                if (frames >= 240)
                {
                    var gm = IdleBlacksmith.Core.GameManager.Instance;
                    var econ = Object.FindFirstObjectByType<IdleBlacksmith.Core.EconomyManager>();
                    var worker = Object.FindFirstObjectByType<IdleBlacksmith.Gameplay.WorkerController>();
                    var rack = Object.FindFirstObjectByType<IdleBlacksmith.Gameplay.SwordRack>();
                    Debug.Log($"[Smoke] frames={frames} gm={(gm != null)} econ={(econ != null)} worker={(worker != null)} rack={(rack != null)} exceptions={exceptions}");
                    Application.logMessageReceived -= OnLog;
                    EditorApplication.update -= Tick;
                    EditorApplication.ExitPlaymode();
                    EditorApplication.Exit(exceptions == 0 ? 0 : 1);
                }
            }
        }

        static void Log(string step, System.Action action)
        {
            Debug.Log($"[IdleBlacksmith] Building {step}...");
            action();
        }
    }
}
