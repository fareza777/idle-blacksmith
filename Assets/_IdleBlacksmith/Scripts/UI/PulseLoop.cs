using UnityEngine;

namespace IdleBlacksmith.UI
{
    /// <summary>Gentle idle scale pulse to draw attention (e.g. the Upgrades button).</summary>
    public class PulseLoop : MonoBehaviour
    {
        public float amplitude = 0.045f;
        public float speed = 2.6f;
        public bool startStopped;

        Vector3 baseScale = Vector3.one;
        bool captured;
        bool stopped;

        void OnEnable()
        {
            if (!captured)
            {
                baseScale = transform.localScale;
                captured = true;
            }
            stopped = startStopped;
        }

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

        /// <summary>Drive the pulse on/off — when turned off the scale eases back to rest.</summary>
        public void SetActive(bool on) => stopped = !on;
    }
}
