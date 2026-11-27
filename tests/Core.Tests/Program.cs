using System;
using System.Collections.Generic;
using System.Linq;
using DrownedBell.Core;

// Headless checks for the Drowned Bell simulation. Run: dotnet run (from tests/Core.Tests).
internal static class Program
{
    static int failed;
    static void Ok(bool cond, string msg)
    {
        if (!cond) failed++;
        Console.WriteLine((cond ? "  ok    " : "  FAIL  ") + msg);
    }
    static void Section(string s) => Console.WriteLine("\n" + s);
    const float Dt = World.FixedDt;

    static readonly MovementTuning M = new MovementTuning();
    static readonly CombatTuning C = new CombatTuning();

    /// <summary>A small test room: a 3-tile ledge on the left (top at y=4), a one-way ledge in the middle (top at y=4), floor at y=1.</summary>
    static Level TestRoom()
    {
        string[] rows =
        {
            "####################",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#######....===.....#",
            "#######............#",
            "#######............#",
            "####################",
        };
        return Level.Parse(rows, new[] { ("test", "test", 0, 0, 20, 10) });
    }

    static int Main()
    {
        if (Environment.GetEnvironmentVariable("BOSS_TRACE") == "1") { BossTrace.Run(); return 0; }
        if (Environment.GetEnvironmentVariable("ROUTE_TRACE") == "1") { RouteTrace.Run(); return 0; }
        if (Environment.GetEnvironmentVariable("ARCHIVE_TRACE") == "1") { ArchiveTrace.Run(); return 0; }
        if (Environment.GetEnvironmentVariable("ENGINE_TRACE") == "1") { EngineChecks.Trace(); return 0; }
        if (Environment.GetEnvironmentVariable("PAD_TRACE") == "1") { EngineChecks.PadTrace(); return 0; }
        if (Environment.GetEnvironmentVariable("DIFFICULTY_TABLE") == "1") { BossDifficulty.Table(); return 0; }
        if (Environment.GetEnvironmentVariable("DIFFICULTY_SEEDS") == "1") { BossDifficulty.SeedTable(); return 0; }
        if (Environment.GetEnvironmentVariable("DIFFICULTY_UNDERTOLL_SEED") is string us) { var r = BossDifficulty.Undertoll(new NavGraph(false), new NavGraph(true), BossDifficulty.Style.Reader, 1500f, 30, int.Parse(us)); Console.WriteLine(r); return 0; }
        SceneTuning.Check(Ok);
        Movement();
        MouseAim();
        TestRoomChecks.Run(Ok);
        Combat();
        Enemies();
        BossFight();
        BossEnding();
        SluiceChecks();
        PumpChecks();
        EdgeCases();
        RunState();
        SaveChecks();
        ArchiveChecks.Run(Ok);
        EngineChecks.Run(Ok);
        BossDesignChecks.Run(Ok);
        SmoothnessChecks.Run(Ok);
        Route();
        LevelPlanChecks.Run(Ok);
        Pilot();
        Determinism();
        Console.WriteLine(failed == 0 ? "\nall checks passed" : "\n" + failed + " check(s) FAILED");
        return failed == 0 ? 0 : 1;
    }

    // ---------------------------------------------------------------------------------------------
    static void SluiceChecks()
    {
        var w = new World(level: SluiceWorks.Build());
        Vec2 vent = w.Level.Vents[0];
        float start = 4f - vent.X * 0.125f % 4f;
        foreach (var sample in new[] { (0.5f, VentState.Quiet), (2.5f, VentState.Warning), (3.5f, VentState.Blowing) })
            Ok(SluiceWorks.VentAt(start + sample.Item1, vent) == sample.Item2, "vent cycle " + sample.Item2);
        w.Player.Place(vent, w.Move);
        for (int i = 0; i < 240; i++) w.Update(Dt, default);
        Ok(w.Player.Health < w.Player.MaxHealth, "standing in the pressure jet costs health");
        w = new World(level: SluiceWorks.Build());
        w.Player.Place(w.Level.Outlet.Value, w.Move);
        w.Update(Dt, default);
        Ok(w.Phase == Phase.Playing, "outlet requires the valve");
        w.Respawn();
        var visited = new HashSet<int>();
        for (int i = 0; i < 60 * 180 && w.Phase != Phase.Victory; i++)
        {
            visited.Add(w.Room);
            w.Update(Dt, SluiceWorks.RouteInput(w));
        }
        Console.WriteLine($"Works run: {w.Phase}, rooms {visited.Count}, feet {w.Player.Body.Feet}, deaths {w.Deaths}, gauge {w.GaugeFound}, Warden {w.Warden.Health}");
        Ok(visited.Count == 7 && w.LampLit && w.LiftOnline && w.GaugeFound && w.PumpKey && w.GrateOpen && w.Phase == Phase.Victory,
            "input-only route clears seven Works rooms, keeper record, Warden, pump key and outlet");
        w.Respawn();
        Ok(w.GrateOpen && w.Player.Body.Feet.X == w.Level.LiftLamp.Value.X && w.PumpKey, "lift lamp, key and open valve survive respawn");
    }

