using System;
using System.Collections.Generic;

namespace DrownedBell.Core
{
    public enum MothBeat { Hover, Mark, Dive, Lodged, Rise }

    /// <summary>
    /// Ink Moth. Folds its wings over a floor mark for 0.9 s (the mark is where the diver stood), dives straight at
    /// it, then sits lodged for 0.9 s: the window to strike. Only the dive hurts. It flies, so tiles do not stop it.
    /// </summary>
    public sealed class InkMoth : Enemy
    {
        public const float MarkTime = 0.9f, LodgedTime = 0.9f, DiveSpeed = 16f, RiseSpeed = 5f;
        public MothBeat Beat { get; private set; }
        public float Elapsed { get; private set; }
        public Vec2 Mark { get; private set; }

        public InkMoth(int id, Vec2 perch, int room) : base(id, EnemyKind.Moth, perch, room, 0.9f, 0.7f, 3) { }

        public override string StateName => Beat.ToString();
        public override int RejectedTransitions => 0;
        public override bool Harmful => Alive && Beat == MothBeat.Dive;
        public float MarkProgress => Beat == MothBeat.Mark ? Mathx.Clamp01(Elapsed / MarkTime) : 0f;

        void Enter(MothBeat next) { Beat = next; Elapsed = 0f; }

        public override void Reset()
        {
            base.Reset();
            Enter(MothBeat.Hover);
        }

        public override void Update(World w, float dt)
        {
            if (!Alive) return;
            if (Flash > 0f) Flash -= dt;
            Elapsed += dt;
            Vec2 feet = w.Player.Body.Feet;
            switch (Beat)
            {
                case MothBeat.Hover:
                    Body.Pos = Home + new Vec2(MathF.Sin(w.Time * 1.7f + Id) * 0.6f, MathF.Sin(w.Time * 2.3f + Id) * 0.3f);
                    if (Elapsed >= 1f && MathF.Abs(feet.X - Home.X) < 9f && MathF.Abs(feet.Y - Home.Y) < 10f)
                    {
                        RoomInfo r = w.Level.Rooms[Room];
                        float x = Mathx.Clamp(feet.X, r.MinX + 1.5f, r.MaxX - 1.5f);
                        Mark = new Vec2(x, w.Level.FloorBelow(x, feet.Y + 0.5f));
                        Facing = Mark.X >= Body.Pos.X ? 1 : -1;
                        Enter(MothBeat.Mark);
                        w.Emit(GameEventType.MothMark, Mark, Id);
                    }
                    break;
                case MothBeat.Mark:
                    if (Elapsed >= MarkTime) { Enter(MothBeat.Dive); w.Emit(GameEventType.MothDive, Body.Pos, Id); }
                    break;
                case MothBeat.Dive:
                {
                    Vec2 target = new Vec2(Mark.X, Mark.Y + Body.Half.Y);
                    Vec2 d = target - Body.Pos;
                    float step = DiveSpeed * dt;
                    if (d.Length <= step) { Body.Pos = target; Enter(MothBeat.Lodged); w.Emit(GameEventType.MothLodged, Mark, Id); }
                    else Body.Pos = Body.Pos + d * (step / d.Length);
                    break;
                }
                case MothBeat.Lodged:
                    if (Elapsed >= LodgedTime) Enter(MothBeat.Rise);
                    break;
                case MothBeat.Rise:
                {
                    Vec2 d = Home - Body.Pos;
                    float step = RiseSpeed * dt;
                    if (d.Length <= step) { Body.Pos = Home; Enter(MothBeat.Hover); }
                    else Body.Pos = Body.Pos + d * (step / d.Length);
                    break;
                }
            }
        }
    }

    public enum WidowBeat { Idle, Spread, Ring, Recover }

    /// <summary>
    /// Tuning Widow. Spreads its forks for 0.8 s, then rings a floor ripple each way and an echo pair 0.75 s later.
    /// Its casing only takes damage in the 1.2 s sag after the echo. Touching it is safe: the rings are the threat.
    /// </summary>
    public sealed class TuningWidow : Enemy
    {
        public const float SpreadTime = 0.8f, RecoverTime = 1.2f, Reach = 9f;
        public WidowBeat Beat { get; private set; }
        public float Elapsed { get; private set; }
        public readonly List<Shockwave> Waves = new List<Shockwave>(4);
        bool echoed;

        public TuningWidow(int id, Vec2 feet, int room) : base(id, EnemyKind.Widow, feet, room, 1.4f, 1.1f, 3) { }

        public override string StateName => Beat.ToString();
        public override int RejectedTransitions => 0;
        public override bool Harmful => false;
        public float SpreadProgress => Beat == WidowBeat.Spread ? Mathx.Clamp01(Elapsed / SpreadTime) : 0f;

        void Enter(WidowBeat next) { Beat = next; Elapsed = 0f; }

