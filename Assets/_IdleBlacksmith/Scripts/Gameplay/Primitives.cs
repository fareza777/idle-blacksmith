using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Collider-free primitive spawner. <see cref="GameObject.CreatePrimitive"/> adds a
    /// collider whose class is stripped in player builds, spamming "class doesn't exist"
    /// in logcat. This path creates only the visual — no physics type is ever touched.
    /// </summary>
    public static class Primitives
    {
        public static GameObject Create(PrimitiveType type)
        {
            var go = new GameObject(type.ToString());
            go.AddComponent<MeshFilter>().sharedMesh =
                Resources.GetBuiltinResource<Mesh>(MeshName(type));
            go.AddComponent<MeshRenderer>();
            return go;
        }

        static string MeshName(PrimitiveType t)
        {
            switch (t)
            {
                case PrimitiveType.Sphere: return "Sphere.fbx";
                case PrimitiveType.Quad: return "Quad.fbx";
                case PrimitiveType.Cylinder: return "Cylinder.fbx";
                case PrimitiveType.Capsule: return "New-Capsule.fbx";
                case PrimitiveType.Plane: return "Plane.fbx";
                default: return "Cube.fbx";
            }
        }
    }
}
