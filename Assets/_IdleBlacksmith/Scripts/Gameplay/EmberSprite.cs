using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// The lucky ember: every couple of minutes a glowing sprite drifts over the village for
    /// a few seconds. Tapping it grants a gold bonus worth several times the going price of
    /// the shop's best blade — the idle genre's wandering bonus, and a reason to keep an eye
    /// on the world. Everything here is spawned at runtime; the scene holds nothing.
    /// </summary>
    public class EmberSprite : MonoBehaviour
    {
        [Tooltip("Seconds before the first sprite appears after a session starts")]
        public float firstDelay = 45f;
        [Tooltip("Seconds between appearances (randomized in this range)")]
        public Vector2 interval = new Vector2(70f, 120f);
        [Tooltip("Seconds the sprite stays up before fading away")]
        public float lifetime = 9f;
        [Tooltip("Bonus pays this many times the price of the best sword on the rack")]
        public float rewardMultiplier = 10f;
        [Tooltip("Horizontal wander bounds around the village centre")]
        public Vector2 wanderX = new Vector2(-4.5f, 4.5f);
        public Vector2 wanderZ = new Vector2(-2.5f, 6.5f);
        public float floatHeight = 2.6f;

        Transform sprite;
        float nextAt;
        float dieAt;
        float seed;
        Vector3 home;
        bool active;

        /// <summary>Screen-testable only while the sprite is up.</summary>
        public bool IsActive => active;
        public Vector3 AnchorWorld => sprite != null ? sprite.position : transform.position;

        void Start()
        {
            sprite = BuildSprite();
            sprite.gameObject.SetActive(false);
            nextAt = Time.time + firstDelay;
            seed = Random.value * 6.283f;
        }

        void Update()
        {
            if (!active)
            {
                if (Time.time >= nextAt) Appear();
                return;
            }

            // Slow figure-eight drift with a bob — reads as a wandering firefly spirit.
            float t = Time.time - (dieAt - lifetime);
            Vector3 p = home + new Vector3(
                Mathf.Sin(t * 0.9f + seed) * 0.9f,
                Mathf.Sin(t * 1.7f + seed) * 0.35f,
                Mathf.Cos(t * 0.7f + seed) * 0.6f);
            sprite.position = p;
            float pulse = 1f + Mathf.Sin(t * 5f) * 0.12f;
            sprite.localScale = Vector3.one * 0.30f * pulse;

            if (Time.time >= dieAt) Vanish();
        }

        void Appear()
        {
            // Only worth offering once the forge actually runs.
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.economy == null || gm.Data == null)
            {
                nextAt = Time.time + 10f;
                return;
            }

            home = new Vector3(Random.Range(wanderX.x, wanderX.y), floatHeight, Random.Range(wanderZ.x, wanderZ.y));
            sprite.position = home;
            sprite.gameObject.SetActive(true);
            dieAt = Time.time + lifetime;
            active = true;

            sprite.localScale = Vector3.zero;
            Tween.Scale(sprite, Vector3.one * 0.30f, 0.4f, Ease.OutBack);
            AudioManager.Play("pop", 0.04f, 0.5f);
        }

        void Vanish()
        {
            active = false;
            nextAt = Time.time + Random.Range(interval.x, interval.y);
            Tween.Scale(sprite, Vector3.zero, 0.3f, Ease.InBack)
                .OnComplete(() => { if (sprite != null) sprite.gameObject.SetActive(false); });
        }

        /// <summary>Called by BuildingPicker when the player taps the sprite.</summary>
        public void Collect()
        {
            if (!active) return;
            GameManager gm = GameManager.Instance;
            int reward = RewardFor(gm);
            if (gm != null && gm.economy != null) gm.economy.AddGold(reward);
            if (gm != null && gm.Data != null && gm.Data.stats != null) gm.Data.stats.embersCaught++;

            UIManager.Instance?.SpawnFloatingText(
                sprite.position + Vector3.up * 0.5f,
                "+" + reward + " EMBER!",
                new Color(1f, 0.80f, 0.30f));
            AudioManager.Play("achievement", 0.06f, 0.75f);
            UI.SettingsPanel.Buzz();
            Vanish();
        }

        int RewardFor(GameManager gm)
        {
            if (gm == null) return 50;
            SwordItem best = gm.rack != null ? gm.rack.BestItem : null;
            if (best != null) return Mathf.Max(15, Mathf.RoundToInt(gm.PriceOf(best) * rewardMultiplier));
            RecipeDef active = gm.ActiveRecipe;
            if (active != null)
                return Mathf.Max(15, Mathf.RoundToInt(gm.PriceOf(new SwordItem(active.id, Rarity.Common)) * rewardMultiplier));
            return Mathf.Max(15, Mathf.RoundToInt(gm.config.swordPrice * rewardMultiplier));
        }

        Transform BuildSprite()
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(transform, false);
            var r = c.GetComponent<MeshRenderer>();
            if (r != null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                var m = new Material(sh);
                m.SetColor("_BaseColor", new Color(1f, 0.66f, 0.22f));
                r.material = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return c.transform;
        }
    }
}
