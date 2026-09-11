using PrimeTween;
using TMPro;
using UnityEngine;

namespace IdleBlacksmith.UI
{
    /// <summary>Pooled "+10" style popup that rises and fades.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FloatingText : MonoBehaviour
    {
        public TMP_Text label;

        CanvasGroup cg;
        RectTransform rt;
        System.Action<FloatingText> onDone;

        void Awake()
        {
            rt = transform as RectTransform;
            cg = GetComponent<CanvasGroup>();
        }

        public void Play(Vector2 anchoredPos, string text, Color color, System.Action<FloatingText> onFinished)
        {
            onDone = onFinished;
            gameObject.SetActive(true);
            if (label != null)
            {
                label.text = text;
                label.color = color;
            }
            if (rt == null) rt = transform as RectTransform;
            rt.anchoredPosition = anchoredPos + new Vector2(Random.Range(-28f, 28f), 0f);
            cg.alpha = 1f;
            transform.localScale = Vector3.one * 0.4f;

            Tween.Scale(transform, 1.1f, 0.22f, Ease.OutBack);
            float y0 = rt.anchoredPosition.y;
            Tween.Custom(0f, 95f, 0.95f, v =>
            {
                if (rt == null) return;
                Vector2 p = rt.anchoredPosition;
                p.y = y0 + v;
                rt.anchoredPosition = p;
            }, Ease.OutQuad);
            Tween.Delay(0.6f, () => { if (cg != null) Tween.Alpha(cg, 0f, 0.35f, Ease.InQuad); });
            Tween.Delay(0.98f, () =>
            {
                gameObject.SetActive(false);
                onDone?.Invoke(this);
            });
        }
    }
}
