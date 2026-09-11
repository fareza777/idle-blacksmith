using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>World-space "3/6" stock indicator above the sword rack.</summary>
    public class RackStockBar : MonoBehaviour
    {
        public TMP_Text label;
        public Image fill;
        public RectTransform root;

        int lastStock = -1;

        public void SetStock(int stock, int capacity)
        {
            if (label != null) label.text = stock + "/" + capacity;
            if (fill != null && capacity > 0) fill.fillAmount = (float)stock / capacity;
            if (lastStock >= 0 && stock != lastStock && root != null)
                Tween.PunchScale(root, Vector3.one * 0.16f, 0.3f);
            lastStock = stock;
        }
    }
}
