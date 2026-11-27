using System;
using System.Collections.Generic;

namespace DrownedBell.Core
{
    public enum KeeperAttack { Hammer, Wave, Lane, Echo }
    public enum KeeperBeat { Dormant, Idle, Windup, Strike, Recover, Rest }

    /// <summary>
    /// The Tidekeeper, last boss of the Tidal Engine. It stands its ground and turns to face the diver, then picks an
    /// attack at random (see Dice): a hammer (a low sweep ahead of it), a floor wave (a crest each way along the floor),
    /// a pressure lane (a long horizontal jet at chest height) or an echo (a ripple pair and a second pair 0.75 s later).
    /// Every attack locks its facing when it begins and every one ends in a reset where its hinge is exposed: the only time
    /// it can be hurt. Below half health it fights in phrases of three attacks, each phrase ending in a grounded resting
    /// beat. Standing behind it is always safe from the hammer and the lane (they only reach the side it faces).
    /// </summary>
    public sealed class Tidekeeper : Enemy
    {
        public const float HammerReach = 3.8f, LaneReach = 7.5f, WaveReach = 12f;
        public KeeperBeat Beat { get; private set; }
        public KeeperAttack Attack { get; private set; }
        public float Elapsed { get; private set; }
        public bool Engaged => Beat != KeeperBeat.Dormant;
        public bool SecondPhase => Health * 2 <= MaxHealth;
        public readonly List<Shockwave> Waves = new List<Shockwave>(6);
        /// <summary>Seconds until the echo's second pair, or negative when none is pending.</summary>
        public float EchoIn => echoIn;

        readonly Dice dice = new Dice(4);
        readonly Bag attackBag = new Bag(0, 1, 2, 3);
        int phraseLeft, lastAttack = -1, repeats;
        float idleFor = 0.8f, echoIn = -1f;
        bool struck;

        public Tidekeeper(int id, Vec2 feet, int room) : base(id, EnemyKind.Tidekeeper, feet, room, 2.4f, 3.6f, 56) { }

        public override string StateName => Beat + (Beat == KeeperBeat.Windup || Beat == KeeperBeat.Strike ? " " + Attack : "");
        public override int RejectedTransitions => 0;
        public override bool IsBoss => true;
        public override bool Harmful => false;
        public override int AttackDamage => Attack == KeeperAttack.Hammer && Beat == KeeperBeat.Strike ? 2 : 1;

        /// <summary>The wind-up of each attack: the telegraph. Never below 0.45 s.</summary>
        public float WindupTime(KeeperAttack a) => SecondPhase
            ? (a == KeeperAttack.Hammer ? 0.75f : a == KeeperAttack.Lane ? 0.55f : 0.65f)
            : (a == KeeperAttack.Hammer ? 0.9f : a == KeeperAttack.Lane ? 0.7f : 0.8f);
        public float StrikeTime(KeeperAttack a) => a == KeeperAttack.Hammer ? 0.3f : a == KeeperAttack.Wave ? 0.15f : a == KeeperAttack.Lane ? 0.6f : 0.3f;
        /// <summary>The reset after every attack, with the hinge exposed. Never below 0.35 s.</summary>
        public float RecoverTime => SecondPhase ? 0.65f : 0.8f;
        public float RestTime => 0.7f;
        public float WindupProgress => Beat == KeeperBeat.Windup ? Mathx.Clamp01(Elapsed / WindupTime(Attack)) : 0f;

        /// <summary>For tests: takes `damage` off the health without the usual rules.</summary>
        public void TakeHitDirect(int damage) { Health -= damage; }

        public void Reseed(int seed)
        {
            dice.Reseed(seed);
            attackBag.Reset();
            lastAttack = -1;
            repeats = 0;
            idleFor = dice.Range(0.45f, 0.8f);
        }

        void Enter(KeeperBeat next) { Beat = next; Elapsed = 0f; }

        public override void Reset()
        {
            base.Reset();
            Enter(KeeperBeat.Dormant);
            Waves.Clear();
            echoIn = -1f;
            phraseLeft = 0;
            struck = false;
        }

        public override bool TakeHit(World w, int swingId, int damage, float fromX)
        {
            if (Beat != KeeperBeat.Recover) return false;
            bool before = SecondPhase;
            bool hit = base.TakeHit(w, swingId, damage, fromX);
            if (hit && !before && SecondPhase && Alive) w.Emit(GameEventType.TidekeeperPhase2, Body.Pos, Id);
            if (hit && !Alive)
            {
                Waves.Clear();
                echoIn = -1f;
                w.Emit(GameEventType.TidekeeperDefeated, Body.Pos, Id);
            }
            return hit;
        }

