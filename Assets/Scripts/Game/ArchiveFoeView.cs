using System.Collections.Generic;
using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// PLACEHOLDER visuals for the Sounding Archive foes: plain shapes that show every gameplay tell (marks, spread,
    /// the real clapper, ripples) so the chapter is playable and testable. The final pixel art replaces the shapes;
    /// see docs/ARCHIVE_ART_BRIEF.md for the list. Gameplay reads only Core state, so swapping art changes nothing else.
    /// </summary>
    sealed class ArchiveFoeView
    {
        readonly Enemy foe;
        readonly Transform parent;
        readonly SpriteRenderer body, tell, mark, decoy, clapper, decoyMark;
        readonly List<SpriteRenderer> waves = new List<SpriteRenderer>();
        float time;

        public ArchiveFoeView(Enemy enemy, Transform parent)
        {
            foe = enemy;
            this.parent = parent;
            Color ink = new Color(0.16f, 0.12f, 0.22f);
            switch (foe)
            {
                case InkMoth _:
                    body = Art.NewSprite("PLACEHOLDER ink moth", Art.Dome, ink, 12, parent, 1.1f, 0.8f);
                    mark = Art.NewSprite("moth landing mark", Art.Square, Palette.Danger, 5, parent, 1.2f, 0.125f);
                    break;
                case TuningWidow _:
                    body = Art.NewSprite("PLACEHOLDER tuning widow", Art.Dome, new Color(0.35f, 0.42f, 0.5f), 12, parent, 1.5f, 1.2f);
                    tell = Art.NewSprite("widow forks", Art.Square, Palette.Danger, 13, parent, 0.15f, 1f);
                    break;
                case Archivist _:
                    body = Art.NewSprite("PLACEHOLDER Archivist", Art.Dome, ink, 12, parent, 2.4f, 2.6f);
                    decoy = Art.NewSprite("PLACEHOLDER ink decoy", Art.Dome, ink, 11, parent, 2.4f, 2.6f);
                    clapper = Art.NewSprite("real clapper", Art.Circle, Palette.Warm, 13, parent, 0.5f, 0.5f);
                    mark = Art.NewSprite("real landing mark", Art.Square, Palette.Danger, 5, parent, 2.4f, 0.125f);
                    decoyMark = Art.NewSprite("decoy landing mark", Art.Square, Palette.Danger, 5, parent, 2.4f, 0.125f);
                    break;
            }
            Tick(0f, false);
        }

        SpriteRenderer Wave(int i)
        {
            while (waves.Count <= i) waves.Add(Art.NewSprite("ripple", Art.Dome, Palette.Wave, 26, parent));
            waves[i].enabled = true;
            return waves[i];
        }

        void DrawWaves(List<Shockwave> list, float height)
        {
            for (int i = 0; i < list.Count; i++) Art.DrawWave(Wave(i), list[i], height, time);
            for (int i = list.Count; i < waves.Count; i++) waves[i].enabled = false;
        }

        public void Tick(float dt, bool reducedFlashing, float waveHeight = 0.9f)
        {
            time += dt;
            body.enabled = foe.Alive;
            body.transform.position = new Vector3(foe.Body.Pos.X, foe.Body.Pos.Y - foe.Body.Half.Y);
            body.flipX = foe.Facing < 0;
            Color flash = foe.Flash > 0f && !reducedFlashing ? Palette.Text : Color.white;
            switch (foe)
            {
                case InkMoth moth:
                {
                    bool marking = moth.Alive && (moth.Beat == MothBeat.Mark || moth.Beat == MothBeat.Dive);
                    mark.enabled = marking;
                    mark.transform.position = new Vector3(moth.Mark.X, moth.Mark.Y + 0.06f);
                    // Folded wings: narrower while marking; lodged: flat on the floor.
                    float w = moth.Beat == MothBeat.Mark ? 0.7f : moth.Beat == MothBeat.Lodged ? 1.3f : 1.1f;
                    body.transform.localScale = new Vector3(w, moth.Beat == MothBeat.Lodged ? 0.5f : 0.8f, 1f);
                    body.color = foe.Flash > 0f && !reducedFlashing ? Palette.Text : moth.Beat == MothBeat.Lodged ? new Color(0.4f, 0.3f, 0.5f) : new Color(0.16f, 0.12f, 0.22f);
                    break;
                }
                case TuningWidow widow:
                {
                    float spread = widow.Beat == WidowBeat.Spread ? widow.SpreadProgress : widow.Beat == WidowBeat.Ring ? 1f : 0f;
                    tell.enabled = widow.Alive && spread > 0f;
                    tell.transform.position = new Vector3(widow.Body.Pos.X, widow.Body.Pos.Y + widow.Body.Half.Y + 0.5f * spread);
                    tell.transform.localScale = new Vector3(0.15f + 1.2f * spread, 1f, 1f);
                    body.color = widow.Beat == WidowBeat.Recover ? new Color(0.55f, 0.6f, 0.65f) : flash * new Color(0.35f, 0.42f, 0.5f);
                    DrawWaves(widow.Waves, waveHeight);
                    break;
                }
                case Archivist boss:
                {
                    bool marking = boss.Alive && (boss.Beat == InkBeat.Mark || boss.Beat == InkBeat.Dive);
                    mark.enabled = decoyMark.enabled = marking;
                    mark.transform.position = new Vector3(boss.RealMark.X, boss.RealMark.Y + 0.06f);
                    decoyMark.transform.position = new Vector3(boss.DecoyMark.X, boss.DecoyMark.Y + 0.06f);
                    decoy.enabled = boss.Alive && boss.Beat == InkBeat.Mark;
                    decoy.transform.position = new Vector3(boss.DecoyMark.X, body.transform.position.y);
                    decoy.color = new Color(0.16f, 0.12f, 0.22f, 0.85f);
                    // The tell that separates them: only the real one swings its clapper.
                    clapper.enabled = boss.Alive && boss.Engaged;
                    float swing = boss.Beat == InkBeat.Mark ? Mathf.Sin(time * 14f) * 0.6f : 0f;
                    clapper.transform.position = new Vector3(boss.Body.Pos.X + swing, boss.Body.Pos.Y - boss.Body.Half.Y + 0.2f);
                    body.color = boss.Beat == InkBeat.Recover ? new Color(0.3f, 0.25f, 0.4f) : flash * new Color(0.16f, 0.12f, 0.22f);
                    if (boss.SecondPhase) body.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
                    DrawWaves(boss.Waves, waveHeight);
                    break;
                }
            }
        }
    }
}
