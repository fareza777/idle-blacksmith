using UnityEngine;

namespace IdleBlacksmith.UI
{
    /// <summary>Gentle idle scale pulse to draw attention (e.g. the Upgrades button).</summary>
    public class PulseLoop : MonoBehaviour
    {
        public float amplitude = 0.045f;
        public float speed = 2.6f;

        Vector3 baseScale = Vector3.one;
        bool stopped;

        void OnEnable() => baseScale = transform.localScale;

        void Update()
        {
            if (stopped)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 12f * Time.deltaTime);
                return;
            }
            float s = 1f + Mathf.Sin(Time.time * speed) * amplitude;
            transform.localScale = baseScale * s;
        }

        public void Stop() => stopped = true;
    }
}
