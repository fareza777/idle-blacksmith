using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// The Forge sheet: pick which recipe the smith hammers out next. Locked recipes show
    /// exactly which building level opens them, so the upgrade path is never a mystery.
    /// </summary>
    public class ForgePanel : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform sheet;
        public CanvasGroup backdrop;
        public Button backdropButton;
        public Button closeButton;
        public RecipeCard cardPrefab;
        public Transform cardsParent;
        public TMP_Text hintLabel;
        public TMP_Text oreLabel;

        [Header("Layout")]
        public float openY = 26f;
        public float closedY = -1900f;

        public bool IsOpen { get; private set; }

        readonly System.Collections.Generic.List<RecipeCard> cards = new System.Collections.Generic.List<RecipeCard>();
        bool built;
        float nextPoll;

        /// <summary>Counts on the cards tick over while the sheet sits open.</summary>
        void Update()
        {
            if (IsOpen && Time.unscaledTime >= nextPoll)
            {
                nextPoll = Time.unscaledTime + 1.5f;
                RefreshAll();
            }
        }

        public void Init()
        {
            BuildCards();
            gameObject.SetActive(false);
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.recipes != null) gm.recipes.OnChanged += RefreshAll;
                if (gm.resources != null) gm.resources.OnOreChanged += (_, __) => RefreshOre();
            }
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
        }

        void BuildCards()
        {
            if (built) return;
            built = true;
            GameManager gm = GameManager.Instance;
            GameConfig config = gm != null ? gm.config : null;
            if (config == null || config.recipes == null || cardPrefab == null || cardsParent == null) return;

            foreach (RecipeDef def in config.recipes)
            {
                if (def == null) continue;
                RecipeCard card = Instantiate(cardPrefab, cardsParent);
                card.Bind(def);
                cards.Add(card);
            }
        }

        public void RefreshAll()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.recipes == null) return;
            RefreshOre();
            if (hintLabel != null) hintLabel.text = gm.recipes.NextUnlockHint();
            foreach (RecipeCard c in cards)
                if (c != null) c.Refresh();
        }

        void RefreshOre()
        {
            GameManager gm = GameManager.Instance;
            if (oreLabel != null && gm != null && gm.resources != null)
                oreLabel.text = $"{gm.resources.Ore} / {gm.resources.OreCapacity}";
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

        public void PreviewOpenForScreenshot()
        {
            BuildCards();
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
