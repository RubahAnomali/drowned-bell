using System;

namespace DrownedBell.Core
{
    public struct PlayerInput
    {
        public float MoveX;
        public bool Up, Down;
        public bool JumpPressed, JumpHeld;
        public bool DashPressed;
        public bool AttackPressed;
        /// <summary>
        /// Mouse aim: a direction from the diver toward the cursor, set only on the step a mouse click asked for
        /// a swing or dash. Held direction keys still win over it.
        /// </summary>
        public bool HasAim;
        public float AimX, AimY;
    }

    public enum AttackDir { Side, Up, Down }

    /// <summary>
    /// The salvage diver. Run with acceleration, variable-height jump with coyote time and a jump buffer, one
    /// dash on the ground or once per airtime, and a three-direction hook swing (down-swing pogoes).
    /// </summary>
    public sealed class Player
    {
        public readonly Body Body;
        public int Facing = 1;
        public int Health, MaxHealth;
        public float Invuln;
        public float Stun;
        public bool Dashing => dashLeft > 0f;
        public float DashCooldown;
        public bool AirDashUsed;

        // Attack state. AttackTime < 0 means no swing in progress.
        public float AttackTime = -1f;
        public AttackDir Attack;
        public int SwingId;
        public bool SwingConnected;

        /// <summary>Last spot on solid rock away from spikes; hazards return the diver here.</summary>
        public Vec2 LastSafe;

        float coyote, jumpBuffer, attackBuffer, dashLeft;
        bool jumping;
        bool attackHasAim;
        float attackAimX, attackAimY;

        public Player(Vec2 feet, MovementTuning m, int maxHealth)
        {
            Body = new Body(new Vec2(feet.X, feet.Y + m.Height * 0.5f + 0.001f), m.Width, m.Height);
            MaxHealth = Health = maxHealth;
            LastSafe = Body.Pos;
        }

        public void Place(Vec2 feet, MovementTuning m)
        {
            Body.Pos = new Vec2(feet.X, feet.Y + m.Height * 0.5f + 0.001f);
            Body.Vel = new Vec2(0, 0);
            Body.DropThrough = 0f;
            Stun = Invuln = 0f;
            dashLeft = DashCooldown = 0f;
            AttackTime = -1f;
            coyote = jumpBuffer = attackBuffer = 0f;
            jumping = false;
            AirDashUsed = false;
            LastSafe = Body.Pos;
        }

        public bool AttackActive(CombatTuning c) => AttackTime >= c.AttackWindup && AttackTime < c.AttackWindup + c.AttackActive;
        public bool Attacking => AttackTime >= 0f;

        /// <summary>The hook's hit box while the swing is active.</summary>
        public Aabb AttackBox(CombatTuning c, MovementTuning m)
        {
            Vec2 p = Body.Pos;
            float reach = c.AttackReach, thick = c.AttackThickness;
            switch (Attack)
            {
                case AttackDir.Up: return new Aabb(new Vec2(p.X, p.Y + Body.Half.Y + reach * 0.5f), new Vec2(thick * 0.6f, reach * 0.5f));
                case AttackDir.Down: return new Aabb(new Vec2(p.X, p.Y - Body.Half.Y - reach * 0.5f), new Vec2(thick * 0.6f, reach * 0.5f));
                default: return new Aabb(new Vec2(p.X + Facing * (Body.Half.X + reach * 0.5f), p.Y), new Vec2(reach * 0.5f, thick * 0.5f));
            }
        }

        /// <summary>The swing hit something: recoil sideways, or bounce for a down-swing (which also refunds the air dash).</summary>
        public void OnSwingHit(CombatTuning c)
        {
            SwingConnected = true;
            if (Attack == AttackDir.Down)
            {
                Body.Vel.Y = c.PogoVelocity;
                AirDashUsed = false;
                jumping = false;
            }
            else if (Attack == AttackDir.Side)
            {
                Body.Vel.X = -Facing * c.RecoilSpeed;
            }
        }

        /// <summary>
        /// Swing toward the cursor: mostly up is an up-swing, mostly down (in the air) a down-swing, anything else
        /// a side swing that also turns the diver to face the cursor.
        /// </summary>
        AttackDir AimedAttack(float ax, float ay, bool grounded)
        {
            if (ay > MathF.Abs(ax) * 1.2f) return AttackDir.Up;
            if (ay < -MathF.Abs(ax) * 1.2f && !grounded) return AttackDir.Down;
            if (MathF.Abs(ax) > 0.05f) Facing = Mathx.Sign(ax);
            return AttackDir.Side;
        }

        public void Hurt(float fromX, CombatTuning c)
        {
            Invuln = c.InvulnTime;
            Stun = c.HurtStun;
            dashLeft = 0f;
            AttackTime = -1f;
            int away = Body.Pos.X >= fromX ? 1 : -1;
            Body.Vel = new Vec2(away * c.HurtKnockX, c.HurtKnockY);
            jumping = false;
        }

