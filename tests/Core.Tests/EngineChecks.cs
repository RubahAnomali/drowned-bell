using System;
using System.Collections.Generic;
using System.Linq;
using DrownedBell.Core;

/// <summary>Chapter IV, the Tidal Engine: map, stations, foes, the route, the pad sequence and saves.</summary>
internal static class EngineChecks
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

    /// <summary>ENGINE_TRACE=1 dotnet run: prints where the scripted route is every two seconds.</summary>
    public static void Trace()
    {
        var w = new World(level: TidalEngine.Build());
        w.Event += e => { if (e.Type != GameEventType.Jump && e.Type != GameEventType.Land && e.Type != GameEventType.Swing) Console.WriteLine($"{w.Time,7:0.0} {e.Type} {e.Pos}"); };
        for (int i = 0; i < 60 * 120 && w.Phase != Phase.Victory; i++)
        {
            var input = TidalEngine.RouteInput(w);
            if (i % 90 == 0) Console.WriteLine($"{w.Time,7:0.0} room {w.Room} feet {w.Player.Body.Feet} move {input.MoveX} jump {input.JumpPressed} grounded {w.Player.Body.Grounded} hp {w.Player.Health}");
            w.Update(Dt, input);
        }
    }

    public static void PadTrace()
    {
        var w = new World(level: TidalEngine.Build());
        RoomInfo watch = w.Level.Rooms[3];
        w.Player.Place(new Vec2(watch.MinX + 4f, w.Level.FloorBelow(watch.MinX + 4f, watch.MinY + 6f)), w.Move);
        w.Event += e => { if (e.Type == GameEventType.PadBounce || e.Type == GameEventType.EngineRecordFound) Console.WriteLine($"{w.Time,6:0.00} {e.Type}"); };
        for (int i = 0; i < 60 * 20; i++)
        {
            var input = TidalEngine.PadRoute(w);
            if (i % 15 == 0) Console.WriteLine($"{w.Time,6:0.00} feet {w.Player.Body.Feet} vel {w.Player.Body.Vel} grounded {w.Player.Body.Grounded} airdash {w.Player.AirDashUsed} in: move {input.MoveX} jump {input.JumpPressed} dash {input.DashPressed} down {input.Down}");
            w.Update(Dt, input);
        }
    }

    public static void Run(Action<bool, string> ok)
    {
        Console.WriteLine("\nTidal Engine");
        var w = new World(level: TidalEngine.Build());
        Level lv = w.Level;
        ok(lv.Chapter == Chapter.Engine && w.Chapter == Chapter.Engine && lv.Rooms.Count == 7 && lv.Outlet.HasValue && lv.Record.HasValue &&
           lv.Latches.Count == 2 && lv.Pads.Count == 3 && lv.Wheel.HasValue && lv.StairLift.HasValue && lv.LiftLamp.HasValue && lv.DockModel.HasValue,
            "the Engine map has seven rooms, two latches, three pads, a wheel, a lift, a last lamp, a record and an exit");
        ok(w.Tidekeeper != null && w.Foes.OfType<PumpMachine>().Count(m => m.Kind == EnemyKind.Knight) == 1 && lv.Vents.Count == 2 && w.RippleBells.Count == 1,
            "one Ballast Knight, the Tidekeeper, two vents and a floor-wave outlet");
        ok(lv.Rooms.Select(r => r.Name).SequenceEqual(new[] { "Service Dock", "Flywheel Cradle", "Condenser Bridge", "Night Watch", "Governor Stair", "Sea Gate", "Morning Nave" }),
            "the rooms are in the order of the room plates");

        Geometry(ok);
        Fairness(ok);
        Stations(ok);
        Route(ok);
        Pads(ok);
        Saves(ok);
    }

    static void Geometry(Action<bool, string> ok)
    {
        Level lv = TidalEngine.Build();
        // The required route: no climb above two tiles, no gap wider than three (the controller rises 3.26 and reaches 5.33).
        float Surface(int x, float from) => lv.FloorBelow(x + 0.5f, from);
        int worstRise = 0, worstGap = 0, gap = 0;
        float prev = Surface(2, 30f);                 // the last dry surface
        for (int x = 3; x < 158; x++)
        {
            float s = Surface(x, 30f);
            if (x > 126 && x < 129) { prev = s; continue; }                        // the drop off the stair into the Sea Gate
            if (s < 22f - 0.5f && x < 96) { gap++; continue; }                      // a pit or channel (the bridge side)
            worstGap = Math.Max(worstGap, gap); gap = 0;
            if (s > prev) worstRise = Math.Max(worstRise, (int)MathF.Round(s - prev));
            prev = s;
        }
        bool bridgeOk = worstRise <= 2 && worstGap <= 3;
        ok(bridgeOk, "the required route never climbs more than two tiles at once (" + worstRise + ") or crosses a gap wider than three (" + worstGap + ")");
        // The hatch to the Night Watch is a stack of one-way shelves two tiles apart.
        var shelves = new List<int>();
        for (int y = 0; y < lv.H; y++) if (lv.OneWay(66, y)) shelves.Add(y);
        ok(shelves.Count >= 8 && shelves.Zip(shelves.Skip(1), (a, b) => b - a).All(d => d == 2), "the hatch to the Night Watch is a shelf every two tiles (" + string.Join(",", shelves) + ")");
        // Pads: spaced so a pogo and a dash can cross, each one tile higher.
        var pads = lv.Pads.OrderBy(p => p.X).ToList();
        ok(pads.Zip(pads.Skip(1), (a, b) => b.X - a.X >= 4f && b.X - a.X <= 6f && b.Y - a.Y == 1f).All(x => x), "the three spring pads are four to six tiles apart and each one tile higher");
    }

    static void Fairness(Action<bool, string> ok)
    {
        var w = new World(level: TidalEngine.Build());
        Tidekeeper tk = w.Tidekeeper;
        float minWindup = Enum.GetValues(typeof(KeeperAttack)).Cast<KeeperAttack>().Min(a => tk.WindupTime(a));
        ok(minWindup >= 0.45f && tk.RecoverTime >= 0.35f && tk.RestTime > 0.35f, "Tidekeeper telegraphs at least 0.45 s (shortest " + minWindup.ToString("0.00") + " s) and leaves a reset of " + tk.RecoverTime.ToString("0.00") + " s");
        tk.Health = tk.MaxHealth / 2;
        float minWindup2 = Enum.GetValues(typeof(KeeperAttack)).Cast<KeeperAttack>().Min(a => tk.WindupTime(a));
        ok(tk.SecondPhase && minWindup2 >= 0.45f && tk.RecoverTime >= 0.35f, "and below half health the shortest telegraph is " + minWindup2.ToString("0.00") + " s");
        PumpMachine knight = w.Foes.OfType<PumpMachine>().First(m => m.Kind == EnemyKind.Knight);
        ok(knight.BraceTime >= 0.45f && knight.RecoverTime >= 0.35f && knight.AttackDamage >= 1, "the Ballast Knight braces its hammer and resets for at least a swing");

        // The hammer only reaches the side the Tidekeeper faces, so standing behind it is safe.
        w = new World(level: TidalEngine.Build());
        tk = w.Tidekeeper;
        RoomInfo room = w.Level.Rooms[tk.Room];
        Stand(w, tk.Home.X - 6f, 24f);
        w.Player.Invuln = 9999f;
        bool hammerSeen = false, behindSafe = true;
        for (int i = 0; i < 60 * 60 && !hammerSeen; i++)
        {
            w.Update(Dt, default);
            if (tk.Beat == KeeperBeat.Strike && (tk.Attack == KeeperAttack.Hammer || tk.Attack == KeeperAttack.Lane) && tk.TryAttackBox(out Aabb box))
            {
                hammerSeen = true;
                float behindX = tk.Home.X - tk.Facing * 1.5f;
                behindSafe = !box.Contains(new Vec2(behindX, w.Player.Body.Pos.Y)) && (tk.Facing > 0 ? box.MinX > tk.Home.X - 0.01f : box.MaxX < tk.Home.X + 0.01f);
            }
        }
        ok(hammerSeen && behindSafe, "the hammer and the lane only reach the side the Tidekeeper faces");
        ok(room.Contains(tk.Body.Pos), "and it stays in the Sea Gate");

        // Random order: all four attacks appear, never the same one three times in a row.
        w = new World(level: TidalEngine.Build());
        tk = w.Tidekeeper;
        w.Player.Invuln = 9999f;
        Stand(w, tk.Home.X - 6f, 24f);
        var attacks = new List<KeeperAttack>();
        KeeperBeat last = KeeperBeat.Dormant;
        for (int i = 0; i < 60 * 150; i++)
        {
            w.Update(Dt, default);
            if (tk.Beat == KeeperBeat.Windup && last != KeeperBeat.Windup) attacks.Add(tk.Attack);
            last = tk.Beat;
            if (tk.Health == tk.MaxHealth && i == 60 * 60) tk.TakeHitDirect(30);
        }
        bool noTriple = true;
        for (int i = 2; i < attacks.Count; i++) noTriple &= !(attacks[i] == attacks[i - 1] && attacks[i] == attacks[i - 2]);
        ok(attacks.Distinct().Count() == 4 && noTriple, "all four attacks appear in a random order, never three alike in a row (" + string.Join(",", attacks.Take(12).Select(a => a.ToString()[0])) + ")");
        ok(tk.SecondPhase, "and the fight passes half health");

        // Only the reset is a window: the hinge is shut during the wind-up and the strike.
        w = new World(level: TidalEngine.Build());
        tk = w.Tidekeeper;
        w.Player.Invuln = 9999f;
        Stand(w, tk.Home.X - 6f, 24f);
        bool shut = true, open = false;
        for (int i = 0; i < 60 * 40; i++)
        {
            w.Update(Dt, default);
            int hp = tk.Health;
            bool took = tk.TakeHit(w, 9000 + i, 1, tk.Home.X - 1f);
            if (tk.Beat != KeeperBeat.Recover && took) shut = false;
            if (tk.Beat == KeeperBeat.Recover && took) open = true;
        }
        ok(shut && open, "the hinge only takes hits while the Tidekeeper resets");
    }

    static void Stations(Action<bool, string> ok)
    {
        var w = new World(level: TidalEngine.Build());
        Level lv = w.Level;
        Stand(w, lv.Checkpoint.X, lv.Checkpoint.Y);
        ok(w.LampLit && Vec2.Distance(w.RespawnFeet, lv.Checkpoint) < 0.01f, "the dock lamp is the first checkpoint");
        Stand(w, lv.DockModel.Value.X, lv.DockModel.Value.Y);
        ok(w.DockModelSeen, "walking up to the dock model inspects it");

        // Latches: struck with the hook, in the room they stand in.
        for (int i = 0; i < 2; i++)
        {
            Vec2 latch = lv.Latches[i];
            Stand(w, latch.X - 1.1f, latch.Y);
            w.Player.Facing = 1;
            w.Update(Dt, new PlayerInput { AttackPressed = true });
            Step(w, 0.3f);
            ok(w.LatchOpen[i], "striking governor latch " + (i + 1) + " throws it");
        }
        ok(w.LiftOnline, "with both latches thrown the lift comes online");

        Stand(w, lv.StairLift.Value.X, lv.StairLift.Value.Y);
        w.Update(Dt, new PlayerInput { Up = true });
        ok(Vec2.Distance(w.Player.Body.Feet, lv.Checkpoint) < 0.1f, "Up at the stair lift rides to the dock lamp");
        w.Update(Dt, default);
        w.Update(Dt, new PlayerInput { Up = true });
        ok(Vec2.Distance(w.Player.Body.Feet, lv.StairLift.Value) < 0.1f, "and Up at the dock lamp rides back");

        var early = new World(level: TidalEngine.Build());
        Stand(early, lv.StairLift.Value.X, lv.StairLift.Value.Y);
        early.Update(Dt, new PlayerInput { Up = true });
        ok(Vec2.Distance(early.Player.Body.Feet, lv.StairLift.Value) < 0.1f, "the lift does nothing before both latches");

        // The last lamp.
        Stand(w, lv.LiftLamp.Value.X, lv.LiftLamp.Value.Y);
        ok(w.RestLit && Vec2.Distance(w.RespawnFeet, lv.LiftLamp.Value) < 0.01f, "the last lamp over the sea gate becomes the respawn point");

        // The wheel: only after the Tidekeeper is down.
        Vec2 wheel = lv.Wheel.Value;
        Stand(w, wheel.X - 1.1f, wheel.Y);
        w.Player.Facing = 1;
        w.Update(Dt, new PlayerInput { AttackPressed = true });
        Step(w, 0.3f);
        ok(!w.TideReversed && !w.GrateOpen, "the bypass wheel will not turn while the Tidekeeper stands");
        w.Tidekeeper.Alive = false;
        Stand(w, wheel.X - 1.1f, wheel.Y);
        w.Player.Facing = 1;
        w.Update(Dt, new PlayerInput { AttackPressed = true });
        Step(w, 0.3f);
        ok(w.TideReversed && w.GrateOpen, "once it is down, striking the wheel reverses the tide and opens the sea gate");
        Stand(w, lv.Outlet.Value.X, lv.Outlet.Value.Y);
        Step(w, 0.2f);
        ok(w.Phase == Phase.Victory, "walking out into the morning nave ends the campaign");

        var closed = new World(level: TidalEngine.Build());
        Stand(closed, lv.Outlet.Value.X, lv.Outlet.Value.Y);
        Step(closed, 0.2f);
        ok(closed.Phase == Phase.Playing, "the nave exit does nothing before the tide is reversed");
    }

    static void Route(Action<bool, string> ok)
    {
        var w = new World(level: TidalEngine.Build());
        var visited = new HashSet<int>();
        float engagedAt = -1f;
        for (int i = 0; i < 60 * 600 && w.Phase != Phase.Victory; i++)
        {
            visited.Add(w.Room);
            w.Update(Dt, TidalEngine.RouteInput(w));
            if (engagedAt < 0 && w.Tidekeeper.Engaged) engagedAt = w.Time;
        }
        Console.WriteLine($"        Engine run: {w.Phase} at {w.Time:0.0}s, rooms {visited.Count}, deaths {w.Deaths}, latches {w.LatchOpen[0]}/{w.LatchOpen[1]}, keeper {(w.Tidekeeper.Alive ? "up" : "down")}, tide {w.TideReversed}, hp {w.Player.Health}/{w.Player.MaxHealth}");
        ok(w.Phase == Phase.Victory && w.LatchOpen[0] && w.LatchOpen[1] && !w.Tidekeeper.Alive && w.TideReversed && w.LampLit && w.RestLit,
            "the input-only route throws both latches, beats the Ballast Knight and the Tidekeeper, reverses the tide and walks out");
        ok(visited.Count == 6 && !visited.Contains(3), "it passes through six rooms and leaves the optional Night Watch alone");
    }

    static void Pads(Action<bool, string> ok)
    {
        var w = new World(level: TidalEngine.Build());
        Level lv = w.Level;
        RoomInfo watch = lv.Rooms[3];
        w.Player.Place(new Vec2(watch.MinX + 4f, lv.FloorBelow(watch.MinX + 4f, watch.MinY + 6f)), w.Move);
        int bounces = 0;
        w.Event += e => { if (e.Type == GameEventType.PadBounce) bounces++; };
        for (int i = 0; i < 60 * 40 && !w.EngineRecord; i++) w.Update(Dt, TidalEngine.PadRoute(w));
        ok(w.EngineRecord && bounces >= 2, "a diver can pogo across the three spring pads (" + bounces + " bounces) and take the keeper's record");
        // Without the pads there is no way up: the platform is eight tiles above the water.
        var low = new World(level: TidalEngine.Build());
        low.Player.Place(new Vec2(watch.MinX + 4f, low.Level.FloorBelow(watch.MinX + 4f, watch.MinY + 6f)), low.Move);
        for (int i = 0; i < 60 * 12; i++) low.Update(Dt, new PlayerInput { MoveX = 1, JumpPressed = i % 40 == 0, JumpHeld = true, DashPressed = i % 25 == 0 });
        ok(!low.EngineRecord, "running and jumping alone never reaches the record");
    }

    static void Saves(Action<bool, string> ok)
    {
        // Beating the Archivist saves the start of the Tidal Engine, keeping both records.
        var a = new World(level: SoundingArchive.Build());
        for (int i = 0; i < 60 * 240 && a.Phase != Phase.Victory; i++) a.Update(Dt, SoundingArchive.RouteInput(a));
        SaveData atEnd = SaveData.Parse(a.Snapshot().Serialize());
        ok(a.Phase == Phase.Victory && atEnd.Chapter == Chapter.Engine && atEnd.Ledger == a.LedgerFound && !atEnd.Latch1,
            "finishing the Archive saves the start of the Tidal Engine, keeping the keeper records");
        World e = a.EnterEngine();
        ok(e.Chapter == Chapter.Engine && e.LedgerFound == a.LedgerFound && e.GaugeFound == a.GaugeFound && e.Time == a.Time && e.Player.MaxHealth == a.Player.MaxHealth,
            "entering the Engine carries time, deaths, records and masks");

        // Mid-Engine: throw the latches, take the last lamp, beat the Tidekeeper.
        var w = new World(level: TidalEngine.Build());
        for (int i = 0; i < 60 * 600 && !(w.RestLit && w.LatchOpen[0] && w.LatchOpen[1]); i++) w.Update(Dt, TidalEngine.RouteInput(w));
        var c = World.Restore(SaveData.Parse(w.Snapshot().Serialize()));
        ok(c.Chapter == Chapter.Engine && c.LatchOpen[0] && c.LatchOpen[1] && c.LiftOnline && c.RestLit && !c.TideReversed,
            "an Engine save keeps the latches, the lift and the last lamp");
        ok(Vec2.Distance(c.Player.Body.Feet, c.Level.LiftLamp.Value) < 0.1f && c.Room == c.Level.RoomIndexAt(c.Player.Body.Pos), "and wakes at the last lamp");
        for (int i = 0; i < 60 * 400 && c.Phase != Phase.Victory; i++) c.Update(Dt, TidalEngine.RouteInput(c));
        ok(c.Phase == Phase.Victory && c.TideReversed, "a continued Engine run can still finish the chapter");

        var d = World.Restore(new SaveData { Chapter = Chapter.Engine, Lamp = true, Latch1 = true, KeeperDown = true, Reversed = true, EngineRecord = true });
        ok(d.Tidekeeper != null && !d.Tidekeeper.Alive && d.TideReversed && d.GrateOpen && d.EngineRecord, "a save after the Tidekeeper restores the open sea gate and its record");

        string text = new SaveData { Chapter = Chapter.Engine, Latch1 = true, EngineRecord = true, RestLit = true }.Serialize();
        SaveData round = SaveData.Parse(text);
        ok(round != null && round.Latch1 && !round.Latch2 && round.EngineRecord && round.RestLit && round.Place() == "Governor Stair", "the new save fields round-trip");
        string old = string.Join("\n", text.Split('\n').Where(l => !l.StartsWith("latch") && !l.StartsWith("engrecord") && !l.StartsWith("keeperdown") && !l.StartsWith("reversed") && !l.StartsWith("restlit")));
        SaveData oldSave = SaveData.Parse(old);
        ok(oldSave != null && oldSave.Chapter == Chapter.Engine && !oldSave.Latch1, "a save written before the Engine existed still loads");
    }
}
