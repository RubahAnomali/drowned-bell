using System;
using System.Collections.Generic;
using System.Linq;
using DrownedBell.Core;

/// <summary>Chapter III, the Sounding Archive: map, foes, stations, route, retry time and saves.</summary>
internal static class ArchiveChecks
{
    const float Dt = World.FixedDt;

    static void Step(World w, float seconds, PlayerInput input = default)
    {
        int n = (int)MathF.Round(seconds / Dt);
        for (int i = 0; i < n; i++) w.Update(Dt, input);
    }

    static void Stand(World w, float x, float y)
    {
        w.Player.Place(new Vec2(x, w.Level.FloorBelow(x, y + 0.5f)), w.Move);
        w.Update(Dt, default);
    }

    public static void Run(Action<bool, string> ok)
    {
        Console.WriteLine("\nSounding Archive");
        var w = new World(level: SoundingArchive.Build());
        Level lv = w.Level;
        ok(lv.Chapter == Chapter.Archive && w.Chapter == Chapter.Archive && lv.Rooms.Count == 6 && lv.Outlet.HasValue && lv.Record.HasValue,
            "the Archive map has six rooms, a lamp, the loft record and an exit");
        ok(w.Foes.OfType<InkMoth>().Count() == 2 && w.Widow != null && w.Archivist != null &&
           w.RippleBells.Count(b => b.Harmless) == 1 && w.RippleBells.Count(b => b.Echo && !b.Harmless) == 1,
            "two moths, a widow, the Archivist, a harmless practice drum and one live echo drum");

        // The main path is flat floor from the lamp to the exit, apart from the drum-key door.
        bool flat = true;
        for (int x = 1; x < 159; x++) flat &= lv.Solid(x, 1) && !lv.Solid(x, 2) && !lv.Solid(x, 3);
        ok(!flat, "the key door blocks the corridor while locked");
        lv.KeyOpen = true;
        flat = true;
        for (int x = 1; x < 159; x++) flat &= lv.Solid(x, 1) && !lv.Solid(x, 2) && !lv.Solid(x, 3);
        ok(flat, "with the key door open the whole required route is level floor (no climb or gap)");
        lv.KeyOpen = false;
        var shelves = new List<int>();
        for (int y = 0; y < lv.H; y++) if (lv.OneWay(68, y)) shelves.Add(y);
        ok(shelves.Count >= 8 && shelves.Zip(shelves.Skip(1), (a, b) => b - a).All(d => d == 2) && shelves[0] == 3,
            "the optional Map Loft climb is a shelf every two tiles (" + string.Join(",", shelves) + ")");

        Drums(ok);
        Moth(ok);
        Widow(ok);
        Archivist(ok);
        Stations(ok);
        Route(ok);
        Saves(ok);
    }

    static void Drums(Action<bool, string> ok)
    {
        var w = new World(level: SoundingArchive.Build());
        RippleBell practice = w.RippleBells.First(b => b.Harmless);
        Stand(w, practice.Pos.X, practice.Floor);
        var tolls = new List<float>();
        w.Event += e => { if (e.Type == GameEventType.RippleToll) tolls.Add(w.Time); };
        int hp = w.Player.Health;
        Step(w, 5f);
        ok(tolls.Count >= 2 && MathF.Abs(tolls[1] - tolls[0] - RippleBell.EchoDelay) < 0.02f, "the practice drum rings twice, 0.75 s apart");
        ok(w.Player.Health == hp, "and its ripples never hurt");

        w = new World(level: SoundingArchive.Build());
        w.Level.KeyOpen = true;
        RippleBell live = w.RippleBells.First(b => b.Echo && !b.Harmless);
        Stand(w, live.Pos.X + 4f, live.Floor);
        hp = w.Player.Health;
        Step(w, 3f);
        ok(w.Player.Health < hp, "the Return Chute echo drum hurts a diver who does not jump");
    }

