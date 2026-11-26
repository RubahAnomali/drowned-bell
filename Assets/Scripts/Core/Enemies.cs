using System;
using System.Collections.Generic;

namespace DrownedBell.Core
{
    public enum EnemyKind { Crab, Jelly, Dummy }

    public abstract class Enemy
    {
        public readonly int Id;
        public readonly EnemyKind Kind;
        public readonly Vec2 Home;
        public readonly int Room;
        public readonly Body Body;
        public int Health, MaxHealth;
        public int Facing = -1;
        public bool Alive = true;
        /// <summary>Seconds of white hit-flash left, for the view.</summary>
        public float Flash;
        int lastSwing = -1;

        protected Enemy(int id, EnemyKind kind, Vec2 homeFeet, int room, float w, float h, int health)
        {
            Id = id;
            Kind = kind;
            Room = room;
            Home = new Vec2(homeFeet.X, homeFeet.Y + h * 0.5f + 0.001f);
            Body = new Body(Home, w, h);
            MaxHealth = Health = health;
        }

        public abstract string StateName { get; }
        public abstract int RejectedTransitions { get; }
        /// <summary>Touching the body hurts the diver.</summary>
        public virtual bool Harmful => Alive;
        /// <summary>The active attack box, if an attack is live this tick.</summary>
        public virtual bool TryAttackBox(out Aabb box) { box = default; return false; }
        public abstract void Update(World w, float dt);

        public virtual void Reset()
        {
            Body.Pos = Home;
            Body.Vel = new Vec2(0, 0);
            Health = MaxHealth;
            Alive = true;
            Flash = 0f;
            Facing = -1;
            lastSwing = -1;
        }

        /// <summary>A swing can only ever damage an enemy once. Returns true when this swing is new.</summary>
        public bool TakeHit(World w, int swingId, int damage, float fromX)
        {
            if (!Alive || swingId == lastSwing) return false;
            lastSwing = swingId;
            Health -= damage;
            Flash = 0.12f;
            OnHit(w, Body.Pos.X >= fromX ? 1 : -1);
            if (Health <= 0)
            {
                Alive = false;
                w.Emit(GameEventType.EnemyKilled, Body.Pos, Id);
            }
            return true;
        }

        protected virtual void OnHit(World w, int awayDir) { }

        protected bool SeesPlayer(World w, float rangeX, float rangeY)
        {
            Vec2 p = w.Player.Body.Pos;
            return MathF.Abs(p.X - Body.Pos.X) < rangeX && MathF.Abs(p.Y - Body.Pos.Y) < rangeY && w.Level.LineClear(Body.Pos, p);
        }

        /// <summary>Is there safe floor one step ahead? Walkers never step off ledges or onto spikes.</summary>
        protected bool GroundAhead(Level lv, int dir)
        {
            float x = Body.Pos.X + dir * (Body.Half.X + 0.15f);
            int cx = (int)MathF.Floor(x);
            int footY = (int)MathF.Floor(Body.Bottom + 0.05f);
            bool floor = lv.Solid(cx, footY - 1) || lv.OneWay(cx, footY - 1);
            bool wall = lv.Solid(cx, footY);
            return floor && !wall && !lv.Spiky(cx, footY);
        }
    }

    public enum CrabState { Patrol, Approach, Windup, Lunge, Recover }

    /// <summary>
    /// Brine Crab. Teaches spacing: it closes in, rears back for a long, readable wind-up, lunges a fixed
    /// distance with its claw, then sits in a long recovery. Stand just outside the lunge, then punish.
    /// </summary>
    public sealed class Crab : Enemy
    {
        public readonly StateMachine<CrabState> Fsm;
        readonly EnemyTuning t;
        float lostFor;

        static readonly Dictionary<CrabState, CrabState[]> Allowed = new Dictionary<CrabState, CrabState[]>
        {
            { CrabState.Patrol, new[] { CrabState.Approach } },
            { CrabState.Approach, new[] { CrabState.Patrol, CrabState.Windup } },
            { CrabState.Windup, new[] { CrabState.Lunge } },
            { CrabState.Lunge, new[] { CrabState.Recover } },
            { CrabState.Recover, new[] { CrabState.Approach, CrabState.Patrol } },
        };

        public Crab(int id, Vec2 feet, int room, EnemyTuning t) : base(id, EnemyKind.Crab, feet, room, 1.3f, 0.75f, t.CrabHealth)
        {
            this.t = t;
            Fsm = new StateMachine<CrabState>(CrabState.Patrol, Allowed);
        }

        public override string StateName => Fsm.State.ToString();
        public override int RejectedTransitions => Fsm.Rejected;
        public CrabState State => Fsm.State;
        /// <summary>0..1 progress of the wind-up, so the view can rear the claws back.</summary>
        public float WindupProgress => State == CrabState.Windup ? Mathx.Clamp01(Fsm.Time / t.CrabWindup) : 0f;

