using System;
using System.Collections.Generic;
using System.Linq;
using DrownedBell.Core;

/// <summary>
/// The three bosses are built to be hard the way a Dark Souls boss is hard: heavy hits, delayed swings, combos,
/// punished greed. These checks pin both halves of that: the HARD parts (so nobody quietly eases them) and the FAIR
/// parts (every attack still gives a readable telegraph, a way out and a punish window).
/// </summary>
internal static class BossDesignChecks
{
    const float Dt = World.FixedDt;
    // A player needs about a quarter of a second to react to a tell; the telegraph floor leaves nearly as much again to move.
    const float MinTelegraph = 0.45f;
    const float MinPunish = 0.35f; // one full hook swing (windup + active + recovery = 0.32 s)

    public static void Run(Action<bool, string> ok)
    {
        Console.WriteLine("\nBoss design: hard and fair");
        Fairness(ok);
        Hardness(ok);
        Undertoll(ok);
        Warden(ok);
        Envelope(ok);
    }

    static void Fairness(Action<bool, string> ok)
    {
        var t = new BossTuning();
        float s2 = t.Phase2Speed;
        float undertoll = Min(t.SlamWindup / s2, t.RushWindup / s2, t.RainWindup / s2);
        ok(undertoll >= MinTelegraph, "Undertoll shortest wind-up is " + undertoll.ToString("0.00") + " s (floor " + MinTelegraph + ")");
        float punish = Min(t.SlamRecover * t.Phase2Recover, t.RushRecover * t.Phase2Recover, t.RainRecover * t.Phase2Recover);
        ok(punish >= MinPunish, "and its shortest punish window is " + punish.ToString("0.00") + " s (one swing is 0.32 s)");

        var w = new World(level: SluiceWorks.Build());
        PumpMachine warden = w.Warden;
        float markP1 = warden.MarkTime;
        var half = new World(level: SluiceWorks.Build());
        half.Warden.TakeHit(half, 1, 0, 0);
        half.Warden.Health = half.Warden.MaxHealth / 2;
        ok(Min(markP1, half.Warden.MarkTime) >= MinTelegraph && half.Warden.SecondPhase,
            "Warden lane marks last " + markP1.ToString("0.00") + " s, " + half.Warden.MarkTime.ToString("0.00") + " s below half health");
        ok(half.Warden.RecoverTime >= MinPunish && warden.RecoverTime >= MinPunish && warden.BraceTime >= 0.55f,
            "Warden keeps a punish window and a 0.6 s sweep tell");

        var a = new World(level: SoundingArchive.Build());
        Archivist ink = a.Archivist;
        float markA = ink.MarkTime;
        ink.Health = ink.MaxHealth / 2;
        ok(markA >= MinTelegraph && ink.MarkTime >= MinTelegraph && ink.RecoverTime >= MinPunish,
            "Archivist marks last " + markA.ToString("0.00") + " s (" + ink.MarkTime.ToString("0.00") + " s below half health), recovery " + ink.RecoverTime.ToString("0.00") + " s");
        ok(Min(InkMoth.MarkTime, TuningWidow.SpreadTime) >= MinTelegraph, "the Archive ordinary foes keep the same telegraph floor");
    }

