using System;
using DrownedBell.Core;

/// <summary>Debug aid: BOSS_TRACE=1 dotnet run prints a fight between the autopilot and the bell.</summary>
internal static class BossTrace
{
    public static void Run()
    {
        var closed = new NavGraph(false);
        var open = new NavGraph(true);
        var w = new World();
        w.Level.GrateOpen = true;
        var ap = new Autopilot(w, closed, open, 0);
        // Skip the route: stand the diver in the arena and let the pilot take over from the Arena goal.
        RoomInfo arena = w.Level.Rooms[w.ArenaRoom];
        ap.StartAt(Autopilot.Goal.Arena);
        w.Player.Place(new Vec2(arena.MinX + 8.5f, w.Level.FloorBelow(arena.MinX + 8.5f, arena.MinY + 6f)), w.Move);
        w.Event += e =>
        {
            if (e.Type == GameEventType.PlayerHurt || e.Type == GameEventType.BossHit || e.Type == GameEventType.PlayerDied)
                Console.WriteLine("   " + w.Time.ToString("0.00") + " " + e.Type + " " + e.Id + "  boss " + w.Boss.State + " t" + w.Boss.Fsm.Time.ToString("0.00") + " bx " + w.Boss.Body.Pos.X.ToString("0.0") + " px " + w.Player.Body.Pos + " waves " + w.Boss.Waves.Count);
        };
        string last = "";
        for (int i = 0; i < 60 * 90 && w.Phase == Phase.Playing; i++)
        {
            PlayerInput inp = ap.Next();
            w.Update(World.FixedDt, inp);
            string st = w.Boss.State.ToString();
            if (st != last) { Console.WriteLine(" " + w.Time.ToString("0.00") + " boss -> " + st + "  bx " + w.Boss.Body.Pos.X.ToString("0.0") + " px " + w.Player.Body.Pos + " hp " + w.Boss.Health); last = st; }
        }
    }
}
