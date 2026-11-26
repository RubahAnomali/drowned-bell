namespace DrownedBell.Core
{
    /// <summary>
    /// The movement and combat test room. Pillars of 4, 3, 2 and 1 tiles, shallow pits of 4, 6, 8 and 10 tiles
    /// (falling in is harmless: there is floor underneath), a target dummy that counts hits, and one Brine Crab.
    /// The game shows a live readout of each jump here; Metrics.Measure gives the same numbers headless.
    /// </summary>
    public static class TestRoom
    {
        public static readonly string[] Rows =
        {
            "################################################################",
            "#..............................................................#",
            "#..............................................................#",
            "#..............................................................#",
            "#..............................................................#",
            "#..............................................................#",
            "#..............................................................#",
            "#..............................................................#",
            "#..............................................................#",
            "#..............................................................#",
            "#..............................................................#",
            "#.##...........................................................#",
            "#.##.##........................................................#",
            "#.##.##.##.....................................................#",
            "#.##.##.##.##.P.....................................T.....c....#",
            "################....##......##........##..........##############",
            "################....##......##........##..........##############",
            "################################################################",
        };

        public static readonly (string Name, string Purpose, int Col, int Row, int W, int H)[] Rooms =
        {
            ("Test Room", "Measure jump and dash reach; practise on the target and one crab", 0, 0, 64, 18),
        };

        public static Level Build() => Level.Parse(Rows, Rooms);
    }

    /// <summary>The controller's real reach, measured by running Player.Update on flat ground.</summary>
    public struct MovementMetrics
    {
        public float FullJumpHeight, ShortHopHeight, Airtime;
        public float RunningJumpReach, StandingJumpReach, DashReach, DashJumpReach;

        public override string ToString() =>
            "full jump rise " + FullJumpHeight.ToString("0.00") + " tiles, short hop " + ShortHopHeight.ToString("0.00") +
            ", airtime " + Airtime.ToString("0.00") + "s, running jump reach " + RunningJumpReach.ToString("0.00") +
            ", standing jump " + StandingJumpReach.ToString("0.00") + ", ground dash " + DashReach.ToString("0.00") +
            ", running jump + dash " + DashJumpReach.ToString("0.00");
    }

    public static class Metrics
    {
        static Level Flat()
        {
            var rows = new string[8];
            for (int i = 0; i < 7; i++) rows[i] = "#" + new string('.', 198) + "#";
            rows[7] = new string('#', 200);
            return Level.Parse(rows, new[] { ("flat", "metrics", 0, 0, 200, 8) });
        }

        /// <summary>
        /// Takeoff from the ground and landing back at the same height. `hold` is how long jump is held,
        /// `running` starts at full run speed, `dashAt` presses dash that many seconds after takeoff (or -1).
        /// Returns horizontal distance, peak rise and airtime.
        /// </summary>
        static (float dist, float rise, float air) Jump(MovementTuning m, CombatTuning c, float hold, bool running, float dashAt)
        {
            Level lv = Flat();
            var p = new Player(new Vec2(20.5f, 1f), m, 5);
            p.Update(World.FixedDt, default, lv, m, c);
            if (running) for (int i = 0; i < 30; i++) p.Update(World.FixedDt, new PlayerInput { MoveX = 1 }, lv, m, c);
            float x0 = p.Body.Pos.X, y0 = p.Body.Bottom, peak = y0, t = 0f;
            bool air = false;
            for (int i = 0; i < 600; i++)
            {
                var input = new PlayerInput { MoveX = running || dashAt >= 0f ? 1 : 0, JumpPressed = i == 0, JumpHeld = t < hold };
                if (dashAt >= 0f && System.MathF.Abs(t - dashAt) < 0.5f * World.FixedDt) input.DashPressed = true;
                if (!running && dashAt < 0f) input.MoveX = 1;
                p.Update(World.FixedDt, input, lv, m, c);
                t += World.FixedDt;
                peak = System.MathF.Max(peak, p.Body.Bottom);
                if (!p.Body.Grounded) air = true;
                else if (air) break;
            }
            return (p.Body.Pos.X - x0, peak - y0, t);
        }

        public static MovementMetrics Measure(MovementTuning m = null, CombatTuning c = null)
        {
            m ??= new MovementTuning();
            c ??= new CombatTuning();
            var full = Jump(m, c, 9f, true, -1f);
            var hop = Jump(m, c, 0.02f, false, -1f);
            var stand = Jump(m, c, 9f, false, -1f);
            var dashJump = Jump(m, c, 9f, true, 0.3f);

            Level lv = Flat();
            var d = new Player(new Vec2(20.5f, 1f), m, 5);
            d.Update(World.FixedDt, default, lv, m, c);
            float x0 = d.Body.Pos.X;
            for (int i = 0; i < 30; i++) d.Update(World.FixedDt, new PlayerInput { DashPressed = i == 0 }, lv, m, c);
            return new MovementMetrics
            {
                FullJumpHeight = full.rise,
                ShortHopHeight = hop.rise,
                Airtime = full.air,
                RunningJumpReach = full.dist,
                StandingJumpReach = stand.dist,
                DashReach = d.Body.Pos.X - x0,
                DashJumpReach = dashJump.dist,
            };
        }
    }
}
