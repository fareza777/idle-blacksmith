using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Recomputes the Production bag. Call Recalculate after anything is bought, unlocked
    /// or prestiged; it is cheap enough to run on every purchase.
    /// </summary>
    public class ProductionManager : MonoBehaviour
    {
        public GameConfig config;

        /// <summary>Fired after every recalculation, for UI that shows a derived total.</summary>
        public event System.Action OnChanged;

        IProductionModifier[] modifiers;

        public GameConfig Config => config;

        public void Recalculate()
        {
            Production.Reset(config);
            foreach (IProductionModifier m in ResolveModifiers())
                m?.Contribute(config);
            OnChanged?.Invoke();
        }

        IProductionModifier[] ResolveModifiers()
        {
            if (modifiers == null || modifiers.Length == 0)
            {
                var found = new System.Collections.Generic.List<IProductionModifier>();
                foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb is IProductionModifier mod) found.Add(mod);
                modifiers = found.ToArray();
            }
            return modifiers;
        }

        /// <summary>Forces the modifier list to be rebuilt — call after adding managers at runtime.</summary>
        public void InvalidateModifiers() => modifiers = null;
    }
}
