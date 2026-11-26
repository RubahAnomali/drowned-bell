using System;
using System.Collections.Generic;

namespace DrownedBell.Core
{
    /// <summary>One recorded way of getting from one standing spot to another: walk, or an exact input script.</summary>
    public struct NavScript
    {
        public int Dir;
        /// <summary>Seconds jump is held, or -1 for walking off without jumping.</summary>
        public float Hold;
        /// <summary>When to dash (seconds after the start), or -1 for no dash.</summary>
        public float Dash;
        /// <summary>Delay horizontal movement to land on narrow, nearby ledges.</summary>
        public float MoveAfter;
        public bool Drop;
    }

    public sealed class NavEdge
    {
        public int To;
        public bool Walk;
        public NavScript Script;
        public float Time;
    }

    /// <summary>
    /// Every spot the diver can stand on, linked by the moves the real Player code can make between them. Built by
    /// running Player.Update from each spot with a set of input scripts (standing starts only, so any of them can
    /// be reproduced exactly). Used by the route checks and by the QA autopilot.
    /// </summary>
    public sealed class NavGraph
    {
        public readonly Level Level;
        readonly MovementTuning m;
        readonly CombatTuning c;
        public readonly List<(int X, int Y)> Nodes = new List<(int, int)>();
        readonly Dictionary<(int, int), int> index = new Dictionary<(int, int), int>();
        public readonly List<List<NavEdge>> Out = new List<List<NavEdge>>();
        public int Simulations;
        readonly bool allowDash;

        static readonly float[] Holds = { -1f, 0.06f, 0.14f, 0.24f, 2f };
        static readonly float[] Dashes = { -1f, 0.08f, 0.22f, 0.4f };

        public NavGraph(bool grateOpen, MovementTuning move = null, CombatTuning combat = null, Level level = null, bool allowDash = true)
        {
            m = move ?? new MovementTuning();
            c = combat ?? new CombatTuning();
            this.allowDash = allowDash;
            Level = level ?? Level.Build();
            Level.GrateOpen = grateOpen;
            Level.DoorsLocked = false;
            for (int y = 1; y < Level.H; y++)
                for (int x = 0; x < Level.W; x++)
                    if (Standable(x, y)) { index[(x, y)] = Nodes.Count; Nodes.Add((x, y)); }
            for (int i = 0; i < Nodes.Count; i++) Out.Add(new List<NavEdge>());
            for (int i = 0; i < Nodes.Count; i++) Explore(i);
        }

        bool Standable(int x, int y)
        {
            var p = new Player(new Vec2(x + 0.5f, y), m, 5);
            if (Level.OverlapsSolid(p.Body.Box) || Level.TouchesSpikes(p.Body.Box)) return false;
            return Physics.Supported(Level, p.Body, out _);
        }

        public int Find(int x, int y) => index.TryGetValue((x, y), out int i) ? i : -1;

        /// <summary>The standing spot under a pair of feet, searching a little sideways and downward.</summary>
        public int NodeAt(Vec2 feet, int maxDx = 1)
        {
            int fx = (int)MathF.Floor(feet.X), fy = (int)MathF.Round(feet.Y);
            for (int d = 0; d <= maxDx; d++)
                foreach (int x in d == 0 ? new[] { fx } : new[] { fx - d, fx + d })
                    for (int y = fy; y >= fy - 3; y--)
                        if (index.TryGetValue((x, y), out int i)) return i;
            return -1;
        }

        public Vec2 Feet(int node) => new Vec2(Nodes[node].X + 0.5f, Nodes[node].Y);
        public int RoomOf(int node) => Level.RoomIndexAt(new Vec2(Nodes[node].X + 0.5f, Nodes[node].Y + 0.5f));

        void Add(int from, NavEdge e)
        {
            foreach (NavEdge old in Out[from])
            {
                if (old.To != e.To) continue;
                if (e.Time < old.Time) { old.Walk = e.Walk; old.Script = e.Script; old.Time = e.Time; }
                return;
            }
            Out[from].Add(e);
        }