    static void PumpChecks()
    {
        var w = new World(level: SluiceWorks.Build());
        foreach (var machine in w.Foes.OfType<PumpMachine>())
        {
            w.Player.Place(new Vec2(machine.Home.X - 3, machine.Body.Bottom), w.Move);
            machine.Reset();
            bool warned = false, struck = false, recovered = false;
            for (int i = 0; i < 420; i++)
            {
                machine.Update(w, Dt);
                if (machine.Beat == MachineBeat.Brace)
                {
                    warned = true;
                    if (!machine.IsWarden)
                        w.Player.Place(new Vec2(machine.Home.X + 2, machine.Body.Bottom), w.Move);
                }
                if (machine.Beat == MachineBeat.Strike)
                    struck |= warned && machine.Facing == -1 && machine.TryAttackBox(out Aabb lane) && lane.MaxX <= machine.Home.X + 1e-3f;
                if (machine.Beat == MachineBeat.Recover)
                {
                    recovered = !machine.TryAttackBox(out _) && machine.TakeHit(w, 1, 1, machine.Home.X - 2);
                    break;
                }
            }
            Ok(warned && struck && recovered, machine.Kind + " locks its bearing, strikes on the visible side and permits recovery hits");
        }

        var boss = w.Warden;
        boss.Reset();
        Ok(!boss.TakeHit(w, 2, 1, 0), "Warden casing resists damage outside recovery");
        boss.Health = boss.MaxHealth / 2;
        var lanes = new List<int>();
        MachineBeat previous = boss.Beat;
        for (int i = 0; i < 420 && boss.Beat != MachineBeat.Recover; i++)
        {
            boss.Update(w, Dt);
            if (boss.Beat == MachineBeat.Jet && boss.Beat != previous) lanes.Add(boss.Lane);
            previous = boss.Beat;
        }
        Ok(lanes.Count >= 3 && lanes.Count <= 4 && lanes.Zip(lanes.Skip(1), (x, y) => x != y).All(d => d), "overpressure vents three or four pulses in a row before the sweep, never the same lane twice running");
        w.Respawn();
        Ok(!boss.Engaged && boss.Health == boss.MaxHealth && !w.Level.DoorsLocked, "an unfinished Warden resets and releases the arena on respawn");

        w = new World(level: SluiceWorks.Build());
        for (int i = 0; i < 10800 && w.Phase != Phase.Victory; i++)
            w.Update(Dt, SluiceWorks.RouteInput(w, false));
        Ok(w.Phase == Phase.Victory && !w.GaugeFound && w.PumpKey, "jumping the drain skips the optional record and still opens the archive hatch");

        w = new World(level: SluiceWorks.Build());
        w.Player.Place(SluiceWorks.WellStop, w.Move);
        w.Update(Dt, new PlayerInput { Up = true });
        Ok(w.BypassOpen && MathF.Abs(w.Player.Body.Feet.X - w.Level.Checkpoint.X) < 0.01f, "opening the bypass at the well rides to the intake");
        w.Update(Dt, default);
        w.Update(Dt, new PlayerInput { Up = true });
        Ok(MathF.Abs(w.Player.Body.Feet.X - SluiceWorks.WellStop.X) < 0.01f, "the intake bypass returns to the well");

        w = new World(level: SluiceWorks.Build());
        w.Player.Place(w.Level.LiftLamp.Value, w.Move);
        w.Update(Dt, new PlayerInput { Up = true });
        w.Update(Dt, default);
        w.Update(Dt, new PlayerInput { Up = true });
        Ok(w.LiftOnline && MathF.Abs(w.Player.Body.Feet.X - w.Level.LiftLamp.Value.X) < 0.01f,
            "late lift returns to the checkpoint even when the well bypass was skipped");
    }

    static float JumpPeak(Level lv, float holdFor)
    {
        var p = new Player(new Vec2(14.5f, 1f), M, 5);
        p.Update(Dt, default, lv, M, C);
        float start = p.Body.Bottom, peak = start;
        for (int i = 0; i < 120; i++)
        {
            float t = i * Dt;
            p.Update(Dt, new PlayerInput { JumpPressed = i == 0, JumpHeld = t < holdFor }, lv, M, C);
            peak = MathF.Max(peak, p.Body.Bottom);
        }
        return peak - start;
    }

    static void Movement()
    {
        Section("Movement");
        Level lv = TestRoom();
        float full = JumpPeak(lv, 9f), hop = JumpPeak(lv, 0.05f);
        Ok(MathF.Abs(full - M.JumpHeight) < 0.35f, "full jump reaches the tuned height (" + full.ToString("0.00") + " vs " + M.JumpHeight + " tiles)");
        Ok(hop < full * 0.6f, "tapping jump gives a short hop (" + hop.ToString("0.00") + " tiles)");

        // Coyote time: jump shortly after running off the ledge.
        bool CoyoteJump(float lateBy)
        {
            var p = new Player(new Vec2(5.5f, 4f), M, 5);
            p.Update(Dt, default, lv, M, C);
            float leftAt = -1f;
            for (int i = 0; i < 120; i++)
            {
                float t = i * Dt;
                bool press = leftAt >= 0f && t >= leftAt + lateBy && t < leftAt + lateBy + Dt * 0.99f;
                p.Update(Dt, new PlayerInput { MoveX = 1, JumpPressed = press, JumpHeld = press }, lv, M, C);
                if (leftAt < 0f && !p.Body.Grounded) leftAt = t;
                if (press) return p.Body.Vel.Y > 0f;
            }
            return false;
        }
        Ok(CoyoteJump(0.05f), "jump 0.05s after leaving a ledge still works (coyote time)");
        Ok(!CoyoteJump(0.2f), "jump 0.2s after leaving a ledge does not");

        // Jump buffer: pressing just before landing jumps on landing.
        {
            var p = new Player(new Vec2(14.5f, 1f), M, 5);
            p.Update(Dt, default, lv, M, C);
            bool pressed = false, rejumped = false;
            for (int i = 0; i < 160; i++)
            {
                bool press = false;
                if (i == 0) press = true;
                else if (!pressed && p.Body.Vel.Y < 0f && p.Body.Bottom < 1.45f) { press = true; pressed = true; }
                p.Update(Dt, new PlayerInput { JumpPressed = press, JumpHeld = press || i < 10 }, lv, M, C);
                if (pressed && p.Body.Vel.Y > 0f) { rejumped = true; break; }
            }
            Ok(pressed && rejumped, "jump pressed just before landing fires on landing (buffer)");
        }

        // Dash distance, and only one air dash per airtime.
        {
            var p = new Player(new Vec2(9.5f, 1f), M, 5);
            p.Update(Dt, default, lv, M, C);
            float x0 = p.Body.Pos.X;
            for (int i = 0; i < 12; i++) p.Update(Dt, new PlayerInput { DashPressed = i == 0 }, lv, M, C);
            float d = p.Body.Pos.X - x0;
            Ok(d > M.DashSpeed * M.DashTime * 0.85f, "a ground dash covers about " + (M.DashSpeed * M.DashTime).ToString("0.0") + " tiles (" + d.ToString("0.0") + ")");

            p = new Player(new Vec2(3.5f, 4f), M, 5);
            p.Update(Dt, default, lv, M, C);
            int dashes = 0;
            for (int i = 0; i < 70; i++)
            {
                var input = new PlayerInput { JumpPressed = i == 0, JumpHeld = true, DashPressed = i == 6 || i == 40 };
                if (p.Update(Dt, input, lv, M, C).Dashed) dashes++;
                if (p.Body.Grounded && i > 5) break;
            }
            Ok(dashes == 1, "only one dash per airtime (" + dashes + ")");
        }

        // A low tile ceiling and a wall leave room to reverse after overlapping inputs.
        {
            string[] rows = { "##########", "#........#", "#...####.#", "#........#", "#........#", "##########" };
            var tunnel = Level.Parse(rows, new[] { ("Tunnel", "Input stress", 0, 0, 10, 6) });
            var p = new Player(new Vec2(5.5f, 1f), M, 5);
            p.Update(Dt, default, tunnel, M, C);
            bool ceiling = false, overlapped = false;
            for (int i = 0; i < 65; i++)
            {
                p.Update(Dt, new PlayerInput { MoveX = i < 35 ? 1 : -1, JumpPressed = i == 0,
                    JumpHeld = i < 12, DashPressed = i == 10, AttackPressed = i == 10 || i == 28 },
                    tunnel, M, C);
                ceiling |= p.Body.HitCeiling;
                overlapped |= tunnel.OverlapsSolid(p.Body.Box);
            }
            Ok(ceiling && !overlapped && p.Body.Grounded && p.Body.Vel.X < 0f && p.SwingId > 0,
                "low ceiling, dash against wall, attack and rapid reversal leave the diver mobile");
        }

        // Walls and one-way ledges.
        {
            var p = new Player(new Vec2(15.5f, 1f), M, 5);
            for (int i = 0; i < 90; i++) p.Update(Dt, new PlayerInput { MoveX = 1 }, lv, M, C);
            Ok(p.Body.Pos.X + p.Body.Half.X <= 19.001f && !lv.OverlapsSolid(p.Body.Box), "running into a wall stops flush against it");
            for (int i = 0; i < 20; i++) p.Update(Dt, new PlayerInput { MoveX = 1, DashPressed = i == 0 }, lv, M, C);
            Ok(!lv.OverlapsSolid(p.Body.Box), "dashing into a wall never tunnels into it");

            p = new Player(new Vec2(12.5f, 1f), M, 5);
            p.Update(Dt, default, lv, M, C);
            for (int i = 0; i < 90; i++) p.Update(Dt, new PlayerInput { JumpPressed = i == 0, JumpHeld = true }, lv, M, C);
            Ok(p.Body.Grounded && MathF.Abs(p.Body.Bottom - 4f) < 0.05f, "jumping up through a one-way ledge lands on top of it (" + p.Body.Bottom.ToString("0.00") + ")");
            for (int i = 0; i < 60; i++) p.Update(Dt, new PlayerInput { Down = true, JumpPressed = i == 0 }, lv, M, C);
            Ok(p.Body.Grounded && MathF.Abs(p.Body.Bottom - 1f) < 0.05f, "down + jump drops back through it");
        }
    }

