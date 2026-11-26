using System.Collections.Generic;
using UnityEngine;

namespace DrownedBell.Game
{
    /// <summary>Cool water tones for the world, warm brass only for lamps, landmarks and the way forward, coral for danger.</summary>
    public static class Palette
    {
        public static readonly Color SkyTop = Hex(0x142028), SkyBottom = Hex(0x0b0f14);
        public static readonly Color Rock = Hex(0x1f3340), RockDeep = Hex(0x142028), RockEdge = Hex(0x4b7378), RockSide = Hex(0x2f4b55);
        public static readonly Color Ledge = Hex(0x8fb3a8), LedgeUnder = Hex(0x2f4b55);
        public static readonly Color Spike = Hex(0xe0584a), SpikeDark = Hex(0x8c4a2f);
        public static readonly Color Warm = Hex(0xf2c14e), WarmDeep = Hex(0xd79a3b), WarmGlow = new Color(1f, 0.7f, 0.3f, 0.35f);
        public static readonly Color Pillar = Hex(0x1f3340), PillarFar = Hex(0x142028);
        public static readonly Color Water = new Color(0.25f, 0.55f, 0.65f, 0.18f);
        public static readonly Color Suit = Hex(0xe6e1cf), Visor = Hex(0x142028), VisorGlow = Hex(0x8fb3a8), Tool = Hex(0x4b7378);
        public static readonly Color Crab = Hex(0xe0584a), CrabDark = Hex(0x8c4a2f), Danger = Hex(0xe0584a);
        public static readonly Color Jelly = Hex(0x6e5a9e), JellyDim = Hex(0x6e5a9e), JellyRing = Hex(0xb7a6e0);
        public static readonly Color Bell = Hex(0x4b7378), BellDark = Hex(0x2f4b55), BellRim = Hex(0xd79a3b);
        public static readonly Color Wave = Hex(0x8fb3a8), Drip = Hex(0x8fb3a8);
        public static readonly Color Text = Hex(0xe6e1cf), TextDim = Hex(0x8fb3a8);

        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        public static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }

    /// <summary>Sprites, meshes and the shared material, all generated in code. No art files.</summary>
    public static class Art
    {
        static Sprite square, circle, glow, ring, dome, crescent;
        static Sprite trainingBell;
        static Material material;

        public static Material Material
        {
            get
            {
                if (material == null) material = new Material(Shader.Find("Sprites/Default"));
                return material;
            }
        }

        public static Sprite Square => square ??= Make(4, (u, v) => 1f, FilterMode.Point);
        public static Sprite Circle => circle ??= Make(12, (u, v) => Len(u, v) <= 1f ? 1f : 0f);
        /// <summary>Soft radial falloff for glows and lights.</summary>
        public static Sprite Glow => glow ??= Make(24, (u, v) => Mathf.Floor(Mathf.Clamp01(1f - Len(u, v)) * 5f) / 5f);
        /// <summary>A thin ring; at scale s its outer radius is s/2.</summary>
        public static Sprite Ring => ring ??= Make(48, (u, v) => Len(u, v) is <= 1f and >= 0.88f ? 1f : 0f);
        /// <summary>Upper half of a disc, pivot at the bottom-centre of the flat edge.</summary>
        public static Sprite Dome => dome ??= Make(16, (u, v) => v >= 0f && Len(u, v) <= 1f ? 1f : 0f);
        /// <summary>A slash arc pointing right, used for the hook swing.</summary>
        public static Sprite Crescent => crescent ??= Make(32, (u, v) =>
        {
            float outer = Edge(1f - Len(u, v), 128);
            float inner = Edge(Len(u - 0.35f, v * 1.1f) - 0.78f, 128);
            return u > -0.2f && outer * inner > 0.45f ? 1f : 0f;
        });

        public static Sprite TrainingBell => trainingBell ??= PixelSprite(new[]
        {
            ".....ss.....",
            ".....bb.....",
            "....ssss....",
            "...sbbbbs...",
            "...seatbs...",
            "...seatbs...",
            "...seatbs...",
            "..sseabsss..",
            "..saabbbbs..",
            ".saaaaabbbs.",
            ".ssssssssss.",
            "....sabs....",
        }, new Dictionary<char, Color32>
        {
            ['s'] = new Color32(11, 15, 20, 255),
            ['b'] = new Color32(140, 74, 47, 255),
            ['a'] = new Color32(215, 154, 59, 255),
            ['e'] = new Color32(242, 193, 78, 255),
            ['t'] = new Color32(47, 75, 85, 255),
        });

