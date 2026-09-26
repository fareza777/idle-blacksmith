using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Blazing countdown pill that slams in while rush hour runs. Sits under the order
    /// banner and breathes a little pulse so it reads as a live event, not a label.
    /// </summary>
    public class RushBanner : MonoBehaviour
    {
        public TMP_Text label;
        public TMP_Text timer;
        public CanvasGroup group;
        public Image bg;

        static readonly Color Hot = new Color(1f, 0.45f, 0.16f);

        public void Init()
        {
            if (GameManager.Instance != null && GameManager.Instance.rush != null)
                GameManager.Instance.rush.OnChanged += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            RushHourManager rush = GameManager.Instance != null ? GameManager.Instance.rush : null;
            bool on = rush != null && rush.Active;
            if (group != null)
            {
                bool wasHidden = group.alpha < 0.05f;
                group.alpha = on ? 1f : 0f;
                group.blocksRaycasts = false;
                if (on && wasHidden)
                {
                    Tween.Scale(transform, Vector3.one * 0.6f, Vector3.one, 0.35f, Ease.OutBack);
                    AudioManager.Play("fanfare", 0.06f, 0.5f);
                }
            }
            if (on && label != null)
                label.text = "RUSH HOUR  ·  +25% PRICES";
        }

        void Update()
        {
            RushHourManager rush = GameManager.Instance != null ? GameManager.Instance.rush : null;
            if (rush == null || !rush.Active) return;

            float left = rush.TimeLeft;
            if (timer != null)
                timer.text = $"0:{Mathf.CeilToInt(left):00}";
            // Gentle 2 Hz heartbeat while the surge is on.
            if (bg != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12.57f);
                bg.color = Color.Lerp(Hot * 0.85f, Hot, pulse);
            }
        }
    }
}