    /// <summary>Mouse aim: which way a clicked swing or dash goes.</summary>
    static void MouseAim()
    {
        Section("Mouse aim");
        Level lv = TestRoom();
        AttackDir Swing(float ax, float ay, bool air, bool up = false, bool down = false)
        {
            var p = new Player(new Vec2(14.5f, 1f), M, 5);
            p.Update(Dt, default, lv, M, C);
            if (air) for (int i = 0; i < 12; i++) p.Update(Dt, new PlayerInput { JumpPressed = i == 0, JumpHeld = true }, lv, M, C);
            p.Update(Dt, new PlayerInput { AttackPressed = true, HasAim = true, AimX = ax, AimY = ay, Up = up, Down = down, JumpHeld = air }, lv, M, C);
            return p.Attack;
        }
        var queued = new Player(new Vec2(4.5f, 1f), M, 5);
        queued.AttackTime = C.AttackWindup + C.AttackActive + C.AttackRecovery - 2.5f * Dt;
        queued.Update(Dt, new PlayerInput { AttackPressed = true, HasAim = true, AimY = 3f }, lv, M, C);
        for (int i = 0; i < 3; i++) queued.Update(Dt, default, lv, M, C);
        Ok(queued.SwingId == 1 && queued.Attack == AttackDir.Up, "an upward click buffered during recovery still swings upward");

        Ok(Swing(0.2f, 3f, false) == AttackDir.Up, "clicking above the diver swings up");
        Ok(Swing(3f, 0.4f, false) == AttackDir.Side, "clicking to the side swings sideways");
        Ok(Swing(0.2f, -3f, true) == AttackDir.Down, "clicking below while airborne swings down (pogo)");
        Ok(Swing(0.2f, -3f, false) == AttackDir.Side, "clicking below while on the ground swings sideways");
        Ok(Swing(3f, 0f, false, up: true) == AttackDir.Up, "a held W still wins over the cursor");
        {
            var p = new Player(new Vec2(14.5f, 1f), M, 5);
            p.Update(Dt, default, lv, M, C);
            p.Facing = 1;
            p.Update(Dt, new PlayerInput { AttackPressed = true, HasAim = true, AimX = -2f, AimY = 0f }, lv, M, C);
            Ok(p.Facing == -1 && p.Attack == AttackDir.Side, "a side click behind the diver turns it around");
        }
        {
            var p = new Player(new Vec2(9.5f, 1f), M, 5);
            p.Update(Dt, default, lv, M, C);
            p.Facing = 1;
            float x0 = p.Body.Pos.X;
            for (int i = 0; i < 12; i++) p.Update(Dt, new PlayerInput { DashPressed = i == 0, HasAim = i == 0, AimX = -3f, AimY = 0f }, lv, M, C);
            Ok(p.Body.Pos.X < x0 - 2f, "right click dashes toward the cursor when no direction is held");
        }
    }

    // ---------------------------------------------------------------------------------------------
    static void Step(World w, float seconds, PlayerInput input = default)
    {
        int n = (int)MathF.Round(seconds / Dt);
        for (int i = 0; i < n; i++) w.Update(Dt, input);
    }

    /// <summary>Put the diver's feet on the floor below a world x, in a given room.</summary>
    static void Teleport(World w, float x, float y)
    {
        w.Player.Place(new Vec2(x, w.Level.FloorBelow(x, y)), w.Move);
        w.Player.Body.Grounded = true;
        w.Update(Dt, default);
    }

    static Crab CrabIn(World w, string roomName) => w.Foes.OfType<Crab>().First(c => w.Level.Rooms[c.Room].Name == roomName);
    static Jelly JellyIn(World w, string roomName) => w.Foes.OfType<Jelly>().First(c => w.Level.Rooms[c.Room].Name == roomName);

