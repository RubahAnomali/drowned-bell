using System;

namespace DrownedBell.Core
{
    public enum MachineBeat { Idle, Mark, Jet, Brace, Strike, Recover }

    // Fixed machines lock their bearing when they brace. The same geometry drives the
    // hazard and its renderer, including the harmless chain/piston warning.
    public sealed class PumpMachine : Enemy
    {
        public MachineBeat Beat { get; private set; }
        public float Elapsed { get; private set; }
        public bool Engaged { get; private set; }
        public bool SecondPhase => IsWarden && Health * 2 <= MaxHealth;
        public bool IsWarden => Kind == EnemyKind.Warden;
        public override bool IsBoss => IsWarden;
        public override bool Harmful => false;
        public override string StateName => Beat.ToString();
        public override int RejectedTransitions => 0;
        public int Lane { get; private set; }
        int pulses, attacks;
        /// <summary>Phase two: this pulse covers two neighbouring pipes at once, leaving one lane quiet.</summary>
        public bool Wide { get; private set; }
        /// <summary>How long the current Brace lasts: a delayed swing hangs 0.4 s longer, to punish a dodge made too early.</summary>
        public float BraceTime { get; private set; } = 0.6f;
        public float MarkTime => SecondPhase ? 0.45f : 0.6f;
        public float RecoverTime => IsWarden ? (SecondPhase ? 0.7f : 0.85f) : Kind == EnemyKind.Porter ? 1.1f : 1f;
        public override int AttackDamage => IsWarden && Beat == MachineBeat.Strike ? 2 : 1;

        public PumpMachine(int id, EnemyKind kind, Vec2 feet, int room) : base(id, kind, feet, room,
            kind == EnemyKind.Warden ? 3.5f : 1.5f, kind == EnemyKind.Warden ? 3.5f : 1.5f,
            kind == EnemyKind.Warden ? 22 : kind == EnemyKind.Limpet ? 3 : 4) { }

        /// <summary>Lane 3 is a tracked jet: when the diver keeps well back from the pipes it vents under their feet, so range is never safe.</summary>
        public float TrackX { get; private set; }
        public Vec2 Pipe(int lane) => lane == 3 ? new Vec2(TrackX, Body.Bottom) : new Vec2(Home.X + 4f * (lane - 1), Body.Bottom);
        public Aabb AttackLane
        {
            get
            {
                if (Beat == MachineBeat.Mark || Beat == MachineBeat.Jet)
                {
                    int other = Wide && Lane < 3 ? (Lane == 2 ? 1 : Lane + 1) : Lane;
                    float lo = Pipe(Math.Min(Lane, other)).X - 1.25f, hi = Pipe(Math.Max(Lane, other)).X + 1.25f;
                    return new Aabb(new Vec2((lo + hi) * 0.5f, Body.Bottom + 2f), new Vec2((hi - lo) * 0.5f, 2f));
                }
                // The Warden's sweep starts at its own centre, so standing against its casing is no longer safe:
                // dodge across it or jump it (the sweep is low).
                if (IsWarden)
                    return new Aabb(new Vec2(Home.X + Facing * 2.75f, Body.Bottom + 0.55f), new Vec2(2.75f, 0.38f));
                float reach = Kind == EnemyKind.Porter ? 5.5f : 3f;
                return new Aabb(new Vec2(Home.X + Facing * (reach + Body.Half.X) * 0.5f, Body.Bottom + 0.55f),
                    new Vec2((reach - Body.Half.X) * 0.5f, 0.38f));
            }
        }

        public override bool TryAttackBox(out Aabb box)
        {
            box = AttackLane;
            return Alive && (Beat == MachineBeat.Strike || Beat == MachineBeat.Jet);
        }

        public override bool TakeHit(World w, int swingId, int damage, float fromX)
        {
            if (IsWarden && Beat != MachineBeat.Recover) return false;
            bool before = SecondPhase;
            bool hit = base.TakeHit(w, swingId, damage, fromX);
            if (hit && IsWarden && !before && SecondPhase && Alive)
                w.Emit(GameEventType.PumpSurge, Body.Pos);
            if (hit && IsWarden && !Alive) w.Level.DoorsLocked = false;
            return hit;
        }

        void Enter(MachineBeat next)
        {
            Beat = next;
            Elapsed = 0f;
        }

        public override void Reset()
        {
            base.Reset();
            Enter(MachineBeat.Idle);
            Engaged = false;
            pulses = 0;
            attacks = 0;
            Wide = false;
            BraceTime = 0.6f;
            Lane = 0;
        }

        public override void Update(World w, float dt)
        {
            if (!Alive) return;
            Flash = MathF.Max(0, Flash - dt);
            float dx = w.Player.Body.Pos.X - Home.X;
            Elapsed += dt;
            var previous = Beat;
            switch (Beat)
            {
                case MachineBeat.Idle:
                    if (Elapsed < 0.32f || !SeesPlayer(w, IsWarden ? 13f : 7f, 5f)) break;
                    Engaged = true;
                    Facing = dx >= 0 ? 1 : -1;
                    if (IsWarden)
                    {
                        w.Level.DoorsLocked = true;
                        TrackX = w.Player.Body.Pos.X;
                        Lane = MathF.Abs(dx) >= 5.5f ? 3 : dx < -2f ? 0 : dx > 2f ? 2 : 1;
                        pulses = SecondPhase ? 4 : 3;
                        Wide = SecondPhase;
                        // Every other attack the sweep is a delayed swing.
                        BraceTime = (attacks++ & 1) == 1 ? 0.95f : 0.6f;
                        Enter(MachineBeat.Mark);
                    }
                    else Enter(MachineBeat.Brace);
                    break;
                case MachineBeat.Mark:
                    if (Elapsed >= MarkTime) Enter(MachineBeat.Jet);
                    break;
                case MachineBeat.Jet:
                    if (Elapsed < 0.5f) break;
                    if (--pulses > 0) { Lane = (Lane + 1) % 3; Wide = SecondPhase && pulses % 2 == 1; Enter(MachineBeat.Mark); }
                    else { Facing = dx >= 0 ? 1 : -1; Enter(MachineBeat.Brace); }
                    break;
                case MachineBeat.Brace:
                    if (Elapsed >= (IsWarden ? BraceTime : Kind == EnemyKind.Limpet ? 0.8f : 0.85f)) Enter(MachineBeat.Strike);
                    break;
                case MachineBeat.Strike:
                    if (Elapsed >= 0.30f) Enter(MachineBeat.Recover);
                    break;
                case MachineBeat.Recover:
                    if (Elapsed >= RecoverTime) Enter(MachineBeat.Idle);
                    break;
            }
            if (Beat != previous && Beat == MachineBeat.Brace) w.Emit(GameEventType.MachineBrace, Home);
            if (Beat != previous && Beat == MachineBeat.Mark) w.Emit(GameEventType.PipeWarning, Pipe(Lane));
        }
    }
}