        public override void Reset()
        {
            base.Reset();
            Enter(WidowBeat.Idle);
            Waves.Clear();
            echoed = false;
        }

        public override bool TakeHit(World w, int swingId, int damage, float fromX)
            => Beat == WidowBeat.Recover && base.TakeHit(w, swingId, damage, fromX);

        void Ring(World w)
        {
            Waves.Add(new Shockwave { X = Body.Pos.X - Body.Half.X, FloorY = Body.Bottom, Dir = -1 });
            Waves.Add(new Shockwave { X = Body.Pos.X + Body.Half.X, FloorY = Body.Bottom, Dir = 1 });
            w.Emit(GameEventType.WidowRing, Body.Feet, Id);
        }

        public override void Update(World w, float dt)
        {
            if (Flash > 0f) Flash -= dt;
            for (int i = Waves.Count - 1; i >= 0; i--)
                if (!Waves[i].Advance(w.Level, w.BossT.WaveSpeed * dt) || MathF.Abs(Waves[i].X - Body.Pos.X) > Reach) Waves.RemoveAt(i);
            if (!Alive) return;
            Elapsed += dt;
            switch (Beat)
            {
                case WidowBeat.Idle:
                    if (Elapsed >= 0.6f && SeesPlayer(w, 10f, 4f))
                    {
                        Facing = w.Player.Body.Pos.X >= Body.Pos.X ? 1 : -1;
                        Enter(WidowBeat.Spread);
                        w.Emit(GameEventType.WidowSpread, Body.Pos, Id);
                    }
                    break;
                case WidowBeat.Spread:
                    if (Elapsed >= SpreadTime) { Ring(w); echoed = false; Enter(WidowBeat.Ring); }
                    break;
                case WidowBeat.Ring:
                    if (!echoed && Elapsed >= RippleBell.EchoDelay) { Ring(w); echoed = true; Enter(WidowBeat.Recover); }
                    break;
                case WidowBeat.Recover:
                    if (Elapsed >= RecoverTime) Enter(WidowBeat.Idle);
                    break;
            }
        }
    }

    public enum InkBeat { Dormant, Hover, Mark, Dive, Recover, Rise }

    /// <summary>
    /// The Archivist of Depths. Two ink silhouettes mark two landing spots for 1.0 s; only the real one swings its
    /// clapper. The real one dives onto its mark, then sits grounded for 1.1 s, the only time it takes damage.
    /// Below half health every landing also sends a ripple pair 0.75 s later. The real silhouette alternates between
    /// the mark under the diver and the decoy mark, so standing still is only safe when the clapper is elsewhere.
    /// </summary>
    public sealed class Archivist : Enemy
    {
        public const float DiveSpeed = 22f, RiseSpeed = 9f, HoverHeight = 7f;
        /// <summary>Phase two chains every third dive: a short recovery, no hover, a quick second mark.</summary>
        public bool Chain => SecondPhase && Dives % 3 == 0 && Dives > 0;
        public float MarkTime => Chain ? 0.5f : SecondPhase ? 0.55f : 0.7f;
        public float RecoverTime => Chain ? 0.4f : SecondPhase ? 0.7f : 0.85f;
        public float HoverTime => Chain ? 0.1f : 0.35f;
        public InkBeat Beat { get; private set; }
        public float Elapsed { get; private set; }
        public override bool IsBoss => true;
        public override int ContactDamage => 2;
        public bool Engaged => Beat != InkBeat.Dormant;
        public bool SecondPhase => Health * 2 <= MaxHealth;
        /// <summary>The two marks during Mark; RealMark is where the real body will land.</summary>
        public Vec2 RealMark { get; private set; }
        public Vec2 DecoyMark { get; private set; }
        /// <summary>Where the sea-gate order lies after the fight.</summary>
        public Vec2 DropAt { get; private set; }
        public int Dives { get; private set; }
        public readonly List<Shockwave> Waves = new List<Shockwave>(4);
        float echoIn = -1f;
        int echoLeft;

        public Archivist(int id, Vec2 feet, int room) : base(id, EnemyKind.Archivist, feet, room, 2.2f, 2.4f, 20)
        {
            DropAt = feet;
        }

        public override string StateName => Beat.ToString();
        public override int RejectedTransitions => 0;
        public override bool Harmful => Alive && Beat == InkBeat.Dive;
        public float MarkProgress => Beat == InkBeat.Mark ? Mathx.Clamp01(Elapsed / MarkTime) : 0f;
        /// <summary>Seconds until the phase-two echo pair, or negative when none is pending.</summary>
        public float EchoIn => echoIn;

        void Enter(InkBeat next) { Beat = next; Elapsed = 0f; }

        public override void Reset()
        {
            base.Reset();
            Enter(InkBeat.Dormant);
            Waves.Clear();
            echoIn = -1f;
            Dives = 0;
            DropAt = new Vec2(Home.X, Home.Y - Body.Half.Y);
        }

