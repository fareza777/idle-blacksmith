using System.Collections;
using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Walks in, buys one sword off the rack (with a happy hop and floating gold),
    /// then strolls out carrying the purchase.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SimpleWalker))]
    public class CustomerController : MonoBehaviour
    {
        [Header("Rig")]
        public Transform model;
        public GameObject carriedSwordProp;

        Animator anim;
        SimpleWalker walker;
        SwordRack rack;
        Vector3[] enterPath;
        Vector3[] leavePath;
        System.Action<CustomerController> onLeft;

        void Awake()
        {
            anim = GetComponent<Animator>();
            walker = GetComponent<SimpleWalker>();
            if (model == null) model = transform.Find("Model");
            if (carriedSwordProp != null) carriedSwordProp.SetActive(false);
        }

        public void Init(SwordRack rackRef, Vector3[] enterWaypoints, Vector3[] leaveWaypoints,
            System.Action<CustomerController> onGone)
        {
            rack = rackRef;
            enterPath = enterWaypoints;
            leavePath = leaveWaypoints;
            onLeft = onGone;
        }

        void Start() => StartCoroutine(Routine());

        IEnumerator Routine()
        {
            GameConfig config = GameManager.Instance.config;

            if (enterPath != null)
                foreach (Vector3 wp in enterPath)
                    yield return walker.MoveTo(wp, config.customerMoveSpeed);

            if (rack != null) walker.FaceTowards(rack.transform.position);
            yield return new WaitForSeconds(0.35f);

            if (rack != null && rack.TrySellSword(out Vector3 swordPos))
            {
                if (carriedSwordProp != null) carriedSwordProp.SetActive(true);
                HappyHop();

                int price = GameManager.Instance.CurrentSwordPrice; // relic ore + shop tier bonuses included
                GameManager.Instance.economy.AddGold(price);
                UIManager.Instance?.SpawnFloatingText(swordPos + Vector3.up * 0.4f, "+" + price);
                AudioManager.Play("coin");
                GameManager.Instance.Save();
                yield return new WaitForSeconds(0.75f);
            }

            if (leavePath != null)
                foreach (Vector3 wp in leavePath)
                    yield return walker.MoveTo(wp, config.customerMoveSpeed * config.customerLeaveSpeedMultiplier);

            onLeft?.Invoke(this);
            Destroy(gameObject, 0.05f);
        }

        void HappyHop()
        {
            if (model == null) return;
            float baseY = model.localPosition.y;
            Tween.LocalPositionY(model, baseY + 0.16f, 0.16f, Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (model != null)
                        Tween.LocalPositionY(model, baseY, 0.2f, Ease.InQuad);
                });
            Tween.PunchScale(model, new Vector3(0.07f, -0.07f, 0.07f), 0.3f);
        }
    }
}
