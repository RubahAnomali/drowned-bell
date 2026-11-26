using System;
using System.Collections.Generic;

namespace DrownedBell.Core
{
    public enum Phase { Playing, Dying, Victory }

    public enum GameEventType
    {
        Jump, Land, Dash, Swing, Pogo,
        HitEnemy, EnemyKilled, PlayerHurt, HazardHit, PlayerDied, Respawned,
        LampLit, Rested, WinchPulled, LungTaken, RoomEntered,
        CrabWindup, CrabLunge, JellyWindup, JellyPulse,
        RippleWindup, RippleToll,
        BossAwake, BossSlamWindup, BossSlam, BossRushWindup, BossRush, BossWallHit, BossRainWarn, DripSplash,
        BossHit, BossPhase2, BossDefeated, Victory,
    }

    public struct GameEvent
    {
        public GameEventType Type;
        public Vec2 Pos;
        public int Id;
    }

    /// <summary>
    /// The whole demo as plain C#: no UnityEngine, no rendering. Time moves only through Update(FixedDt), and
    /// everything that happens is reported through Event, so it runs headless in tests.
    /// </summary>
    public sealed class World
    {
        public const float FixedDt = 1f / 60f;
        public const float DeathDelay = 1.6f;
        public const float VictoryDelay = 3.2f;

        public readonly MovementTuning Move;
        public readonly CombatTuning Combat;
        public readonly EnemyTuning Enemies;
        public readonly BossTuning BossT;
        public readonly Level Level;
        public readonly Player Player;
        public readonly List<Enemy> Foes = new List<Enemy>();
        public readonly List<RippleBell> RippleBells = new List<RippleBell>();
        public readonly Boss Boss;
        public readonly int ArenaRoom;

        public Phase Phase { get; private set; } = Phase.Playing;
        public float Time { get; private set; }
        public int Deaths { get; private set; }
        public int Room { get; private set; }
        public bool LampLit { get; private set; }
        public bool LungTaken { get; private set; }
        public bool GrateOpen => Level.GrateOpen;
        public Vec2 RespawnFeet { get; private set; }
        /// <summary>Seconds of hit-stop requested by the last impact; the game loop consumes it.</summary>
        public float HitStop;
        public event Action<GameEvent> Event;

        float phaseTimer;
        /// <summary>Separate from the death timer so a death in the same frame as the bell's can't overwrite it.</summary>
        float victoryTimer;
        bool victoryPending, victoryDone;

        public World(MovementTuning move = null, CombatTuning combat = null, EnemyTuning enemies = null, BossTuning boss = null, Level level = null)
        {
            Move = move ?? new MovementTuning();
            Combat = combat ?? new CombatTuning();
            Enemies = enemies ?? new EnemyTuning();
            BossT = boss ?? new BossTuning();
            Level = level ?? Level.Build();
            Player = new Player(Level.PlayerStart, Move, Combat.MaxHealth);
            RespawnFeet = Level.PlayerStart;
            int id = 0;
            foreach (Spawn s in Level.Spawns)
            {
                if (s.Kind == 'W') RippleBells.Add(new RippleBell(s, Level));
                else if (s.Kind == 'T') Foes.Add(new Dummy(id++, s.Pos, s.Room));
                else if (s.Kind == 'c') Foes.Add(new Crab(id++, s.Pos, s.Room, Enemies));
                else Foes.Add(new Jelly(id++, s.Pos, s.Room, Enemies));
            }
            Boss = new Boss(Level.BossSpawn, BossT);
            ArenaRoom = Level.HasBoss ? Level.RoomIndexAt(new Vec2(Level.BossSpawn.X, Level.BossSpawn.Y + 1f)) : -1;
            Room = Level.RoomIndexAt(Player.Body.Pos);
        }

        public void Emit(GameEventType type, Vec2 pos, int id = 0)
        {
            Event?.Invoke(new GameEvent { Type = type, Pos = pos, Id = id });
        }

