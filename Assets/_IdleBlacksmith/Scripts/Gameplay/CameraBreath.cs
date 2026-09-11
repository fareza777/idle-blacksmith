using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>Barely-there camera sway so the shop feels alive.</summary>
    public class CameraBreath : MonoBehaviour
    {
        public float swayAmount = 0.04f;
        public float swaySpeed = 0.35f;

        Vector3 origin;

        void Start() => origin = transform.position;

        void Update()
        {
            float t = Time.time * swaySpeed;
            transform.position = origin + new Vector3(
                Mathf.Sin(t) * swayAmount,
                Mathf.Sin(t * 0.7f) * swayAmount * 0.5f,
                Mathf.Cos(t * 0.85f) * swayAmount);
        }
    }
}
