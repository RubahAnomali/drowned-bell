using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    sealed class PumpMachineView
    {
        static Sprite[,] poses;
        static Sprite[,] windups;
        static Sprite[,] wardenPoses;
        readonly PumpMachine actor;
        readonly SpriteRenderer body, lane, surge;
        readonly SpriteRenderer[] links = new SpriteRenderer[8];
        readonly int row;

        public PumpMachineView(PumpMachine machine, Transform parent)
        {
            actor = machine;
            row = actor.Kind == EnemyKind.Limpet ? 0 : actor.Kind == EnemyKind.Porter ? 1 : 2;
            if (poses == null)
            {
                var sheet = Resources.Load<Texture2D>("PumpMachines");
                poses = new Sprite[3, 4];
                windups = new Sprite[2, 3];
                int[] windupColumns = { 1, 4, 5 };
                for (int r = 0; r < 2; r++)
                    for (int f = 0; f < windupColumns.Length; f++)
                    {
                        // Wider canvas gives the hauling chain room to swing behind the porter.
                        var rect = new Rect(windupColumns[f] * 64 + 12, (2 - r) * 64, 40, 40);
                        windups[r, f] = Sprite.Create(sheet, rect, new Vector2(0.5f, 0), 16f, 0, SpriteMeshType.FullRect);
                    }
                int[] sizes = { 24, 28, 56 };
                for (int r = 0; r < 3; r++)
                    for (int f = 0; f < 4; f++)
                    {
                        int n = sizes[r];
                        var rect = new Rect(f * 64 + (64 - n) / 2, (2 - r) * 64, n, n);
                        poses[r, f] = Sprite.Create(sheet, rect, new Vector2(0.5f, 0), 16f, 0, SpriteMeshType.FullRect);
                    }
            }
            if (machine.IsWarden && wardenPoses == null)
            {
                var sheet = Resources.Load<Texture2D>("WardenPoses");
                wardenPoses = new Sprite[2,4];
                for (int attack = 0; attack < 2; attack++)
                    for (int f = 0; f < 4; f++)
                        wardenPoses[attack,f] = Sprite.Create(sheet, new Rect(f*80,(1-attack)*80,80,80),
                            new Vector2(0.5f,0),16f,0,SpriteMeshType.FullRect);
            }
            body = Art.NewSprite(actor.Kind.ToString(), poses[row, 0], Color.white, 12, parent);
            body.transform.position = new Vector3(actor.Home.X, actor.Body.Bottom);
            lane = Art.NewSprite("attack tell", Art.Square, Palette.Danger, 10, parent);
            surge = Art.NewSprite("overpressure exhaust", PixelArt.SteamJet, Palette.Wave, 11, parent, 0.5f, 0.5f);
            surge.transform.position = body.transform.position + new Vector3(0, 3.9f);
            for (int i = 0; i < links.Length; i++)
                links[i] = Art.NewSprite("attack linkage", machine.Kind == EnemyKind.Porter ? Art.Ring : Art.Square, Palette.WarmDeep, 12, parent);
            if (machine.IsWarden)
                for (int i = 0; i < 3; i++)
                {
                    Vec2 p = machine.Pipe(i);
                    var plate = Art.NewSprite("relief manifold", PixelArt.VentPlate, Color.white, 4, parent, 2.5f, 1f);
                    plate.transform.position = new Vector3(p.X, p.Y + 0.12f);
                }
            Tick(false);
        }

        public void Tick(bool reducedFlashing)
        {
            MachineBeat beat = actor.Beat;
            bool warning = beat == MachineBeat.Mark || beat == MachineBeat.Brace;
            bool live = beat == MachineBeat.Jet || beat == MachineBeat.Strike;
            body.enabled = actor.Alive;
            body.sprite = poses[row, warning ? 1 : live ? 2 : beat == MachineBeat.Recover ? 3 : 0];
            if (beat == MachineBeat.Brace && !actor.IsWarden)
            {
                int frame = actor.Elapsed < 0.16f ? 0 : actor.Elapsed < 0.38f ? 1 : 2;
                body.sprite = windups[row, frame];
            }
            if (actor.IsWarden && (warning || live))
            {
                bool vent = beat == MachineBeat.Mark || beat == MachineBeat.Jet;
                float progress = actor.Elapsed / (vent ? actor.MarkTime : actor.BraceTime);
                int frame = live ? 3 : progress < 0.25f ? 0 : progress < 0.58f ? 1 : 2;
                body.sprite = wardenPoses[vent ? 0 : 1,frame];
            }
            body.flipX = actor.Facing < 0;
            body.color = actor.Flash > 0 && !reducedFlashing ? Palette.Text : Color.white;
            surge.enabled = actor.Alive && actor.SecondPhase;
            surge.flipX = actor.Facing < 0;
            lane.enabled = actor.Alive && (warning || live);
            Aabb box = actor.AttackLane;
            bool steam = beat == MachineBeat.Jet || beat == MachineBeat.Mark;
            float width = box.Half.X * 2, height = live ? box.Half.Y * 2 : 0.125f;
            lane.sprite = beat == MachineBeat.Jet ? PixelArt.SteamJet : Art.Square;
            lane.color = warning ? Palette.Danger : steam ? Color.white : Palette.RockEdge;
            lane.transform.position = new Vector3(box.Center.X, steam ? box.MinY + height * 0.5f : box.Center.Y);
            lane.transform.localScale = new Vector3(width, beat == MachineBeat.Jet ? height / 3f : height, 1);
            for (int i = 0; i < links.Length; i++)
            {
                SpriteRenderer link = links[i];
                link.enabled = actor.Alive && beat == MachineBeat.Strike;
                if (!link.enabled) continue;
                link.transform.position = new Vector3(box.MinX + width * (i + 0.5f) / links.Length, box.Center.Y);
                link.transform.localScale = new Vector3(width / links.Length, (i & 1) == 0 ? 0.45f : 0.25f, 1);
            }
        }
    }
}
