using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Crafting station: tracks progress, shows the hot sword, fires sparks and
    /// drives a world-space progress bar.
    /// </summary>
    public class AnvilStation : MonoBehaviour
    {
        [Tooltip("Where workers stand, one per lane")]
        public Transform[] workPoints;
        [Tooltip("Where the hot sword sits while crafting")]
        public Transform craftPoint;
        public GameObject hotSwordVisual;
        public ParticleSystem sparks;
        public WorldProgressBar progressBar;

        public bool IsCrafting { get; private set; }

        float timer;
        float duration;
        System.Action onComplete;
        Vector3 hotSwordBaseScale = Vector3.one;

        void Awake()
        {
            if (hotSwordVisual != null)
            {
                hotSwordBaseScale = hotSwordVisual.transform.localScale;
                hotSwordVisual.SetActive(false);
            }
        }

        public Vector3 GetWorkPoint(int lane)
        {
            if (workPoints == null || workPoints.Length == 0) return transform.position + Vector3.back;
            return workPoints[Mathf.Clamp(lane, 0, workPoints.Length - 1)].position;
        }

        public void BeginCraft(float craftDuration, System.Action onCraftComplete)
        {
            if (IsCrafting) { onCraftComplete?.Invoke(); return; }
            IsCrafting = true;
            duration = Mathf.Max(0.1f, craftDuration);
            timer = 0f;
            onComplete = onCraftComplete;
            if (hotSwordVisual != null)
            {
                hotSwordVisual.SetActive(true);
                hotSwordVisual.transform.localScale = Vector3.zero;
                Tween.Scale(hotSwordVisual.transform, hotSwordBaseScale, 0.3f, Ease.OutBack);
            }
        }

        void Update()
        {
            if (!IsCrafting) return;
            timer += Time.deltaTime;
            float p = Mathf.Clamp01(timer / duration);
            if (progressBar != null) progressBar.SetProgress(p);
            if (timer >= duration)
            {
                IsCrafting = false;
                if (progressBar != null) progressBar.CompleteFlash();
                onComplete?.Invoke();
                onComplete = null;
            }
        }

        /// <summary>Called by the worker's hammer animation event on each strike.</summary>
        public void OnHammerStrike()
        {
            if (sparks != null) sparks.Play();
            AudioManager.Play("hammer", 0.09f, 0.9f);
            if (hotSwordVisual != null)
                Tween.PunchScale(hotSwordVisual.transform, hotSwordBaseScale * 0.18f, 0.25f);
        }

        public GameObject TakeForgedSword(Transform hand)
        {
            if (hotSwordVisual != null) hotSwordVisual.SetActive(false);
            if (progressBar != null) progressBar.SetProgress(0f);
            GameObject prefab = GameManager.Instance != null ? GameManager.Instance.config.swordPrefab : null;
            if (prefab == null || hand == null) return null;
            GameObject sword = Instantiate(prefab, hand);
            sword.transform.localPosition = new Vector3(0f, 0.04f, 0.06f);
            sword.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            return sword;
        }
    }
}
