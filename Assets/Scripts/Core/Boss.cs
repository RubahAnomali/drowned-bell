using System;
using System.Collections.Generic;

namespace DrownedBell.Core
{
    public enum BossState
    {
        Dormant, Intro, Idle,
        SlamWindup, SlamFall, SlamRecover,
        RushWindup, Rush, RushRecover,
        RainWindup, Rain, RainRecover,
        Dead,
    }

    public sealed class Shockwave
    {
        public float X, FloorY;
        public int Dir;
        public bool Advance(Level level, float distance)
        {
            X += Dir * distance;
            return !level.Solid((int)MathF.Floor(X + Dir * 0.4f), (int)MathF.Floor(FloorY + 0.3f));
        }
    }

    public sealed class Drip
    {
        public float X, Y;
        public float Warn;
        public bool Alive = true;
        public bool Falling => Warn <= 0f;
    }

    /// <summary>
    /// The Undertoll, the drowned bell. Three attacks, each with a clear tell and a recovery to punish:
    ///  - Slam: rises over the diver, drops, and sends a shockwave along the floor each way (jump it).
    ///  - Rush: rears back and swings across the arena at ground height (get onto a side ledge).
    ///  - Rain (below half health): marks spots on the ceiling, then drips fall there (step out of the marks).
    /// The choice is deterministic: Rush when the diver is far, Slam when close, Rain every third attack in phase two.
    /// </summary>
    public sealed class Boss
    {
        public readonly Body Body;
        public readonly StateMachine<BossState> Fsm;
        public readonly List<Shockwave> Waves = new List<Shockwave>();
        public readonly List<Drip> Drips = new List<Drip>();
        public int Health, MaxHealth;
        public int Facing = -1;
        public float Flash;
        public bool Phase2;
        public int AttackCount;
        readonly BossTuning t;
        readonly Vec2 home;
        int lastSwing = -1;
        float riseFromY;
        float slamHold, echoIn = -1f, idleFor;
        int comboLeft, lastKind = -1, repeats;
        // Randomness (see Dice): the same seed replays the same fight, a new seed plays a new one.
        readonly Dice dice = new Dice(1);
        readonly Bag slamKinds = new Bag(0, 0, 0, 1);   // one delayed slam in every four, in random order
        readonly Bag rainBag = new Bag(1, 0, 0);        // phase two: about one attack in three is rain
        readonly Bag comboBag = new Bag(1, 0);          // phase two: about half the recoveries chain straight into a slam
        Vec2 slamAt;
        float slamFloor;

        static readonly Dictionary<BossState, BossState[]> Allowed = new Dictionary<BossState, BossState[]>
        {
            { BossState.Dormant, new[] { BossState.Intro } },
            { BossState.Intro, new[] { BossState.Idle } },
            { BossState.Idle, new[] { BossState.SlamWindup, BossState.RushWindup, BossState.RainWindup, BossState.Dead } },
            { BossState.SlamWindup, new[] { BossState.SlamFall, BossState.Dead } },
            { BossState.SlamFall, new[] { BossState.SlamRecover, BossState.Dead } },
            { BossState.SlamRecover, new[] { BossState.Idle, BossState.Dead } },
            { BossState.RushWindup, new[] { BossState.Rush, BossState.Dead } },
            { BossState.Rush, new[] { BossState.RushRecover, BossState.Dead } },
            { BossState.RushRecover, new[] { BossState.Idle, BossState.Dead } },
            { BossState.RainWindup, new[] { BossState.Rain, BossState.Dead } },
            { BossState.Rain, new[] { BossState.RainRecover, BossState.Dead } },
            { BossState.RainRecover, new[] { BossState.Idle, BossState.Dead } },
            { BossState.Dead, new BossState[0] },
        };

        public Boss(Vec2 feet, BossTuning t)
        {
            this.t = t;
            home = new Vec2(feet.X, feet.Y + 1.4f + 0.001f);
            Body = new Body(home, 2.6f, 2.8f);
            Fsm = new StateMachine<BossState>(BossState.Dormant, Allowed);
            MaxHealth = Health = t.Health;
        }