    static void Combat()
    {
        Section("Combat");
        var w = new World();
        Crab crab = CrabIn(w, "Flooded Nave");
        Teleport(w, crab.Body.Pos.X - 1.5f, crab.Body.Pos.Y + 0.5f);
        w.Player.Facing = 1;
        int before = crab.Health;
        w.Update(Dt, new PlayerInput { AttackPressed = true });
        Step(w, 0.3f);
        Ok(before - crab.Health == 1, "one swing deals exactly one hit even though the hook overlaps for several frames");
        w.Player.Invuln = 5f; // isolate the swing count from the crab fighting back
        Step(w, 0.1f);
        w.Update(Dt, new PlayerInput { AttackPressed = true });
        Step(w, 0.3f);
        Ok(before - crab.Health == 2, "a second swing hits again");

        w = new World();
        int hp = w.Player.Health;
        w.HurtPlayer(w.Player.Body.Pos.X + 1);
        w.HurtPlayer(w.Player.Body.Pos.X + 1);
        Ok(w.Player.Health == hp - 1, "a second hit during invulnerability is ignored");
        Ok(w.Player.Body.Vel.X < 0f, "being hit knocks the diver away from the source");
        Step(w, C.InvulnTime + 0.1f);
        w.HurtPlayer(w.Player.Body.Pos.X + 1);
        Ok(w.Player.Health == hp - 2, "after the invulnerable window, hits land again");

        // Spikes: lose a mask and return to the last safe ground.
        w = new World();
        RoomInfo shaft = w.Level.Rooms.First(r => r.Name == "Gutter Shaft");
        Teleport(w, shaft.MinX + 4.5f, shaft.MinY + 3f);
        Step(w, 0.2f);
        Vec2 safe = w.Player.LastSafe;
        hp = w.Player.Health;
        w.Player.Body.Pos = new Vec2(shaft.MinX + 20.5f, shaft.MinY + 4.5f); // move without resetting the safe spot
        bool hazard = false;
        w.Event += e => { if (e.Type == GameEventType.HazardHit) hazard = true; };
        Step(w, 1.0f);
        Ok(hazard && w.Player.Health == hp - 1, "falling onto spikes costs one mask");
        Ok(Vec2.Distance(w.Player.Body.Pos, safe) < 0.6f, "and puts the diver back on the last safe ledge");

        // Pogo: a down-swing on spikes bounces instead of hurting.
        w = new World();
        w.Player.Place(new Vec2(shaft.MinX + 20.5f, shaft.MinY + 4.4f), w.Move);
        hp = w.Player.Health;
        bool pogo = false;
        w.Event += e => { if (e.Type == GameEventType.Pogo) pogo = true; };
        for (int i = 0; i < 30 && !pogo; i++) w.Update(Dt, new PlayerInput { Down = true, AttackPressed = i == 4 });
        Ok(pogo && w.Player.Body.Vel.Y > 0f && w.Player.Health == hp, "a down-swing onto spikes pogoes the diver upward unharmed");
    }

    // ---------------------------------------------------------------------------------------------
    static void Enemies()
    {
        Section("Enemies");
        // Crab: standing inside its lunge gets you hit; backing off during the wind-up avoids it; the recovery can be punished.
        {
            var w = new World();
            Crab crab = CrabIn(w, "Flooded Nave");
            Teleport(w, crab.Body.Pos.X - 2.2f, crab.Body.Pos.Y + 0.5f);
            int hp = w.Player.Health;
            var seen = new List<CrabState>();
            for (int i = 0; i < 180; i++)
            {
                w.Update(Dt, default);
                if (seen.Count == 0 || seen[seen.Count - 1] != crab.State) seen.Add(crab.State);
            }
            Ok(seen.Contains(CrabState.Windup) && seen.Contains(CrabState.Lunge) && seen.Contains(CrabState.Recover), "crab: approach, wind-up, lunge, recover (" + string.Join(">", seen) + ")");
            Ok(w.Player.Health < hp, "standing still inside its reach gets you clawed");
        }
        {
            var w = new World();
            Crab crab = CrabIn(w, "Flooded Nave");
            Teleport(w, crab.Body.Pos.X - 2.2f, crab.Body.Pos.Y + 0.5f);
            int hp = w.Player.Health;
            bool punished = false;
            for (int i = 0; i < 240 && !punished; i++)
            {
                var input = new PlayerInput();
                float dx = crab.Body.Pos.X - w.Player.Body.Pos.X;
                if (crab.State == CrabState.Windup || crab.State == CrabState.Lunge) input.MoveX = -1;           // step back out of reach
                else if (crab.State == CrabState.Recover)
                {
                    if (dx > 2f) input.MoveX = 1;
                    else input.AttackPressed = true;
                }
                int h = crab.Health;
                w.Update(Dt, input);
                if (crab.Health < h && crab.State == CrabState.Recover) punished = true;
            }
            Ok(w.Player.Health == hp, "backing off during the wind-up dodges the lunge");
            Ok(punished, "and the recovery is long enough to step in and hit it");
            Ok(crab.RejectedTransitions == 0, "crab never attempted an illegal state change");
        }

        // Jelly: the ring only hurts inside its band, and the spent jelly is safe to touch.
        {
            var w = new World();
            Jelly j = JellyIn(w, "Gutter Shaft");
            w.Player.Place(new Vec2(j.Home.X + 1.6f, j.Home.Y - 1.2f), w.Move);
            w.Player.Invuln = 0f;
            int hp = w.Player.Health;
            var states = new List<JellyState>();
            for (int i = 0; i < 300; i++)
            {
                // Hold the diver in mid-air at a fixed offset so only the ring can reach it.
                w.Player.Place(new Vec2(j.Body.Pos.X + 1.6f, j.Body.Pos.Y - 0.2f), w.Move);
                w.Update(Dt, default);
                if (states.Count == 0 || states[states.Count - 1] != j.State) states.Add(j.State);
                if (w.Player.Health < hp) break;
            }
            Ok(states.Contains(JellyState.Windup) && states.Contains(JellyState.Pulse), "jelly winds up and pulses when the diver is near (" + string.Join(">", states) + ")");
            Ok(w.Player.Health < hp, "the ring catches a diver standing inside its reach");

            w = new World();
            j = JellyIn(w, "Gutter Shaft");
            hp = w.Player.Health;
            for (int i = 0; i < 400; i++)
            {
                w.Player.Place(new Vec2(j.Body.Pos.X + 4.6f, j.Body.Pos.Y - 0.2f), w.Move);
                w.Update(Dt, default);
            }
            Ok(w.Player.Health == hp, "outside the ring's radius it never reaches you");

            w = new World();
            j = JellyIn(w, "Gutter Shaft");
            bool spentTouched = false;
            hp = w.Player.Health;
            for (int i = 0; i < 400 && !spentTouched; i++)
            {
                if (j.State == JellyState.Spent && j.Fsm.Time > 0.1f)
                {
                    w.Player.Place(new Vec2(j.Body.Pos.X, j.Body.Pos.Y - 0.3f), w.Move);
                    w.Player.Invuln = 0f;
                    int h = w.Player.Health;
                    w.Update(Dt, default);
                    spentTouched = w.Player.Health == h;
                    break;
                }
                w.Player.Place(new Vec2(j.Body.Pos.X + 1.4f, j.Body.Pos.Y + 2.5f), w.Move);
                w.Player.Invuln = 9f;
                w.Update(Dt, default);
            }
            Ok(spentTouched, "a spent jelly is harmless to touch (the punish window)");
            Ok(j.RejectedTransitions == 0, "jelly never attempted an illegal state change");
        }
    }

