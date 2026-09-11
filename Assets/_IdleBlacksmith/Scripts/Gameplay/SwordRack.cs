using System.Collections.Generic;
using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Sword storage with visible peg slots. Capacity comes from the rack upgrade;
    /// pegs beyond capacity are hidden.
    /// </summary>
    public class SwordRack : MonoBehaviour
    {
        [Tooltip("Peg objects; sword visuals are parented here. Active count == capacity.")]
        public Transform[] slotPoints;
        [Tooltip("Where workers stand to deposit, one per lane")]
        public Transform[] depositPoints;
        [Tooltip("Where workers wait while the rack is full, one per lane")]
        public Transform[] waitPoints;
        public Transform customerPoint;
        public RackStockBar stockBar;

        public int Stock { get; private set; }
        public int Capacity { get; private set; }
        public bool IsFull => Stock >= Capacity;

        public event System.Action<int, int> OnStockChanged; // (stock, capacity)

        readonly List<GameObject> swordVisuals = new List<GameObject>();

        public void SetCapacity(int capacity)
        {
            Capacity = Mathf.Clamp(capacity, 1, slotPoints != null ? slotPoints.Length : capacity);
            if (slotPoints != null)
                for (int i = 0; i < slotPoints.Length; i++)
                    if (slotPoints[i] != null)
                        slotPoints[i].gameObject.SetActive(i < Capacity);
            Notify();
        }

        public void RestoreStock(int count)
        {
            int n = Mathf.Min(count, Capacity);
            for (int i = 0; i < n; i++) SpawnSwordVisual(false);
            Notify();
        }

        public void DepositSword()
        {
            if (IsFull) return;
            SpawnSwordVisual(true);
            Notify();
        }

        void SpawnSwordVisual(bool animate)
        {
            GameConfig config = GameManager.Instance.config;
            if (slotPoints == null || Stock >= slotPoints.Length || config.swordPrefab == null) return;
            Transform slot = slotPoints[Stock];
            GameObject sword = Instantiate(config.swordPrefab, slot);
            sword.transform.localPosition = Vector3.zero;
            sword.transform.localRotation = Quaternion.Euler(-72f, 0f, 0f);
            swordVisuals.Add(sword);
            Stock++;
            if (animate)
            {
                Transform t = sword.transform;
                Vector3 targetScale = t.localScale;
                t.localScale = Vector3.zero;
                Tween.Scale(t, targetScale, 0.35f, Ease.OutBack);
                AudioManager.Play("pop", 0.05f, 0.7f);
            }
        }

        public bool TrySellSword(out Vector3 swordWorldPos)
        {
            swordWorldPos = transform.position + Vector3.up;
            if (Stock <= 0 || swordVisuals.Count == 0) return false;
            Stock--;
            GameObject sword = swordVisuals[swordVisuals.Count - 1];
            swordVisuals.RemoveAt(swordVisuals.Count - 1);
            swordWorldPos = sword.transform.position;
            Tween.Scale(sword.transform, Vector3.zero, 0.25f, Ease.InBack)
                .OnComplete(() => { if (sword != null) Destroy(sword); });
            Notify();
            return true;
        }

        void Notify()
        {
            OnStockChanged?.Invoke(Stock, Capacity);
            if (stockBar != null) stockBar.SetStock(Stock, Capacity);
        }

        public Vector3 GetDepositPoint(int lane)
        {
            if (depositPoints == null || depositPoints.Length == 0) return transform.position + Vector3.forward;
            return depositPoints[Mathf.Clamp(lane, 0, depositPoints.Length - 1)].position;
        }

        public Vector3 GetWaitPoint(int lane)
        {
            if (waitPoints == null || waitPoints.Length == 0) return transform.position + Vector3.forward * 1.5f;
            return waitPoints[Mathf.Clamp(lane, 0, waitPoints.Length - 1)].position;
        }
    }
}
