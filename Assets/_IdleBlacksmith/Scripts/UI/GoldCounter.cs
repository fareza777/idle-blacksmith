using PrimeTween;
using TMPro;
using UnityEngine;

namespace IdleBlacksmith.UI
{
    /// <summary>Gold pill with animated counting and a satisfying punch on income.</summary>
    public class GoldCounter : MonoBehaviour
    {
        public TMP_Text label;
        public RectTransform coinIcon;

        int shown = -1;
        Tween countTween;

        public void SetValueInstant(int v)
        {
            shown = v;
            Apply();
        }

        public void SetValue(int v)
        {
            if (shown < 0) { SetValueInstant(v); return; }
            if (countTween.isAlive) countTween.Stop();
            countTween = Tween.Custom(shown, v, 0.45f, x =>
            {
                shown = Mathf.RoundToInt(x);
                Apply();
            }, Ease.OutQuad);
        }

        void Apply()
        {
            if (label != null) label.text = Format(shown);
        }

        public void Punch()
        {
            Tween.PunchScale(transform, Vector3.one * 0.16f, 0.3f);
            if (coinIcon != null) Tween.PunchScale(coinIcon, Vector3.one * 0.35f, 0.35f);
        }

        public static string Format(int v)
        {
            if (v < 10000) return v.ToString("N0");
            if (v < 1000000) return (v / 1000f).ToString("0.#") + "K";
            return (v / 1000000f).ToString("0.#") + "M";
        }
    }
}