        public BossState State => Fsm.State;
        public bool Dead => State == BossState.Dead;
        public bool Awake => State != BossState.Dormant;
        float Speed => Phase2 ? t.Phase2Speed : 1f;
        float RecoverMul => Phase2 ? t.Phase2Recover : 1f;

        /// <summary>Touching the bell hurts, except while it is helpless in a recovery (the punish window).</summary>
        public bool Harmful => State != BossState.Dormant && State != BossState.Intro && State != BossState.Dead &&
                               State != BossState.SlamRecover && State != BossState.RushRecover && State != BossState.RainRecover;

        public bool Vulnerable => State != BossState.Dormant && State != BossState.Intro && State != BossState.Dead;

        /// <summary>0..1 progress of the current wind-up, for the view's tell.</summary>
        public float WindupProgress
        {
            get
            {
                switch (State)
                {
                    case BossState.SlamWindup: return Mathx.Clamp01(Fsm.Time / (t.SlamWindup / Speed));
                    case BossState.RushWindup: return Mathx.Clamp01(Fsm.Time / (t.RushWindup / Speed));
                    case BossState.RainWindup: return Mathx.Clamp01(Fsm.Time / (t.RainWindup / Speed));
                    default: return 0f;
                }
            }
        }

        /// <summary>A new fight order: called when the run starts and after every death.</summary>
        public void Reseed(int seed)
        {
            dice.Reseed(seed);
            slamKinds.Reset();
            rainBag.Reset();
            comboBag.Reset();
            lastKind = -1;
            repeats = 0;
            idleFor = t.IdleTime * dice.Range(0.55f, 1.6f);
        }

        public void Reset()
        {
            Body.Pos = Body.Prev = home;
            Body.Vel = new Vec2(0, 0);
            Health = MaxHealth = t.Health;
            Facing = -1;
            Flash = 0f;
            Phase2 = false;
            AttackCount = 0;
            lastSwing = -1;
            slamHold = 0f;
            echoIn = -1f;
            comboLeft = 0;
            Waves.Clear();
            Drips.Clear();
            Fsm.Reset();
        }

        public void Awaken(World w)
        {
            if (Fsm.Go(BossState.Intro)) w.Emit(GameEventType.BossAwake, Body.Pos, 0);
        }

        public bool TakeHit(World w, int swingId, int damage)
        {
            if (!Vulnerable || swingId == lastSwing) return false;
            lastSwing = swingId;
            Health -= damage;
            Flash = 0.1f;
            w.Emit(GameEventType.BossHit, Body.Pos, Health);
            if (!Phase2 && Health <= MaxHealth / 2)
            {
                Phase2 = true;
                w.Emit(GameEventType.BossPhase2, Body.Pos, 0);
            }
            if (Health <= 0)
            {
                Health = 0;
                Fsm.Go(BossState.Dead);
                echoIn = -1f;
                Waves.Clear();
                Drips.Clear();
                w.Emit(GameEventType.BossDefeated, Body.Pos, 0);
            }
            return true;
        }

