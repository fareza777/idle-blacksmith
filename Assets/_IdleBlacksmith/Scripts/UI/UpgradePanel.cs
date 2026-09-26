using System.Collections.Generic;
using IdleBlacksmith.Core;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>Bottom-sheet upgrade shop with slide/bounce transitions.</summary>
    public class UpgradePanel : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform sheet;
        public CanvasGroup backdrop;
        public UpgradeRow rowPrefab;
        public Transform rowsParent;
        public HelperCard helperCard;
        public ExpandCard expandCard;
        public Button backdropButton;
        public Button closeButton;

        [Header("Layout")]
        public float openY = 40f;
        public float closedY = -1500f;

        public bool IsOpen { get; private set; }

        readonly List<UpgradeRow> rows = new List<UpgradeRow>();
        bool built;

        public void Init()
        {
            BuildRows();
            gameObject.SetActive(false);

            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                gm.economy.OnGoldChanged += (_, __) => RefreshRows();
                gm.upgrades.OnUpgradeChanged += (_, __) => RefreshRows();
                gm.OnHelperUnlockedChanged += _ => RefreshRows();
            }
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
        }

        void BuildRows()
        {
            if (built) return;
            built = true;
            GameConfig config = GameManager.Instance.config;
            if (config.upgrades != null && rowPrefab != null && rowsParent != null)
            {
                foreach (UpgradeDef def in config.upgrades)
                {
                    if (def == null) continue;
                    UpgradeRow row = Instantiate(rowPrefab, rowsParent);
                    row.Bind(def);
                    rows.Add(row);
                }
            }
            if (helperCard != null) helperCard.Bind();
            if (expandCard != null) expandCard.Bind();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen || sheet == null) return;
            IsOpen = true;
            gameObject.SetActive(true);
            RefreshRows();

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
            AudioManager.Play("whoosh", 0.04f, 0.45f);
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

        public void RefreshRows()
        {
            if (!built) return;
            foreach (UpgradeRow r in rows)
                if (r != null) r.Refresh();
            if (helperCard != null) helperCard.Refresh();
            if (expandCard != null) expandCard.Refresh();
        }

        /// <summary>Editor tooling hook: show the fully-open state without tweens.</summary>
        public void PreviewOpenForScreenshot()
        {
            BuildRows();
            gameObject.SetActive(true);
            IsOpen = true;
            RefreshRows();
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
    }
}
