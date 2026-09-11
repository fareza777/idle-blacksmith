using System.Collections.Generic;
using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Bottom-sheet dungeon panel: lists expeditions, shows the running one with a live
    /// progress bar, and handles claiming (gold + relic ore) and the relic-ore bonus readout.
    /// </summary>
    public class DungeonPanel : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform sheet;
        public CanvasGroup backdrop;
        public Button backdropButton;
        public Button closeButton;
        public TMP_Text relicOreLabel;
        public TMP_Text priceBonusLabel;
        public DungeonRow rowPrefab;
        public Transform rowsParent;

        [Header("Layout")]
        public float openY = 24f;
        public float closedY = -1900f;

        public bool IsOpen { get; private set; }

        readonly List<DungeonRow> rows = new List<DungeonRow>();
        bool built;

        public void Init()
        {
            BuildRows();
            gameObject.SetActive(false);
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.expeditions != null) gm.expeditions.OnChanged += RefreshAll;
                gm.OnRelicOreChanged += _ => RefreshAll();
                gm.economy.OnGoldChanged += (_, __) => RefreshAll();
            }
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
        }

        void BuildRows()
        {
            if (built) return;
            built = true;
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.config : null;
            if (config != null && config.expeditions != null && rowPrefab != null && rowsParent != null)
                foreach (ExpeditionDef def in config.expeditions)
                {
                    if (def == null) continue;
                    DungeonRow row = Instantiate(rowPrefab, rowsParent);
                    row.Bind(def);
                    rows.Add(row);
                }
        }

        void Update()
        {
            // Live countdown + claim-ready flip while the panel is open.
            if (IsOpen) RefreshAll();
        }

        public void RefreshAll()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (relicOreLabel != null) relicOreLabel.text = gm.RelicOre.ToString();
            if (priceBonusLabel != null && gm.config != null)
            {
                int pct = Mathf.RoundToInt(
                    (1f + gm.config.relicOrePriceBonus * Mathf.Min(gm.RelicOre, gm.config.relicOreMaxBonusCount) - 1f) * 100f);
                priceBonusLabel.text = pct > 0 ? $"+{pct}% sword prices" : "Boosts sword prices";
            }
            foreach (DungeonRow r in rows)
                if (r != null) r.Refresh();
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            if (IsOpen || sheet == null) return;
            IsOpen = true;
            gameObject.SetActive(true);
            RefreshAll();
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

        /// <summary>Editor tooling hook: fully-open state without tweens.</summary>
        public void PreviewOpenForScreenshot()
        {
            BuildRows();
            gameObject.SetActive(true);
            IsOpen = true;
            RefreshAll();
            if (sheet != null)
                sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, openY);
            if (backdrop != null)
            {
                backdrop.alpha = 1f;
                backdrop.blocksRaycasts = false;
                backdrop.interactable = false;
            }
        }

        public void PreviewClose()
        {
            IsOpen = false;
            if (sheet != null)
                sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, closedY);
            if (backdrop != null) backdrop.alpha = 0f;
            gameObject.SetActive(false);
        }

        public static string FormatDuration(int seconds)
        {
            if (seconds >= 3600) return (seconds / 3600f).ToString("0.#") + "h";
            if (seconds >= 60) return Mathf.RoundToInt(seconds / 60f) + " min";
            return seconds + "s";
        }
    }
}
