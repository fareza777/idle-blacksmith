using System.Collections.Generic;
using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Royal orders: timed premium contracts from named patrons. While an order is live it
    /// asks for N swords of one unlocked recipe; every matching sword delivered pays the
    /// premium rate instantly, and finishing the lot pays a relic-ore bonus on top. Orders
    /// that expire simply go unclaimed — the contract was missed, nothing is taken away.
    /// </summary>
    public class OrderManager : MonoBehaviour
    {
        public class Order
        {
            public string patron;
            public string recipeId;
            public int needed;
            public int delivered;
            public float expiresAt;
            public float priceMult;
            public int bonusRelic;
            public int Left => needed - delivered;
            public float SecondsLeft => Mathf.Max(0f, expiresAt - Time.time);
        }

        [Header("Cadence")]
        [Tooltip("Delay between an order finishing/expiring and the next one arriving")]
        public Vector2 nextOrderDelay = new Vector2(70f, 130f);
        [Tooltip("How long a patron waits for delivery before walking")]
        public float orderDuration = 150f;

        [Header("Terms")]
        public Vector2Int countRange = new Vector2Int(3, 6);
        [Tooltip("Sale-price multiplier paid per delivered sword")]
        public float priceMult = 2f;
        [Tooltip("Relic ore paid when the full order lands")]
        public int bonusRelic = 3;
        [Tooltip("First order arrives this many seconds into the session")]
        public float firstOrderDelay = 45f;

        public Order Active { get; private set; }
        public event System.Action OnChanged;

        static readonly string[] Patrons =
        {
            "Sir Aldric", "Captain Voss", "Lady Merriam", "Warden Pike",
            "Squire Tommel", "Baron Krell", "Magistrate Ulla", "The Steward",
        };

        float nextOrderAt = -1f;

        void Start()
        {
            nextOrderAt = Time.time + firstOrderDelay;
        }

        void Update()
        {
            if (Active == null)
            {
                if (nextOrderAt < 0f || Time.time < nextOrderAt) return;
                TryIssue();
                return;
            }

            if (Time.time >= Active.expiresAt)
            {
                UIManager.Instance?.SpawnFloatingText(
                    new Vector3(0f, 2.1f, 0f), "Order expired", new Color(0.85f, 0.8f, 0.75f));
                AudioManager.Play("denied", 0.06f, 0.6f);
                Active = null;
                ScheduleNext();
                OnChanged?.Invoke();
            }
        }

        /// <summary>Immediately issues an order if none is active — used by the smoke test and QA builds.</summary>
        public void ForceIssue()
        {
            if (Active == null) TryIssue();
        }

        void TryIssue()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.recipes == null || gm.config == null || gm.config.recipes == null) return;

            var pool = new List<RecipeDef>();
            foreach (RecipeDef r in gm.config.recipes)
                if (r != null && gm.recipes.IsAvailable(r)) pool.Add(r);
            if (pool.Count == 0) { nextOrderAt = Time.time + 20f; return; }

            RecipeDef pick = pool[Random.Range(0, pool.Count)];
            // Bigger ask on cheaper weapons — patrons order daggers by the dozen, greatswords one by one.
            int n = Mathf.Clamp(
                Random.Range(countRange.x, countRange.y + 1) - (pick.baseValue >= 100 ? 2 : 0) - (pick.baseValue >= 400 ? 2 : 0),
                1, 7);
            Active = new Order
            {
                patron = Patrons[Random.Range(0, Patrons.Length)],
                recipeId = pick.id,
                needed = n,
                delivered = 0,
                expiresAt = Time.time + orderDuration,
                priceMult = priceMult,
                bonusRelic = bonusRelic,
            };
            OnChanged?.Invoke();
            AudioManager.Play("quest_done", 0.05f, 0.55f);
        }

        /// <summary>
        /// Offers a forged sword to the order. Returns true when the order takes it — the
        /// worker should then skip stocking the rack.
        /// </summary>
        public bool TryDeliver(SwordItem item, Vector3 handPos)
        {
            if (Active == null || item == null || item.recipeId != Active.recipeId) return false;
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.economy == null) return false;

            int pay = Mathf.RoundToInt(gm.PriceOf(item) * Active.priceMult);
            gm.economy.AddGold(pay);
            Active.delivered++;
            UIManager.Instance?.SpawnFloatingText(
                handPos + Vector3.up * 0.5f, "+" + pay + " order", new Color(1f, 0.86f, 0.42f));
            AudioManager.Play("coin", 0.07f, 0.75f);

            if (Active.delivered >= Active.needed) Complete();
            else OnChanged?.Invoke();
            return true;
        }

        void Complete()
        {
            GameManager gm = GameManager.Instance;
            int relic = Active != null ? Active.bonusRelic : 0;
            string patron = Active != null ? Active.patron : "Order";
            if (gm != null && relic > 0) gm.AddRelicOre(relic);
            UIManager.Instance?.SpawnFloatingText(
                new Vector3(0f, 2.1f, 0f),
                patron + "'s order complete! +" + relic + " relic ore",
                new Color(0.62f, 0.9f, 1f));
            AudioManager.Play("fanfare");
            Active = null;
            ScheduleNext();
            OnChanged?.Invoke();
        }

        void ScheduleNext()
        {
            nextOrderAt = Time.time + Random.Range(nextOrderDelay.x, nextOrderDelay.y);
        }
    }
}
