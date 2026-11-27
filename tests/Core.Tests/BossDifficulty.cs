using System;
using System.Collections.Generic;
using System.Linq;
using DrownedBell.Core;

/// <summary>
/// How hard is each boss? Three scripted divers fight it from the arena entrance:
///  - the READER sees every tell perfectly and plays it with ordinary inputs (move, dash, jump, aimed swing).
///    The boss must be beatable by it, but not for free: it has to take real time and may still make mistakes.
///  - the GREEDY diver walks up and swings non-stop without ever dodging. A boss that punishes greed kills it fast
///    and never lets it win, however many tries it gets.
///  - the IDLE diver does nothing, to show every boss attacks on its own.
/// Each death restarts the fight (the boss heals fully) with the diver back at the arena entrance.
/// DIFFICULTY_TABLE=1 dotnet run prints the table.
/// </summary>
internal static class BossDifficulty
{
    const float Dt = World.FixedDt;

    public readonly struct Result
    {
        public readonly bool Won;
        public readonly int Deaths;
        public readonly float Seconds, FirstDeathAt;
        public Result(bool won, int deaths, float seconds, float firstDeathAt) { Won = won; Deaths = deaths; Seconds = seconds; FirstDeathAt = firstDeathAt; }
        public override string ToString() =>
            (Won ? "won" : "NOT won") + ", " + Deaths + " death" + (Deaths == 1 ? "" : "s") + ", " + Seconds.ToString("0") + " s" +
            (FirstDeathAt >= 0 ? ", first death at " + FirstDeathAt.ToString("0.0") + " s" : "");
    }

    public enum Style { Reader, Greedy, Idle }

    /// <summary>`retryAt` is where the diver stands again after each death: the fight is measured, not the run back to it.</summary>
    static Result Trial(World w, Func<PlayerInput> router, Func<bool> done, float capSeconds, int maxDeaths, Vec2 retryAt)
    {
        float firstDeath = -1f, start = w.Time;
        w.Event += e =>
        {
            if (e.Type == GameEventType.PlayerDied && firstDeath < 0f) firstDeath = w.Time - start;
            if (e.Type != GameEventType.Respawned) return;
            w.Player.Place(new Vec2(retryAt.X, w.Level.FloorBelow(retryAt.X, retryAt.Y + 0.5f)), w.Move);
        };
        while (!done() && w.Time - start < capSeconds && w.Deaths < maxDeaths) w.Update(Dt, router());
        return new Result(done(), w.Deaths, w.Time - start, firstDeath);
    }

    static PlayerInput Greedy(World w, float bossX)
    {
        float feetX = w.Player.Body.Feet.X, dx = bossX - feetX;
        return new PlayerInput
        {
            MoveX = System.MathF.Abs(dx) > 2.2f ? Mathx.Sign(dx) : 0f, AttackPressed = !w.Player.Attacking,
            HasAim = true, AimX = dx,
        };
    }

    public static Result Undertoll(NavGraph closed, NavGraph open, Style style, float cap = 600f, int maxDeaths = 12, int seed = World.DefaultSeed)
    {
        var w = new World(seed: seed);
        w.Level.GrateOpen = true;
        w.Player.Place(w.Level.Checkpoint, w.Move);
        w.Update(Dt, default);
        RoomInfo arena = w.Level.Rooms[w.ArenaRoom];
        var pilot = new Autopilot(w, closed, open, 0);
        pilot.StartAt(Autopilot.Goal.Arena);
        var at = new Vec2(arena.MinX + 8.5f, arena.MinY + 6f);
        w.Player.Place(new Vec2(at.X, w.Level.FloorBelow(at.X, at.Y)), w.Move);
        if (Environment.GetEnvironmentVariable("DIFFICULTY_TRACE") == "1" && style == Style.Reader)
            w.Event += e =>
            {
                if (e.Type == GameEventType.PlayerHurt || e.Type == GameEventType.PlayerDied)
                    Console.WriteLine($"   {w.Time:0.0} {e.Type} hp {e.Id} boss {w.Boss.State} t{w.Boss.Fsm.Time:0.00} hpB {w.Boss.Health} p2 {w.Boss.Phase2} bx {w.Boss.Body.Pos.X:0.0} diver {w.Player.Body.Pos.X:0.0},{w.Player.Body.Pos.Y:0.0} waves {w.Boss.Waves.Count} drips {w.Boss.Drips.Count}");
            };
        Func<PlayerInput> router = style == Style.Reader ? pilot.Next : style == Style.Greedy ? () => Greedy(w, w.Boss.Body.Pos.X) : () => default;
        return Trial(w, router, () => w.Boss.Dead, cap, maxDeaths, at);
    }

