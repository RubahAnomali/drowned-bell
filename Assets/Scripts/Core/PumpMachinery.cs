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
        public override bool Harmful => false;
        public override string StateName => Beat.ToString();
        public override int RejectedTransitions => 0;
        public int Lane { get; private set; }
        int pulses;

        public PumpMachine(int id, EnemyKind kind, Vec2 feet, int room) : base(id, kind, feet, room,
            kind == EnemyKind.Warden ? 3.5f : 1.5f, kind == EnemyKind.Warden ? 3.5f : 1.5f,
            kind == EnemyKind.Warden ? 14 : kind == EnemyKind.Limpet ? 3 : 4) { }

        public Vec2 Pipe(int lane) => new Vec2(Home.X + 4f * (lane - 1), Body.Bottom);
        public Aabb AttackLane
        {
            get
            {
                if (Beat == MachineBeat.Mark || Beat == MachineBeat.Jet)
                    return new Aabb(Pipe(Lane) + new Vec2(0, 2f), new Vec2(1.25f, 2f));
                float reach = IsWarden ? 5f : Kind == EnemyKind.Porter ? 5.5f : 3f;
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
                        Lane = dx < -2f ? 0 : dx > 2f ? 2 : 1;
                        pulses = SecondPhase ? 2 : 1;
                        Enter(MachineBeat.Mark);
                    }
                    else Enter(MachineBeat.Brace);
                    break;
                case MachineBeat.Mark:
                    if (Elapsed >= 1f) Enter(MachineBeat.Jet);
                    break;
                case MachineBeat.Jet:
                    if (Elapsed < 0.65f) break;
                    if (--pulses > 0) { Lane = (Lane + 1) % 3; Enter(MachineBeat.Mark); }
                    else { Facing = dx >= 0 ? 1 : -1; Enter(MachineBeat.Brace); }
                    break;
                case MachineBeat.Brace:
                    if (Elapsed >= (Kind == EnemyKind.Limpet ? 0.8f : 0.85f)) Enter(MachineBeat.Strike);
                    break;
                case MachineBeat.Strike:
                    if (Elapsed >= 0.30f) Enter(MachineBeat.Recover);
                    break;
                case MachineBeat.Recover:
                    if (Elapsed >= (IsWarden ? 1.2f : Kind == EnemyKind.Porter ? 1.1f : 1f)) Enter(MachineBeat.Idle);
                    break;
            }
            if (Beat != previous && Beat == MachineBeat.Brace) w.Emit(GameEventType.MachineBrace, Home);
            if (Beat != previous && Beat == MachineBeat.Mark) w.Emit(GameEventType.PipeWarning, Pipe(Lane));
        }
    }
}
