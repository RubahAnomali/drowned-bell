using System;
using System.Collections.Generic;

namespace DrownedBell.Core
{
    /// <summary>
    /// A hanging bell ('W') that sends a floor ripple each way on a fixed beat after a visible wind-up. Archive echo
    /// drums ('O') ring the same ripple twice, the second pair EchoDelay later; the Receiving Desk drum ('o') does the
    /// same over a harmless floor, so the double jump can be watched before it can hurt.
    /// </summary>
    public sealed class RippleBell
    {
        public const float EchoDelay = 0.75f;
        const float Period = 2.2f;
        const float WindupTime = 0.8f;
        const float Reach = 9f;
        public readonly Vec2 Pos;
        public readonly int Room;
        public readonly float Floor;
        public readonly List<Shockwave> Waves = new List<Shockwave>(4);
        public readonly bool Echo, Harmless;
        float elapsed, echoIn = -1f;

        public RippleBell(Spawn spawn, Level level)
        {
            Pos = spawn.Pos;
            Room = spawn.Room;
            Floor = level.FloorBelow(Pos.X, Pos.Y + 0.5f);
            Echo = spawn.Kind == 'O' || spawn.Kind == 'o';
            Harmless = spawn.Kind == 'o';
        }

        void Toll(World world)
        {
            Waves.Add(new Shockwave { X = Pos.X, FloorY = Floor, Dir = -1 });
            Waves.Add(new Shockwave { X = Pos.X, FloorY = Floor, Dir = 1 });
            world.Emit(GameEventType.RippleToll, new Vec2(Pos.X, Floor));
        }

        /// <summary>Seconds until the echo pair, or negative when none is pending.</summary>
        public float EchoIn => echoIn;
        public float Windup => Mathx.Clamp((elapsed - (Period - WindupTime)) / WindupTime, 0f, 1f);

        public void Update(World world, float dt)
        {
            if (world.Room != Room)
            {
                Reset();
                return;
            }

            float previous = elapsed;
            elapsed += dt;
            if (previous < Period - WindupTime && elapsed >= Period - WindupTime)
                world.Emit(GameEventType.RippleWindup, Pos);
            if (elapsed >= Period)
            {
                elapsed -= Period;
                Toll(world);
                if (Echo) echoIn = EchoDelay;
            }
            if (echoIn >= 0f && (echoIn -= dt) < 0f) Toll(world);
            for (int i = Waves.Count - 1; i >= 0; i--)
            {
                Shockwave wave = Waves[i];
                if (!wave.Advance(world.Level, world.BossT.WaveSpeed * dt) || MathF.Abs(wave.X - Pos.X) >= Reach)
                    Waves.RemoveAt(i);
            }
        }

        public void Reset()
        {
            Waves.Clear();
            elapsed = 0f;
            echoIn = -1f;
        }
    }
}
