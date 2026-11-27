using System.Collections.Generic;
using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// The Ballast Knight and the Tidekeeper, drawn from the finished Tidal Engine sheets. Every cel is 128 x 96 px, faces
    /// left, and rests its soles on pixel row 85 (see art/TidalEngine/Art guide.md). Attacks.png has twelve cels per attack
    /// (0-4 anticipation, 5-8 strike, 9-11 reset) in the rows Knight hammer, Tidekeeper hammer, floor wave, pressure lane
    /// and echo; OpenCrown.png repeats the hammer and the wave with the phase-two vane; ActorSupport.png holds idle, step
    /// and hurt cels (Knight, Tidekeeper, phase two).
    /// </summary>
    sealed class EngineFoeView
    {
        const float Pivot = 11f / 96f;
        readonly Enemy foe;
        readonly Transform parent;
        readonly SpriteRenderer body, tell;
        readonly List<SpriteRenderer> waves = new List<SpriteRenderer>();
        float time;

        public EngineFoeView(Enemy enemy, Transform parent)
        {
            foe = enemy;
            this.parent = parent;
            body = Art.NewSprite(enemy.Kind.ToString(), Cel("ActorSupport", 0, enemy is Tidekeeper ? 1 : 0), Color.white, 12, parent);
            tell = Art.NewSprite("attack tell", Art.Square, Palette.Danger, 10, parent);
            Tick(0f, false);
        }

        static Sprite Cel(string sheet, int col, int row) => CampaignArt.Cell(sheet, col * 128, row * 96, 128, 96, 0.5f, Pivot);

        static int Anticipation(float progress) => Mathf.Clamp((int)(progress * 5f), 0, 4);
        static int Strike(float progress) => 5 + Mathf.Clamp((int)(progress * 4f), 0, 3);
        static int Reset(float progress) => 9 + Mathf.Clamp((int)(progress * 3f), 0, 2);

        SpriteRenderer Wave(int i)
        {
            while (waves.Count <= i) waves.Add(Art.NewSprite("floor crest", Art.Dome, Palette.Wave, 26, parent));
            waves[i].enabled = true;
            return waves[i];
        }

        void SetTell(Vector2 centre, float width, float height, bool on)
        {
            tell.enabled = on;
            tell.transform.position = new Vector3(centre.x, centre.y);
            tell.transform.localScale = new Vector3(width, height, 1f);
        }

        public void Tick(float dt, bool reducedFlashing, float waveHeight = 0.9f)
        {
            time += dt;
            body.enabled = foe.Alive;
            Vector2 at = Interp.Of(foe.Body);
            body.transform.position = new Vector3(at.x, at.y - foe.Body.Half.Y);
            body.flipX = foe.Facing > 0;               // the cels face left
            body.color = foe.Flash > 0f && !reducedFlashing ? Palette.Warm : Color.white;
            if (foe is PumpMachine knight) TickKnight(knight);
            else if (foe is Tidekeeper keeper) TickKeeper(keeper, waveHeight);
        }

        void TickKnight(PumpMachine m)
        {
            int idle = ((int)(time * 4f)) % 6;
            Sprite pose = Cel("ActorSupport", m.Flash > 0f ? 6 : idle, 0);
            bool warning = m.Beat == MachineBeat.Mark || m.Beat == MachineBeat.Brace;
            float reach = 3.4f;
            Aabb lane = m.AttackLane;
            if (m.Beat == MachineBeat.Mark || m.Beat == MachineBeat.Brace)
                pose = Cel("Attacks", Anticipation(m.Elapsed / (m.Beat == MachineBeat.Brace ? 1.0f : 0.6f)), 0);
            else if (m.Beat == MachineBeat.Strike)
                pose = Cel("Attacks", Strike(m.Elapsed / 0.3f), 0);
            else if (m.Beat == MachineBeat.Recover)
                pose = Cel("Attacks", Reset(m.Elapsed / m.RecoverTime), 0);
            body.sprite = pose;
            bool live = m.Beat == MachineBeat.Strike;
            tell.color = live ? Palette.Warm : Palette.Danger;
            SetTell(new Vector2(lane.Center.X, lane.MinY + 0.0625f), reach, 0.125f, m.Alive && (warning || live));
        }

        void TickKeeper(Tidekeeper k, float waveHeight)
        {
            bool crown = k.SecondPhase;
            Sprite pose = Cel("ActorSupport", k.Flash > 0f ? 6 : ((int)(time * 4f)) % 6, crown ? 2 : 1);
            if (k.Beat == KeeperBeat.Recover || k.Beat == KeeperBeat.Rest) pose = Cel("ActorSupport", k.Flash > 0f ? 6 : ((int)(time * 3f)) % 6, crown ? 2 : 1);
            bool windup = k.Beat == KeeperBeat.Windup, strike = k.Beat == KeeperBeat.Strike, reset = k.Beat == KeeperBeat.Recover;
            if (windup || strike || reset)
            {
                int row = k.Attack == KeeperAttack.Hammer ? 1 : k.Attack == KeeperAttack.Wave ? 2 : k.Attack == KeeperAttack.Lane ? 3 : 4;
                int col = windup ? Anticipation(k.WindupProgress) : strike ? Strike(k.Elapsed / k.StrikeTime(k.Attack)) : Reset(k.Elapsed / k.RecoverTime);
                bool open = crown && (k.Attack == KeeperAttack.Hammer || k.Attack == KeeperAttack.Wave);
                pose = open ? Cel("OpenCrown", col, k.Attack == KeeperAttack.Hammer ? 0 : 1) : Cel("Attacks", col, row);
            }
            body.sprite = pose;
            // The telegraph on the floor: where the attack will land, shown through the whole wind-up and strike.
            float floor = k.Body.Bottom;
            bool tellOn = k.Alive && (windup || strike);
            Vec2 home = k.Home;
            if (k.Attack == KeeperAttack.Hammer)
                SetTell(new Vector2(home.X + k.Facing * (0.4f + Tidekeeper.HammerReach) * 0.5f, floor + 0.0625f), Tidekeeper.HammerReach - 0.4f, 0.125f, tellOn);
            else if (k.Attack == KeeperAttack.Lane)
                SetTell(new Vector2(home.X + k.Facing * (0.3f + Tidekeeper.LaneReach) * 0.5f, floor + 0.95f), Tidekeeper.LaneReach - 0.3f, 0.125f, tellOn);
            else
                SetTell(new Vector2(home.X, floor + 0.0625f), 2f * (k.Body.Half.X + 1.6f), 0.125f, tellOn);
            tell.color = strike ? Palette.Warm : Palette.Danger;
            for (int i = 0; i < k.Waves.Count; i++) Art.DrawWave(Wave(i), k.Waves[i], waveHeight, time);
            for (int i = k.Waves.Count; i < waves.Count; i++) waves[i].enabled = false;
        }
    }
}
