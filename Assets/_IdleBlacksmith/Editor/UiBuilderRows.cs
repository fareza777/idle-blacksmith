using IdleBlacksmith.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// Sheets, panels and row widgets added with the complex expansion. Shares every helper
    /// with the core UiBuilder through the partial class, so layout conventions stay identical.
    /// </summary>
    public static partial class UiBuilder
    {
        // ------------------------------------------------------------ shared sheet scaffolding

        /// <summary>Result of <see cref="Sheet"/>: the pieces every bottom sheet needs.</summary>
        class SheetRefs
        {
            public RectTransform root;
            public CanvasGroup backdrop;
            public Button backdropButton;
            public RectTransform sheet;
            public Button closeButton;
            public float closedY;
        }

        /// <summary>
        /// Builds the standard bottom sheet: full-screen dim backdrop, a rounded cream panel
        /// that slides up, and a close button. Panels differ only in what they put inside.
        /// </summary>
        static SheetRefs Sheet(Transform parent, string name, float height, Color backdropTint)
        {
            var refs = new SheetRefs();
            refs.root = StretchBox(name, parent);

            var backdrop = StretchBox("Backdrop", refs.root);
            var backdropImg = backdrop.gameObject.AddComponent<Image>();
            backdropImg.color = backdropTint;
            refs.backdrop = backdrop.gameObject.AddComponent<CanvasGroup>();
            refs.backdrop.alpha = 0f;
            refs.backdrop.blocksRaycasts = false;
            refs.backdropButton = backdrop.gameObject.AddComponent<Button>();
            refs.backdropButton.transition = Selectable.Transition.None;
            refs.backdropButton.targetGraphic = backdropImg;

            float closed = -(height + 120f);
            refs.closedY = closed;
            refs.sheet = Box("Sheet", refs.root, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, closed), new Vector2(1000, height));
            var sheetImg = refs.sheet.gameObject.AddComponent<Image>();
            sheetImg.sprite = rounded; sheetImg.type = Image.Type.Sliced; sheetImg.color = Cream;
            SoftShadow(refs.sheet.gameObject, -6f, 0.4f);

            var closeGo = Box("CloseButton", refs.sheet, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(68, 68));
            var closeBtn = closeGo.gameObject.AddComponent<BouncyButton>();
            var closeImg = closeGo.gameObject.AddComponent<Image>();
            closeImg.sprite = circle; closeImg.type = Image.Type.Sliced; closeImg.color = Hex(0xEBDCC3);
            closeBtn.targetGraphic = closeImg;
            SetButtonColors(closeBtn);
            var closeX = Box("X", closeGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52));
            Txt(closeX, "×", 40, Brown, TextAlignmentOptions.Center, titleFont);
            refs.closeButton = closeBtn;

            return refs;
        }

        static VerticalLayoutGroup Rows(Transform sheet, float topOffset, float height)
        {
            var area = Box("RowsArea", sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, topOffset), new Vector2(940, height));
            var vlg = area.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return vlg;
        }

        static void SheetTitle(RectTransform sheet, string title, string subtitle, out TMP_Text subLabel)
        {
            var titleGo = Box("Title", sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -22), new Vector2(620, 60));
            Txt(titleGo, title, 52, Brown, TextAlignmentOptions.Left, titleFont);
            var subGo = Box("Subtitle", sheet, new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -76), new Vector2(640, 38));
            subLabel = Txt(subGo, subtitle, 26, Secondary, TextAlignmentOptions.Left, bodyFont);
        }

        /// <summary>A small pill used for level / cost / state chips on a row.</summary>
        static RectTransform Chip(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var go = Box(name, parent, anchor, anchor, pos, size);
            var img = go.gameObject.AddComponent<Image>();
            img.sprite = pill; img.type = Image.Type.Sliced; img.color = color;
            return go;
        }

        /// <summary>Icon tile with a rounded mask and a centred, aspect-preserving icon.</summary>
        static Image RowIcon(RectTransform row, Sprite sprite)
        {
            var tile = Box("IconTile", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(104, 104));
            var tileImg = tile.gameObject.AddComponent<Image>();
            tileImg.sprite = roundedSmall; tileImg.type = Image.Type.Sliced; tileImg.color = Color.white;
            var mask = tile.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var iconGo = StretchBox("Icon", tile);
            var icon = iconGo.gameObject.AddComponent<Image>();
            icon.sprite = sprite;
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            return icon;
        }

        /// <summary>Standard row background + layout height.</summary>
        static CanvasGroup RowShell(GameObject root, float height, Color tint)
        {
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(940, height);
            var le = root.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            var img = root.AddComponent<Image>();
            img.sprite = roundedSmall; img.type = Image.Type.Sliced; img.color = tint;
            SoftShadow(root, -3f, 0.22f);
            return root.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// A right-aligned action pill with a label, used as the row's CTA.
        /// <paramref name="rightOffset"/> is the distance from the row's right edge to the button's
        /// right edge, so callers can stack a cost pill beside it without overlap.
        /// </summary>
        static BouncyButton RowAction(RectTransform row, string label, Color color, out TMP_Text labelText,
            float rightOffset = 18f, float width = 200f)
        {
            var go = Box("Action", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-rightOffset, 0), new Vector2(width, 70));
            var btn = go.gameObject.AddComponent<BouncyButton>();
            var img = go.gameObject.AddComponent<Image>();
            img.sprite = pill; img.type = Image.Type.Sliced; img.color = color;
            btn.targetGraphic = img;
            SetButtonColors(btn);
            SoftShadow(go.gameObject, -3f, 0.3f);
            var txtGo = Box("Label", go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width - 16f, 52));
            labelText = Txt(txtGo, label, 30, Color.white, TextAlignmentOptions.Center, titleFont);
            return btn;
        }

        /// <summary>
        /// The cost chip that sits beside a row's action button. Keeping it in the same horizontal
        /// band as the button means it can never collide with the row's text column.
        /// </summary>
        static RectTransform RowCostPill(RectTransform row, string iconName, Color tint, Color textColor,
            float rightOffset, out TMP_Text costLabel)
        {
            var pill = Chip("CostPill", row, new Vector2(1, 0.5f), new Vector2(-rightOffset, 0), new Vector2(170, 62), tint);
            var iconGo = Box("Icon", pill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(42, 42));
            Img(iconGo.gameObject, AssetFactory.LoadIcon(iconName), Color.white).raycastTarget = false;
            var costGo = Box("Value", pill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(58, 0), new Vector2(104, 48));
            costLabel = Txt(costGo, "0", 30, textColor, TextAlignmentOptions.Left, titleFont);
            return pill;
        }

        /// <summary>A tiny row of level pips showing filled vs empty progress.</summary>
        static void Pips(RectTransform row, Vector2 pos, int count, out Image[] pips)
        {
            var go = Box("Pips", row, new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(420, 22));
            var hlg = go.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            pips = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var pipGo = Box("Pip" + i, go, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(30, 16));
                var img = pipGo.gameObject.AddComponent<Image>();
                img.sprite = bar; img.type = Image.Type.Sliced; img.color = new Color(0.78f, 0.74f, 0.68f);
                img.raycastTarget = false;
                var le = pipGo.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 30; le.preferredHeight = 16;
                pips[i] = img;
            }
        }

        /// <summary>Rebuilds a prefab asset from a temporary GameObject, in place so its GUID survives.</summary>
        static GameObject SavePrefabRow(GameObject root, string path)
        {
            AssetReplace.SavePrefab(root, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // ------------------------------------------------------------ building row prefab

        public const string BuildingRowPrefabPath = Paths.Prefabs + "/UI_BuildingRow.prefab";

        static GameObject BuildBuildingRowPrefab()
        {
            var root = new GameObject("BuildingRow", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            var content = RowShell(root, 168, Color.white);

            Image icon = RowIcon(rt, null);

            // Left column: everything descriptive.
            var nameGo = Box("Name", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -14), new Vector2(420, 42));
            var nameLabel = Txt(nameGo, "Building", 32, Brown, TextAlignmentOptions.Left, titleFont);

            var perkGo = Box("Perk", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -58), new Vector2(430, 30));
            var perkLabel = Txt(perkGo, "Effect", 22, Secondary, TextAlignmentOptions.Left, bodyFont);

            Pips(rt, new Vector2(140, -96), 5, out Image[] levelPips);

            // Right column: state, cost, action — laid out horizontally so nothing can overlap.
            var levelGo = Box("Level", rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -16), new Vector2(220, 34));
            var levelLabel = Txt(levelGo, "Not built", 24, Secondary, TextAlignmentOptions.Right, bodyFont);

            RectTransform costPill = RowCostPill(rt, "coin", Hex(0xF2994A), Color.white, 224f, out TMP_Text costLabel);
            BouncyButton buyBtn = RowAction(rt, "BUILD", Orange, out _, 18f, 190f);

            var maxBadge = Chip("MaxBadge", rt, new Vector2(1, 1), new Vector2(-18, -58), new Vector2(190, 54), Hex(0xC99638));
            var maxGo = StretchBox("Label", maxBadge);
            Txt(maxGo, "MAXED", 28, Color.white, TextAlignmentOptions.Center, titleFont);
            maxBadge.gameObject.SetActive(false);

            var runeGo = Box("RuneButton", rt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(140, 22), new Vector2(210, 62));
            var runeBtn = runeGo.gameObject.AddComponent<BouncyButton>();
            var runeImg = runeGo.gameObject.AddComponent<Image>();
            runeImg.sprite = pill; runeImg.type = Image.Type.Sliced; runeImg.color = Teal;
            runeBtn.targetGraphic = runeImg;
            SetButtonColors(runeBtn);
            var runeTxtGo = Box("Label", runeGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(196, 50));
            var runeLabel = Txt(runeTxtGo, "RUNES", 26, Color.white, TextAlignmentOptions.Center, titleFont);
            runeGo.gameObject.SetActive(false);

            var row = root.AddComponent<BuildingRow>();
            row.icon = icon;
            row.nameLabel = nameLabel;
            row.levelLabel = levelLabel;
            row.perkLabel = perkLabel;
            row.costLabel = costLabel;
            row.costPill = costPill.gameObject;
            row.buyButton = buyBtn;
            row.maxBadge = maxBadge.gameObject;
            row.lockedBadge = null;
            row.content = content;
            row.levelPips = levelPips;
            row.runeButton = runeBtn;
            row.runeButtonLabel = runeLabel;

            return SavePrefabRow(root, BuildingRowPrefabPath);
        }

        // ------------------------------------------------------------ rune row prefab

        public const string RuneRowPrefabPath = Paths.Prefabs + "/UI_RuneRow.prefab";

        static GameObject BuildRuneRowPrefab()
        {
            var root = new GameObject("RuneRow", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            var content = RowShell(root, 168, Color.white);

            Image icon = RowIcon(rt, null);

            var nameGo = Box("Name", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -14), new Vector2(420, 42));
            var nameLabel = Txt(nameGo, "Rune", 32, Brown, TextAlignmentOptions.Left, titleFont);

            var descGo = Box("Desc", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -58), new Vector2(430, 30));
            var descLabel = Txt(descGo, "Effect", 22, Secondary, TextAlignmentOptions.Left, bodyFont);

            var levelGo = Box("Level", rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -16), new Vector2(220, 34));
            var levelLabel = Txt(levelGo, "Lv 0/4", 24, Secondary, TextAlignmentOptions.Right, bodyFont);

            RectTransform costPill = RowCostPill(rt, "ore", new Color(0.18f, 0.32f, 0.36f, 1f), OreText, 224f, out TMP_Text costLabel);
            BouncyButton buyBtn = RowAction(rt, "CARVE", PurpleAccent, out _, 18f, 190f);

            var maxBadge = Chip("MaxBadge", rt, new Vector2(1, 1), new Vector2(-18, -58), new Vector2(190, 54), Hex(0xC99638));
            var maxGo = StretchBox("Label", maxBadge);
            Txt(maxGo, "MAXED", 28, Color.white, TextAlignmentOptions.Center, titleFont);
            maxBadge.gameObject.SetActive(false);

            var row = root.AddComponent<RuneRow>();
            row.icon = icon;
            row.nameLabel = nameLabel;
            row.descLabel = descLabel;
            row.levelLabel = levelLabel;
            row.costLabel = costLabel;
            row.costPill = costPill.gameObject;
            row.buyButton = buyBtn;
            row.maxBadge = maxBadge.gameObject;
            row.content = content;

            return SavePrefabRow(root, RuneRowPrefabPath);
        }

        static readonly Color PurpleAccent = Hex(0x9A6FB8);

        // ------------------------------------------------------------ recipe card prefab

        public const string RecipeCardPrefabPath = Paths.Prefabs + "/UI_RecipeCard.prefab";

        static GameObject BuildRecipeCardPrefab()
        {
            var root = new GameObject("RecipeCard", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            var content = RowShell(root, 168, Color.white);

            var frameGo = StretchBox("Frame", rt);
            var frameImg = frameGo.gameObject.AddComponent<Image>();
            frameImg.sprite = roundedSmall; frameImg.type = Image.Type.Sliced; frameImg.color = Color.white;
            frameImg.raycastTarget = false;
            // keep the frame behind the content it outlines
            frameGo.SetAsFirstSibling();

            // tier accent: a thin strip on the left edge, tinted per recipe metal
            var accentGo = Box("Accent", rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(16, 140));
            var accentImg = accentGo.gameObject.AddComponent<Image>();
            accentImg.sprite = bar; accentImg.type = Image.Type.Sliced;
            accentImg.raycastTarget = false;

            Image icon = RowIcon(rt, null);

            var nameGo = Box("Name", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -14), new Vector2(420, 42));
            var nameLabel = Txt(nameGo, "Recipe", 32, Brown, TextAlignmentOptions.Left, titleFont);

            var statGo = Box("Stats", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -58), new Vector2(430, 30));
            var statLabel = Txt(statGo, "1 ore · 10 gold · 3.2s", 22, Secondary, TextAlignmentOptions.Left, bodyFont);

            var forgedGo = Box("Forged", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -92), new Vector2(430, 30));
            var forgedLabel = Txt(forgedGo, "", 22, Secondary, TextAlignmentOptions.Left, bodyFont);

            BouncyButton selectBtn = RowAction(rt, "SELECT", Orange, out TMP_Text selectLabel);

            var activeBadge = Chip("ActiveBadge", rt, new Vector2(1, 1), new Vector2(-18, -58), new Vector2(190, 54), Hex(0x5BA86B));
            var activeGo = StretchBox("Label", activeBadge);
            Txt(activeGo, "FORGING NOW", 22, Color.white, TextAlignmentOptions.Center, bodyFont);
            activeBadge.gameObject.SetActive(false);

            var lockedBadge = Chip("LockedBadge", rt, new Vector2(1, 1), new Vector2(-18, -58), new Vector2(190, 54), new Color(0.62f, 0.60f, 0.58f, 0.92f));
            var lockedGo = StretchBox("Label", lockedBadge);
            var lockedLabel = Txt(lockedGo, "Needs Smithy 2", 20, Color.white, TextAlignmentOptions.Center, bodyFont);
            lockedBadge.gameObject.SetActive(false);

            // Gold corner tag marking today's featured recipe (+30% sale price).
            var dailyBadge = Chip("DailyBadge", rt, new Vector2(1, 1), new Vector2(-14, -8), new Vector2(172, 38), Hex(0xE8B84B));
            var dailyGo = StretchBox("Label", dailyBadge);
            Txt(dailyGo, "TODAY +30%", 19, new Color(0.35f, 0.22f, 0.05f), TextAlignmentOptions.Center, bodyFont);
            dailyBadge.gameObject.SetActive(false);

            var card = root.AddComponent<RecipeCard>();
            card.icon = icon;
            card.frame = frameImg;
            card.accent = accentImg;
            card.nameLabel = nameLabel;
            card.statLabel = statLabel;
            card.forgedLabel = forgedLabel;
            card.descLabel = null;
            card.selectButton = selectBtn;
            card.selectLabel = selectLabel;
            card.lockedBadge = lockedBadge.gameObject;
            card.lockedLabel = lockedLabel;
            card.activeBadge = activeBadge.gameObject;
            card.dailyBadge = dailyBadge.gameObject;
            card.content = content;

            return SavePrefabRow(root, RecipeCardPrefabPath);
        }

        // ------------------------------------------------------------ achievement row prefab

        public const string AchRowPrefabPath = Paths.Prefabs + "/UI_AchRow.prefab";

        static GameObject BuildAchRowPrefab()
        {
            var root = new GameObject("AchRow", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            var content = RowShell(root, 132, Color.white);

            var frameGo = StretchBox("Frame", rt);
            var frameImg = frameGo.gameObject.AddComponent<Image>();
            frameImg.sprite = roundedSmall; frameImg.type = Image.Type.Sliced; frameImg.color = new Color(0.66f, 0.63f, 0.60f);
            frameImg.raycastTarget = false;
            frameGo.SetAsFirstSibling();

            Image icon = RowIcon(rt, null);

            var nameGo = Box("Name", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -16), new Vector2(440, 42));
            var nameLabel = Txt(nameGo, "Achievement", 32, Brown, TextAlignmentOptions.Left, titleFont);

            var descGo = Box("Desc", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -58), new Vector2(520, 32));
            var descLabel = Txt(descGo, "Requirement", 24, Secondary, TextAlignmentOptions.Left, bodyFont);

            var progressGo = Box("Progress", rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, -22), new Vector2(220, 40));
            var progressLabel = Txt(progressGo, "0 / 1", 26, Secondary, TextAlignmentOptions.Right, bodyFont);

            var bonusGo = Box("Bonus", rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -18), new Vector2(300, 36));
            var bonusLabel = Txt(bonusGo, "+2% gold", 24, Hex(0x5BA86B), TextAlignmentOptions.Right, bodyFont);

            var unlockedBadge = Chip("UnlockedBadge", rt, new Vector2(0, 0), new Vector2(140, 18), new Vector2(180, 48), Hex(0x5BA86B));
            var unlockedGo = StretchBox("Label", unlockedBadge);
            Txt(unlockedGo, "UNLOCKED", 22, Color.white, TextAlignmentOptions.Center, bodyFont);
            unlockedBadge.gameObject.SetActive(false);

            var row = root.AddComponent<AchRow>();
            row.icon = icon;
            row.frame = frameImg;
            row.nameLabel = nameLabel;
            row.descLabel = descLabel;
            row.bonusLabel = bonusLabel;
            row.progressLabel = progressLabel;
            row.unlockedBadge = unlockedBadge.gameObject;
            row.content = content;

            return SavePrefabRow(root, AchRowPrefabPath);
        }

        // ------------------------------------------------------------ talent row prefab

        public const string TalentRowPrefabPath = Paths.Prefabs + "/UI_TalentRow.prefab";

        static GameObject BuildTalentRowPrefab()
        {
            var root = new GameObject("TalentRow", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            var content = RowShell(root, 168, Color.white);

            Image icon = RowIcon(rt, null);

            var nameGo = Box("Name", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -14), new Vector2(420, 42));
            var nameLabel = Txt(nameGo, "Talent", 32, Brown, TextAlignmentOptions.Left, titleFont);

            var descGo = Box("Desc", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -58), new Vector2(430, 30));
            var descLabel = Txt(descGo, "Effect", 22, Secondary, TextAlignmentOptions.Left, bodyFont);

            var levelGo = Box("Level", rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -16), new Vector2(220, 34));
            var levelLabel = Txt(levelGo, "Lv 0/5", 24, Secondary, TextAlignmentOptions.Right, bodyFont);

            RectTransform costPill = RowCostPill(rt, "ember", Hex(0xF2994A), Color.white, 224f, out TMP_Text costLabel);
            BouncyButton buyBtn = RowAction(rt, "LEARN", Hex(0xDF7F47), out _, 18f, 190f);

            var maxBadge = Chip("MaxBadge", rt, new Vector2(1, 1), new Vector2(-18, -58), new Vector2(190, 54), Hex(0xC99638));
            var maxGo = StretchBox("Label", maxBadge);
            Txt(maxGo, "MASTERED", 26, Color.white, TextAlignmentOptions.Center, titleFont);
            maxBadge.gameObject.SetActive(false);

            var row = root.AddComponent<TalentRow>();
            row.icon = icon;
            row.nameLabel = nameLabel;
            row.descLabel = descLabel;
            row.levelLabel = levelLabel;
            row.costLabel = costLabel;
            row.costPill = costPill.gameObject;
            row.buyButton = buyBtn;
            row.maxBadge = maxBadge.gameObject;
            row.content = content;

            return SavePrefabRow(root, TalentRowPrefabPath);
        }
    }
}
