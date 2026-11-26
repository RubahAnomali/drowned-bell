using System;
using System.Collections.Generic;

namespace DrownedBell.Core
{
    /// <summary>
    /// QA autopilot. Plays the game only through PlayerInput, the same way the keyboard does: it walks the
    /// recorded NavGraph moves, fights by watching each enemy's tell, and runs the scripted QA scenario:
    /// light the lamp, take the Brass Lung, open the winch, climb the shortcut, fight the bell, optionally die
    /// on purpose N times to measure boss retries, then win. It never moves the diver or changes the world.
    /// </summary>
    public sealed class Autopilot
    {
        public enum Goal { Lamp, Lung, Winch, ShortcutUp, Arena, Fight, Done }

        readonly World w;
        readonly NavGraph closedG, openG;
        public Goal Current { get; private set; } = Goal.Lamp;
        public int RetriesWanted;
        public int RetriesDone { get; private set; }
        public readonly List<string> Log = new List<string>();
        public readonly List<float> RetryTimes = new List<float>();
        public readonly Dictionary<Goal, float> ReachedAt = new Dictionary<Goal, float>();
        public bool ShortcutClimbed { get; private set; }
        public float VictoryAt = -1f;
        public int NoPathEvents;
        /// <summary>Recorded moves that landed somewhere other than expected (the pilot then replans).</summary>
        public int Misses;
        public bool Verbose;

        List<NavEdge> plan;
        int planIdx, planFrom = -1, scriptTick = -1;
        bool leftGround, settling;
        int settleTicks;
        float respawnAt = -1f;
        bool clearingRipple;
        readonly float arenaFloor, ledgeLeftX, ledgeRightX, arenaMinX, arenaMaxX;
        readonly int lampRoom;

        public Autopilot(World w, NavGraph closed, NavGraph open, int retries)
        {
            this.w = w;
            closedG = closed;
            openG = open;
            RetriesWanted = retries;
            w.Event += OnEvent;
            RoomInfo arena = w.Level.Rooms[w.ArenaRoom];
            arenaMinX = arena.MinX;
            arenaMaxX = arena.MaxX;
            arenaFloor = w.Level.FloorBelow(w.Level.BossSpawn.X, w.Level.BossSpawn.Y + 0.5f);
            // The arena's one-way ledges: one cluster on each side.
            float lSum = 0, rSum = 0;
            int lN = 0, rN = 0;
            for (int x = (int)arena.MinX; x < (int)arena.MaxX; x++)
                for (int y = (int)arena.MinY; y < (int)arena.MaxY; y++)
                    if (w.Level.OneWay(x, y))
                    {
                        if (x < (arena.MinX + arena.MaxX) * 0.5f) { lSum += x + 0.5f; lN++; }
                        else { rSum += x + 0.5f; rN++; }
                    }
            ledgeLeftX = lN > 0 ? lSum / lN : arena.MinX + 3f;
            ledgeRightX = rN > 0 ? rSum / rN : arena.MaxX - 3f;
            lampRoom = w.Level.RoomIndexAt(new Vec2(w.Level.Checkpoint.X, w.Level.Checkpoint.Y + 0.5f));
        }

        NavGraph G => w.Level.GrateOpen ? openG : closedG;

        void Note(string s) { Log.Add(FormatTime(w.Time) + "  " + s); }
        static string FormatTime(float t) => ((int)t / 60) + ":" + (t % 60f).ToString("00.0");

        void Reach(Goal g)
        {
            if (!ReachedAt.ContainsKey(g)) ReachedAt[g] = w.Time;
        }

        void OnEvent(GameEvent e)
        {
            switch (e.Type)
            {
                case GameEventType.LampLit: Note("tide lamp lit"); break;
                case GameEventType.LungTaken: Note("Brass Lung taken"); break;
                case GameEventType.WinchPulled: Note("winch hit, grate open"); break;
                case GameEventType.EnemyKilled: Note("enemy " + e.Id + " killed"); break;
                case GameEventType.PlayerHurt: Note("hurt, " + e.Id + " masks left"); break;
                case GameEventType.HazardHit: Note("spikes, " + e.Id + " masks left"); break;
                case GameEventType.BossAwake: Note("the Undertoll wakes"); break;
                case GameEventType.BossPhase2: Note("boss phase two"); break;
                case GameEventType.PlayerDied:
                    Note("died (death " + e.Id + ")");
                    ResetNav();
                    break;
                case GameEventType.Respawned:
                    Note("respawned at the lamp");
                    respawnAt = w.Time;
                    ResetNav();
                    if (Current == Goal.Fight) { RetriesDone++; Current = Goal.Arena; }
                    break;
                case GameEventType.BossDefeated: Note("the Undertoll falls silent"); break;
                case GameEventType.Victory:
                    VictoryAt = w.Time;
                    Current = Goal.Done;
                    Note("VICTORY screen");
                    break;
            }
        }