    static void Moth(Action<bool, string> ok)
    {
        var w = new World(level: SoundingArchive.Build());
        InkMoth moth = w.Foes.OfType<InkMoth>().First(m => w.Level.Rooms[m.Room].Name == "Silt Stacks");
        Stand(w, moth.Home.X - 2f, 2f);
        float markAt = -1f;
        w.Event += e => { if (e.Type == GameEventType.MothMark) markAt = w.Time; };
        for (int i = 0; i < 180 && moth.Beat != MothBeat.Mark; i++) w.Update(Dt, default);
        ok(moth.Beat == MothBeat.Mark && MathF.Abs(moth.Mark.X - w.Player.Body.Feet.X) < 0.01f, "the moth marks the floor where the diver stands");
        Step(w, InkMoth.MarkTime - 0.05f);
        ok(moth.Beat == MothBeat.Mark && !moth.Harmful, "for 0.9 s it only marks; touching it is safe");
        int hp = w.Player.Health;
        Step(w, 0.6f);
        ok(w.Player.Health < hp, "standing on the mark gets you hit by the dive");

        w = new World(level: SoundingArchive.Build());
        moth = w.Foes.OfType<InkMoth>().First(m => w.Level.Rooms[m.Room].Name == "Silt Stacks");
        Stand(w, moth.Home.X - 2f, 2f);
        for (int i = 0; i < 180 && moth.Beat != MothBeat.Mark; i++) w.Update(Dt, default);
        Step(w, 0.4f, new PlayerInput { MoveX = -1 });
        hp = w.Player.Health;
        for (int i = 0; i < 120 && moth.Beat != MothBeat.Lodged; i++) w.Update(Dt, default);
        ok(moth.Beat == MothBeat.Lodged && w.Player.Health == hp, "walking off the mark makes it miss and lodge");
        ok(!moth.Harmful && moth.TakeHit(w, 7001, 1, moth.Body.Pos.X - 1f), "a lodged moth is safe to touch and takes damage");
        Step(w, InkMoth.LodgedTime + 0.1f);
        ok(moth.Beat == MothBeat.Rise || moth.Beat == MothBeat.Hover, "after 0.9 s it rises back to its perch");
    }

    static void Widow(Action<bool, string> ok)
    {
        var w = new World(level: SoundingArchive.Build());
        TuningWidow widow = w.Widow;
        Stand(w, widow.Body.Pos.X - 1.5f, 2f);
        var rings = new List<float>();
        w.Event += e => { if (e.Type == GameEventType.WidowRing) rings.Add(w.Time); };
        for (int i = 0; i < 120 && widow.Beat != WidowBeat.Spread; i++) w.Update(Dt, default);
        float spreadAt = w.Time;
        ok(widow.Beat == WidowBeat.Spread && !widow.TakeHit(w, 7101, 1, 0f), "the widow spreads its forks and cannot be hurt yet");
        w.Player.Invuln = 99f;
        Step(w, 2f);
        ok(rings.Count == 2 && MathF.Abs(rings[0] - spreadAt - TuningWidow.SpreadTime) < 0.03f && MathF.Abs(rings[1] - rings[0] - RippleBell.EchoDelay) < 0.03f,
            "0.8 s of spread, then a ring and its echo 0.75 s apart");

        w = new World(level: SoundingArchive.Build());
        widow = w.Widow;
        Stand(w, widow.Body.Pos.X - 1.5f, 2f);
        int hp = w.Player.Health;
        for (int i = 0; i < 300 && widow.Beat != WidowBeat.Recover; i++) w.Update(Dt, default);
        ok(w.Player.Health < hp, "standing still beside it, a ring hits");

        w = new World(level: SoundingArchive.Build());
        widow = w.Widow;
        Stand(w, widow.Body.Pos.X - 1.5f, 2f);
        hp = w.Player.Health;
        for (int i = 0; i < 300 && widow.Beat != WidowBeat.Recover; i++) w.Update(Dt, SoundingArchive.RouteInput(w, takeLedger: false));
        ok(w.Player.Health == hp && widow.Beat == WidowBeat.Recover, "jumping each ring clears both");
        ok(widow.TakeHit(w, 7102, 1, 0f), "and the sag after the echo is the punish window");
    }