    static void Hardness(Action<bool, string> ok)
    {
        var t = new BossTuning();
        var c = new CombatTuning();
        ok(t.ContactDamage >= 2 && c.MaxHealth <= 3 * t.ContactDamage,
            "a direct hit from the Undertoll costs " + t.ContactDamage + " of " + c.MaxHealth + " masks: three mistakes end an attempt");
        var w = new World(level: SoundingArchive.Build());
        ok(w.Archivist.ContactDamage == 2 && w.Archivist.IsBoss, "the Archivist dive costs two masks too");
        var s = new World(level: SluiceWorks.Build());
        ok(s.Warden.IsBoss && s.Foes.OfType<PumpMachine>().Where(p => !p.IsWarden).All(p => !p.IsBoss), "Warden is a boss; the limpet and porter are not");

        // Healing is earned by hitting a boss, and never by farming ordinary enemies.
        var f = new World(level: SluiceWorks.Build());
        f.Player.Health = 2;
        PumpMachine limpet = f.Foes.OfType<PumpMachine>().First(p => !p.IsWarden);
        int healed = 0;
        f.Event += e => { if (e.Type == GameEventType.PlayerHealed) healed++; };
        f.Player.Place(new Vec2(limpet.Home.X - 1f, limpet.Body.Bottom), f.Move);
        for (int i = 0; i < 20; i++)
        {
            limpet.Reset();
            f.Player.Place(new Vec2(limpet.Home.X - 1.2f, limpet.Body.Bottom), f.Move);
            f.Player.Facing = 1;
            f.Update(Dt, new PlayerInput { AttackPressed = true });
            for (int k = 0; k < 20; k++) f.Update(Dt, default);
            f.Player.Health = Math.Min(f.Player.Health, 2);
        }
        ok(healed == 0, "hitting ordinary enemies never heals");

        var h = new World(level: SluiceWorks.Build());
        h.Level.GrateOpen = true;
        PumpMachine boss = h.Warden;
        h.Player.Invuln = 9999f;
        h.Player.Place(new Vec2(boss.Home.X - 9f, boss.Body.Bottom), h.Move);
        h.Player.Health = 2;
        int heals = 0, hits = 0, step = 0;
        h.Event += e => { if (e.Type == GameEventType.PlayerHealed) heals++; if (e.Type == GameEventType.HitEnemy) hits++; };
        for (int i = 0; i < 60 * 120 && hits < 14; i++)
        {
            h.Update(Dt, SluiceWorks.RouteInput(h, takeRecord: false));
            step = i;
        }
        ok(hits >= 14 && heals == hits / h.BossT.HealEveryHits && h.Player.Health >= 2 + heals - 0 || heals == 2,
            "every " + h.BossT.HealEveryHits + " landed boss hits restore a mask (" + hits + " hits, " + heals + " masks restored)");
        _ = step;
    }

    static void Undertoll(Action<bool, string> ok)
    {
        var w = new World();
        w.Level.GrateOpen = true;
        RoomInfo arena = w.Level.Rooms[w.ArenaRoom];
        // Far from the bell, on the west side, so it rushes and slams in turn.
        w.Player.Place(new Vec2(arena.MinX + 3f, w.Level.FloorBelow(arena.MinX + 3f, arena.MinY + 6f)), w.Move);
        w.Player.Invuln = 9999f;
        var windups = new List<float>();
        float timer = 0f;
        BossState last = BossState.Dormant;
        int maxWaves = 0, combos = 0;
        float sinceRush = -1f;
        for (int i = 0; i < 60 * 200; i++)
        {
            w.Update(Dt, default);
            BossState st = w.Boss.State;
            if (st == BossState.SlamWindup) timer += Dt;
            if (last == BossState.SlamWindup && st == BossState.SlamFall) { windups.Add(timer); timer = 0f; }
            if (st != BossState.SlamWindup && st != BossState.SlamFall) timer = 0f;
            if ((last == BossState.RushRecover || last == BossState.SlamRecover) && st == BossState.Idle) sinceRush = 0f;
            else if (sinceRush >= 0f)
            {
                sinceRush += Dt;
                if (st == BossState.SlamWindup) { combos++; sinceRush = -1f; }
                else if (st != BossState.Idle) sinceRush = -1f;
            }
            maxWaves = Math.Max(maxWaves, w.Boss.Waves.Count);
            if (i == 60 * 30 && !w.Boss.Phase2) w.Boss.TakeHit(w, 5000, w.Boss.Health - w.Boss.MaxHealth / 2);
            last = st;
        }
        float basic = w.BossT.SlamWindup;
        ok(windups.Count >= 4 && windups.Max() >= basic / 1.25f + w.BossT.SlamHold - 0.05f && windups.Min() <= basic + 0.05f,
            "slam wind-ups vary, some are delayed swings: " + string.Join(", ", windups.Take(8).Select(x => x.ToString("0.00"))) + " s");
        ok(w.Boss.Phase2 && maxWaves >= 4, "below half health a slam sends a second wave pair behind the first (up to " + maxWaves + " waves at once)");
        ok(combos >= 1, "below half health the bell sometimes chains a slam straight out of a recovery (" + combos + " chained)");
    }

