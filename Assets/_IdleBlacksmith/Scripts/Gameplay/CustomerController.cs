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
        float priceMult = 1f;
        bool isVip;

        void Awake()
        {
            anim = GetComponent<Animator>();
            walker = GetComponent<SimpleWalker>();
            if (model == null) model = transform.Find("Model");
            if (carriedSwordProp != null) carriedSwordProp.SetActive(false);
        }

        public void Init(SwordRack rackRef, Vector3[] enterWaypoints, Vector3[] leaveWaypoints,
            System.Action<CustomerController> onGone, float payMult = 1f, bool vip = false)
        {
            rack = rackRef;
            enterPath = enterWaypoints;
            leavePath = leaveWaypoints;
            onLeft = onGone;
            priceMult = payMult;
            isVip = vip;
        }

        void Start() => StartCoroutine(Routine());

        IEnumerator Routine()
        {
            GameConfig config = GameManager.Instance.config;

            if (enterPath != null)
                foreach (Vector3 wp in enterPath)
                    yield return walker.MoveTo(wp, config.customerMoveSpeed);

            if (isVip)
                UIManager.Instance?.SpawnFloatingText(
                    transform.position + Vector3.up * 2.2f, "VIP!", new Color(1f, 0.84f, 0.3f));

            if (rack != null) walker.FaceTowards(rack.transform.position);
            yield return new WaitForSeconds(0.35f);

            if (rack != null && rack.TrySellSword(out SwordItem item, out Vector3 swordPos))
            {
                if (carriedSwordProp != null) carriedSwordProp.SetActive(true);
                HappyHop();

                int price = Mathf.RoundToInt(GameManager.Instance.PriceOf(item) * priceMult);
                GameManager.Instance.economy.AddGold(price);
                GameManager.Instance.RegisterSale(item);
                UIManager.Instance?.SpawnFloatingText(
                    swordPos + Vector3.up * 0.4f,
                    isVip ? "VIP +" + price : "+" + price,
                    isVip ? new Color(1f, 0.86f, 0.3f) : RarityInfo.TextColor(item.rarity));
                UIManager.Instance?.FlyCoin(swordPos);
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
