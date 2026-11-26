using System;
using System.Linq;
using DrownedBell.Core;

/// <summary>Debug aid: ROUTE_TRACE=1 dotnet run prints the open-grate path from the lamp to the arena.</summary>
internal static class RouteTrace
{
    public static void Run()
    {
        var g = new NavGraph(true);
        Level lv = g.Level;
        int lamp = g.NodeAt(lv.Checkpoint);
        int arena = lv.Rooms.FindIndex(r => r.Name == "The Undertoll");
        var path = g.Path(lamp, n => g.RoomOf(n) == arena);
        float t = 0f;
        int cur = lamp;
        foreach (NavEdge e in path)
        {
            t += e.Time;
            Console.WriteLine("  " + g.Nodes[cur] + " -> " + g.Nodes[e.To] + "  " + (e.Walk ? "walk" : "d" + e.Script.Dir + " h" + e.Script.Hold + " s" + e.Script.Dash + (e.Script.Drop ? " drop" : "")) + "  room " + lv.Rooms[g.RoomOf(e.To)].Name + "  t " + t.ToString("0.0"));
            cur = e.To;
        }
        // Is there any edge down into the grate shaft?
        foreach (var n in Enumerable.Range(0, g.Nodes.Count).Where(n => g.Nodes[n].Y >= 37 && g.Nodes[n].Y <= 38 && g.Nodes[n].X >= 23 && g.Nodes[n].X <= 31))
            Console.WriteLine("  R1 floor node " + g.Nodes[n] + " edges: " + string.Join(" ", g.Out[n].Select(e => g.Nodes[e.To].ToString())));
        foreach (var n in Enumerable.Range(0, g.Nodes.Count).Where(n => g.Nodes[n].X >= 26 && g.Nodes[n].X <= 28 && g.Nodes[n].Y > 18 && g.Nodes[n].Y < 37))
            Console.WriteLine("  shaft node " + g.Nodes[n] + " edges: " + string.Join(" ", g.Out[n].Select(e => g.Nodes[e.To].ToString())));
    }
}
