using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
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

        [MenuItem("Tools/Idle Blacksmith/Build Android APK")]
        public static void BuildApk()
        {
            EditorBoot.BuildAll(); // make sure generated assets/scene exist and are fresh

            // ------------------------------------------------ player settings
            PlayerSettings.companyName = "CozyForge";
            PlayerSettings.productName = "Idle Blacksmith RPG";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.cozyforge.idleblacksmith");
            // IL2CPP + ARM64: the reliably supported Android config on Unity 6
            // (Mono reported "target architecture not specified" from BuildPlayer despite PlayerSettings).
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            AssetDatabase.SaveAssets();
            Debug.Log($"[Android] arch readback={PlayerSettings.Android.targetArchitectures}, backend={PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)}, activeTarget={EditorUserBuildSettings.activeBuildTarget}");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.bundleVersion = "2.0";
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            EditorUserBuildSettings.buildAppBundle = false; // APK, not AAB
            AssetDatabase.SaveAssets();

            // ------------------------------------------------ build
            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneBuilder.ScenePath },
                target = BuildTarget.Android,
                locationPathName = ApkPath,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[IdleBlacksmith] Android build result: {report.summary.result}, " +
                      $"size {report.summary.totalSize / (1024f * 1024f):0.0} MB, " +
                      $"errors {report.summary.totalErrors}, time {report.summary.totalTime.TotalSeconds:0}s");
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception("[IdleBlacksmith] Android APK build failed — see log above.");
        }
    }
}
