using System.IO;
using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// Renders verification screenshots of the Shop scene without entering play mode:
    /// portrait HUD, upgrade sheet, dungeon panel, splash, onboarding, and a counter close-up.
    /// Output goes to _Screenshots/ next to Assets.
    /// </summary>
    public static class PreviewBuilder
    {
        public static void CaptureAll()
        {
            if (!File.Exists(SceneBuilder.ScenePath))
            {
                Debug.LogWarning("[Preview] Scene missing; build first.");
                return;
            }
            EditorSceneManager.OpenScene(SceneBuilder.ScenePath, OpenSceneMode.Single);
            var cam = Object.FindFirstObjectByType<Camera>();
            // The world-space bars are canvases too; the HUD canvas is the one with UIManager.
            var ui = Object.FindFirstObjectByType<UIManager>();
            var canvas = ui != null ? ui.GetComponent<Canvas>() : Object.FindFirstObjectByType<Canvas>();
            if (cam == null || canvas == null)
            {
                Debug.LogWarning("[Preview] Camera or Canvas missing.");
                return;
            }
            Directory.CreateDirectory("_Screenshots");

            // Give the systems a live state so panels show real levels and costs.
            GameManager preload = Object.FindFirstObjectByType<GameManager>();
            if (preload != null) preload.EnsureInitializedForPreview(1);

            RenderMode origMode = canvas.renderMode;
            Camera origCam = canvas.worldCamera;
            float origPlane = canvas.planeDistance;
            Vector3 origPos = cam.transform.position;
            Quaternion origRot = cam.transform.rotation;
            float origSize = cam.orthographicSize;

            // ScreenSpaceCamera so overlay UI renders into the target texture.
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = cam.nearClipPlane + 0.3f; // keep UI in front of world geometry
            Canvas.ForceUpdateCanvases();
            Render(cam, 810, 1440, "_Screenshots/1_hud.png");

            if (ui != null && ui.upgradePanel != null)
            {
                ui.upgradePanel.PreviewOpenForScreenshot();
                Canvas.ForceUpdateCanvases();
                Render(cam, 810, 1440, "_Screenshots/2_upgrades.png");
                ui.upgradePanel.PreviewClose();
            }

            if (ui != null && ui.dungeonPanel != null)
            {
                ui.dungeonPanel.PreviewOpenForScreenshot();
                Canvas.ForceUpdateCanvases();
                Render(cam, 810, 1440, "_Screenshots/3_dungeon.png");
                ui.dungeonPanel.PreviewClose();
            }

            if (ui != null && ui.splashScreen != null)
            {
                ui.splashScreen.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                Render(cam, 810, 1440, "_Screenshots/4_splash.png");
                ui.splashScreen.gameObject.SetActive(false);
            }

            if (ui != null && ui.onboardingPanel != null)
            {
                ui.onboardingPanel.PreviewShow();
                Canvas.ForceUpdateCanvases();
                Render(cam, 810, 1440, "_Screenshots/5_onboarding.png");
                ui.onboardingPanel.PreviewHide();
            }

            // Close-up of the counter sales area without UI.
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cam.transform.position = new Vector3(3.4f, 3.4f, -5.2f);
            cam.transform.LookAt(new Vector3(0.6f, 0.8f, -1.6f));
            cam.orthographicSize = 1.9f;
            Render(cam, 1080, 1080, "_Screenshots/6_closeup.png");

            // Remaining sheets all render in overlay mode against a frozen camera.
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = cam.nearClipPlane + 0.3f;
            RenderOverlay(cam, canvas, ui, 810, 1440);

            // restore
            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            cam.orthographicSize = origSize;
            canvas.renderMode = origMode;
            canvas.worldCamera = origCam;
            canvas.planeDistance = origPlane;
            Debug.Log("[Preview] Captured screenshots to _Screenshots/");
        }

        /// <summary>
        /// Frames the whole complex with every building at a chosen level. This is how the
        /// building placement and the camera pull-back get checked without playing the game.
        /// </summary>
        [MenuItem("Tools/Idle Blacksmith/Capture Complex Stages", priority = 3)]
        public static void CaptureComplexStages()
        {
            if (!File.Exists(SceneBuilder.ScenePath))
            {
                Debug.LogWarning("[Preview] Scene missing; build first.");
                return;
            }
            EditorSceneManager.OpenScene(SceneBuilder.ScenePath, OpenSceneMode.Single);

            var cam = Object.FindFirstObjectByType<Camera>();
            var director = Object.FindFirstObjectByType<IdleBlacksmith.Gameplay.CameraDirector>();
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (cam == null || gm == null || gm.buildings == null)
            {
                Debug.LogWarning("[Preview] Camera or GameManager missing.");
                return;
            }
            Directory.CreateDirectory("_Screenshots");

            var ui = Object.FindFirstObjectByType<UIManager>();
            if (ui != null) ui.gameObject.SetActive(false);

            var plots = Object.FindObjectsByType<IdleBlacksmith.Gameplay.BuildingVisuals>(FindObjectsSortMode.None);
            gm.EnsureInitializedForPreview();

            // The framing depends on the aspect ratio, so match the capture size up front.
            float origAspect = cam.aspect;
            cam.aspect = 810f / 1440f;

            int[] stages = { 0, 1, 3, 5 };
            foreach (int level in stages)
            {
                int smithyLevel = Mathf.Clamp(level < 1 ? 1 : level, 1, ModelFactory.EnvironmentTierPrefabs.Length);
                foreach (BuildingDef def in gm.config.buildings)
                {
                    if (def == null) continue;
                    // Stage 0 is the true fresh game: only the Smithy, everything else a bare plot.
                    int target = def.id == BuildingId.Smithy ? smithyLevel : level;
                    gm.buildings.ForceLevel(def.id, target);
                    // In edit mode Start() has not run, so the visuals are applied directly.
                    foreach (var plot in plots)
                        if (plot != null && plot.buildingId == def.id) plot.Apply(target);
                    if (def.id == BuildingId.Smithy) SwapEnvironment(gm, smithyLevel);
                }
                Canvas.ForceUpdateCanvases();
                if (director != null) director.SnapToTarget();
                Render(cam, 810, 1440, $"_Screenshots/16_complex_L{level}.png");
            }

            if (ui != null) ui.gameObject.SetActive(true);
            cam.aspect = origAspect;
            Debug.Log("[Preview] captured complex stages");
        }

        /// <summary>
        /// Replaces the shop environment in edit mode. GameManager.SpawnEnvironment uses Destroy,
        /// which is deferred and never happens outside play mode, so the preview needs its own swap.
        /// </summary>
        static void SwapEnvironment(GameManager gm, int level)
        {
            if (gm.environmentRoot == null || gm.environmentPrefabs == null) return;
            int idx = Mathf.Clamp(level - 1, 0, gm.environmentPrefabs.Length - 1);
            GameObject prefab = gm.environmentPrefabs[idx];
            if (prefab == null) return;

            for (int i = gm.environmentRoot.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(gm.environmentRoot.GetChild(i).gameObject);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetParent(gm.environmentRoot, false);
        }

        /// <summary>
        /// Captures every panel added with the complex expansion. Each one is opened through its
        /// own preview hook, rendered, then closed, so the scene is left as it was found.
        /// </summary>
        static void RenderOverlay(Camera cam, Canvas canvas, UIManager ui, int w, int h)
        {
            if (ui == null) return;

            if (ui.mainMenuPanel != null)
            {
                ui.mainMenuPanel.PreviewOpenForScreenshot();
                Canvas.ForceUpdateCanvases();
                Render(cam, w, h, "_Screenshots/7_mainmenu.png");
                ui.mainMenuPanel.PreviewClose();
            }

            if (ui.complexPanel != null)
            {
                ui.complexPanel.PreviewOpenForScreenshot();
                Canvas.ForceUpdateCanvases();
                Render(cam, w, h, "_Screenshots/8_complex.png");
                ui.complexPanel.PreviewClose();
            }

            if (ui.forgePanel != null)
            {
                ui.forgePanel.PreviewOpenForScreenshot();
                Canvas.ForceUpdateCanvases();
                Render(cam, w, h, "_Screenshots/9_forge.png");
                ui.forgePanel.PreviewClose();
            }

            if (ui.questPanel != null)
            {
                ui.questPanel.PreviewOpenForScreenshot();
                Canvas.ForceUpdateCanvases();
                Render(cam, w, h, "_Screenshots/10_quest.png");
                ui.questPanel.PreviewClose();
            }

            if (ui.metaPanel != null)
            {
                ui.metaPanel.PreviewOpenForScreenshot(false);
                Canvas.ForceUpdateCanvases();
                Render(cam, w, h, "_Screenshots/11_achievements.png");
                ui.metaPanel.ShowPage(1);
                Canvas.ForceUpdateCanvases();
                Render(cam, w, h, "_Screenshots/12_stats.png");
                ui.metaPanel.PreviewClose();
            }

            if (ui.prestigePanel != null)
            {
                ui.prestigePanel.PreviewOpenForScreenshot();
                Canvas.ForceUpdateCanvases();
                Render(cam, w, h, "_Screenshots/13_prestige.png");
                ui.prestigePanel.PreviewClose();
            }

            if (ui.settingsPanel != null)
            {
                ui.settingsPanel.PreviewOpenForScreenshot();
                Canvas.ForceUpdateCanvases();
                Render(cam, w, h, "_Screenshots/14_settings.png");
                ui.settingsPanel.PreviewClose();
            }

            if (ui.welcomeBackPanel != null)
            {
                ui.welcomeBackPanel.PreviewOpenForScreenshot();
                Canvas.ForceUpdateCanvases();
                Render(cam, w, h, "_Screenshots/15_welcome.png");
                ui.welcomeBackPanel.PreviewClose();
            }

            var dialogueMgr = Object.FindFirstObjectByType<IdleBlacksmith.Core.DialogueManager>();
            if (ui.dialoguePanel != null && dialogueMgr != null)
            {
                var seq = new DialogueSequence
                {
                    id = "preview",
                    lines = new[]
                    {
                        new DialogueLine
                        {
                            speaker = "Petra Flint",
                            portrait = AssetFactory.LoadMenuArt("portrait_petra"),
                            text = "Ore while you sleep, ore while you eat. Just keep my lanterns lit, smith.",
                        },
                    },
                };
                ui.dialoguePanel.PreviewShow(seq);
                Canvas.ForceUpdateCanvases();
                Render(cam, w, h, "_Screenshots/16_dialogue.png");
                ui.dialoguePanel.PreviewHide();
            }

            if (ui.introCinematic != null)
            {
                ui.introCinematic.PreviewShow(0);
                Canvas.ForceUpdateCanvases();
                Render(cam, w, h, "_Screenshots/17_intro.png");
                ui.introCinematic.PreviewHide();
            }
        }

        static void Render(Camera cam, int w, int h, string path)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            // The Game View's aspect has nothing to do with the capture size, so an orthographic
            // camera would otherwise frame for the wrong width. Set it before rendering.
            float origAspect = cam.aspect;
            cam.aspect = w / (float)h;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            cam.aspect = origAspect;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Debug.Log("[Preview] wrote " + path);
        }
    }
}
