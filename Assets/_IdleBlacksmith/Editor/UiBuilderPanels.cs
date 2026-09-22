using IdleBlacksmith.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// Screen construction for the expanded game: complex, forge, runes, quests, meta,
    /// prestige, settings, welcome-back and the title screen, plus the reworked HUD.
    /// </summary>
    public static partial class UiBuilder
    {
        // ------------------------------------------------------------ complex sheet

        static ComplexPanel BuildComplexPanel(Transform parent, GameObject buildingRowPrefab, GameObject runeRowPrefab)
        {
            SheetRefs s = Sheet(parent, "ComplexPanel", 1560, new Color(0.16f, 0.11f, 0.07f, 0.62f));
            var panel = s.root.gameObject.AddComponent<ComplexPanel>();

            SheetTitle(s.sheet, "The Forge Complex", "Five buildings — each one earns while you are away",
                out TMP_Text subtitle);

            var runePanel = BuildRunePanel(s.sheet, runeRowPrefab);

            Rows(s.sheet, -120f, 1380);

            panel.sheet = s.sheet;
            panel.backdrop = s.backdrop;
            panel.backdropButton = s.backdropButton;
            panel.closeButton = s.closeButton;
            panel.subtitleLabel = subtitle;
            panel.rowPrefab = buildingRowPrefab.GetComponent<BuildingRow>();
            panel.rowsParent = s.sheet.Find("RowsArea");
            panel.runePanel = runePanel;
            panel.openY = 26f;
            panel.closedY = s.closedY;
            s.root.gameObject.SetActive(false);
            return panel;
        }

        static RunePanel BuildRunePanel(RectTransform complexSheet, GameObject runeRowPrefab)
        {
            // The rune sheet lives inside the complex sheet so it slides over it naturally.
            var root = StretchBox("RunePanel", complexSheet);
            var panel = root.gameObject.AddComponent<RunePanel>();

            var backdrop = StretchBox("Backdrop", root);
            var backdropImg = backdrop.gameObject.AddComponent<Image>();
            backdropImg.color = new Color(0.10f, 0.09f, 0.13f, 0.72f);
            var backdropCg = backdrop.gameObject.AddComponent<CanvasGroup>();
            backdropCg.alpha = 0f;
            backdropCg.blocksRaycasts = false;
            var backdropBtn = backdrop.gameObject.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.targetGraphic = backdropImg;

            var sheet = Box("Sheet", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, -1500), new Vector2(960, 1200));
            var sheetImg = sheet.gameObject.AddComponent<Image>();
            sheetImg.sprite = rounded; sheetImg.type = Image.Type.Sliced; sheetImg.color = Cream;
            SoftShadow(sheet.gameObject, -6f, 0.4f);

            var titleGo = Box("Title", sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -20), new Vector2(560, 56));
            Txt(titleGo, "Runes", 48, Brown, TextAlignmentOptions.Left, titleFont);
            var capGo = Box("Cap", sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(38, -70), new Vector2(560, 34));
            var capLabel = Txt(capGo, "Max rune level 4", 24, Secondary, TextAlignmentOptions.Left, bodyFont);

            // relic ore budget
            var oreRow = Box("OreRow", sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(900, 78));
            var oreImg = oreRow.gameObject.AddComponent<Image>();
            oreImg.sprite = roundedSmall; oreImg.type = Image.Type.Sliced; oreImg.color = new Color(0.18f, 0.32f, 0.36f, 1f);
            var oreIconGo = Box("Icon", oreRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(52, 52));
            Img(oreIconGo.gameObject, AssetFactory.LoadIcon("ore"), Color.white).raycastTarget = false;
            var oreCountGo = Box("Count", oreRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(88, 0), new Vector2(140, 56));
            var relicLabel = Txt(oreCountGo, "0", 40, OreText, TextAlignmentOptions.Left, titleFont);
            var oreNameGo = Box("Name", oreRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(230, 0), new Vector2(340, 44));
            Txt(oreNameGo, "Relic Ore", 28, Color.white, TextAlignmentOptions.Left, titleFont);

            var rowsArea = Box("RowsArea", sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(900, 900));
            var vlg = rowsArea.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var closeGo = Box("CloseButton", sheet, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(68, 68));
            var closeBtn = closeGo.gameObject.AddComponent<BouncyButton>();
            var closeImg = closeGo.gameObject.AddComponent<Image>();
            closeImg.sprite = circle; closeImg.type = Image.Type.Sliced; closeImg.color = Hex(0xEBDCC3);
            closeBtn.targetGraphic = closeImg;
            SetButtonColors(closeBtn);
            var closeX = Box("X", closeGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52));
            Txt(closeX, "×", 40, Brown, TextAlignmentOptions.Center, titleFont);

            panel.sheet = sheet;
            panel.backdrop = backdropCg;
            panel.backdropButton = backdropBtn;
            panel.closeButton = closeBtn;
            panel.relicLabel = relicLabel;
            panel.capLabel = capLabel;
            panel.rowPrefab = runeRowPrefab.GetComponent<RuneRow>();
            panel.rowsParent = rowsArea;
            panel.openY = 26f;
            panel.closedY = -1500f;
            root.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------ forge sheet

        static ForgePanel BuildForgePanel(Transform parent, GameObject recipeCardPrefab)
        {
            SheetRefs s = Sheet(parent, "ForgePanel", 1560, new Color(0.16f, 0.11f, 0.07f, 0.62f));
            var panel = s.root.gameObject.AddComponent<ForgePanel>();

            SheetTitle(s.sheet, "The Forge", "Choose what your smith hammers out next", out TMP_Text hint);

            var oreRow = Box("OreRow", s.sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(940, 74));
            var oreImg = oreRow.gameObject.AddComponent<Image>();
            oreImg.sprite = roundedSmall; oreImg.type = Image.Type.Sliced; oreImg.color = new Color(0.34f, 0.27f, 0.22f, 1f);
            var oreIconGo = Box("Icon", oreRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(50, 50));
            Img(oreIconGo.gameObject, AssetFactory.LoadIcon("ore"), Color.white).raycastTarget = false;
            var oreLabelGo = Box("Count", oreRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(84, 0), new Vector2(200, 52));
            var oreLabel = Txt(oreLabelGo, "0 / 40", 34, Color.white, TextAlignmentOptions.Left, titleFont);
            var oreNameGo = Box("Name", oreRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(300, 0), new Vector2(300, 44));
            Txt(oreNameGo, "Ore stored", 26, new Color(0.86f, 0.80f, 0.72f), TextAlignmentOptions.Left, bodyFont);
            var rateGo = Box("Rate", oreRow, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(320, 48));
            Txt(rateGo, "No ore, no swords", 24, new Color(0.86f, 0.80f, 0.72f), TextAlignmentOptions.Right, bodyFont);

            Rows(s.sheet, -206f, 1300);

            panel.sheet = s.sheet;
            panel.backdrop = s.backdrop;
            panel.backdropButton = s.backdropButton;
            panel.closeButton = s.closeButton;
            panel.cardPrefab = recipeCardPrefab.GetComponent<RecipeCard>();
            panel.cardsParent = s.sheet.Find("RowsArea");
            panel.hintLabel = hint;
            panel.oreLabel = oreLabel;
            panel.openY = 26f;
            panel.closedY = s.closedY;
            s.root.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------ quest sheet

        static QuestPanel BuildQuestPanel(Transform parent)
        {
            SheetRefs s = Sheet(parent, "QuestPanel", 900, new Color(0.16f, 0.11f, 0.07f, 0.62f));
            var panel = s.root.gameObject.AddComponent<QuestPanel>();

            var titleGo = Box("Title", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -24), new Vector2(620, 60));
            var titleLabel = Txt(titleGo, "Quest", 52, Brown, TextAlignmentOptions.Left, titleFont);
            var counterGo = Box("Counter", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -78), new Vector2(620, 36));
            var counterLabel = Txt(counterGo, "0 / 0 complete", 26, Secondary, TextAlignmentOptions.Left, bodyFont);

            // objective card
            var card = Box("Card", s.sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(920, 560));
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.sprite = roundedSmall; cardImg.type = Image.Type.Sliced; cardImg.color = new Color(1f, 1f, 1f, 0.85f);

            var iconGo = Box("Icon", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(120, 120));
            Img(iconGo.gameObject, AssetFactory.LoadIcon("scroll"), Color.white).raycastTarget = false;

            var bodyGo = Box("Body", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(820, 180));
            var bodyLabel = Txt(bodyGo, "...", 30, Secondary, TextAlignmentOptions.Center, bodyFont, true);

            // progress bar
            var barBg = Box("BarBg", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -390), new Vector2(760, 34));
            var barBgImg = barBg.gameObject.AddComponent<Image>();
            barBgImg.sprite = bar; barBgImg.type = Image.Type.Sliced; barBgImg.color = Hex(0xEBDCC3);
            var fillGo = Box("Fill", barBg, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(760, 34));
            fillGo.anchorMin = new Vector2(0, 0); fillGo.anchorMax = new Vector2(1, 1);
            fillGo.offsetMin = Vector2.zero; fillGo.offsetMax = Vector2.zero;
            var fillImg = fillGo.gameObject.AddComponent<Image>();
            fillImg.sprite = bar; fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.color = Orange; fillImg.fillAmount = 0f; fillImg.raycastTarget = false;

            var progressGo = Box("Progress", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -400), new Vector2(220, 44));
            var progressLabel = Txt(progressGo, "0 / 5", 30, Brown, TextAlignmentOptions.Right, titleFont);
            var rewardGo = Box("Reward", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(820, 56));
            var rewardLabel = Txt(rewardGo, "Rewards", 28, Hex(0x5BA86B), TextAlignmentOptions.Center, titleFont);

            var achGo = Box("AchievementsButton", s.sheet, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 30), new Vector2(420, 96));
            var achBtn = achGo.gameObject.AddComponent<BouncyButton>();
            var achImg = achGo.gameObject.AddComponent<Image>();
            achImg.sprite = pill; achImg.type = Image.Type.Sliced; achImg.color = Green;
            achBtn.targetGraphic = achImg;
            SetButtonColors(achBtn);
            SoftShadow(achGo.gameObject, -4f, 0.3f);
            var achIconGo = Box("Icon", achGo, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(60, 60));
            Img(achIconGo.gameObject, AssetFactory.LoadIcon("trophy"), Color.white).raycastTarget = false;
            var achTxtGo = Box("Label", achGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26, 0), new Vector2(300, 56));
            Txt(achTxtGo, "ACHIEVEMENTS", 30, Color.white, TextAlignmentOptions.Center, titleFont);

            panel.sheet = s.sheet;
            panel.backdrop = s.backdrop;
            panel.backdropButton = s.backdropButton;
            panel.closeButton = s.closeButton;
            panel.titleLabel = titleLabel;
            panel.bodyLabel = bodyLabel;
            panel.progressLabel = progressLabel;
            panel.rewardLabel = rewardLabel;
            panel.counterLabel = counterLabel;
            panel.fill = fillImg;
            panel.achievementsButton = achBtn;
            panel.openY = 26f;
            panel.closedY = s.closedY;
            s.root.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------ meta sheet (achievements + stats)

        static MetaPanel BuildMetaPanel(Transform parent, GameObject achRowPrefab)
        {
            SheetRefs s = Sheet(parent, "MetaPanel", 1560, new Color(0.16f, 0.11f, 0.07f, 0.62f));
            var panel = s.root.gameObject.AddComponent<MetaPanel>();

            var titleGo = Box("Title", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -24), new Vector2(620, 56));
            var titleLabel = Txt(titleGo, "Achievements", 52, Brown, TextAlignmentOptions.Left, titleFont);
            var counterGo = Box("Counter", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -76), new Vector2(700, 34));
            var counterLabel = Txt(counterGo, "0 / 0", 26, Secondary, TextAlignmentOptions.Left, bodyFont);

            var barBg = Box("AchBarBg", s.sheet, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-120, -78), new Vector2(400, 22));
            var barBgImg = barBg.gameObject.AddComponent<Image>();
            barBgImg.sprite = bar; barBgImg.type = Image.Type.Sliced; barBgImg.color = Hex(0xEBDCC3);
            var fillGo = StretchBox("Fill", barBg);
            var fillImg = fillGo.gameObject.AddComponent<Image>();
            fillImg.sprite = bar; fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.color = Green; fillImg.fillAmount = 0f; fillImg.raycastTarget = false;

            // tabs
            var achTabGo = Box("AchievementsTab", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -120), new Vector2(300, 76));
            var achTab = achTabGo.gameObject.AddComponent<BouncyButton>();
            var achTabImg = achTabGo.gameObject.AddComponent<Image>();
            achTabImg.sprite = pill; achTabImg.type = Image.Type.Sliced; achTabImg.color = Orange;
            achTab.targetGraphic = achTabImg;
            SetButtonColors(achTab);
            var achTabTxtGo = Box("Label", achTabGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 52));
            Txt(achTabTxtGo, "ACHIEVEMENTS", 26, Color.white, TextAlignmentOptions.Center, titleFont);

            var statsTabGo = Box("StatsTab", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(352, -120), new Vector2(260, 76));
            var statsTab = statsTabGo.gameObject.AddComponent<BouncyButton>();
            var statsTabImg = statsTabGo.gameObject.AddComponent<Image>();
            statsTabImg.sprite = pill; statsTabImg.type = Image.Type.Sliced; statsTabImg.color = new Color(0.86f, 0.80f, 0.72f);
            statsTab.targetGraphic = statsTabImg;
            SetButtonColors(statsTab);
            var statsTabTxtGo = Box("Label", statsTabGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 52));
            Txt(statsTabTxtGo, "STATS", 26, Color.white, TextAlignmentOptions.Center, titleFont);

            // achievements page
            var achPage = Box("AchievementsPage", s.sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -212), new Vector2(940, 1300));
            var vlg = achPage.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // stats page
            var statsPage = Box("StatsPage", s.sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -212), new Vector2(920, 1260));
            var statsImg = statsPage.gameObject.AddComponent<Image>();
            statsImg.sprite = roundedSmall; statsImg.type = Image.Type.Sliced; statsImg.color = new Color(1f, 1f, 1f, 0.85f);
            var statsBodyGo = StretchBox("Body", statsPage);
            statsBodyGo.offsetMin = new Vector2(36, 30);
            statsBodyGo.offsetMax = new Vector2(-36, -30);
            var statsBody = Txt(statsBodyGo, "...", 30, Brown, TextAlignmentOptions.TopLeft, bodyFont, true);
            statsPage.gameObject.SetActive(false);

            panel.sheet = s.sheet;
            panel.backdrop = s.backdrop;
            panel.backdropButton = s.backdropButton;
            panel.closeButton = s.closeButton;
            panel.titleLabel = titleLabel;
            panel.counterLabel = counterLabel;
            panel.achievementsTab = achTab;
            panel.statsTab = statsTab;
            panel.achievementsPage = achPage.gameObject;
            panel.statsPage = statsPage.gameObject;
            panel.achRowPrefab = achRowPrefab.GetComponent<AchRow>();
            panel.achParent = achPage;
            panel.achFill = fillImg;
            panel.statsBody = statsBody;
            panel.openY = 26f;
            panel.closedY = s.closedY;
            s.root.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------ prestige sheet

        static PrestigePanel BuildPrestigePanel(Transform parent, GameObject talentRowPrefab)
        {
            SheetRefs s = Sheet(parent, "PrestigePanel", 1600, new Color(0.16f, 0.08f, 0.05f, 0.72f));
            var panel = s.root.gameObject.AddComponent<PrestigePanel>();

            var artGo = Box("ArtMask", s.sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(976, 300));
            var artImg = artGo.gameObject.AddComponent<Image>();
            artImg.sprite = roundedSmall; artImg.type = Image.Type.Sliced; artImg.color = Color.white;
            var artMask = artGo.gameObject.AddComponent<Mask>();
            artMask.showMaskGraphic = true;
            var artInner = StretchBox("Art", artGo);
            var artImage = artInner.gameObject.AddComponent<Image>();
            artImage.sprite = AssetFactory.LoadMenuArt("prestige_art");
            artImage.raycastTarget = false;
            CoverFitter(artInner, artImage, 1f);
            var dimGo = Box("Dim", artGo, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(976, 120));
            var dimImg = dimGo.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.12f, 0.06f, 0.04f, 0.55f);
            dimImg.raycastTarget = false;
            var artTitleGo = Box("Title", artGo, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 16), new Vector2(600, 56));
            Txt(artTitleGo, "Rekindle the Forge", 46, Color.white, TextAlignmentOptions.Left, titleFont);

            // shard summary
            var shardRow = Box("ShardRow", s.sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(940, 86));
            var shardImg = shardRow.gameObject.AddComponent<Image>();
            shardImg.sprite = roundedSmall; shardImg.type = Image.Type.Sliced; shardImg.color = new Color(0.28f, 0.16f, 0.10f, 1f);
            var shardIconGo = Box("Icon", shardRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(58, 58));
            Img(shardIconGo.gameObject, AssetFactory.LoadIcon("ember"), Color.white).raycastTarget = false;
            var shardGo = Box("Count", shardRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(94, 0), new Vector2(180, 60));
            var shardLabel = Txt(shardGo, "0", 44, new Color(1f, 0.72f, 0.32f), TextAlignmentOptions.Left, titleFont);
            var countGo = Box("Count", shardRow, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-26, 0), new Vector2(560, 46));
            var countLabel = Txt(countGo, "The forge has never been rekindled", 26, new Color(0.95f, 0.85f, 0.78f), TextAlignmentOptions.Right, bodyFont);

            var pendingGo = Box("Pending", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -436), new Vector2(880, 40));
            var pendingLabel = Txt(pendingGo, "Earn more gold this run", 28, Brown, TextAlignmentOptions.Left, bodyFont);

            var barBg = Box("BarBg", s.sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -486), new Vector2(920, 30));
            var barBgImg = barBg.gameObject.AddComponent<Image>();
            barBgImg.sprite = bar; barBgImg.type = Image.Type.Sliced; barBgImg.color = Hex(0xEBDCC3);
            var fillGo = StretchBox("Fill", barBg);
            var fillImg = fillGo.gameObject.AddComponent<Image>();
            fillImg.sprite = bar; fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.color = Hex(0xDF7F47); fillImg.fillAmount = 0f; fillImg.raycastTarget = false;

            var runGo = Box("RunLabel", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -526), new Vector2(880, 36));
            var runLabel = Txt(runGo, "This run: 0 gold", 26, Secondary, TextAlignmentOptions.Left, bodyFont);

            // rekindle button
            var prestGo = Box("Rekindle", s.sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -600), new Vector2(560, 104));
            var prestBtn = prestGo.gameObject.AddComponent<BouncyButton>();
            var prestImg = prestGo.gameObject.AddComponent<Image>();
            prestImg.sprite = pill; prestImg.type = Image.Type.Sliced; prestImg.color = Hex(0xD95F4E);
            prestBtn.targetGraphic = prestImg;
            SetButtonColors(prestBtn);
            SoftShadow(prestGo.gameObject, -5f, 0.35f);
            var prestTxtGo = Box("Label", prestGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540, 76));
            var prestLabel = Txt(prestTxtGo, "REKINDLE", 42, Color.white, TextAlignmentOptions.Center, titleFont);

            var talentTitleGo = Box("TalentTitle", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -722), new Vector2(700, 42));
            Txt(talentTitleGo, "Ember Talents — permanent, kept through every rekindle", 28, Secondary, TextAlignmentOptions.Left, bodyFont);

            var talentArea = Box("RowsArea", s.sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -772), new Vector2(940, 780));
            var tlg = talentArea.gameObject.AddComponent<VerticalLayoutGroup>();
            tlg.spacing = 12;
            tlg.childAlignment = TextAnchor.UpperCenter;
            tlg.childControlWidth = true;
            tlg.childControlHeight = false;
            tlg.childForceExpandWidth = true;
            tlg.childForceExpandHeight = false;

            // confirm modal
            var confirm = StretchBox("Confirm", s.sheet);
            var confirmDim = StretchBox("Dim", confirm);
            var confirmDimImg = confirmDim.gameObject.AddComponent<Image>();
            confirmDimImg.color = new Color(0.10f, 0.06f, 0.04f, 0.82f);
            confirmDimImg.raycastTarget = true;
            var confirmCard = Box("Card", confirm, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 620));
            var confirmCardImg = confirmCard.gameObject.AddComponent<Image>();
            confirmCardImg.sprite = rounded; confirmCardImg.type = Image.Type.Sliced; confirmCardImg.color = Cream;
            SoftShadow(confirmCard.gameObject, -6f, 0.45f);
            var confirmBodyGo = Box("Body", confirmCard, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(740, 300));
            var confirmBody = Txt(confirmBodyGo, "Rekindle?", 28, Brown, TextAlignmentOptions.Center, bodyFont, true);

            var yesGo = Box("Yes", confirmCard, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-160, 150), new Vector2(290, 100));
            var yesBtn = yesGo.gameObject.AddComponent<BouncyButton>();
            var yesImg = yesGo.gameObject.AddComponent<Image>();
            yesImg.sprite = pill; yesImg.type = Image.Type.Sliced; yesImg.color = Hex(0xD95F4E);
            yesBtn.targetGraphic = yesImg;
            SetButtonColors(yesBtn);
            var yesTxtGo = Box("Label", yesGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270, 72));
            Txt(yesTxtGo, "REKINDLE", 32, Color.white, TextAlignmentOptions.Center, titleFont);

            var noGo = Box("No", confirmCard, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(160, 150), new Vector2(290, 100));
            var noBtn = noGo.gameObject.AddComponent<BouncyButton>();
            var noImg = noGo.gameObject.AddComponent<Image>();
            noImg.sprite = pill; noImg.type = Image.Type.Sliced; noImg.color = new Color(0.78f, 0.72f, 0.64f);
            noBtn.targetGraphic = noImg;
            SetButtonColors(noBtn);
            var noTxtGo = Box("Label", noGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270, 72));
            Txt(noTxtGo, "NOT YET", 32, Brown, TextAlignmentOptions.Center, titleFont);
            confirm.gameObject.SetActive(false);

            panel.sheet = s.sheet;
            panel.backdrop = s.backdrop;
            panel.backdropButton = s.backdropButton;
            panel.closeButton = s.closeButton;
            panel.artImage = artImage;
            panel.shardLabel = shardLabel;
            panel.pendingLabel = pendingLabel;
            panel.runLabel = runLabel;
            panel.countLabel = countLabel;
            panel.progressFill = fillImg;
            panel.prestigeButton = prestBtn;
            panel.prestigeButtonLabel = prestLabel;
            panel.talentRowPrefab = talentRowPrefab.GetComponent<TalentRow>();
            panel.talentsParent = talentArea;
            panel.confirmRoot = confirm.gameObject;
            panel.confirmBody = confirmBody;
            panel.confirmYes = yesBtn;
            panel.confirmNo = noBtn;
            panel.openY = 26f;
            panel.closedY = s.closedY;
            s.root.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------ settings sheet

        static SettingsPanel BuildSettingsPanel(Transform parent)
        {
            SheetRefs s = Sheet(parent, "SettingsPanel", 1560, new Color(0.12f, 0.09f, 0.07f, 0.72f));
            var panel = s.root.gameObject.AddComponent<SettingsPanel>();

            SheetTitle(s.sheet, "Settings", "Sound, music, haptics and your save", out _);

            float y = -136f;
            BouncyButton muteBtn = SettingsRow(s.sheet, "Sound", y, out TMP_Text muteLabel);
            y -= 104f;
            var volRow = Box("VolumeRow", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, y), new Vector2(920, 88));
            var volImg = volRow.gameObject.AddComponent<Image>();
            volImg.sprite = roundedSmall; volImg.type = Image.Type.Sliced; volImg.color = new Color(1f, 1f, 1f, 0.9f);
            var volNameGo = Box("Name", volRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(300, 52));
            Txt(volNameGo, "Volume", 32, Brown, TextAlignmentOptions.Left, titleFont);
            var sliderGo = Box("Slider", volRow, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-190, 0), new Vector2(360, 40));
            var slider = sliderGo.gameObject.AddComponent<Slider>();
            var sliderBg = StretchBox("Background", sliderGo);
            var sliderBgImg = sliderBg.gameObject.AddComponent<Image>();
            sliderBgImg.sprite = bar; sliderBgImg.type = Image.Type.Sliced; sliderBgImg.color = Hex(0xEBDCC3);
            var fillArea = StretchBox("Fill Area", sliderGo);
            fillArea.offsetMin = new Vector2(8, 8); fillArea.offsetMax = new Vector2(-8, -8);
            var fillRect = StretchBox("Fill", fillArea);
            var volFill = fillRect.gameObject.AddComponent<Image>();
            volFill.sprite = bar; volFill.type = Image.Type.Sliced; volFill.color = Orange;
            var handleArea = StretchBox("Handle Slide Area", sliderGo);
            var handleGo = Box("Handle", handleArea, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
            var handleImg = handleGo.gameObject.AddComponent<Image>();
            handleImg.sprite = circle; handleImg.type = Image.Type.Sliced; handleImg.color = Cream;
            slider.fillRect = fillRect;
            slider.handleRect = handleGo;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            var volValueGo = Box("Value", volRow, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(120, 48));
            var volumeLabel = Txt(volValueGo, "80%", 30, Brown, TextAlignmentOptions.Right, titleFont);
            y -= 104f;

            BouncyButton musicBtn = SettingsRow(s.sheet, "Music", y, out TMP_Text musicLabel);
            y -= 104f;

            BouncyButton hapticBtn = SettingsRow(s.sheet, "Haptics", y, out TMP_Text hapticLabel);
            y -= 104f;

            BouncyButton fxBtn = SettingsRow(s.sheet, "Effects", y, out TMP_Text fxLabel);
            y -= 104f;

            BouncyButton introBtn = SettingsRow(s.sheet, "Replay Intro", y, out TMP_Text introLabel);
            if (introLabel != null) introLabel.text = ">";
            y -= 104f;

            var saveGo = Box("SavePath", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, y), new Vector2(920, 76));
            var saveLabel = Txt(saveGo, "Save file: ...", 20, Secondary, TextAlignmentOptions.Left, bodyFont, true);
            y -= 92f;

            BouncyButton credBtn = SettingsRow(s.sheet, "Credits", y, out TMP_Text credLabel);
            if (credLabel != null) credLabel.text = ">";
            y -= 104f;

            var menuGo = Box("MenuRow", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, y), new Vector2(920, 96));
            var menuBtn = menuGo.gameObject.AddComponent<BouncyButton>();
            var menuImg = menuGo.gameObject.AddComponent<Image>();
            menuImg.sprite = roundedSmall; menuImg.type = Image.Type.Sliced; menuImg.color = Teal;
            menuBtn.targetGraphic = menuImg;
            SetButtonColors(menuBtn);
            var menuTxtGo = Box("Label", menuGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880, 70));
            Txt(menuTxtGo, "BACK TO TITLE", 34, Color.white, TextAlignmentOptions.Center, titleFont);
            y -= 116f;

            var resetGo = Box("ResetRow", s.sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, y), new Vector2(920, 96));
            var resetBtn = resetGo.gameObject.AddComponent<BouncyButton>();
            var resetImg = resetGo.gameObject.AddComponent<Image>();
            resetImg.sprite = roundedSmall; resetImg.type = Image.Type.Sliced; resetImg.color = new Color(0.80f, 0.62f, 0.55f);
            resetBtn.targetGraphic = resetImg;
            SetButtonColors(resetBtn);
            var resetTxtGo = Box("Label", resetGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880, 70));
            var resetLabel = Txt(resetTxtGo, "RESET SAVE", 34, Color.white, TextAlignmentOptions.Center, titleFont);

            // credits overlay
            var credits = StretchBox("Credits", s.sheet);
            var creditsDim = StretchBox("Dim", credits);
            var creditsDimImg = creditsDim.gameObject.AddComponent<Image>();
            creditsDimImg.color = new Color(0.10f, 0.07f, 0.05f, 0.88f);
            creditsDimImg.raycastTarget = true;
            var creditsBodyGo = Box("Body", credits, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 1100));
            var creditsBody = Txt(creditsBodyGo, "", 24, Cream, TextAlignmentOptions.TopLeft, bodyFont, true);
            var creditsCloseGo = Box("Close", credits, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(320, 90));
            var creditsCloseBtn = creditsCloseGo.gameObject.AddComponent<BouncyButton>();
            var creditsCloseImg = creditsCloseGo.gameObject.AddComponent<Image>();
            creditsCloseImg.sprite = pill; creditsCloseImg.type = Image.Type.Sliced; creditsCloseImg.color = Orange;
            creditsCloseBtn.targetGraphic = creditsCloseImg;
            SetButtonColors(creditsCloseBtn);
            var creditsCloseTxtGo = Box("Label", creditsCloseGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 66));
            Txt(creditsCloseTxtGo, "BACK", 32, Color.white, TextAlignmentOptions.Center, titleFont);
            credits.gameObject.SetActive(false);

            panel.sheet = s.sheet;
            panel.backdrop = s.backdrop;
            panel.backdropButton = s.backdropButton;
            panel.closeButton = s.closeButton;
            panel.muteButton = muteBtn;
            panel.muteLabel = muteLabel;
            panel.musicButton = musicBtn;
            panel.musicLabel = musicLabel;
            panel.volumeSlider = slider;
            panel.volumeLabel = volumeLabel;
            panel.hapticButton = hapticBtn;
            panel.hapticLabel = hapticLabel;
            panel.fxButton = fxBtn;
            panel.fxLabel = fxLabel;
            panel.introButton = introBtn;
            panel.introLabel = introLabel;
            panel.menuButton = menuBtn;
            panel.resetButton = resetBtn;
            panel.resetLabel = resetLabel;
            panel.creditsButton = credBtn;
            panel.creditsRoot = credits.gameObject;
            panel.creditsBody = creditsBody;
            panel.creditsClose = creditsCloseBtn;
            panel.savePathLabel = saveLabel;
            panel.openY = 26f;
            panel.closedY = s.closedY;
            s.root.gameObject.SetActive(false);
            return panel;
        }

        /// <summary>One settings row: name on the left, a value pill on the right, whole row tappable.</summary>
        static BouncyButton SettingsRow(RectTransform sheet, string label, float y, out TMP_Text valueLabel)
        {
            var go = Box(label + "Row", sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, y), new Vector2(920, 96));
            var btn = go.gameObject.AddComponent<BouncyButton>();
            var img = go.gameObject.AddComponent<Image>();
            img.sprite = roundedSmall; img.type = Image.Type.Sliced; img.color = new Color(1f, 1f, 1f, 0.9f);
            btn.targetGraphic = img;
            SetButtonColors(btn);
            SoftShadow(go.gameObject, -3f, 0.2f);

            var nameGo = Box("Name", go, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(500, 56));
            Txt(nameGo, label, 34, Brown, TextAlignmentOptions.Left, titleFont);

            var valueGo = Box("Value", go, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(220, 56));
            valueLabel = Txt(valueGo, "ON", 32, Orange, TextAlignmentOptions.Right, titleFont);
            return btn;
        }

        // ------------------------------------------------------------ welcome back

        static WelcomeBackPanel BuildWelcomeBack(Transform parent)
        {
            var root = StretchBox("WelcomeBack", parent);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            var dim = StretchBox("Dim", root);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.10f, 0.07f, 0.04f, 0.82f);
            dimImg.raycastTarget = true;

            var card = Box("Card", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880, 900));
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.sprite = rounded; cardImg.type = Image.Type.Sliced; cardImg.color = Cream;
            SoftShadow(card.gameObject, -8f, 0.45f);

            var artMask = Box("ArtMask", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(848, 400));
            var artMaskImg = artMask.gameObject.AddComponent<Image>();
            artMaskImg.sprite = roundedSmall; artMaskImg.type = Image.Type.Sliced; artMaskImg.color = Color.white;
            var mask = artMask.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var artGo = StretchBox("Art", artMask);
            var artImg = artGo.gameObject.AddComponent<Image>();
            artImg.raycastTarget = false;
            Sprite offlineArt = AssetFactory.LoadMenuArt("menu_bg");
            if (offlineArt == null) offlineArt = AssetFactory.LoadMenuArt("splash");
            artImg.sprite = offlineArt;
            CoverFitter(artGo, artImg, 1f);

            var titleGo = Box("Title", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -434), new Vector2(800, 56));
            var titleLabel = Txt(titleGo, "Welcome back!", 50, Brown, TextAlignmentOptions.Center, titleFont);
            var elapsedGo = Box("Elapsed", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -494), new Vector2(800, 40));
            var elapsedLabel = Txt(elapsedGo, "...", 28, Secondary, TextAlignmentOptions.Center, bodyFont);

            var oreGo = Box("Ore", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -548), new Vector2(800, 48));
            var oreLabel = Txt(oreGo, "", 34, new Color(0.30f, 0.55f, 0.62f), TextAlignmentOptions.Center, titleFont);
            var goldGo = Box("Gold", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -600), new Vector2(800, 48));
            var goldLabel = Txt(goldGo, "", 34, Hex(0xC99638), TextAlignmentOptions.Center, titleFont);
            var expGo = Box("Expedition", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -652), new Vector2(820, 44));
            var expLabel = Txt(expGo, "", 26, Teal, TextAlignmentOptions.Center, bodyFont);

            var collectGo = Box("Collect", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(460, 104));
            var collectBtn = collectGo.gameObject.AddComponent<BouncyButton>();
            var collectImg = collectGo.gameObject.AddComponent<Image>();
            collectImg.sprite = pill; collectImg.type = Image.Type.Sliced; collectImg.color = Orange;
            collectBtn.targetGraphic = collectImg;
            SetButtonColors(collectBtn);
            SoftShadow(collectGo.gameObject, -5f, 0.35f);
            var collectTxtGo = Box("Label", collectGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 76));
            Txt(collectTxtGo, "COLLECT", 40, Color.white, TextAlignmentOptions.Center, titleFont);

            var panel = root.gameObject.AddComponent<WelcomeBackPanel>();
            panel.group = cg;
            panel.card = card;
            panel.artImage = artImg;
            panel.titleLabel = titleLabel;
            panel.elapsedLabel = elapsedLabel;
            panel.oreLabel = oreLabel;
            panel.goldLabel = goldLabel;
            panel.expeditionLabel = expLabel;
            panel.collectButton = collectBtn;
            root.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------ daily ember

        static DailyClaimPanel BuildDailyClaim(Transform parent)
        {
            var root = StretchBox("DailyClaim", parent);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            var dim = StretchBox("Dim", root);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.10f, 0.07f, 0.04f, 0.78f);
            dimImg.raycastTarget = true;

            var card = Box("Card", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 640));
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.sprite = rounded; cardImg.type = Image.Type.Sliced; cardImg.color = Cream;
            SoftShadow(card.gameObject, -8f, 0.45f);

            var emblem = Box("Emblem", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(148, 148));
            var emblemImg = emblem.gameObject.AddComponent<Image>();
            emblemImg.sprite = circle; emblemImg.type = Image.Type.Sliced; emblemImg.color = Orange;
            var emblemTxt = Box("Mark", emblem, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 110));
            Txt(emblemTxt, "*", 92, Color.white, TextAlignmentOptions.Center, titleFont);

            var titleGo = Box("Title", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(700, 56));
            var titleLabel = Txt(titleGo, "DAILY EMBER", 46, Brown, TextAlignmentOptions.Center, titleFont);
            var streakGo = Box("Streak", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -248), new Vector2(700, 40));
            var streakLabel = Txt(streakGo, "", 28, Secondary, TextAlignmentOptions.Center, bodyFont);

            var goldGo = Box("Gold", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -310), new Vector2(700, 48));
            var goldLabel = Txt(goldGo, "", 36, Hex(0xC99638), TextAlignmentOptions.Center, titleFont);
            var relicGo = Box("Relic", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -366), new Vector2(700, 46));
            var relicLabel = Txt(relicGo, "", 32, new Color(0.30f, 0.55f, 0.62f), TextAlignmentOptions.Center, titleFont);
            var relicUnit = Box("RelicUnit", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -412), new Vector2(700, 34));
            Txt(relicUnit, "relic ore", 24, Secondary, TextAlignmentOptions.Center, bodyFont);

            var claimGo = Box("Claim", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-116, 42), new Vector2(330, 96));
            var claimBtn = claimGo.gameObject.AddComponent<BouncyButton>();
            var claimImg = claimGo.gameObject.AddComponent<Image>();
            claimImg.sprite = pill; claimImg.type = Image.Type.Sliced; claimImg.color = Orange;
            claimBtn.targetGraphic = claimImg;
            SetButtonColors(claimBtn);
            SoftShadow(claimGo.gameObject, -5f, 0.35f);
            var claimTxtGo = Box("Label", claimGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(310, 70));
            Txt(claimTxtGo, "CLAIM", 36, Color.white, TextAlignmentOptions.Center, titleFont);

            var laterGo = Box("Later", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(232, 42), new Vector2(190, 96));
            var laterBtn = laterGo.gameObject.AddComponent<BouncyButton>();
            var laterImg = laterGo.gameObject.AddComponent<Image>();
            laterImg.sprite = pill; laterImg.type = Image.Type.Sliced; laterImg.color = new Color(1f, 1f, 1f, 0.55f);
            laterBtn.targetGraphic = laterImg;
            SetButtonColors(laterBtn);
            var laterTxtGo = Box("Label", laterGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 70));
            Txt(laterTxtGo, "LATER", 30, Brown, TextAlignmentOptions.Center, titleFont);

            var panel = root.gameObject.AddComponent<DailyClaimPanel>();
            panel.group = cg;
            panel.card = card;
            panel.titleLabel = titleLabel;
            panel.streakLabel = streakLabel;
            panel.goldLabel = goldLabel;
            panel.relicLabel = relicLabel;
            panel.claimButton = claimBtn;
            panel.laterButton = laterBtn;
            // Stays active: the panel polls quietly and only shows itself when today's ember is unclaimed.
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            return panel;
        }

        // ------------------------------------------------------------ main menu

        /// <summary>Small pill button on the menu: label centred, caller picks the tint.</summary>
        static BouncyButton MenuPill(RectTransform parent, string name, string label, Vector2 pos, Vector2 size,
            Color fill, Color labelColor, int fontSize, out TMP_Text labelText)
        {
            var go = Box(name, parent, new Vector2(0.5f, 0), new Vector2(0.5f, 0), pos, size);
            var btn = go.gameObject.AddComponent<BouncyButton>();
            var img = go.gameObject.AddComponent<Image>();
            img.sprite = pill; img.type = Image.Type.Sliced; img.color = fill;
            btn.targetGraphic = img;
            SetButtonColors(btn);
            SoftShadow(go.gameObject, -4f, 0.3f);
            var txtGo = Box("Label", go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(20, 30));
            labelText = Txt(txtGo, label, fontSize, labelColor, TextAlignmentOptions.Center, titleFont);
            return btn;
        }

        static MainMenuPanel BuildMainMenu(Transform parent)
        {
            var root = StretchBox("MainMenu", parent);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            var bgGo = StretchBox("Backdrop", root);
            var bgImg = bgGo.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.10f, 0.08f, 0.06f);
            bgImg.raycastTarget = true;

            var artGo = StretchBox("Art", root);
            var artImg = artGo.gameObject.AddComponent<Image>();
            Sprite menuArt = AssetFactory.LoadMenuArt("menu_bg");
            if (menuArt == null) menuArt = AssetFactory.LoadMenuArt("splash");
            artImg.sprite = menuArt;
            artImg.raycastTarget = false;
            CoverFitter(artGo, artImg, 9f / 16f);

            var shadeTop = Box("ShadeTop", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(1080, 700));
            var shadeTopImg = shadeTop.gameObject.AddComponent<Image>();
            shadeTopImg.color = new Color(0.08f, 0.06f, 0.04f, 0.45f);
            shadeTopImg.raycastTarget = false;
            var shadeBottom = Box("ShadeBottom", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(1080, 980));
            var shadeBottomImg = shadeBottom.gameObject.AddComponent<Image>();
            shadeBottomImg.color = new Color(0.08f, 0.06f, 0.04f, 0.66f);
            shadeBottomImg.raycastTarget = false;

            var block = Box("TitleBlock", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 240), new Vector2(960, 560));
            var emblemGo = Box("Emblem", block, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(280, 280));
            var emblemImg = emblemGo.gameObject.AddComponent<Image>();
            emblemImg.sprite = AssetFactory.LoadMenuArt("emblem");
            emblemImg.preserveAspect = true;
            emblemImg.raycastTarget = false;
            var titleGo = Box("Title", block, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(980, 110));
            var titleLabel = Txt(titleGo, "EMBERFORGE", 84, GoldText, TextAlignmentOptions.Center, titleFont);
            var tagGo = Box("Tagline", block, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -412), new Vector2(960, 44));
            var taglineLabel = Txt(tagGo, "an idle blacksmith tale", 32, new Color(1f, 0.93f, 0.80f, 0.92f), TextAlignmentOptions.Center, bodyFont);
            var progGo = Box("Progress", block, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -470), new Vector2(940, 44));
            var progressLabel = Txt(progGo, "", 28, new Color(0.92f, 0.88f, 0.78f), TextAlignmentOptions.Center, bodyFont);

            // primary actions
            var contBtn = MenuPill(root, "ContinueButton", "CONTINUE", new Vector2(0, 470), new Vector2(560, 128),
                Orange, Color.white, 52, out _);
            var newBtn = MenuPill(root, "NewGameButton", "START THE FORGE", new Vector2(0, 336), new Vector2(560, 110),
                new Color(0.86f, 0.55f, 0.30f), Color.white, 40, out TMP_Text newLabel);

            // secondary row
            var settingsBtn = MenuPill(root, "SettingsButton", "SETTINGS", new Vector2(-160, 216), new Vector2(280, 92),
                new Color(0.78f, 0.72f, 0.64f), Brown, 30, out _);
            var aboutBtn = MenuPill(root, "AboutButton", "ABOUT", new Vector2(160, 216), new Vector2(280, 92),
                new Color(0.78f, 0.72f, 0.64f), Brown, 30, out _);

            // store row — quieter ghost pills
            var shareBtn = MenuPill(root, "ShareButton", "SHARE", new Vector2(-160, 110), new Vector2(280, 84),
                new Color(0.42f, 0.30f, 0.20f, 0.88f), Cream, 28, out _);
            var rateBtn = MenuPill(root, "RateButton", "RATE ★", new Vector2(160, 110), new Vector2(280, 84),
                new Color(0.42f, 0.30f, 0.20f, 0.88f), Hex(0xFFD966), 28, out _);

            // credits overlay (About)
            var creditsPanel = StretchBox("Credits", root);
            var cpDim = StretchBox("Dim", creditsPanel);
            var cpDimImg = cpDim.gameObject.AddComponent<Image>();
            cpDimImg.color = new Color(0.08f, 0.06f, 0.05f, 0.92f);
            cpDimImg.raycastTarget = true;
            var cpBodyGo = Box("Body", creditsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(900, 1200));
            var cpBody = Txt(cpBodyGo, "", 24, Cream, TextAlignmentOptions.TopLeft, bodyFont, true);
            var cpCloseGo = Box("Close", creditsPanel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(340, 96));
            var cpCloseBtn = cpCloseGo.gameObject.AddComponent<Button>();
            var cpCloseImg = cpCloseGo.gameObject.AddComponent<Image>();
            cpCloseImg.sprite = pill; cpCloseImg.type = Image.Type.Sliced; cpCloseImg.color = Orange;
            cpCloseBtn.targetGraphic = cpCloseImg;
            var cpCloseTxtGo = Box("Label", cpCloseGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 70));
            Txt(cpCloseTxtGo, "BACK", 32, Color.white, TextAlignmentOptions.Center, titleFont);
            creditsPanel.gameObject.SetActive(false);

            // new-game wipe confirm
            var confirm = StretchBox("Confirm", root);
            var cfDim = StretchBox("Dim", confirm);
            var cfDimImg = cfDim.gameObject.AddComponent<Image>();
            cfDimImg.color = new Color(0.08f, 0.06f, 0.05f, 0.85f);
            cfDimImg.raycastTarget = true;
            var cfCard = Box("Card", confirm, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840, 560));
            var cfCardImg = cfCard.gameObject.AddComponent<Image>();
            cfCardImg.sprite = rounded; cfCardImg.type = Image.Type.Sliced; cfCardImg.color = Cream;
            SoftShadow(cfCard.gameObject, -8f, 0.45f);
            var cfTitleGo = Box("Title", cfCard, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(760, 60));
            Txt(cfTitleGo, "Start a new forge?", 48, Brown, TextAlignmentOptions.Center, titleFont);
            var cfBodyGo = Box("Body", cfCard, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(720, 180));
            Txt(cfBodyGo, "Your saved forge, swords and buildings will be wiped clean.", 30, Secondary, TextAlignmentOptions.Center, bodyFont, true);
            var cfNoBtn = MenuPill(cfCard, "No", "KEEP FORGING", new Vector2(-170, 100), new Vector2(320, 92),
                new Color(0.62f, 0.55f, 0.47f), Color.white, 28, out _);
            var cfYesBtn = MenuPill(cfCard, "Yes", "WIPE & START", new Vector2(170, 100), new Vector2(340, 92),
                new Color(0.86f, 0.40f, 0.30f), Color.white, 30, out _);
            confirm.gameObject.SetActive(false);

            var panel = root.gameObject.AddComponent<MainMenuPanel>();
            panel.group = cg;
            panel.background = bgImg;
            panel.emblem = emblemImg;
            panel.titleBlock = block;
            panel.titleLabel = titleLabel;
            panel.taglineLabel = taglineLabel;
            panel.progressLabel = progressLabel;
            panel.continueButton = contBtn;
            panel.newGameButton = newBtn;
            panel.newGameLabel = newLabel;
            panel.settingsButton = settingsBtn;
            panel.aboutButton = aboutBtn;
            panel.shareButton = shareBtn;
            panel.rateButton = rateBtn;
            panel.creditsRoot = creditsPanel.gameObject;
            panel.creditsBody = cpBody;
            panel.creditsClose = cpCloseBtn;
            panel.confirmRoot = confirm.gameObject;
            panel.confirmYes = cfYesBtn;
            panel.confirmNo = cfNoBtn;
            root.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------ hud ticker

        static HudTicker BuildTicker(RectTransform hud, QuestPanel questPanel)
        {
            // Sits below the resource pills (the last of which ends at y -244), so the three
            // resource readouts and the objective banner never overlap.
            var go = Box("Ticker", hud, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -326), new Vector2(760, 84));
            var cg = go.gameObject.AddComponent<CanvasGroup>();
            var bg = go.gameObject.AddComponent<Image>();
            bg.sprite = pill; bg.type = Image.Type.Sliced; bg.color = new Color(0.24f, 0.18f, 0.13f, 0.86f);
            SoftShadow(go.gameObject, -4f, 0.3f);

            var btn = go.gameObject.AddComponent<BouncyButton>();
            btn.targetGraphic = bg;
            SetButtonColors(btn);

            var iconGo = Box("Icon", go, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(56, 56));
            Img(iconGo.gameObject, AssetFactory.LoadIcon("scroll"), Color.white).raycastTarget = false;

            var textGo = Box("Objective", go, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(82, 0), new Vector2(520, 44));
            var objective = Txt(textGo, "...", 28, Cream, TextAlignmentOptions.Left, bodyFont);

            var progressGo = Box("Progress", go, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(130, 44));
            var progress = Txt(progressGo, "", 26, new Color(1f, 0.82f, 0.42f), TextAlignmentOptions.Right, titleFont);

            var barBg = Box("BarBg", go, new Vector2(0, 0), new Vector2(0, 0), new Vector2(82, 8), new Vector2(520, 10));
            var barBgImg = barBg.gameObject.AddComponent<Image>();
            barBgImg.sprite = bar; barBgImg.type = Image.Type.Sliced; barBgImg.color = new Color(0f, 0f, 0f, 0.35f);
            var fillGo = StretchBox("Fill", barBg);
            var fillImg = fillGo.gameObject.AddComponent<Image>();
            fillImg.sprite = bar; fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.color = new Color(1f, 0.72f, 0.32f); fillImg.fillAmount = 0f; fillImg.raycastTarget = false;

            var ticker = go.gameObject.AddComponent<HudTicker>();
            ticker.objectiveLabel = objective;
            ticker.progressLabel = progress;
            ticker.fill = fillImg;
            ticker.openButton = btn;
            ticker.questPanel = questPanel;
            ticker.group = cg;
            return ticker;
        }

        /// <summary>Contract banner parked just under the quest ticker; hidden until an order lands.</summary>
        static OrderTicker BuildOrderTicker(RectTransform hud)
        {
            var go = Box("OrderTicker", hud, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -420), new Vector2(700, 64));
            var cg = go.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
            var bg = go.gameObject.AddComponent<Image>();
            bg.sprite = pill; bg.type = Image.Type.Sliced; bg.color = new Color(0.30f, 0.20f, 0.10f, 0.9f);
            SoftShadow(go.gameObject, -3f, 0.3f);

            var btn = go.gameObject.AddComponent<BouncyButton>();
            btn.targetGraphic = bg;
            SetButtonColors(btn);

            var iconGo = Box("Icon", go, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(44, 44));
            Img(iconGo.gameObject, AssetFactory.LoadIcon("coin"), Color.white).raycastTarget = false;

            var textGo = Box("Line", go, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(66, 0), new Vector2(470, 38));
            var line = Txt(textGo, "", 24, Cream, TextAlignmentOptions.Left, bodyFont);

            var timerGo = Box("Timer", go, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(110, 38));
            var timer = Txt(timerGo, "", 26, new Color(1f, 0.82f, 0.42f), TextAlignmentOptions.Right, titleFont);

            var barBg = Box("BarBg", go, new Vector2(0, 0), new Vector2(0, 0), new Vector2(66, 7), new Vector2(470, 8));
            var barBgImg = barBg.gameObject.AddComponent<Image>();
            barBgImg.sprite = bar; barBgImg.type = Image.Type.Sliced; barBgImg.color = new Color(0f, 0f, 0f, 0.35f);
            var fillGo = StretchBox("Fill", barBg);
            var fillImg = fillGo.gameObject.AddComponent<Image>();
            fillImg.sprite = bar; fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.color = new Color(0.62f, 0.9f, 1f); fillImg.fillAmount = 0f; fillImg.raycastTarget = false;

            var t = go.gameObject.AddComponent<OrderTicker>();
            t.line = line;
            t.timer = timer;
            t.fill = fillImg;
            t.openButton = btn;
            t.group = cg;
            return t;
        }

        /// <summary>Hot pulsing pill that slams in while rush hour runs.</summary>
        static RushBanner BuildRushBanner(RectTransform hud)
        {
            var go = Box("RushBanner", hud, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -510), new Vector2(560, 56));
            var cg = go.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
            var bg = go.gameObject.AddComponent<Image>();
            bg.sprite = pill; bg.type = Image.Type.Sliced; bg.color = new Color(1f, 0.45f, 0.16f);
            bg.raycastTarget = false;
            SoftShadow(go.gameObject, -4f, 0.4f);

            var labelGo = Box("Label", go, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(430, 40));
            var label = Txt(labelGo, "RUSH HOUR", 26, Color.white, TextAlignmentOptions.Left, titleFont);
            label.raycastTarget = false;

            var timerGo = Box("Timer", go, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(90, 40));
            var timer = Txt(timerGo, "", 28, Color.white, TextAlignmentOptions.Right, titleFont);
            timer.raycastTarget = false;

            var rb = go.gameObject.AddComponent<RushBanner>();
            rb.label = label;
            rb.timer = timer;
            rb.group = cg;
            rb.bg = bg;
            return rb;
        }

        // ------------------------------------------------------------ story dialogue

        /// <summary>Full-width bar pinned to an edge, height driven by the cinematic script.</summary>
        static RectTransform EdgeBar(string name, Transform parent, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0, top ? 1 : 0);
            rt.anchorMax = new Vector2(1, top ? 1 : 0);
            rt.pivot = new Vector2(0.5f, top ? 1 : 0);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.03f, 0.02f, 0.015f, 0.97f);
            img.raycastTarget = true;
            return rt;
        }

        static DialoguePanel BuildDialogue(Transform parent)
        {
            var root = StretchBox("Dialogue", parent);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            // soft dim behind the card — keeps the world visible but mutes it
            var dim = StretchBox("Dim", root);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.08f, 0.05f, 0.03f, 0.35f);
            dimImg.raycastTarget = true;

            var card = Box("Card", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(1000, 360));
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.sprite = rounded; cardImg.type = Image.Type.Sliced;
            cardImg.color = new Color(0.16f, 0.11f, 0.07f, 0.97f);
            SoftShadow(card.gameObject, -8f, 0.5f);
            var cardBtn = card.gameObject.AddComponent<Button>();
            cardBtn.transition = Selectable.Transition.None;
            cardBtn.targetGraphic = cardImg;

            // portrait medallion: circle mask on the card's top-left corner, half-proud of the edge
            var medGo = Box("Portrait", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -18), new Vector2(180, 180));
            var medImg = medGo.gameObject.AddComponent<Image>();
            medImg.sprite = circle; medImg.type = Image.Type.Sliced; medImg.color = new Color(0.92f, 0.78f, 0.55f);
            SoftShadow(medGo.gameObject, -5f, 0.45f);
            var medMask = medGo.gameObject.AddComponent<Mask>();
            medMask.showMaskGraphic = true;
            var faceGo = StretchBox("Face", medGo);
            var faceImg = faceGo.gameObject.AddComponent<Image>();
            faceImg.preserveAspect = false;
            faceImg.raycastTarget = false;

            // speaker name plate overlapping the portrait
            var nameGo = Box("NamePlate", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -170), new Vector2(300, 56));
            var nameImg = nameGo.gameObject.AddComponent<Image>();
            nameImg.sprite = pill; nameImg.type = Image.Type.Sliced; nameImg.color = Orange;
            var nameTxtGo = Box("Label", nameGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 44));
            var nameLabel = Txt(nameTxtGo, "Bram", 30, Color.white, TextAlignmentOptions.Center, titleFont);

            var bodyGo = Box("Body", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(300, -42), new Vector2(660, 250));
            var bodyLabel = Txt(bodyGo, "...", 34, Cream, TextAlignmentOptions.TopLeft, bodyFont, true);
            bodyLabel.enableAutoSizing = false;
            bodyLabel.richText = true;

            var pageGo = Box("Page", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -24), new Vector2(120, 40));
            var pageLabel = Txt(pageGo, "1/3", 26, new Color(1f, 0.85f, 0.6f, 0.7f), TextAlignmentOptions.Right, bodyFont);

            // blinking advance arrow
            var hintGo = Box("NextHint", card, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-34, 30), new Vector2(56, 56));
            var hintCg = hintGo.gameObject.AddComponent<CanvasGroup>();
            hintCg.blocksRaycasts = false;
            Txt(hintGo, "▶", 34, Orange, TextAlignmentOptions.Center, titleFont);

            var panel = root.gameObject.AddComponent<DialoguePanel>();
            panel.group = cg;
            panel.card = card;
            panel.portraitImage = faceImg;
            panel.nameLabel = nameLabel;
            panel.bodyLabel = bodyLabel;
            panel.cardButton = cardBtn;
            panel.nextHint = hintCg;
            panel.pageLabel = pageLabel;
            panel.openY = 26f;
            panel.closedY = -460f;
            root.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------ intro cinematic

        static IntroCinematic BuildIntro(Transform parent)
        {
            var root = StretchBox("IntroCinematic", parent);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            var bgGo = StretchBox("Backdrop", root);
            var bgImg = bgGo.gameObject.AddComponent<Image>();
            bgImg.color = Color.black;
            bgImg.raycastTarget = true;

            // whole screen advances the cinematic, so it sits under every interactive element
            var advGo = StretchBox("Advance", root);
            var advBtn = advGo.gameObject.AddComponent<Button>();
            advBtn.transition = Selectable.Transition.None;
            var advImg = advGo.gameObject.AddComponent<Image>();
            advImg.color = new Color(0f, 0f, 0f, 0.001f);

            var artGo = StretchBox("Art", root);
            var artImg = artGo.gameObject.AddComponent<Image>();
            artImg.raycastTarget = false;
            artImg.preserveAspect = true;
            // portrait screen + landscape frames: cover-crop the art so it always fills
            var artFitter = CoverFitter(artGo, artImg, 1.5f);

            // letterbox bars
            var barTop = EdgeBar("BarTop", root, true);
            var barBottom = EdgeBar("BarBottom", root, false);

            // caption block rides just above the bottom bar
            var capGo = Box("Caption", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 220), new Vector2(920, 260));
            var capCg = capGo.gameObject.AddComponent<CanvasGroup>();
            capCg.blocksRaycasts = false;
            var capLabel = Txt(capGo, "...", 40, new Color(0.98f, 0.94f, 0.86f), TextAlignmentOptions.Center, bodyFont, true);
            capLabel.fontStyle = FontStyles.Italic;
            var capShadow = capGo.gameObject.AddComponent<Shadow>();
            capShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            capShadow.effectDistance = new Vector2(2, -2);

            var pageGo = Box("Page", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(200, 44));
            var pageLabel = Txt(pageGo, "1 / 4", 26, new Color(1f, 1f, 1f, 0.55f), TextAlignmentOptions.Center, titleFont);

            var hintGo = Box("TapHint", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 200), new Vector2(220, 50));
            var hintCg = hintGo.gameObject.AddComponent<CanvasGroup>();
            hintCg.blocksRaycasts = false;
            Txt(hintGo, "TAP ▸", 30, new Color(1f, 0.85f, 0.5f, 0.9f), TextAlignmentOptions.Right, titleFont);

            var skipGo = Box("Skip", root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -80), new Vector2(200, 80));
            var skipBtn = skipGo.gameObject.AddComponent<BouncyButton>();
            var skipImg = skipGo.gameObject.AddComponent<Image>();
            skipImg.sprite = pill; skipImg.type = Image.Type.Sliced;
            skipImg.color = new Color(1f, 1f, 1f, 0.16f);
            skipBtn.targetGraphic = skipImg;
            SetButtonColors(skipBtn);
            var skipTxtGo = Box("Label", skipGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 56));
            Txt(skipTxtGo, "SKIP ▸▸", 30, Color.white, TextAlignmentOptions.Center, titleFont);

            var cine = root.gameObject.AddComponent<IntroCinematic>();
            cine.group = cg;
            cine.artImage = artImg;
            cine.barTop = barTop;
            cine.barBottom = barBottom;
            cine.captionLabel = capLabel;
            cine.captionGroup = capCg;
            cine.skipButton = skipBtn;
            cine.advanceButton = advBtn;
            cine.pageLabel = pageLabel;
            cine.tapHint = hintCg;
            cine.frames = new[]
            {
                new IntroCinematic.Frame
                {
                    art = AssetFactory.LoadMenuArt("intro_village"),
                    caption = "The village of Emberhold had gone quiet. The forge that once lit the valley burned out generations ago.",
                },
                new IntroCinematic.Frame
                {
                    art = AssetFactory.LoadMenuArt("intro_ember"),
                    caption = "But on the longest night, a single ember fell from the old hearth — and landed in waiting hands.",
                },
                new IntroCinematic.Frame
                {
                    art = AssetFactory.LoadMenuArt("intro_oath"),
                    caption = "\"I swear it,\" the smith whispered. \"Iron will sing again. The racks will fill. The gate will open.\"",
                },
                new IntroCinematic.Frame
                {
                    art = AssetFactory.LoadMenuArt("intro_rise"),
                    caption = "And so the forge woke. This is where your legend begins.",
                },
            };
            root.gameObject.SetActive(false);
            return cine;
        }
    }
}
