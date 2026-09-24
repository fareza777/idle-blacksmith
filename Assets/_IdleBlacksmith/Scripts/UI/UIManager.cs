using System.Collections.Generic;
using IdleBlacksmith.Core;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// HUD hub and screen router. Owns the launch flow (splash -> main menu -> onboarding ->
    /// game), makes sure only one sheet is open at a time, and routes the Android back button
    /// to "close the top panel" instead of "quit".
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        public GoldCounter goldCounter;
        public TMPro.TMP_Text oreLabel;
        public TMPro.TMP_Text metalOreLabel;
        public TMPro.TMP_Text metalOreRateLabel;
        public Button muteButton;
        public Image muteIcon;
        public CanvasGroup flashOverlay;
        public Image flashTint;
        public Sprite soundOnSprite;
        public Sprite soundOffSprite;

        [Header("HUD buttons")]
        public BouncyButton complexButton;
        public BouncyButton forgeButton;
        public BouncyButton dungeonButton;
        public BouncyButton questButton;
        public BouncyButton menuButton;
        public GameObject dungeonBadge;
        public GameObject questBadge;
        public PulseLoop complexButtonPulse;
        public BouncyButton upgradesButton;
        public PulseLoop upgradesButtonPulse;

        [Header("Panels")]
        public ComplexPanel complexPanel;
        public ForgePanel forgePanel;
        public OrderTicker orderTicker;
        public RushBanner rushBanner;
        public DailyClaimPanel dailyPanel;
        public DungeonPanel dungeonPanel;
        public QuestPanel questPanel;
        public UpgradePanel upgradePanel;
        public MetaPanel metaPanel;
        public PrestigePanel prestigePanel;
        public SettingsPanel settingsPanel;

        [Header("Menu flow")]
        public SplashScreen splashScreen;
        public MainMenuPanel mainMenuPanel;
        public OnboardingPanel onboardingPanel;
        public WelcomeBackPanel welcomeBackPanel;
        public IntroCinematic introCinematic;
        public DialoguePanel dialoguePanel;

        /// <summary>Set before a scene reload so the next boot skips the menu and lands in the intro.</summary>
        static bool pendingNewGame;

        /// <summary>Wipes the save and reboots the scene so every system starts cold.</summary>
        public static void RequestNewGame()
        {
            pendingNewGame = true;
            SaveSystem.DeleteSave();
            PlayerPrefs.Save();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }

        public bool IntroPlaying => introCinematic != null && introCinematic.IsPlaying;
        /// <summary>True once the menu's Play button finished routing — guards against modals racing the fade.</summary>
        public bool HasLaunched => launched;

        [Header("HUD chrome")]
        public HudTicker ticker;
        public CanvasGroup hudGroup;

        [Header("Floating text")]
        public RectTransform floatingTextLayer;
        public FloatingText floatingTextPrefab;
        [Min(2)] public int floatingTextPoolSize = 12;

        static readonly Color GoldColor = new Color(1f, 0.84f, 0.28f);

        Camera mainCamera;
        readonly Queue<FloatingText> pool = new Queue<FloatingText>();
        bool launched;
        float badgeCheckTimer;
        // Static on purpose: this build pipeline corrupts level0 when extra instance
        // fields get serialized onto scene MonoBehaviours.
        static Vector2 lastFloaterPos;
        static float lastFloaterAt = -10f;
        static int floaterStreak;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.economy != null)
            {
                if (goldCounter != null) goldCounter.SetValueInstant(gm.economy.Gold);
                gm.economy.OnGoldChanged += HandleGoldChanged;
                gm.OnRelicOreChanged += HandleOreChanged;
                HandleOreChanged(gm.RelicOre);
            }
            if (gm != null && gm.resources != null)
            {
                gm.resources.OnOreChanged += HandleMetalOreChanged;
                HandleMetalOreChanged(gm.resources.Ore, gm.resources.OreCapacity);
            }

            WireButtons();
            InitPanels();
            RefreshMuteIcon();
            PrewarmPool();
            SetDungeonBadge(gm != null && gm.expeditions != null && gm.expeditions.ReadyToClaim);

            if (hudGroup != null) hudGroup.alpha = 0f;

            // Music starts under the splash so the menu already has its theme — deep once
            // the smithy has reached the third tier.
            AudioManager.PlayMusic(gm != null && gm.ShopTier >= 3 ? "music_deep" : "music_forge", 2f);
            AudioManager.PlayAmbience("amb_fire", 3f);

            // Launch flow: splash, then the title screen, then the intro once, then onboarding.
            if (splashScreen != null)
                splashScreen.Play(() => BootAfterSplash(gm));
            else
                BootAfterSplash(gm);
        }

        void BootAfterSplash(GameManager gm)
        {
            if (pendingNewGame)
            {
                pendingNewGame = false;
                AfterMenu(gm, true);
                return;
            }
            ShowMenu(gm);
        }

        void WireButtons()
        {
            if (menuButton != null) menuButton.onClick.AddListener(OpenMenu);
            if (muteButton != null) muteButton.onClick.AddListener(ToggleMute);
            if (complexButton != null && complexPanel != null)
                complexButton.onClick.AddListener(() =>
                {
                    OpenExclusive(complexPanel);
                    if (complexButtonPulse != null) complexButtonPulse.Stop();
                });
            if (forgeButton != null && forgePanel != null)
                forgeButton.onClick.AddListener(() => OpenExclusive(forgePanel));
            if (dungeonButton != null && dungeonPanel != null)
                dungeonButton.onClick.AddListener(() =>
                {
                    OpenExclusive(dungeonPanel);
                    SetDungeonBadge(false);
                });
            if (questButton != null && questPanel != null)
                questButton.onClick.AddListener(() => OpenExclusive(questPanel));
            if (upgradesButton != null && upgradePanel != null)
                upgradesButton.onClick.AddListener(() =>
                {
                    OpenExclusive(upgradePanel);
                    if (upgradesButtonPulse != null) upgradesButtonPulse.Stop();
                });
        }

        void InitPanels()
        {
            if (upgradePanel != null) upgradePanel.Init();
            if (dungeonPanel != null) dungeonPanel.Init();
            if (complexPanel != null) complexPanel.Init();
            if (forgePanel != null) forgePanel.Init();
            if (orderTicker != null) orderTicker.Init();
            if (rushBanner != null) rushBanner.Init();
            if (questPanel != null) questPanel.Init();
            if (metaPanel != null) metaPanel.Init();
            if (prestigePanel != null) prestigePanel.Init();
            if (settingsPanel != null) settingsPanel.Init();
            if (welcomeBackPanel != null) welcomeBackPanel.Init();
            if (dailyPanel != null && GameManager.Instance != null)
                dailyPanel.Init(GameManager.Instance.daily);
            if (mainMenuPanel != null)
            {
                mainMenuPanel.Init();
                mainMenuPanel.SettingsRequested += OpenSettings;
            }
            if (settingsPanel != null)
                settingsPanel.MenuRequested += OpenMenu;
            if (ticker != null) ticker.Init();
        }

        // ------------------------------------------------------------ launch flow

        void ShowMenu(GameManager gm)
        {
            if (mainMenuPanel != null)
                mainMenuPanel.Show(newGame => AfterMenu(gm, newGame));
            else
                AfterMenu(gm, false);
        }

        void AfterMenu(GameManager gm, bool newGame)
        {
            if (launched) return;
            launched = true;

            // The cinematic belongs to a fresh forge; returning players never see it twice.
            // The HUD stays dark underneath it so pills never bleed through the letterbox.
            if (introCinematic != null && gm != null && gm.Data != null && !gm.Data.introSeen)
            {
                if (hudGroup != null) hudGroup.alpha = 0f;
                introCinematic.Play(() => AfterIntro(gm));
                return;
            }
            if (hudGroup != null) hudGroup.alpha = 1f;
            AfterIntro(gm);
        }

        void AfterIntro(GameManager gm)
        {
            RevealHud();
            if (gm != null && !gm.HasSeenOnboarding && onboardingPanel != null)
            {
                onboardingPanel.onFinished = () => ShowWelcomeBack(gm);
                onboardingPanel.Show();
                return;
            }
            ShowWelcomeBack(gm);
        }

        /// <summary>HUD fades in once the story overlays are done — never during the cinematic.</summary>
        void RevealHud()
        {
            if (hudGroup == null || hudGroup.alpha > 0.99f) return;
            Tween.Alpha(hudGroup, 1f, 0.5f, Ease.OutQuad);
        }

        /// <summary>Offline payout sheet, shown once per launch when there is something to collect.</summary>
        void ShowWelcomeBack(GameManager gm)
        {
            if (gm == null || welcomeBackPanel == null) return;
            welcomeBackPanel.Show(gm.LastOffline);
        }

        public void OpenMenu()
        {
            if (mainMenuPanel == null) return;
            CloseAllPanels();
            mainMenuPanel.Show(_ => { if (hudGroup != null) hudGroup.alpha = 1f; });
        }

        void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.Open();
        }

        // ------------------------------------------------------------ screen routing

        /// <summary>Opens one sheet and closes any other, so panels can never stack.</summary>
        void OpenExclusive(MonoBehaviour panel)
        {
            if (panel == null) return;

            if (panel != complexPanel && complexPanel != null && complexPanel.IsOpen) complexPanel.Close();
            if (panel != forgePanel && forgePanel != null && forgePanel.IsOpen) forgePanel.Close();
            if (panel != dungeonPanel && dungeonPanel != null && dungeonPanel.IsOpen) dungeonPanel.Close();
            if (panel != questPanel && questPanel != null && questPanel.IsOpen) questPanel.Close();
            if (panel != upgradePanel && upgradePanel != null && upgradePanel.IsOpen) upgradePanel.Close();
            if (panel != metaPanel && metaPanel != null && metaPanel.IsOpen) metaPanel.Close();
            if (panel != prestigePanel && prestigePanel != null && prestigePanel.IsOpen) prestigePanel.Close();

            if (panel == complexPanel) complexPanel.Open();
            else if (panel == forgePanel) forgePanel.Open();
            else if (panel == dungeonPanel) dungeonPanel.Open();
            else if (panel == questPanel) questPanel.Open();
            else if (panel == upgradePanel) upgradePanel.Open();
        }

        public MetaPanel Meta => metaPanel;
        public PrestigePanel Prestige => prestigePanel;

        /// <summary>Opens the forge sheet — used by the order banner's "switch recipe" tap.</summary>
        public void OpenForge()
        {
            OpenExclusive(forgePanel);
        }

        /// <summary>Replays the opening cinematic on demand (from Settings).</summary>
        public void ReplayIntro()
        {
            if (introCinematic == null || introCinematic.IsPlaying) return;
            AudioManager.PlayMusic("music_intro", 0.6f);
            if (hudGroup != null) hudGroup.alpha = 0f;
            introCinematic.Play(() =>
            {
                RevealHud();
                AudioManager.PlayMusic(ThemeId(), 1.5f);
            });
        }

        /// <summary>The workshop theme this save should hear — deep-forge from smithy tier three.</summary>
        public static string ThemeId()
        {
            var gm = GameManager.Instance;
            return gm != null && gm.ShopTier >= 3 ? "music_deep" : "music_forge";
        }

        /// <summary>Opens the complex sheet with one building's row highlighted (world tap).</summary>
        public void OpenComplexFocused(string buildingId)
        {
            if (complexPanel == null) return;
            OpenExclusive(complexPanel);
            complexPanel.OpenFocused(buildingId);
        }

        public void OpenMeta(bool stats)
        {
            if (metaPanel == null) return;
            metaPanel.ShowPage(stats ? 1 : 0);
            OpenExclusive(metaPanel);
            metaPanel.Open();
        }

        public void OpenPrestige()
        {
            if (prestigePanel == null) return;
            OpenExclusive(prestigePanel);
            prestigePanel.Open();
        }

        public bool AnyPanelOpen
        {
            get
            {
                return (complexPanel != null && complexPanel.IsOpen)
                    || (forgePanel != null && forgePanel.IsOpen)
                    || (dungeonPanel != null && dungeonPanel.IsOpen)
                    || (questPanel != null && questPanel.IsOpen)
                    || (upgradePanel != null && upgradePanel.IsOpen)
                    || (metaPanel != null && metaPanel.IsOpen)
                    || (prestigePanel != null && prestigePanel.IsOpen)
                    || (settingsPanel != null && settingsPanel.IsOpen)
                    || (welcomeBackPanel != null && welcomeBackPanel.IsOpen)
                    || (dailyPanel != null && dailyPanel.IsOpen)
                    || (mainMenuPanel != null && mainMenuPanel.IsOpen)
                    || (onboardingPanel != null && onboardingPanel.gameObject.activeSelf);
            }
        }

        void CloseAllPanels()
        {
            if (complexPanel != null && complexPanel.IsOpen) complexPanel.Close();
            if (forgePanel != null && forgePanel.IsOpen) forgePanel.Close();
            if (dungeonPanel != null && dungeonPanel.IsOpen) dungeonPanel.Close();
            if (questPanel != null && questPanel.IsOpen) questPanel.Close();
            if (upgradePanel != null && upgradePanel.IsOpen) upgradePanel.Close();
            if (metaPanel != null && metaPanel.IsOpen) metaPanel.Close();
            if (prestigePanel != null && prestigePanel.IsOpen) prestigePanel.Close();
            if (settingsPanel != null && settingsPanel.IsOpen) settingsPanel.Close();
        }

        void Update()
        {
            // Android back: skip the cinematic, close the top sheet, quit on an empty screen.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IntroPlaying) introCinematic.SkipIntro();
                else if (AnyPanelOpen) CloseAllPanels();
                else Application.Quit();
            }

            badgeCheckTimer += Time.deltaTime;
            if (badgeCheckTimer >= 0.5f)
            {
                badgeCheckTimer = 0f;
                GameManager gm = GameManager.Instance;
                if (gm != null)
                {
                    SetDungeonBadge(gm.expeditions != null && gm.expeditions.ReadyToClaim);
                    if (questBadge != null && gm.quests != null)
                        questBadge.SetActive(gm.quests.IsComplete);
                }
            }
        }

        // ------------------------------------------------------------ hud updates

        void HandleGoldChanged(int total, int delta)
        {
            if (goldCounter == null) return;
            goldCounter.SetValue(total);
            if (delta > 0) goldCounter.Punch();
        }

        void HandleOreChanged(int ore)
        {
            if (oreLabel != null)
            {
                oreLabel.text = ore.ToString();
                Tween.PunchScale(oreLabel.transform, Vector3.one * 0.14f, 0.3f);
            }
        }

        void HandleMetalOreChanged(int ore, int capacity)
        {
            if (metalOreLabel != null) metalOreLabel.text = ore.ToString();
            if (metalOreRateLabel != null)
            {
                ResourceManager res = GameManager.Instance != null ? GameManager.Instance.resources : null;
                metalOreRateLabel.text = res != null ? "+" + res.OrePerSecond.ToString("0.#") + "/s" : "";
                metalOreRateLabel.color = (res != null && res.IsFull)
                    ? new Color(1f, 0.72f, 0.4f)
                    : new Color(0.78f, 0.86f, 0.90f);
            }
        }

        public void SetDungeonBadge(bool on)
        {
            if (dungeonBadge != null && dungeonBadge.activeSelf != on)
                dungeonBadge.SetActive(on);
        }

        void ToggleMute()
        {
            AudioManager.Muted = !AudioManager.Muted;
            RefreshMuteIcon();
            if (!AudioManager.Muted) AudioManager.Play("pop", 0.03f);
        }

        void RefreshMuteIcon()
        {
            if (muteIcon != null)
                muteIcon.sprite = AudioManager.Muted ? soundOffSprite : soundOnSprite;
        }

        void PrewarmPool()
        {
            if (floatingTextPrefab == null || floatingTextLayer == null) return;
            for (int i = 0; i < floatingTextPoolSize; i++)
            {
                FloatingText ft = Instantiate(floatingTextPrefab, floatingTextLayer);
                ft.gameObject.SetActive(false);
                pool.Enqueue(ft);
            }
        }

        /// <summary>Full-screen color pulse — the rekindle flash, a soft daily-claim glow.</summary>
        public void FlashScreen(Color color, float peak = 0.8f, float duration = 0.9f)
        {
            if (flashOverlay == null) return;
            if (flashTint != null) flashTint.color = color;
            flashOverlay.alpha = 0f;
            Tween.Alpha(flashOverlay, peak, duration * 0.3f, Ease.OutQuad)
                .OnComplete(() => Tween.Alpha(flashOverlay, 0f, duration * 0.7f, Ease.InQuad));
        }

        public void SpawnFloatingText(Vector3 worldPos, string text) =>
            SpawnFloatingText(worldPos, text, GoldColor);

        public void SpawnFloatingText(Vector3 worldPos, string text, Color color)
        {
            if (floatingTextPrefab == null || floatingTextLayer == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 screen = mainCamera.WorldToScreenPoint(worldPos);
            if (screen.z < 0f) return;

            Canvas canvas = floatingTextLayer.GetComponentInParent<Canvas>();
            Camera uiCam = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                floatingTextLayer, screen, uiCam, out Vector2 local);

            // Burst stagger: several floaters hitting the same anchor at once fan
            // upward/sideways instead of stacking into one unreadable blob.
            bool burst = Time.unscaledTime - lastFloaterAt < 0.9f
                && (local - lastFloaterPos).sqrMagnitude < 14400f;
            floaterStreak = burst ? floaterStreak + 1 : 0;
            lastFloaterAt = Time.unscaledTime;
            lastFloaterPos = local;
            if (floaterStreak > 0)
            {
                local.y += floaterStreak * 40f;
                local.x += ((floaterStreak & 1) == 0 ? 1f : -1f) * floaterStreak * 30f;
            }

            FloatingText ft = pool.Count > 0 ? pool.Dequeue() : Instantiate(floatingTextPrefab, floatingTextLayer);
            ft.Play(local, text, color, f => pool.Enqueue(f));
        }

        // ------------------------------------------------------------ coin flight

        readonly Queue<Image> coinPool = new Queue<Image>();
        Sprite coinSprite;

        /// <summary>A coin arcs from a world-space sale point to the gold pill — the money beat.</summary>
        public void FlyCoin(Vector3 worldPos)
        {
            if (floatingTextLayer == null || goldCounter == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 screen = mainCamera.WorldToScreenPoint(worldPos);
            if (screen.z < 0f) return;

            Canvas canvas = floatingTextLayer.GetComponentInParent<Canvas>();
            Camera uiCam = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                floatingTextLayer, screen, uiCam, out Vector2 from);

            if (coinSprite == null && goldCounter.coinIcon != null)
                coinSprite = goldCounter.coinIcon.GetComponentInChildren<Image>()?.sprite;

            // Both elements share the canvas, so InverseTransformPoint lands in the
            // layer's anchor space without caring about the canvas render mode.
            Vector3 iconWorld = goldCounter.coinIcon != null
                ? goldCounter.coinIcon.position
                : goldCounter.transform.position;
            Vector2 to = floatingTextLayer.InverseTransformPoint(iconWorld);

            Image img = coinPool.Count > 0 ? coinPool.Dequeue() : SpawnCoin();
            if (img == null) return;
            img.sprite = coinSprite;
            img.gameObject.SetActive(true);
            var rt = img.rectTransform;
            rt.anchoredPosition = from;
            rt.localScale = Vector3.one;

            float arc = 110f + Random.Range(0f, 70f);
            Tween.Custom(0f, 1f, 0.55f, p =>
            {
                Vector2 pos = Vector2.LerpUnclamped(from, to, p);
                pos.y += Mathf.Sin(p * Mathf.PI) * arc;
                rt.anchoredPosition = pos;
                rt.localScale = Vector3.one * (1f - p * 0.5f);
            }, Ease.InQuad).OnComplete(() =>
            {
                img.gameObject.SetActive(false);
                coinPool.Enqueue(img);
                goldCounter.Punch();
            });
        }

        Image SpawnCoin()
        {
            var go = new GameObject("FlyCoin", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(floatingTextLayer, false);
            rt.sizeDelta = new Vector2(46f, 46f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }
    }
}