    // ---------------------------------------------------------------------------------------------
    static World ArenaWorld(out RoomInfo arena, float farRushChance = 0.7f)
    {
        var w = new World(boss: new BossTuning { FarRushChance = farRushChance });
        arena = w.Level.Rooms[w.ArenaRoom];
        Teleport(w, arena.MinX + 8.5f, arena.MinY + 6f);
        for (int i = 0; i < 4 && !w.Boss.Awake; i++) w.Update(Dt, default);
        return w;
    }

    static void BossFight()
    {
        Section("Boss");
        {
            var w = ArenaWorld(out RoomInfo arena);
            Ok(w.Boss.Awake && w.Level.DoorsLocked, "landing in the arena wakes the bell and locks the door above");
        }
        // Slam: jumping the shockwave avoids it.
        foreach (bool jump in new[] { false, true })
        {
            var w = ArenaWorld(out RoomInfo arena);
            w.Player.Place(new Vec2(w.Boss.Body.Pos.X - 5f, w.Level.FloorBelow(w.Boss.Body.Pos.X - 5f, arena.MinY + 6f)), w.Move);
            int hp = w.Player.Health;
            bool slammed = false;
            for (int i = 0; i < 60 * 8; i++)
            {
                var input = new PlayerInput();
                if (jump && w.Boss.State == BossState.SlamWindup) input.MoveX = -1; // read the tell: back away from the shadow
                if (jump)
                {
                    foreach (Shockwave s in w.Boss.Waves)
                        if (s.Dir < 0 && s.X - w.Player.Body.Pos.X < 1.9f && s.X > w.Player.Body.Pos.X) { input.JumpPressed = true; input.JumpHeld = true; }
                    if (!w.Player.Body.Grounded) input.JumpHeld = true;
                }
                w.Update(Dt, input);
                if (w.Boss.State == BossState.SlamRecover) slammed = true;
                if (slammed && w.Boss.Waves.Count == 0) break;
            }
            if (!jump) Ok(slammed && w.Player.Health < hp, "standing still, the slam's shockwave hits you");
            else Ok(slammed && w.Player.Health == hp, "jumping the shockwave at the right moment clears it");
        }
        // Rush: on the floor it hits you; on a side ledge it passes under.
        foreach (bool onLedge in new[] { false, true })
        {
            var w = ArenaWorld(out RoomInfo arena, farRushChance: 1f); // force the rush, whatever the seed
            float x = arena.MinX + 3.5f;
            float y = onLedge ? arena.MinY + 6f : arena.MinY + 2.5f;
            w.Player.Place(new Vec2(x, onLedge ? w.Level.FloorBelow(x, y) : w.Level.FloorBelow(x, arena.MinY + 2.9f)), w.Move);
            int hp = w.Player.Health;
            bool rushed = false;
            for (int i = 0; i < 60 * 6; i++)
            {
                w.Update(Dt, default);
                if (w.Boss.State == BossState.Rush) rushed = true;
                if (w.Boss.State == BossState.RushRecover) break;
            }
            if (!onLedge) Ok(rushed && w.Player.Health < hp, "far away on the floor, the rush runs you over");
            else Ok(rushed && w.Player.Health == hp, "standing on a side ledge, the rush passes underneath");
        }
        // Recovery is a punish window, phase two adds the rain, and the bell can be beaten.
        {
            var w = ArenaWorld(out RoomInfo arena);
            w.Player.Invuln = 999f;
            var states = new HashSet<BossState>();
            bool hitInRecovery = false, phase2 = false, victory = false;
            w.Event += e =>
            {
                if (e.Type == GameEventType.BossPhase2) phase2 = true;
                if (e.Type == GameEventType.Victory) victory = true;
            };
            for (int i = 0; i < 60 * 240 && !victory; i++)
            {
                w.Player.Invuln = 999f;
                var input = new PlayerInput();
                Body b = w.Boss.Body;
                bool recovering = w.Boss.State == BossState.SlamRecover || w.Boss.State == BossState.RushRecover || w.Boss.State == BossState.RainRecover;
                float dx = b.Pos.X - w.Player.Body.Pos.X;
                if (recovering)
                {
                    if (MathF.Abs(dx) > 2.2f) input.MoveX = Mathx.Sign(dx);
                    else { w.Player.Facing = Mathx.Sign(dx); input.AttackPressed = i % 20 == 0; }
                }
                int h = w.Boss.Health;
                w.Update(Dt, input);
                states.Add(w.Boss.State);
                if (recovering && w.Boss.Health < h) hitInRecovery = true;
            }
            Ok(hitInRecovery, "the bell takes hits during its recoveries");
            Ok(phase2 && states.Contains(BossState.RainWindup) && states.Contains(BossState.Rain), "below half health it adds the drip rain");
            // A diver who stays close to punish never gets a rush (it needs the diver far away); the forced-rush test above covers it.
            Ok(states.Contains(BossState.SlamWindup), "up close it slams");
            Ok(victory && w.Phase == Phase.Victory && !w.Level.DoorsLocked, "punishing recoveries alone beats it, ends the run and unlocks the door");
            Ok(w.Boss.Fsm.Rejected == 0, "the boss never attempted an illegal state change");
        }
    }

