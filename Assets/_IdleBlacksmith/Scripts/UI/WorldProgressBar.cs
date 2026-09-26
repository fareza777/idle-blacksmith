using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>World-space fill bar (used above the anvil while crafting).</summary>
    public class WorldProgressBar : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        public Image fill;
        public RectTransform root;

        /// <summary>Name of what's on the anvil — fades in and out with the bar.</summary>
        public TMPro.TMP_Text label;

        bool visible;

        void Awake()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        public void SetProgress(float p)
        {
            SetVisible(p > 0.001f && p < 0.999f);
            if (fill != null) fill.fillAmount = Mathf.Clamp01(p);
        }

        public void CompleteFlash()
        {
            SetVisible(false);
            if (root != null) Tween.PunchScale(root, Vector3.one * 0.22f, 0.3f);
        }

        void SetVisible(bool v)
        {
            if (visible == v || canvasGroup == null) return;
            visible = v;
            Tween.Alpha(canvasGroup, v ? 1f : 0f, 0.15f);
        }
    }
}
