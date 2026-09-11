using System.Collections.Generic;
using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// First-launch onboarding: three illustrated pages (forge, counter, dungeon) with
    /// page dots, Next / Skip. Shown once; completion is persisted in the save file.
    /// </summary>
    public class OnboardingPanel : MonoBehaviour
    {
        [System.Serializable]
        public class Page
        {
            public Sprite art;
            public string title;
            [TextArea] public string body;
        }

        [Header("Refs")]
        public CanvasGroup group;
        public RectTransform card;
        public Image artImage;
        public AspectRatioFitter artFitter;
        public TMP_Text titleLabel;
        public TMP_Text bodyLabel;
        public Image[] dots;
        public BouncyButton nextButton;
        public TMP_Text nextLabel;
        public Button skipButton;

        [Header("Content (filled from GameConfig menu art at build time)")]
        public Page[] pages;

        static readonly Color DotOn = new Color(0.95f, 0.60f, 0.29f);
        static readonly Color DotOff = new Color(0.66f, 0.55f, 0.42f, 0.5f);

        int index;

        public void Show()
        {
            if (pages == null || pages.Length == 0)
            {
                Finish();
                return;
            }
            index = 0;
            gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = 0f;
                Tween.Alpha(group, 1f, 0.35f, Ease.OutQuad);
            }
            ApplyPage(false);
            if (nextButton != null) nextButton.onClick.AddListener(Next);
            if (skipButton != null) skipButton.onClick.AddListener(Finish);
        }

        /// <summary>Swaps the page art and re-fits it to the new sprite's aspect ratio.</summary>
        void SetArt(Sprite sprite)
        {
            if (artImage == null) return;
            artImage.sprite = sprite;
            if (artFitter != null)
                artFitter.aspectRatio = (sprite != null && sprite.rect.height > 0.01f)
                    ? sprite.rect.width / sprite.rect.height
                    : 1.5f;
        }

        void ApplyPage(bool animate)
        {
            Page p = pages[index];
            SetArt(p.art);
            if (titleLabel != null) titleLabel.text = p.title;
            if (bodyLabel != null) bodyLabel.text = p.body;
            if (nextLabel != null) nextLabel.text = index >= pages.Length - 1 ? "START FORGING!" : "NEXT";
            if (dots != null)
                for (int i = 0; i < dots.Length; i++)
                    if (dots[i] != null)
                        dots[i].color = i == index ? DotOn : DotOff;
            if (animate && card != null)
                Tween.PunchScale(card, Vector3.one * 0.04f, 0.35f);
            AudioManager.Play("pop", 0.03f);
        }

        void Next()
        {
            if (index >= pages.Length - 1)
            {
                Finish();
                return;
            }
            index++;
            ApplyPage(true);
        }

        void Finish()
        {
            GameManager.Instance?.MarkOnboardingSeen();
            if (group != null)
                Tween.Alpha(group, 0f, 0.3f, Ease.InQuad)
                    .OnComplete(() => gameObject.SetActive(false));
            else
                gameObject.SetActive(false);
        }

        /// <summary>Editor tooling hook: first page, fully visible, no tweens.</summary>
        public void PreviewShow()
        {
            if (pages == null || pages.Length == 0) return;
            index = 0;
            gameObject.SetActive(true);
            if (group != null) group.alpha = 1f;
            Page p = pages[0];
            SetArt(p.art);
            if (titleLabel != null) titleLabel.text = p.title;
            if (bodyLabel != null) bodyLabel.text = p.body;
            if (nextLabel != null) nextLabel.text = "NEXT";
            if (dots != null)
                for (int i = 0; i < dots.Length; i++)
                    if (dots[i] != null)
                        dots[i].color = i == 0 ? DotOn : DotOff;
        }

        public void PreviewHide()
        {
            gameObject.SetActive(false);
            if (group != null) group.alpha = 0f;
        }
    }
}
