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

        Animator anim;
        SimpleWalker walker;
        OrePile orePile;
        AnvilStation anvil;
        SwordRack rack;
        int lane;
        bool initialized;

        GameObject carriedOre;
        GameObject carriedSword;
        Vector3 baseModelScale = Vector3.one;
        bool announcedFullRack;

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

        void Start()
        {
            if (!initialized && GameManager.Instance != null)
                Init(GameManager.Instance.orePile, GameManager.Instance.anvil, GameManager.Instance.rack, 0);
            StartCoroutine(WorkLoop());
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
                float moveSpeed = GameManager.Instance.upgrades.MoveSpeed(config);

                // 1. Walk to the ore pile and pick up a chunk.
                yield return walker.MoveTo(orePile.GetPickupPoint(lane), moveSpeed);
                walker.FaceTowards(orePile.transform.position);
                yield return SquashWait(config.pickupDuration);
                carriedOre = orePile.TakeOre(handAnchor);

                // 2. Walk to the anvil and hammer the sword.
                yield return walker.MoveTo(anvil.GetWorkPoint(lane), moveSpeed);
                walker.FaceTowards(anvil.transform.position);
                if (carriedOre != null) { Destroy(carriedOre); carriedOre = null; }

                float craftDuration = GameManager.Instance.upgrades.CraftDuration(config);
                anim.SetBool(HammerHash, true);
                bool done = false;
                anvil.BeginCraft(craftDuration, () => done = true);
                while (!done) yield return null;
                anim.SetBool(HammerHash, false);

                carriedSword = anvil.TakeForgedSword(handAnchor);
                anim.SetBool(CarryHash, true);

                // 3. Carry the sword to the rack; wait politely if it's full.
                if (rack.IsFull)
                {
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

                yield return walker.MoveTo(rack.GetDepositPoint(lane), moveSpeed);
                walker.FaceTowards(rack.transform.position);
                yield return new WaitForSeconds(config.depositDuration);

                if (carriedSword != null) { Destroy(carriedSword); carriedSword = null; }
                rack.DepositSword();
                anim.SetBool(CarryHash, false);
            }
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
