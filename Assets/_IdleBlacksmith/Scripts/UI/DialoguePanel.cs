using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// One beat of story: who is speaking, their portrait sprite, and the line itself.
    /// Portrait names resolve to Art/Menu/portrait_*.png sprites, filled at build time.
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        public string speaker = "";
        public Sprite portrait;
        [TextArea] public string text = "";
    }

    /// <summary>
    /// A named exchange — a short stack of lines with a stable save id so it plays once.
    /// </summary>
    [System.Serializable]
    public class DialogueSequence
    {
        public string id = "";
        public DialogueLine[] lines;
    }

    /// <summary>
    /// Bottom-anchored story card: portrait medallion, speaker name plate and a
    /// typewriter body. Tap anywhere on the card to advance; fast taps finish the
    /// line instantly, a second tap moves on.
    /// </summary>
    public class DialoguePanel : MonoBehaviour
    {
        [Header("Refs")]
        public CanvasGroup group;
        public RectTransform card;
        public Image portraitImage;
        public TMP_Text nameLabel;
        public TMP_Text bodyLabel;
        public Button cardButton;
        public CanvasGroup nextHint;
        public TMP_Text pageLabel;

        [Header("Feel")]
        public float charsPerSecond = 55f;
        public float openY = 0f;
        public float closedY = -560f;

        public bool IsOpen { get; private set; }

        DialogueSequence sequence;
        int lineIndex;
        int lineLength;
        float shownChars;
        bool typing;
        System.Action onComplete;

        public void Play(DialogueSequence seq, System.Action done)
        {
            if (seq == null || seq.lines == null || seq.lines.Length == 0 || card == null)
            {
                done?.Invoke();
                return;
            }
            sequence = seq;
            onComplete = done;
            IsOpen = true;
            lineIndex = 0;
            gameObject.SetActive(true);
            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(Advance);
            }
            if (group != null)
            {
                group.alpha = 0f;
                Tween.Alpha(group, 1f, 0.3f, Ease.OutQuad);
            }
            Vector2 p = card.anchoredPosition;
            card.anchoredPosition = new Vector2(p.x, closedY);
            Tween.UIAnchoredPosition(card, new Vector2(p.x, openY), 0.45f, Ease.OutBack);
            AudioManager.Play("pop", 0.03f);
            ShowLine();
        }

        void ShowLine()
        {
            DialogueLine line = sequence.lines[lineIndex];
            if (nameLabel != null) nameLabel.text = line.speaker;
            if (portraitImage != null && line.portrait != null)
            {
                portraitImage.sprite = line.portrait;
                portraitImage.enabled = true;
                Tween.PunchScale(portraitImage.transform, Vector3.one * 0.05f, 0.3f);
            }
            shownChars = 0f;
            typing = true;
            lineLength = line.text?.Length ?? 0;
            if (bodyLabel != null)
            {
                bodyLabel.text = line.text;
                bodyLabel.maxVisibleCharacters = 0;
            }
            if (nextHint != null) nextHint.alpha = 0f;
            if (pageLabel != null)
                pageLabel.text = $"{lineIndex + 1}/{sequence.lines.Length}";
        }

        void Update()
        {
            if (!typing || bodyLabel == null) return;
            shownChars += charsPerSecond * Time.unscaledDeltaTime;
            int want = Mathf.Min(lineLength, Mathf.FloorToInt(shownChars));
            bodyLabel.maxVisibleCharacters = want;
            // a soft tick every few letters sells the typewriter without machine-gun blips
            if (want > 0 && want % 7 == 3) AudioManager.Play("blip", 0.05f, 0.22f);
            if (want >= lineLength)
            {
                typing = false;
                if (nextHint != null)
                    Tween.Alpha(nextHint, 1f, 0.3f, Ease.OutQuad);
            }
        }

        void Advance()
        {
            AudioManager.Play("pop", 0.03f);
            if (typing)
            {
                typing = false;
                if (bodyLabel != null)
                    bodyLabel.maxVisibleCharacters = int.MaxValue;
                if (nextHint != null) nextHint.alpha = 1f;
                return;
            }
            lineIndex++;
            if (lineIndex >= sequence.lines.Length)
            {
                Finish();
                return;
            }
            ShowLine();
        }

        void Finish()
        {
            IsOpen = false;
            Vector2 p = card.anchoredPosition;
            Tween.UIAnchoredPosition(card, new Vector2(p.x, closedY), 0.3f, Ease.InBack);
            if (group != null)
                Tween.Alpha(group, 0f, 0.3f, Ease.InQuad)
                    .OnComplete(() => gameObject.SetActive(false));
            else
                gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        /// <summary>Editor preview: first line fully typed, panel pinned open.</summary>
        public void PreviewShow(DialogueSequence seq)
        {
            if (seq == null || seq.lines == null || seq.lines.Length == 0) return;
            sequence = seq;
            IsOpen = true;
            lineIndex = 0;
            typing = false;
            gameObject.SetActive(true);
            if (group != null) group.alpha = 1f;
            if (card != null) card.anchoredPosition = new Vector2(card.anchoredPosition.x, openY);
            DialogueLine line = seq.lines[0];
            if (nameLabel != null) nameLabel.text = line.speaker;
            if (portraitImage != null) portraitImage.sprite = line.portrait;
            if (bodyLabel != null)
            {
                bodyLabel.text = line.text;
                bodyLabel.maxVisibleCharacters = int.MaxValue;
            }
            if (nextHint != null) nextHint.alpha = 1f;
            if (pageLabel != null) pageLabel.text = "1/" + seq.lines.Length;
        }

        public void PreviewHide()
        {
            IsOpen = false;
            gameObject.SetActive(false);
        }
    }
}