        public void StartAt(Goal goal)
        {
            Current = goal;
            ResetNav();
        }

        void ResetNav()
        {
            plan = null;
            scriptTick = -1;
            settling = false;
            clearingRipple = false;
        }

        // ---- goals ----------------------------------------------------------------------------

        bool GoalNode(int n)
        {
            NavGraph g = G;
            var (x, y) = g.Nodes[n];
            switch (Current)
            {
                case Goal.Lamp: return MathF.Abs(x + 0.5f - w.Level.Checkpoint.X) < 0.6f && MathF.Abs(y - w.Level.Checkpoint.Y) < 0.6f;
                case Goal.Lung: return MathF.Abs(x + 0.5f - w.Level.Heart.X) < 0.6f && MathF.Abs(y - w.Level.Heart.Y) < 0.6f;
                case Goal.Winch: return MathF.Abs(x + 0.5f - (w.Level.Lever.X - 1.5f)) < 0.6f && MathF.Abs(y - w.Level.Lever.Y) < 0.6f;
                case Goal.ShortcutUp: return g.RoomOf(n) == lampRoom && MathF.Abs(x + 0.5f - w.Level.Checkpoint.X) < 0.6f;
                case Goal.Arena: return g.RoomOf(n) == w.ArenaRoom;
                default: return false;
            }
        }

        void Advance()
        {
            Goal before = Current;
            if (Current == Goal.Lamp && w.LampLit) Current = Goal.Lung;
            if (Current == Goal.Lung && w.LungTaken) Current = Goal.Winch;
            if (Current == Goal.Winch && w.GrateOpen) Current = Goal.ShortcutUp;
            if (Current == Goal.ShortcutUp && w.Room == lampRoom && w.Player.Body.Grounded && MathF.Abs(w.Player.Body.Pos.X - w.Level.Checkpoint.X) < 1.5f)
            {
                ShortcutClimbed = true;
                Note("climbed the shortcut back to the lamp");
                Current = Goal.Arena;
            }
            if (Current == Goal.Arena && w.Room == w.ArenaRoom)
            {
                if (respawnAt >= 0f && RetryTimes.Count < RetriesDone)
                {
                    RetryTimes.Add(w.Time - respawnAt);
                    Note("back in the arena " + (w.Time - respawnAt).ToString("0.0") + "s after respawning");
                }
                Current = Goal.Fight;
            }
            if (Current != before)
            {
                Reach(before);
                ResetNav();
            }
        }

        // ---- main ---------------------------------------------------------------------------------

        public PlayerInput Next()
        {
            if (w.Phase != Phase.Playing || Current == Goal.Done) { ResetNav(); return default; }
            Advance();
            if (Current != Goal.Fight && AvoidRipple(out PlayerInput dodge)) return dodge;
            if (scriptTick < 0 && Combat(out PlayerInput fight)) return fight;
            if (Current == Goal.Fight) return BossFight();
            if (Current == Goal.Winch && GoalNodeHere()) return HitWinch();
            return Navigate();
        }

        bool AvoidRipple(out PlayerInput input)
        {
            input = default;
            Player p = w.Player;
            if (clearingRipple && !p.Body.Grounded)
            {
                input.JumpHeld = true;
                return true;
            }
            clearingRipple = false;
            if (scriptTick >= 0 || !p.Body.Grounded) return false;
            foreach (RippleBell bell in w.RippleBells)
            {
                if (bell.Room != w.Room || MathF.Abs(p.Body.Bottom - bell.Floor) > 0.1f) continue;
                foreach (Shockwave wave in bell.Waves)
                {
                    if (!WaveApproaching(wave, p, 2f)) continue;
                    ResetNav();
                    clearingRipple = true;
                    input.JumpPressed = input.JumpHeld = true;
                    return true;
                }
                if (bell.Windup > 0.65f && MathF.Abs(p.Body.Pos.X - bell.Pos.X) < 2.5f) return true;
            }
            return false;
        }

