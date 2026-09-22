using System.Collections;
using IdleBlacksmith.Core;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// The patron behind a royal order: walks in when the contract lands, waits at the
    /// counter while it's live, then strolls out — a celebratory hop when the smiths
    /// delivered in time, a slower grumpy trudge when it lapsed.
    /// </summary>
    [RequireComponent(typeof(SimpleWalker))]
    public class PatronController : MonoBehaviour
    {
        public Transform model;

        OrderManager orders;
        OrderManager.Order watch;
        Vector3[] enterPath;
        Vector3[] leavePath;
        SimpleWalker walker;

        void Awake()
        {
            walker = GetComponent<SimpleWalker>();
            if (model == null) model = transform.Find("Model");
        }

        public void Init(OrderManager mgr, OrderManager.Order order, Vector3[] enter, Vector3[] leave)
        {
            orders = mgr;
            watch = order;
            enterPath = enter;
            leavePath = leave;
            StartCoroutine(Routine());
        }

        IEnumerator Routine()
        {
            GameConfig config = GameManager.Instance.config;
            if (enterPath != null)
                foreach (Vector3 wp in enterPath)
                    yield return walker.MoveTo(wp, config.customerMoveSpeed);

            AnvilStation anvil = GameManager.Instance != null ? GameManager.Instance.anvil : null;
            if (anvil != null) walker.FaceTowards(anvil.transform.position);

            while (orders != null && ReferenceEquals(orders.Active, watch))
            {
                if (model != null)
                    Tween.PunchScale(model, new Vector3(0.02f, 0.02f, 0.02f), 0.55f);
                yield return new WaitForSeconds(1.7f);
            }

            bool happy = watch != null && watch.delivered >= watch.needed;
            if (happy && model != null)
            {
                float baseY = model.localPosition.y;
                Tween.LocalPositionY(model, baseY + 0.22f, 0.18f, Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if (model != null)
                            Tween.LocalPositionY(model, baseY, 0.24f, Ease.InQuad);
                    });
                yield return new WaitForSeconds(0.55f);
            }

            float speed = config.customerMoveSpeed * (happy ? 1f : 0.7f);
            if (leavePath != null)
                foreach (Vector3 wp in leavePath)
                    yield return walker.MoveTo(wp, speed);

            Destroy(gameObject);
        }
    }
}
