using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// One-shot Android APK build for on-device testing.
    /// Run headless:
    ///   Unity.exe -batchmode -quit -buildTarget Android -projectPath "&lt;proj&gt;" `
    ///     -executeMethod IdleBlacksmith.EditorTools.BuildAndroid.BuildApk
    /// Produces _Builds/IdleBlacksmith.apk (debug-signed, IL2CPP, ARM64, GLES3).
    /// </summary>
    public static class BuildAndroid
    {
        const string ApkPath = "_Builds/IdleBlacksmith.apk";
        const string AabPath = "_Builds/Emberforge.aab";

        /// <summary>
        /// Pass this on the command line to skip the asset rebuild. The player build then runs in
        /// a clean editor session that only reads already-generated assets, which keeps it away
        /// from the import caches that regenerating everything would invalidate mid-build.
        /// </summary>
        const string SkipRebuildFlag = "-skipAssetRebuild";

        [MenuItem("Tools/Idle Blacksmith/Build Android APK")]
        public static void BuildApk()
        {
            bool skipRebuild = System.Array.IndexOf(
                System.Environment.GetCommandLineArgs(), SkipRebuildFlag) >= 0;

            if (!skipRebuild) EditorBoot.BuildAll(); // make sure generated assets/scene exist and are fresh

            // ------------------------------------------------ player settings
            PlayerSettings.companyName = "CozyForge";
            PlayerSettings.productName = "Emberforge: Idle Blacksmith";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.cozyforge.emberforge");
            // IL2CPP + ARM64: the reliably supported Android config on Unity 6
            // (Mono reported "target architecture not specified" from BuildPlayer despite PlayerSettings).
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            AssetDatabase.SaveAssets();
            Debug.Log($"[Android] arch readback={PlayerSettings.Android.targetArchitectures}, backend={PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)}, activeTarget={EditorUserBuildSettings.activeBuildTarget}");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.bundleVersion = "3.0";
            PlayerSettings.Android.bundleVersionCode = 3;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            EditorUserBuildSettings.buildAppBundle = false; // APK, not AAB
            AssetDatabase.SaveAssets();

            BuildTo(ApkPath);
        }

        /// <summary>
        /// Play Store upload artifact: the same player as an Android App Bundle (.aab).
        /// Signing still needs a release keystore — the bundle comes out debug-signed
        /// until PlayerSettings.Android keystore fields are filled in.
        /// Run headless:
        ///   Unity.exe -batchmode -quit -buildTarget Android -projectPath "&lt;proj&gt;" `
        ///     -executeMethod IdleBlacksmith.EditorTools.BuildAndroid.BuildAab
        /// </summary>
        [MenuItem("Tools/Idle Blacksmith/Build Android AAB")]
        public static void BuildAab()
        {
            bool skipRebuild = System.Array.IndexOf(
                System.Environment.GetCommandLineArgs(), SkipRebuildFlag) >= 0;

            if (!skipRebuild) EditorBoot.BuildAll();

            PlayerSettings.companyName = "CozyForge";
            PlayerSettings.productName = "Emberforge: Idle Blacksmith";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.cozyforge.emberforge");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.bundleVersion = "3.0";
            PlayerSettings.Android.bundleVersionCode = 3;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            EditorUserBuildSettings.buildAppBundle = true; // Play Store artifact
            AssetDatabase.SaveAssets();

            BuildTo(AabPath);
        }

        static void BuildTo(string locationPath)
        {
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneBuilder.ScenePath },
                target = BuildTarget.Android,
                locationPathName = locationPath,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[IdleBlacksmith] Android build result: {report.summary.result}, " +
                      $"size {report.summary.totalSize / (1024f * 1024f):0.0} MB, " +
                      $"errors {report.summary.totalErrors}, time {report.summary.totalTime.TotalSeconds:0}s");
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception("[IdleBlacksmith] Android build failed — see log above.");
        }

        // The player build serializes scenes from disk. BuildAll has just rewritten the shop
        // scene, and leaving it open and dirty while the build runs is what produces a
        // half-written level0 that crashes on device with "level0 is corrupted". BuildTo
        // flushes everything to disk and lets the build read a clean, closed scene instead.
    }
}
