using System.Collections.Generic;
using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// Draws the rooms: one static mesh per room for rock, ledges and spikes, a cool gradient backdrop with
    /// pillar silhouettes, and the warm landmarks (the great bell, the tide lamp, the winch, the arch lamps).
    /// Dynamic pieces (grate, arena door, lamp glow, pickups) are updated from the world each frame.
    /// </summary>
    public sealed class LevelView
    {
        public readonly Transform Root;
        readonly World world;
        readonly Level lv;
        readonly List<SpriteRenderer> grate = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> doors = new List<SpriteRenderer>();
        readonly SpriteRenderer lampFlame, lampGlow, lampPost, winchArm, winchGlow, lung, lungGlow;
        readonly List<SpriteRenderer> archGlows = new List<SpriteRenderer>();
        readonly List<(RippleBell Bell, SpriteRenderer Shell, SpriteRenderer Signal, List<SpriteRenderer> Waves)> ripples =
            new List<(RippleBell, SpriteRenderer, SpriteRenderer, List<SpriteRenderer>)>();
        float time;

        public LevelView(World w, Transform parent)
        {
            world = w;
            lv = w.Level;
            Root = Art.Group("Level", parent);
            foreach (RoomInfo r in lv.Rooms) BuildRoom(r);
            foreach (RippleBell bell in w.RippleBells)
            {
                Vector3 p = V(bell.Pos);
                var rope = Art.NewSprite("ripple support", Art.Square, Palette.RockSide, -1, Root, 0.125f, 10f);
                rope.transform.position = p + new Vector3(0, 5f);
                var shell = Art.NewSprite("ripple bell", Art.TrainingBell, Color.white, 4, Root, 1.5f, 1.5f);
                shell.transform.position = p;
                var signal = Art.NewSprite("ripple floor tell", Art.Square, Palette.Wave, 5, Root, 0.25f, 0.125f);
                signal.transform.position = new Vector3(bell.Pos.X, bell.Floor + 0.07f);
                ripples.Add((bell, shell, signal, new List<SpriteRenderer>(2)));
            }
            if (w.ArenaRoom < 0)
            {
                foreach (Enemy foe in w.Foes)
                {
                    if (!(foe is Dummy)) continue;
                    var stand = Art.NewSprite("Target stand", Art.Square, Palette.RockEdge, 8, Root, 0.1875f, 0.8f);
                    stand.transform.position = V(foe.Body.Feet) + new Vector3(0, 0.4f);
                    var target = Art.NewSprite("Target face", Art.Ring, Palette.Warm, 9, Root, 0.85f, 0.85f);
                    target.transform.position = V(foe.Body.Pos);
                    var center = Art.NewSprite("Target centre", Art.Circle, Palette.WarmDeep, 10, Root, 0.2f, 0.2f);
                    center.transform.position = target.transform.position;
                }
                return;
            }

            // Grate (the shortcut) and the arena door, drawn as bars so they read as "a way through, but closed".
            for (int y = 0; y < lv.H; y++)
                for (int x = 0; x < lv.W; x++)
                {
                    Tile t = lv.At(x, y);
                    if (t != Tile.Grate && t != Tile.Door) continue;
                    var sr = Art.NewSprite(t == Tile.Grate ? "grate" : "door", Art.Square, t == Tile.Grate ? Palette.WarmDeep : Palette.RockEdge, 2, Root, t == Tile.Grate ? 0.86f : 0.22f, t == Tile.Grate ? 0.86f : 1f);
                    sr.transform.position = new Vector3(x + 0.5f, y + 0.5f);
                    (t == Tile.Grate ? grate : doors).Add(sr);
                }

            // Tide lamp: the checkpoint. Warm, tall, and lit from the start of the room.
            Vector3 lamp = V(lv.Checkpoint);
            lampGlow = Art.NewSprite("lampGlow", Art.Glow, Palette.WarmGlow, -5, Root, 7f, 7f);
            lampGlow.transform.position = lamp + new Vector3(0, 1.6f);
            lampPost = Art.NewSprite("lampPost", Art.Square, Palette.RockEdge, 3, Root, 0.16f, 1.6f);
            lampPost.transform.position = lamp + new Vector3(0, 0.8f);
            lampFlame = Art.NewSprite("lampFlame", Art.Circle, Palette.Warm, 4, Root, 0.42f, 0.55f);
            lampFlame.transform.position = lamp + new Vector3(0, 1.75f);

            // Winch: opens the grate. A warm wheel so it reads as "interactable, part of the way forward".
            Vector3 wn = V(lv.Lever);
            var wheel = Art.NewSprite("winch", Art.Ring, Palette.Warm, 3, Root, 0.9f, 0.9f);
            wheel.transform.position = wn + new Vector3(0, 0.7f);
            winchArm = Art.NewSprite("winchArm", Art.Square, Palette.WarmDeep, 4, Root, 0.12f, 0.8f);
            winchArm.transform.position = wn + new Vector3(0, 0.7f);
            winchGlow = Art.NewSprite("winchGlow", Art.Glow, Palette.A(Palette.Warm, 0.25f), -4, Root, 2.4f, 2.4f);
            winchGlow.transform.position = wn + new Vector3(0, 0.7f);

            // Brass Lung: the optional reward, glinting on its ledge so it can be seen from below.
            Vector3 lg = V(lv.Heart);
            lungGlow = Art.NewSprite("lungGlow", Art.Glow, Palette.A(Palette.Warm, 0.45f), -4, Root, 2.2f, 2.2f);
            lungGlow.transform.position = lg + new Vector3(0, 0.5f);
            lung = Art.NewSprite("lung", Art.Circle, Palette.Warm, 6, Root, 0.5f, 0.62f);
            lung.transform.position = lg + new Vector3(0, 0.5f);

            // Warm arch lamps flank the drop into the bell's chamber.
            foreach (Vec2 a in lv.ArchLamps)
            {
                Vector3 p = V(a);
                var post = Art.NewSprite("arch", Art.Square, Palette.RockEdge, 3, Root, 0.14f, 1.3f);
                post.transform.position = p + new Vector3(0, 0.65f);
                var fl = Art.NewSprite("archFlame", Art.Circle, Palette.Warm, 4, Root, 0.3f, 0.38f);
                fl.transform.position = p + new Vector3(0, 1.38f);
                var gl = Art.NewSprite("archGlow", Art.Glow, Palette.WarmGlow, -5, Root, 4.5f, 4.5f);
                gl.transform.position = p + new Vector3(0, 1.3f);
                archGlows.Add(gl);
            }

            // The great bell: the landmark seen from the first room, and again (alive) in the last.
            foreach (Vec2 b in lv.Landmarks)
            {
                var glow = Art.NewSprite("bellGlow", Art.Glow, new Color(1f, 0.68f, 0.3f, 0.22f), -15, Root, 9f, 9f);
                glow.transform.position = V(b) + new Vector3(0, -0.5f);
                var mb = new Art.MeshBuilder();
                List<Vector2> outline = Art.BellOutline(4.4f, 4.8f);
                for (int k = 0; k < outline.Count; k++) outline[k] += new Vector2(b.X, b.Y - 0.5f);
                mb.Poly(outline, new Color(0.16f, 0.24f, 0.26f));
                mb.Quad(b.X - 2.2f, b.Y - 2.9f, b.X + 2.2f, b.Y - 2.6f, Palette.A(Palette.BellRim, 0.8f));
                mb.Quad(b.X - 0.06f, b.Y + 1.9f, b.X + 0.06f, b.Y + 9f, new Color(0.12f, 0.18f, 0.2f));
                mb.Build("landmarkBell", Root, -14);
            }
        }

        static Vector3 V(Vec2 p) => new Vector3(p.X, p.Y);

        bool Solidish(int x, int y)
        {
            Tile t = lv.At(x, y);
            return t == Tile.Rock;
        }

        void BuildRoom(RoomInfo r)
        {
            var bg = new Art.MeshBuilder();
            bg.Quad(r.MinX, r.MinY, r.MaxX, r.MaxY, Palette.SkyTop);
            for (float x = r.MinX + 2f; x + 5f < r.MaxX; x += 7f)
            {
                for (float y = r.MinY + 3f; y + 8f < r.MaxY; y += 15f)
                {
                    bg.Quad(x, y, x + 5f, y + 6f, Palette.Pillar);
                    bg.Quad(x + 0.5f, y + 0.5f, x + 4.5f, y + 5f, Palette.SkyBottom);
                    for (int step = 0; step < 4; step++)
                    {
                        float inset = step * 0.25f;
                        bg.Quad(x + inset, y + 6f + inset, x + 5f - inset, y + 6.25f + inset, Palette.Pillar);
                        bg.Quad(x + 0.625f + inset, y + 5f + inset, x + 4.375f - inset, y + 5.25f + inset, Palette.SkyBottom);
                    }
                    bg.Quad(x + 2.375f, y + 0.5f, x + 2.625f, y + 5.5f, Palette.Pillar);
                }
            }
            for (float y = r.MinY + 4f; y < r.MaxY; y += 2f)
            {
                float start = r.MinX + ((int)y % 4 == 0 ? 1.5f : 3.5f);
                for (float x = start; x + 0.875f < r.MaxX; x += 9f)
                    bg.Quad(x, y, x + 0.875f, y + 0.0625f, Palette.Pillar);
            }
            float tide = r.MinY + 2.625f;
            bg.Quad(r.MinX, r.MinY, r.MaxX, tide, Palette.A(Palette.RockSide, 0.45f));
            for (float x = r.MinX + 1.5f; x + 1.5f < r.MaxX; x += 4.5f)
                bg.Quad(x, tide, x + 1.5f, tide + 0.0625f, Palette.RockSide);
            bg.Build("backdrop " + r.Name, Root, -20);

            var mb = new Art.MeshBuilder();
            int x0 = (int)r.MinX, x1 = (int)r.MaxX, y0 = (int)r.MinY, y1 = (int)r.MaxY;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    Tile t = lv.At(x, y);
                    if (t == Tile.Rock)
                    {
                        bool open = !Solidish(x, y + 1) || !Solidish(x, y - 1) || !Solidish(x - 1, y) || !Solidish(x + 1, y);
                        // Rock next to open space is lighter; deep rock fades to near-black so rooms read as shapes.
                        mb.Quad(x, y, x + 1, y + 1, open ? Palette.Rock : Palette.RockDeep);
                        if (lv.At(x, y + 1) != Tile.Rock && lv.At(x, y + 1) != Tile.Grate) mb.Quad(x, y + 0.82f, x + 1, y + 1, Palette.RockEdge);
                        if (!Solidish(x - 1, y)) mb.Quad(x, y, x + 0.1f, y + 1, Palette.RockSide);
                        if (!Solidish(x + 1, y)) mb.Quad(x + 0.9f, y, x + 1, y + 1, Palette.RockSide);
                        if (open && (x + y * 13) % 3 != 0)
                        {
                            mb.Quad(x + 0.125f, y + 0.4375f, x + 0.6875f, y + 0.5f, Palette.RockDeep);
                            mb.Quad(x + 0.625f, y + 0.1875f, x + 0.6875f, y + 0.4375f, Palette.RockDeep);
                        }
                    }
                    else if (t == Tile.OneWay)
                    {
                        mb.Quad(x, y + 0.75f, x + 1, y + 1, Palette.LedgeUnder);
                        mb.Quad(x, y + 0.9375f, x + 1, y + 1, Palette.Ledge);
                        mb.Quad(x + 0.125f, y + 0.4375f, x + 0.25f, y + 0.75f, Palette.LedgeUnder);
                    }
                    else if (t == Tile.Spikes)
                    {
                        // Three barnacle spikes per tile, coral so they never read as floor.
                        for (int k = 0; k < 3; k++)
                        {
                            float sx = x + k / 3f;
                            mb.Tri(new Vector2(sx + 0.02f, y), new Vector2(sx + 0.31f, y), new Vector2(sx + 0.165f, y + 0.72f), Palette.Spike);
                        }
                        mb.Quad(x, y, x + 1, y + 0.12f, Palette.SpikeDark);
                    }
                }
            mb.Build("tiles " + r.Name, Root, 0);
        }

        public void Tick(float dt)
        {
            if (world.ArenaRoom < 0) return;
            time += dt;
            foreach (var view in ripples)
            {
                float windup = view.Bell.Windup;
                view.Shell.transform.rotation = Quaternion.Euler(0, 0, windup * 22f * Mathf.Sin(time * 17f));
                view.Shell.color = Color.Lerp(Color.white, new Color(1f, 0.74f, 0.43f), windup);
                view.Signal.enabled = windup > 0f;
                view.Signal.transform.localScale = new Vector3(0.25f + windup * 2f, 0.125f, 1f);
                while (view.Waves.Count < view.Bell.Waves.Count)
                    view.Waves.Add(Art.NewSprite("ripple wave", Art.Dome, Palette.Wave, 26, Root));
                for (int i = 0; i < view.Waves.Count; i++)
                {
                    SpriteRenderer sprite = view.Waves[i];
                    sprite.enabled = i < view.Bell.Waves.Count;
                    if (sprite.enabled) Art.DrawWave(sprite, view.Bell.Waves[i], world.BossT.WaveHeight, time);
                }
            }
            foreach (SpriteRenderer g in grate) g.enabled = !lv.GrateOpen;
            foreach (SpriteRenderer d in doors) d.enabled = lv.DoorsLocked;

            float flicker = 1f + 0.08f * Mathf.Sin(time * 11f) + 0.05f * Mathf.Sin(time * 4.3f);
            bool lit = world.LampLit;
            lampFlame.color = lit ? Palette.Warm : Palette.A(Palette.WarmDeep, 0.6f);
            lampFlame.transform.localScale = new Vector3(0.42f, 0.55f * (lit ? flicker : 0.7f), 1f);
            lampGlow.color = Palette.A(Palette.Warm, lit ? 0.32f * flicker : 0.1f);

            float turn = lv.GrateOpen ? 140f : 0f;
            winchArm.transform.rotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(winchArm.transform.eulerAngles.z, turn, dt * 4f));
            winchGlow.enabled = !lv.GrateOpen;
            winchGlow.color = Palette.A(Palette.Warm, 0.18f + 0.1f * Mathf.Sin(time * 3f));

            lung.enabled = lungGlow.enabled = !world.LungTaken;
            lung.transform.localPosition = V(lv.Heart) + new Vector3(0, 0.55f + 0.08f * Mathf.Sin(time * 2.4f));
            foreach (SpriteRenderer a in archGlows) a.color = Palette.A(Palette.Warm, 0.3f * flicker);
        }
    }
}
