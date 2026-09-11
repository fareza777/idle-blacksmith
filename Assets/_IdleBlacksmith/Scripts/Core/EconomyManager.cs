using System;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    public class EconomyManager : MonoBehaviour
    {
        public int Gold { get; private set; }
        public int TotalEarned { get; private set; }

        /// <summary>(newTotal, delta)</summary>
        public event Action<int, int> OnGoldChanged;

        public void Init(int startingGold, int totalEarned)
        {
            Gold = Mathf.Max(0, startingGold);
            TotalEarned = Mathf.Max(0, totalEarned);
            OnGoldChanged?.Invoke(Gold, 0);
        }

        public void AddGold(int amount)
        {
            if (amount == 0) return;
            Gold += amount;
            if (amount > 0) TotalEarned += amount;
            OnGoldChanged?.Invoke(Gold, amount);
        }

        public bool Spend(int amount)
        {
            if (amount < 0 || Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold, -amount);
            return true;
        }
    }
}
