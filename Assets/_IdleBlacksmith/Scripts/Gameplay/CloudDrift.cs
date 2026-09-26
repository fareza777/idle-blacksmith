using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// A cloud lazily crossing the sky: drifts on +x, wraps to the far side, and bobs
    /// gently so the sky never looks static when the camera pans wide.
    /// </summary>
    public class CloudDrift : MonoBehaviour
    {
        public float speed = 0.28f;
        public float minX = -14f;
        public float maxX = 14f;
        public float bobAmplitude = 0.18f;
        public float bobPeriod = 7f;

        float baseY;
        float phase;

        void Start()
        {
            baseY = transform.position.y;
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            Vector3 p = transform.position;
            p.x += speed * Time.deltaTime;
            if (p.x > maxX) p.x = minX;
            p.y = baseY + Mathf.Sin(Time.time * (Mathf.PI * 2f / bobPeriod) + phase) * bobAmplitude;
            transform.position = p;
        }
    }
}
