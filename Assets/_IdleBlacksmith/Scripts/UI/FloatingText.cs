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
                // Shrink long floaters (tool names) so they stay inside the box.
                label.fontSize = text.Length > 18 ? 26f : (text.Length > 12 ? 34f : 48f);
            }
            if (rt == null) rt = transform as RectTransform;
            Vector2 p0 = anchoredPos + new Vector2(Random.Range(-28f, 28f), 0f);
            // Floaters spawned near the screen edge get nudged back inside the layer.
            if (rt.parent is RectTransform pr)
            {
                float halfW = pr.rect.width * 0.5f;
                p0.x = Mathf.Clamp(p0.x, -halfW + 170f, halfW - 170f);
            }
            rt.anchoredPosition = p0;
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
