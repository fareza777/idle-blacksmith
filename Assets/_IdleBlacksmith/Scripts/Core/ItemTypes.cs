using System;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>Forged-sword quality tiers. Order matters: index is serialized in the save.</summary>
    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
    }

    /// <summary>Static lookup for rarity pricing, naming and the gem colour shown on the sword.</summary>
    public static class RarityInfo
    {
        public const int Count = 5;

        /// <summary>Sale-price multiplier per rarity.</summary>
        public static readonly float[] Multiplier = { 1f, 1.5f, 2.6f, 4.5f, 9f };

        public static readonly string[] DisplayName = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };

        /// <summary>Base roll weight before luck shifts the distribution upward.</summary>
        public static readonly float[] Weight = { 62f, 22f, 10f, 4.5f, 1.5f };

        static readonly Color[] GemColors =
        {
            new Color(0.84f, 0.86f, 0.88f), // Common    — plain steel
            new Color(0.42f, 0.80f, 0.45f), // Uncommon  — green
            new Color(0.36f, 0.62f, 0.95f), // Rare      — blue
            new Color(0.72f, 0.44f, 0.92f), // Epic      — purple
            new Color(1.00f, 0.72f, 0.22f), // Legendary — gold
        };

        public static Color GemColor(Rarity r)
            => GemColors[Mathf.Clamp((int)r, 0, Count - 1)];

        public static Color TextColor(Rarity r)
            => GemColor(r);

        public static float MultiplierOf(Rarity r)
            => Multiplier[Mathf.Clamp((int)r, 0, Count - 1)];

        public static string NameOf(Rarity r)
            => DisplayName[Mathf.Clamp((int)r, 0, Count - 1)];

        /// <summary>
        /// Rolls a rarity. <paramref name="luck"/> (0 = none) multiplies the weight of every
        /// tier above Common, so high luck tilts the curve toward Epic/Legendary without
        /// ever making them certain.
        /// </summary>
        public static Rarity Roll(float luck, System.Random rng)
        {
            float boost = 1f + Mathf.Max(0f, luck);
            float total = 0f;
            var w = new float[Count];
            for (int i = 0; i < Count; i++)
            {
                w[i] = Weight[i] * (i == 0 ? 1f : Mathf.Pow(boost, i));
                total += w[i];
            }

            double roll = rng.NextDouble() * total;
            float acc = 0f;
            for (int i = 0; i < Count; i++)
            {
                acc += w[i];
                if (roll <= acc) return (Rarity)i;
            }
            return Rarity.Common;
        }
    }

    /// <summary>One forged sword sitting in the rack. Serialized inside the save file.</summary>
    [Serializable]
    public class SwordItem
    {
        public string recipeId = RecipeId.Copper;
        public Rarity rarity = Rarity.Common;

        public SwordItem() { }

        public SwordItem(string recipe, Rarity quality)
        {
            recipeId = recipe;
            rarity = quality;
        }

        public SwordItem Clone() => new SwordItem(recipeId, rarity);
    }
}