    static void Archivist(Action<bool, string> ok)
    {
        var w = new World(level: SoundingArchive.Build());
        w.Level.KeyOpen = true;
        Archivist boss = w.Archivist;
        RoomInfo arena = w.Level.Rooms[boss.Room];
        Stand(w, arena.MinX + 5f, 2f);
        Step(w, 0.5f);
        ok(!boss.Engaged && !w.Level.DoorsLocked, "the Archivist sleeps until the diver walks in");
        Stand(w, boss.Home.X - 6f, 2f);
        Step(w, 0.1f);
        ok(boss.Engaged && w.Level.DoorsLocked, "it wakes and seals the choir");
        ok(!boss.TakeHit(w, 7201, 1, 0f), "it cannot be hurt in the air");
        var reals = new List<bool>();
        bool spaced = true;
        w.Player.Invuln = 999f;
        for (int i = 0; i < 60 * 20; i++)
        {
            InkBeat before = boss.Beat;
            w.Update(Dt, default);
            if (before != InkBeat.Mark && boss.Beat == InkBeat.Mark)
            {
                reals.Add(MathF.Abs(boss.RealMark.X - w.Player.Body.Feet.X) < 0.01f);
                spaced &= MathF.Abs(boss.RealMark.X - boss.DecoyMark.X) >= 4.9f && arena.Contains(new Vec2(boss.DecoyMark.X, boss.DecoyMark.Y + 1f));
            }
            if (reals.Count >= 4) break;
        }
        ok(reals.Count >= 4 && spaced, "each dive shows two marks five tiles apart inside the choir");
        ok(reals.Take(4).SequenceEqual(new[] { true, false, true, false }), "the real one alternates: under the diver, then the decoy spot");

        // Punish: hits land only while it is grounded; below half health landings send an echo.
        // One hit per grounded recovery, like a careful player.
        int hits = 0, punishedDive = -1;
        bool Punish()
        {
            if (boss.Beat != InkBeat.Recover || punishedDive == boss.Dives) return false;
            punishedDive = boss.Dives;
            return boss.TakeHit(w, 7300 + hits, 1, boss.Body.Pos.X - 1f);
        }
        for (int i = 0; i < 60 * 120 && boss.Alive; i++)
        {
            w.Update(Dt, default);
            if (Punish()) hits++;
            if (boss.SecondPhase && boss.Waves.Count > 0) break;
        }
        ok(boss.SecondPhase && boss.Waves.Count > 0, "below half health its landing sends a delayed ripple pair");
        for (int i = 0; i < 60 * 120 && boss.Alive; i++)
        {
            w.Update(Dt, default);
            if (Punish()) hits++;
        }
        ok(!boss.Alive && !w.Level.DoorsLocked && hits == boss.MaxHealth, "twelve punishes silence it and unseal the choir");
        w.Player.Invuln = 0f;
        w.Respawn();
        ok(!boss.Alive, "a silenced Archivist stays down after a death");

        // Standing under the real mark is a hit.
        w = new World(level: SoundingArchive.Build());
        w.Level.KeyOpen = true;
        boss = w.Archivist;
        Stand(w, boss.Home.X - 6f, 2f);
        int hp = w.Player.Health;
        for (int i = 0; i < 60 * 4 && boss.Beat != InkBeat.Recover; i++) w.Update(Dt, default);
        ok(boss.Beat == InkBeat.Recover && w.Player.Health < hp, "the first dive lands on a diver who stays put");
    }

    static void Stations(Action<bool, string> ok)
    {
        var w = new World(level: SoundingArchive.Build());
        Level lv = w.Level;
        Stand(w, lv.Checkpoint.X, lv.Checkpoint.Y);
        ok(w.LampLit && Vec2.Distance(w.RespawnFeet, lv.Checkpoint) < 0.01f, "the green lamp is the Archive checkpoint");
        w.Update(Dt, new PlayerInput { Up = true });
        w.Update(Dt, default);
        ok(Vec2.Distance(w.Player.Body.Feet, lv.Checkpoint) < 0.1f, "the chute does nothing before the counterweight");
        Stand(w, lv.Lever.X - 1.05f, 2f);
        w.Update(Dt, new PlayerInput { AttackPressed = true });
        Step(w, 0.3f);
        ok(w.GrateOpen, "striking the counterweight opens the chute");
        Stand(w, SoundingArchive.ChuteStop.X, 2f);
        w.Update(Dt, new PlayerInput { Up = true });
        ok(Vec2.Distance(w.Player.Body.Feet, lv.Checkpoint) < 0.1f, "Up at the chute rides to the lamp");
        w.Update(Dt, default);
        w.Update(Dt, new PlayerInput { Up = true });
        ok(Vec2.Distance(w.Player.Body.Feet, SoundingArchive.ChuteStop) < 0.1f, "and Up at the lamp rides back");

        w.Widow.Alive = false;
        Stand(w, w.Widow.Body.Feet.X, 2f);
        ok(w.DrumKey && lv.KeyOpen, "the widow leaves the drum key, which opens the choir door");
    }

