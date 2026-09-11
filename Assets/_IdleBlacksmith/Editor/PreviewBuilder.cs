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

            // restore
            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            cam.orthographicSize = origSize;
            canvas.renderMode = origMode;
            canvas.worldCamera = origCam;
            canvas.planeDistance = origPlane;
            Debug.Log("[Preview] Captured 6 screenshots to _Screenshots/");
        }

        static void Render(Camera cam, int w, int h, string path)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
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
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Debug.Log("[Preview] wrote " + path);
        }
    }
}
