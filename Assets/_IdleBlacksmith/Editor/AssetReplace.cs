using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// In-place asset replacement for the generated pipeline.
    ///
    /// Every generated asset used to be written as delete-then-create. That mints a NEW GUID on
    /// every rebuild, and the player build's caches are keyed on those GUIDs, so a later build
    /// can pack a level0 whose object table no longer matches the data — which crashes the
    /// player at startup with "The file 'level0' is corrupted! [Position out of bounds!]".
    /// Overwriting in place keeps each asset's identity stable across rebuilds.
    /// </summary>
    public static class AssetReplace
    {
        /// <summary>Saves a freshly built mesh at <paramref name="path"/>, refilling an existing asset.</summary>
        public static Mesh SaveMesh(Mesh built, string name, string path)
        {
            built.name = name;
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(built, path);
                return built;
            }

            // Refill the existing mesh so every MeshFilter that already references it stays valid.
            // Clear() resets indexFormat back to UInt16, so the format has to be set after it —
            // the environment meshes exceed the 65k vertex limit of a 16-bit index buffer.
            existing.Clear();
            if (built.vertexCount > 65000)
                existing.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            existing.SetVertices(built.vertices);
            existing.SetNormals(built.normals);
            existing.SetUVs(0, built.uv);
            existing.subMeshCount = built.subMeshCount;
            for (int i = 0; i < built.subMeshCount; i++)
                existing.SetTriangles(built.GetTriangles(i), i);
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);

            Object.DestroyImmediate(built);
            return existing;
        }

        /// <summary>Saves a clip at <paramref name="path"/>, copying into an existing asset.</summary>
        public static AnimationClip SaveClip(AnimationClip built, string path)
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(built, path);
                return built;
            }

            EditorUtility.CopySerialized(built, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(built);
            return existing;
        }

        /// <summary>
        /// Saves an animator controller at <paramref name="path"/>. Controllers can only be authored
        /// by creating one, so this builds at a scratch path and copies the result into the existing
        /// asset, keeping its GUID.
        /// </summary>
        public static AnimatorController SaveController(AnimatorController built, string path)
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing == null)
            {
                AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(built), path);
                return built;
            }

            EditorUtility.CopySerialized(built, existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(built));
            return existing;
        }

        /// <summary>Scratch path for authoring an asset that will be copied into place.</summary>
        public static string ScratchPath(string folder, string name)
            => $"{folder}/__tmp_{name}.asset";

        /// <summary>
        /// Saves a prefab at <paramref name="path"/>. SaveAsPrefabAsset overwrites the existing
        /// prefab without touching its .meta, so the GUID survives — deleting first would not.
        /// </summary>
        public static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }
    }
}