        public override bool TakeHit(World w, int swingId, int damage, float fromX)
        {
            if (Beat != InkBeat.Recover) return false;
            bool before = SecondPhase;
            bool hit = base.TakeHit(w, swingId, damage, fromX);
            if (hit && !before && SecondPhase && Alive) w.Emit(GameEventType.ArchivistPhase2, Body.Pos, Id);
            // (SecondPhase is Health*2 <= MaxHealth, so it flips on the hit that crosses half.)
            if (hit && !Alive)
            {
                Waves.Clear();
                w.Level.DoorsLocked = false;
                DropAt = Body.Feet;
                w.Emit(GameEventType.ArchivistDefeated, Body.Pos, Id);
            }
            return hit;
        }

        float Floor(World w) => Home.Y - Body.Half.Y;

        public override void Update(World w, float dt)
        {
            if (Flash > 0f) Flash -= dt;
            for (int i = Waves.Count - 1; i >= 0; i--)
                if (!Waves[i].Advance(w.Level, w.BossT.WaveSpeed * dt) || MathF.Abs(Waves[i].X - DropAt.X) > 12f) Waves.RemoveAt(i);
            if (!Alive) return;
            Elapsed += dt;
            RoomInfo r = w.Level.Rooms[Room];
            Vec2 feet = w.Player.Body.Feet;
            float hoverY = Floor(w) + HoverHeight;
            if (echoIn >= 0f && (echoIn -= dt) < 0f)
            {
                Waves.Add(new Shockwave { X = DropAt.X - Body.Half.X, FloorY = DropAt.Y, Dir = -1 });
                Waves.Add(new Shockwave { X = DropAt.X + Body.Half.X, FloorY = DropAt.Y, Dir = 1 });
                w.Emit(GameEventType.WidowRing, DropAt, Id);
                if (--echoLeft > 0) echoIn = RippleBell.EchoDelay;
            }
            switch (Beat)
            {
                case InkBeat.Dormant:
                    if (MathF.Abs(feet.X - Home.X) < 11f && w.Room == Room)
                    {
                        w.Level.DoorsLocked = true;
                        Enter(InkBeat.Rise);
                        w.Emit(GameEventType.ArchivistAwake, Body.Pos, Id);
                    }
                    break;
                case InkBeat.Rise:
                {
                    Vec2 target = new Vec2(Body.Pos.X, hoverY);
                    Vec2 d = target - Body.Pos;
                    float step = RiseSpeed * dt;
                    if (d.Length <= step) { Body.Pos = target; Enter(InkBeat.Hover); }
                    else Body.Pos = Body.Pos + d * (step / d.Length);
                    break;
                }
                case InkBeat.Hover:
                    Body.Pos = new Vec2(Body.Pos.X, hoverY + MathF.Sin(w.Time * 2f) * 0.25f);
                    if (Elapsed >= HoverTime)
                    {
                        float lo = r.MinX + 3.5f, hi = r.MaxX - 3.5f;
                        float under = Mathx.Clamp(feet.X, lo, hi);
                        int side = (Dives & 2) == 0 ? 1 : -1;
                        float other = under + side * 5f;
                        if (other < lo || other > hi) other = under - side * 5f;
                        float floorY = Floor(w);
                        bool realUnder = (Dives & 1) == 0;
                        RealMark = new Vec2(realUnder ? under : other, floorY);
                        DecoyMark = new Vec2(realUnder ? other : under, floorY);
                        Body.Pos = new Vec2(RealMark.X, hoverY);
                        Facing = feet.X >= RealMark.X ? 1 : -1;
                        Enter(InkBeat.Mark);
                        w.Emit(GameEventType.ArchivistMark, RealMark, Id);
                    }
                    break;
                case InkBeat.Mark:
                    if (Elapsed >= MarkTime) Enter(InkBeat.Dive);
                    break;
                case InkBeat.Dive:
                {
                    float target = RealMark.Y + Body.Half.Y;
                    float y = Body.Pos.Y - DiveSpeed * dt;
                    if (y <= target)
                    {
                        Body.Pos = new Vec2(RealMark.X, target);
                        DropAt = RealMark;
                        Dives++;
                        Enter(InkBeat.Recover);
                        w.Emit(GameEventType.ArchivistLand, RealMark, Id);
                        // Every landing sends a ripple pair shortly after (0.45 s; 0.3 s below half health: time for one swing),
                        // and below half health an echo pair follows 0.75 s later.
                        echoIn = SecondPhase ? 0.3f : 0.45f;
                        echoLeft = SecondPhase ? 2 : 1;
                    }
                    else Body.Pos = new Vec2(Body.Pos.X, y);
                    break;
                }
                case InkBeat.Recover:
                    if (Elapsed >= RecoverTime) Enter(InkBeat.Rise);
                    break;
            }
        }
    }
}