        /// <summary>One fixed step. Returns flags describing what happened, for sound and effects.</summary>
        public PlayerFrame Update(float dt, PlayerInput input, Level lv, MovementTuning m, CombatTuning c)
        {
            var f = new PlayerFrame();
            Body b = Body;
            if (Invuln > 0f) Invuln -= dt;
            if (DashCooldown > 0f) DashCooldown -= dt;
            if (input.JumpPressed) jumpBuffer = m.JumpBuffer; else jumpBuffer -= dt;
            if (input.AttackPressed)
            {
                attackBuffer = c.AttackBuffer;
                attackHasAim = input.HasAim;
                attackAimX = input.AimX;
                attackAimY = input.AimY;
            }
            else attackBuffer -= dt;

            // Attack timeline
            if (AttackTime >= 0f)
            {
                AttackTime += dt;
                if (AttackTime >= c.AttackWindup + c.AttackActive + c.AttackRecovery) AttackTime = -1f;
            }

            if (Stun > 0f)
            {
                Stun -= dt;
                b.Vel.Y = MathF.Max(b.Vel.Y - m.Gravity * m.FallGravityMul * dt, -m.MaxFallSpeed);
                b.Vel.X = Mathx.MoveToward(b.Vel.X, 0f, m.AirDecel * dt);
                Physics.Move(lv, b, dt);
                return f;
            }

            if (b.Grounded) { coyote = m.CoyoteTime; AirDashUsed = false; }
            else coyote -= dt;

            // Dash: fixed speed, no gravity, can't be steered. One on the ground or once per airtime.
            if (input.DashPressed && DashCooldown <= 0f && dashLeft <= 0f && (b.Grounded || !AirDashUsed))
            {
                if (MathF.Abs(input.MoveX) > 0.1f) Facing = Mathx.Sign(input.MoveX);
                else if (input.HasAim && MathF.Abs(input.AimX) > 0.05f) Facing = Mathx.Sign(input.AimX);
                dashLeft = m.DashTime;
                DashCooldown = m.DashCooldown;
                if (!b.Grounded) AirDashUsed = true;
                AttackTime = -1f;
                jumping = false;
                f.Dashed = true;
            }
            if (dashLeft > 0f)
            {
                dashLeft -= dt;
                b.Vel = new Vec2(Facing * m.DashSpeed, 0f);
                Physics.Move(lv, b, dt);
                if (dashLeft <= 0f) b.Vel.X = Facing * m.RunSpeed;
                f.Landed = b.Landed;
                return f;
            }

            // Run
            float target = input.MoveX * m.RunSpeed;
            bool steering = MathF.Abs(input.MoveX) > 0.05f;
            float accel = b.Grounded ? (steering ? m.GroundAccel : m.GroundDecel) : (steering ? m.AirAccel : m.AirDecel);
            b.Vel.X = Mathx.MoveToward(b.Vel.X, target, accel * dt);
            if (steering && AttackTime < 0f) Facing = Mathx.Sign(input.MoveX);

            // Drop through a one-way ledge with down + jump.
            if (jumpBuffer > 0f && input.Down && b.Grounded && b.OnOneWay)
            {
                b.DropThrough = 0.25f;
                jumpBuffer = 0f;
                coyote = 0f;
            }
            else if (jumpBuffer > 0f && coyote > 0f)
            {
                b.Vel.Y = m.JumpVelocity;
                jumpBuffer = 0f;
                coyote = 0f;
                jumping = true;
                f.Jumped = true;
            }

            // Variable height: letting go early cuts the rise once.
            if (jumping && !input.JumpHeld && b.Vel.Y > 0f)
            {
                b.Vel.Y *= m.JumpCutMul;
                jumping = false;
            }
            if (b.Vel.Y <= 0f) jumping = false;

            float g = m.Gravity;
            if (b.Vel.Y < 0f) g *= m.FallGravityMul;
            else if (input.JumpHeld && MathF.Abs(b.Vel.Y) < m.ApexThreshold) g *= m.ApexGravityMul;
            b.Vel.Y = MathF.Max(b.Vel.Y - g * dt, -m.MaxFallSpeed);

            // Swing
            if (attackBuffer > 0f && AttackTime < 0f)
            {
                attackBuffer = 0f;
                Attack = input.Up ? AttackDir.Up : (input.Down && !b.Grounded ? AttackDir.Down : AttackDir.Side);
                if (attackHasAim && !input.Up && !input.Down) Attack = AimedAttack(attackAimX, attackAimY, b.Grounded);
                AttackTime = 0f;
                SwingId++;
                SwingConnected = false;
                f.Swung = true;
            }

            Physics.Move(lv, b, dt);
            f.Landed = b.Landed;
            if (b.Grounded && !b.OnOneWay && !lv.TouchesSpikes(new Aabb(b.Pos, new Vec2(b.Half.X + 1.2f, b.Half.Y + 0.6f))))
                LastSafe = b.Pos;
            return f;
        }
    }

    public struct PlayerFrame
    {
        public bool Jumped, Landed, Dashed, Swung;
    }
}
