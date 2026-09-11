using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// The Sanctum's rune sheet. Runes are bought with relic ore, which is what finally gives
    /// the dungeon currency somewhere to go once the price bonus is capped.
    /// </summary>
    public class RunePanel : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform sheet;
        public CanvasGroup backdrop;
        public Button backdropButton;
        public Button closeButton;
        public TMP_Text relicLabel;
        public TMP_Text capLabel;
        public RuneRow rowPrefab;
        public Transform rowsParent;

        [Header("Layout")]
        public float openY = 26f;
        public float closedY = -1900f;

        public bool IsOpen { get; private set; }

        readonly System.Collections.Generic.List<RuneRow> rows = new System.Collections.Generic.List<RuneRow>();
        bool built;

        public void Init()
        {
            BuildRows();
            gameObject.SetActive(false);
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.runes != null) gm.runes.OnChanged += RefreshAll;
                gm.OnRelicOreChanged += _ => RefreshAll();
            }
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
        }

        void BuildRows()
        {
            if (built) return;
            built = true;
            GameManager gm = GameManager.Instance;
            GameConfig config = gm != null ? gm.config : null;
            if (config == null || config.runes == null || rowPrefab == null || rowsParent == null) return;

            foreach (RuneDef def in config.runes)
            {
                if (def == null) continue;
                RuneRow row = Instantiate(rowPrefab, rowsParent);
                row.Bind(def);
                rows.Add(row);
            }
        }

        public void RefreshAll()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (relicLabel != null) relicLabel.text = gm.RelicOre.ToString();
            if (capLabel != null && gm.runes != null)
                capLabel.text = $"Max rune level {gm.runes.MaxLevel}";
            foreach (RuneRow r in rows)
                if (r != null) r.Refresh();
        }

        public void Open()
        {
            if (IsOpen || sheet == null) return;
            IsOpen = true;
            gameObject.SetActive(true);
            RefreshAll();
            float x = sheet.anchoredPosition.x;
            sheet.anchoredPosition = new Vector2(x, closedY);
            Tween.UIAnchoredPosition(sheet, new Vector2(x, openY), 0.4f, Ease.OutBack);
            if (backdrop != null)
            {
                backdrop.alpha = 0f;
                Tween.Alpha(backdrop, 1f, 0.28f, Ease.OutQuad);
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
                Tween.Alpha(backdrop, 0f, 0.22f, Ease.InQuad);
            }
            float x = sheet.anchoredPosition.x;
            Tween.UIAnchoredPosition(sheet, new Vector2(x, closedY), 0.28f, Ease.InBack)
                .OnComplete(() => { if (!IsOpen) gameObject.SetActive(false); });
        }

        public void PreviewOpenForScreenshot()
        {
            BuildRows();
            gameObject.SetActive(true);
            IsOpen = true;
            RefreshAll();
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
