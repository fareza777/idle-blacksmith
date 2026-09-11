using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Title screen. It is an overlay on the live Shop scene rather than its own scene, so the
    /// forge keeps working behind it and no build settings have to change.
    /// </summary>
    public class MainMenuPanel : MonoBehaviour
    {
        [Header("Refs")]
        public CanvasGroup group;
        public Image background;
        public Image emblem;
        public RectTransform titleBlock;
        public TMP_Text titleLabel;
        public TMP_Text taglineLabel;
        public TMP_Text progressLabel;

        public BouncyButton playButton;
        public BouncyButton settingsButton;
        public BouncyButton creditsButton;

        public GameObject creditsRoot;
        public TMP_Text creditsBody;
        public Button creditsClose;

        public bool IsOpen { get; private set; }

        System.Action onPlay;

        public void Init()
        {
            gameObject.SetActive(false);
            if (creditsRoot != null) creditsRoot.SetActive(false);
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (settingsButton != null) settingsButton.onClick.AddListener(() => SettingsRequested?.Invoke());
            if (creditsButton != null) creditsButton.onClick.AddListener(ShowCredits);
            if (creditsClose != null) creditsClose.onClick.AddListener(() => { if (creditsRoot != null) creditsRoot.SetActive(false); });
            if (creditsBody != null) creditsBody.text = CreditsText();
        }

        /// <summary>Raised when the player taps SETTINGS.</summary>
        public event System.Action SettingsRequested;

        public static string CreditsText() =>
            "IDLE BLACKSMITH RPG\n" +
            "a cozy forge adventure\n\n" +
            "DESIGN, CODE & PROCEDURAL WORLD\n" +
            "CozyForge\n\n" +
            "ART\n" +
            "Menu, splash and onboarding art generated with\n" +
            "Replicate · black-forest-labs/flux-schnell\n" +
            "UI icons generated with Recraft v3\n" +
            "All 3D models, animations and the shop itself are\n" +
            "generated in code — no imported meshes.\n\n" +
            "AUDIO\n" +
            "Sound effects generated with ElevenLabs\n" +
            "(synthesised fallbacks ship in the build)\n\n" +
            "FONTS & LIBRARIES\n" +
            "Baloo 2 — SIL Open Font License\n" +
            "TextMeshPro — Unity Technologies\n" +
            "PrimeTween 1.4.11 — Apache-2.0, Kyrylo Kuzyk\n\n" +
            "Built with Unity 6000.3 · URP";

        public void Show(System.Action play)
        {
            onPlay = play;
            IsOpen = true;
            gameObject.SetActive(true);
            if (creditsRoot != null) creditsRoot.SetActive(false);

            GameManager gm = GameManager.Instance;
            if (progressLabel != null && gm != null && gm.Data != null)
            {
                StatBlock s = gm.Data.stats;
                progressLabel.text = s == null || s.swordsForged == 0
                    ? "A fresh anvil awaits"
                    : $"{GoldCounter.Format(gm.economy.Gold)} gold  ·  {s.swordsForged} swords forged"
                      + (gm.prestige != null && gm.prestige.Count > 0 ? $"  ·  rekindled {gm.prestige.Count}×" : "");
            }

            if (group != null)
            {
                group.alpha = 0f;
                Tween.Alpha(group, 1f, 0.4f, Ease.OutQuad);
            }
            if (titleBlock != null)
            {
                Vector2 p = titleBlock.anchoredPosition;
                titleBlock.anchoredPosition = p + Vector2.down * 30f;
                Tween.UIAnchoredPosition(titleBlock, p, 0.55f, Ease.OutCubic);
            }
            if (emblem != null)
            {
                emblem.transform.localScale = Vector3.one * 0.7f;
                Tween.Scale(emblem.transform, Vector3.one, 0.6f, Ease.OutBack);
            }
        }

        void Play()
        {
            AudioManager.Play("unlock");
            IsOpen = false;
            if (group != null)
                Tween.Alpha(group, 0f, 0.35f, Ease.InQuad)
                    .OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                        onPlay?.Invoke();
                    });
            else
            {
                gameObject.SetActive(false);
                onPlay?.Invoke();
            }
        }

        void ShowCredits()
        {
            AudioManager.Play("pop", 0.03f);
            if (creditsRoot != null) creditsRoot.SetActive(!creditsRoot.activeSelf);
        }

        public void PreviewOpenForScreenshot()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            if (group != null) group.alpha = 1f;
            if (titleBlock != null) titleBlock.anchoredPosition = new Vector2(0, titleBlock.anchoredPosition.y);
            if (progressLabel != null) progressLabel.text = "12.4K gold  ·  187 swords forged";
            if (creditsRoot != null) creditsRoot.SetActive(false);
        }

        public void PreviewClose()
        {
            IsOpen = false;
            gameObject.SetActive(false);
        }
    }
}
