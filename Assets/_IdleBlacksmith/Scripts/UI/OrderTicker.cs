using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Slim contract banner under the quest ticker: patron, swords still owed, countdown and
    /// the premium tag. Fades out entirely while no order is on the table; tapping it opens
    /// the Forge sheet so the player can swap to the recipe the patron asked for.
    /// </summary>
    public class OrderTicker : MonoBehaviour
    {
        public TMP_Text line;
        public TMP_Text timer;
        public Image fill;
        public Button openButton;
        public CanvasGroup group;

        static readonly Color Gold = new Color(1f, 0.82f, 0.42f);
        static readonly Color Urgent = new Color(1f, 0.45f, 0.38f);

        public void Init()
        {
            if (openButton != null)
                openButton.onClick.AddListener(() => UIManager.Instance?.OpenForge());
            if (GameManager.Instance != null && GameManager.Instance.orders != null)
                GameManager.Instance.orders.OnChanged += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            OrderManager orders = GameManager.Instance != null ? GameManager.Instance.orders : null;
            OrderManager.Order o = orders != null ? orders.Active : null;
            bool on = o != null;
            if (group != null)
            {
                bool wasHidden = group.alpha < 0.05f;
                group.alpha = on ? 1f : 0f;
                group.interactable = on;
                group.blocksRaycasts = on;
                if (on && wasHidden) Tween.PunchScale(transform, Vector3.one * 0.05f, 0.35f);
            }
            if (!on) return;

            GameManager gm = GameManager.Instance;
            RecipeDef recipe = gm != null && gm.config != null ? gm.config.GetRecipe(o.recipeId) : null;
            string name = recipe != null ? recipe.displayName : o.recipeId;
            bool wrongRecipe = gm != null && gm.recipes != null && gm.recipes.ActiveId != o.recipeId;
            if (line != null)
                line.text = wrongRecipe
                    ? $"{o.patron}: {o.delivered}/{o.needed} × {name}  —  switch recipe!"
                    : $"{o.patron}: {o.delivered}/{o.needed} × {name}";
        }

        void Update()
        {
            OrderManager orders = GameManager.Instance != null ? GameManager.Instance.orders : null;
            OrderManager.Order o = orders != null ? orders.Active : null;
            if (o == null) return;

            float left = o.SecondsLeft;
            if (timer != null)
            {
                int s = Mathf.CeilToInt(left);
                timer.text = $"{s / 60}:{s % 60:00}";
                timer.color = left < 30f ? Urgent : Gold;
            }
            if (fill != null)
                fill.fillAmount = Mathf.Clamp01(o.delivered / (float)o.needed);
            // The countdown drives an expiry, so refresh the label each second for free.
            if (line != null && Mathf.FloorToInt(Time.time * 2f) != Mathf.FloorToInt((Time.time - Time.deltaTime) * 2f))
                Refresh();
        }
    }
}
