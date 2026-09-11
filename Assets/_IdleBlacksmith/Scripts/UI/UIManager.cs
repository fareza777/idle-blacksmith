using System.Collections.Generic;
using IdleBlacksmith.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>HUD hub: gold counter, upgrade/dungeon panels, mute toggle,
    /// splash + onboarding flow, floating text pool.</summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        public GoldCounter goldCounter;
        public TMPro.TMP_Text oreLabel;
        public BouncyButton upgradesButton;
        public Button muteButton;
        public Image muteIcon;
        public Sprite soundOnSprite;
        public Sprite soundOffSprite;
        public UpgradePanel upgradePanel;
        public PulseLoop upgradesButtonPulse;

        [Header("Dungeon")]
        public BouncyButton dungeonButton;
        public DungeonPanel dungeonPanel;
        public GameObject dungeonBadge;
        public PulseLoop dungeonButtonPulse;

        [Header("Menu flow")]
        public SplashScreen splashScreen;
        public OnboardingPanel onboardingPanel;

        [Header("Floating text")]
        public RectTransform floatingTextLayer;
        public FloatingText floatingTextPrefab;
        [Min(2)] public int floatingTextPoolSize = 12;

        static readonly Color GoldColor = new Color(1f, 0.84f, 0.28f);

        Camera mainCamera;
        readonly Queue<FloatingText> pool = new Queue<FloatingText>();

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
            if (upgradePanel != null) upgradePanel.Init();
            if (dungeonPanel != null) dungeonPanel.Init();
            if (upgradesButton != null && upgradePanel != null)
                upgradesButton.onClick.AddListener(() =>
                {
                    upgradePanel.Toggle();
                    if (upgradesButtonPulse != null) upgradesButtonPulse.Stop();
                });
            if (dungeonButton != null && dungeonPanel != null)
                dungeonButton.onClick.AddListener(() =>
                {
                    dungeonPanel.Toggle();
                    SetDungeonBadge(false);
                });
            if (muteButton != null)
                muteButton.onClick.AddListener(ToggleMute);
            RefreshMuteIcon();
            SetDungeonBadge(gm != null && gm.expeditions != null && gm.expeditions.ReadyToClaim);
            PrewarmPool();

            // Menu flow: splash first, onboarding (once) right after.
            if (splashScreen != null)
                splashScreen.Play(() =>
                {
                    if (gm != null && !gm.HasSeenOnboarding && onboardingPanel != null)
                        onboardingPanel.Show();
                });
            else if (gm != null && !gm.HasSeenOnboarding && onboardingPanel != null)
                onboardingPanel.Show();
        }

        float badgeCheckTimer;

        void Update()
        {
            // The expedition end is wall-clock based, so poll to catch the moment it finishes.
            badgeCheckTimer += Time.deltaTime;
            if (badgeCheckTimer >= 0.5f)
            {
                badgeCheckTimer = 0f;
                GameManager gm = GameManager.Instance;
                if (gm != null)
                    SetDungeonBadge(gm.expeditions != null && gm.expeditions.ReadyToClaim);
            }
        }

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

        /// <summary>Red "!" bubble on the dungeon button when an expedition is ready to claim.</summary>
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
