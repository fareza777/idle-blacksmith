using IdleBlacksmith.Core;
using TMPro;
using UnityEngine;

namespace IdleBlacksmith.UI
{
    /// <summary>Small HUD chip showing which dawn the forge is on. Polls the save stat.</summary>
    public class DayChip : MonoBehaviour
    {
        public TMP_Text label;

        int shown = -1;

        void Update()
        {
            var gm = GameManager.Instance;
            int days = (gm != null && gm.Data != null && gm.Data.stats != null)
                ? gm.Data.stats.dayCycles + 1 : 1;
            if (days != shown)
            {
                shown = days;
                if (label != null) label.text = "Day " + days;
            }
        }
    }
}