        internal static Sprite PixelSprite(string[] rows, Dictionary<char, Color32> inks)
        {
            int width = rows[0].Length, height = rows.Length;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (inks.TryGetValue(rows[y][x], out Color32 color))
                        pixels[(height - y - 1) * width + x] = color;
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
        }

        public static Vector3 Snap(Vector3 point) =>
            new Vector3(Mathf.Round(point.x * 16f) / 16f, Mathf.Round(point.y * 16f) / 16f, point.z);

        public static void DrawWave(SpriteRenderer sprite, Core.Shockwave wave, float height, float time)
        {
            sprite.transform.position = new Vector3(wave.X, wave.FloorY + 0.02f);
            sprite.transform.localScale = new Vector3(0.9f, height * 2f * (0.85f + 0.15f * Mathf.Sin(time * 30f)), 1f);
            sprite.color = Palette.A(Palette.Wave, 0.9f);
        }

        static float Len(float u, float v) => Mathf.Sqrt(u * u + v * v);
        static float Edge(float d, int n) => Mathf.Clamp01(d * n * 0.5f);

        /// <summary>Square texture of n x n, alpha from f(u, v) with u, v in [-1, 1].</summary>
        static Sprite Make(int n, System.Func<float, float, float> f, FilterMode mode = FilterMode.Point, Vector2? pivot = null)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = mode, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(f(u, v)) * 255f));
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), pivot ?? new Vector2(0.5f, 0.5f), n);
        }

        public static SpriteRenderer NewSprite(string name, Sprite s, Color c, int order, Transform parent, float sx = 1f, float sy = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(sx, sy, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

        public static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>A static, vertex-coloured mesh built from quads and triangles.</summary>
        public sealed class MeshBuilder
        {
            readonly List<Vector3> v = new List<Vector3>();
            readonly List<Color> c = new List<Color>();
            readonly List<int> i = new List<int>();
            public int Count => v.Count;

            public void Quad(float x0, float y0, float x1, float y1, Color bottom, Color top)
            {
                int b = v.Count;
                v.Add(new Vector3(x0, y0)); v.Add(new Vector3(x1, y0)); v.Add(new Vector3(x1, y1)); v.Add(new Vector3(x0, y1));
                c.Add(bottom); c.Add(bottom); c.Add(top); c.Add(top);
                i.Add(b); i.Add(b + 2); i.Add(b + 1); i.Add(b); i.Add(b + 3); i.Add(b + 2);
            }

            public void Quad(float x0, float y0, float x1, float y1, Color col) => Quad(x0, y0, x1, y1, col, col);

            public void Tri(Vector2 a, Vector2 b2, Vector2 c2, Color col)
            {
                int b = v.Count;
                v.Add(a); v.Add(b2); v.Add(c2);
                c.Add(col); c.Add(col); c.Add(col);
                i.Add(b); i.Add(b + 2); i.Add(b + 1);
                i.Add(b); i.Add(b + 1); i.Add(b + 2);
            }

            /// <summary>Convex polygon as a triangle fan.</summary>
            public void Poly(IList<Vector2> pts, Color col)
            {
                int b = v.Count;
                foreach (Vector2 p in pts) { v.Add(p); c.Add(col); }
                for (int k = 1; k + 1 < pts.Count; k++)
                {
                    i.Add(b); i.Add(b + k); i.Add(b + k + 1);
                    i.Add(b); i.Add(b + k + 1); i.Add(b + k);
                }
            }

            public MeshRenderer Build(string name, Transform parent, int order)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(v);
                mesh.SetColors(c);
                mesh.SetTriangles(i, 0);
                mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = Material;
                mr.sortingOrder = order;
                return mr;
            }
        }

        /// <summary>Outline of a bell, centred on (0, 0): flared mouth at the bottom, dome on top.</summary>
        public static List<Vector2> BellOutline(float w, float h)
        {
            var pts = new List<Vector2>();
            float hw = w * 0.5f, hh = h * 0.5f;
            pts.Add(new Vector2(-hw, -hh));
            pts.Add(new Vector2(hw, -hh));
            pts.Add(new Vector2(hw * 0.82f, -hh + h * 0.18f));
            pts.Add(new Vector2(hw * 0.66f, hh - h * 0.38f));
            for (int k = 0; k <= 8; k++)
            {
                float a = Mathf.PI * k / 8f;
                pts.Add(new Vector2(Mathf.Cos(a) * hw * 0.66f, hh - h * 0.38f + Mathf.Sin(a) * h * 0.38f));
            }
            pts.Add(new Vector2(-hw * 0.66f, hh - h * 0.38f));
            pts.Add(new Vector2(-hw * 0.82f, -hh + h * 0.18f));
            return pts;
        }
    }
}
