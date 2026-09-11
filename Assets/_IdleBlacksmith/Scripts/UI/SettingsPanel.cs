using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Settings: sound, haptics, save reset and a way back to the title screen.
    /// The reset is a two-step confirmation because it is the only destructive action here.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform sheet;
        public CanvasGroup backdrop;
        public Button backdropButton;
        public Button closeButton;
        public TMP_Text titleLabel;
        public TMP_Text savePathLabel;

        [Header("Rows")]
        public BouncyButton muteButton;
        public TMP_Text muteLabel;
        public Slider volumeSlider;
        public TMP_Text volumeLabel;
        public BouncyButton hapticButton;
        public TMP_Text hapticLabel;
        public BouncyButton menuButton;
        public BouncyButton resetButton;
        public TMP_Text resetLabel;
        public BouncyButton creditsButton;
        public GameObject creditsRoot;
        public TMP_Text creditsBody;
        public Button creditsClose;

        [Header("Layout")]
        public float openY = 26f;
        public float closedY = -1900f;

        public bool IsOpen { get; private set; }

        public event System.Action MenuRequested;

        bool resetArmed;

        const string HapticKey = "IB_Haptics";
        const string VolumeKey = "IB_Volume";

        public static bool HapticsEnabled
        {
            get => PlayerPrefs.GetInt(HapticKey, 1) == 1;
            set => PlayerPrefs.SetInt(HapticKey, value ? 1 : 0);
        }

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            set => PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
        }

        /// <summary>Short buzz on supported devices; a no-op everywhere else.</summary>
        public static void Buzz()
        {
            if (!HapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        public void Init()
        {
            gameObject.SetActive(false);
            if (creditsRoot != null) creditsRoot.SetActive(false);

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
            if (muteButton != null) muteButton.onClick.AddListener(ToggleMute);
            if (hapticButton != null) hapticButton.onClick.AddListener(ToggleHaptics);
            if (menuButton != null) menuButton.onClick.AddListener(() => MenuRequested?.Invoke());
            if (resetButton != null) resetButton.onClick.AddListener(OnReset);
            if (creditsButton != null) creditsButton.onClick.AddListener(() => { if (creditsRoot != null) creditsRoot.SetActive(!creditsRoot.activeSelf); });
            if (creditsClose != null) creditsClose.onClick.AddListener(() => { if (creditsRoot != null) creditsRoot.SetActive(false); });
            if (creditsBody != null) creditsBody.text = MainMenuPanel.CreditsText();

            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.value = Volume;
                volumeSlider.onValueChanged.AddListener(OnVolume);
            }
            ApplyVolume();
            Refresh();
        }

        void OnVolume(float v)
        {
            Volume = v;
            ApplyVolume();
            Refresh();
        }

        static void ApplyVolume()
        {
            AudioListener.volume = AudioManager.Muted ? 0f : Volume;
        }

        void ToggleMute()
        {
            AudioManager.Muted = !AudioManager.Muted;
            ApplyVolume();
            if (!AudioManager.Muted) AudioManager.Play("pop", 0.03f);
            Refresh();
        }

        void ToggleHaptics()
        {
            HapticsEnabled = !HapticsEnabled;
            Buzz();
            Refresh();
        }

        void OnReset()
        {
            if (!resetArmed)
            {
                resetArmed = true;
                AudioManager.Play("denied");
                Refresh();
                return;
            }

            AudioManager.Play("fanfare");
            SaveSystem.DeleteSave();
            PlayerPrefs.Save();
            resetArmed = false;
            Refresh();
        }

        public void Refresh()
        {
            if (muteLabel != null) muteLabel.text = AudioManager.Muted ? "OFF" : "ON";
            if (volumeLabel != null) volumeLabel.text = Mathf.RoundToInt(Volume * 100f) + "%";
            if (hapticLabel != null) hapticLabel.text = HapticsEnabled ? "ON" : "OFF";
            if (resetLabel != null)
                resetLabel.text = resetArmed ? "TAP AGAIN TO ERASE" : "RESET SAVE";
            if (resetButton != null)
            {
                var img = resetButton.targetGraphic as Image;
                if (img != null) img.color = resetArmed ? new Color(0.89f, 0.36f, 0.31f) : new Color(0.80f, 0.62f, 0.55f);
            }
            if (savePathLabel != null) savePathLabel.text = "Save file: " + SaveSystem.PathForLog;
        }

        public void Open()
        {
            if (IsOpen || sheet == null) return;
            IsOpen = true;
            resetArmed = false;
            gameObject.SetActive(true);
            Refresh();
            float x = sheet.anchoredPosition.x;
            sheet.anchoredPosition = new Vector2(x, closedY);
            Tween.UIAnchoredPosition(sheet, new Vector2(x, openY), 0.45f, Ease.OutBack);
            if (backdrop != null)
            {
                backdrop.alpha = 0f;
                Tween.Alpha(backdrop, 1f, 0.3f, Ease.OutQuad);
                backdrop.blocksRaycasts = true;
                backdrop.interactable = true;
            }
            AudioManager.Play("pop", 0.03f);
        }

        public void Close()
        {
            if (!IsOpen || sheet == null) return;
            IsOpen = false;
            resetArmed = false;
            if (creditsRoot != null) creditsRoot.SetActive(false);
            if (backdrop != null)
            {
                backdrop.blocksRaycasts = false;
                backdrop.interactable = false;
                Tween.Alpha(backdrop, 0f, 0.25f, Ease.InQuad);
            }
            float x = sheet.anchoredPosition.x;
            Tween.UIAnchoredPosition(sheet, new Vector2(x, closedY), 0.3f, Ease.InBack)
                .OnComplete(() => { if (!IsOpen) gameObject.SetActive(false); });
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void PreviewOpenForScreenshot()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            Refresh();
            if (sheet != null) sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, openY);
            if (backdrop != null) backdrop.alpha = 1f;
        }

        public void PreviewClose()
        {
            IsOpen = false;
            if (sheet != null) sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, closedY);
            if (backdrop != null) backdrop.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
