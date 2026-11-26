using System;
using System.Collections.Generic;
using System.Linq;
using DrownedBell.Core;

internal static class LevelPlanChecks
{
    static HashSet<(int Room, bool Open)> Explore(int start, bool open, bool useWinch = true)
    {
        var pending = new Queue<(int Room, bool Open)>();
        var seen = new HashSet<(int Room, bool Open)>();
        pending.Enqueue((start, open));
        while (pending.Count > 0)
        {
            var state = pending.Dequeue();
            if (!seen.Add(state)) continue;
            if (useWinch && state.Room == LevelPlan.WinchRoom && !state.Open)
                pending.Enqueue((state.Room, true));
            foreach (var exit in LevelPlan.Exits)
                if (exit.From == state.Room && (!exit.NeedsWinch || state.Open))
                    pending.Enqueue((exit.To, state.Open));
        }
        return seen;
    }

    public static void Run(Action<bool, string> check)
    {
        var beats = LevelPlan.Beats;
        int highRun = 0;
        bool paced = beats[0].Beat == Beat.Teach;
        for (int i = 0; i < beats.Length; i++)
        {
            highRun = beats[i].Intensity >= 0.7f ? highRun + 1 : 0;
            paced &= highRun <= 2 && (beats[i].Beat != Beat.Climax || i > 0 && beats[i - 1].Beat == Beat.Rest);
        }
        check(paced && beats.Length == Level.Build().Rooms.Count, "pacing starts with teaching and rests before the climax");
        var reachable = Explore(0, false);
        check(reachable.Contains((6, true)) && reachable.Contains((6, false)) &&
            !Explore(5, false, useWinch: false).Contains((0, false)),
            "winch acquisition permits the shortcut and the arena is reachable");
        check(reachable.Where(s => s.Room != 6).All(s => Explore(s.Room, s.Open).Any(r => r.Room == 0)),
            "every reachable pre-arena room has a route back to the lamp");

        MovementMetrics metrics = Metrics.Measure();
        int rise = (int)(metrics.FullJumpHeight * 0.7f), gap = (int)(metrics.RunningJumpReach * 0.7f);
        var graph = new NavGraph(false, allowDash: false);
        Level level = graph.Level;
        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            var from = graph.Nodes[i];
            graph.Out[i].RemoveAll(edge =>
            {
                var to = graph.Nodes[edge.To];
                if (to.Y - from.Y > rise) return true;
                if (from.Y != to.Y) return false;
                int unsupported = 0;
                for (int x = Math.Min(from.X, to.X) + 1; x < Math.Max(from.X, to.X); x++)
                {
                    unsupported = level.Solid(x, from.Y - 1) || level.OneWay(x, from.Y - 1) ? 0 : unsupported + 1;
                    if (unsupported > gap) return true;
                }
                return false;
            });
        }
        var path = graph.From(graph.NodeAt(level.PlayerStart));
        check(path.Contains(graph.NodeAt(level.Checkpoint)) && path.Contains(graph.NodeAt(level.Lever)) &&
            path.Any(n => graph.RoomOf(n) == 6), $"dash-free route reaches lamp, winch and arena with rises <= {rise} and pits <= {gap}");

        var world = new World();
        RippleBell bell = world.RippleBells[0];
        world.Player.Place(new Vec2(bell.Pos.X - 5f, bell.Floor), world.Move);
        bool warned = false, tolled = false;
        world.Event += e =>
        {
            if (e.Type == GameEventType.RippleWindup) warned = true;
            if (e.Type == GameEventType.RippleToll) tolled = warned;
        };
        for (int i = 0; i < 195; i++) world.Update(World.FixedDt, default);
        check(tolled && world.Player.Health == world.Player.MaxHealth - 1, "Ripple Walk telegraphs before a wave can hit the diver");
        world.Respawn();
        check(bell.Waves.Count == 0 && bell.Windup == 0f, "respawning clears ripple waves and wind-up");

        world.Player.Place(new Vec2(bell.Pos.X - 5f, bell.Floor), world.Move);
        bool jumped = false;
        for (int i = 0; i < 195; i++)
        {
            bool incoming = !jumped && bell.Waves.Any(wave =>
                wave.Dir < 0 && wave.X - world.Player.Body.Pos.X is > 0f and < 2f);
            jumped |= incoming;
            world.Update(World.FixedDt, new PlayerInput { JumpPressed = incoming, JumpHeld = jumped });
        }
        check(jumped && world.Player.Health == world.Player.MaxHealth, "the boss's jump timing also clears a training ripple");
    }
}
