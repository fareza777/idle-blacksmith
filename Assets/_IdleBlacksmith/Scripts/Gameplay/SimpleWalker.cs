using System.Collections;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Flat-ground locomotion: smooth acceleration near arrival, smooth turning,
    /// and drives the Animator "Speed" float so Idle/Walk transitions blend properly.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class SimpleWalker : MonoBehaviour
    {
        static readonly int SpeedHash = Animator.StringToHash("Speed");

        public float turnSpeed = 12f;

        [Tooltip("How fast a step between ground levels is taken, in units per second. The yard sits " +
                 "a few centimetres below the shop floor, and without this the walker would keep " +
                 "whatever height it spawned at and appear to float off the grass.")]
        public float stepSpeed = 1.2f;

        Animator anim;

        void Awake()
        {
            if (anim == null) anim = GetComponent<Animator>();
        }

        public IEnumerator MoveTo(Vector3 target, float speed, float arriveDist = 0.07f)
        {
            while (true)
            {
                Vector3 pos = transform.position;
                Vector3 to = target - pos;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist <= arriveDist) break;

                float effective = speed * Mathf.Clamp01(dist * 2f + 0.2f);
                Vector3 next = Vector3.MoveTowards(pos, new Vector3(target.x, pos.y, target.z),
                    effective * Time.deltaTime);

                // Height is eased separately so stepping between the grass and the floor reads as a
                // step rather than a jump, and never blocks arrival in the horizontal plane.
                next.y = Mathf.MoveTowards(pos.y, target.y, stepSpeed * Time.deltaTime);
                transform.position = next;

                if (to.sqrMagnitude > 0.0001f)
                {
                    Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
                }
                SetSpeedNorm(speed > 0.001f ? Mathf.Clamp01(effective / speed) : 0f);
                yield return null;
            }
            transform.position = target;
            SetSpeedNorm(0f);
        }

        public void FaceTowards(Vector3 worldPos, float duration = 0.18f)
        {
            Vector3 to = worldPos - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return;
            Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
            Tween.Rotation(transform, look, duration, Ease.OutQuad);
        }

        public void SetSpeedNorm(float v)
        {
            if (anim != null) anim.SetFloat(SpeedHash, v);
        }
    }
}
