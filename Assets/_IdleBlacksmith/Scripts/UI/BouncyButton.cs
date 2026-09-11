using IdleBlacksmith.Core;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Button with juicy press/release scale feedback and a click sound.
    /// Uses ColorTint transition for state colors; scale is handled here.
    /// </summary>
    [AddComponentMenu("UI/Bouncy Button")]
    public class BouncyButton : Button
    {
        [Range(0.7f, 1f)] public float pressScale = 0.9f;
        public string clickSfx = "pop";

        bool pressed;

        protected override void Awake()
        {
            base.Awake();
            transition = Transition.ColorTint;
            onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(clickSfx)) AudioManager.Play(clickSfx, 0.03f, 0.8f);
            });
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (!interactable) return;
            pressed = true;
            Tween.Scale(transform, pressScale, 0.08f, Ease.OutQuad);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (!pressed) return;
            pressed = false;
            Tween.Scale(transform, Vector3.one, 0.3f, Ease.OutBack);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            if (interactable) Tween.PunchScale(transform, Vector3.one * 0.1f, 0.25f);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            pressed = false;
            transform.localScale = Vector3.one;
        }
    }
}
