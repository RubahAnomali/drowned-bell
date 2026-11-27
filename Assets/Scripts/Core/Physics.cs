using System;

namespace DrownedBell.Core
{
    /// <summary>A moving box. Position is the centre; the box is Half.X wide each way and Half.Y tall each way.</summary>
    public sealed class Body
    {
        public Vec2 Pos;
        /// <summary>Where the body stood at the start of the last simulation step. Only the renderer reads it, to draw smoothly between the 60 Hz steps.</summary>
        public Vec2 Prev;
        public Vec2 Vel;
        public Vec2 Half;
        public bool Grounded;
        /// <summary>Standing on a one-way ledge rather than rock (so down+jump can drop through).</summary>
        public bool OnOneWay;
        public bool HitWall, HitCeiling, Landed;
        /// <summary>While positive, one-way ledges are ignored.</summary>
        public float DropThrough;

        public Body(Vec2 pos, float w, float h)
        {
            Pos = Prev = pos;
            Half = new Vec2(w * 0.5f, h * 0.5f);
        }

        /// <summary>The position `alpha` of the way from the last step to this one (0 = Prev, 1 = Pos).</summary>
        public Vec2 Lerp(float alpha) => Prev + (Pos - Prev) * alpha;

        public Aabb Box => new Aabb(Pos, Half);
        public float Bottom => Pos.Y - Half.Y;
        public Vec2 Feet => new Vec2(Pos.X, Pos.Y - Half.Y);
    }

    /// <summary>
    /// Tile collision for boxes. Each axis moves separately in steps no longer than 0.2 tiles, so a fast dash
    /// cannot tunnel through a one-tile wall. One-way ledges only stop a box that was above them and is falling.
    /// </summary>
    public static class Physics
    {
        const float Step = 0.2f;
        const float Skin = 1e-3f;

        public static void Move(Level lv, Body b, float dt)
        {
            bool wasGrounded = b.Grounded;
            b.HitWall = b.HitCeiling = b.Landed = false;
            if (b.DropThrough > 0f) b.DropThrough -= dt;

            float dx = b.Vel.X * dt;
            int nx = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(dx) / Step));
            for (int i = 0; i < nx; i++)
            {
                float next = b.Pos.X + dx / nx;
                var box = new Aabb(new Vec2(next, b.Pos.Y), b.Half);
                if (lv.OverlapsSolid(box))
                {
                    if (dx > 0f) b.Pos.X = MathF.Floor(next + b.Half.X) - b.Half.X - Skin;
                    else b.Pos.X = MathF.Floor(next - b.Half.X) + 1f + b.Half.X + Skin;
                    b.Vel.X = 0f;
                    b.HitWall = true;
                    break;
                }
                b.Pos.X = next;
            }

            float dy = b.Vel.Y * dt;
            int ny = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(dy) / Step));
            for (int i = 0; i < ny; i++)
            {
                float next = b.Pos.Y + dy / ny;
                var box = new Aabb(new Vec2(b.Pos.X, next), b.Half);
                bool blocked = lv.OverlapsSolid(box);
                if (!blocked && dy < 0f && b.DropThrough <= 0f) blocked = LandsOnOneWay(lv, b, next);
                if (blocked)
                {
                    if (dy > 0f)
                    {
                        b.Pos.Y = MathF.Floor(next + b.Half.Y) - b.Half.Y - Skin;
                        b.HitCeiling = true;
                    }
                    else b.Pos.Y = MathF.Floor(next - b.Half.Y) + 1f + b.Half.Y + Skin;
                    b.Vel.Y = 0f;
                    break;
                }
                b.Pos.Y = next;
            }

            b.Grounded = Supported(lv, b, out b.OnOneWay);
            if (b.Grounded && !wasGrounded) b.Landed = true;
        }

        /// <summary>Would moving the box down to `nextY` put its feet through the top of a one-way ledge it was above?</summary>
        static bool LandsOnOneWay(Level lv, Body b, float nextY)
        {
            float oldBottom = b.Pos.Y - b.Half.Y;
            float newBottom = nextY - b.Half.Y;
            int x0 = (int)MathF.Floor(b.Pos.X - b.Half.X), x1 = (int)MathF.Floor(b.Pos.X + b.Half.X - 1e-4f);
            // The only ledge top a single step can cross is the integer just at or below the old feet.
            float top = MathF.Floor(oldBottom + 0.01f);
            if (newBottom >= top || oldBottom < top - 0.01f) return false;
            int ty = (int)top - 1;
            for (int x = x0; x <= x1; x++) if (lv.OneWay(x, ty)) return true;
            return false;
        }

        /// <summary>Something to stand on directly under the feet (rock, closed grate or door, or a one-way ledge top).</summary>
        public static bool Supported(Level lv, Body b, out bool oneWayOnly)
        {
            oneWayOnly = false;
            float bottom = b.Pos.Y - b.Half.Y;
            int ty = (int)MathF.Floor(bottom - 0.02f);
            if (bottom - (ty + 1f) > 0.02f) return false;
            int x0 = (int)MathF.Floor(b.Pos.X - b.Half.X + 0.01f), x1 = (int)MathF.Floor(b.Pos.X + b.Half.X - 0.01f);
            bool solid = false, oneWay = false;
            for (int x = x0; x <= x1; x++)
            {
                if (lv.Solid(x, ty)) solid = true;
                else if (lv.OneWay(x, ty) && b.DropThrough <= 0f) oneWay = true;
            }
            oneWayOnly = oneWay && !solid;
            return solid || oneWay;
        }
    }
}