    static void EdgeCases()
    {
        Section("Edge cases");
        // Dying before the lamp is lit returns you to the very start.
        {
            var w = new World();
            Ok(!w.LampLit, "the lamp starts unlit");
            Vec2 start = w.Player.Body.Feet;
            RoomInfo nave = w.Level.Rooms.First(r => r.Name == "Flooded Nave");
            Teleport(w, nave.MinX + 4.5f, nave.MinY + 3f);
            for (int i = 0; i < 20 && w.Phase == Phase.Playing; i++) { w.Player.Invuln = 0f; w.HurtPlayer(w.Player.Body.Pos.X + 1); w.Update(Dt, default); }
            Step(w, World.DeathDelay + 0.2f);
            Ok(w.Phase == Phase.Playing && Vec2.Distance(w.Player.Body.Feet, start) < 0.2f, "dying before lighting the lamp respawns at the start (" + w.Player.Body.Feet + ")");
        }
        {
            var w = new World();
            var start = w.Level.Rooms.First(r => r.Name == "Flooded Nave");
            w.Player.Place(new Vec2(start.MinX + 1.5f, w.Level.PlayerStart.Y), w.Move);
            bool advanced = false, returned = false;
            for (int i = 0; i < 680; i++)
            {
                w.Update(Dt, new PlayerInput { MoveX = advanced ? -1 : 1 });
                if (w.Room == 2) advanced = true;
                if (advanced && w.Room == 1) { returned = true; break; }
            }
            Ok(advanced && returned && w.Foes.All(e => e.Alive),
                "skipping the live nave crab permits entry into Ripple Walk and immediate return");
        }
        // The bell and the diver fall in the same frame: the run still ends, exactly once.
        {
            var w = ArenaWorld(out RoomInfo arena);
            w.Player.Invuln = 999f;
            for (int i = 0; i < 600 && w.Boss.State != BossState.Idle; i++) w.Update(Dt, default);
            int victories = 0;
            w.Event += e => { if (e.Type == GameEventType.Victory) victories++; };
            w.Player.Invuln = 0f;
            w.Player.Health = 1;
            w.Boss.TakeHit(w, 777001, 999);
            w.HurtPlayer(w.Player.Body.Pos.X + 1);
            for (int i = 0; i < 60 * 12; i++) w.Update(Dt, default);
            Ok(w.Phase == Phase.Victory && victories == 1, "a simultaneous death and boss defeat still ends the run once (" + w.Phase + ", " + victories + " victory events)");
        }
        // Many boss retries in a row: the pilot dies four times on purpose, then wins.
        {
            var w = new World();
            var ap = new Autopilot(w, new NavGraph(false), new NavGraph(true), 4);
            int victories = 0;
            w.Event += e => { if (e.Type == GameEventType.Victory) victories++; };
            for (int i = 0; i < 60 * 60 * 25 && ap.Current != Autopilot.Goal.Done; i++) w.Update(Dt, ap.Next());
            // The fight order is random, so the scripted diver may slip once or twice beyond its four planned deaths.
            Ok(ap.RetryTimes.Count >= 4 && ap.RetryTimes.Count <= 7 && w.Phase == Phase.Victory && victories == 1, "four boss retries in a row, then victory once (retries " + string.Join(", ", ap.RetryTimes.Select(r => r.ToString("0.0") + "s")) + ")");
            Ok(w.Boss.Fsm.Rejected == 0 && w.Foes.All(f => f.RejectedTransitions == 0), "no illegal state transitions across the whole run");
        }
    }

    static void BossEnding()
    {
        var w = ArenaWorld(out RoomInfo arena);
        w.Player.Invuln = 999f;
        for (int i = 0; i < 600 && w.Boss.State != BossState.Idle; i++) w.Update(Dt, default); // the intro is invulnerable by design
        w.Boss.TakeHit(w, 424242, 999);
        Step(w, World.VictoryDelay + 0.3f);
        Ok(w.Phase == Phase.Victory, "however the bell falls, the belfry chapter reaches its interlude");
    }

    // ---------------------------------------------------------------------------------------------
    static void RunState()
    {
        Section("Death, respawn and persistence");
        var w = new World();
        Teleport(w, w.Level.Checkpoint.X, w.Level.Checkpoint.Y + 1f);
        Ok(w.LampLit && Vec2.Distance(w.RespawnFeet, w.Level.Checkpoint) < 0.01f, "touching the tide lamp lights it and makes it the respawn point");

        // Kill a crab, take the Brass Lung, open the grate, then die in the arena.
        Crab crab = CrabIn(w, "Flooded Nave");
        crab.TakeHit(w, 9001, 99, crab.Body.Pos.X - 1);
        w.Player.Place(new Vec2(w.Level.Heart.X, w.Level.Heart.Y), w.Move);
        w.Update(Dt, default);
        Ok(w.LungTaken && w.Player.MaxHealth == C.MaxHealth + 1, "the Brass Lung adds a mask");
        w.Player.Place(new Vec2(w.Level.Lever.X - 1.2f, w.Level.Lever.Y), w.Move);
        w.Player.Facing = 1;
        w.Update(Dt, new PlayerInput { AttackPressed = true });
        Step(w, 0.3f);
        Ok(w.GrateOpen, "hitting the winch opens the grate");

        RoomInfo arena = w.Level.Rooms[w.ArenaRoom];
        Teleport(w, arena.MinX + 8.5f, arena.MinY + 6f);
        Step(w, 0.1f);
        w.Boss.TakeHit(w, 9002, 3);
        bool died = false;
        w.Event += e => { if (e.Type == GameEventType.PlayerDied) died = true; };
        for (int i = 0; i < 20 && !died; i++) { w.Player.Invuln = 0f; w.HurtPlayer(w.Player.Body.Pos.X + 1); w.Update(Dt, default); }
        Step(w, World.DeathDelay + 0.1f);
        Ok(died && w.Phase == Phase.Playing && w.Deaths == 1, "dying in the boss fight returns you to play after a short delay");
        Ok(Vec2.Distance(w.Player.Body.Feet, w.Level.Checkpoint) < 0.1f && w.Player.Health == w.Player.MaxHealth, "at the lamp, at full health (feet " + w.Player.Body.Feet + ", lamp " + w.Level.Checkpoint + ", hp " + w.Player.Health + "/" + w.Player.MaxHealth + ")");
        Ok(crab.Alive && crab.Health == crab.MaxHealth, "ordinary enemies are back");
        Ok(w.Boss.State == BossState.Dormant && w.Boss.Health == w.Boss.MaxHealth && !w.Level.DoorsLocked, "the boss resets and the arena door is open again");
        Ok(w.GrateOpen && w.LungTaken && w.Player.MaxHealth == C.MaxHealth + 1, "the opened shortcut and the extra mask stay for the rest of the run");
        Teleport(w, arena.MinX + 8.5f, arena.MinY + 6f);
        w.Player.Invuln = 999f;
        for (int i = 0; i < 600 && w.Boss.State != BossState.Idle; i++) w.Update(Dt, default);
        w.Boss.TakeHit(w, 9003, 999);
        Step(w, World.VictoryDelay + 0.3f);
        var intake = w.EnterSluice();
        Ok(intake.LungTaken && intake.Player.MaxHealth == C.MaxHealth + 1 && intake.Time == w.Time && intake.Deaths == 1,
            "chapter transition carries the Brass Lung, time and deaths");
        Ok(!intake.GrateOpen && !intake.LampLit && intake.Phase == Phase.Playing, "Sluice Works starts with its own lamp and valve");
    }