    public static Result Warden(Style style, float cap = 600f, int maxDeaths = 12, int seed = World.DefaultSeed)
    {
        var w = new World(level: SluiceWorks.Build(), seed: seed);
        w.Level.GrateOpen = true;
        w.Player.Place(w.Level.LiftLamp.Value, w.Move);
        w.Update(Dt, default);
        RoomInfo room = w.Level.Rooms[w.Warden.Room];
        if (style == Style.Idle) w.Player.Place(new Vec2(w.Warden.Home.X - 6f, w.Level.FloorBelow(w.Warden.Home.X - 6f, room.MinY + 3f)), w.Move);
        Func<PlayerInput> router = style == Style.Reader ? () => SluiceWorks.RouteInput(w, takeRecord: false) : style == Style.Greedy ? () => Greedy(w, w.Warden.Home.X) : () => default;
        return Trial(w, router, () => !w.Warden.Alive, cap, maxDeaths, new Vec2(style == Style.Idle ? w.Warden.Home.X - 6f : room.MinX + 3f, room.MinY + 3f));
    }

    public static Result Archivist(Style style, float cap = 600f, int maxDeaths = 12, int seed = World.DefaultSeed)
    {
        var w = new World(level: SoundingArchive.Build(), seed: seed);
        w.Level.GrateOpen = true;
        w.Widow.Alive = false;
        w.Level.KeyOpen = true;
        w.Player.Place(w.Level.Checkpoint, w.Move);
        w.Update(Dt, default);
        RoomInfo room = w.Level.Rooms[w.Archivist.Room];
        if (Environment.GetEnvironmentVariable("DIFFICULTY_TRACE") == "1" && style == Style.Reader)
            w.Event += e =>
            {
                if (e.Type == GameEventType.PlayerHurt) Console.WriteLine($"   {w.Time:0.0} hurt (hp {e.Id}) boss {w.Archivist.Beat} t{w.Archivist.Elapsed:0.00} dives {w.Archivist.Dives} p2 {w.Archivist.SecondPhase} hp {w.Archivist.Health} diver {w.Player.Body.Pos.X:0.0} body {w.Archivist.Body.Pos.X:0.0} mark {w.Archivist.RealMark.X:0.0} waves {w.Archivist.Waves.Count}");
            };
        if (style == Style.Idle) w.Player.Place(new Vec2(w.Archivist.Home.X - 6f, w.Level.FloorBelow(w.Archivist.Home.X - 6f, room.MinY + 3f)), w.Move);
        Func<PlayerInput> router = style == Style.Reader ? () => SoundingArchive.RouteInput(w, takeLedger: false)
            : style == Style.Greedy ? () => Greedy(w, w.Archivist.Engaged ? w.Archivist.Body.Pos.X : w.Archivist.Home.X) : () => default;
        return Trial(w, router, () => !w.Archivist.Alive, cap, maxDeaths, new Vec2(style == Style.Idle ? w.Archivist.Home.X - 6f : room.MinX + 4f, room.MinY + 3f));
    }

    /// <summary>The reader and the greedy diver against several run seeds: random fights must stay beatable and stay punishing for every order.</summary>
    public static void SeedTable(int seeds = 8)
    {
        var closed = new NavGraph(false);
        var open = new NavGraph(true);
        foreach (var (name, run) in new (string, Func<Style, int, Result>)[]
        {
            ("Undertoll", (s, seed) => Undertoll(closed, open, s, 420f, 8, seed)),
            ("Pump Warden", (s, seed) => Warden(s, 420f, 8, seed)),
            ("Archivist", (s, seed) => Archivist(s, 420f, 8, seed)),
        })
        {
            var readers = new List<Result>();
            var greedy = new List<Result>();
            for (int i = 1; i <= seeds; i++)
            {
                int seed = i * 7919;
                readers.Add(run(Style.Reader, seed));
                greedy.Add(run(Style.Greedy, seed));
            }
            Console.WriteLine($"{name,-12} reader : " + string.Join("  ", readers.Select(r => r.Won ? r.Deaths + "d/" + r.Seconds.ToString("0") + "s" : "NOT(" + r.Deaths + "d)")));
            Console.WriteLine($"{name,-12} greedy : " + string.Join("  ", greedy.Select(r => (r.Won ? "WON" : "first death " + (r.FirstDeathAt < 0 ? "never" : r.FirstDeathAt.ToString("0") + "s")))));
        }
    }

    public static void Table()
    {
        var closed = new NavGraph(false);
        var open = new NavGraph(true);
        string only = Environment.GetEnvironmentVariable("DIFFICULTY_ONLY");
        foreach (var (name, run) in new (string, Func<Style, Result>)[]
        {
            ("Undertoll", s => Undertoll(closed, open, s)), ("Pump Warden", s => Warden(s)), ("Archivist", s => Archivist(s)),
        })
            foreach (Style s in Enum.GetValues(typeof(Style)))
                if (only == null || name.StartsWith(only)) Console.WriteLine($"{name,-12} {s,-7} {run(s)}");
    }
}