        public override void Reset()
        {
            base.Reset();
            MaxHealth = Health = t.CrabHealth;
            Fsm.Reset();
            lostFor = 0f;
        }

        public override bool TryAttackBox(out Aabb box)
        {
            box = new Aabb(new Vec2(Body.Pos.X + Facing * (Body.Half.X + t.CrabClawReach * 0.5f), Body.Pos.Y - 0.05f),
                new Vec2(t.CrabClawReach * 0.5f, 0.38f));
            return Alive && State == CrabState.Lunge;
        }

        protected override void OnHit(World w, int awayDir)
        {
            if (State == CrabState.Patrol || State == CrabState.Approach)
            {
                Body.Vel.X = awayDir * 3f;
                Fsm.Go(CrabState.Approach);
            }
        }

        public override void Update(World w, float dt)
        {
            if (!Alive) return;
            Fsm.Tick(dt);
            if (Flash > 0f) Flash -= dt;
            Level lv = w.Level;
            float dx = w.Player.Body.Pos.X - Body.Pos.X;
            bool sees = SeesPlayer(w, t.CrabSightX, 2f);

            switch (State)
            {
                case CrabState.Patrol:
                    if (!GroundAhead(lv, Facing)) Facing = -Facing;
                    Body.Vel.X = Mathx.MoveToward(Body.Vel.X, Facing * t.CrabPatrolSpeed, 20f * dt);
                    if (sees) Fsm.Go(CrabState.Approach);
                    break;
                case CrabState.Approach:
                    Facing = dx >= 0f ? 1 : -1;
                    lostFor = sees ? 0f : lostFor + dt;
                    if (lostFor > 1.5f) { Fsm.Go(CrabState.Patrol); break; }
                    if (MathF.Abs(dx) <= t.CrabAttackRange && sees)
                    {
                        Body.Vel.X = 0f;
                        Fsm.Go(CrabState.Windup);
                        w.Emit(GameEventType.CrabWindup, Body.Pos, Id);
                        break;
                    }
                    float want = GroundAhead(lv, Facing) ? Facing * t.CrabApproachSpeed : 0f;
                    Body.Vel.X = Mathx.MoveToward(Body.Vel.X, want, 20f * dt);
                    break;
                case CrabState.Windup:
                    // Rear back a little: the tell is both the pose and a small backward shuffle.
                    Body.Vel.X = GroundAhead(lv, -Facing) ? -Facing * 0.5f : 0f;
                    if (Fsm.Time >= t.CrabWindup)
                    {
                        Fsm.Go(CrabState.Lunge);
                        w.Emit(GameEventType.CrabLunge, Body.Pos, Id);
                    }
                    break;
                case CrabState.Lunge:
                    Body.Vel.X = GroundAhead(lv, Facing) ? Facing * t.CrabLungeSpeed : 0f;
                    if (Fsm.Time >= t.CrabLungeTime) Fsm.Go(CrabState.Recover);
                    break;
                case CrabState.Recover:
                    Body.Vel.X = Mathx.MoveToward(Body.Vel.X, 0f, 30f * dt);
                    if (Fsm.Time >= t.CrabRecover) Fsm.Go(sees ? CrabState.Approach : CrabState.Patrol);
                    break;
            }

            Body.Vel.Y = MathF.Max(Body.Vel.Y - 40f * dt, -20f);
            Physics.Move(lv, Body, dt);
            if (Body.HitWall && State == CrabState.Patrol) Facing = -Facing;
        }
    }

    public enum JellyState { Drift, Windup, Pulse, Spent }

    /// <summary>
    /// Knell Jelly. Teaches timing: it hovers near the diver, swells and rings for almost a second, then sends
    /// out a ring that only hurts inside its moving band. Afterwards it sags and is harmless to touch: the window
    /// to strike.
    /// </summary>
    public sealed class Jelly : Enemy
    {
        public readonly StateMachine<JellyState> Fsm;
        readonly EnemyTuning t;

        static readonly Dictionary<JellyState, JellyState[]> Allowed = new Dictionary<JellyState, JellyState[]>
        {
            { JellyState.Drift, new[] { JellyState.Windup } },
            { JellyState.Windup, new[] { JellyState.Pulse } },
            { JellyState.Pulse, new[] { JellyState.Spent } },
            { JellyState.Spent, new[] { JellyState.Drift } },
        };

        public Jelly(int id, Vec2 feet, int room, EnemyTuning t) : base(id, EnemyKind.Jelly, new Vec2(feet.X, feet.Y), room, 0.9f, 1.05f, t.JellyHealth)
        {
            this.t = t;
            Fsm = new StateMachine<JellyState>(JellyState.Drift, Allowed);
        }

