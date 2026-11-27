using System;
using System.Collections.Generic;
using System.Linq;
using DrownedBell.Core;

/// <summary>
/// Movement smoothness. The simulation steps at 60 Hz while the screen draws at 144 Hz, 75 Hz or 60 Hz with jittery
/// frame times. Drawing the latest step each frame makes some frames repeat a position and others jump two steps;
/// drawing between the last two steps (Body.Lerp) moves the same distance every frame. This replays a run the way the
/// game loop does and measures how even the drawn motion is.
/// </summary>
internal static class SmoothnessChecks
{
    const float Dt = World.FixedDt;

    /// <summary>Per-frame distance moved by the drawn diver while running steadily, for one display rate.</summary>
    static List<float> Frames(double displayHz, bool blend, int jitterSeed)
    {
        var w = new World(level: SoundingArchive.Build());
        w.Player.Place(new Vec2(4f, 2f), w.Move);
        var rng = new Random(jitterSeed);
        double frame = 1.0 / displayHz, acc = 0.0;
        var drawn = new List<float>();
        for (int i = 0; i < (int)(displayHz * 3); i++)
        {
            // A real frame time wobbles a little around the refresh interval.
            double dt = jitterSeed == 0 ? frame : frame * (0.85 + 0.3 * rng.NextDouble());
            acc += dt;
            while (acc >= Dt)
            {
                w.Update(Dt, new PlayerInput { MoveX = 1 });
                acc -= Dt;
            }
            float x = blend ? w.Player.Body.Lerp((float)(acc / Dt)).X : w.Player.Body.Pos.X;
            drawn.Add(x);
        }
        // Skip the first second (acceleration) and look at the steady run.
        var deltas = new List<float>();
        for (int i = (int)displayHz; i < drawn.Count; i++) deltas.Add(drawn[i] - drawn[i - 1]);
        return deltas;
    }

    static float Unevenness(List<float> d) => d.Max() - d.Min();

    public static void Run(Action<bool, string> ok)
    {
        Console.WriteLine("\nSmoothness");
        var w = new World();
        w.Player.Place(new Vec2(5f, 2f), w.Move);
        Vec2 before = w.Player.Body.Pos;
        w.Update(Dt, new PlayerInput { MoveX = 1 });
        Vec2 after = w.Player.Body.Pos;
        ok(MathF.Abs(w.Player.Body.Prev.X - before.X) < 1e-6f && MathF.Abs(w.Player.Body.Lerp(0f).X - before.X) < 1e-6f &&
           MathF.Abs(w.Player.Body.Lerp(1f).X - after.X) < 1e-6f && MathF.Abs(w.Player.Body.Lerp(0.5f).X - (before.X + after.X) * 0.5f) < 1e-5f,
            "Prev holds the last step start, and Lerp runs from it to the new position");
        w.Player.Place(new Vec2(40f, 2f), w.Move);
        ok(w.Player.Body.Lerp(0.5f).X == w.Player.Body.Pos.X, "a teleport does not streak the diver across the room");
        w.Event += _ => { };
        var crab = w.Foes.OfType<Crab>().FirstOrDefault();
        if (crab != null)
        {
            crab.TakeHit(w, 1, 99, 0f);
            crab.Reset();
            ok(crab.Body.Lerp(0.5f).X == crab.Home.X, "a reset enemy appears at home, not sliding there");
        }

        foreach (double hz in new[] { 144.0, 75.0, 60.0 })
        {
            var naive = Frames(hz, blend: false, jitterSeed: 0);
            var blended = Frames(hz, blend: true, jitterSeed: 0);
            ok(Unevenness(blended) < 0.01f * 8.2f,
                hz.ToString("0") + " Hz: blended motion is even: " + Unevenness(blended).ToString("0.0000") + " tiles of spread per frame (naive: " + Unevenness(naive).ToString("0.0000") + ")");
            var naiveJitter = Frames(hz, blend: false, jitterSeed: 7);
            var blendedJitter = Frames(hz, blend: true, jitterSeed: 7);
            ok(Unevenness(blendedJitter) < Unevenness(naiveJitter) || Unevenness(naiveJitter) < 1e-4f,
                hz.ToString("0") + " Hz with +-15% frame-time jitter: blending is smoother than drawing the last step (" +
                Unevenness(blendedJitter).ToString("0.0000") + " vs " + Unevenness(naiveJitter).ToString("0.0000") + ")");
            // Moving forward never steps back.
            ok(blendedJitter.All(d => d >= -1e-5f), hz.ToString("0") + " Hz: the drawn diver never steps backwards");
        }
    }
}
