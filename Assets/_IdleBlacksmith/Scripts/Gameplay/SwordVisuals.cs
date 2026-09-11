using IdleBlacksmith.Core;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>Applies a sword's recipe mesh and rarity look to a spawned instance.</summary>
    public static class SwordVisuals
    {
        /// <summary>Child that carries the rarity gem (submesh 1, the emissive slot).</summary>
        public const string GemChild = "Gem";

        public static void ApplyRarity(GameObject sword, Rarity rarity, GameConfig config)
        {
            if (sword == null) return;
            Transform gem = sword.transform.Find(GemChild);
            if (gem == null) return;

            Material gemMat = config != null ? config.GemMaterial(rarity) : null;
            var mr = gem.GetComponent<MeshRenderer>();
            if (mr != null && gemMat != null)
            {
                // Clone the array so only this instance's gem submesh changes — the shared
                // sword material assets stay untouched.
                Material[] mats = mr.sharedMaterials;
                if (mats != null && mats.Length > 1)
                {
                    var swapped = (Material[])mats.Clone();
                    swapped[1] = gemMat;
                    mr.sharedMaterials = swapped;
                }
            }
            gem.gameObject.SetActive(rarity != Rarity.Common);
        }

        /// <summary>Spawns the sparkle burst that marks a Rare or better sword.</summary>
        public static void PlaySparkle(GameObject sword, Rarity rarity, GameConfig config)
        {
            if (sword == null || config == null || config.raritySparklePrefab == null) return;
            if (rarity < Rarity.Epic) return;
            ParticleSystem ps = Object.Instantiate(
                config.raritySparklePrefab, sword.transform.position, Quaternion.identity, sword.transform);
            ps.Play();
            Object.Destroy(ps.gameObject, 2.5f);
        }
    }
}