        public void Update(World w, float dt)
        {
            Fsm.Tick(dt);
            if (Flash > 0f) Flash -= dt;
            UpdateHazards(w, dt);
            if (State == BossState.Dormant || State == BossState.Dead)
            {
                Fall(w, dt);
                return;
            }
            Level lv = w.Level;
            Vec2 p = w.Player.Body.Pos;
            float dx = p.X - Body.Pos.X;
            bool gravity = true;

            switch (State)
            {
                case BossState.Intro:
                    Body.Vel.X = 0f;
                    if (Fsm.Time >= t.IntroTime) Fsm.Go(BossState.Idle);
                    break;
                case BossState.Idle:
                    Facing = dx >= 0f ? 1 : -1;
                    Body.Vel.X = Mathx.MoveToward(Body.Vel.X, 0f, 30f * dt);
                    if (Fsm.Time >= (comboLeft > 0 ? 0.1f : idleFor / Speed)) ChooseAttack(w, dx);
                    break;
                case BossState.SlamWindup:
                {
                    // Rise above the floor and drift over the diver: the shadow under it shows where it will land.
                    gravity = false;
                    float k = WindupProgress;
                    Body.Pos.Y = riseFromY + t.SlamRise * (1f - (1f - k) * (1f - k));
                    // A delayed slam hangs at the top: after the rise it holds still, then drops.
                    if (k >= 1f && slamHold > 0f) { Body.Vel = new Vec2(0f, 0f); if (Fsm.Time < t.SlamWindup / Speed + slamHold) break; }
                    // It follows only during the first 70% of the wind-up, then commits: the landing spot is
                    // readable, and the shockwave starts far enough away to react to.
                    float drift = k < 0.7f ? Mathx.Clamp(dx * 2f, -t.SlamDrift, t.SlamDrift) * Speed : 0f;
                    Body.Vel = new Vec2(drift, 0f);
                    if (k >= 1f)
                    {
                        Body.Vel = new Vec2(0f, -26f);
                        Fsm.Go(BossState.SlamFall);
                    }
                    break;
                }
                case BossState.SlamFall:
                    Body.Vel.X = 0f;
                    if (Body.Grounded)
                    {
                        float floor = Body.Bottom;
                        Waves.Add(new Shockwave { X = Body.Pos.X - Body.Half.X, Dir = -1, FloorY = floor });
                        Waves.Add(new Shockwave { X = Body.Pos.X + Body.Half.X, Dir = 1, FloorY = floor });
                        slamAt = Body.Pos;
                        slamFloor = floor;
                        if (Phase2) echoIn = t.SlamEcho;
                        Fsm.Go(BossState.SlamRecover);
                        w.Emit(GameEventType.BossSlam, Body.Feet, 0);
                    }
                    break;
                case BossState.SlamRecover:
                    Body.Vel.X = 0f;
                    if (Fsm.Time >= t.SlamRecover * RecoverMul)
                    {
                        // Phase two sometimes follows a slam with another at once.
                        if (Phase2 && comboBag.Next(dice) == 1) comboLeft = 1;
                        Fsm.Go(BossState.Idle);
                    }
                    break;
                case BossState.RushWindup:
                    // Lean away from the diver before swinging at them.
                    Body.Vel.X = -Facing * 0.8f;
                    if (WindupProgress >= 1f)
                    {
                        Fsm.Go(BossState.Rush);
                        w.Emit(GameEventType.BossRush, Body.Pos, Facing);
                    }
                    break;
                case BossState.Rush:
                    Body.Vel.X = Facing * t.RushSpeed * Speed;
                    break;
                case BossState.RushRecover:
                    Body.Vel.X = Mathx.MoveToward(Body.Vel.X, 0f, 40f * dt);
                    if (Fsm.Time >= t.RushRecover * RecoverMul)
                    {
                        // Phase two sometimes chains a slam straight out of a rush, with no idle beat.
                        if (Phase2 && comboBag.Next(dice) == 1) comboLeft = 1;
                        Fsm.Go(BossState.Idle);
                    }
                    break;
                case BossState.RainWindup:
                    Body.Vel.X = 0f;
                    if (WindupProgress >= 1f) Fsm.Go(BossState.Rain);
                    break;
                case BossState.Rain:
                    if (Drips.Count == 0 || Fsm.Time > 2.5f) Fsm.Go(BossState.RainRecover);
                    break;
                case BossState.RainRecover:
                    if (Fsm.Time >= t.RainRecover * RecoverMul) Fsm.Go(BossState.Idle);
                    break;
            }

            if (gravity) Body.Vel.Y = MathF.Max(Body.Vel.Y - 45f * dt, -30f);
            Physics.Move(lv, Body, dt);
            if (State == BossState.Rush && Body.HitWall)
            {
                Fsm.Go(BossState.RushRecover);
                w.Emit(GameEventType.BossWallHit, Body.Pos, Facing);
            }
        }

        void Fall(World w, float dt)
        {
            Body.Vel.X = 0f;
            Body.Vel.Y = MathF.Max(Body.Vel.Y - 45f * dt, -30f);
            Physics.Move(w.Level, Body, dt);
        }

