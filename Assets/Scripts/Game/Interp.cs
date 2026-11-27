using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// The simulation steps at a fixed 60 Hz while the screen draws at its own rate (60, 120, 144 Hz...). Drawing the latest
    /// step each frame makes motion judder, because some steps last two frames and some one. Instead every view draws the
    /// point Alpha of the way between the previous step and the latest one (Alpha = the part of a step already elapsed).
    /// Game rules never read this: it only changes where things are drawn.
    /// </summary>
    static class Interp
    {
        public static float Alpha = 1f;

        public static Vector2 Of(Body b)
        {
            Vec2 v = b.Lerp(Alpha);
            return new Vector2(v.X, v.Y);
        }
    }
}
