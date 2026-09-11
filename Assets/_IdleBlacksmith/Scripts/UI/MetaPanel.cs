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
        public GameObject achievementsPage;
        public GameObject statsPage;

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
        bool built;
        bool showingStats;

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
            if (achievementsTab != null) achievementsTab.onClick.AddListener(() => ShowPage(false));
            if (statsTab != null) statsTab.onClick.AddListener(() => ShowPage(true));
            ShowPage(false);
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

        public void ShowPage(bool stats)
        {
            showingStats = stats;
            if (achievementsPage != null) achievementsPage.SetActive(!stats);
            if (statsPage != null) statsPage.SetActive(stats);
            if (achievementsTab != null)
            {
                var img = achievementsTab.targetGraphic as Image;
                if (img != null) img.color = stats ? new Color(0.86f, 0.80f, 0.72f) : new Color(0.95f, 0.60f, 0.29f);
            }
            if (statsTab != null)
            {
                var img = statsTab.targetGraphic as Image;
                if (img != null) img.color = stats ? new Color(0.95f, 0.60f, 0.29f) : new Color(0.86f, 0.80f, 0.72f);
            }
            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.achievements == null) return;

            int unlocked = gm.achievements.UnlockedCount;
            int total = gm.achievements.TotalCount;
            if (titleLabel != null) titleLabel.text = showingStats ? "Statistics" : "Achievements";
            if (counterLabel != null)
                counterLabel.text = showingStats
                    ? (gm.Data != null && gm.Data.stats != null
                        ? $"Best sword: {RarityInfo.NameOf((Rarity)gm.Data.stats.bestRarity)}"
                        : "")
                    : $"{unlocked} / {total} unlocked   ·   each grants a permanent bonus";
            if (achFill != null && total > 0) achFill.fillAmount = unlocked / (float)total;

            if (showingStats && statsBody != null && gm.Data != null && gm.Data.stats != null)
                statsBody.text = StatsText(gm);

            foreach (AchRow r in rows)
                if (r != null) r.Refresh();
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
            ShowPage(stats);
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
