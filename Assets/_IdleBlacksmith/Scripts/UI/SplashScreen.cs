using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Launch splash: full-screen key art + emblem + title. Holds a minimum time,
    /// then fades out and hands control back (tap skips once the minimum passed).
    /// </summary>
    public class SplashScreen : MonoBehaviour
    {
        public CanvasGroup group;
        public RectTransform emblem;
        public RectTransform titleBlock;
        public CanvasGroup tapHint;
        [Min(0.5f)] public float minTime = 1.8f;

        System.Action onDone;
        float shownAt;
        bool closing;

        public void Play(System.Action done)
        {
            onDone = done;
            shownAt = Time.unscaledTime;
            gameObject.SetActive(true);
            if (group != null) group.alpha = 1f;

            if (emblem != null)
            {
                emblem.localScale = Vector3.one * 0.6f;
                Tween.Scale(emblem, Vector3.one, 0.7f, Ease.OutBack);
            }
            if (titleBlock != null)
            {
                Vector2 p = titleBlock.anchoredPosition;
                titleBlock.anchoredPosition = p + Vector2.down * 40f;
                Tween.UIAnchoredPosition(titleBlock, p, 0.6f, Ease.OutCubic);
            }
            if (tapHint != null)
            {
                tapHint.alpha = 0f;
                Tween.Alpha(tapHint, 1f, 0.6f, Ease.OutQuad, startDelay: minTime);
            }
        }

        void Update()
        {
            if (closing) return;
            float elapsed = Time.unscaledTime - shownAt;
            bool tapped = Input.GetMouseButtonDown(0);
            if (elapsed >= minTime + 1.2f || (tapped && elapsed >= minTime))
                BeginClose();
        }

        void BeginClose()
        {
            if (closing) return;
            closing = true;
            if (group != null)
                Tween.Alpha(group, 0f, 0.55f, Ease.InQuad)
                    .OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                        onDone?.Invoke();
                    });
            else
            {
                gameObject.SetActive(false);
                onDone?.Invoke();
            }
        }
    }
}
