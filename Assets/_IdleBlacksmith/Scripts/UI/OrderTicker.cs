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

        [Tooltip("Seconds left at which the banner starts calling attention to the deadline")]
        public float urgentAt = 30f;

        OrderManager.Order lastOrder;
        bool urgentArmed;
        bool wasWrong;
        float nextPulse;
        float nextTick;

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
            if (!on) { wasWrong = false; return; }

            GameManager gm = GameManager.Instance;
            RecipeDef recipe = gm != null && gm.config != null ? gm.config.GetRecipe(o.recipeId) : null;
            string name = recipe != null ? recipe.displayName : o.recipeId;
            bool wrongRecipe = gm != null && gm.recipes != null && gm.recipes.ActiveId != o.recipeId;
            // First frame the banner calls out a wrong recipe gets a blip — after that the
            // text is loud enough on its own.
            if (wrongRecipe && !wasWrong) AudioManager.Play("blip", 0.05f, 0.45f);
            wasWrong = wrongRecipe;
            if (line != null)
                line.text = wrongRecipe
                    ? $"{o.patron}: {o.delivered}/{o.needed} × {name}  —  switch recipe!"
                    : $"{o.patron}: {o.delivered}/{o.needed} × {name}";
        }

        void Update()
        {
            OrderManager orders = GameManager.Instance != null ? GameManager.Instance.orders : null;
            OrderManager.Order o = orders != null ? orders.Active : null;
            if (o == null) { lastOrder = null; return; }
            if (!ReferenceEquals(o, lastOrder))
            {
                lastOrder = o;
                urgentArmed = true;
                nextPulse = 0f;
                nextTick = 0f;
            }

            float left = o.SecondsLeft;
            if (timer != null)
            {
                int s = Mathf.CeilToInt(left);
                timer.text = $"{s / 60}:{s % 60:00}";
                timer.color = left < urgentAt ? Urgent : Gold;
            }
            // Final stretch: the banner breathes and ticks so a missed contract is never silent.
            if (left > 0f && left <= urgentAt)
            {
                if (urgentArmed)
                {
                    urgentArmed = false;
                    SettingsPanel.Buzz();
                    AudioManager.Play("blip", 0.05f, 0.5f);
                }
                if (Time.time >= nextPulse)
                {
                    Tween.PunchScale(transform, Vector3.one * 0.035f, 0.45f);
                    nextPulse = Time.time + 1f;
                }
                if (Time.time >= nextTick)
                {
                    AudioManager.Play("blip", 0.12f, 0.3f);
                    nextTick = Time.time + 5f;
                }
            }
            if (fill != null)
                fill.fillAmount = Mathf.Clamp01(o.delivered / (float)o.needed);
            // The countdown drives an expiry, so refresh the label each second for free.
            if (line != null && Mathf.FloorToInt(Time.time * 2f) != Mathf.FloorToInt((Time.time - Time.deltaTime) * 2f))
                Refresh();
        }
    }
}