        static bool WaveApproaching(Shockwave wave, Player p, float distance)
        {
            float ahead = (p.Body.Pos.X - wave.X) * wave.Dir;
            return ahead >= 0f && ahead < distance;
        }

        bool GoalNodeHere()
        {
            if (!w.Player.Body.Grounded) return false;
            int n = G.NodeAt(w.Player.Body.Feet, 0);
            return n >= 0 && GoalNode(n);
        }

        PlayerInput HitWinch()
        {
            Player p = w.Player;
            float target = w.Level.Lever.X - 1.3f;
            float dx = target - p.Body.Pos.X;
            if (MathF.Abs(dx) > 0.15f) return new PlayerInput { MoveX = Mathx.Clamp(dx * 3f, -1f, 1f) };
            return new PlayerInput { MoveX = 0.06f, AttackPressed = !p.Attacking };
        }

        PlayerInput Navigate()
        {
            NavGraph g = G;
            Body b = w.Player.Body;
            if (scriptTick >= 0)
            {
                NavEdge cur = plan[planIdx];
                if (!b.Grounded) leftGround = true;
                if (!settling && leftGround && b.Grounded && b.Vel.Y <= 0f && scriptTick > 3) settling = true;
                if (settling)
                {
                    // Let the diver come to rest with no input, exactly as the move was recorded.
                    settleTicks++;
                    if (NavGraph.Settled(w.Player) || settleTicks > NavGraph.SettleTicks)
                    {
                        scriptTick = -1;
                        settling = false;
                        int landed = g.NodeAt(b.Feet, 0);
                        if (landed == cur.To) { planFrom = landed; planIdx++; }
                        else
                        {
                            Misses++;
                            if (Misses <= 12)
                                Note("move missed: from " + g.Nodes[planFrom] + " expected " + g.Nodes[cur.To] + " landed " + (landed >= 0 ? g.Nodes[landed].ToString() : "off-graph") +
                                     " (dir " + cur.Script.Dir + " hold " + cur.Script.Hold + " dash " + cur.Script.Dash + (cur.Script.Drop ? " drop" : "") + ")");
                            plan = null;
                        }
                    }
                    return default;
                }
                if (scriptTick > NavGraph.MaxTicks) { scriptTick = -1; plan = null; return default; }
                return NavGraph.Input(cur.Script, scriptTick++);
            }
            if (!b.Grounded) return default;
            int node = g.NodeAt(b.Feet, 1);
            if (node < 0)
            {
                // Off the graph (e.g. knocked onto an odd spot): step toward the room's middle.
                RoomInfo r = w.Level.Rooms[Math.Max(0, w.Room)];
                return new PlayerInput { MoveX = b.Pos.X < (r.MinX + r.MaxX) * 0.5f ? 0.5f : -0.5f };
            }
            if (plan == null || planIdx >= plan.Count || planFrom != node)
            {
                if (GoalNode(node)) return default;
                plan = g.Path(node, GoalNode);
                if (Verbose) Note("plan from " + g.Nodes[node] + " to " + Current + ": " + (plan == null ? "none" : string.Join(" ; ", plan.ConvertAll(x => (x.Walk ? "walk " : "jump d" + x.Script.Dir + " h" + x.Script.Hold + " s" + x.Script.Dash + (x.Script.Drop ? " drop" : "") + " ") + "->" + g.Nodes[x.To]))));
                planIdx = 0;
                planFrom = node;
                if (plan == null)
                {
                    NoPathEvents++;
                    if (NoPathEvents % 120 == 1) Note("no path to " + Current + " from " + g.Nodes[node]);
                    return default;
                }
            }
            NavEdge e = plan[planIdx];
            if (e.Walk)
            {
                float tx = g.Feet(e.To).X;
                if (MathF.Abs(tx - b.Pos.X) < 0.25f) { planFrom = e.To; planIdx++; return default; }
                return new PlayerInput { MoveX = Math.Sign(tx - b.Pos.X) };
            }
            // Line up exactly on the spot the move was recorded from, standing still, then replay it.
            float cx = g.Feet(node).X;
            float dx = cx - b.Pos.X;
            // A recorded move only replays exactly from the same state: dash ready, no swing, not dropping through.
            Player pl = w.Player;
            bool ready = pl.DashCooldown <= 0f && !pl.Attacking && !pl.Dashing && b.DropThrough <= 0f && pl.Stun <= 0f;
            if (MathF.Abs(dx) > 0.03f || MathF.Abs(b.Vel.X) > 0.4f || !ready)
            {
                return new PlayerInput { MoveX = MathF.Abs(dx) <= 0.03f ? 0f : Mathx.Clamp(dx * 4f, -1f, 1f) };
            }
            if (Verbose) Note("move " + g.Nodes[node] + " -> " + g.Nodes[e.To] + " dir " + e.Script.Dir + " hold " + e.Script.Hold + " dash " + e.Script.Dash + (e.Script.Drop ? " drop" : "") + " plan " + planIdx + "/" + plan.Count);
            scriptTick = 0;
            leftGround = false;
            settling = false;
            settleTicks = 0;
            return Navigate();
        }