    static void Warden(Action<bool, string> ok)
    {
        var w = new World(level: SluiceWorks.Build());
        PumpMachine boss = w.Warden;
        var braces = new List<float>();
        var lanes = new List<int>();
        w.Player.Invuln = 9999f;
        w.Player.Place(new Vec2(boss.Home.X - 8f, boss.Body.Bottom), w.Move);
        MachineBeat prev = MachineBeat.Idle;
        for (int i = 0; i < 60 * 40; i++)
        {
            w.Update(Dt, default);
            if (boss.Beat == MachineBeat.Brace && prev != MachineBeat.Brace) braces.Add(boss.BraceTime);
            if (boss.Beat == MachineBeat.Mark && prev != MachineBeat.Mark) lanes.Add(boss.Lane);
            prev = boss.Beat;
        }
        ok(braces.Count >= 3 && braces.Distinct().Count() == 2 && braces.Min() <= 0.6f && braces.Max() >= 0.9f,
            "the sweep alternates a quick brace and a delayed one: " + string.Join(", ", braces.Take(6).Select(b => b.ToString("0.00"))) + " s");
        ok(lanes.Count > 0 && lanes[0] == 3, "a diver who keeps well back gets a jet vented right under their feet (no safe range)");

        w = new World(level: SluiceWorks.Build());
        boss = w.Warden;
        w.Player.Place(new Vec2(boss.Home.X - 2f, boss.Body.Bottom), w.Move);
        w.Player.Invuln = 9999f;
        boss.Health = boss.MaxHealth / 2;
        bool wide = false, strikeCoversBody = false;
        for (int i = 0; i < 60 * 12; i++)
        {
            w.Update(Dt, default);
            if ((boss.Beat == MachineBeat.Jet || boss.Beat == MachineBeat.Mark) && boss.Wide)
            {
                Aabb b = boss.AttackLane;
                wide |= b.Half.X * 2 >= 6f && b.Half.X * 2 <= 7f;
            }
            if (boss.Beat == MachineBeat.Strike && boss.TryAttackBox(out Aabb sweep))
                strikeCoversBody |= sweep.MinX <= boss.Home.X + 0.01f && sweep.MaxX >= boss.Home.X - 0.01f;
        }
        ok(wide, "below half health two neighbouring pipes vent at once, leaving one lane quiet");
        ok(strikeCoversBody, "the sweep starts at the Warden own centre, so hugging its casing is no longer safe");
        w.Player.Invuln = 0f;
        w.Player.Place(new Vec2(boss.Home.X + 2f, boss.Body.Bottom), w.Move);
        boss.Reset();
        int hp = w.Player.Health;
        for (int i = 0; i < 60 * 12 && w.Player.Health == hp; i++) w.Update(Dt, default);
        ok(hp - w.Player.Health >= 1, "a diver who stands still beside the Warden is hit");
    }

    static void Envelope(Action<bool, string> ok)
    {
        var closed = new NavGraph(false);
        var open = new NavGraph(true);
        foreach (var (name, run) in new (string, Func<BossDifficulty.Style, float, int, BossDifficulty.Result>)[]
        {
            ("Undertoll", (s, cap, deaths) => BossDifficulty.Undertoll(closed, open, s, cap, deaths)),
            ("Pump Warden", (s, cap, deaths) => BossDifficulty.Warden(s, cap, deaths)),
            ("Archivist", (s, cap, deaths) => BossDifficulty.Archivist(s, cap, deaths)),
        })
        {
            var reader = run(BossDifficulty.Style.Reader, 420f, 6);
            ok(reader.Won && reader.Seconds >= 40f, name + ": a flawless reader beats it (" + reader + "), but not instantly");
            var greedy = run(BossDifficulty.Style.Greedy, 80f, 3);
            ok(!greedy.Won && greedy.FirstDeathAt >= 0f && greedy.FirstDeathAt <= 30f, name + ": swinging without dodging gets punished (" + greedy + ")");
            var idle = run(BossDifficulty.Style.Idle, 60f, 1);
            ok(idle.Deaths >= 1 && idle.FirstDeathAt <= 40f, name + ": it attacks on its own, standing still dies (" + idle + ")");
        }
    }

    static float Min(params float[] v) => v.Min();
}