    // ---------------------------------------------------------------------------------------------
    static SaveData ThroughFile(SaveData s) => SaveData.Parse(s.Serialize());

    static void SaveChecks()
    {
        Section("Save and continue");
        var full = new SaveData { Chapter = Chapter.Sluice, Time = 1234.5678f, Deaths = 7, Lung = true, Lamp = true, Gate = true, Gauge = true, Bypass = true, Lift = true, Key = true, WardenDown = true };
        SaveData back = ThroughFile(full);
        Ok(back != null && back.Serialize() == full.Serialize() && back.Time == full.Time, "a save reads back exactly as written");
        string text = full.Serialize();
        Ok(SaveData.Parse(text.Substring(0, text.IndexOf("end="))) == null, "a file cut off mid-write is ignored");
        Ok(SaveData.Parse(text.Replace("version=1", "version=99")) == null, "a save from another version is ignored");
        Ok(SaveData.Parse(text.Replace("deaths=7", "deaths=seven")) == null && SaveData.Parse(text.Replace("chapter=Sluice", "chapter=Moon")) == null,
            "a damaged value is ignored instead of crashing");
        Ok(SaveData.Parse("") == null && SaveData.Parse("garbage\n=\n==") == null && SaveData.Parse(new SaveData { Time = 2f }.Serialize().Replace("time=2", "time=NaN")) == null,
            "empty or nonsense files are ignored");
        Ok(SaveData.Parse(text.Replace("\n", "\r\n")) != null, "Windows line endings still read");

        // Belfry: lamp, Brass Lung and the winch survive a quit and continue.
        var w = new World();
        Teleport(w, w.Level.Checkpoint.X, w.Level.Checkpoint.Y + 1f);
        w.Player.Place(new Vec2(w.Level.Heart.X, w.Level.Heart.Y), w.Move);
        w.Update(Dt, default);
        w.Player.Place(new Vec2(w.Level.Lever.X - 1.2f, w.Level.Lever.Y), w.Move);
        w.Player.Facing = 1;
        w.Update(Dt, new PlayerInput { AttackPressed = true });
        Step(w, 0.3f);
        var c = World.Restore(ThroughFile(w.Snapshot()));
        Ok(c.Chapter == Chapter.Belfry && c.LampLit && c.GrateOpen && c.LungTaken && c.Player.MaxHealth == C.MaxHealth + 1 && c.Player.Health == c.Player.MaxHealth,
            "Belfry continue keeps the lamp, open grate and Brass Lung, at full health");
        Ok(Vec2.Distance(c.Player.Body.Feet, w.Level.Checkpoint) < 0.1f && c.Room == c.Level.RoomIndexAt(c.Player.Body.Pos) && MathF.Abs(c.Time - w.Time) < 1e-4f,
            "and wakes at the tide lamp with the same play time");
        Ok(World.Restore(new SaveData()).Player.Body.Feet.X == new World().Player.Body.Feet.X, "a save made before the lamp starts at the beginning");

        // Beating the Undertoll saves the start of the next chapter.
        RoomInfo arena = w.Level.Rooms[w.ArenaRoom];
        Teleport(w, arena.MinX + 8.5f, arena.MinY + 6f);
        w.Player.Invuln = 999f;
        for (int i = 0; i < 600 && w.Boss.State != BossState.Idle; i++) w.Update(Dt, default);
        w.Boss.TakeHit(w, 9101, 999);
        SaveData atClear = ThroughFile(w.Snapshot());
        var works = World.Restore(atClear);
        Ok(atClear.Chapter == Chapter.Sluice && works.Chapter == Chapter.Sluice && !works.LampLit && !works.GrateOpen && works.LungTaken,
            "a save after the Undertoll continues at the start of the Sluice Works with the Brass Lung");
        Ok(Vec2.Distance(works.Player.Body.Feet, works.Level.PlayerStart) < 0.1f, "at the Works entrance");

        // Sluice: snapshot just after the pump key, continue, and finish the chapter from there.
        var s = new World(level: SluiceWorks.Build());
        SaveData afterKey = null;
        for (int i = 0; i < 60 * 180 && afterKey == null; i++)
        {
            s.Update(Dt, SluiceWorks.RouteInput(s));
            if (s.PumpKey) afterKey = ThroughFile(s.Snapshot());
        }
        Ok(afterKey != null && afterKey.Key && afterKey.WardenDown && afterKey.Lift && afterKey.Lamp && afterKey.Gate, "the pump key saves the lift, valve, Warden and key");
        var r = World.Restore(afterKey);
        Ok(!r.Warden.Alive && r.PumpKey && r.LiftOnline && r.GrateOpen && r.GaugeFound == s.GaugeFound, "Sluice continue restores every opened station");
        Ok(Vec2.Distance(r.Player.Body.Feet, r.Level.LiftLamp.Value) < 0.1f, "and wakes at the counterweight lift");
        r.Respawn();
        Ok(!r.Warden.Alive, "a beaten Warden stays down after a death");
        for (int i = 0; i < 60 * 120 && r.Phase != Phase.Victory; i++) r.Update(Dt, SluiceWorks.RouteInput(r));
        Ok(r.Phase == Phase.Victory, "a continued run can still reach the archive hatch");
    }

