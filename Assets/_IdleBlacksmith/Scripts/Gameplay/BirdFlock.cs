using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Ambient sky life: small birds cross the yard every few seconds, one at a time or in a
    /// loose pair, wings flapping by z-rotation and the body bobbing on a sine. They spawn off
    /// the left edge and despawn past the right — pure dressing, zero gameplay weight.
    /// </summary>
    public class BirdFlock : MonoBehaviour
    {
        [Tooltip("Prefab with Body/WingL/WingR children, sized for the sky")]
        public GameObject birdPrefab;
        public Vector2 spawnInterval = new Vector2(7f, 16f);
        public Vector2 speedRange = new Vector2(1.8f, 3.2f);
        public Vector2 heightRange = new Vector2(6.5f, 10.5f);
        public Vector2 zRange = new Vector2(-8f, 6f);
        public float despawnX = 16f;
        public int maxAlive = 3;

        class Flyer
        {
            public Transform t;
            public Transform wingL;
            public Transform wingR;
            public float speed;
            public float bobPhase;
            public float flapPhase;
        }

        readonly System.Collections.Generic.List<Flyer> birds = new System.Collections.Generic.List<Flyer>();
        float timer;

        void Update()
        {
            if (birdPrefab == null) return;

            timer -= Time.deltaTime;
            if (timer <= 0f && birds.Count < maxAlive)
            {
                timer = Random.Range(spawnInterval.x, spawnInterval.y);
                int n = Random.value < 0.35f ? 2 : 1;
                for (int i = 0; i < n && birds.Count < maxAlive; i++) Spawn(i);
            }

            for (int i = birds.Count - 1; i >= 0; i--)
            {
                Flyer f = birds[i];
                if (f.t == null) { birds.RemoveAt(i); continue; }

                f.t.position += new Vector3(f.speed * Time.deltaTime, 0f, 0f);
                f.bobPhase += Time.deltaTime * 2.2f;
                Vector3 p = f.t.position;
                p.y += Mathf.Sin(f.bobPhase) * 0.004f;
                f.t.position = p;

                f.flapPhase += Time.deltaTime * 9f;
                float flap = Mathf.Sin(f.flapPhase) * 42f;
                if (f.wingL != null) f.wingL.localEulerAngles = new Vector3(0f, 0f, -flap);
                if (f.wingR != null) f.wingR.localEulerAngles = new Vector3(0f, 0f, flap);

                if (p.x > despawnX)
                {
                    Destroy(f.t.gameObject);
                    birds.RemoveAt(i);
                }
            }
        }

        void Spawn(int index)
        {
            Vector3 pos = new Vector3(
                -despawnX - index * 1.6f,
                Random.Range(heightRange.x, heightRange.y),
                Random.Range(zRange.x, zRange.y));
            GameObject go = Instantiate(birdPrefab, pos, Quaternion.Euler(0f, 90f, 0f), transform);
            float s = Random.Range(0.85f, 1.25f);
            go.transform.localScale = Vector3.one * s;
            var f = new Flyer
            {
                t = go.transform,
                wingL = go.transform.Find("WingL"),
                wingR = go.transform.Find("WingR"),
                speed = Random.Range(speedRange.x, speedRange.y),
                bobPhase = Random.Range(0f, Mathf.PI * 2f),
                flapPhase = Random.Range(0f, Mathf.PI * 2f),
            };
            birds.Add(f);
        }
    }
}