        /// <summary>
        /// Picks the next attack at random, within what is fair. A rush only comes when the diver is far (6+ tiles), so it
        /// needs time to reach them and the side ledges stay reachable; up close it is always a slam. Rain joins in
        /// phase two. No attack is picked three times in a row. Slams may hang (a delayed swing) and, in phase two,
        /// may chain into another slam. Even the pause between attacks varies from fight to fight.
        /// </summary>
        void ChooseAttack(World w, float dx)
        {
            AttackCount++;
            bool combo = comboLeft > 0;
            comboLeft = 0;
            bool far = MathF.Abs(dx) > 6f;
            int kind = 0; // 0 slam, 1 rush, 2 rain
            if (!combo)
            {
                if (far && dice.Chance(t.FarRushChance)) kind = 1;
                if (Phase2 && rainBag.Next(dice) == 1) kind = 2;
                if (kind == lastKind && repeats >= 2) kind = kind == 0 && far ? 1 : 0;
            }
            repeats = kind == lastKind ? repeats + 1 : 1;
            lastKind = kind;
            idleFor = t.IdleTime * dice.Range(0.55f, 1.6f);
            slamHold = 0f;
            if (kind == 2)
            {
                Fsm.Go(BossState.RainWindup);
                SpawnDrips(w);
                w.Emit(GameEventType.BossRainWarn, Body.Pos, 0);
            }
            else if (kind == 1)
            {
                Fsm.Go(BossState.RushWindup);
                w.Emit(GameEventType.BossRushWindup, Body.Pos, Facing);
            }
            else
            {
                slamHold = slamKinds.Next(dice) == 1 ? t.SlamHold : 0f;
                riseFromY = Body.Pos.Y;
                Fsm.Go(BossState.SlamWindup);
                w.Emit(GameEventType.BossSlamWindup, Body.Pos, 0);
            }
        }

        void SpawnDrips(World w)
        {
            RoomInfo room = w.Level.Rooms[w.ArenaRoom];
            float px = w.Player.Body.Pos.X;
            float ceiling = room.MaxY - 2.2f;
            for (int i = 0; i < t.RainDrops; i++)
            {
                float off = (i - (t.RainDrops - 1) * 0.5f) * 2.4f;
                float x = Mathx.Clamp(px + off, room.MinX + 1.6f, room.MaxX - 1.6f);
                Drips.Add(new Drip { X = x, Y = ceiling, Warn = t.RainWindup / Speed + i * 0.12f });
            }
        }

        public static Aabb WaveBox(Shockwave s, BossTuning t) => new Aabb(new Vec2(s.X, s.FloorY + t.WaveHeight * 0.5f), new Vec2(0.4f, t.WaveHeight * 0.5f));
        public static Aabb DripBox(Drip d) => new Aabb(new Vec2(d.X, d.Y), new Vec2(0.32f, 0.45f));

        void UpdateHazards(World w, float dt)
        {
            Level lv = w.Level;
            if (echoIn >= 0f && (echoIn -= dt) < 0f)
            {
                Waves.Add(new Shockwave { X = slamAt.X - Body.Half.X, Dir = -1, FloorY = slamFloor });
                Waves.Add(new Shockwave { X = slamAt.X + Body.Half.X, Dir = 1, FloorY = slamFloor });
                w.Emit(GameEventType.RippleToll, new Vec2(slamAt.X, slamFloor));
            }
            for (int i = Waves.Count - 1; i >= 0; i--)
            {
                Shockwave s = Waves[i];
                if (!s.Advance(lv, t.WaveSpeed * Speed * dt)) Waves.RemoveAt(i);
            }
            for (int i = Drips.Count - 1; i >= 0; i--)
            {
                Drip d = Drips[i];
                if (d.Warn > 0f) { d.Warn -= dt; continue; }
                d.Y -= t.RainFallSpeed * dt;
                float floor = lv.FloorBelow(d.X, d.Y + 0.5f);
                if (d.Y - 0.45f <= floor)
                {
                    Drips.RemoveAt(i);
                    w.Emit(GameEventType.DripSplash, new Vec2(d.X, floor), 0);
                }
            }
        }
    }
}
