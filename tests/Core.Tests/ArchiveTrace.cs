using System;
using DrownedBell.Core;

/// <summary>Debug aid: ARCHIVE_TRACE=1 dotnet run prints the Archive route every half second.</summary>
internal static class ArchiveTrace
{
    public static void Run()
    {
        var w = new World(level: SoundingArchive.Build());
        w.Event += e => { if (e.Type != GameEventType.Jump && e.Type != GameEventType.Land && e.Type != GameEventType.Swing) Console.WriteLine($"{w.Time,6:0.00} {e.Type} {e.Pos}"); };
        for (int i = 0; i < 60 * 90 && w.Phase != Phase.Victory; i++)
        {
            var input = SoundingArchive.RouteInput(w);
            if (i % 30 == 0) Console.WriteLine($"{w.Time,6:0.00} room {w.Room} feet {w.Player.Body.Feet} move {input.MoveX} jump {input.JumpPressed} down {input.Down} grounded {w.Player.Body.Grounded} widow {w.Widow.Beat} {w.Widow.Health}");
            w.Update(World.FixedDt, input);
        }
    }
}
