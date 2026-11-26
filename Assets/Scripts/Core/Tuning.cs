using System;

namespace DrownedBell.Core
{
    // Every feel number lives in these [Serializable] classes. The GameController holds one of each as a
    // public field, so Unity shows them in the Inspector (edit in the scene, or live while playing).

    [Serializable]
    public sealed class MovementTuning
    {
        public float RunSpeed = 8.2f;
        public float GroundAccel = 95f;
        public float GroundDecel = 120f;
        public float AirAccel = 70f;
        public float AirDecel = 40f;
        /// <summary>Tiles from the ground to the top of a full jump.</summary>
        public float JumpHeight = 3.4f;
        public float TimeToApex = 0.36f;
        public float FallGravityMul = 1.65f;
        public float JumpCutMul = 0.45f;
        /// <summary>Gravity multiplier near the apex while jump is held, for a little hang time.</summary>
        public float ApexGravityMul = 0.6f;
        public float ApexThreshold = 2.2f;
        public float MaxFallSpeed = 22f;
        public float CoyoteTime = 0.1f;
        public float JumpBuffer = 0.12f;
        public float DashSpeed = 18f;
        public float DashTime = 0.16f;
        public float DashCooldown = 0.42f;
        public float Width = 0.62f;
        public float Height = 1.05f;

        public float Gravity => 2f * JumpHeight / (TimeToApex * TimeToApex);
        public float JumpVelocity => 2f * JumpHeight / TimeToApex;
    }

    [Serializable]
    public sealed class CombatTuning
    {
        public int MaxHealth = 5;
        public float AttackWindup = 0.04f;
        public float AttackActive = 0.1f;
        public float AttackRecovery = 0.18f;
        /// <summary>How far the side slash reaches past the diver's body.</summary>
        public float AttackReach = 1.35f;
        public float AttackThickness = 1.0f;
        public float AttackBuffer = 0.12f;
        public float RecoilSpeed = 5f;
        public float PogoVelocity = 12.5f;
        public float InvulnTime = 1.1f;
        public float HurtStun = 0.28f;
        public float HurtKnockX = 7f;
        public float HurtKnockY = 7.5f;
        public float HitStop = 0.055f;
        public float HazardStun = 0.4f;
    }

    [Serializable]
    public sealed class EnemyTuning
    {
        // Brine Crab: teaches spacing
        public int CrabHealth = 3;
        public float CrabPatrolSpeed = 1.5f;
        public float CrabApproachSpeed = 2.4f;
        public float CrabSightX = 7f;
        public float CrabAttackRange = 2.4f;
        public float CrabWindup = 0.55f;
        public float CrabLungeTime = 0.22f;
        public float CrabLungeSpeed = 7.5f;
        public float CrabClawReach = 1.05f;
        public float CrabRecover = 0.8f;
        // Knell Jelly: teaches timing
        public int JellyHealth = 3;
        public float JellyDriftSpeed = 1.3f;
        public float JellyAggroRange = 6f;
        public float JellyHoverDistance = 2.6f;
        public float JellyDrift = 1.6f;
        public float JellyWindup = 0.9f;
        public float JellyPulseTime = 0.35f;
        public float JellyPulseRadius = 3.1f;
        public float JellyPulseBand = 0.8f;
        public float JellySpent = 1.3f;
    }

    [Serializable]
    public sealed class BossTuning
    {
        public int Health = 26;
        /// <summary>Masks a direct hit from the bell's body costs (a slam landing on you, a rush). Waves and drips cost one.</summary>
        public int ContactDamage = 2;
        /// <summary>Boss fights only: every this many landed hook hits on a boss restore one mask, so a bold fight can be won
        /// but a greedy one cannot (the bosses punish greed). Hits on ordinary enemies never heal.</summary>
        public int HealEveryHits = 7;
        public float IntroTime = 1.6f;
        public float IdleTime = 0.45f;
        public float SlamWindup = 0.7f;
        /// <summary>Delayed slam: the bell hangs this long at the top before dropping, to punish early dodges.</summary>
        public float SlamHold = 0.35f;
        /// <summary>Phase two: a second shockwave pair follows the first this long after landing.</summary>
        public float SlamEcho = 0.55f;
        public float SlamRise = 2.2f;
        /// <summary>How fast the rising bell drifts toward the diver during the first part of its wind-up.</summary>
        public float SlamDrift = 2.5f;
        public float SlamRecover = 1.05f;
        public float WaveSpeed = 8f;
        public float WaveHeight = 0.9f;
        public float RushWindup = 0.65f;
        public float RushSpeed = 12f;
        public float RushRecover = 0.95f;
        public float RainWindup = 0.85f;
        public float RainFallSpeed = 16f;
        public float RainRecover = 0.95f;
        /// <summary>Odd on purpose: the middle drip falls exactly where the diver stood, so standing still is never safe.</summary>
        public int RainDrops = 5;
        /// <summary>Speed and recovery multiplier once the bell is below half health.</summary>
        public float Phase2Speed = 1.25f;
        public float Phase2Recover = 0.75f;
    }
}