        public override string StateName => Fsm.State.ToString();
        public override int RejectedTransitions => Fsm.Rejected;
        public JellyState State => Fsm.State;
        public float WindupProgress => State == JellyState.Windup ? Mathx.Clamp01(Fsm.Time / t.JellyWindup) : 0f;
        /// <summary>Current ring radius while pulsing, else 0.</summary>
        public float RingRadius => State == JellyState.Pulse ? t.JellyPulseRadius * Mathx.Clamp01(Fsm.Time / t.JellyPulseTime) : 0f;
        public override bool Harmful => Alive && State != JellyState.Spent;

        public override void Reset()
        {
            base.Reset();
            MaxHealth = Health = t.JellyHealth;
            Fsm.Reset();
        }

        /// <summary>Is the diver inside the moving ring band right now?</summary>
        public bool RingHits(Vec2 p)
        {
            if (!Alive || State != JellyState.Pulse) return false;
            float r = RingRadius;
            float d = Vec2.Distance(p, Body.Pos);
            return d <= r && d >= r - t.JellyPulseBand;
        }

        protected override void OnHit(World w, int awayDir)
        {
            Body.Vel = new Vec2(awayDir * 2.5f, 1f);
        }

        public override void Update(World w, float dt)
        {
            if (!Alive) return;
            Fsm.Tick(dt);
            if (Flash > 0f) Flash -= dt;
            Vec2 p = w.Player.Body.Pos;
            float dist = Vec2.Distance(p, Body.Pos);
            bool near = dist < t.JellyAggroRange && w.Level.LineClear(Body.Pos, p);
            Vec2 goal = Home;

            switch (State)
            {
                case JellyState.Drift:
                    if (near)
                    {
                        // Hover off to one side and a little above, never far from home.
                        float side = Body.Pos.X >= p.X ? 1f : -1f;
                        goal = new Vec2(p.X + side * t.JellyHoverDistance, p.Y + 1.2f);
                        Vec2 leash = goal - Home;
                        float l = leash.Length;
                        if (l > 5f) goal = Home + leash * (5f / l);
                    }
                    Steer(goal, t.JellyDriftSpeed, dt);
                    if (near && Fsm.Time >= t.JellyDrift && dist < t.JellyPulseRadius + 1.5f)
                    {
                        Fsm.Go(JellyState.Windup);
                        w.Emit(GameEventType.JellyWindup, Body.Pos, Id);
                    }
                    break;
                case JellyState.Windup:
                    Body.Vel = Body.Vel * MathF.Max(0f, 1f - 8f * dt);
                    if (Fsm.Time >= t.JellyWindup)
                    {
                        Fsm.Go(JellyState.Pulse);
                        w.Emit(GameEventType.JellyPulse, Body.Pos, Id);
                    }
                    break;
                case JellyState.Pulse:
                    Body.Vel = new Vec2(0, 0);
                    if (Fsm.Time >= t.JellyPulseTime) Fsm.Go(JellyState.Spent);
                    break;
                case JellyState.Spent:
                    Body.Vel = new Vec2(0f, Fsm.Time < 0.5f ? -0.9f : 0f);
                    if (Fsm.Time >= t.JellySpent) Fsm.Go(JellyState.Drift);
                    break;
            }
            Physics.Move(w.Level, Body, dt);
        }

        void Steer(Vec2 goal, float speed, float dt)
        {
            Vec2 d = goal - Body.Pos;
            float l = d.Length;
            Vec2 want = l > 0.05f ? d * (MathF.Min(speed, l * 2f) / l) : new Vec2(0, 0);
            Body.Vel = new Vec2(Mathx.MoveToward(Body.Vel.X, want.X, 6f * dt), Mathx.MoveToward(Body.Vel.Y, want.Y, 6f * dt));
            if (MathF.Abs(d.X) > 0.1f) Facing = d.X > 0f ? 1 : -1;
        }
    }

    /// <summary>Test-room target: takes hits, counts them, never attacks and never dies.</summary>
    public sealed class Dummy : Enemy
    {
        public int Hits;

        public Dummy(int id, Vec2 feet, int room) : base(id, EnemyKind.Dummy, feet, room, 0.8f, 1.6f, 9999) { }

        public override string StateName => "Target (" + Hits + " hits)";
        public override int RejectedTransitions => 0;
        public override bool Harmful => false;

        protected override void OnHit(World w, int awayDir)
        {
            Hits++;
            Health = MaxHealth;
        }

        public override void Update(World w, float dt)
        {
            if (Flash > 0f) Flash -= dt;
        }
    }
}
