using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Opening cinematic: four Ken-Burns story cards under letterbox bars, each with a
    /// typewriter caption. Tap = finish current caption; tap again = next card. SKIP jumps
    /// straight to the end. Plays once for a fresh forge; the seen flag lives in the save.
    /// Bar heights and typing animate in Update so no tween features beyond fades are needed.
    /// </summary>
    public class IntroCinematic : MonoBehaviour
    {
        [System.Serializable]
        public class Frame
        {
            public Sprite art;
            [TextArea] public string caption = "";
        }

        [Header("Refs")]
        public CanvasGroup group;
        public Image artImage;
        public RectTransform barTop;
        public RectTransform barBottom;
        public TMP_Text captionLabel;
        public CanvasGroup captionGroup;
        public Button skipButton;
        public Button advanceButton;
        public TMP_Text pageLabel;
        public CanvasGroup tapHint;

        [Header("Content")]
        public Frame[] frames;

        [Header("Feel")]
        public float secondsPerFrame = 6.5f;
        public float charsPerSecond = 48f;
        public float barHeight = 150f;
        public float zoomFrom = 1.0f;
        public float zoomTo = 1.08f;
        public float barSeconds = 0.9f;

        public bool IsPlaying { get; private set; }

        int index;
        float shownChars;
        bool typing;
        float frameTimer;
        bool finishing;
        float barT;
        float barFrom;
        float barTarget;
        System.Action onDone;

        void UpdateBars()
        {
            float p = Mathf.Clamp01(barT / Mathf.Max(0.01f, barSeconds));
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            float h = Mathf.Lerp(barFrom, barTarget, eased);
            if (barTop != null) barTop.sizeDelta = new Vector2(0, h);
            if (barBottom != null) barBottom.sizeDelta = new Vector2(0, h);
        }

        void SetBarsInstant(float h)
        {
            barFrom = barTarget = h;
            barT = barSeconds;
            if (barTop != null) barTop.sizeDelta = new Vector2(0, h);
            if (barBottom != null) barBottom.sizeDelta = new Vector2(0, h);
        }

        void TweenBars(float to)
        {
            barFrom = barTop != null ? barTop.sizeDelta.y : barTarget;
            barTarget = to;
            barT = 0f;
        }

        public void Play(System.Action done)
        {
            if (frames == null || frames.Length == 0 || artImage == null)
            {
                done?.Invoke();
                return;
            }
            onDone = done;
            IsPlaying = true;
            finishing = false;
            index = 0;
            gameObject.SetActive(true);
            AudioManager.PlayMusic("music_intro", 0.9f);
            AudioManager.Play("ember_whoosh", 0.03f, 0.9f);

            if (group != null)
            {
                group.alpha = 0f;
                Tween.Alpha(group, 1f, 0.6f, Ease.OutQuad);
            }
            SetBarsInstant(0f);
            TweenBars(barHeight);

            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(Skip);
            }
            if (advanceButton != null)
            {
                advanceButton.onClick.RemoveAllListeners();
                advanceButton.onClick.AddListener(Advance);
            }
            ShowFrame(0, false);
        }

        void ShowFrame(int i, bool animate)
        {
            index = i;
            Frame f = frames[index];
            frameTimer = 0f;
            typing = false;

            artImage.sprite = f.art;
            if (animate)
            {
                artImage.color = new Color(1f, 1f, 1f, 0f);
                Tween.Alpha(artImage, 1f, 0.7f, Ease.OutQuad);
            }
            else
            {
                artImage.color = Color.white;
            }
            // slow push-in for the whole frame
            artImage.transform.localScale = Vector3.one * zoomFrom;
            Tween.Scale(artImage.transform, Vector3.one * zoomTo, secondsPerFrame + 1f, Ease.Linear);

            if (captionLabel != null)
            {
                captionLabel.text = f.caption;
                captionLabel.maxVisibleCharacters = 0;
            }
            shownChars = 0f;
            typing = true;
            if (captionGroup != null)
            {
                captionGroup.alpha = 0f;
                Tween.Alpha(captionGroup, 1f, 0.5f, Ease.OutQuad);
            }
            if (tapHint != null) tapHint.alpha = 0f;
            if (pageLabel != null) pageLabel.text = $"{index + 1} / {frames.Length}";
        }

        void Update()
        {
            barT += Time.unscaledDeltaTime;
            if (barT < barSeconds) UpdateBars();

            if (!IsPlaying || finishing) return;
            frameTimer += Time.unscaledDeltaTime;

            if (typing && captionLabel != null)
            {
                shownChars += charsPerSecond * Time.unscaledDeltaTime;
                int want = Mathf.Min(captionLabel.textInfo.characterCount, Mathf.FloorToInt(shownChars));
                captionLabel.maxVisibleCharacters = want;
                if (want % 9 == 4) AudioManager.Play("blip", 0.06f, 0.16f);
                if (want >= captionLabel.textInfo.characterCount)
                {
                    typing = false;
                    if (tapHint != null) Tween.Alpha(tapHint, 1f, 0.4f, Ease.OutQuad);
                }
            }

            // auto-advance once the caption has been fully readable for a breath
            if (!typing && frameTimer >= secondsPerFrame)
                Advance();
        }

        void Advance()
        {
            if (typing && captionLabel != null)
            {
                typing = false;
                captionLabel.maxVisibleCharacters = int.MaxValue;
                if (tapHint != null) tapHint.alpha = 1f;
                frameTimer = secondsPerFrame * 0.5f; // still leave a beat to read
                return;
            }
            if (index >= frames.Length - 1)
            {
                Finish();
                return;
            }
            AudioManager.Play("whoosh", 0.04f, 0.5f);
            ShowFrame(index + 1, true);
        }

        /// <summary>Public skip hook — UI buttons and the hardware back key both land here.</summary>
        public void SkipIntro() => Skip();

        void Skip()
        {
            AudioManager.Play("pop", 0.03f);
            Finish();
        }

        void Finish()
        {
            if (finishing) return;
            finishing = true;
            IsPlaying = false;

            GameManager gm = GameManager.Instance;
            if (gm != null && gm.Data != null)
            {
                gm.Data.introSeen = true;
                gm.Save();
            }
            AudioManager.PlayMusic("music_forge", 1.4f);
            TweenBars(0f);

            if (group != null)
                Tween.Alpha(group, 0f, 0.7f, Ease.InQuad)
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

        /// <summary>Editor preview: a mid frame fully readable, bars down.</summary>
        public void PreviewShow(int frame)
        {
            if (frames == null || frames.Length == 0) return;
            IsPlaying = true;
            finishing = false;
            index = Mathf.Clamp(frame, 0, frames.Length - 1);
            gameObject.SetActive(true);
            if (group != null) group.alpha = 1f;
            SetBarsInstant(barHeight);
            Frame f = frames[index];
            if (artImage != null)
            {
                artImage.sprite = f.art;
                artImage.color = Color.white;
                artImage.transform.localScale = Vector3.one * 1.05f;
            }
            if (captionLabel != null)
            {
                captionLabel.text = f.caption;
                captionLabel.maxVisibleCharacters = int.MaxValue;
            }
            if (captionGroup != null) captionGroup.alpha = 1f;
            if (tapHint != null) tapHint.alpha = 1f;
            if (pageLabel != null) pageLabel.text = $"{index + 1} / {frames.Length}";
            typing = false;
            frameTimer = 0f;
        }

        public void PreviewHide()
        {
            IsPlaying = false;
            gameObject.SetActive(false);
        }
    }
}