        public void Update(float dt, PlayerInput input)
        {
            if (Phase == Phase.Dying)
            {
                phaseTimer -= dt;
                if (phaseTimer <= 0f) Respawn();
                return;
            }
            if (Phase == Phase.Victory) return;
            Time += dt;

            PlayerFrame pf = Player.Update(dt, input, Level, Move, Combat);
            Vec2 pp = Player.Body.Pos;
            if (pf.Jumped) Emit(GameEventType.Jump, Player.Body.Feet);
            if (pf.Landed) Emit(GameEventType.Land, Player.Body.Feet);
            if (pf.Dashed) Emit(GameEventType.Dash, pp, Player.Facing);
            if (pf.Swung) Emit(GameEventType.Swing, pp, (int)Player.Attack);

            int room = Level.RoomIndexAt(pp);
            if (room >= 0 && room != Room)
            {
                Room = room;
                Emit(GameEventType.RoomEntered, pp, room);
            }

            Interactables();
            ResolvePlayerAttack();

            foreach (Enemy e in Foes) if (e.Room == Room) e.Update(this, dt);
            foreach (RippleBell bell in RippleBells) bell.Update(this, dt);
            UpdateArena(dt);

            ResolveDamage();
            Hazards();

            if (Player.Health <= 0) Die();
            if (victoryPending && Phase == Phase.Playing)
            {
                victoryTimer -= dt;
                if (victoryTimer <= 0f)
                {
                    victoryPending = false;
                    Phase = Phase.Victory;
                    Emit(GameEventType.Victory, pp);
                }
            }
        }

        // ---- interactables --------------------------------------------------------------------

        public static Aabb LampBox(Vec2 lampFeet) => new Aabb(new Vec2(lampFeet.X, lampFeet.Y + 1f), new Vec2(0.9f, 1f));
        public static Aabb WinchBox(Vec2 winchFeet) => new Aabb(new Vec2(winchFeet.X, winchFeet.Y + 0.7f), new Vec2(0.45f, 0.7f));
        public static Aabb LungBox(Vec2 lungFeet) => new Aabb(new Vec2(lungFeet.X, lungFeet.Y + 0.5f), new Vec2(0.4f, 0.45f));

        void Interactables()
        {
            Aabb pb = Player.Body.Box;
            if (pb.Overlaps(LampBox(Level.Checkpoint)))
            {
                if (!LampLit)
                {
                    LampLit = true;
                    RespawnFeet = Level.Checkpoint;
                    Player.Health = Player.MaxHealth;
                    Emit(GameEventType.LampLit, Level.Checkpoint);
                }
                else if (Player.Health < Player.MaxHealth)
                {
                    Player.Health = Player.MaxHealth;
                    Emit(GameEventType.Rested, Level.Checkpoint);
                }
            }
            if (!LungTaken && pb.Overlaps(LungBox(Level.Heart)))
            {
                LungTaken = true;
                Player.MaxHealth += 1;
                Player.Health = Player.MaxHealth;
                Emit(GameEventType.LungTaken, Level.Heart);
            }
        }

        // ---- combat ---------------------------------------------------------------------------

        void ResolvePlayerAttack()
        {
            if (!Player.AttackActive(Combat)) return;
            Aabb hit = Player.AttackBox(Combat, Move);
            int swing = Player.SwingId;
            bool connected = false;

            foreach (Enemy e in Foes)
            {
                if (e.Room != Room || !e.Alive || !hit.Overlaps(e.Body.Box)) continue;
                if (e.TakeHit(this, swing, 1, Player.Body.Pos.X))
                {
                    connected = true;
                    HitStop = Combat.HitStop;
                    Emit(GameEventType.HitEnemy, e.Body.Pos, e.Id);
                }
            }
            if (ArenaRoom >= 0 && Room == ArenaRoom && hit.Overlaps(Boss.Body.Box) && Boss.TakeHit(this, swing, 1))
            {
                connected = true;
                HitStop = Boss.Dead ? 0.12f : Combat.HitStop;
            }
            // The winch, and spikes for a down-swing pogo, also count as something to hit.
            if (!Level.GrateOpen && hit.Overlaps(WinchBox(Level.Lever)))
            {
                Level.GrateOpen = true;
                connected = true;
                Emit(GameEventType.WinchPulled, Level.Lever);
            }
            bool pogoSpikes = Player.Attack == AttackDir.Down && Level.TouchesSpikes(hit);
            if ((connected || pogoSpikes) && !Player.SwingConnected)
            {
                Player.OnSwingHit(Combat);
                if (Player.Attack == AttackDir.Down) Emit(GameEventType.Pogo, Player.Body.Feet);
            }
        }

