using System;

namespace DrownedBell.Core
{
    /// <summary>Minimal 2D vector so the simulation never depends on UnityEngine. Units are tiles; y points up.</summary>
    public struct Vec2
    {
        public float X, Y;
        public Vec2(float x, float y) { X = x; Y = y; }
        public float Length => MathF.Sqrt(X * X + Y * Y);
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
        public override string ToString() => "(" + X.ToString("0.00") + ", " + Y.ToString("0.00") + ")";
    }

    /// <summary>Axis-aligned box given by its centre and half size.</summary>
    public struct Aabb
    {
        public Vec2 Center;
        public Vec2 Half;
        public Aabb(Vec2 center, Vec2 half) { Center = center; Half = half; }
        public float MinX => Center.X - Half.X;
        public float MaxX => Center.X + Half.X;
        public float MinY => Center.Y - Half.Y;
        public float MaxY => Center.Y + Half.Y;
        public bool Overlaps(Aabb o) =>
            MathF.Abs(Center.X - o.Center.X) < Half.X + o.Half.X && MathF.Abs(Center.Y - o.Center.Y) < Half.Y + o.Half.Y;
        public bool Contains(Vec2 p) => MathF.Abs(p.X - Center.X) <= Half.X && MathF.Abs(p.Y - Center.Y) <= Half.Y;
    }

    public static class Mathx
    {
        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
        public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        public static float MoveToward(float v, float target, float step)
        {
            if (v < target) return MathF.Min(v + step, target);
            return MathF.Max(v - step, target);
        }
        public static int Sign(float v) => v > 0f ? 1 : v < 0f ? -1 : 0;
    }
}