    // ---------------------------------------------------------------------------------------------
    static void Route()
    {
        Section("Route, sized with the real controller");
        var t0 = DateTime.Now;
        var closed = new NavGraph(false);
        var open = new NavGraph(true);
        Console.WriteLine("        " + closed.Nodes.Count + " standing spots, " + (closed.Simulations + open.Simulations) + " simulated jumps in " + (DateTime.Now - t0).TotalSeconds.ToString("0.0") + "s");
        Level lv = closed.Level;
        int start = closed.NodeAt(lv.PlayerStart, 2);
        var fromStart = closed.From(start);
        string[] names = lv.Rooms.Select(r => r.Name).ToArray();
        foreach (RoomInfo r in lv.Rooms)
        {
            bool reached = fromStart.Any(n => closed.RoomOf(n) == r.Index);
            Ok(reached, "from the start, " + r.Name + " is reachable (grate closed)");
        }
        int lamp = closed.NodeAt(lv.Checkpoint, 2), winch = closed.NodeAt(lv.Lever, 2), lung = closed.NodeAt(lv.Heart, 1);
        Ok(fromStart.Contains(lamp), "the tide lamp is reachable");
        Ok(winch >= 0 && fromStart.Contains(winch), "a spot within swing reach of the winch is reachable");
        Ok(lung >= 0 && fromStart.Contains(lung), "the optional Brass Lung ledge is reachable");

        int r5 = open.NodeAt(lv.Lever, 2);
        int lampOpen = open.NodeAt(lv.Checkpoint, 2);
        Ok(open.From(r5).Contains(lampOpen), "with the grate open, the Bell Ropes lead back up to the lamp (the shortcut)");
        Ok(!closed.From(closed.NodeAt(lv.Lever, 2)).Contains(lamp), "with it closed they do not (so the shortcut is a real unlock)");
        Ok(open.From(lampOpen).Any(n => open.RoomOf(n) == lv.Rooms.First(r => r.Name == "The Undertoll").Index), "from the lamp, the boss arena is reachable through the shortcut (short retry route)");

        // No soft-locks: before the winch, every spot you can reach can still get to the winch; after, to the lamp.
        int arenaIndex = lv.Rooms.First(r => r.Name == "The Undertoll").Index;
        var stuckBefore = fromStart.Where(n => closed.RoomOf(n) != arenaIndex && !closed.From(n).Contains(winch) && !closed.From(n).Contains(lamp)).ToList();
        Ok(stuckBefore.Count == 0, "no reachable spot (outside the arena) is a dead end before the winch is pulled" + (stuckBefore.Count > 0 ? " (stuck at " + string.Join(" ", stuckBefore.Take(6).Select(n => closed.Nodes[n].ToString())) + ")" : ""));
        var fromLampOpen = open.From(lampOpen);
        var stuckAfter = fromLampOpen.Where(n => open.RoomOf(n) != arenaIndex && !open.From(n).Contains(lampOpen)).ToList();
        Ok(stuckAfter.Count == 0, "with the grate open, every reachable spot outside the arena can get back to the lamp" + (stuckAfter.Count > 0 ? " (stuck at " + string.Join(" ", stuckAfter.Take(6).Select(n => open.Nodes[n].ToString())) + ")" : ""));
    }

    /// <summary>The QA autopilot plays the whole game through inputs only, dies twice on purpose in the boss fight, then wins.</summary>
    static void Pilot()
    {
        Section("Autopilot playthrough (inputs only)");
        var t0 = DateTime.Now;
        var closed = new NavGraph(false);
        var open = new NavGraph(true);
        var w = new World();
        var ap = new Autopilot(w, closed, open, 2) { Verbose = Environment.GetEnvironmentVariable("PILOT_TRACE") == "1" };
        int deaths = 0, hazards = 0;
        w.Event += e => { if (e.Type == GameEventType.PlayerDied) deaths++; if (e.Type == GameEventType.HazardHit) hazards++; };
        int ticks = 0;
        for (; ticks < 60 * 60 * 25 && ap.Current != Autopilot.Goal.Done; ticks++)
        {
            PlayerInput inp = ap.Next();
            w.Update(Dt, inp);
            if (Environment.GetEnvironmentVariable("PILOT_TRACE") == "1" && ticks % 30 == 0 && ticks > 60 * 29 && ticks < 60 * 40)
                Console.WriteLine("        t=" + w.Time.ToString("0.0") + " room " + w.Room + " pos " + w.Player.Body.Pos + " vel " + w.Player.Body.Vel + " grounded " + w.Player.Body.Grounded + " node " + (w.GrateOpen ? open : closed).NodeAt(w.Player.Body.Feet, 1) + " in " + inp.MoveX + (inp.JumpPressed ? " J" : "") + (inp.DashPressed ? " D" : "") + " goal " + ap.Current);
        }
        foreach (string line in ap.Log) Console.WriteLine("        " + line);
        Console.WriteLine("        (simulated in " + (DateTime.Now - t0).TotalSeconds.ToString("0.0") + "s; goal " + ap.Current + "; no-path events " + ap.NoPathEvents + ")");
        Ok(w.LampLit && w.LungTaken && w.GrateOpen, "lamp lit, Brass Lung taken, winch opened");
        Ok(ap.ShortcutClimbed, "climbed the opened shortcut back up to the lamp");
        Ok(ap.RetryTimes.Count == 2, "died twice in the boss fight and came back both times (" + string.Join(", ", ap.RetryTimes.Select(r => r.ToString("0.0") + "s")) + " from respawn to arena)");
        Ok(ap.RetryTimes.All(r => r < 25f), "each boss retry takes under 25 s of play from the lamp");
        Ok(w.Phase == Phase.Victory, "beat the Undertoll and reached the ending (" + (ap.VictoryAt >= 0 ? ap.VictoryAt.ToString("0") + "s" : "not reached") + ")");
        int nonScripted = deaths - 2;
        Ok(nonScripted <= 2, "unplanned deaths along the way: " + nonScripted + ", spike hits: " + hazards);
    }

    static void Determinism()
    {
        Section("Determinism");
        string Run()
        {
            var w = new World();
            var log = new System.Text.StringBuilder();
            w.Event += e => log.Append((int)e.Type).Append(',');
            for (int i = 0; i < 60 * 30; i++)
                w.Update(Dt, new PlayerInput { MoveX = MathF.Sin(i * 0.02f) > -0.3f ? 1 : -1, JumpPressed = i % 47 == 0, JumpHeld = i % 47 < 20, AttackPressed = i % 23 == 0, DashPressed = i % 101 == 0 });
            return w.Player.Body.Pos + "|" + w.Player.Health + "|" + log;
        }
        Ok(Run() == Run(), "two runs with the same inputs end identically");
    }
}
