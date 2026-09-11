using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// Shared color palette for the whole asset family. One 8x8 palette texture,
    /// one Lit material, every mesh UV-mapped onto palette cells: guarantees the
    /// "one consistent stylized asset family" look.
    /// </summary>
    public static class Palette
    {
        public const int WoodDark = 0, WoodMid = 1, WoodLight = 2, FloorA = 3, FloorB = 4,
            WallCream = 5, BeamDark = 6, MetalDark = 7, MetalLight = 8, Skin = 9, SkinShade = 10,
            Apron = 11, ShirtBlue = 12, ShirtGreen = 13, ShirtOrange = 14, ShirtPurple = 15,
            Pants = 16, Boots = 17, OreRock = 18, OreCrystal = 19, Gold = 20, Ember = 21,
            ClothCream = 22, RedAccent = 23, White = 24, PlantGreen = 25, HairBrown = 26,
            HairBlack = 27, HairBlond = 28, Teal = 29, PlumDark = 30, Stone = 31, StoneDark = 32,
            Straw = 33, EyeDark = 34, Grass = 35, PathStone = 36, Cheek = 37, Banner = 38,
            Coal = 39, SwordBlade = 40, SwordGuard = 41, Grip = 42, RugRed = 43, RugCream = 44,
            GrassDark = 45, WoodPale = 46, Terracotta = 47, FlowerRed = 48, FlowerYellow = 49,
            FlowerPink = 50, GrassLight = 51;

        public static readonly Color32[] Colors =
        {
            Hex(0x6B4A2F), Hex(0x8A5A38), Hex(0xB3825A), Hex(0xC99A68), Hex(0xBE8F60),
            Hex(0xF4E7CE), Hex(0x54382A), Hex(0x3C414C), Hex(0xA8B1BE), Hex(0xF3C69E),
            Hex(0xE2AB7F), Hex(0x7A4E2E), Hex(0x4E8CC9), Hex(0x5BA86B), Hex(0xDF7F47),
            Hex(0x9A6FB8), Hex(0x5A4A66), Hex(0x43302A), Hex(0x565B66), Hex(0x7FD4E8),
            Hex(0xF2C14E), Hex(0xFF8A3D), Hex(0xFBF3E2), Hex(0xD95F4E), Hex(0xFFFFFF),
            Hex(0x6FA85C), Hex(0x4A3226), Hex(0x28241F), Hex(0xE9C46A), Hex(0x4FA3A5),
            Hex(0x6E4A5E), Hex(0x8E8E96), Hex(0x6A6A72), Hex(0xE8CE7A), Hex(0x2B2320),
            Hex(0x7CB35B), Hex(0xB9B2A4), Hex(0xF0A08A), Hex(0xC4483F), Hex(0x2E2A28),
            Hex(0xD7DEE8), Hex(0xE8B84B), Hex(0x5A3A2A), Hex(0xC25E4E), Hex(0xEBD9B4),
            Hex(0x699E4C), Hex(0xD9B98C), Hex(0xC9744A), Hex(0xE25B4E), Hex(0xF4C542),
            Hex(0xEF9BC0), Hex(0x93C86B),
        };

        static Color32 Hex(int rgb)
            => new Color32((byte)(rgb >> 16 & 0xFF), (byte)(rgb >> 8 & 0xFF), (byte)(rgb & 0xFF), 255);

        public static Vector2 UV(int index)
        {
            int x = index % 8, y = index / 8;
            // Inset slightly inside the cell to avoid bleeding.
            return new Vector2((x + 0.5f) / 8f, 1f - (y + 0.5f) / 8f);
        }
    }

    /// <summary>
    /// Flat-shaded mesh builder. Faces are bucketed per material slot so a single
    /// mesh can mix the palette material with an emissive material via submeshes.
    /// Winding follows Unity's clockwise-front convention; normal = Cross(c-a, b-a).
    /// </summary>
    public class MeshBuilder
    {
        class Bucket
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> T = new List<Vector2>();
            public readonly List<int> Tris = new List<int>();
        }

        readonly Dictionary<int, Bucket> buckets = new Dictionary<int, Bucket>();

        Bucket B(int mat)
        {
            if (!buckets.TryGetValue(mat, out Bucket b))
            {
                b = new Bucket();
                buckets[mat] = b;
            }
            return b;
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, int color, int mat = 0)
        {
            Bucket k = B(mat);
            Vector3 n = Vector3.Cross(c - a, b - a).normalized;
            Vector2 uv = Palette.UV(color);
            int i = k.V.Count;
            k.V.Add(a); k.V.Add(b); k.V.Add(c);
            k.N.Add(n); k.N.Add(n); k.N.Add(n);
            k.T.Add(uv); k.T.Add(uv); k.T.Add(uv);
            // Unity front faces wind clockwise seen from the normal side:
            // emit (0,2,1) so the visible side matches n = Cross(c-a, b-a).
            k.Tris.Add(i); k.Tris.Add(i + 2); k.Tris.Add(i + 1);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int color, int mat = 0)
        {
            Tri(a, b, c, color, mat);
            Tri(a, c, d, color, mat);
        }

        /// <summary>Quad from center + axes. Outward normal = Cross(right, up).</summary>
        public void Face(Vector3 center, Vector3 right, Vector3 up, int color, int mat = 0)
        {
            Quad(center - right - up, center - right + up, center + right + up, center + right - up, color, mat);
        }

        public void Box(Vector3 center, Vector3 size, int color, int mat = 0)
            => Box(center, size, Quaternion.identity, color, mat);

        // Axis-aligned box with a separate (typically lighter) top-face color.
        public void Box(Vector3 center, Vector3 size, int color, int mat, int topColor)
            => Box(center, size, Quaternion.identity, color, mat, topColor);

        public void Box(Vector3 center, Vector3 size, Quaternion rot, int color, int mat = 0, int topColor = -1)
        {
            Vector3 e = size * 0.5f;
            Vector3 X = rot * new Vector3(e.x, 0, 0);
            Vector3 Y = rot * new Vector3(0, e.y, 0);
            Vector3 Z = rot * new Vector3(0, 0, e.z);
            Face(center + Z, X, Y, color, mat);                       // front
            Face(center - Z, -X, Y, color, mat);                      // back
            Face(center + X, -Z, Y, color, mat);                      // right
            Face(center - X, Z, Y, color, mat);                       // left
            Face(center + Y, X, -Z, topColor >= 0 ? topColor : color, mat); // top
            Face(center - Y, X, Z, color, mat);                       // bottom
        }

        public void Cylinder(Vector3 center, float radius, float height, int segments, int color, int mat = 0, int topColor = -1)
        {
            float h = height * 0.5f;
            for (int i = 0; i < segments; i++)
            {
                float a0 = Mathf.PI * 2f * i / segments;
                float a1 = Mathf.PI * 2f * (i + 1) / segments;
                var p0b = center + new Vector3(Mathf.Cos(a0) * radius, -h, Mathf.Sin(a0) * radius);
                var p1b = center + new Vector3(Mathf.Cos(a1) * radius, -h, Mathf.Sin(a1) * radius);
                var p0t = p0b + Vector3.up * height;
                var p1t = p1b + Vector3.up * height;
                Quad(p0b, p1b, p1t, p0t, color, mat);
                Tri(center + Vector3.up * h, p0t, p1t, topColor >= 0 ? topColor : color, mat);
                Tri(center - Vector3.up * h, p1b, p0b, color, mat);
            }
        }

        /// <summary>Jittered octahedron — the classic low-poly rock.</summary>
        public void Rock(Vector3 center, Vector3 size, int color, float seed, int mat = 0)
        {
            Vector3[] corners =
            {
                center + Jit(new Vector3(1, 0, 0), size, seed, 0),
                center + Jit(new Vector3(0, 1, 0), size, seed, 1),
                center + Jit(new Vector3(0, 0, 1), size, seed, 2),
                center + Jit(new Vector3(-1, 0, 0), size, seed, 3),
                center + Jit(new Vector3(0, -1, 0), size, seed, 4),
                center + Jit(new Vector3(0, 0, -1), size, seed, 5),
            };
            int[][] faces =
            {
                new[] { 1, 0, 2 }, new[] { 1, 2, 3 }, new[] { 1, 3, 5 }, new[] { 1, 5, 0 },
                new[] { 4, 2, 0 }, new[] { 4, 3, 2 }, new[] { 4, 5, 3 }, new[] { 4, 0, 5 },
            };
            foreach (int[] f in faces)
                Tri(corners[f[0]], corners[f[1]], corners[f[2]], color, mat);
        }

        static Vector3 Jit(Vector3 dir, Vector3 size, float seed, int idx)
        {
            float h1 = Mathf.Abs(Mathf.Sin(seed * 12.9898f + idx * 78.233f) * 43758.5453f) % 1f;
            float h2 = Mathf.Abs(Mathf.Sin(seed * 39.3468f + idx * 11.135f) * 24634.6345f) % 1f;
            float j = 0.82f + 0.36f * h1;
            return Vector3.Scale(dir, size) * j + new Vector3(0, (h2 - 0.5f) * 0.12f * size.y, 0);
        }

        /// <summary>Triangular prism ramp (anvil horn, roof slopes). Slopes down toward +Z.</summary>
        public void Wedge(Vector3 center, Vector3 size, int color, int mat = 0)
        {
            Vector3 e = size * 0.5f;
            var a = center + new Vector3(-e.x, -e.y, -e.z);
            var b = center + new Vector3(e.x, -e.y, -e.z);
            var c = center + new Vector3(-e.x, e.y, -e.z);
            var d = center + new Vector3(e.x, e.y, -e.z);
            var f = center + new Vector3(-e.x, -e.y, e.z);
            var g = center + new Vector3(e.x, -e.y, e.z);
            // back (normal -Z)
            Quad(b, a, c, d, color, mat);
            // bottom
            Quad(f, a, b, g, color, mat);
            // slope (quad centered on the ramp, normal up+forward)
            Vector3 right = new Vector3(e.x, 0, 0);
            Vector3 up = new Vector3(0, size.y, -size.z) * 0.5f;
            Face(center, right, up, color, mat);
            // sides
            Tri(a, c, f, color, mat);
            Tri(g, d, b, color, mat);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            var mats = new List<int>(buckets.Keys);
            mats.Sort();
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            mesh.subMeshCount = mats.Count;
            var perBucketTris = new List<int[]>();
            foreach (int m in mats)
            {
                Bucket k = buckets[m];
                int baseIndex = verts.Count;
                verts.AddRange(k.V);
                normals.AddRange(k.N);
                uvs.AddRange(k.T);
                var shifted = new int[k.Tris.Count];
                for (int i = 0; i < shifted.Length; i++) shifted[i] = k.Tris[i] + baseIndex;
                perBucketTris.Add(shifted);
            }
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            for (int s = 0; s < perBucketTris.Count; s++)
                mesh.SetTriangles(perBucketTris[s], s);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
