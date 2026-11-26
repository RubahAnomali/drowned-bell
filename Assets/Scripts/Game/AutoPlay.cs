using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// QA mode, off unless the build is launched with "-autoplay &lt;folder&gt;" (optional "-retries N").
    /// The normal game runs with full rendering and audio, but each fixed step takes its input from the
    /// Autopilot instead of the keyboard. Screenshots are saved at every milestone, pause and a smaller window
    /// are exercised, every runtime error is captured, and a report is written before the game quits.
    /// </summary>
    public sealed class AutoPlay : MonoBehaviour
    {
        GameController game;
        string dir;
        int retries = 2;
        Autopilot pilot;
        readonly List<string> report = new List<string>();
        readonly List<string> errors = new List<string>();
        readonly Queue<string> shotQueue = new Queue<string>();
        readonly HashSet<string> taken = new HashSet<string>();
        int victories, deaths;
        float realStart;

        public static void AttachIfRequested(GameController game)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-autoplay");
            if (i < 0 || i + 1 >= args.Length) return;
            SaveStore.Enabled = false; // QA never reads or writes the player's save
            Application.runInBackground = true;
            var ap = game.gameObject.AddComponent<AutoPlay>();
            ap.game = game;
            ap.dir = args[i + 1];
            int r = Array.IndexOf(args, "-retries");
            if (r >= 0 && r + 1 < args.Length && int.TryParse(args[r + 1], out int n)) ap.retries = n;
            int f = Array.IndexOf(args, "-frames");
            if (f >= 0 && f + 1 < args.Length) ap.framesDir = args[f + 1];
        }

        /// <summary>
        /// "-frames &lt;folder&gt;" saves every frame of the boss fight, at a fixed 30 fps of game time, as numbered PNGs
        /// (for the portfolio clip: ffmpeg -framerate 30 -i f%05d.png). Capture time does not slow the game down.
        /// </summary>
        string framesDir;
        bool recording;

        IEnumerator Record()
        {
            recording = true;
            Directory.CreateDirectory(framesDir);
            Time.captureFramerate = 30;
            for (int i = 0; i < 30 * 60 && game.CurrentWorld.Phase != Phase.Victory; i++)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(framesDir, "f" + i.ToString("00000") + ".png"));
            }
            Time.captureFramerate = 0;
        }

        void OnEnable() { Application.logMessageReceived += OnLog; }
        void OnDisable() { Application.logMessageReceived -= OnLog; }

        void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(type + ": " + msg + "\n" + stack);
        }

        void Shot(string name)
        {
            if (taken.Add(name)) shotQueue.Enqueue(name);
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            realStart = Time.realtimeSinceStartup;
            yield return new WaitForSeconds(1.5f);
            yield return Capture("00_title");

            game.BeginAutoplay();
            World w = game.CurrentWorld;
            var t0 = Time.realtimeSinceStartup;
            var closed = new NavGraph(false, game.movement, game.combat);
            var open = new NavGraph(true, game.movement, game.combat);
            report.Add("movement graph: " + closed.Nodes.Count + " spots, built in " + (Time.realtimeSinceStartup - t0).ToString("0.0") + "s");
            pilot = new Autopilot(w, closed, open, retries);
            game.SetPilot(pilot.Next);
            w.Event += e => OnEvent(w, e);

            bool pausedOnce = false, resized = false;
            float giveUp = Time.realtimeSinceStartup + 15f * 60f;
            float periodic = 0f;
            float longestCombatFrame = 0f, combatSeconds = 0f, audioPeak = 0f;
            int combatFrames = 0;
            float[] samples = new float[256];
            while (Time.realtimeSinceStartup < giveUp)
            {
                AudioListener.GetOutputData(samples, 0);
                foreach (float sample in samples) audioPeak = Mathf.Max(audioPeak, Mathf.Abs(sample));
                if (w.Boss.Awake && !w.Boss.Dead && w.Phase == Phase.Playing)
                {
                    combatFrames++;
                    combatSeconds += Time.unscaledDeltaTime;
                    longestCombatFrame = Mathf.Max(longestCombatFrame, Time.unscaledDeltaTime);
                }
                // Pause once, mid-route, and check the game stops and resumes.
                if (!pausedOnce && w.Time > 12f)
                {
                    pausedOnce = true;
                    Vec2 before = w.Player.Body.Pos;
                    float tb = w.Time;
                    game.TourPause(true);
                    yield return new WaitForSecondsRealtime(1.0f);
                    yield return Capture("pause_menu");
                    bool frozen = Math.Abs(w.Time - tb) < 1e-4f && Vec2.Distance(before, w.Player.Body.Pos) < 1e-4f;
                    report.Add("pause: simulation frozen while paused = " + frozen);
                    game.TourPause(false);
                }
                // A smaller window, once, after the shortcut has been climbed.
                if (!resized && pilot.ShortcutClimbed)
                {
                    resized = true;
                    Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                    yield return new WaitForSecondsRealtime(0.8f);
                    yield return Capture("window_1280x720");
                    report.Add("resized to " + Screen.width + "x" + Screen.height + " (camera aspect " + Camera.main.aspect.ToString("0.00") + ")");
                    Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
                    yield return new WaitForSecondsRealtime(0.5f);
                }
                while (shotQueue.Count > 0) yield return Capture(shotQueue.Dequeue());
                periodic += Time.unscaledDeltaTime;
                if (periodic > 20f) { periodic = 0f; yield return Capture("t" + ((int)w.Time).ToString("000") + "_" + (pilot.Current.ToString())); }
                if (w.Phase == Phase.Victory)
                {
                    yield return new WaitForSecondsRealtime(2.5f);
                    yield return Capture("zz_victory_screen");
                    // "Dive again" from the victory screen: a fresh run must start clean.
                    game.ContinueJourney();
                    World intake = game.CurrentWorld;
                    int initialDeaths = intake.Deaths;
                    float respawnAt = -1f, retrySeconds = -1f;
                    intake.Event += e =>
                    {
                        if (e.Type == GameEventType.Respawned) respawnAt = intake.Time;
                    };
                    game.SetPilot(() =>
                    {
                        if (intake.Room == intake.Warden.Room && intake.Deaths == initialDeaths)
                        {
                            float gap = intake.Warden.Home.X - intake.Player.Body.Pos.X;
                            return new PlayerInput { MoveX = Mathx.Clamp(gap * 2f, -1, 1) };
                        }
                        if (respawnAt >= 0 && retrySeconds < 0 && intake.Warden.Engaged)
                            retrySeconds = intake.Time - respawnAt;
                        return SluiceWorks.RouteInput(intake);
                    });
                    var seenBeats = new HashSet<string>();
                    int lastRoom = -1;
                    float intakeDeadline = Time.realtimeSinceStartup + 180f;
                    while (intake.Phase != Phase.Victory && Time.realtimeSinceStartup < intakeDeadline)
                    {
                        foreach (Enemy foe in intake.Foes)
                        {
                            if (!(foe is PumpMachine m) || !m.Alive || m.Room != intake.Room || !m.Engaged) continue;
                            string name = m.Kind + "_" + m.Beat + (m.SecondPhase ? "_surge" : "");
                            if (seenBeats.Add(name)) yield return Capture("works_" + name);
                        }
                        if (lastRoom != intake.Room)
                        {
                            lastRoom = intake.Room;
                            yield return Capture("sluice_" + lastRoom);
                        }
                        yield return null;
                    }
                    report.Add("Sluice Works: " + intake.Phase + ", lamp " + intake.LampLit + ", gate " + intake.GrateOpen +
                        ", total deaths " + intake.Deaths + ", campaign time " + intake.Time.ToString("0.0"));
                    report.Add($"Pump Warden: retry {retrySeconds:F1}s from lift respawn to engagement, gauge {intake.GaugeFound}, key {intake.PumpKey}, lift {intake.LiftOnline}");
                    if (retrySeconds < 0 || !intake.GaugeFound || !intake.PumpKey || intake.Warden.Alive) errors.Add("Incomplete Works coverage");
                    if (intake.Phase != Phase.Victory) { errors.Add("Sluice outlet unreachable"); break; }
                    yield return Capture("sluice_outlet");
                    game.ContinueJourney();

                    // Chapter III: the Sounding Archive, played by the same input-only route the headless checks use.
                    World archive = game.CurrentWorld;
                    game.SetPilot(() => SoundingArchive.RouteInput(archive));
                    var archiveBeats = new HashSet<string>();
                    int archiveRoom = -1;
                    float archiveDeadline = Time.realtimeSinceStartup + 180f;
                    while (archive.Phase != Phase.Victory && Time.realtimeSinceStartup < archiveDeadline)
                    {
                        if (archiveRoom != archive.Room) { archiveRoom = archive.Room; yield return Capture("archive_room_" + archiveRoom); }
                        foreach (Enemy foe in archive.Foes)
                        {
                            if (foe.Room != archive.Room || !foe.Alive) continue;
                            string beat = foe is Archivist ar && ar.Engaged ? "archivist_" + ar.Beat + (ar.SecondPhase ? "_phase2" : "")
                                : foe is TuningWidow tw && tw.Beat != WidowBeat.Idle ? "widow_" + tw.Beat
                                : foe is InkMoth im && im.Beat != MothBeat.Hover ? "moth_" + im.Beat : null;
                            if (beat != null && archiveBeats.Add(beat)) yield return Capture(beat);
                        }
                        yield return null;
                    }
                    report.Add("Sounding Archive: " + archive.Phase + ", ledger " + archive.LedgerFound + ", drum key " + archive.DrumKey + ", chute " + archive.GrateOpen +
                        ", order " + archive.OrderTaken + ", total deaths " + archive.Deaths + ", campaign time " + archive.Time.ToString("0.0"));
                    if (archive.Phase != Phase.Victory) { errors.Add("Archive exit unreachable"); break; }
                    yield return new WaitForSecondsRealtime(2f);
                    yield return Capture("archive_victory_screen");
                    game.ContinueJourney();
                    yield return new WaitForSecondsRealtime(1.5f);
                    World fresh = game.CurrentWorld;
                    report.Add("dive again: new run time " + fresh.Time.ToString("0.0") + "s, deaths " + fresh.Deaths + ", lamp " + fresh.LampLit + ", grate " + fresh.GrateOpen + ", lung " + fresh.LungTaken +
                               ", boss " + fresh.Boss.State + " hp " + fresh.Boss.Health + ", phase " + fresh.Phase + ", enemies alive " + fresh.Foes.FindAll(f => f.Alive).Count + "/" + fresh.Foes.Count);
                    yield return Capture("zz_dive_again");
                    break;
                }
                yield return null;
            }
            report.Add($"audio output peak {audioPeak:F4}, boss average {combatFrames / Mathf.Max(combatSeconds, 0.001f):F1} fps, longest frame {longestCombatFrame * 1000f:F0} ms");
            WriteReport(w);
            Application.Quit();
        }

        void OnEvent(World w, GameEvent e)
        {
            string room = w.Room >= 0 ? w.Level.Rooms[w.Room].Name.Replace(' ', '_') : "none";
            switch (e.Type)
            {
                case GameEventType.RoomEntered: Shot("room_" + room); break;
                case GameEventType.LampLit: Shot("lamp_lit"); break;
                case GameEventType.LungTaken: Shot("brass_lung"); break;
                case GameEventType.WinchPulled: Shot("winch_grate_open"); break;
                case GameEventType.BossAwake:
                    Shot("boss_awake_" + pilot.RetriesDone);
                    if (framesDir != null && !recording) StartCoroutine(Record());
                    break;
                case GameEventType.BossPhase2: Shot("boss_phase2"); break;
                case GameEventType.BossDefeated: Shot("boss_defeated"); break;
                case GameEventType.PlayerDied: deaths++; Shot("death_" + deaths); break;
                case GameEventType.Respawned: Shot("respawn_" + deaths); break;
                case GameEventType.Victory: victories++; break;
            }
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return null;
        }

        void WriteReport(World w)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DROWNED BELL autoplay report");
            sb.AppendLine("result: " + (w.Phase == Phase.Victory ? "VICTORY" : "NOT FINISHED (" + pilot.Current + ")"));
            sb.AppendLine("game time to victory: " + (pilot.VictoryAt >= 0 ? pilot.VictoryAt.ToString("0.0") + "s" : "-"));
            sb.AppendLine("real time: " + (Time.realtimeSinceStartup - realStart).ToString("0") + "s");
            sb.AppendLine("deaths: " + w.Deaths + " (planned boss retries " + retries + ")   victory events: " + victories);
            sb.AppendLine("boss retry, respawn to arena: " + string.Join(", ", pilot.RetryTimes.ConvertAll(t => t.ToString("0.0") + "s")) + " (+" + World.DeathDelay + "s death fade)");
            sb.AppendLine("shortcut climbed: " + pilot.ShortcutClimbed + "   lamp " + w.LampLit + "   lung " + w.LungTaken + "   grate " + w.GrateOpen);
            sb.AppendLine("moves that landed off-plan: " + pilot.Misses + "   no-path events: " + pilot.NoPathEvents);
            foreach (var kv in pilot.ReachedAt) sb.AppendLine("reached " + kv.Key + " at " + kv.Value.ToString("0.0") + "s");
            foreach (string r in report) sb.AppendLine(r);
            sb.AppendLine("runtime errors: " + errors.Count);
            foreach (string e in errors) sb.AppendLine(e);
            sb.AppendLine("--- pilot log ---");
            foreach (string l in pilot.Log) sb.AppendLine(l);
            File.WriteAllText(Path.Combine(dir, "report.txt"), sb.ToString());
        }
    }
}
