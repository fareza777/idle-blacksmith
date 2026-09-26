using IdleBlacksmith.Core;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// The forge cat: mostly asleep by the hearth, occasionally stretching up to wander a
    /// few steps before flopping down again. Tail flicks and drifting "z"s while napping —
    /// the cheapest charm in the game.
    /// </summary>
    public class CatAmbient : MonoBehaviour
    {
        public Transform tail;
        public float wanderRadius = 1.7f;
        public float walkSpeed = 0.55f;
        public Vector2 restTime = new Vector2(14f, 30f);
        public Vector2 sitTime = new Vector2(4f, 9f);

        enum CatState { Rest, Walk, Sit }

        CatState state = CatState.Rest;
        Vector3 homePos;
        Vector3 target;
        float timer;
        float zzTimer = 3f;

        void Start()
        {
            homePos = transform.position;
            timer = Random.Range(restTime.x, restTime.y);
        }

        /// <summary>Tap the cat: a happy bounce, hearts, and a purr. No gameplay effect — only joy.</summary>
        public void Pet()
        {
            Tween.PunchScale(transform, Vector3.one * 0.18f, 0.45f);
            if (tail != null) Tween.PunchScale(tail, Vector3.one * 0.35f, 0.5f);
            AudioManager.Play("pop", 0.12f, 0.4f);
            UI.SettingsPanel.Buzz();
            Vector3 up = transform.position + Vector3.up * 0.6f;
            UI.UIManager.Instance?.SpawnFloatingText(up, "\u2665", new Color(1f, 0.45f, 0.55f));
            UI.UIManager.Instance?.SpawnFloatingText(up + new Vector3(0.25f, 0.15f, 0f), "purr", new Color(1f, 0.7f, 0.75f));
            var gm = GameManager.Instance;
            if (gm != null && gm.Data != null && gm.Data.stats != null) gm.Data.stats.catPets++;
            // a petted cat purrs a while longer where it lies
            state = CatState.Rest;
            timer = Random.Range(restTime.x, restTime.y);
        }

        void Update()
        {
            switch (state)
            {
                case CatState.Rest: TickRest(); break;
                case CatState.Walk: TickWalk(); break;
                case CatState.Sit:
                    timer -= Time.deltaTime;
                    if (timer <= 0f) { target = homePos; state = CatState.Walk; }
                    break;
            }
        }

        void TickRest()
        {
            timer -= Time.deltaTime;

            zzTimer -= Time.deltaTime;
            if (zzTimer <= 0f)
            {
                zzTimer = 3.6f;
                UI.UIManager.Instance?.SpawnFloatingText(
                    transform.position + Vector3.up * 0.5f, "z", new Color(0.55f, 0.62f, 0.78f));
            }

            // tail flicks about once every five seconds — a cat is never fully asleep
            if (tail != null && Random.value < Time.deltaTime / 5f)
                Tween.PunchScale(tail, Vector3.one * 0.25f, 0.5f);

            if (timer <= 0f)
            {
                Vector2 o = Random.insideUnitCircle * wanderRadius;
                target = homePos + new Vector3(o.x, 0f, o.y);
                state = CatState.Walk;
            }
        }

        void TickWalk()
        {
            Vector3 p = transform.position;
            Vector3 flat = new Vector3(target.x - p.x, 0f, target.z - p.z);
            float dist = flat.magnitude;
            if (dist < 0.05f)
            {
                bool goingHome = (target - homePos).sqrMagnitude < 0.01f;
                if (goingHome || Random.value < 0.55f)
                {
                    state = CatState.Rest;
                    timer = Random.Range(restTime.x, restTime.y);
                }
                else
                {
                    state = CatState.Sit;
                    timer = Random.Range(sitTime.x, sitTime.y);
                }
                transform.position = new Vector3(target.x, homePos.y, target.z);
                return;
            }

            flat /= dist;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(flat), 8f * Time.deltaTime);
            p += flat * (walkSpeed * Time.deltaTime);
            p.y = homePos.y + Mathf.Abs(Mathf.Sin(Time.time * 11f)) * 0.02f; // pad-pad-pad
            transform.position = p;
        }
    }
}
