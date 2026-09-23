using System.Collections.Generic;
using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Achievements and lifetime statistics in one sheet. Both are read-only views of the
    /// save, so the panel only has to rebuild when something unlocks.
    /// </summary>
    public class MetaPanel : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform sheet;
        public CanvasGroup backdrop;
        public Button backdropButton;
        public Button closeButton;
        public TMP_Text titleLabel;
        public TMP_Text counterLabel;

        [Header("Tabs")]
        public BouncyButton achievementsTab;
        public BouncyButton statsTab;
        public BouncyButton codexTab;
        public GameObject achievementsPage;
        public GameObject statsPage;
        public GameObject codexPage;
        public Transform codexParent;

        [Header("Achievements")]
        public AchRow achRowPrefab;
        public Transform achParent;
        public Image achFill;

        [Header("Stats")]
        public TMP_Text statsBody;

        [Header("Layout")]
        public float openY = 26f;
        public float closedY = -1900f;

        public bool IsOpen { get; private set; }

        readonly List<AchRow> rows = new List<AchRow>();
        readonly List<(AchRow row, RecipeDef recipe)> codexRows = new List<(AchRow, RecipeDef)>();
        bool built;
        int page; // 0 achievements, 1 stats, 2 codex

        public void Init()
        {
            BuildRows();
            gameObject.SetActive(false);

            GameManager gm = GameManager.Instance;
            if (gm != null && gm.achievements != null)
            {
                gm.achievements.OnUnlocked += HandleUnlocked;
                gm.achievements.OnChanged += Refresh;
            }
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
            if (achievementsTab != null) achievementsTab.onClick.AddListener(() => ShowPage(0));
            if (statsTab != null) statsTab.onClick.AddListener(() => ShowPage(1));
            if (codexTab != null) codexTab.onClick.AddListener(() => ShowPage(2));
            ShowPage(0);
        }

        void BuildRows()
        {
            if (built) return;
            built = true;
            GameManager gm = GameManager.Instance;
            GameConfig config = gm != null ? gm.config : null;
            if (config == null || config.achievements == null || achRowPrefab == null || achParent == null) return;

            foreach (AchievementDef def in config.achievements)
            {
                if (def == null) continue;
                AchRow row = Instantiate(achRowPrefab, achParent);
                row.Bind(def);
                rows.Add(row);
            }

            // Forge Codex: one row per recipe showing how many of it were forged
            // and which rarity tiers have been hit (reuses the achievement row visual).
            if (codexParent != null && config.recipes != null)
                foreach (RecipeDef recipe in config.recipes)
                {
                    if (recipe == null) continue;
                    AchRow row = Instantiate(achRowPrefab, codexParent);
                    if (row.icon != null && recipe.icon != null) row.icon.sprite = recipe.icon;
                    if (row.nameLabel != null) row.nameLabel.text = recipe.displayName;
                    if (row.descLabel != null) row.descLabel.text = recipe.description;
                    codexRows.Add((row, recipe));
                }
        }

        void HandleUnlocked(AchievementDef def)
        {
            AudioManager.Play("achievement");
            if (UIManager.Instance != null)
                UIManager.Instance.SpawnFloatingText(
                    new Vector3(0f, 1.9f, 0f), "Achievement: " + def.displayName,
                    new Color(1f, 0.86f, 0.42f));
            Refresh();
        }

        public void ShowPage(int pageIndex)
        {
            page = pageIndex;
            if (achievementsPage != null) achievementsPage.SetActive(page == 0);
            if (statsPage != null) statsPage.SetActive(page == 1);
            if (codexPage != null) codexPage.SetActive(page == 2);
            TintTab(achievementsTab, page == 0);
            TintTab(statsTab, page == 1);
            TintTab(codexTab, page == 2);
            Refresh();
        }

        static void TintTab(BouncyButton tab, bool active)
        {
            if (tab == null) return;
            var img = tab.targetGraphic as Image;
            if (img != null) img.color = active
                ? new Color(0.95f, 0.60f, 0.29f)
                : new Color(0.86f, 0.80f, 0.72f);
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.achievements == null) return;

            int unlocked = gm.achievements.UnlockedCount;
            int total = gm.achievements.TotalCount;
            if (titleLabel != null)
                titleLabel.text = page == 1 ? "Statistics" : page == 2 ? "Forge Codex" : "Achievements";
            if (counterLabel != null)
                counterLabel.text = page == 1
                    ? (gm.Data != null && gm.Data.stats != null
                        ? $"Best sword: {RarityInfo.NameOf((Rarity)gm.Data.stats.bestRarity)}"
                        : "")
                    : page == 2
                        ? CodexCounter(gm)
                        : $"{unlocked} / {total} unlocked   ·   each grants a permanent bonus";
            if (achFill != null)
                achFill.fillAmount = page == 2 ? CodexFill(gm) : (total > 0 ? unlocked / (float)total : 0f);

            if (page == 1 && statsBody != null && gm.Data != null && gm.Data.stats != null)
                statsBody.text = StatsText(gm);

            if (page == 2) RefreshCodex(gm);

            foreach (AchRow r in rows)
                if (r != null) r.Refresh();
        }

        static string CodexCounter(GameManager gm)
        {
            if (gm.Data == null || gm.Data.stats == null || gm.config == null || gm.config.recipes == null)
                return "";
            int found = 0, totalRecipes = 0;
            foreach (RecipeDef r in gm.config.recipes)
            {
                if (r == null) continue;
                totalRecipes++;
                if (gm.Data.stats.ForgedCount(r.id) > 0) found++;
            }
            return $"{found} / {totalRecipes} recipes forged   ·   pips show rarity tiers hit";
        }

        static float CodexFill(GameManager gm)
        {
            if (gm.Data == null || gm.Data.stats == null || gm.config == null || gm.config.recipes == null)
                return 0f;
            int found = 0, totalRecipes = 0;
            foreach (RecipeDef r in gm.config.recipes)
            {
                if (r == null) continue;
                totalRecipes++;
                if (gm.Data.stats.ForgedCount(r.id) > 0) found++;
            }
            return totalRecipes > 0 ? found / (float)totalRecipes : 0f;
        }

        void RefreshCodex(GameManager gm)
        {
            StatBlock s = gm.Data != null ? gm.Data.stats : null;
            foreach ((AchRow row, RecipeDef recipe) in codexRows)
            {
                if (row == null || recipe == null) continue;
                int count = s != null ? s.ForgedCount(recipe.id) : 0;
                int mask = s != null ? s.RarityMask(recipe.id) : 0;
                int best = s != null ? s.BestRarityOf(recipe.id) : -1;

                if (row.unlockedBadge != null) row.unlockedBadge.SetActive(count > 0);
                if (row.frame != null)
                    row.frame.color = count > 0 && best >= 0
                        ? RarityInfo.TextColor((Rarity)best)
                        : new Color(0.66f, 0.63f, 0.60f);
                if (row.content != null) row.content.alpha = count > 0 ? 1f : 0.55f;
                if (row.bonusLabel != null) row.bonusLabel.text = count > 0 ? Pips(mask) : "";
                if (row.progressLabel != null)
                    row.progressLabel.text = count > 0 ? count + " forged" : "not yet forged";
            }
        }

        static string Pips(int mask)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                bool hit = (mask & (1 << i)) != 0;
                if (hit)
                    sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(RarityInfo.TextColor((Rarity)i))).Append(">●</color>");
                else
                    sb.Append("○");
                if (i < 4) sb.Append(' ');
            }
            return sb.ToString();
        }

        static string StatsText(GameManager gm)
        {
            StatBlock s = gm.Data.stats;
            var lines = new List<string>
            {
                $"Swords forged          {s.swordsForged}",
                $"Swords sold            {s.swordsSold}",
                $"Gold earned (lifetime) {GoldCounter.Format(s.goldEarned)}",
                $"Customers served       {s.customersServed}",
                $"Expeditions claimed    {s.expeditionsClaimed}",
                $"Relic ore earned       {s.relicsEarned}",
                $"Buildings upgraded     {s.buildingsUpgraded}",
                $"Recipes unlocked       {s.recipesUnlocked}",
                $"Best sword forged      {RarityInfo.NameOf((Rarity)s.bestRarity)}",
                $"Workbench tools used   {s.toolUses}",
                $"Cat pets               {s.catPets}",
                $"Times rekindled        {s.prestiges}",
                $"Play time              {FormatPlaytime(s.playSeconds)}",
            };
            if (gm.resources != null)
                lines.Add($"Ore per second         {gm.resources.OrePerSecond:0.##}");
            if (gm.upgrades != null)
                lines.Add($"Luck                   {gm.upgrades.Luck:0.##}");
            if (gm.expeditions != null)
                lines.Add($"Expedition slots       {gm.expeditions.Capacity}");
            return string.Join("\n", lines);
        }

        static string FormatPlaytime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            int h = total / 3600;
            int m = (total % 3600) / 60;
            return h > 0 ? $"{h}h {m}m" : $"{m}m";
        }

        public void Open()
        {
            if (IsOpen || sheet == null) return;
            IsOpen = true;
            gameObject.SetActive(true);
            Refresh();
            float x = sheet.anchoredPosition.x;
            sheet.anchoredPosition = new Vector2(x, closedY);
            Tween.UIAnchoredPosition(sheet, new Vector2(x, openY), 0.45f, Ease.OutBack);
            if (backdrop != null)
            {
                backdrop.alpha = 0f;
                Tween.Alpha(backdrop, 1f, 0.3f, Ease.OutQuad);
                backdrop.blocksRaycasts = true;
                backdrop.interactable = true;
            }
            AudioManager.Play("pop", 0.03f);
        }

        public void Close()
        {
            if (!IsOpen || sheet == null) return;
            IsOpen = false;
            if (backdrop != null)
            {
                backdrop.blocksRaycasts = false;
                backdrop.interactable = false;
                Tween.Alpha(backdrop, 0f, 0.25f, Ease.InQuad);
            }
            float x = sheet.anchoredPosition.x;
            Tween.UIAnchoredPosition(sheet, new Vector2(x, closedY), 0.3f, Ease.InBack)
                .OnComplete(() => { if (!IsOpen) gameObject.SetActive(false); });
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void PreviewOpenForScreenshot(bool stats = false)
        {
            BuildRows();
            gameObject.SetActive(true);
            IsOpen = true;
            ShowPage(stats ? 1 : 0);
            if (sheet != null) sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, openY);
            if (backdrop != null) backdrop.alpha = 1f;
        }

        public void PreviewClose()
        {
            IsOpen = false;
            if (sheet != null) sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, closedY);
            if (backdrop != null) backdrop.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
