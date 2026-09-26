using System.Collections;
using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// The blacksmith: ore pickup -> hammer at the anvil -> carry sword to the rack.
    /// Drives proper Animator transitions (Speed/Carry/Hammer) and reacts to animation
    /// events for hammer hits. Lane index keeps the helper on his own approach path.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SimpleWalker))]
    public class WorkerController : MonoBehaviour
    {
        static readonly int CarryHash = Animator.StringToHash("Carry");
        static readonly int HammerHash = Animator.StringToHash("Hammer");

        [Header("Rig")]
        public Transform model;
        public Transform handAnchor;
        [Tooltip("The smith's hammer prop — hidden while ore or a finished sword occupies the same hand.")]
        public GameObject hammerProp;

        Animator anim;
        SimpleWalker walker;
        OrePile orePile;
        AnvilStation anvil;
        SwordRack rack;
        int lane;
        bool initialized;

        GameObject carriedOre;
        GameObject carriedSword;
        SwordItem carriedItem;
        Vector3 baseModelScale = Vector3.one;
        bool announcedFullRack;
        bool announcedNoOre;

        void Awake()
        {
            anim = GetComponent<Animator>();
            walker = GetComponent<SimpleWalker>();
            if (model == null) model = transform.Find("Model");
            if (handAnchor == null && model != null) handAnchor = model.Find("ArmR/HandAnchor");
            if (model != null) baseModelScale = model.localScale;
        }

        public void Init(OrePile pile, AnvilStation anvilStation, SwordRack swordRack, int laneIndex)
        {
            orePile = pile;
            anvil = anvilStation;
            rack = swordRack;
            lane = laneIndex;
            initialized = true;
        }

        /// <summary>
        /// Which step of the work loop the smith is on. Purely diagnostic — the smoke test reads
        /// it to tell a stall apart from a slow loop.
        /// </summary>
        public string Phase { get; private set; } = "starting";

        void Start()
        {
            if (!initialized && GameManager.Instance != null)
                Init(GameManager.Instance.orePile, GameManager.Instance.anvil, GameManager.Instance.rack, 0);
            StartCoroutine(WorkLoop());
        }

        void Update()
        {
            // One hand, one tool: the hammer only shows while the hand isn't holding ore or a blade.
            if (hammerProp == null) return;
            bool show = carriedOre == null && carriedSword == null;
            if (hammerProp.activeSelf != show) hammerProp.SetActive(show);
        }

        public void PlaySpawnEffect()
        {
            if (model == null) return;
            model.localScale = Vector3.zero;
            Tween.Scale(model, baseModelScale, 0.55f, Ease.OutBack);
        }

        IEnumerator WorkLoop()
        {
            // Let every station finish Start() before we begin.
            yield return null;
            while (true)
            {
                if (orePile == null || anvil == null || rack == null || GameManager.Instance == null)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                GameConfig config = GameManager.Instance.config;
                ResourceManager resources = GameManager.Instance.resources;
                float moveSpeed = GameManager.Instance.upgrades.MoveSpeed(config);

                // 1. Walk to the ore pile; wait there until there is ore to smelt.
                Phase = "walk-to-pile";
                yield return walker.MoveTo(orePile.GetPickupPoint(lane), moveSpeed);
                Phase = "at-pile";
                walker.FaceTowards(orePile.transform.position);

                int oreCost = GameManager.Instance.ActiveOreCost;
                if (resources != null && !resources.TryConsume(oreCost))
                {
                    Phase = "waiting-for-ore";
                    if (!announcedNoOre)
                    {
                        announcedNoOre = true;
                        UIManager.Instance?.SpawnFloatingText(
                            orePile.transform.position + Vector3.up * 1.3f, "No ore!",
                            new Color(1f, 0.62f, 0.3f));
                    }
                    // The base prospecting rate is always positive, so this always clears.
                    while (!resources.TryConsume(oreCost)) yield return null;
                }
                announcedNoOre = false;

                Phase = "pickup";
                yield return SquashWait(config.pickupDuration);
                carriedOre = orePile.TakeOre(handAnchor);

                // 2. Walk to the anvil and hammer the sword.
                Phase = "walk-to-anvil";
                yield return walker.MoveTo(anvil.GetWorkPoint(lane), moveSpeed);
                Phase = "at-anvil";
                walker.FaceTowards(anvil.transform.position);
                if (carriedOre != null) { Destroy(carriedOre); carriedOre = null; }

                float craftDuration = GameManager.Instance.upgrades.CraftDuration(config, GameManager.Instance.ActiveRecipe);
                Phase = $"crafting({craftDuration:0.##}s)";
                anim.SetBool(HammerHash, true);
                bool done = false;
                anvil.BeginCraft(craftDuration, GameManager.Instance.ActiveRecipe, () => done = true);
                while (!done) yield return null;
                anim.SetBool(HammerHash, false);
                Phase = "forged";

                carriedSword = anvil.TakeForgedSword(handAnchor);
                carriedItem = anvil.LastForged;
                AnnounceRarity(carriedItem, anvil.transform.position);
                anim.SetBool(CarryHash, true);

                // 3. Carry the sword to the rack; wait politely if it's full.
                if (rack.IsFull)
                {
                    Phase = "rack-full";
                    if (!announcedFullRack)
                    {
                        announcedFullRack = true;
                        UIManager.Instance?.SpawnFloatingText(
                            rack.transform.position + Vector3.up * 1.6f, "Rack full!", new Color(1f, 0.62f, 0.3f));
                    }
                    yield return walker.MoveTo(rack.GetWaitPoint(lane), moveSpeed);
                    walker.FaceTowards(rack.transform.position);
                    while (rack.IsFull) yield return null;
                }
                announcedFullRack = false;

                Phase = "walk-to-rack";
                yield return walker.MoveTo(rack.GetDepositPoint(lane), moveSpeed);
                walker.FaceTowards(rack.transform.position);
                yield return new WaitForSeconds(config.depositDuration);

                if (carriedSword != null) { Destroy(carriedSword); carriedSword = null; }
                var orders = GameManager.Instance != null ? GameManager.Instance.orders : null;
                if (orders == null || !orders.TryDeliver(carriedItem, transform.position))
                    rack.DepositSword(carriedItem);
                carriedItem = null;
                anim.SetBool(CarryHash, false);
            }
        }

        /// <summary>
        /// Records the forge in the stats and celebrates anything better than Common, so the
        /// player notices when a Rare or Legendary drops.
        /// </summary>
        void AnnounceRarity(SwordItem item, Vector3 anvilPos)
        {
            GameManager gm = GameManager.Instance;
            if (item == null || gm == null) return;
            gm.RegisterForged(item);
            if (item.rarity < Rarity.Uncommon) return;

            UIManager.Instance?.SpawnFloatingText(
                anvilPos + Vector3.up * 1.95f,
                RarityInfo.NameOf(item.rarity) + "!",
                RarityInfo.TextColor(item.rarity));
            AudioManager.Play(item.rarity >= Rarity.Epic ? "achievement" : "levelup", 0.05f, 0.65f);
        }

        IEnumerator SquashWait(float seconds)
        {
            if (model != null) Tween.PunchScale(model, new Vector3(0.12f, -0.12f, 0.12f), 0.35f);
            yield return new WaitForSeconds(seconds);
        }

        /// <summary>Animation event fired by the Hammer clip on each impact frame.</summary>
        public void OnHammerHit()
        {
            if (anim != null && anim.GetBool(HammerHash) && anvil != null)
                anvil.OnHammerStrike();
        }
    }
}