    static void Route(Action<bool, string> ok)
    {
        var w = new World(level: SoundingArchive.Build());
        var visited = new HashSet<int>();
        float engagedAt = -1f;
        for (int i = 0; i < 60 * 240 && w.Phase != Phase.Victory; i++)
        {
            visited.Add(w.Room);
            w.Update(Dt, SoundingArchive.RouteInput(w));
            if (engagedAt < 0 && w.Archivist.Engaged) engagedAt = w.Time;
        }
        Console.WriteLine($"        Archive run: {w.Phase} at {w.Time:0.0}s, rooms {visited.Count}, deaths {w.Deaths}, ledger {w.LedgerFound}, key {w.DrumKey}, order {w.OrderTaken}, hp {w.Player.Health}/{w.Player.MaxHealth}");
        ok(w.Phase == Phase.Victory && visited.Count == 6 && w.LedgerFound && w.DrumKey && w.OrderTaken && w.LampLit && w.GrateOpen,
            "input-only route visits all six rooms, takes the loft record, drum key and order, and exits");

        // Boss retry: from the lamp, ride the chute and reach the Archivist quickly.
        w = new World(level: SoundingArchive.Build());
        Stand(w, w.Level.Checkpoint.X, 2f);
        w.Level.GrateOpen = true;
        w.Widow.Alive = false;
        Stand(w, w.Widow.Body.Feet.X, 2f);
        Stand(w, w.Level.Checkpoint.X, 2f);
        float start = w.Time;
        w.Update(Dt, new PlayerInput { Up = true });
        for (int i = 0; i < 60 * 30 && !w.Archivist.Engaged; i++) w.Update(Dt, SoundingArchive.RouteInput(w, takeLedger: false));
        float retry = w.Time - start;
        ok(w.Archivist.Engaged && retry < 25f, "lamp to Archivist via the chute takes " + retry.ToString("0.0") + " s (target under 25 s)");
    }

    static void Saves(Action<bool, string> ok)
    {
        // The Pump Warden's defeat saves the start of the Archive, carrying the upgrades and records.
        var s = new World(level: SluiceWorks.Build());
        for (int i = 0; i < 60 * 180 && s.Phase != Phase.Victory; i++) s.Update(Dt, SluiceWorks.RouteInput(s));
        SaveData atEnd = SaveData.Parse(s.Snapshot().Serialize());
        ok(s.Phase == Phase.Victory && atEnd.Chapter == Chapter.Archive && atEnd.Gauge == s.GaugeFound && !atEnd.Lamp,
            "finishing the Works saves the start of the Archive, keeping the keeper record");
        World a = s.EnterArchive();
        ok(a.Chapter == Chapter.Archive && a.Time == s.Time && a.Deaths == s.Deaths && a.GaugeFound == s.GaugeFound && a.Player.MaxHealth == s.Player.MaxHealth,
            "entering the Archive carries time, deaths, the keeper record and the masks");

        // Mid-Archive: after the drum key and the chute, quit and continue.
        var w = new World(level: SoundingArchive.Build());
        for (int i = 0; i < 60 * 240 && !(w.DrumKey && w.GrateOpen); i++) w.Update(Dt, SoundingArchive.RouteInput(w));
        var c = World.Restore(SaveData.Parse(w.Snapshot().Serialize()));
        ok(c.Chapter == Chapter.Archive && c.DrumKey && c.Level.KeyOpen && c.GrateOpen && c.LampLit && c.LedgerFound == w.LedgerFound,
            "an Archive save keeps the drum key, open door, chute and loft record");
        ok(Vec2.Distance(c.Player.Body.Feet, c.Level.Checkpoint) < 0.1f, "and wakes at the green lamp");
        for (int i = 0; i < 60 * 180 && c.Phase != Phase.Victory; i++) c.Update(Dt, SoundingArchive.RouteInput(c));
        ok(c.Phase == Phase.Victory && c.OrderTaken, "a continued Archive run can still finish the chapter");

        string v03 = new SaveData { Chapter = Chapter.Sluice, Lamp = true }.Serialize();
        foreach (string key in new[] { "ledger", "drumkey", "archivist", "order" })
            v03 = string.Join("\n", v03.Split('\n').Where(l => !l.StartsWith(key + "=")));
        SaveData old = SaveData.Parse(v03);
        ok(old != null && old.Chapter == Chapter.Sluice && old.Lamp && !old.DrumKey, "a save written by v0.3 (without the Archive fields) still loads");
    }
}