        public override bool TryAttackBox(out Aabb box)
        {
            box = default;
            if (!Alive || Beat != KeeperBeat.Strike) return false;
            if (Attack == KeeperAttack.Hammer)
            {
                box = new Aabb(new Vec2(Home.X + Facing * (0.4f + HammerReach) * 0.5f, Body.Bottom + 1.3f), new Vec2((HammerReach - 0.4f) * 0.5f, 1.3f));
                return true;
            }
            if (Attack == KeeperAttack.Lane)
            {
                box = new Aabb(new Vec2(Home.X + Facing * (0.3f + LaneReach) * 0.5f, Body.Bottom + 0.95f), new Vec2((LaneReach - 0.3f) * 0.5f, 0.45f));
                return true;
            }
            return false;
        }

        void SpawnPair(World w)
        {
            Waves.Add(new Shockwave { X = Home.X - Body.Half.X, FloorY = Body.Bottom, Dir = -1 });
            Waves.Add(new Shockwave { X = Home.X + Body.Half.X, FloorY = Body.Bottom, Dir = 1 });
            w.Emit(GameEventType.TidekeeperWave, new Vec2(Home.X, Body.Bottom), Id);
        }

        KeeperAttack Pick()
        {
            int pick = attackBag.Next(dice);
            // Never the same attack three times running.
            if (pick == lastAttack && repeats >= 2) pick = (pick + 1 + dice.Int(3)) % 4;
            repeats = pick == lastAttack ? repeats + 1 : 1;
            lastAttack = pick;
            return (KeeperAttack)pick;
        }

        public override void Update(World w, float dt)
        {
            if (Flash > 0f) Flash -= dt;
            for (int i = Waves.Count - 1; i >= 0; i--)
                if (!Waves[i].Advance(w.Level, w.BossT.WaveSpeed * dt) || MathF.Abs(Waves[i].X - Home.X) > WaveReach) Waves.RemoveAt(i);
            if (!Alive) return;
            if (echoIn >= 0f && (echoIn -= dt) < 0f) SpawnPair(w);
            Elapsed += dt;
            Vec2 feet = w.Player.Body.Feet;
            switch (Beat)
            {
                case KeeperBeat.Dormant:
                    if (w.Room == Room && MathF.Abs(feet.X - Home.X) < 14f)
                    {
                        Enter(KeeperBeat.Idle);
                        Facing = feet.X >= Home.X ? 1 : -1;
                        w.Emit(GameEventType.TidekeeperAwake, Body.Pos, Id);
                    }
                    break;
                case KeeperBeat.Idle:
                    Facing = feet.X >= Home.X ? 1 : -1;
                    if (Elapsed >= idleFor)
                    {
                        Attack = Pick();
                        if (SecondPhase && phraseLeft == 0) phraseLeft = 3;
                        struck = false;
                        Enter(KeeperBeat.Windup);   // the facing stays locked from here to the end of the recovery
                        w.Emit(GameEventType.TidekeeperWindup, Body.Pos, (int)Attack);
                    }
                    break;
                case KeeperBeat.Windup:
                    if (Elapsed >= WindupTime(Attack))
                    {
                        Enter(KeeperBeat.Strike);
                        w.Emit(GameEventType.TidekeeperStrike, Body.Pos, (int)Attack);
                        if (Attack == KeeperAttack.Wave || Attack == KeeperAttack.Echo) SpawnPair(w);
                        if (Attack == KeeperAttack.Echo) echoIn = RippleBell.EchoDelay;
                    }
                    break;
                case KeeperBeat.Strike:
                    if (Elapsed >= StrikeTime(Attack)) Enter(KeeperBeat.Recover);
                    break;
                case KeeperBeat.Recover:
                    if (Elapsed >= RecoverTime)
                    {
                        idleFor = dice.Range(0.3f, 0.7f);
                        if (SecondPhase && --phraseLeft > 0) idleFor = 0.2f;                       // the next attack of a phrase follows at once
                        else if (SecondPhase) { Enter(KeeperBeat.Rest); break; }                  // then a grounded resting beat
                        Enter(KeeperBeat.Idle);
                    }
                    break;
                case KeeperBeat.Rest:
                    if (Elapsed >= RestTime) { idleFor = dice.Range(0.2f, 0.5f); Enter(KeeperBeat.Idle); }
                    break;
            }
            _ = struck;
        }
    }
}