        void ResolveDamage()
        {
            if (Player.Invuln > 0f || Phase != Phase.Playing) return;
            Aabb pb = Player.Body.Box;
            foreach (Enemy e in Foes)
            {
                if (e.Room != Room || !e.Alive) continue;
                if (e.Harmful && pb.Overlaps(e.Body.Box)) { HurtPlayer(e.Body.Pos.X); return; }
                if (e.TryAttackBox(out Aabb box) && pb.Overlaps(box)) { HurtPlayer(e.Body.Pos.X); return; }
                if (e is Jelly j && j.RingHits(Player.Body.Pos)) { HurtPlayer(e.Body.Pos.X); return; }
            }
            foreach (RippleBell bell in RippleBells)
                if (HitByWave(bell.Waves, pb)) return;
            if (ArenaRoom < 0 || Room != ArenaRoom) return;
            if (Boss.Harmful && pb.Overlaps(Boss.Body.Box)) { HurtPlayer(Boss.Body.Pos.X); return; }
            if (HitByWave(Boss.Waves, pb)) return;
            foreach (Drip d in Boss.Drips)
                if (d.Falling && pb.Overlaps(Boss.DripBox(d))) { HurtPlayer(d.X); return; }
        }

        bool HitByWave(List<Shockwave> waves, Aabb player)
        {
            foreach (Shockwave wave in waves)
            {
                if (!player.Overlaps(Boss.WaveBox(wave, BossT))) continue;
                HurtPlayer(wave.X - wave.Dir);
                return true;
            }
            return false;
        }

        public void HurtPlayer(float fromX)
        {
            if (Player.Invuln > 0f || Phase != Phase.Playing) return;
            Player.Health -= 1;
            Player.Hurt(fromX, Combat);
            HitStop = Combat.HitStop * 1.6f;
            Emit(GameEventType.PlayerHurt, Player.Body.Pos, Player.Health);
        }

        /// <summary>Spikes: lose a mask (unless still invulnerable) and go back to the last safe ledge.</summary>
        void Hazards()
        {
            if (Phase != Phase.Playing) return;
            var shrunk = new Aabb(Player.Body.Pos, new Vec2(Player.Body.Half.X - 0.08f, Player.Body.Half.Y - 0.08f));
            if (!Level.TouchesSpikes(shrunk)) return;
            if (Player.Invuln <= 0f) Player.Health -= 1;
            Emit(GameEventType.HazardHit, Player.Body.Pos, Player.Health);
            if (Player.Health <= 0) return;
            Vec2 safe = Player.LastSafe;
            Player.Place(new Vec2(safe.X, safe.Y - Player.Body.Half.Y), Move);
            Player.Stun = Combat.HazardStun;
            Player.Invuln = Combat.InvulnTime;
        }

        // ---- arena, death, respawn ------------------------------------------------------------

        void UpdateArena(float dt)
        {
            if (ArenaRoom < 0) return; // the test room has no arena
            RoomInfo arena = Level.Rooms[ArenaRoom];
            if (!Boss.Dead && !Boss.Awake && Room == ArenaRoom && Player.Body.Grounded && Player.Body.Pos.Y < arena.MaxY - 4f)
            {
                Level.DoorsLocked = true;
                Boss.Awaken(this);
            }
            if (Room == ArenaRoom || Boss.Awake) Boss.Update(this, dt);
            if (Boss.Dead && Level.DoorsLocked) Level.DoorsLocked = false;
            // However the bell falls, the run ends a few seconds later.
            if (Boss.Dead && !victoryPending && !victoryDone)
            {
                victoryPending = true;
                victoryDone = true;
                victoryTimer = VictoryDelay;
            }
        }

        void Die()
        {
            if (Phase != Phase.Playing) return;
            Phase = Phase.Dying;
            phaseTimer = DeathDelay;
            Deaths++;
            Emit(GameEventType.PlayerDied, Player.Body.Pos, Deaths);
        }

        /// <summary>
        /// Back to the lamp at full health. Ordinary enemies and an unfinished boss fight reset; the opened
        /// grate, the lit lamp and a taken Brass Lung stay as they are for the rest of the run.
        /// </summary>
        public void Respawn()
        {
            Player.Place(RespawnFeet, Move);
            Player.Health = Player.MaxHealth;
            foreach (Enemy e in Foes) e.Reset();
            foreach (RippleBell bell in RippleBells) bell.Reset();
            if (!Boss.Dead) Boss.Reset();
            Level.DoorsLocked = false;
            Phase = Phase.Playing;
            HitStop = 0f;
            Room = Level.RoomIndexAt(Player.Body.Pos);
            Emit(GameEventType.Respawned, Player.Body.Pos);
        }
    }
}