        void Explore(int from)
        {
            var (x, y) = Nodes[from];
            foreach (int dx in new[] { -1, 1 })
                if (index.TryGetValue((x + dx, y), out int n)) Add(from, new NavEdge { To = n, Walk = true, Time = 1f / m.RunSpeed });
            foreach (int dir in new[] { -1, 0, 1 })
                foreach (float hold in Holds)
                    foreach (float dash in Dashes)
                    {
                        if (dir == 0 && (hold < 0f || dash >= 0f)) continue;
                        if (!allowDash && dash >= 0f) continue;
                        Simulate(from, new NavScript { Dir = dir, Hold = hold, Dash = dash });
                    }
            if (Level.OneWay(x, y - 1))
                foreach (int dir in new[] { -1, 0, 1 })
                    Simulate(from, new NavScript { Dir = dir, Hold = -1f, Dash = -1f, Drop = true });
            foreach (int dir in new[] { -1, 1 })
                foreach (float delay in new[] { 0.22f, 0.30f })
                    Simulate(from, new NavScript { Dir = dir, Hold = 2f, Dash = -1f, MoveAfter = delay });
        }

        /// <summary>The input for tick i of a script.</summary>
        public static PlayerInput Input(NavScript s, int i)
        {
            float t = i * World.FixedDt;
            var input = new PlayerInput { MoveX = t >= s.MoveAfter ? s.Dir : 0 };
            if (s.Drop)
            {
                input.Down = i < 2;
                input.JumpPressed = i == 0;
            }
            else if (s.Hold >= 0f)
            {
                input.JumpPressed = i == 0;
                input.JumpHeld = t < s.Hold;
            }
            if (s.Dash >= 0f && MathF.Abs(t - s.Dash) < 0.5f * World.FixedDt) input.DashPressed = true;
            return input;
        }

        public const int MaxTicks = 180;

        /// <summary>
        /// Replay a script from a standing start. The move ends where the diver comes to rest after touching down
        /// with no further input (a landing that slides off a ledge is not a landing).
        /// </summary>
        void Simulate(int from, NavScript s)
        {
            Simulations++;
            var (x, y) = Nodes[from];
            var p = new Player(new Vec2(x + 0.5f, y), m, 5);
            p.Body.Grounded = Physics.Supported(Level, p.Body, out p.Body.OnOneWay); // standing exactly as physics would report it
            p.Facing = s.Dir == 0 ? 1 : s.Dir;
            bool left = false, settling = false;
            for (int i = 0; i < MaxTicks + SettleTicks; i++)
            {
                PlayerInput input = settling ? default : Input(s, i);
                p.Update(World.FixedDt, input, Level, m, c);
                if (Level.TouchesSpikes(p.Body.Box)) return;
                if (!p.Body.Grounded) { left = true; continue; }
                if (!settling && left && p.Body.Vel.Y <= 0f && i > 2) settling = true;
                if (settling && Settled(p))
                {
                    int lx = (int)MathF.Floor(p.Body.Pos.X), ly = (int)MathF.Round(p.Body.Bottom);
                    if (index.TryGetValue((lx, ly), out int n) && n != from) Add(from, new NavEdge { To = n, Script = s, Time = (i + 1) * World.FixedDt + 0.15f });
                    return;
                }
                if (!settling && i >= MaxTicks) return;
            }
        }

        public const int SettleTicks = 60;
        public static bool Settled(Player p) => p.Body.Grounded && MathF.Abs(p.Body.Vel.X) < 0.05f && !p.Dashing;

        /// <summary>Fastest path (list of edges) from one node to the first node that satisfies `goal`.</summary>
        public List<NavEdge> Path(int from, Func<int, bool> goal)
        {
            int n = Nodes.Count;
            var dist = new float[n];
            var prevEdge = new NavEdge[n];
            var prevNode = new int[n];
            var done = new bool[n];
            for (int i = 0; i < n; i++) { dist[i] = float.PositiveInfinity; prevNode[i] = -1; }
            dist[from] = 0f;
            int found = -1;
            for (;;)
            {
                int u = -1;
                float best = float.PositiveInfinity;
                for (int i = 0; i < n; i++) if (!done[i] && dist[i] < best) { best = dist[i]; u = i; }
                if (u < 0) break;
                if (goal(u)) { found = u; break; }
                done[u] = true;
                foreach (NavEdge e in Out[u])
                {
                    float nd = dist[u] + e.Time;
                    if (nd < dist[e.To]) { dist[e.To] = nd; prevEdge[e.To] = e; prevNode[e.To] = u; }
                }
            }
            if (found < 0) return null;
            var path = new List<NavEdge>();
            for (int v = found; v != from; v = prevNode[v]) path.Add(prevEdge[v]);
            path.Reverse();
            return path;
        }

        public HashSet<int> From(int start)
        {
            var seen = new HashSet<int> { start };
            var q = new Queue<int>();
            q.Enqueue(start);
            while (q.Count > 0)
            {
                int u = q.Dequeue();
                foreach (NavEdge e in Out[u]) if (seen.Add(e.To)) q.Enqueue(e.To);
            }
            return seen;
        }
    }
}
