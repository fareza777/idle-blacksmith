using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.EditorTools
{
    public class UiRefs
    {
        public UIManager uiManager;
        public WorldProgressBar anvilBar;
        public WorldProgressBar apprenticeBar;
        public RackStockBar rackBar;
    }

    /// <summary>
    /// Generates the whole casual-mobile UI in code (portrait 1080x1920): HUD with gold +
    /// relic ore pills, bottom bar (Upgrades / Dungeon), bottom-sheet panels, splash screen,
    /// onboarding, world-space bars, plus the UpgradeRow / DungeonRow / FloatingText prefabs.
    /// </summary>
    public static partial class UiBuilder
    {
        static readonly Color Brown = Hex(0x6B4127);
        static readonly Color Secondary = Hex(0xA98B6C);
        static readonly Color Cream = Hex(0xFBF3E4);
        static readonly Color PillDark = Hex(0x5B3F2C);
        static readonly Color Orange = Hex(0xF2994A);
        static readonly Color Green = Hex(0x5BA86B);
        static readonly Color Teal = Hex(0x4FA3A5);
        static readonly Color GoldText = Hex(0xFFD966);
        static readonly Color OreText = Hex(0x9FE3F0);
        static readonly Color RowWhite = Hex(0xFFFFFF);

        static Sprite rounded, roundedSmall, pill, circle, bar;
        static TMP_FontAsset bodyFont, titleFont;

        static Color Hex(int rgb) => new Color32(
            (byte)(rgb >> 16 & 0xFF), (byte)(rgb >> 8 & 0xFF), (byte)(rgb & 0xFF), 255);

        public static UiRefs Build(GameConfig config, Transform anvilStation, Transform apprenticeStation, Transform rackStation)
        {
            rounded = LoadSprite("rounded");
            roundedSmall = LoadSprite("rounded_small");
            pill = LoadSprite("pill");
            circle = LoadSprite("circle");
            bar = LoadSprite("bar");
            bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetFactory.FontBodyPath);
            titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetFactory.FontTitlePath);

            var refs = new UiRefs();
            GameObject floatingTextPrefab = BuildFloatingTextPrefab();
            GameObject upgradeRowPrefab = BuildUpgradeRowPrefab();
            GameObject dungeonRowPrefab = BuildDungeonRowPrefab();
            GameObject buildingRowPrefab = BuildBuildingRowPrefab();
            GameObject runeRowPrefab = BuildRuneRowPrefab();
            GameObject recipeCardPrefab = BuildRecipeCardPrefab();
            GameObject achRowPrefab = BuildAchRowPrefab();
            GameObject talentRowPrefab = BuildTalentRowPrefab();

            // ------------------------------------------------ canvas
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var safeArea = StretchBox("SafeArea", canvasGo.transform);
            safeArea.gameObject.AddComponent<SafeArea>();

            // ------------------------------------------------ HUD
            var hud = StretchBox("HUD", safeArea);

            // Gold pill (top-left)
            var goldPill = Box("GoldPill", hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -22), new Vector2(300, 96));
            var goldImg = goldPill.gameObject.AddComponent<Image>();
            goldImg.sprite = pill; goldImg.type = Image.Type.Sliced; goldImg.color = PillDark;
            SoftShadow(goldPill.gameObject);
            var coinIcon = Box("CoinIcon", goldPill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(68, 68));
            Img(coinIcon.gameObject, AssetFactory.LoadIcon("coin"), Color.white).raycastTarget = false;
            var goldLabelGo = Box("Label", goldPill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(94, 0), new Vector2(190, 64));
            var goldLabel = Txt(goldLabelGo, "0", 50, GoldText, TextAlignmentOptions.Left, titleFont);
            var goldCounter = goldPill.gameObject.AddComponent<GoldCounter>();
            goldCounter.label = goldLabel;
            goldCounter.coinIcon = coinIcon;

            // Metal ore pill (below gold) — the resource every forge spends.
            var metalPill = Box("MetalOrePill", hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -128), new Vector2(250, 72));
            var metalImg = metalPill.gameObject.AddComponent<Image>();
            metalImg.sprite = pill; metalImg.type = Image.Type.Sliced; metalImg.color = new Color(0.34f, 0.27f, 0.22f, 0.94f);
            SoftShadow(metalPill.gameObject);
            var metalIconGo = Box("Icon", metalPill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(50, 50));
            Img(metalIconGo.gameObject, AssetFactory.LoadIcon("ore"), Color.white).raycastTarget = false;
            var metalLabelGo = Box("Label", metalPill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(68, 0), new Vector2(100, 50));
            var metalOreLabel = Txt(metalLabelGo, "0", 38, Color.white, TextAlignmentOptions.Left, titleFont);
            var metalPerSecGo = Box("Rate", metalPill, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(86, 46));
            var metalRateLabel = Txt(metalPerSecGo, "+0/s", 24, new Color(0.78f, 0.86f, 0.90f), TextAlignmentOptions.Right, bodyFont);

            // Relic ore pill (dungeon currency, below metal ore)
            var orePill = Box("OrePill", hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -210), new Vector2(250, 68));
            var oreImg = orePill.gameObject.AddComponent<Image>();
            oreImg.sprite = pill; oreImg.type = Image.Type.Sliced; oreImg.color = new Color(0.18f, 0.32f, 0.36f, 0.94f);
            SoftShadow(orePill.gameObject);
            var oreIconGo = Box("OreIcon", orePill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(48, 48));
            Img(oreIconGo.gameObject, AssetFactory.LoadIcon("gem"), Color.white).raycastTarget = false;
            var oreLabelGo = Box("Label", orePill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(68, 0), new Vector2(150, 48));
            var oreLabel = Txt(oreLabelGo, "0", 36, OreText, TextAlignmentOptions.Left, titleFont);

            // Day chip (below relic ore) — counts dawns over the forge.
            var dayPill = Box("DayPill", hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -290), new Vector2(200, 56));
            var dayImg = dayPill.gameObject.AddComponent<Image>();
            dayImg.sprite = pill; dayImg.type = Image.Type.Sliced; dayImg.color = new Color(0.24f, 0.28f, 0.38f, 0.9f);
            SoftShadow(dayPill.gameObject);
            var dayLabelGo = Box("Label", dayPill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(160, 44));
            var dayLabel = Txt(dayLabelGo, "Day 1", 30, new Color(0.85f, 0.88f, 1f), TextAlignmentOptions.Left, bodyFont);
            var dayChip = dayPill.gameObject.AddComponent<DayChip>();
            dayChip.label = dayLabel;

            // ---------------- top-right rail: menu, upgrades, achievements, prestige, mute
            var railGo = Box("TopRail", hud, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -22), new Vector2(84, 460));
            var rail = railGo.gameObject.AddComponent<VerticalLayoutGroup>();
            rail.spacing = 14;
            rail.childAlignment = TextAnchor.UpperRight;
            rail.childControlWidth = false;
            rail.childControlHeight = false;
            rail.childForceExpandWidth = false;
            rail.childForceExpandHeight = false;

            BouncyButton menuBtn = RailButton(railGo, "MenuButton", "settings", Cream, out Image _);
            BouncyButton upBtn = RailButton(railGo, "UpgradesButton", "craft", Orange, out _);
            BouncyButton achBtn = RailButton(railGo, "AchievementsButton", "trophy", Hex(0xC99638), out _);
            BouncyButton prestBtn = RailButton(railGo, "PrestigeButton", "ember", Hex(0xD95F4E), out _);
            BouncyButton muteBtn = RailButton(railGo, "MuteButton", "sound_on", Cream, out Image muteIcon);

            // ---------------- bottom bar: Complex / Forge / Dungeon / Quest
            BouncyButton complexBtn = BarButton(hud, "ComplexButton", "complex", Hex(0xF2994A), 0, out _);
            // Pulses until the player opens it, so a new player is pointed at the build menu.
            var complexPulse = complexBtn.gameObject.AddComponent<PulseLoop>();
            BouncyButton forgeBtn = BarButton(hud, "ForgeButton", "craft", Hex(0xDF7F47), 1, out _);
            BouncyButton dgBtn = BarButton(hud, "DungeonButton", "dungeon", Teal, 2, out _);
            BouncyButton questBtn = BarButton(hud, "QuestButton", "scroll", Green, 3, out _);

            // claim-ready badge on the dungeon button
            var badgeGo = Box("Badge", dgBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(6, 6), new Vector2(52, 52));
            var badgeImg = badgeGo.gameObject.AddComponent<Image>();
            badgeImg.sprite = circle; badgeImg.type = Image.Type.Sliced; badgeImg.color = Hex(0xE25B4E);
            var badgeTxtGo = Box("Mark", badgeGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
            Txt(badgeTxtGo, "!", 34, Color.white, TextAlignmentOptions.Center, titleFont);
            badgeGo.gameObject.SetActive(false);

            // quest-complete badge on the quest button
            var qBadgeGo = Box("Badge", questBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(6, 6), new Vector2(52, 52));
            var qBadgeImg = qBadgeGo.gameObject.AddComponent<Image>();
            qBadgeImg.sprite = circle; qBadgeImg.type = Image.Type.Sliced; qBadgeImg.color = Hex(0xF2C14E);
            var qBadgeTxtGo = Box("Mark", qBadgeGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
            Txt(qBadgeTxtGo, "✓", 30, Brown, TextAlignmentOptions.Center, titleFont);
            qBadgeGo.gameObject.SetActive(false);

            var titleBadge = Box("TitleBadge", hud, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -128), new Vector2(430, 62));
            var titleImg = titleBadge.gameObject.AddComponent<Image>();
            titleImg.sprite = pill; titleImg.type = Image.Type.Sliced;
            titleImg.color = new Color(0.98f, 0.95f, 0.89f, 0.92f);
            SoftShadow(titleBadge.gameObject);
            var logoGo = Box("Logo", titleBadge, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(48, 48));
            Img(logoGo.gameObject, AssetFactory.LoadMenuArt("emblem"), Color.white).raycastTarget = false;
            var titleTextGo = Box("Text", titleBadge, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(352, 46));
            Txt(titleTextGo, "EMBERFORGE", 30, Brown, TextAlignmentOptions.Right, titleFont);

            // Floating text layer
            var ftLayer = StretchBox("FloatingTextLayer", safeArea);
            var ftCg = ftLayer.gameObject.AddComponent<CanvasGroup>();
            ftCg.blocksRaycasts = false;
            ftCg.interactable = false;

            // ------------------------------------------------ panels
            var panel = BuildUpgradePanel(canvasGo.transform, upgradeRowPrefab);
            var dungeonPanel = BuildDungeonPanel(canvasGo.transform, dungeonRowPrefab);
            var complexPanel = BuildComplexPanel(canvasGo.transform, buildingRowPrefab, runeRowPrefab);
            var forgePanel = BuildForgePanel(canvasGo.transform, recipeCardPrefab);
            var questPanel = BuildQuestPanel(canvasGo.transform);
            var metaPanel = BuildMetaPanel(canvasGo.transform, achRowPrefab);
            var prestigePanel = BuildPrestigePanel(canvasGo.transform, talentRowPrefab);
            var settingsPanel = BuildSettingsPanel(canvasGo.transform);
            var welcomeBack = BuildWelcomeBack(canvasGo.transform);
            var dailyClaim = BuildDailyClaim(canvasGo.transform);
            var dialogue = BuildDialogue(canvasGo.transform);
            var mainMenu = BuildMainMenu(canvasGo.transform);
            var onboarding = BuildOnboarding(canvasGo.transform);
            var intro = BuildIntro(canvasGo.transform);
            var splash = BuildSplash(canvasGo.transform);
            var ticker = BuildTicker(hud, questPanel);
            var orderTicker = BuildOrderTicker(hud);
            var rushBanner = BuildRushBanner(hud);

            // Full-screen flash overlay — last sibling so nothing draws above it.
            var flashGo = StretchBox("FlashOverlay", canvasGo.transform);
            var flashImg = flashGo.gameObject.AddComponent<Image>();
            flashImg.color = new Color(1f, 0.9f, 0.7f, 1f);
            flashImg.raycastTarget = false;
            var flashCg = flashGo.gameObject.AddComponent<CanvasGroup>();
            flashCg.alpha = 0f;
            flashCg.blocksRaycasts = false;
            flashCg.interactable = false;

            // ------------------------------------------------ world bars
            refs.anvilBar = BuildAnvilBar(anvilStation, new Vector3(0, 1.55f, 0), "AnvilBar");
            refs.apprenticeBar = BuildAnvilBar(apprenticeStation, new Vector3(0, 1.35f, 0), "ApprenticeBar");
            refs.rackBar = BuildRackBar(rackStation, new Vector3(0, 2.05f, -0.35f));

            // ------------------------------------------------ UIManager wiring
            var ui = canvasGo.AddComponent<UIManager>();
            ui.goldCounter = goldCounter;
            ui.oreLabel = oreLabel;
            ui.metalOreLabel = metalOreLabel;
            ui.metalOreRateLabel = metalRateLabel;
            ui.muteButton = muteBtn;
            ui.muteIcon = muteIcon;
            ui.soundOnSprite = AssetFactory.LoadIcon("sound_on");
            ui.soundOffSprite = AssetFactory.LoadIcon("sound_off");
            ui.upgradesButton = upBtn;
            ui.upgradePanel = panel;
            ui.dungeonButton = dgBtn;
            ui.dungeonPanel = dungeonPanel;
            ui.dungeonBadge = badgeGo.gameObject;
            ui.complexButton = complexBtn;
            ui.complexButtonPulse = complexPulse;
            ui.complexPanel = complexPanel;
            ui.forgeButton = forgeBtn;
            ui.forgePanel = forgePanel;
            ui.questButton = questBtn;
            ui.questPanel = questPanel;
            ui.questBadge = qBadgeGo.gameObject;
            ui.menuButton = menuBtn;
            ui.metaPanel = metaPanel;
            ui.prestigePanel = prestigePanel;
            ui.settingsPanel = settingsPanel;
            ui.welcomeBackPanel = welcomeBack;
            ui.dailyPanel = dailyClaim;
            ui.mainMenuPanel = mainMenu;
            ui.ticker = ticker;
            ui.orderTicker = orderTicker;
            ui.rushBanner = rushBanner;
            ui.hudGroup = hud.gameObject.AddComponent<CanvasGroup>();
            ui.flashOverlay = flashCg;
            ui.flashTint = flashImg;
            ui.splashScreen = splash;
            ui.onboardingPanel = onboarding;
            ui.introCinematic = intro;
            ui.dialoguePanel = dialogue;
            ui.floatingTextLayer = ftLayer;
            ui.floatingTextPrefab = floatingTextPrefab.GetComponent<FloatingText>();

            // Secondary entry points: achievements and prestige are reached from the top rail,
            // so they go through the same exclusive-open path as the bottom-bar sheets.
            achBtn.onClick.AddListener(() => ui.OpenMeta(false));
            prestBtn.onClick.AddListener(() => ui.OpenPrestige());
            if (questPanel.achievementsButton != null)
            {
                var qa = questPanel.achievementsButton.GetComponent<BouncyButton>();
                if (qa != null) qa.onClick.AddListener(() => ui.OpenMeta(false));
            }

            refs.uiManager = ui;
            return refs;
        }

        // ------------------------------------------------------------ hud buttons

        /// <summary>One round button in the top-right rail (menu / upgrades / achievements / prestige / mute).</summary>
        static BouncyButton RailButton(RectTransform rail, string name, string icon, Color tint, out Image iconImage)
        {
            var go = Box(name, rail, new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(80, 80));
            var btn = go.gameObject.AddComponent<BouncyButton>();
            var img = go.gameObject.AddComponent<Image>();
            img.sprite = circle; img.type = Image.Type.Sliced; img.color = tint;
            btn.targetGraphic = img;
            SetButtonColors(btn);
            SoftShadow(go.gameObject, -3f, 0.3f);
            var le = go.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 80; le.preferredHeight = 80;

            var iconGo = Box("Icon", go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50, 50));
            iconImage = Img(iconGo.gameObject, AssetFactory.LoadIcon(icon), Color.white);
            iconImage.raycastTarget = false;
            return btn;
        }

        /// <summary>One of the four wide buttons along the bottom bar.</summary>
        static BouncyButton BarButton(RectTransform hud, string name, string icon, Color tint, int index, out TMP_Text label)
        {
            const float margin = 18f;
            const float gap = 12f;
            float width = (1080f - margin * 2f - gap * 3f) / 4f;
            float x = margin + index * (width + gap);

            var go = Box(name, hud, new Vector2(0, 0), new Vector2(0, 0), new Vector2(x, 26), new Vector2(width, 118));
            var btn = go.gameObject.AddComponent<BouncyButton>();
            var img = go.gameObject.AddComponent<Image>();
            img.sprite = pill; img.type = Image.Type.Sliced; img.color = tint;
            btn.targetGraphic = img;
            SetButtonColors(btn);
            SoftShadow(go.gameObject, -5f, 0.35f);

            var iconGo = Box("Icon", go, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(52, 52));
            Img(iconGo.gameObject, AssetFactory.LoadIcon(icon), Color.white).raycastTarget = false;

            var labelGo = Box("Label", go, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(width - 8f, 40));
            label = Txt(labelGo, name.Replace("Button", "").ToUpperInvariant(), 24, Color.white, TextAlignmentOptions.Center, titleFont);
            return btn;
        }

        // ------------------------------------------------------------ upgrade panel

        static UpgradePanel BuildUpgradePanel(Transform parent, GameObject rowPrefab)
        {
            var root = StretchBox("UpgradePanel", parent);
            var panel = root.gameObject.AddComponent<UpgradePanel>();

            var backdrop = StretchBox("Backdrop", root);
            var backdropImg = backdrop.gameObject.AddComponent<Image>();
            backdropImg.color = new Color(0.16f, 0.11f, 0.07f, 0.55f);
            var backdropCg = backdrop.gameObject.AddComponent<CanvasGroup>();
            backdropCg.alpha = 0f;
            backdropCg.blocksRaycasts = false;
            var backdropBtn = backdrop.gameObject.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.targetGraphic = backdropImg;

            var sheet = Box("Sheet", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -1400), new Vector2(1000, 1300));
            var sheetImg = sheet.gameObject.AddComponent<Image>();
            sheetImg.sprite = rounded; sheetImg.type = Image.Type.Sliced; sheetImg.color = Cream;
            SoftShadow(sheet.gameObject, -6f, 0.4f);

            var titleGo = Box("Title", sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -22), new Vector2(500, 60));
            Txt(titleGo, "Upgrades", 52, Brown, TextAlignmentOptions.Left, titleFont);
            var subGo = Box("Subtitle", sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -76), new Vector2(500, 38));
            Txt(subGo, "Make your forge legendary", 26, Secondary, TextAlignmentOptions.Left, bodyFont);

            var closeGo = Box("CloseButton", sheet, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(68, 68));
            var closeBtn = closeGo.gameObject.AddComponent<BouncyButton>();
            var closeImg = closeGo.gameObject.AddComponent<Image>();
            closeImg.sprite = circle; closeImg.type = Image.Type.Sliced; closeImg.color = Hex(0xEBDCC3);
            closeBtn.targetGraphic = closeImg;
            var closeTxtGo = Box("X", closeGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52));
            Txt(closeTxtGo, "×", 40, Brown, TextAlignmentOptions.Center, titleFont);

            // rows area
            var rowsArea = Box("RowsArea", sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -132), new Vector2(940, 1140));
            var vlg = rowsArea.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var helperCard = BuildHelperCard(rowsArea);
            var expandCard = BuildExpandCard(rowsArea);

            panel.sheet = sheet;
            panel.backdrop = backdropCg;
            panel.rowPrefab = rowPrefab.GetComponent<UpgradeRow>();
            panel.rowsParent = rowsArea;
            panel.helperCard = helperCard;
            panel.expandCard = expandCard;
            panel.backdropButton = backdropBtn;
            panel.closeButton = closeBtn;
            panel.openY = 26f;
            panel.closedY = -1400f;
            root.gameObject.SetActive(false);
            return panel;
        }

        static HelperCard BuildHelperCard(Transform parent)
        {
            var row = Box("HelperCard", parent, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(940, 150));
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 150;
            var img = row.gameObject.AddComponent<Image>();
            img.sprite = roundedSmall; img.type = Image.Type.Sliced; img.color = Hex(0xEAF6E2);
            SoftShadow(row.gameObject, -3f, 0.25f);
            var content = row.gameObject.AddComponent<CanvasGroup>();

            BuildIconTile(row, AssetFactory.LoadIcon("helper"));

            var nameGo = Box("Name", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -22), new Vector2(430, 46));
            Txt(nameGo, "Helper Blacksmith", 36, Brown, TextAlignmentOptions.Left, titleFont);
            var stateGo = Box("State", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -68), new Vector2(460, 38));
            var stateLabel = Txt(stateGo, "Hire a second blacksmith", 26, Secondary, TextAlignmentOptions.Left, bodyFont);
            var perkGo = Box("Perk", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -106), new Vector2(480, 34));
            Txt(perkGo, "Forges swords alongside you", 24, Hex(0x7FA877), TextAlignmentOptions.Left, bodyFont);

            var hireGo = Box("HireButton", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(220, 94));
            var hireBtn = hireGo.gameObject.AddComponent<BouncyButton>();
            var hireImg = hireGo.gameObject.AddComponent<Image>();
            hireImg.sprite = pill; hireImg.type = Image.Type.Sliced; hireImg.color = Green;
            hireBtn.targetGraphic = hireImg;
            SetButtonColors(hireBtn);
            var costPill = Box("CostPill", hireGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210, 86));
            var coinGo = Box("Coin", costPill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(46, 46));
            Img(coinGo.gameObject, AssetFactory.LoadIcon("coin"), Color.white).raycastTarget = false;
            var costGo = Box("Cost", costPill, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(130, 60));
            var costLabel = Txt(costGo, "300", 38, Color.white, TextAlignmentOptions.Right, titleFont);

            var hiredGo = Box("HiredBadge", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(220, 94));
            var hiredImg = hiredGo.gameObject.AddComponent<Image>();
            hiredImg.sprite = pill; hiredImg.type = Image.Type.Sliced; hiredImg.color = Hex(0x8FA98B);
            var hiredTxtGo = Box("Label", hiredGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210, 64));
            Txt(hiredTxtGo, "WORKING!", 32, Color.white, TextAlignmentOptions.Center, titleFont);
            hiredGo.gameObject.SetActive(false);

            var card = row.gameObject.AddComponent<HelperCard>();
            card.stateLabel = stateLabel;
            card.costLabel = costLabel;
            card.costPill = costPill.gameObject;
            card.hireButton = hireBtn;
            card.hiredBadge = hiredGo.gameObject;
            card.content = content;
            return card;
        }

        static ExpandCard BuildExpandCard(Transform parent)
        {
            var row = Box("ExpandCard", parent, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(940, 150));
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 150;
            var img = row.gameObject.AddComponent<Image>();
            img.sprite = roundedSmall; img.type = Image.Type.Sliced; img.color = Hex(0xFDF1DC);
            SoftShadow(row.gameObject, -3f, 0.25f);
            var content = row.gameObject.AddComponent<CanvasGroup>();

            BuildIconTile(row, AssetFactory.LoadMenuArt("emblem"));

            var nameGo = Box("Name", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -22), new Vector2(430, 46));
            Txt(nameGo, "Expand Shop", 36, Brown, TextAlignmentOptions.Left, titleFont);
            var stateGo = Box("State", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -68), new Vector2(500, 38));
            var stateLabel = Txt(stateGo, "Roadside Stall → Village Smithy", 26, Secondary, TextAlignmentOptions.Left, bodyFont);
            var perkGo = Box("Perk", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -106), new Vector2(500, 34));
            var perkLabel = Txt(perkGo, "+2 rack slots, +15% prices", 24, Hex(0xC99638), TextAlignmentOptions.Left, bodyFont);

            var buyGo = Box("ExpandButton", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(220, 94));
            var buyBtn = buyGo.gameObject.AddComponent<BouncyButton>();
            var buyImg = buyGo.gameObject.AddComponent<Image>();
            buyImg.sprite = pill; buyImg.type = Image.Type.Sliced; buyImg.color = Orange;
            buyBtn.targetGraphic = buyImg;
            SetButtonColors(buyBtn);
            var costPill = Box("CostPill", buyGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210, 86));
            var coinGo = Box("Coin", costPill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(46, 46));
            Img(coinGo.gameObject, AssetFactory.LoadIcon("coin"), Color.white).raycastTarget = false;
            var costGo = Box("Cost", costPill, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(130, 60));
            var costLabel = Txt(costGo, "250", 38, Color.white, TextAlignmentOptions.Right, titleFont);

            var maxGo = Box("MaxBadge", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(220, 94));
            var maxImg = maxGo.gameObject.AddComponent<Image>();
            maxImg.sprite = pill; maxImg.type = Image.Type.Sliced; maxImg.color = Hex(0xC99638);
            var maxTxtGo = Box("Label", maxGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210, 64));
            Txt(maxTxtGo, "GRAND!", 32, Color.white, TextAlignmentOptions.Center, titleFont);
            maxGo.gameObject.SetActive(false);

            var card = row.gameObject.AddComponent<ExpandCard>();
            card.stateLabel = stateLabel;
            card.perkLabel = perkLabel;
            card.costLabel = costLabel;
            card.costPill = costPill.gameObject;
            card.expandButton = buyBtn;
            card.maxBadge = maxGo.gameObject;
            card.content = content;
            return card;
        }

        // ------------------------------------------------------------ dungeon panel

        static DungeonPanel BuildDungeonPanel(Transform parent, GameObject rowPrefab)
        {
            var root = StretchBox("DungeonPanel", parent);
            var panel = root.gameObject.AddComponent<DungeonPanel>();

            var backdrop = StretchBox("Backdrop", root);
            var backdropImg = backdrop.gameObject.AddComponent<Image>();
            backdropImg.color = new Color(0.08f, 0.10f, 0.14f, 0.6f);
            var backdropCg = backdrop.gameObject.AddComponent<CanvasGroup>();
            backdropCg.alpha = 0f;
            backdropCg.blocksRaycasts = false;
            var backdropBtn = backdrop.gameObject.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.targetGraphic = backdropImg;

            var sheet = Box("Sheet", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -1560), new Vector2(1000, 1460));
            var sheetImg = sheet.gameObject.AddComponent<Image>();
            sheetImg.sprite = rounded; sheetImg.type = Image.Type.Sliced; sheetImg.color = Cream;
            SoftShadow(sheet.gameObject, -6f, 0.4f);

            // header art (masked into the sheet's rounded top)
            var artMaskGo = Box("ArtMask", sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(976, 360));
            var artMaskImg = artMaskGo.gameObject.AddComponent<Image>();
            artMaskImg.sprite = roundedSmall; artMaskImg.type = Image.Type.Sliced; artMaskImg.color = Color.white;
            var artMask = artMaskGo.gameObject.AddComponent<Mask>();
            artMask.showMaskGraphic = true;
            var artGo = StretchBox("Art", artMaskGo);
            var art = artGo.gameObject.AddComponent<Image>();
            // The wide banner, not the 9:16 corridor: a portrait sprite in this 2.7:1 band would
            // only ever show a thin horizontal slice of itself, which reads as a squashed image.
            art.sprite = AssetFactory.LoadMenuArt("dungeon_banner") ?? AssetFactory.LoadMenuArt("dungeon_bg");
            art.raycastTarget = false;
            CoverFitter(artGo, art, 16f / 9f);
            // dim the bottom of the art so the title reads
            var dimGo = Box("Dim", artMaskGo, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(976, 120));
            var dimImg = dimGo.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.10f, 0.12f, 0.16f, 0.45f);
            dimImg.raycastTarget = false;

            var titleGo = Box("Title", artMaskGo, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 18), new Vector2(600, 56));
            Txt(titleGo, "Dungeon Expeditions", 46, Color.white, TextAlignmentOptions.Left, titleFont);

            var closeGo = Box("CloseButton", sheet, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(68, 68));
            var closeBtn = closeGo.gameObject.AddComponent<BouncyButton>();
            var closeImg = closeGo.gameObject.AddComponent<Image>();
            closeImg.sprite = circle; closeImg.type = Image.Type.Sliced; closeImg.color = new Color(1f, 1f, 1f, 0.9f);
            closeBtn.targetGraphic = closeImg;
            var closeTxtGo = Box("X", closeGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52));
            Txt(closeTxtGo, "×", 40, Brown, TextAlignmentOptions.Center, titleFont);

            // relic ore status row
            var oreRow = Box("OreRow", sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -396), new Vector2(940, 84));
            var oreRowImg = oreRow.gameObject.AddComponent<Image>();
            oreRowImg.sprite = roundedSmall; oreRowImg.type = Image.Type.Sliced; oreRowImg.color = new Color(0.18f, 0.32f, 0.36f, 1f);
            var oreIconGo = Box("Icon", oreRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(56, 56));
            Img(oreIconGo.gameObject, AssetFactory.LoadIcon("ore"), Color.white).raycastTarget = false;
            var oreCountGo = Box("Count", oreRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(92, 0), new Vector2(120, 60));
            var relicOreLabel = Txt(oreCountGo, "0", 42, OreText, TextAlignmentOptions.Left, titleFont);
            var oreNameGo = Box("Name", oreRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(200, 0), new Vector2(300, 44));
            Txt(oreNameGo, "Relic Ore", 30, Color.white, TextAlignmentOptions.Left, titleFont);
            var bonusGo = Box("Bonus", oreRow, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-26, 0), new Vector2(380, 48));
            var priceBonusLabel = Txt(bonusGo, "Boosts sword prices", 28, OreText, TextAlignmentOptions.Right, bodyFont);

            var subGo = Box("Subtitle", sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -502), new Vector2(700, 38));
            Txt(subGo, "Adventurers raid while you forge — even while you're away", 26, Secondary, TextAlignmentOptions.Left, bodyFont);

            var rowsArea = Box("RowsArea", sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -552), new Vector2(940, 900));
            var vlg = rowsArea.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            panel.sheet = sheet;
            panel.backdrop = backdropCg;
            panel.backdropButton = backdropBtn;
            panel.closeButton = closeBtn;
            panel.relicOreLabel = relicOreLabel;
            panel.priceBonusLabel = priceBonusLabel;
            panel.rowPrefab = rowPrefab.GetComponent<DungeonRow>();
            panel.rowsParent = rowsArea;
            panel.openY = 26f;
            panel.closedY = -1560f;
            root.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------ splash + onboarding

        static SplashScreen BuildSplash(Transform parent)
        {
            var root = StretchBox("SplashScreen", parent);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            var bgGo = StretchBox("Backdrop", root);
            var bgImg = bgGo.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.13f, 0.09f, 0.06f);
            bgImg.raycastTarget = true;

            var artGo = StretchBox("Art", root);
            var artImg = artGo.gameObject.AddComponent<Image>();
            artImg.sprite = AssetFactory.LoadMenuArt("splash");
            artImg.raycastTarget = false;
            CoverFitter(artGo, artImg, 9f / 16f);

            // soft dark gradient strip at the bottom for the title block
            var shadeGo = Box("Shade", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(1080, 620));
            var shadeImg = shadeGo.gameObject.AddComponent<Image>();
            shadeImg.color = new Color(0.10f, 0.07f, 0.04f, 0.52f);
            shadeImg.raycastTarget = false;

            var block = Box("TitleBlock", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 290), new Vector2(960, 560));
            var emblemGo = Box("Emblem", block, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(260, 260));
            var emblemImg = emblemGo.gameObject.AddComponent<Image>();
            emblemImg.sprite = AssetFactory.LoadMenuArt("emblem");
            emblemImg.preserveAspect = true;
            emblemImg.raycastTarget = false;
            var titleGo = Box("Title", block, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(960, 90));
            Txt(titleGo, "EMBERFORGE", 88, GoldText, TextAlignmentOptions.Center, titleFont);
            var subGo = Box("Sub", block, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -412), new Vector2(960, 56));
            Txt(subGo, "I D L E   B L A C K S M I T H", 40, Cream, TextAlignmentOptions.Center, titleFont);
            var tagGo = Box("Tagline", block, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -478), new Vector2(960, 44));
            Txt(tagGo, "a cozy forge adventure", 30, new Color(1f, 0.93f, 0.80f, 0.9f), TextAlignmentOptions.Center, bodyFont);

            var hintGo = Box("TapHint", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(600, 60));
            var hintCg = hintGo.gameObject.AddComponent<CanvasGroup>();
            hintCg.blocksRaycasts = false;
            Txt(hintGo, "TAP TO START", 34, new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.Center, titleFont);

            var splash = root.gameObject.AddComponent<SplashScreen>();
            splash.group = cg;
            splash.emblem = emblemGo;
            splash.titleBlock = block;
            splash.tapHint = hintCg;
            root.gameObject.SetActive(false);
            return splash;
        }

        static OnboardingPanel BuildOnboarding(Transform parent)
        {
            var root = StretchBox("Onboarding", parent);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            var dimGo = StretchBox("Dim", root);
            var dimImg = dimGo.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.10f, 0.07f, 0.04f, 0.78f);
            dimImg.raycastTarget = true;

            var card = Box("Card", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 1300));
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.sprite = rounded; cardImg.type = Image.Type.Sliced; cardImg.color = Cream;
            SoftShadow(card.gameObject, -8f, 0.45f);

            // art with rounded mask
            var artMaskGo = Box("ArtMask", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(908, 620));
            var artMaskImg = artMaskGo.gameObject.AddComponent<Image>();
            artMaskImg.sprite = roundedSmall; artMaskImg.type = Image.Type.Sliced; artMaskImg.color = Color.white;
            var mask = artMaskGo.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var artGo = StretchBox("Art", artMaskGo);
            var artImg = artGo.gameObject.AddComponent<Image>();
            artImg.raycastTarget = false;
            var artFitter = CoverFitter(artGo, artImg, 3f / 2f);

            var titleGo = Box("Title", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -672), new Vector2(860, 64));
            var titleLabel = Txt(titleGo, "Welcome!", 50, Brown, TextAlignmentOptions.Center, titleFont);
            var bodyGo = Box("Body", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -748), new Vector2(800, 200));
            var bodyLabel = Txt(bodyGo, "...", 30, Secondary, TextAlignmentOptions.Center, bodyFont, true);

            // dots
            var dotsGo = Box("Dots", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 258), new Vector2(300, 40));
            var hlg = dotsGo.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 18;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            var dots = new Image[5];
            for (int i = 0; i < dots.Length; i++)
            {
                var dotGo = Box("Dot" + i, dotsGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 26));
                var dotImg = dotGo.gameObject.AddComponent<Image>();
                dotImg.sprite = circle; dotImg.type = Image.Type.Sliced; dotImg.color = Secondary;
                dotImg.raycastTarget = false;
                var dle = dotGo.gameObject.AddComponent<LayoutElement>();
                dle.preferredWidth = 26; dle.preferredHeight = 26;
                dots[i] = dotImg;
            }

            // next button
            var nextGo = Box("NextButton", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(440, 104));
            var nextBtn = nextGo.gameObject.AddComponent<BouncyButton>();
            var nextImg = nextGo.gameObject.AddComponent<Image>();
            nextImg.sprite = pill; nextImg.type = Image.Type.Sliced; nextImg.color = Orange;
            nextBtn.targetGraphic = nextImg;
            SetButtonColors(nextBtn);
            SoftShadow(nextGo.gameObject, -5f, 0.35f);
            var nextTxtGo = Box("Label", nextGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 72));
            var nextLabel = Txt(nextTxtGo, "NEXT", 44, Color.white, TextAlignmentOptions.Center, titleFont);

            // skip
            var skipGo = Box("SkipButton", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -28), new Vector2(140, 60));
            var skipBtn = skipGo.gameObject.AddComponent<Button>();
            var skipImg = skipGo.gameObject.AddComponent<Image>();
            skipImg.color = new Color(1f, 1f, 1f, 0.001f); // easy target, invisible
            skipBtn.targetGraphic = skipImg;
            skipBtn.transition = Selectable.Transition.None;
            var skipTxtGo = Box("Label", skipGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 44));
            Txt(skipTxtGo, "SKIP", 30, Secondary, TextAlignmentOptions.Center, titleFont);

            var ob = root.gameObject.AddComponent<OnboardingPanel>();
            ob.group = cg;
            ob.card = card;
            ob.artImage = artImg;
            ob.artFitter = artFitter;
            ob.titleLabel = titleLabel;
            ob.bodyLabel = bodyLabel;
            ob.dots = dots;
            ob.nextButton = nextBtn;
            ob.nextLabel = nextLabel;
            ob.skipButton = skipBtn;
            ob.pages = new[]
            {
                new OnboardingPanel.Page
                {
                    art = LoadPageArt("page_forge", "onboard_forge"),
                    title = "Forge Legendary Swords",
                    body = "Miners dig ore, your smith hammers it on the anvil, and the rack fills up with swords. "
                         + "Every blade rolls its own quality — from Common all the way to Legendary.",
                },
                new OnboardingPanel.Page
                {
                    art = LoadPageArt("page_mine", "onboard_forge"),
                    title = "Feed the Forge",
                    body = "Ore is the fuel of the whole workshop. Raise the Ore Mine so the pile keeps growing, "
                         + "even while the app is closed.",
                },
                new OnboardingPanel.Page
                {
                    art = LoadPageArt("page_market", "onboard_shop"),
                    title = "Sell at the Counter",
                    body = "Customers walk the path and buy the best sword on your rack — better quality, better pay. "
                         + "Gold buys upgrades, and upgrades buy better swords.",
                },
                new OnboardingPanel.Page
                {
                    art = LoadPageArt("page_forge", "onboard_forge"),
                    title = "Look Around",
                    body = "Drag the yard to pan, pinch to zoom, and double-tap to snap back to the forge. "
                         + "Tap the marked plots — or the COMPLEX button — to raise a new building.",
                },
                new OnboardingPanel.Page
                {
                    art = LoadPageArt("page_dungeon", "onboard_dungeon"),
                    title = "Raid the Dungeons",
                    body = "Send adventurers through the Dungeon Gate. They return with gold and relic ore, "
                         + "and the run finishes even while you are away.",
                },
                new OnboardingPanel.Page
                {
                    art = LoadPageArt("page_prestige", "onboard_dungeon"),
                    title = "Rekindle the Forge",
                    body = "When the complex is running at full tilt, burn the run for ember shards. "
                         + "Talents are permanent, so every rekindle makes the next forge stronger.",
                },
            };
            root.gameObject.SetActive(false);
            return ob;
        }

        // ------------------------------------------------------------ prefabs

        static GameObject BuildUpgradeRowPrefab()
        {
            var root = new GameObject("UpgradeRow", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(940, 140);
            var le = root.AddComponent<LayoutElement>();
            le.preferredHeight = 140;
            var img = root.AddComponent<Image>();
            img.sprite = roundedSmall; img.type = Image.Type.Sliced; img.color = RowWhite;
            SoftShadow(root, -3f, 0.22f);
            var content = root.AddComponent<CanvasGroup>();

            var tile = Box("IconTile", rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(104, 104));
            var tileImg = tile.gameObject.AddComponent<Image>();
            tileImg.sprite = roundedSmall; tileImg.type = Image.Type.Sliced; tileImg.color = Color.white;
            var mask = tile.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var iconGo = StretchBox("Icon", tile);
            var icon = iconGo.gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            var nameGo = Box("Name", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -16), new Vector2(380, 44));
            var nameLabel = Txt(nameGo, "Upgrade", 34, Brown, TextAlignmentOptions.Left, titleFont);

            var levelGo = Box("Level", rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-250, -18), new Vector2(150, 36));
            var levelLabel = Txt(levelGo, "Lv 0/10", 26, Secondary, TextAlignmentOptions.Right, bodyFont);

            var descGo = Box("Desc", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -58), new Vector2(480, 34));
            var descLabel = Txt(descGo, "Description", 24, Secondary, TextAlignmentOptions.Left, bodyFont);

            var pipsGo = Box("Pips", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -100), new Vector2(420, 24));
            var hlg = pipsGo.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            var pips = new Image[10];
            for (int i = 0; i < 10; i++)
            {
                var pipGo = Box("Pip" + i, pipsGo, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(22, 22));
                var pipImg = pipGo.gameObject.AddComponent<Image>();
                pipImg.sprite = circle; pipImg.type = Image.Type.Sliced; pipImg.color = Secondary;
                pipImg.raycastTarget = false;
                var ple = pipGo.gameObject.AddComponent<LayoutElement>();
                ple.preferredWidth = 22; ple.preferredHeight = 22;
                pips[i] = pipImg;
            }

            var buyGo = Box("BuyButton", rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(210, 90));
            var buyBtn = buyGo.gameObject.AddComponent<BouncyButton>();
            var buyImg = buyGo.gameObject.AddComponent<Image>();
            buyImg.sprite = pill; buyImg.type = Image.Type.Sliced; buyImg.color = Orange;
            buyBtn.targetGraphic = buyImg;
            SetButtonColors(buyBtn);
            var costPill = Box("CostPill", buyGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 82));
            var coinGo = Box("Coin", costPill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(44, 44));
            Img(coinGo.gameObject, AssetFactory.LoadIcon("coin"), Color.white).raycastTarget = false;
            var costGo = Box("Cost", costPill, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(126, 56));
            var costLabel = Txt(costGo, "50", 36, Color.white, TextAlignmentOptions.Right, titleFont);

            var maxGo = Box("MaxBadge", rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(210, 90));
            var maxImg = maxGo.gameObject.AddComponent<Image>();
            maxImg.sprite = pill; maxImg.type = Image.Type.Sliced; maxImg.color = Hex(0x9AA3A0);
            var maxTxtGo = Box("Label", maxGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 60));
            Txt(maxTxtGo, "MAX", 36, Color.white, TextAlignmentOptions.Center, titleFont);
            maxGo.gameObject.SetActive(false);

            var row = root.AddComponent<UpgradeRow>();
            row.icon = icon;
            row.nameLabel = nameLabel;
            row.descLabel = descLabel;
            row.levelLabel = levelLabel;
            row.pips = pips;
            row.costLabel = costLabel;
            row.costPill = costPill.gameObject;
            row.buyButton = buyBtn;
            row.maxBadge = maxGo.gameObject;
            row.content = content;

            string path = Paths.Prefabs + "/UI_UpgradeRow.prefab";
            AssetReplace.SavePrefab(root, path);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static GameObject BuildDungeonRowPrefab()
        {
            var root = new GameObject("DungeonRow", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(940, 170);
            var le = root.AddComponent<LayoutElement>();
            le.preferredHeight = 170;
            var img = root.AddComponent<Image>();
            img.sprite = roundedSmall; img.type = Image.Type.Sliced; img.color = RowWhite;
            SoftShadow(root, -3f, 0.22f);
            var content = root.AddComponent<CanvasGroup>();

            var tile = Box("IconTile", rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(120, 120));
            var tileImg = tile.gameObject.AddComponent<Image>();
            tileImg.sprite = roundedSmall; tileImg.type = Image.Type.Sliced;
            tileImg.color = new Color(0.18f, 0.32f, 0.36f);
            var mask = tile.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var iconGo = StretchBox("Icon", tile);
            var tileIcon = iconGo.gameObject.AddComponent<Image>();
            tileIcon.sprite = AssetFactory.LoadIcon("dungeon");
            tileIcon.raycastTarget = false;
            tileIcon.preserveAspect = true;

            var nameGo = Box("Name", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(156, -18), new Vector2(360, 46));
            var nameLabel = Txt(nameGo, "Dungeon", 36, Brown, TextAlignmentOptions.Left, titleFont);
            var durGo = Box("Duration", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(420, -26), new Vector2(160, 36));
            var durLabel = Txt(durGo, "2 min", 26, Teal, TextAlignmentOptions.Left, titleFont);
            var descGo = Box("Desc", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(156, -66), new Vector2(470, 34));
            var descLabel = Txt(descGo, "Description", 24, Secondary, TextAlignmentOptions.Left, bodyFont);
            var rewardGo = Box("Reward", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(156, -110), new Vector2(470, 40));
            var rewardLabel = Txt(rewardGo, "+60 gold  +2 relic ore", 26, Hex(0xC99638), TextAlignmentOptions.Left, titleFont);

            // GO state
            var goState = Box("GoState", rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(210, 94));
            var goBtn = goState.gameObject.AddComponent<BouncyButton>();
            var goImg = goState.gameObject.AddComponent<Image>();
            goImg.sprite = pill; goImg.type = Image.Type.Sliced; goImg.color = Teal;
            goBtn.targetGraphic = goImg;
            SetButtonColors(goBtn);
            var goTxtGo = Box("Label", goState, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 66));
            Txt(goTxtGo, "GO!", 40, Color.white, TextAlignmentOptions.Center, titleFont);

            // RUN state: progress bar + timer
            var runState = Box("RunState", rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(220, 110));
            var timerGo = Box("Timer", runState, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(220, 44));
            var timerLabel = Txt(timerGo, "1:59", 36, Brown, TextAlignmentOptions.Center, titleFont);
            var barBgGo = Box("BarBg", runState, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(200, 26));
            var barBgImg = barBgGo.gameObject.AddComponent<Image>();
            barBgImg.sprite = bar; barBgImg.type = Image.Type.Sliced; barBgImg.color = new Color(0f, 0f, 0f, 0.25f);
            barBgImg.raycastTarget = false;
            var fillGo = Box("Fill", barBgGo, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, Vector2.zero);
            var fillRt = (RectTransform)fillGo;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(3, 3);
            fillRt.offsetMax = new Vector2(-3, -3);
            var fill = fillGo.gameObject.AddComponent<Image>();
            fill.sprite = bar; fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.color = Teal;
            fill.raycastTarget = false;
            runState.gameObject.SetActive(false);

            // CLAIM state
            var claimState = Box("ClaimState", rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(210, 94));
            var claimBtn = claimState.gameObject.AddComponent<BouncyButton>();
            var claimImg = claimState.gameObject.AddComponent<Image>();
            claimImg.sprite = pill; claimImg.type = Image.Type.Sliced; claimImg.color = Green;
            claimBtn.targetGraphic = claimImg;
            SetButtonColors(claimBtn);
            var claimTxtGo = Box("Label", claimState, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 66));
            Txt(claimTxtGo, "CLAIM!", 36, Color.white, TextAlignmentOptions.Center, titleFont);
            claimState.gameObject.SetActive(false);

            var row = root.AddComponent<DungeonRow>();
            row.nameLabel = nameLabel;
            row.descLabel = descLabel;
            row.rewardLabel = rewardLabel;
            row.durationLabel = durLabel;
            row.goButton = goBtn;
            row.goState = goState.gameObject;
            row.runState = runState.gameObject;
            row.progressFill = fill;
            row.timerLabel = timerLabel;
            row.claimState = claimState.gameObject;
            row.claimButton = claimBtn;
            row.content = content;

            string path = Paths.Prefabs + "/UI_DungeonRow.prefab";
            AssetReplace.SavePrefab(root, path);
            // Reload through the AssetDatabase: GetComponent on the in-memory result of
            // SaveAsPrefabAsset can return null for a script compiled in this same session.
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static GameObject BuildFloatingTextPrefab()
        {
            var root = new GameObject("FloatingText", typeof(RectTransform), typeof(CanvasGroup));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(240, 90);
            var label = root.AddComponent<TextMeshProUGUI>();
            label.text = "+10";
            label.fontSize = 48;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.84f, 0.28f);
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            if (titleFont != null) label.font = titleFont;
            var outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(0.36f, 0.25f, 0.15f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var ft = root.AddComponent<FloatingText>();
            ft.label = label;

            string path = Paths.Prefabs + "/UI_FloatingText.prefab";
            AssetReplace.SavePrefab(root, path);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // ------------------------------------------------------------ world-space bars

        // UGUI world canvases read correctly from their -Z side, so face +Z along the
        // camera's forward (Shop camera: pos (5.5,10.1,-8.9) looking at (0.15,0,-0.55)).
        static readonly Quaternion WorldBarRotation =
            Quaternion.LookRotation(new Vector3(-0.378f, -0.714f, 0.590f).normalized, Vector3.up);

        static WorldProgressBar BuildAnvilBar(Transform parent, Vector3 localPos, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(IdleBlacksmith.UI.WorldProgressBar));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.rotation = WorldBarRotation; // world-space: face the camera regardless of parent rotation
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(150, 40);
            rt.localScale = Vector3.one * 0.01f;
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var cg = go.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var bgGo = Box("Bg", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 36));
            var bgImg = bgGo.gameObject.AddComponent<Image>();
            bgImg.sprite = bar; bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.18f, 0.13f, 0.09f, 0.92f);
            bgImg.raycastTarget = false;

            var fillGo = Box("Fill", bgGo, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, Vector2.zero);
            var fillRt = (RectTransform)fillGo;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(4, 4);
            fillRt.offsetMax = new Vector2(-4, -4);
            var fill = fillGo.gameObject.AddComponent<Image>();
            fill.sprite = bar; fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.color = Orange;
            fill.raycastTarget = false;

            var barComp = go.GetComponent<WorldProgressBar>();
            barComp.canvasGroup = cg;
            barComp.fill = fill;
            barComp.root = rt;
            return barComp;
        }

        static RackStockBar BuildRackBar(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("RackBar", typeof(RectTransform), typeof(Canvas), typeof(IdleBlacksmith.UI.RackStockBar));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.rotation = WorldBarRotation; // world-space: face the camera regardless of parent rotation
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(200, 52);
            rt.localScale = Vector3.one * 0.01f;
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            var bgGo = Box("Bg", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 52));
            var bgImg = bgGo.gameObject.AddComponent<Image>();
            bgImg.sprite = pill; bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.18f, 0.13f, 0.09f, 0.92f);
            bgImg.raycastTarget = false;

            var iconGo = Box("Icon", bgGo, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 2), new Vector2(34, 34));
            Img(iconGo.gameObject, AssetFactory.LoadIcon("rack"), Color.white).raycastTarget = false;

            var labelGo = Box("Label", bgGo, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -2), new Vector2(140, 34));
            var label = Txt(labelGo, "0/4", 26, Cream, TextAlignmentOptions.Right, bodyFont);

            var barBgGo = Box("BarBg", bgGo, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(12, 6), new Vector2(160, 10));
            var barBgImg = barBgGo.gameObject.AddComponent<Image>();
            barBgImg.sprite = bar; barBgImg.type = Image.Type.Sliced;
            barBgImg.color = new Color(0f, 0f, 0f, 0.45f);
            barBgImg.raycastTarget = false;
            var fillGo = Box("Fill", barBgGo, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, Vector2.zero);
            var fillRt = (RectTransform)fillGo;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);
            var fill = fillGo.gameObject.AddComponent<Image>();
            fill.sprite = bar; fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.color = Orange;
            fill.raycastTarget = false;

            var barComp = go.GetComponent<RackStockBar>();
            barComp.label = label;
            barComp.fill = fill;
            barComp.root = rt;
            return barComp;
        }

        // ------------------------------------------------------------ helpers

        static void BuildIconTile(RectTransform parentRow, Sprite iconSprite)
        {
            var tile = Box("IconTile", parentRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(104, 104));
            var tileImg = tile.gameObject.AddComponent<Image>();
            tileImg.sprite = roundedSmall; tileImg.type = Image.Type.Sliced; tileImg.color = Color.white;
            var mask = tile.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var iconGo = StretchBox("Icon", tile);
            var icon = iconGo.gameObject.AddComponent<Image>();
            icon.sprite = iconSprite;
            icon.raycastTarget = false;
            icon.preserveAspect = true;
        }

        static void SetButtonColors(Selectable btn)
        {
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.04f, 1.04f, 1.04f);
            colors.pressedColor = new Color(0.92f, 0.92f, 0.92f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.55f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
        }

        static void SoftShadow(GameObject go, float dy = -4f, float alpha = 0.3f)
        {
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0.24f, 0.16f, 0.10f, alpha);
            sh.effectDistance = new Vector2(0f, dy);
        }

        static Sprite LoadSprite(string name)
            => AssetDatabase.LoadAssetAtPath<Sprite>($"{Paths.UiSprites}/{name}.png");

        /// <summary>Prefers a purpose-made onboarding page, falling back to an older piece of art.</summary>
        static Sprite LoadPageArt(string preferred, string fallback)
            => AssetFactory.LoadMenuArt(preferred) ?? AssetFactory.LoadMenuArt(fallback);

        static RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go.GetComponent<RectTransform>();
        }

        static RectTransform StretchBox(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>
        /// Cover-fit an art Image inside a masked box: the rect is driven to the sprite's
        /// own aspect ratio and the parent Mask crops the overflow, so the art is never
        /// stretched. The ratio must be assigned BEFORE the mode — AddComponent runs
        /// OnEnable/UpdateRect right away, and with the default AspectMode.None the editor
        /// branch writes the *container's* ratio into the serialized field, which
        /// EnvelopeParent would then use.
        /// </summary>
        static AspectRatioFitter CoverFitter(RectTransform rt, Image img, float fallbackAspect = 1f)
        {
            img.preserveAspect = false;
            var fitter = rt.gameObject.AddComponent<AspectRatioFitter>();
            Sprite s = img.sprite;
            fitter.aspectRatio = (s != null && s.rect.height > 0.01f) ? s.rect.width / s.rect.height : fallbackAspect;
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            return fitter;
        }

        /// <summary>Sprite aspect ratio, or the fallback when the sprite is missing/empty.</summary>
        static float AspectOf(Sprite s, float fallback = 1f)
            => (s != null && s.rect.height > 0.01f) ? s.rect.width / s.rect.height : fallback;

        static Image Img(GameObject go, Sprite sprite, Color color)
        {
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        static TMP_Text Txt(RectTransform rt, string content, float size, Color color,
            TextAlignmentOptions align, TMP_FontAsset font, bool wrap = false)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.enableWordWrapping = wrap;
            t.overflowMode = wrap ? TextOverflowModes.Truncate : TextOverflowModes.Overflow;
            if (font != null) t.font = font;
            return t;
        }
    }
}
