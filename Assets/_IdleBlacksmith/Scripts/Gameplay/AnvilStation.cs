using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Crafting station: tracks progress, shows the hot sword, fires sparks and drives a
    /// world-space progress bar. The rarity of the sword is rolled the moment the craft
    /// completes, so the same anvil can turn out anything from a Common to a Legendary.
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

        /// <summary>The item produced by the craft that just finished.</summary>
        public SwordItem LastForged { get; private set; }

        [Tooltip("Craft seconds granted by one manual anvil tap")]
        public float tapBoostSeconds = 0.6f;
        [Tooltip("Minimum gap between manual taps, so hammer spam stays readable")]
        public float tapCooldown = 0.12f;

        [Header("Tap combo")]
        [Tooltip("Seconds allowed between taps before the streak resets")]
        public float comboWindow = 1.5f;
        [Tooltip("Maximum tempo level reached by chained taps")]
        public int comboMax = 5;
        [Tooltip("Extra craft seconds granted per tempo level")]
        public float comboBonusPerLevel = 0.15f;

        float nextTapAllowed;
        int tapCombo;
        float comboEndsAt;

        float timer;
        float duration;
        System.Action onComplete;
        Vector3 hotSwordBaseScale = Vector3.one;
        int hammerStrikes;

        [Header("Tool buffs")]
        [Tooltip("Set by the grindstone — the next blade rolls its rarity twice and keeps the best")]
        public bool sharpenNext;
        float speedBoost = 1f;
        float boostUntil;

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

        public void BeginCraft(float craftDuration, RecipeDef recipe, System.Action onCraftComplete)
        {
            if (IsCrafting) { onCraftComplete?.Invoke(); return; }
            IsCrafting = true;
            duration = Mathf.Max(0.1f, craftDuration);
            timer = 0f;
            hammerStrikes = 0;
            onComplete = onCraftComplete;
            LastForged = null;
            pendingRecipe = recipe;
            if (progressBar != null && progressBar.label != null)
                progressBar.label.text = recipe != null ? recipe.displayName : "";

            if (hotSwordVisual != null)
            {
                SwapHotSwordMesh(recipe);
                hotSwordVisual.SetActive(true);
                hotSwordVisual.transform.localScale = Vector3.zero;
                Tween.Scale(hotSwordVisual.transform, hotSwordBaseScale, 0.3f, Ease.OutBack);
            }
        }

        /// <summary>The glowing blank on the anvil takes the active recipe's blade shape.</summary>
        void SwapHotSwordMesh(RecipeDef recipe)
        {
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.config : null;
            GameObject prefab = recipe != null && recipe.swordPrefab != null
                ? recipe.swordPrefab
                : (config != null ? config.swordPrefab : null);
            if (prefab == null) return;
            var blade = prefab.transform.Find("Blade");
            var src = blade != null ? blade.GetComponent<MeshFilter>() : null;
            var dst = hotSwordVisual.GetComponent<MeshFilter>();
            if (src != null && dst != null && src.sharedMesh != null)
                dst.sharedMesh = src.sharedMesh;
        }

        RecipeDef pendingRecipe;

        /// <summary>Bellows: the hearth roars and the current craft runs faster for a while.</summary>
        public void Stoke(float multiplier, float seconds)
        {
            speedBoost = Mathf.Max(1f, multiplier);
            boostUntil = Time.unscaledTime + seconds;
        }

        /// <summary>Quench trough: the blade hits the water and the craft completes now.</summary>
        public void Quench()
        {
            if (IsCrafting) timer = duration;
        }

        void Update()
        {
            if (!IsCrafting) return;
            timer += Time.deltaTime * (Time.unscaledTime < boostUntil ? speedBoost : 1f);
            float p = Mathf.Clamp01(timer / duration);
            if (progressBar != null) progressBar.SetProgress(p);
            if (timer >= duration)
            {
                IsCrafting = false;
                LastForged = RollForged();
                if (progressBar != null) progressBar.CompleteFlash();
                CelebrateRarity(LastForged.rarity);
                onComplete?.Invoke();
                onComplete = null;
            }
        }

        SwordItem RollForged()
        {
            GameManager gm = GameManager.Instance;
            string recipeId = pendingRecipe != null ? pendingRecipe.id : RecipeId.Copper;
            Rarity rarity = gm != null && gm.recipes != null ? gm.recipes.RollRarity() : Rarity.Common;
            if (sharpenNext)
            {
                sharpenNext = false;
                Rarity second = gm != null && gm.recipes != null ? gm.recipes.RollRarity() : Rarity.Common;
                if (second > rarity) rarity = second;
            }
            return new SwordItem(recipeId, rarity);
        }

        /// <summary>
        /// Rare-and-up rolls announce themselves: a rarity-coloured banner floats off the anvil,
        /// Epic and Legendary get their own fanfares, and Legendary briefly flashes the screen.
        /// Without this the only rarity tell was the gem tint on the finished blade.
        /// </summary>
        void CelebrateRarity(Rarity rarity)
        {
            if (rarity < Rarity.Rare) return;
            Vector3 where = craftPoint != null ? craftPoint.position : transform.position + Vector3.up;
            bool legendary = rarity >= Rarity.Legendary;
            UI.UIManager.Instance?.SpawnFloatingText(
                where + Vector3.up * 0.7f,
                RarityInfo.NameOf(rarity).ToUpperInvariant() + "!  x" + RarityInfo.MultiplierOf(rarity).ToString("0.#"),
                RarityInfo.TextColor(rarity));
            AudioManager.Play(legendary ? "fanfare" : "levelup", 0.05f, legendary ? 0.9f : 0.55f);
            if (legendary)
            {
                UI.SettingsPanel.Buzz();
                UI.UIManager.Instance?.FlashScreen(new Color(1f, 0.75f, 0.3f), 0.32f, 0.7f);
            }
        }

        /// <summary>Called by the worker's hammer animation event on each strike.</summary>
        public void OnHammerStrike() => Strike(0.9f);

        void Strike(float volume)
        {
            hammerStrikes++;
            if (sparks != null) sparks.Play();
            AudioManager.Play("hammer", 0.09f, volume);
            if (hotSwordVisual != null)
                Tween.PunchScale(hotSwordVisual.transform, hotSwordBaseScale * 0.18f, 0.25f);
        }

        /// <summary>
        /// Manual hammer blow from the player tapping the anvil: knocks a slice of craft time
        /// off the current sword. Ignored while the anvil is idle so it never fights the loop.
        /// </summary>
        public void TapBoost()
        {
            if (!IsCrafting || Time.time < nextTapAllowed) return;
            nextTapAllowed = Time.time + tapCooldown;
            if (Time.time > comboEndsAt) tapCombo = 0;
            tapCombo = Mathf.Min(tapCombo + 1, comboMax);
            comboEndsAt = Time.time + comboWindow;
            float boost = tapBoostSeconds + comboBonusPerLevel * (tapCombo - 1);
            timer = Mathf.Min(timer + boost, duration);
            float p = Mathf.Clamp01(timer / duration);
            if (progressBar != null) progressBar.SetProgress(p);
            Strike(0.55f);
            UI.SettingsPanel.Buzz();
            Tween.PunchScale(transform, Vector3.one * 0.04f, 0.2f);
            // A floater only earns its place once a combo is rolling — single taps get
            // the punch and the ring, so the forge stays readable instead of chatty.
            if (tapCombo >= 2)
            {
                Vector3 where = craftPoint != null ? craftPoint.position : transform.position + Vector3.up;
                bool hot = tapCombo >= 3;
                UIManager.Instance?.SpawnFloatingText(
                    where, "CLANG! x" + tapCombo,
                    hot ? new Color(1f, 0.52f, 0.18f) : new Color(1f, 0.76f, 0.32f));
            }
        }

        /// <summary>Number of hammer blows landed on the current/last craft — drives the smoke test.</summary>
        public int HammerStrikes => hammerStrikes;

        /// <summary>Hands the freshly forged sword to the worker, shaped by its recipe and rarity.</summary>
        public GameObject TakeForgedSword(Transform hand)
        {
            if (hotSwordVisual != null) hotSwordVisual.SetActive(false);
            if (progressBar != null) progressBar.SetProgress(0f);
            if (hand == null || LastForged == null) return null;

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.config : null;
            if (config == null) return null;

            RecipeDef recipe = config.GetRecipe(LastForged.recipeId);
            GameObject prefab = recipe != null && recipe.swordPrefab != null ? recipe.swordPrefab : config.swordPrefab;
            if (prefab == null) return null;

            GameObject sword = Instantiate(prefab, hand);
            sword.transform.localPosition = new Vector3(0f, 0.04f, 0.06f);
            sword.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            SwordVisuals.ApplyRarity(sword, LastForged.rarity, config);
            return sword;
        }
    }
}
