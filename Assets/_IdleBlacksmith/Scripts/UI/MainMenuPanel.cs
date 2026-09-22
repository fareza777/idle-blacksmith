using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Title screen. It is an overlay on the live Shop scene rather than its own scene, so the
    /// forge keeps working behind it and no build settings have to change. CONTINUE resumes the
    /// saved forge; NEW GAME arms a wipe confirm before it restarts clean.
    /// </summary>
    public class MainMenuPanel : MonoBehaviour
    {
        [Header("Refs")]
        public CanvasGroup group;
        public Image background;
        public Image emblem;
        public RectTransform titleBlock;
        public TMP_Text titleLabel;
        public TMP_Text taglineLabel;
        public TMP_Text progressLabel;

        public BouncyButton continueButton;
        public BouncyButton newGameButton;
        public TMP_Text newGameLabel;
        public BouncyButton settingsButton;
        public BouncyButton aboutButton;
        public BouncyButton shareButton;
        public BouncyButton rateButton;

        public GameObject creditsRoot;
        public TMP_Text creditsBody;
        public Button creditsClose;

        [Header("New-game confirm")]
        public GameObject confirmRoot;
        public Button confirmYes;
        public Button confirmNo;

        public bool IsOpen { get; private set; }

        System.Action<bool> onPlay;

        /// <summary>True when there is a real saved forge to continue into.</summary>
        public static bool HasSave =>
            GameManager.Instance != null && GameManager.Instance.Data != null
            && GameManager.Instance.Data.everSaved
            && GameManager.Instance.Data.stats != null
            && (GameManager.Instance.Data.stats.swordsForged > 0
                || GameManager.Instance.Data.gold > 0
                || GameManager.Instance.Data.stats.playSeconds > 5f);

        public void Init()
        {
            gameObject.SetActive(false);
            if (creditsRoot != null) creditsRoot.SetActive(false);
            if (confirmRoot != null) confirmRoot.SetActive(false);
            if (continueButton != null) continueButton.onClick.AddListener(() => Play(false));
            if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
            if (settingsButton != null) settingsButton.onClick.AddListener(() => { AudioManager.Play("pop", 0.03f); SettingsRequested?.Invoke(); });
            if (aboutButton != null) aboutButton.onClick.AddListener(ShowCredits);
            if (shareButton != null) shareButton.onClick.AddListener(Share);
            if (rateButton != null) rateButton.onClick.AddListener(Rate);
            if (creditsClose != null) creditsClose.onClick.AddListener(() => { if (creditsRoot != null) creditsRoot.SetActive(false); });
            if (confirmYes != null) confirmYes.onClick.AddListener(ConfirmNewGame);
            if (confirmNo != null) confirmNo.onClick.AddListener(() => { AudioManager.Play("pop", 0.03f); if (confirmRoot != null) confirmRoot.SetActive(false); });
            if (creditsBody != null) creditsBody.text = CreditsText();
        }

        /// <summary>Raised when the player taps SETTINGS.</summary>
        public event System.Action SettingsRequested;

        public static string CreditsText() =>
            "EMBERFORGE — IDLE BLACKSMITH\n" +
            "a cozy forge adventure\n\n" +
            "The Ember chose you. Raise the smithy, open the mine,\n" +
            "feed the gate, and forge a legend the whole kingdom\n" +
            "talks about.\n\n" +
            "DESIGN, CODE & PROCEDURAL WORLD\n" +
            "CozyForge\n\n" +
            "ART\n" +
            "Menu, splash, cinematic and portrait art generated with\n" +
            "Replicate · black-forest-labs/flux-schnell\n" +
            "UI icons generated with Recraft v3\n" +
            "All 3D models, animations and the shop itself are\n" +
            "generated in code — no imported meshes.\n\n" +
            "AUDIO\n" +
            "Music and sound effects generated with ElevenLabs\n" +
            "(synthesised fallbacks ship in the build)\n\n" +
            "FONTS & LIBRARIES\n" +
            "Baloo 2 — SIL Open Font License\n" +
            "TextMeshPro — Unity Technologies\n" +
            "PrimeTween 1.4.11 — Apache-2.0, Kyrylo Kuzyk\n\n" +
            "Built with Unity 6000.3 · URP";

        public void Show(System.Action<bool> play)
        {
            onPlay = play;
            IsOpen = true;
            gameObject.SetActive(true);
            if (creditsRoot != null) creditsRoot.SetActive(false);
            if (confirmRoot != null) confirmRoot.SetActive(false);

            GameManager gm = GameManager.Instance;
            bool hasSave = HasSave;
            if (continueButton != null) continueButton.gameObject.SetActive(hasSave);
            if (newGameLabel != null)
                newGameLabel.text = hasSave ? "NEW GAME" : "START THE FORGE";

            if (progressLabel != null && gm != null && gm.Data != null)
            {
                StatBlock s = gm.Data.stats;
                progressLabel.text = s == null || s.swordsForged == 0
                    ? "The Ember waits for its new keeper"
                    : $"{GoldCounter.Format(gm.economy.Gold)} gold  ·  {s.swordsForged} swords forged"
                      + (gm.prestige != null && gm.prestige.Count > 0 ? $"  ·  rekindled {gm.prestige.Count}×" : "");
            }

            if (group != null)
            {
                group.alpha = 0f;
                Tween.Alpha(group, 1f, 0.4f, Ease.OutQuad);
            }
            if (titleBlock != null)
            {
                Vector2 p = titleBlock.anchoredPosition;
                titleBlock.anchoredPosition = p + Vector2.down * 30f;
                Tween.UIAnchoredPosition(titleBlock, p, 0.55f, Ease.OutCubic);
            }
            if (emblem != null)
            {
                emblem.transform.localScale = Vector3.one * 0.7f;
                Tween.Scale(emblem.transform, Vector3.one, 0.6f, Ease.OutBack);
            }
        }

        void NewGame()
        {
            if (HasSave)
            {
                AudioManager.Play("denied");
                if (confirmRoot != null) confirmRoot.SetActive(true);
                return;
            }
            Play(true);
        }

        void ConfirmNewGame()
        {
            AudioManager.Play("fanfare");
            // The save is wiped and the scene reloaded so every system boots fresh —
            // the intro cinematic then replays because introSeen is part of the save.
            UIManager.RequestNewGame();
        }

        void Play(bool newGame)
        {
            AudioManager.Play("unlock");
            IsOpen = false;
            if (group != null)
                Tween.Alpha(group, 0f, 0.35f, Ease.InQuad)
                    .OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                        onPlay?.Invoke(newGame);
                    });
            else
            {
                gameObject.SetActive(false);
                onPlay?.Invoke(newGame);
            }
        }

        void ShowCredits()
        {
            AudioManager.Play("pop", 0.03f);
            if (creditsRoot != null) creditsRoot.SetActive(!creditsRoot.activeSelf);
        }

        static string StoreUrl => "https://play.google.com/store/apps/details?id=" + Application.identifier;

        void Share()
        {
            AudioManager.Play("pop", 0.03f);
            string text = "I'm forging legendary swords in EMBERFORGE — the idle blacksmith game. "
                        + "Come swing a hammer: " + StoreUrl;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", "android.intent.action.SEND");
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text);
                intent.Call<AndroidJavaObject>("setType", "text/plain");
                var chooser = intent.CallStatic<AndroidJavaObject>("createChooser", intent, "Share Emberforge");
                activity.Call("startActivity", chooser);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Menu] share sheet failed: " + e.Message);
                GUIUtility.systemCopyBuffer = text;
            }
#else
            GUIUtility.systemCopyBuffer = text;
            Application.OpenURL(StoreUrl);
#endif
        }

        void Rate()
        {
            AudioManager.Play("pop", 0.03f);
#if UNITY_ANDROID && !UNITY_EDITOR
            Application.OpenURL("market://details?id=" + Application.identifier);
#else
            Application.OpenURL(StoreUrl);
#endif
        }

        public void PreviewOpenForScreenshot()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            if (group != null) group.alpha = 1f;
            if (titleBlock != null) titleBlock.anchoredPosition = new Vector2(0, titleBlock.anchoredPosition.y);
            if (progressLabel != null) progressLabel.text = "12.4K gold  ·  187 swords forged";
            if (continueButton != null) continueButton.gameObject.SetActive(true);
            if (creditsRoot != null) creditsRoot.SetActive(false);
            if (confirmRoot != null) confirmRoot.SetActive(false);
        }

        public void PreviewClose()
        {
            IsOpen = false;
            gameObject.SetActive(false);
        }
    }
}
