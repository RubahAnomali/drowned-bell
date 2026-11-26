using System;
using System.Collections.Generic;

namespace DrownedBell.Core
{
    public sealed class RippleBell
    {
        const float Period = 2.2f;
        const float WindupTime = 0.8f;
        const float Reach = 9f;
        public readonly Vec2 Pos;
        public readonly int Room;
        public readonly float Floor;
        public readonly List<Shockwave> Waves = new List<Shockwave>(2);
        float elapsed;

        public RippleBell(Spawn spawn, Level level)
        {
            Pos = spawn.Pos;
            Room = spawn.Room;
            Floor = level.FloorBelow(Pos.X, Pos.Y);
        }

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
                Waves.Add(new Shockwave { X = Pos.X, FloorY = Floor, Dir = -1 });
                Waves.Add(new Shockwave { X = Pos.X, FloorY = Floor, Dir = 1 });
                world.Emit(GameEventType.RippleToll, new Vec2(Pos.X, Floor));
            }
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
        }
    }
}
