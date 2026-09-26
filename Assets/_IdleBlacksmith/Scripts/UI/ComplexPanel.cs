using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Bottom sheet listing every building in the complex: level, effect, cost and a buy
    /// button. Building at level 0 shows as "not built" and is the cheapest way forward.
    /// </summary>
    public class ComplexPanel : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform sheet;
        public CanvasGroup backdrop;
        public Button backdropButton;
        public Button closeButton;
        public TMP_Text titleLabel;
        public TMP_Text subtitleLabel;
        public BuildingRow rowPrefab;
        public Transform rowsParent;
        public RunePanel runePanel;

        [Header("Layout")]
        public float openY = 26f;
        public float closedY = -1900f;

        public bool IsOpen { get; private set; }

        readonly System.Collections.Generic.List<BuildingRow> rows = new System.Collections.Generic.List<BuildingRow>();
        bool built;

        public void Init()
        {
            BuildRows();
            gameObject.SetActive(false);

            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.buildings != null) gm.buildings.OnBuildingChanged += (_, __) => RefreshAll();
                if (gm.economy != null) gm.economy.OnGoldChanged += (_, __) => RefreshAll();
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
            if (config == null || config.buildings == null || rowPrefab == null || rowsParent == null) return;

            foreach (BuildingDef def in config.buildings)
            {
                if (def == null) continue;
                BuildingRow row = Instantiate(rowPrefab, rowsParent);
                row.Bind(def, this);
                rows.Add(row);
            }
        }

        public void RefreshAll()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (subtitleLabel != null)
            {
                int builtCount = 0;
                if (gm.buildings != null && gm.config != null && gm.config.buildings != null)
                    foreach (BuildingDef d in gm.config.buildings)
                        if (d != null && gm.buildings.IsBuilt(d.id)) builtCount++;
                subtitleLabel.text = $"{builtCount} of {rows.Count} buildings raised — each one earns while you are away";
            }
            foreach (BuildingRow r in rows)
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
            if (runePanel != null && runePanel.IsOpen) runePanel.Close();
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

        /// <summary>Opens, then flashes the row for <paramref name="buildingId"/> so a world tap lands somewhere obvious.</summary>
        public void OpenFocused(string buildingId)
        {
            bool wasOpen = IsOpen;
            if (!wasOpen) Open();
            StartCoroutine(FlashRowNextFrame(buildingId));
        }

        System.Collections.IEnumerator FlashRowNextFrame(string buildingId)
        {
            // Wait one frame so the layout groups have positioned the rows before we look them up.
            yield return null;
            foreach (BuildingRow r in rows)
                if (r != null && r.Id == buildingId) r.FlashHighlight();
        }

        /// <summary>Editor tooling hook: fully-open state without tweens.</summary>
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
