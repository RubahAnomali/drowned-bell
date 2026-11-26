using System;
using System.Collections;
using System.IO;
using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// Verification aid, not part of normal play. Launch the build with "-autoshot &lt;folder&gt;" and it tours every
    /// room, waits for each enemy and boss tell, saves a screenshot of each moment, then quits. Without the flag
    /// this component does nothing.
    /// </summary>
    public sealed class AutoTour : MonoBehaviour
    {
        GameController game;
        string dir;

        public static void AttachIfRequested(GameController game)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-autoshot");
            if (i < 0 || i + 1 >= args.Length) return;
            SaveStore.Enabled = false; // QA never reads or writes the player's save
            Application.runInBackground = true;
            var tour = game.gameObject.AddComponent<AutoTour>();
            tour.game = game;
            tour.dir = args[i + 1];
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            if (game.CurrentWorld.ArenaRoom < 0)
            {
                game.BeginForTour();
                World testRoom = game.CurrentWorld;
                foreach (Enemy foe in testRoom.Foes)
                {
                    if (!(foe is Dummy)) continue;
                    Put(testRoom, foe.Body.Pos.X - 1.2f, foe.Body.Feet.Y);
                    yield return Hold(0.6f);
                    game.TourSwing();
                    yield return Hold(0.6f);
                    File.WriteAllText(Path.Combine(dir, "test-room.txt"),
                        Metrics.Measure(testRoom.Move, testRoom.Combat) + "\ntarget hits: " + ((Dummy)foe).Hits);
                    break;
                }
                yield return Shot("test_room");
                yield return new WaitForSecondsRealtime(0.25f);
                Application.Quit();
                yield break;
            }
            yield return new WaitForSeconds(1f);
            yield return Shot("00_title");
            game.BeginForTour();
            World w = game.CurrentWorld;
            Level lv = w.Level;

            yield return Hold(1.2f);
            yield return Shot("01_landing_start");
            Put(w, lv.Checkpoint.X + 1.5f, lv.Checkpoint.Y);
            yield return Hold(1.0f);
            yield return Shot("02_landing_lamp_lit");

            Crab crab = null;
            foreach (Enemy e in w.Foes) if (e is Crab c && lv.Rooms[c.Room].Name == "Flooded Nave") crab = c;
            Put(w, crab.Body.Pos.X - 2.3f, crab.Body.Pos.Y);
            yield return Until(() => crab.State == CrabState.Windup && crab.WindupProgress > 0.6f, 4f);
            yield return Shot("03_nave_crab_windup");
            yield return Until(() => crab.State == CrabState.Lunge, 2f);
            yield return Shot("04_nave_crab_lunge");

            RippleBell ripple = w.RippleBells[0];
            Put(w, ripple.Pos.X - 4f, ripple.Floor);
            yield return Until(() => ripple.Windup > 0.7f, 4f);
            yield return Shot("04a_ripple_windup");
            yield return Until(() => ripple.Waves.Count > 0, 2f);
            yield return Hold(0.25f);
            yield return Shot("04b_ripple_wave");

            RoomInfo shaft = lv.Rooms.Find(r => r.Name == "Gutter Shaft");
            Put(w, shaft.MinX + 4f, shaft.MaxY - 14f);
            yield return Hold(0.8f);
            yield return Shot("05_shaft_top");
            Jelly jelly = null;
            foreach (Enemy e in w.Foes) if (e is Jelly j && lv.Rooms[j.Room].Name == "Gutter Shaft") jelly = j;
            Put(w, shaft.MinX + 8f, shaft.MinY + 10f);
            yield return Until(() => jelly.State == JellyState.Windup && jelly.WindupProgress > 0.7f, 5f);
            yield return Shot("06_shaft_jelly_windup");
            yield return Until(() => jelly.State == JellyState.Pulse && jelly.RingRadius > 1.5f, 2f);
            yield return Shot("07_shaft_jelly_pulse");
            Put(w, shaft.MinX + 12.5f, shaft.MinY + 12f);
            yield return Hold(0.6f);
            yield return Shot("08_shaft_spike_floor");

            RoomInfo gallery = lv.Rooms.Find(r => r.Name == "Choir Gallery");
            Put(w, gallery.MaxX - 8f, gallery.MinY + 2f);
            yield return Hold(1.0f);
            yield return Shot("09_gallery");
            Put(w, lv.Heart.X + 2f, lv.Heart.Y);
            yield return Hold(0.6f);
            yield return Shot("10_gallery_brass_lung");

            Put(w, lv.Lever.X - 1.2f, lv.Lever.Y);
            yield return Hold(0.8f);
            yield return Shot("11_ropes_winch");
            game.TourSwing();
            yield return Hold(1.2f);
            yield return Shot("12_ropes_grate_open");
            Put(w, 9.5f, lv.Lever.Y);
            yield return Hold(0.6f);
            yield return Shot("13_ropes_boss_drop");

            RoomInfo arena = lv.Rooms[w.ArenaRoom];
            Put(w, arena.MinX + 8f, arena.MinY + 4f);
            yield return Until(() => w.Boss.State == BossState.Idle, 5f);
            yield return Shot("14_boss_awake");
            yield return Until(() => w.Boss.State == BossState.SlamWindup && w.Boss.WindupProgress > 0.6f, 6f);
            yield return Shot("15_boss_slam_windup");
            yield return Until(() => w.Boss.Waves.Count > 0, 3f);
            yield return Hold(0.12f);
            yield return Shot("16_boss_shockwaves");
            Put(w, arena.MinX + 3f, arena.MinY + 5.5f);
            yield return Until(() => w.Boss.State == BossState.RushWindup && w.Boss.WindupProgress > 0.5f, 8f);
            yield return Shot("17_boss_rush_windup");
            w.Boss.Health = w.Boss.MaxHealth / 2 + 1;
            w.Boss.TakeHit(w, 900001, 1);
            yield return Until(() => w.Boss.State == BossState.RainWindup && w.Boss.WindupProgress > 0.4f, 25f);
            yield return Shot("18_boss_rain_marks");
            game.TourDebug(true);
            yield return Hold(0.4f);
            yield return Shot("19_inspector_f1");
            game.TourDebug(false);
            w.Boss.TakeHit(w, 900002, 999);
            yield return Until(() => w.Phase == Phase.Victory, 6f);
            yield return Hold(0.6f);
            yield return Shot("20_victory");
            Application.Quit();
        }

        void Put(World w, float x, float y)
        {
            float floor = w.Level.FloorBelow(x, y + 0.5f);
            w.Player.Place(new Vec2(x, floor < 0f ? y : floor), w.Move);
        }

        IEnumerator Hold(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                game.CurrentWorld.Player.Invuln = 99f;
                t += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator Until(Func<bool> cond, float timeout)
        {
            float t = 0f;
            while (!cond() && t < timeout)
            {
                game.CurrentWorld.Player.Invuln = 99f;
                t += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return null;
            yield return null;
        }
    }
}
