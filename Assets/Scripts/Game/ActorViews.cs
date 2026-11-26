using System.Collections.Generic;
using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// The salvage diver: a big round helmet on a small body, a lit visor, and a hooked spanner.
    /// Squash and stretch on jump and land, a crescent slash on each swing, flicker while invulnerable.
    /// </summary>
    public sealed class PlayerView
    {
        readonly Transform root, rig;
        readonly SpriteRenderer diver, tool, slash;
        readonly World w;
        float pose = 1f, poseAge = 1f, time, slashAge = 9f, hurtAge = 1f;
        AttackDir slashDir;
        int slashFacing = 1;

        public PlayerView(World w, Transform parent)
        {
            this.w = w;
            root = Art.Group("Diver", parent);
            rig = Art.Group("rig", root);
            diver = Art.NewSprite("diver sprite", PixelArt.Diver, Color.white, 21, rig, 1.0625f, 1.0625f);
            tool = Art.NewSprite("hook", PixelArt.Hook, Color.white, 18, rig, 0.625f, 0.625f);
            slash = Art.NewSprite("slash", Art.Crescent, new Color(1f, 1f, 1f, 0f), 25, root, 2.2f, 1.6f);
        }

        public void OnSwing(AttackDir dir, int facing)
        {
            slashAge = 0f;
            slashDir = dir;
            slashFacing = facing;
        }

        public void OnJump() { pose = 1.19f; poseAge = 0f; }
        public void OnLand() { pose = 0.81f; poseAge = 0f; }
        public void OnHurt() { hurtAge = 0f; }

        public void Tick(float dt, bool dying, bool reducedFlashing)
        {
            time += dt;
            slashAge += dt;
            Player p = w.Player;
            Vector3 pos = new Vector3(p.Body.Pos.X, p.Body.Pos.Y);
            root.position = Art.Snap(pos);
            root.gameObject.SetActive(!dying);
            poseAge += dt;
            hurtAge += dt;
            float progress = Mathf.Clamp01(poseAge / 0.18f) - 1f;
            float eased = 1f + 2.70158f * progress * progress * progress + 1.70158f * progress * progress;
            float squash = Mathf.LerpUnclamped(pose, 1f, eased);
            float run = p.Body.Grounded ? Mathf.Abs(p.Body.Vel.X) / w.Move.RunSpeed : 0f;
            float bob = Mathf.Sin(time * 18f) * 0.05f * run;
            rig.localScale = new Vector3(p.Facing / squash, squash, 1f);
            rig.localPosition = new Vector3(0, bob - (1f - squash) * 0.3f);
            if (p.Dashing) rig.localRotation = Quaternion.Euler(0, 0, -p.Facing * 12f);
            else rig.localRotation = Quaternion.Euler(0, 0, -p.Body.Vel.X * 0.6f);

            diver.sprite = !p.Body.Grounded ? PixelArt.DiverAir :
                run > 0.15f && Mathf.FloorToInt(time * 10f) % 2 == 0 ? PixelArt.DiverStride : PixelArt.Diver;

            // The hook rests on the back and swings round during an attack.
            float swing = p.Attacking ? Mathf.Clamp01(p.AttackTime / (w.Combat.AttackWindup + w.Combat.AttackActive)) : 0f;
            float restAngle = 35f, endAngle = p.Attack == AttackDir.Up ? -30f : p.Attack == AttackDir.Down ? 200f : 110f;
            float angle = p.Attacking ? Mathf.Lerp(restAngle, endAngle, swing) : restAngle;
            tool.transform.localPosition = p.Attacking ? new Vector3(0.25f, 0f) : new Vector3(-0.28f, -0.05f);
            tool.transform.localRotation = Quaternion.Euler(0, 0, -angle);

            Color impact = reducedFlashing ? Palette.Danger : Color.white;
            Color suit = Color.Lerp(Color.white, impact, 1f - Mathf.Clamp01(hurtAge / 0.12f));
            diver.color = p.Invuln > 0f ? Color.Lerp(suit, Palette.WarmDeep, 0.22f) : suit;

            // The slash crescent lives for the active window plus a short fade.
            float life = w.Combat.AttackWindup + w.Combat.AttackActive + 0.05f;
            float a = slashAge < life ? 1f - slashAge / life : 0f;
            slash.color = new Color(0.9f, 1f, 1f, a * 0.85f);
            float reach = w.Combat.AttackReach;
            switch (slashDir)
            {
                case AttackDir.Up:
                    slash.transform.localPosition = new Vector3(0, p.Body.Half.Y + reach * 0.45f);
                    slash.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                    break;
                case AttackDir.Down:
                    slash.transform.localPosition = new Vector3(0, -p.Body.Half.Y - reach * 0.45f);
                    slash.transform.localRotation = Quaternion.Euler(0, 0, -90f);
                    break;
                default:
                    slash.transform.localPosition = new Vector3(slashFacing * (p.Body.Half.X + reach * 0.4f), 0);
                    slash.transform.localRotation = Quaternion.Euler(0, slashFacing > 0 ? 0f : 180f, 0);
                    break;
            }
            slash.transform.localScale = new Vector3(reach * 1.5f, reach * 1.2f, 1f);
        }
    }

    /// <summary>Brine Crab: wide and low with two claws. The front claw rises and glows coral during the wind-up.</summary>
    public sealed class CrabView
    {
        readonly Crab c;
        readonly Transform root;
        readonly SpriteRenderer shell, clawFront, tell;

        public CrabView(Crab c, Transform parent)
        {
            this.c = c;
            root = Art.Group("Brine Crab " + c.Id, parent);
            shell = Art.NewSprite("crab sprite", PixelArt.Crab, Color.white, 10, root, 1.875f, 1.875f);
            clawFront = Art.NewSprite("clawFront", Art.Circle, Palette.Crab, 11, root, 0.62f, 0.52f);
            tell = Art.NewSprite("tell", Art.Glow, Palette.A(Palette.Danger, 0f), 13, root, 1.2f, 1.2f);
        }

        public void Tick(float dt, bool reducedFlashing)
        {
            root.gameObject.SetActive(c.Alive);
            if (!c.Alive) return;
            Vector3 p = new Vector3(c.Body.Pos.X, c.Body.Pos.Y);
            root.position = Art.Snap(p);
            int f = c.Facing;
            float wind = c.WindupProgress;
            bool lunge = c.State == CrabState.Lunge, recover = c.State == CrabState.Recover;
            float squat = lunge ? 0.85f : 1f - wind * 0.15f;
            shell.transform.localPosition = new Vector3(-f * wind * 0.15f, 0.125f);
            shell.transform.localScale = new Vector3(f * 1.875f, 1.875f * squat, 1f);
            // Front claw: raised high during the wind-up, thrust out on the lunge, dragging in recovery.
            Vector3 claw = new Vector3(f * 0.78f, -0.05f);
            if (wind > 0f) claw = new Vector3(f * (0.75f - wind * 0.2f), -0.05f + wind * 0.55f);
            if (lunge) claw = new Vector3(f * 1.25f, -0.1f);
            if (recover) claw = new Vector3(f * 0.85f, -0.3f);
            clawFront.transform.localPosition = claw;
            Color flash = c.Flash > 0f && reducedFlashing ? Palette.Warm : Color.white;
            shell.color = recover ? Color.Lerp(flash, Palette.CrabDark, 0.4f) : flash;
            clawFront.color = Color.Lerp(Palette.SpikeDark, Palette.Danger, Mathf.Max(wind, lunge ? 1f : 0f));
            tell.transform.localPosition = claw;
            tell.color = Palette.A(Palette.Danger, wind * 0.6f + (lunge ? 0.5f : 0f));
        }
    }

    /// <summary>Knell Jelly: a floating violet bell. Swells and brightens during the wind-up, shows the ring it is about to send, then sags grey.</summary>
    public sealed class JellyView
    {
        readonly Jelly j;
        readonly EnemyTuning t;
        readonly Transform root;
        readonly SpriteRenderer dome, preview, ring;
        float time;

        public JellyView(Jelly j, EnemyTuning t, Transform parent)
        {
            this.j = j;
            this.t = t;
            root = Art.Group("Knell Jelly " + j.Id, parent);
            dome = Art.NewSprite("jelly sprite", PixelArt.Jelly, Color.white, 10, root, 1.375f, 1.375f);
            preview = Art.NewSprite("preview", Art.Ring, Palette.A(Palette.JellyRing, 0f), 14, root, 1f, 1f);
            ring = Art.NewSprite("ring", Art.Ring, Palette.A(Palette.JellyRing, 0f), 15, root, 1f, 1f);
        }

        public void Tick(float dt, bool reducedFlashing)
        {
            time += dt;
            root.gameObject.SetActive(j.Alive);
            if (!j.Alive) return;
            root.position = Art.Snap(new Vector3(j.Body.Pos.X, j.Body.Pos.Y));
            float wind = j.WindupProgress;
            bool spent = j.State == JellyState.Spent;
            float swell = 1f + wind * 0.25f + (wind > 0f ? Mathf.Sin(time * 40f) * 0.03f * wind : 0f);
            dome.transform.localScale = new Vector3(1.375f * swell, 1.375f * swell * (spent ? 0.8f : 1f), 1f);
            Color body = spent ? Palette.JellyDim : Color.white;
            dome.color = j.Flash > 0f && reducedFlashing ? Palette.JellyRing : body;
            // The tell: a faint ring at the full radius grows bolder through the wind-up.
            float full = t.JellyPulseRadius * 2f;
            preview.transform.localScale = new Vector3(full, full, 1f);
            preview.color = Palette.A(Palette.JellyRing, wind * 0.35f);
            float r = j.RingRadius;
            ring.transform.localScale = new Vector3(r * 2f, r * 2f, 1f);
            ring.color = Palette.A(Palette.JellyRing, r > 0f ? 0.9f : 0f);
        }
    }

    /// <summary>
    /// The Undertoll: a great verdigris bell with a brass rim. Shadow on the floor while it rises for a slam, a
    /// backward lean before the rush, coral marks on the ceiling before the drips.
    /// </summary>
    public sealed class BossView
    {
        readonly Boss b;
        readonly BossTuning t;
        readonly Transform root, bell;
        readonly SpriteRenderer shell, crack, clapper, shadow, mouthGlow;
        readonly List<SpriteRenderer> waves = new List<SpriteRenderer>(), drips = new List<SpriteRenderer>(), marks = new List<SpriteRenderer>();
        readonly Transform fxRoot;
        float time;
        float floorY;

        public BossView(Boss b, BossTuning t, Transform parent, float arenaFloorY)
        {
            this.b = b;
            this.t = t;
            floorY = arenaFloorY;
            root = Art.Group("The Undertoll", parent);
            fxRoot = Art.Group("Undertoll fx", parent);
            shadow = Art.NewSprite("shadow", Art.Glow, new Color(0f, 0f, 0f, 0.5f), 11, fxRoot, 3.4f, 0.8f);
            bell = Art.Group("bell", root);
            mouthGlow = Art.NewSprite("mouthGlow", Art.Glow, Palette.A(Palette.Warm, 0.35f), 11, bell, 2.6f, 1.6f);
            mouthGlow.transform.localPosition = new Vector3(0, -1.1f);
            shell = Art.NewSprite("Undertoll sprite", PixelArt.Boss, Color.white, 12, bell, 3.125f, 3.125f);
            crack = Art.NewSprite("Undertoll crack", PixelArt.Crack, Color.white, 13, bell, 3.125f, 3.125f);
            clapper = Art.NewSprite("clapper", Art.Circle, Palette.BellDark, 11, bell, 0.55f, 0.55f);
        }

        SpriteRenderer Pool(List<SpriteRenderer> list, int i, Sprite s, Color c, int order)
        {
            while (list.Count <= i) list.Add(Art.NewSprite("fx", s, c, order, fxRoot));
            list[i].enabled = true;
            return list[i];
        }

        public void Tick(float dt, bool reducedFlashing)
        {
            time += dt;
            root.gameObject.SetActive(!b.Dead || b.Fsm.Time < 2.5f);
            Vector3 pos = new Vector3(b.Body.Pos.X, b.Body.Pos.Y);
            root.position = Art.Snap(pos);
            BossState s = b.State;
            float wind = b.WindupProgress;
            float lean = s == BossState.RushWindup ? -b.Facing * 18f * wind : s == BossState.Rush ? b.Facing * 14f : 0f;
            float wobble = (s == BossState.SlamRecover || s == BossState.RushRecover) ? Mathf.Sin(time * 22f) * 3f * Mathf.Exp(-b.Fsm.Time * 2f) : 0f;
            if (s == BossState.Dead) wobble = Mathf.Sin(time * 30f) * 6f * Mathf.Exp(-b.Fsm.Time);
            bell.localRotation = Quaternion.Euler(0, 0, lean + wobble);
            float sway = Mathf.Sin(time * 2f) * 0.25f + (s == BossState.Rush ? -b.Facing * 0.5f : 0f);
            clapper.transform.localPosition = new Vector3(sway, -1.05f);
            Color intact = s == BossState.Dead ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
            shell.color = b.Flash > 0f ? (reducedFlashing ? Palette.Warm : new Color(1f, 0.94f, 0.74f)) : intact;
            crack.enabled = b.Phase2 && !b.Dead;
            bool recovering = s == BossState.SlamRecover || s == BossState.RushRecover || s == BossState.RainRecover;
            mouthGlow.color = Palette.A(Palette.Warm, s == BossState.Dead ? 0.05f : recovering ? 0.65f : 0.25f + wind * 0.3f);

            // Landing shadow while rising for a slam: it shows exactly where the bell will come down.
            bool airborne = s == BossState.SlamWindup || s == BossState.SlamFall;
            shadow.enabled = airborne;
            if (airborne)
            {
                shadow.transform.position = new Vector3(pos.x, floorY + 0.05f);
                float k = Mathf.Clamp01(1f - (pos.y - b.Body.Half.Y - floorY) / 3f);
                shadow.color = new Color(0f, 0f, 0f, 0.25f + 0.45f * k);
            }

            int wi = 0;
            foreach (Shockwave sw in b.Waves)
            {
                var sr = Pool(waves, wi++, Art.Dome, Palette.Wave, 26);
                Art.DrawWave(sr, sw, t.WaveHeight, time);
            }
            for (int i = wi; i < waves.Count; i++) waves[i].enabled = false;

            int di = 0, mi = 0;
            foreach (Drip d in b.Drips)
            {
                if (!d.Falling)
                {
                    var m = Pool(marks, mi++, Art.Glow, Palette.Danger, 26);
                    m.transform.position = new Vector3(d.X, d.Y + 0.6f);
                    float pulse = 0.5f + 0.5f * Mathf.Sin(time * 16f);
                    m.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
                    m.color = Palette.A(Palette.Danger, 0.35f + 0.35f * pulse);
                    var line = Pool(drips, di++, Art.Square, Palette.A(Palette.Danger, 0.25f), 25);
                    line.transform.position = new Vector3(d.X, (d.Y + floorY) * 0.5f);
                    line.transform.localScale = new Vector3(0.7f, d.Y - floorY, 1f);
                    line.color = Palette.A(Palette.Danger, 0.08f + 0.1f * pulse);
                }
                else
                {
                    var sr = Pool(drips, di++, Art.Circle, Palette.Drip, 26);
                    sr.transform.position = new Vector3(d.X, d.Y);
                    sr.transform.localScale = new Vector3(0.5f, 0.8f, 1f);
                    sr.color = Palette.Drip;
                }
            }
            for (int i = di; i < drips.Count; i++) drips[i].enabled = false;
            for (int i = mi; i < marks.Count; i++) marks[i].enabled = false;
        }
    }
}
