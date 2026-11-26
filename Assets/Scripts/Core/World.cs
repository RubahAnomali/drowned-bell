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
        BossHit, BossPhase2, BossDefeated, Victory, GaugeFound, LiftOnline, PumpKeyTaken, PumpSurge, MachineBrace, PipeWarning, BypassOnline,
        MothMark, MothDive, MothLodged, WidowSpread, WidowRing, ArchivistAwake, ArchivistMark, ArchivistLand, ArchivistPhase2, ArchivistDefeated,
        LedgerFound, DrumKeyTaken, OrderTaken, ChuteRide,
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
        public PumpMachine Warden { get; private set; }
        public bool GaugeFound { get; private set; }
        public bool BypassOpen { get; private set; }
        public bool LiftOnline { get; private set; }
        public bool PumpKey { get; private set; }
        public TuningWidow Widow { get; private set; }
        public Archivist Archivist { get; private set; }
        public bool LedgerFound { get; private set; }
        public bool DrumKey { get; private set; }
        public bool OrderTaken { get; private set; }
        bool upHeld;

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
                else if (s.Kind == 'p' || s.Kind == 'k' || s.Kind == 'M')
                {
                    var kind = s.Kind == 'M' ? EnemyKind.Warden : s.Kind == 'p' ? EnemyKind.Limpet : EnemyKind.Porter;
                    var machine = new PumpMachine(id++, kind, s.Pos, s.Room);
                    Foes.Add(machine);
                    if (machine.IsWarden) Warden = machine;
                }
                else if (s.Kind == 'O' || s.Kind == 'o') RippleBells.Add(new RippleBell(s, Level));
                else if (s.Kind == 'm') Foes.Add(new InkMoth(id++, s.Pos, s.Room));
                else if (s.Kind == 'w') Foes.Add(Widow = new TuningWidow(id++, s.Pos, s.Room));
                else if (s.Kind == 'Z') Foes.Add(Archivist = new Archivist(id++, s.Pos, s.Room));
                else if (s.Kind == 'T') Foes.Add(new Dummy(id++, s.Pos, s.Room));
                else if (s.Kind == 'c') Foes.Add(new Crab(id++, s.Pos, s.Room, Enemies));
                else Foes.Add(new Jelly(id++, s.Pos, s.Room, Enemies));
            }
            Boss = new Boss(Level.BossSpawn, BossT);
            ArenaRoom = Level.HasBoss ? Level.RoomIndexAt(new Vec2(Level.BossSpawn.X, Level.BossSpawn.Y + 1f)) : -1;
            Room = Level.RoomIndexAt(Player.Body.Pos);
        }

        public World EnterSluice()
        {
            if (Phase != Phase.Victory || !Level.HasBoss)
                throw new InvalidOperationException("The Sluice Works opens after the Undertoll falls.");
            var next = new World(Move, Combat, Enemies, BossT, SluiceWorks.Build());
            next.Time = Time;
            next.Deaths = Deaths;
            next.LungTaken = LungTaken;
            next.Player.MaxHealth = Player.MaxHealth;
            next.Player.Health = Player.MaxHealth;
            return next;
        }

        /// <summary>After the Pump Warden: the next chapter, carrying the upgrades, records, time and deaths.</summary>
        public World EnterArchive()
        {
            if (Phase != Phase.Victory || Chapter != Chapter.Sluice)
                throw new InvalidOperationException("The Sounding Archive opens after the Sluice Works.");
            var next = new World(Move, Combat, Enemies, BossT, SoundingArchive.Build());
            next.Time = Time;
            next.Deaths = Deaths;
            next.LungTaken = LungTaken;
            next.GaugeFound = GaugeFound;
            next.Player.MaxHealth = Player.MaxHealth;
            next.Player.Health = Player.MaxHealth;
            return next;
        }

        public Chapter Chapter => Level.Chapter;

        internal void SetArchiveFlag(GameEventType what, Vec2 at)
        {
            if (what == GameEventType.LedgerFound) LedgerFound = true;
            else if (what == GameEventType.DrumKeyTaken) { DrumKey = true; Level.KeyOpen = true; }
            else if (what == GameEventType.OrderTaken) OrderTaken = true;
            Emit(what, at);
        }

        /// <summary>A lift or chute ride: the diver appears at the other stop.</summary>
        internal void Ride(Vec2 to)
        {
            Player.Place(to, Move);
            Room = Level.RoomIndexAt(Player.Body.Pos);
            Emit(GameEventType.ChuteRide, to);
            Emit(GameEventType.RoomEntered, Player.Body.Pos, Room);
        }

        /// <summary>
        /// What a save keeps of this run. Once the Undertoll is down the Belfry is finished, so the save already
        /// points at the start of the Sluice Works, the same state EnterSluice would build.
        /// </summary>
        public SaveData Snapshot()
        {
            var s = new SaveData { Chapter = Chapter, Time = Time, Deaths = Deaths, Lung = LungTaken, Gauge = GaugeFound };
            if (Chapter == Chapter.Belfry && Level.HasBoss && Boss.Dead)
            {
                s.Chapter = Chapter.Sluice;
                return s;
            }
            // A finished chapter saves as the start of the next one.
            if (Chapter == Chapter.Sluice && Phase == Phase.Victory)
            {
                s.Chapter = Chapter.Archive;
                return s;
            }
            s.Lamp = LampLit;
            s.Gate = GrateOpen;
            if (Chapter == Chapter.Archive)
            {
                s.Ledger = LedgerFound;
                s.DrumKey = DrumKey;
                s.ArchivistDown = Archivist != null && !Archivist.Alive;
                s.Order = OrderTaken;
            }
            if (Chapter == Chapter.Sluice)
            {
                s.Bypass = BypassOpen;
                s.Lift = LiftOnline;
                s.Key = PumpKey;
                s.WardenDown = Warden != null && !Warden.Alive;
            }
            return s;
        }

        /// <summary>Rebuild a run from a save: everything opened stays open, and the diver wakes at the last lamp or lift.</summary>
        public static World Restore(SaveData s, MovementTuning move = null, CombatTuning combat = null, EnemyTuning enemies = null, BossTuning boss = null)
        {
            var w = new World(move, combat, enemies, boss,
                s.Chapter == Chapter.Sluice ? SluiceWorks.Build() : s.Chapter == Chapter.Archive ? SoundingArchive.Build() : null);
            w.Time = s.Time;
            w.Deaths = s.Deaths;
            w.LungTaken = s.Lung;
            w.Player.MaxHealth = w.Combat.MaxHealth + (s.Lung ? 1 : 0);
            w.LampLit = s.Lamp;
            w.Level.GrateOpen = s.Gate;
            if (s.Lamp) w.RespawnFeet = w.Level.Checkpoint;
            w.GaugeFound = s.Gauge;
            if (s.Chapter == Chapter.Archive)
            {
                w.LedgerFound = s.Ledger;
                w.DrumKey = w.Level.KeyOpen = s.DrumKey;
                if (s.ArchivistDown && w.Archivist != null) w.Archivist.Alive = false;
                w.OrderTaken = s.Order;
            }
            if (s.Chapter == Chapter.Sluice)
            {
                w.BypassOpen = s.Bypass;
                w.LiftOnline = s.Lift;
                w.PumpKey = s.Key;
                if (s.WardenDown && w.Warden != null) w.Warden.Alive = false;
                if (s.Lift && w.Level.LiftLamp is Vec2 lift) w.RespawnFeet = lift;
            }
            w.Player.Place(w.RespawnFeet, w.Move);
            w.Player.Health = w.Player.MaxHealth;
            w.Room = w.Level.RoomIndexAt(w.Player.Body.Pos);
            return w;
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
            if (Warden != null) SluiceStations(input.Up);
            if (Chapter == Chapter.Archive) SoundingArchive.Stations(this, input.Up, ref upHeld);
            ResolvePlayerAttack();

            foreach (Enemy e in Foes) if (e.Room == Room) e.Update(this, dt);
            foreach (RippleBell bell in RippleBells) bell.Update(this, dt);
            UpdateArena(dt);

            ResolveDamage();
            Hazards();

            if (Player.Health <= 0) Die();
            bool exitOpen = Chapter == Chapter.Archive ? OrderTaken : GrateOpen && PumpKey;
            if (Phase == Phase.Playing && exitOpen && Level.Outlet is Vec2 outlet && Player.Body.Box.Overlaps(LampBox(outlet)))
            {
                Phase = Phase.Victory;
                Emit(GameEventType.Victory, outlet);
            }
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
                    if (!LiftOnline) RespawnFeet = Level.Checkpoint;
                    Player.Health = Player.MaxHealth;
                    Emit(GameEventType.LampLit, Level.Checkpoint);
                }
                else if (Player.Health < Player.MaxHealth)
                {
                    Player.Health = Player.MaxHealth;
                    Emit(GameEventType.Rested, Level.Checkpoint);
                }
            }
            if (Level.HasHeart && !LungTaken && pb.Overlaps(LungBox(Level.Heart)))
            {
                LungTaken = true;
                Player.MaxHealth += 1;
                Player.Health = Player.MaxHealth;
                Emit(GameEventType.LungTaken, Level.Heart);
            }
        }

        void SluiceStations(bool up)
        {
            Vec2 feet = Player.Body.Feet;
            bool near(Vec2 p) => MathF.Abs(feet.X - p.X) < 1f && MathF.Abs(feet.Y - p.Y) < 1.25f;
            if (!GaugeFound && near(Level.Record.Value))
            {
                GaugeFound = true;
                Emit(GameEventType.GaugeFound, Level.Record.Value);
            }
            Vec2 lift = Level.LiftLamp.Value;
            if (near(lift) && (!LiftOnline || Player.Health < Player.MaxHealth))
            {
                LiftOnline = true;
                RespawnFeet = lift;
                Player.Health = Player.MaxHealth;
                Emit(GameEventType.LiftOnline, lift);
            }
            if (!BypassOpen && near(SluiceWorks.WellStop))
            {
                BypassOpen = true;
                Emit(GameEventType.BypassOnline, SluiceWorks.WellStop);
            }
            if (!Warden.Alive && !PumpKey && near(new Vec2(Warden.Home.X, Warden.Body.Bottom)))
            {
                PumpKey = true;
                Emit(GameEventType.PumpKeyTaken, Warden.Home);
            }
            if (up && !upHeld)
            {
                Vec2? target = null;
                if (near(SluiceWorks.TrapLift)) target = SluiceWorks.WellStop;
                else if (near(SluiceWorks.WellStop) || (LiftOnline && near(lift))) target = Level.Checkpoint;
                else if (near(Level.Checkpoint) && (LiftOnline || BypassOpen)) target = LiftOnline ? lift : SluiceWorks.WellStop;
                if (target.HasValue)
                {
                    Player.Place(target.Value, Move);
                    Room = Level.RoomIndexAt(Player.Body.Pos);
                    Emit(GameEventType.RoomEntered, Player.Body.Pos, Room);
                }
            }
            upHeld = up;
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
            foreach (Enemy e in Foes)
            {
                if (e.Room != Room) continue;
                if (e is TuningWidow tw && HitByWave(tw.Waves, pb)) return;
                if (e is Archivist ar && HitByWave(ar.Waves, pb)) return;
            }
            foreach (RippleBell bell in RippleBells)
                if (!bell.Harmless && HitByWave(bell.Waves, pb)) return;
            foreach (Vec2 vent in Level.Vents)
            {
                if (SluiceWorks.VentAt(Time, vent) == VentState.Blowing && pb.Overlaps(SluiceWorks.JetBox(vent)))
                {
                    HurtPlayer(vent.X);
                    return;
                }
            }
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
            foreach (Enemy e in Foes) if ((e != Warden && e != Archivist) || e.Alive) e.Reset();
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
