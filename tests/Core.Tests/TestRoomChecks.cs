using System;
using System.Linq;
using DrownedBell.Core;

/// <summary>Measures the real controller and checks the test room built around it.</summary>
internal static class TestRoomChecks
{
    public static MovementMetrics Run(Action<bool, string> ok)
    {
        Console.WriteLine("\nTest room and controller metrics");
        MovementMetrics mm = Metrics.Measure();
        Console.WriteLine("        " + mm);

        var w = new World(level: TestRoom.Build());
        ok(w.ArenaRoom < 0, "the test room has no boss arena");
        var dummy = w.Foes.OfType<Dummy>().FirstOrDefault();
        ok(dummy != null && w.Foes.OfType<Crab>().Count() == 1, "the test room has a target dummy and one crab");
        // Stand next to the dummy and swing five times.
        w.Player.Place(new Vec2(dummy.Body.Pos.X - 1.6f, dummy.Body.Bottom), w.Move);
        w.Player.Facing = 1;
        for (int s = 0; s < 5; s++)
        {
            w.Update(World.FixedDt, new PlayerInput { AttackPressed = true });
            for (int i = 0; i < 24; i++) w.Update(World.FixedDt, default);
        }
        ok(dummy.Hits == 5 && dummy.Alive, "five swings at the dummy count exactly five hits and it never dies (" + dummy.Hits + ")");

        // Which stairs and pits the real controller clears, with and without the dash.
        Level lv = TestRoom.Build();
        var dash = new NavGraph(false, level: lv);
        var walk = new NavGraph(false, level: TestRoom.Build(), allowDash: false);
        int floorY = lv.H - 15;               // top surface of the main floor
        int start = walk.Find(14, floorY);
        var fromStart = walk.From(start);
        string steps = "";
        int[] stairX = { 11, 8, 5, 2 };   // pillar of height 1, 2, 3, 4
        for (int h = 1; h <= 4; h++)
        {
            // A direct jump from the open floor (not stepping across from a neighbouring pillar).
            int top = walk.Find(stairX[h - 1], floorY + h);
            bool direct = Enumerable.Range(0, walk.Nodes.Count).Any(n => walk.Nodes[n].Y == floorY && walk.Out[n].Any(e => e.To == top));
            steps += h + ":" + (direct ? "yes" : "no") + " ";
        }
        Console.WriteLine("        stairs climbable from the floor (tiles): " + steps);
        ok(steps.Contains("1:yes") && steps.Contains("2:yes") && steps.Contains("3:yes") && steps.Contains("4:no"), "steps of 1 to 3 tiles are climbable, 4 is not");

        (int from, int to, int width)[] pits = { (16, 20, 4), (22, 28, 6), (30, 38, 8), (40, 50, 10) };
        foreach (var (a, b, width) in pits)
        {
            bool Crosses(NavGraph g)
            {
                int n = g.Find(a - 1, floorY);
                if (n < 0) return false;
                return g.Out[n].Any(e => g.Nodes[e.To].Y == floorY && g.Nodes[e.To].X >= b - 1);
            }
            Console.WriteLine("        pit " + width + " tiles: no-dash " + (Crosses(walk) ? "clears" : "falls in") + ", with dash " + (Crosses(dash) ? "clears" : "falls in"));
        }
        return mm;
    }
}
