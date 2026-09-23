using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// A workbench tool that does something when tapped: the bellows stoke the hearth for a
    /// burst of forge speed, the grindstone sharpens so the next blade rolls its rarity twice,
    /// and the quench trough finishes the current craft outright. Each use runs on a cooldown,
    /// so the tools are a rhythm the player returns to — not a macro.
    /// </summary>
    public class ToolStation : MonoBehaviour
    {
        public enum Kind { Bellows, Grindstone, QuenchTrough }

        public Kind kind;
        public AnvilStation anvil;

        [Tooltip("Seconds before this tool can be used again")]
        public float cooldown = 45f;

        [Tooltip("Local point the tap hitbox and floaters anchor to (tools baked into a bigger mesh)")]
        public Vector3 anchorLocal = new Vector3(0f, 0.5f, 0f);

        [Tooltip("Punch the prop's scale on use — off for tools baked into a building mesh")]
        public bool scaleOnUse = true;

        float lastUse = -999f;

        public Vector3 AnchorWorld => transform.TransformPoint(anchorLocal);

        public void Use()
        {
            float since = Time.unscaledTime - lastUse;
            if (since < cooldown)
            {
                Floater($"{Mathf.CeilToInt(cooldown - since)}s", new Color(0.80f, 0.80f, 0.86f));
                AudioManager.Play("blip", 0.05f, 0.35f);
                return;
            }

            bool fired = false;
            switch (kind)
            {
                case Kind.Bellows:
                    if (anvil != null) anvil.Stoke(1.6f, 20f);
                    Floater("STOKED! +60% forge", new Color(1f, 0.60f, 0.22f));
                    AudioManager.Play("ember_whoosh", 0.05f, 0.9f);
                    fired = true;
                    break;
                case Kind.Grindstone:
                    if (anvil != null) anvil.sharpenNext = true;
                    Floater("SHARPENED! next blade rolls twice", new Color(0.55f, 0.83f, 1f));
                    AudioManager.Play("enchant", 0.05f, 0.8f);
                    fired = true;
                    break;
                case Kind.QuenchTrough:
                    if (anvil != null && anvil.IsCrafting)
                    {
                        anvil.Quench();
                        Floater("QUENCHED!", new Color(0.50f, 0.85f, 1f));
                        AudioManager.Play("whoosh", 0.05f, 0.9f);
                        fired = true;
                    }
                    else
                    {
                        Floater("The water waits.", new Color(0.66f, 0.76f, 0.88f));
                    }
                    break;
            }

            if (!fired) return;
            lastUse = Time.unscaledTime;

            if (scaleOnUse) Tween.PunchScale(transform, Vector3.one * 0.16f, 0.35f);
        }

        void Floater(string text, Color color)
        {
            UIManager.Instance?.SpawnFloatingText(AnchorWorld + Vector3.up * 0.45f, text, color);
        }
    }
}