        // ---- ordinary enemies -----------------------------------------------------------------

        bool SafeStep(int dir)
        {
            if (dir == 0) return true;
            Body b = w.Player.Body;
            float x = b.Pos.X + dir * (b.Half.X + 0.4f);
            int cx = (int)MathF.Floor(x), fy = (int)MathF.Floor(b.Bottom + 0.05f);
            bool floor = w.Level.Solid(cx, fy - 1) || w.Level.OneWay(cx, fy - 1);
            return floor && !w.Level.Spiky(cx, fy) && !w.Level.Spiky(cx, fy - 1);
        }

        PlayerInput Move(float x)
        {
            int dir = Math.Sign(x);
            return new PlayerInput { MoveX = SafeStep(dir) ? x : 0f };
        }

        bool Combat(out PlayerInput input)
        {
            input = default;
            if (Current == Goal.Fight) return false;
            Player p = w.Player;
            if (!p.Body.Grounded) return false;
            Vec2 pp = p.Body.Pos;
            Enemy target = null;
            float best = float.PositiveInfinity;
            foreach (Enemy e in w.Foes)
            {
                if (!e.Alive || e.Room != w.Room) continue;
                float d = Vec2.Distance(e.Body.Pos, pp);
                bool relevant = e is Crab ? MathF.Abs(e.Body.Pos.Y - pp.Y) < 1.5f && MathF.Abs(e.Body.Pos.X - pp.X) < 6.5f : d < 6f;
                if (relevant && d < best) { best = d; target = e; }
            }
            if (target == null) return false;
            ResetNav();
            input = target is Crab c ? FightCrab(c) : FightJelly((Jelly)target);
            return true;
        }

        PlayerInput FightCrab(Crab c)
        {
            Player p = w.Player;
            float dx = c.Body.Pos.X - p.Body.Pos.X;
            int dir = Math.Sign(dx);
            switch (c.State)
            {
                case CrabState.Windup:
                case CrabState.Lunge:
                    return Move(-dir);
                case CrabState.Recover:
                    if (MathF.Abs(dx) > 1.9f) return Move(dir);
                    return new PlayerInput { MoveX = dir * 0.06f, AttackPressed = !p.Attacking };
                default:
                    if (MathF.Abs(dx) > 2.9f) return Move(dir * 0.5f);
                    if (MathF.Abs(dx) < 2.0f) return Move(-dir);
                    return new PlayerInput { MoveX = dir * 0.06f };
            }
        }

        PlayerInput FightJelly(Jelly j)
        {
            Player p = w.Player;
            Vec2 d = j.Body.Pos - p.Body.Pos;
            float dist = d.Length;
            int dir = Math.Sign(d.X == 0f ? 1f : d.X);
            switch (j.State)
            {
                case JellyState.Windup:
                case JellyState.Pulse:
                    return dist < w.Enemies.JellyPulseRadius + 0.7f ? Move(-dir) : default;
                case JellyState.Spent:
                    if (MathF.Abs(d.X) > 0.6f && !(MathF.Abs(d.Y) < 0.8f && MathF.Abs(d.X) < 2f)) return Move(dir);
                    if (d.Y > 0.4f) return new PlayerInput { Up = true, AttackPressed = !p.Attacking };
                    return new PlayerInput { MoveX = dir * 0.06f, AttackPressed = !p.Attacking };
                default:
                    if (dist > 4.6f) return Move(dir * 0.5f);
                    if (dist < 3.8f) return Move(-dir);
                    return default;
            }
        }

        // ---- the boss -------------------------------------------------------------------------

