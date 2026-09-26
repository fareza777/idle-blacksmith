using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace IdleBlacksmith.UI
{
    /// <summary>Small HUD chip showing which dawn the forge is on. Polls the save stat —
    /// on market-fair days it switches to a festive "FAIR DAY" tag.</summary>
    public class DayChip : MonoBehaviour
    {
        public TMP_Text label;

        int shown = -1;
        bool wasFair;

        void Update()
        {
            var gm = GameManager.Instance;
            int days = (gm != null && gm.Data != null && gm.Data.stats != null)
                ? gm.Data.stats.dayCycles + 1 : 1;
            bool fair = gm != null && gm.marketFair != null && gm.marketFair.Active;
            if (days != shown || fair != wasFair)
            {
                // A new dawn ticks the chip — skip the very first frame (that's just init).
                if (shown >= 0) Tween.PunchScale(transform, Vector3.one * 0.12f, 0.35f);
                shown = days;
                wasFair = fair;
                if (label != null)
                {
                    label.text = fair ? "Day " + days + " — FAIR!" : "Day " + days;
                    label.color = fair
                        ? new Color(1f, 0.82f, 0.35f)
                        : new Color(0.85f, 0.88f, 1f);
                }
            }
        }
    }
}
