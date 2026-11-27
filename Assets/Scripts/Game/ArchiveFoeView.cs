using System.Collections.Generic;
using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    sealed class ArchiveFoeView
    {
        readonly Enemy foe;
        readonly Transform parent;
        readonly SpriteRenderer body, mark, decoy, clapper, decoyMark;
        readonly List<SpriteRenderer> waves = new List<SpriteRenderer>();
        float time;

        public ArchiveFoeView(Enemy enemy, Transform parent)
        {
            foe = enemy;
            this.parent = parent;
            int row = foe is InkMoth ? 0 : foe is TuningWidow ? 1 : 2;
            body = Art.NewSprite(enemy.Kind.ToString(), ArchiveArt.Actor(row,0), Color.white, 12, parent);
            if (!(foe is TuningWidow))
                mark = Art.NewSprite("landing mark", ArchiveArt.Prop(ArchiveProp.Mark), Color.white, 5, parent);
            if (foe is Archivist)
            {
                decoy = Art.NewSprite("ink double", ArchiveArt.Actor(2,0), Color.white, 11, parent);
                clapper = Art.NewSprite("real clapper", ArchiveArt.Prop(ArchiveProp.Clapper), Color.white, 13, parent);
                decoyMark = Art.NewSprite("second landing mark", ArchiveArt.Prop(ArchiveProp.Mark), Color.white, 5, parent);
            }
            Tick(0f, false);
        }

        static int Anticipation(float progress) => progress < 0.25f ? 4 : progress < 0.56f ? 5 : 6;

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
            Vector2 at = Interp.Of(foe.Body);
            body.transform.position = new Vector3(at.x, at.y - foe.Body.Half.Y);
            body.flipX = foe.Facing < 0;
            body.color = foe.Flash > 0f && !reducedFlashing ? Palette.Warm : Color.white;
            switch (foe)
            {
                case InkMoth moth:
                {
                    mark.enabled = moth.Alive && (moth.Beat == MothBeat.Mark || moth.Beat == MothBeat.Dive);
                    mark.transform.position = new Vector3(moth.Mark.X, moth.Mark.Y + 0.125f);
                    mark.transform.localScale = new Vector3(0.8f, 0.65f + 0.35f * moth.MarkProgress, 1);
                    int frame = moth.Beat == MothBeat.Mark ? Anticipation(moth.MarkProgress)
                        : moth.Beat == MothBeat.Dive ? 7 + ((int)(moth.Elapsed * 14) & 1)
                        : moth.Beat == MothBeat.Lodged ? 9 + Mathf.Min(2, (int)(moth.Elapsed * 4))
                        : (int)(time * 8) % 4;
                    body.sprite = ArchiveArt.Actor(0, frame);
                    break;
                }
                case TuningWidow widow:
                {
                    int frame = widow.Beat == WidowBeat.Spread ? Anticipation(widow.SpreadProgress)
                        : widow.Beat == WidowBeat.Ring ? (widow.Elapsed < 0.22f ? 7 : 8)
                        : widow.Beat == WidowBeat.Recover ? 9 + Mathf.Min(2,(int)(widow.Elapsed * 3))
                        : (int)(time * 3) % 4;
                    body.sprite = ArchiveArt.Actor(1, frame);
                    DrawWaves(widow.Waves, waveHeight);
                    break;
                }
                case Archivist boss:
                {
                    bool marking = boss.Alive && (boss.Beat == InkBeat.Mark || boss.Beat == InkBeat.Dive);
                    mark.enabled = decoyMark.enabled = marking;
                    mark.transform.position = new Vector3(boss.RealMark.X, boss.RealMark.Y + 0.125f);
                    decoyMark.transform.position = new Vector3(boss.DecoyMark.X, boss.DecoyMark.Y + 0.125f);
                    mark.transform.localScale = decoyMark.transform.localScale = new Vector3(1.6f,1,1);
                    int frame = boss.Beat == InkBeat.Mark ? Anticipation(boss.MarkProgress)
                        : boss.Beat == InkBeat.Dive ? 7 + ((int)(boss.Elapsed * 14) & 1)
                        : boss.Beat == InkBeat.Recover ? 9 + Mathf.Min(2,(int)(boss.Elapsed / boss.RecoverTime * 3))
                        : boss.Beat == InkBeat.Dormant ? 9 : (int)(time * 5) % 4;
                    body.sprite = ArchiveArt.Actor(boss.SecondPhase ? 3 : 2, frame);
                    decoy.enabled = boss.Alive && boss.Beat == InkBeat.Mark;
                    decoy.transform.position = new Vector3(boss.DecoyMark.X, body.transform.position.y);
                    decoy.sprite = body.sprite;
                    decoy.flipX = body.flipX;
                    decoy.color = body.color;
                    clapper.enabled = boss.Alive && boss.Engaged;
                    float swing = boss.Beat == InkBeat.Mark ? Mathf.Sin(time * 14f) : 0f;
                    clapper.transform.rotation = Quaternion.Euler(0,0,swing * 25f);
                    clapper.transform.position = new Vector3(at.x + swing * 0.5f, body.transform.position.y + 0.29f);
                    DrawWaves(boss.Waves, waveHeight);
                    break;
                }
            }
        }
    }
}
