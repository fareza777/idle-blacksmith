using System.Collections.Generic;
using IdleBlacksmith.Core;
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

            // Launch flow: splash, then the title screen, then onboarding once.
            if (splashScreen != null)
                splashScreen.Play(() => ShowMenu(gm));
            else
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
            if (questPanel != null) questPanel.Init();
            if (metaPanel != null) metaPanel.Init();
            if (prestigePanel != null) prestigePanel.Init();
            if (settingsPanel != null) settingsPanel.Init();
            if (welcomeBackPanel != null) welcomeBackPanel.Init();
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
                mainMenuPanel.Show(() => AfterMenu(gm));
            else
                AfterMenu(gm);
        }

        void AfterMenu(GameManager gm)
        {
            if (launched) return;
            launched = true;

            if (hudGroup != null) hudGroup.alpha = 1f;

            if (gm != null && !gm.HasSeenOnboarding && onboardingPanel != null)
            {
                onboardingPanel.Show();
                return;
            }
            ShowWelcomeBack(gm);
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
            mainMenuPanel.Show(() => { if (hudGroup != null) hudGroup.alpha = 1f; });
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
            metaPanel.ShowPage(stats);
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
                    || (mainMenuPanel != null && mainMenuPanel.IsOpen);
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
            // Android back closes the top sheet; only an empty screen quits the app.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (AnyPanelOpen) CloseAllPanels();
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
            if (oreLabel != null) oreLabel.text = ore.ToString();
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

            FloatingText ft = pool.Count > 0 ? pool.Dequeue() : Instantiate(floatingTextPrefab, floatingTextLayer);
            ft.Play(local, text, color, f => pool.Enqueue(f));
        }
    }
}
