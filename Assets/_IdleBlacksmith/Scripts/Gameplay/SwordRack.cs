using System.Collections.Generic;
using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Sword storage with visible peg slots. Each peg holds a real forged item, so the
    /// rack remembers which recipe and rarity every sword on it is.
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

        /// <summary>(stock, capacity)</summary>
        public event System.Action<int, int> OnStockChanged;

        readonly List<SwordItem> items = new List<SwordItem>();
        readonly List<GameObject> visuals = new List<GameObject>();

        public int Stock => items.Count;
        public int Capacity { get; private set; }
        public bool IsFull => Stock >= Capacity;
        public IReadOnlyList<SwordItem> Items => items;

        /// <summary>The most valuable sword on display — what a customer would pick first.</summary>
        public SwordItem BestItem
        {
            get
            {
                SwordItem best = null;
                foreach (SwordItem it in items)
                    if (best == null || Compare(it, best) > 0) best = it;
                return best;
            }
        }

        static int Compare(SwordItem a, SwordItem b)
        {
            int byRarity = a.rarity.CompareTo(b.rarity);
            if (byRarity != 0) return byRarity;
            return string.CompareOrdinal(a.recipeId, b.recipeId);
        }

        public void SetCapacity(int capacity)
        {
            Capacity = Mathf.Clamp(capacity, 1, slotPoints != null ? slotPoints.Length : Mathf.Max(1, capacity));
            if (slotPoints != null)
                for (int i = 0; i < slotPoints.Length; i++)
                    if (slotPoints[i] != null)
                        slotPoints[i].gameObject.SetActive(i < Capacity);

            // Trim anything that no longer fits (shrink from the top level down).
            while (items.Count > Capacity) RemoveVisualAt(items.Count - 1);
            Notify();
        }

        /// <summary>Rebuilds the rack contents from a save file.</summary>
        public void RestoreStock(List<SwordItem> saved)
        {
            ClearAll();
            if (saved == null) { Notify(); return; }
            foreach (SwordItem it in saved)
            {
                if (it == null || items.Count >= Capacity) break;
                SpawnSwordVisual(it, false);
            }
            Notify();
        }

        public void ClearAll()
        {
            for (int i = visuals.Count - 1; i >= 0; i--)
                if (visuals[i] != null) Destroy(visuals[i]);
            visuals.Clear();
            items.Clear();
            Notify();
        }

        public void DepositSword(SwordItem item)
        {
            if (item == null || IsFull) return;
            SpawnSwordVisual(item, true);
            Notify();
        }

        void SpawnSwordVisual(SwordItem item, bool animate)
        {
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.config : null;
            if (config == null || slotPoints == null || Stock >= slotPoints.Length) return;

            RecipeDef recipe = config.GetRecipe(item.recipeId);
            GameObject prefab = recipe != null && recipe.swordPrefab != null ? recipe.swordPrefab : config.swordPrefab;
            if (prefab == null) return;

            Transform slot = slotPoints[Stock];
            GameObject sword = Instantiate(prefab, slot);
            sword.transform.localPosition = Vector3.zero;
            sword.transform.localRotation = Quaternion.Euler(-72f, 0f, 0f);
            SwordVisuals.ApplyRarity(sword, item.rarity, config);

            items.Add(item);
            visuals.Add(sword);

            if (animate)
            {
                Transform t = sword.transform;
                Vector3 targetScale = t.localScale;
                t.localScale = Vector3.zero;
                Tween.Scale(t, targetScale, 0.35f, Ease.OutBack);
                AudioManager.Play("pop", 0.05f, 0.7f);
                SwordVisuals.PlaySparkle(sword, item.rarity, config);
            }
        }

        void RemoveVisualAt(int index)
        {
            if (index < 0 || index >= items.Count) return;
            items.RemoveAt(index);
            GameObject go = visuals[index];
            visuals.RemoveAt(index);
            if (go != null) Destroy(go);
        }

        /// <summary>Takes the best sword off the rack. Returns false when the rack is empty.</summary>
        public bool TrySellSword(out SwordItem sold, out Vector3 swordWorldPos)
        {
            sold = null;
            swordWorldPos = transform.position + Vector3.up;
            if (items.Count == 0) return false;

            int index = BestIndex();
            sold = items[index];
            GameObject sword = visuals[index];
            items.RemoveAt(index);
            visuals.RemoveAt(index);

            if (sword != null)
            {
                swordWorldPos = sword.transform.position;
                Tween.Scale(sword.transform, Vector3.zero, 0.25f, Ease.InBack)
                    .OnComplete(() => { if (sword != null) Destroy(sword); });
            }

            Notify();
            return true;
        }

        int BestIndex()
        {
            int best = 0;
            for (int i = 1; i < items.Count; i++)
                if (Compare(items[i], items[best]) > 0) best = i;
            return best;
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
