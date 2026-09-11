using UnityEngine;

namespace IdleBlacksmith.UI
{
    /// <summary>Stretches the RectTransform to the device safe area (notch aware).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform panel;
        Rect last;

        void Awake()
        {
            panel = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != last) Apply();
        }

        void Apply()
        {
            Rect r = Screen.safeArea;
            last = r;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var min = new Vector2(r.position.x / Screen.width, r.position.y / Screen.height);
            var max = new Vector2((r.position.x + r.size.x) / Screen.width, (r.position.y + r.size.y) / Screen.height);
            panel.anchorMin = min;
            panel.anchorMax = max;
        }
    }
}