        PlayerInput BossFight()
        {
            Player p = w.Player;
            Boss b = w.Boss;
            if (w.Room != w.ArenaRoom) { Current = Goal.Arena; return Navigate(); }
            if (!b.Awake || b.Dead) return default;
            // Retry scenario: stand still and let the bell win, to measure the way back.
            if (RetriesDone < RetriesWanted) return default;

            float px = p.Body.Pos.X, bx = b.Body.Pos.X, dx = bx - px;
            int toward = Math.Sign(dx == 0f ? 1f : dx);
            bool onLedge = p.Body.Bottom > arenaFloor + 2.5f;
            var input = new PlayerInput();
            if (!p.Body.Grounded) input.JumpHeld = true;

            // Shockwaves: jump them as they arrive. Only waves still heading for the diver matter.
            bool incoming = false;
            foreach (Shockwave s in b.Waves)
            {
                float rel = (px - s.X) * s.Dir;
                if (rel > 0f) incoming = true;
                if (WaveApproaching(s, p, 2f) && p.Body.Grounded && !onLedge) { input.JumpPressed = true; input.JumpHeld = true; }
            }

            BossState st = b.State;
            bool recovering = st == BossState.SlamRecover || st == BossState.RushRecover || st == BossState.RainRecover;
            float nearWall = MathF.Min(px - arenaMinX, arenaMaxX - px);

            if (st == BossState.RushWindup || st == BossState.Rush)
            {
                // The rush sweeps the floor: get onto the nearest side ledge.
                if (onLedge) { input.MoveX = 0f; return input; }
                float ledge = MathF.Abs(px - ledgeLeftX) < MathF.Abs(px - ledgeRightX) ? ledgeLeftX : ledgeRightX;
                float to = ledge - px;
                input.MoveX = Math.Sign(to);
                if (MathF.Abs(to) > 5f && p.DashCooldown <= 0f) input.DashPressed = true;
                if (MathF.Abs(to) < 2.2f && p.Body.Grounded) { input.JumpPressed = true; input.JumpHeld = true; }
                return input;
            }
            if (st == BossState.SlamWindup || st == BossState.SlamFall)
            {
                if (onLedge) return new PlayerInput { Down = true, JumpPressed = true };
                float clearance = b.Body.Bottom - arenaFloor;
                bool cornered = nearWall < 4.5f && MathF.Abs(dx) < 5f && Math.Sign(px - (arenaMinX + arenaMaxX) * 0.5f) == -toward;
                if (cornered)
                {
                    // Run under the rising bell to the open side once there is head room.
                    if (st == BossState.SlamWindup && clearance > 1.3f) { input.MoveX = toward; if (p.DashCooldown <= 0f) input.DashPressed = true; }
                    return input;
                }
                if (MathF.Abs(dx) < 4.5f) input.MoveX = -toward;
                return input;
            }
            if (st == BossState.RainWindup || st == BossState.Rain)
            {
                float safe = SafeFromDrips(px);
                if (MathF.Abs(safe - px) > 0.2f) input.MoveX = Math.Sign(safe - px);
                return input;
            }
            if (recovering && !incoming)
            {
                if (onLedge) return new PlayerInput { Down = true, JumpPressed = true };
                if (MathF.Abs(dx) > 2.3f) { input.MoveX = toward; return input; }
                input.MoveX = toward * 0.06f;
                input.AttackPressed = !p.Attacking;
                return input;
            }
            // Idle, intro, or waiting for waves to pass: keep a respectful distance.
            if (onLedge && st == BossState.Idle) return new PlayerInput { Down = true, JumpPressed = true };
            if (MathF.Abs(dx) < 3.6f) input.MoveX = -toward;
            else if (MathF.Abs(dx) > 5.5f) input.MoveX = toward;
            return input;
        }

        float SafeFromDrips(float px)
        {
            float best = px, bestCost = float.PositiveInfinity;
            for (float off = -6f; off <= 6f; off += 0.25f)
            {
                float x = px + off;
                if (x < arenaMinX + 1.5f || x > arenaMaxX - 1.5f) continue;
                // Never pick a gap that means walking into or through the bell.
                float bx = w.Boss.Body.Pos.X, keep = w.Boss.Body.Half.X + 1.0f;
                if (MathF.Abs(x - bx) < keep || (x - bx) * (px - bx) < 0f) continue;
                float clear = float.PositiveInfinity;
                foreach (Drip d in w.Boss.Drips) clear = MathF.Min(clear, MathF.Abs(d.X - x));
                if (clear < 1.3f) continue;
                float cost = MathF.Abs(off);
                if (cost < bestCost) { bestCost = cost; best = x; }
            }
            return best;
        }
    }
}
