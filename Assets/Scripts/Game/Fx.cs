using System.Collections.Generic;
using UnityEngine;

namespace DrownedBell.Game
{
    /// <summary>Pooled particles and expanding rings: dust, sparks, splashes, embers. Purely cosmetic.</summary>
    public sealed class Fx
    {
        sealed class P
        {
            public SpriteRenderer Sr;
            public Vector2 Vel;
            public float Age, Life, Size, Grow, Gravity;
            public Color Color;
        }

        readonly Transform root;
        readonly List<P> live = new List<P>();
        readonly Stack<P> free = new Stack<P>();

        public Fx(Transform parent)
        {
            root = Art.Group("Fx", parent);
        }

        P Take(Sprite s)
        {
            P p = free.Count > 0 ? free.Pop() : new P { Sr = Art.NewSprite("fx", s, Color.white, 30, root) };
            p.Sr.sprite = s;
            p.Sr.enabled = true;
            p.Age = 0f;
            live.Add(p);
            return p;
        }

        public void Burst(Vector2 at, Color c, int n, float speed, float size = 0.14f, float gravity = 8f, float spreadUp = 0f)
        {
            for (int i = 0; i < n; i++)
            {
                P p = Take(Art.Circle);
                float a = Random.value * Mathf.PI * 2f;
                float v = speed * (0.35f + Random.value * 0.75f);
                p.Vel = new Vector2(Mathf.Cos(a) * v, Mathf.Abs(Mathf.Sin(a)) * v * (spreadUp > 0f ? 1f : Mathf.Sign(Mathf.Sin(a))) + spreadUp);
                p.Life = 0.35f + Random.value * 0.35f;
                p.Size = size * (0.6f + Random.value * 0.8f);
                p.Grow = 0f;
                p.Gravity = gravity;
                p.Color = c;
                p.Sr.transform.position = at;
            }
        }

        public void Ring(Vector2 at, Color c, float radius, float life)
        {
            P p = Take(Art.Ring);
            p.Vel = Vector2.zero;
            p.Life = life;
            p.Size = 0.3f;
            p.Grow = radius * 2f;
            p.Gravity = 0f;
            p.Color = c;
            p.Sr.transform.position = at;
        }

        public void Puff(Vector2 at, Color c, float size, float life)
        {
            P p = Take(Art.Glow);
            p.Vel = Vector2.zero;
            p.Life = life;
            p.Size = size * 0.4f;
            p.Grow = size;
            p.Gravity = 0f;
            p.Color = c;
            p.Sr.transform.position = at;
        }

        public void Clear()
        {
            foreach (P p in live) { p.Sr.enabled = false; free.Push(p); }
            live.Clear();
        }

        public void Tick(float dt)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                P p = live[i];
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    p.Sr.enabled = false;
                    free.Push(p);
                    live.RemoveAt(i);
                    continue;
                }
                float k = p.Age / p.Life;
                p.Vel.y -= p.Gravity * dt;
                p.Sr.transform.position += (Vector3)(p.Vel * dt);
                float s = p.Grow > 0f ? Mathf.Lerp(p.Size, p.Grow, 1f - (1f - k) * (1f - k)) : p.Size * (1f - k * 0.6f);
                p.Sr.transform.localScale = new Vector3(s, s, 1f);
                p.Sr.color = new Color(p.Color.r, p.Color.g, p.Color.b, p.Color.a * (1f - k));
            }
        }
    }
}
