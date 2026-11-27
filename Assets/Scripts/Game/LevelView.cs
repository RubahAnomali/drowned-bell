using System.Collections.Generic;
using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// Draws the rooms: one static mesh per room for rock, ledges and spikes, a cool gradient backdrop with
    /// pillar silhouettes, and the warm landmarks (the great bell, the tide lamp, the winch, the arch lamps).
    /// Dynamic pieces (grate, arena door, lamp glow, pickups) are updated from the world each frame.
    /// </summary>
    public sealed class LevelView
    {
        public readonly Transform Root;
        readonly World world;
        readonly Level lv;
        readonly List<SpriteRenderer> grate = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> doors = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> keyDoors = new List<SpriteRenderer>();
        // Sounding Archive props follow their corresponding Core markers.
        SpriteRenderer ledger, drumKey, order, chuteLight;
        readonly SpriteRenderer lampFlame, lampGlow, winchArm, winchGlow, lung, lungGlow;
        readonly List<SpriteRenderer> archGlows = new List<SpriteRenderer>();
        readonly List<(RippleBell Bell, SpriteRenderer Shell, SpriteRenderer Signal, List<SpriteRenderer> Waves)> ripples =
            new List<(RippleBell, SpriteRenderer, SpriteRenderer, List<SpriteRenderer>)>();
        readonly List<(Vec2 Feet, SpriteRenderer Jet, SpriteRenderer Gauge)> vents =
            new List<(Vec2, SpriteRenderer, SpriteRenderer)>();
        SpriteRenderer recordPlate, pumpKey, liftLight;
        float time;

        public LevelView(World w, Transform parent)
        {
            world = w;
            lv = w.Level;
            Root = Art.Group("Level", parent);
            foreach (RoomInfo r in lv.Rooms) BuildRoom(r);
            foreach (RippleBell bell in w.RippleBells)
            {
                Vector3 p = V(bell.Pos);
                SpriteRenderer shell;
                if (bell.Echo)
                {
                    // echo drum: grey for the harmless practice drum, warm for a live one.
                    shell = Art.NewSprite("echo drum", ArchiveArt.Prop(bell.Harmless ? ArchiveProp.PracticeDrum : ArchiveProp.LiveDrum), Color.white, 4, Root);
                    shell.transform.position = p + new Vector3(0, 0.6f);
                }
                else
                {
                    var rope = Art.NewSprite("ripple support", Art.Square, Palette.RockSide, -1, Root, 0.125f, 10f);
                    rope.transform.position = p + new Vector3(0, 5f);
                    shell = Art.NewSprite("ripple bell", Art.TrainingBell, Color.white, 4, Root, 1.5f, 1.5f);
                    shell.transform.position = p;
                }
                var signal = Art.NewSprite("ripple floor tell", Art.Square, Palette.Wave, 5, Root, 0.25f, 0.125f);
                signal.transform.position = new Vector3(bell.Pos.X, bell.Floor + 0.07f);
                ripples.Add((bell, shell, signal, new List<SpriteRenderer>(2)));
            }
            if (w.ArenaRoom < 0 && !lv.Outlet.HasValue)
            {
                foreach (Enemy foe in w.Foes)
                {
                    if (!(foe is Dummy)) continue;
                    var stand = Art.NewSprite("Target stand", Art.Square, Palette.RockEdge, 8, Root, 0.1875f, 0.8f);
                    stand.transform.position = V(foe.Body.Feet) + new Vector3(0, 0.4f);
                    var target = Art.NewSprite("Target face", Art.Ring, Palette.Warm, 9, Root, 0.85f, 0.85f);
                    target.transform.position = V(foe.Body.Pos);
                    var center = Art.NewSprite("Target centre", Art.Circle, Palette.WarmDeep, 10, Root, 0.2f, 0.2f);
                    center.transform.position = target.transform.position;
                }
                return;
            }

            // Grate (the shortcut) and the arena door, drawn as bars so they read as "a way through, but closed".
            for (int y = 0; y < lv.H; y++)
                for (int x = 0; x < lv.W; x++)
                {
                    Tile t = lv.At(x, y);
                    if (t == Tile.KeyDoor)
                    {
                        var bars = Art.NewSprite("drum-key door", ArchiveArt.Prop(ArchiveProp.Bars), Color.white, 2, Root);
                        bars.transform.position = new Vector3(x + 0.5f, y + 0.5f);
                        keyDoors.Add(bars);
                        continue;
                    }
                    if (t != Tile.Grate && t != Tile.Door) continue;
                    var sr = Art.NewSprite(t == Tile.Grate ? "grate" : "door", Art.Square, t == Tile.Grate ? Palette.WarmDeep : Palette.RockEdge, 2, Root, t == Tile.Grate ? 0.86f : 0.22f, t == Tile.Grate ? 0.86f : 1f);
                    sr.transform.position = new Vector3(x + 0.5f, y + 0.5f);
                    (t == Tile.Grate ? grate : doors).Add(sr);
                }

            // Tide lamp: the checkpoint. Warm, tall, and lit from the start of the room.
            Vector3 lamp = V(lv.Checkpoint);
            lampGlow = Art.NewSprite("lampGlow", Art.Glow, Palette.WarmGlow, -5, Root, 7f, 7f);
            lampGlow.transform.position = lamp + new Vector3(0, 15f / 16f);
            var housing = Art.NewSprite("tide lamp housing", PixelArt.TideLamp, Color.white, 4, Root);
            housing.transform.position = lamp + new Vector3(0, 11f / 16f);
            lampFlame = Art.NewSprite("pilot light", Art.Square, Palette.Warm, 3, Root, 0.25f, 0.25f);
            lampFlame.transform.position = lamp + new Vector3(0, 15f / 16f);

            // Winch: opens the grate. A warm wheel so it reads as "interactable, part of the way forward".
            Vector3 wn = V(lv.Lever);
            var wheel = Art.NewSprite("winch", lv.Chapter == Chapter.Archive ? ArchiveArt.Prop(ArchiveProp.Weight) : PixelArt.Winch, Color.white, 3, Root);
            wheel.transform.position = wn + new Vector3(0, 0.7f);
            winchArm = Art.NewSprite("winchArm", Art.Square, Palette.WarmDeep, 4, Root, 0.12f, 0.8f);
            winchArm.transform.position = wn + new Vector3(0, 0.7f);
            winchGlow = Art.NewSprite("winchGlow", Art.Glow, Palette.A(Palette.Warm, 0.25f), -4, Root, 2.4f, 2.4f);
            winchGlow.transform.position = wn + new Vector3(0, 0.7f);

            // Brass Lung: the optional reward, glinting on its ledge so it can be seen from below.
            if (lv.HasHeart)
            {
                Vector3 lg = V(lv.Heart);
                lungGlow = Art.NewSprite("lungGlow", Art.Glow, Palette.A(Palette.Warm, 0.45f), -4, Root, 2.2f, 2.2f);
                lungGlow.transform.position = lg + new Vector3(0, 0.5f);
                lung = Art.NewSprite("lung", PixelArt.BrassLung, Color.white, 6, Root);
                lung.transform.position = lg + new Vector3(0, 0.5f);
            }
            if (lv.Chapter == Chapter.Sluice && lv.Outlet is Vec2 outlet) BuildSluiceFixtures(outlet);
            if (lv.Chapter == Chapter.Archive) BuildArchiveFixtures();

            // Warm arch lamps flank the drop into the bell's chamber.
            foreach (Vec2 a in lv.ArchLamps)
            {
                Vector3 p = V(a);
                var post = Art.NewSprite("arch", Art.Square, Palette.RockEdge, 3, Root, 0.14f, 1.3f);
                post.transform.position = p + new Vector3(0, 0.65f);
                var fl = Art.NewSprite("archFlame", Art.Circle, Palette.Warm, 4, Root, 0.3f, 0.38f);
                fl.transform.position = p + new Vector3(0, 1.38f);
                var gl = Art.NewSprite("archGlow", Art.Glow, Palette.WarmGlow, -5, Root, 4.5f, 4.5f);
                gl.transform.position = p + new Vector3(0, 1.3f);
                archGlows.Add(gl);
            }

            // The great bell: the landmark seen from the first room, and again (alive) in the last.
            foreach (Vec2 b in lv.Landmarks)
            {
                var glow = Art.NewSprite("bellGlow", Art.Glow, new Color(1f, 0.68f, 0.3f, 0.22f), -15, Root, 9f, 9f);
                glow.transform.position = V(b) + new Vector3(0, -0.5f);
                var mb = new Art.MeshBuilder();
                List<Vector2> outline = Art.BellOutline(4.4f, 4.8f);
                for (int k = 0; k < outline.Count; k++) outline[k] += new Vector2(b.X, b.Y - 0.5f);
                mb.Poly(outline, new Color(0.16f, 0.24f, 0.26f));
                mb.Quad(b.X - 2.2f, b.Y - 2.9f, b.X + 2.2f, b.Y - 2.6f, Palette.A(Palette.BellRim, 0.8f));
                mb.Quad(b.X - 0.06f, b.Y + 1.9f, b.X + 0.06f, b.Y + 9f, new Color(0.12f, 0.18f, 0.2f));
                mb.Build("landmarkBell", Root, -14);
            }
        }

        void BuildSluiceFixtures(Vec2 outlet)
        {
            foreach (Vec2 feet in lv.Vents)
            {
                var basePlate = Art.NewSprite("slotted relief valve", PixelArt.VentPlate, Color.white, 4, Root);
                basePlate.transform.position = V(feet) + new Vector3(0, 0.125f);
                var jet = Art.NewSprite("pressure plume", PixelArt.SteamJet, Color.white, 6, Root);
                var gauge = Art.NewSprite("valve indicator", Art.Square, Palette.Water, 5, Root, 0.25f, 0.125f);
                gauge.transform.position = V(feet) + new Vector3(0, 0.1875f);
                vents.Add((feet, jet, gauge));
            }
            var warden = world.Warden;
            recordPlate = Art.NewSprite("keeper gauge plate", PixelArt.GaugePlate, Color.white, 8, Root);
            recordPlate.transform.position = V(lv.Record.Value) + new Vector3(0, 0.55f);
            pumpKey = Art.NewSprite("archive pump key", PixelArt.PumpKey, Color.white, 12, Root);
            pumpKey.transform.position = new Vector3(warden.Home.X, warden.Body.Bottom + 0.65f);
            pumpKey.enabled = false;
            foreach (Vec2 stop in new[] { SluiceWorks.TrapLift, SluiceWorks.WellStop, lv.LiftLamp.Value, lv.Checkpoint })
            {
                var deck = Art.NewSprite("brass lift sill", PixelArt.VentPlate, Color.white, 3, Root, 2f, 0.75f);
                deck.transform.position = V(stop) + new Vector3(0, 0.06f);
                for (int side = -1; side <= 1; side += 2)
                {
                    var cable = Art.NewSprite("hoist chain", Art.Square, Palette.RockSide, -4, Root, 0.125f, 10f);
                    cable.transform.position = V(stop) + new Vector3(side, 5);
                }
            }
            var lamp = Art.NewSprite("lift checkpoint", PixelArt.TideLamp, Color.white, 5, Root);
            lamp.transform.position = V(lv.LiftLamp.Value) + new Vector3(0, 11f / 16f);
            liftLight = Art.NewSprite("lift pilot light", Art.Square, Palette.Warm, 4, Root, 0.25f, 0.25f);
            liftLight.transform.position = V(lv.LiftLamp.Value) + new Vector3(0, 15f / 16f);
            var impeller = Art.NewSprite("dry impeller", Art.Ring, Palette.RockSide, -10, Root, 8, 8);
            impeller.transform.position = V(SluiceWorks.WellStop) + new Vector3(-8, 6);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                var blade = Art.NewSprite("impeller vane", Art.Square, Palette.Pillar, -9, Root, 1, 3);
                blade.transform.rotation = Quaternion.Euler(0, 0, angle);
                blade.transform.position = impeller.transform.position + blade.transform.up * 1.8f;
            }
            foreach (int x in new[] { 133, 146, 151 })
            {
                var chain = Art.NewSprite("counterweight rail", Art.Square, Palette.RockSide, -8, Root, 0.15f, 9f);
                chain.transform.position = new Vector3(x, lv.LiftLamp.Value.Y + 11);
                var weight = Art.NewSprite("iron counterweight", Art.Square, Palette.Pillar, -7, Root, 1.4f, 3);
                weight.transform.position = chain.transform.position - new Vector3(0, 4);
            }
            var hatch = Art.NewSprite("inspection hatch", PixelArt.OutletHatch, Color.white, -1, Root, 2, 2);
            hatch.transform.position = V(outlet) + new Vector3(0, 1.5f);
        }

        void BuildArchiveFixtures()
        {
            ledger = Art.NewSprite("keeper ledger", ArchiveArt.Prop(ArchiveProp.Ledger), Color.white, 8, Root);
            ledger.transform.position = V(lv.Record.Value) + new Vector3(0, 0.5f);
            drumKey = Art.NewSprite("drum key", ArchiveArt.Prop(ArchiveProp.Key), Color.white, 12, Root);
            drumKey.transform.position = V(world.Widow.Body.Feet) + new Vector3(0, 0.6f);
            order = Art.NewSprite("sea-gate order", ArchiveArt.Prop(ArchiveProp.Order), Color.white, 12, Root);
            var chute = Art.NewSprite("chute sill", ArchiveArt.Prop(ArchiveProp.Sill), Color.white, 3, Root);
            chute.transform.position = V(SoundingArchive.ChuteStop) + new Vector3(0, 0.1f);
            chuteLight = Art.NewSprite("chute light", ArchiveArt.Prop(ArchiveProp.Lamp), Color.white, 4, Root);
            chuteLight.transform.position = V(SoundingArchive.ChuteStop) + new Vector3(0, 1.2f);
            var exit = Art.NewSprite("engine-room exit", ArchiveArt.Prop(ArchiveProp.Exit), Color.white, -1, Root);
            exit.transform.position = V(lv.Outlet.Value) + new Vector3(0, 1.2f);
        }

        static Vector3 V(Vec2 p) => new Vector3(p.X, p.Y);

        bool Solidish(int x, int y)
        {
            Tile t = lv.At(x, y);
            return t == Tile.Rock;
        }

        void BuildRoom(RoomInfo r)
        {
            var bg = new Art.MeshBuilder();
            bg.Quad(r.MinX, r.MinY, r.MaxX, r.MaxY, Palette.SkyTop);
            for (float x = r.MinX + 2f; x + 5f < r.MaxX; x += 7f)
            {
                for (float y = r.MinY + 3f; y + 8f < r.MaxY; y += 15f)
                {
                    bg.Quad(x, y, x + 5f, y + 6f, Palette.Pillar);
                    bg.Quad(x + 0.5f, y + 0.5f, x + 4.5f, y + 5f, Palette.SkyBottom);
                    for (int step = 0; step < 4; step++)
                    {
                        float inset = step * 0.25f;
                        bg.Quad(x + inset, y + 6f + inset, x + 5f - inset, y + 6.25f + inset, Palette.Pillar);
                        bg.Quad(x + 0.625f + inset, y + 5f + inset, x + 4.375f - inset, y + 5.25f + inset, Palette.SkyBottom);
                    }
                    bg.Quad(x + 2.375f, y + 0.5f, x + 2.625f, y + 5.5f, Palette.Pillar);
                }
            }
            for (float y = r.MinY + 4f; y < r.MaxY; y += 2f)
            {
                float start = r.MinX + ((int)y % 4 == 0 ? 1.5f : 3.5f);
                for (float x = start; x + 0.875f < r.MaxX; x += 9f)
                    bg.Quad(x, y, x + 0.875f, y + 0.0625f, Palette.Pillar);
            }
            float tide = r.MinY + 2.625f;
            bg.Quad(r.MinX, r.MinY, r.MaxX, tide, Palette.A(Palette.RockSide, 0.45f));
            for (float x = r.MinX + 1.5f; x + 1.5f < r.MaxX; x += 4.5f)
                bg.Quad(x, tide, x + 1.5f, tide + 0.0625f, Palette.RockSide);
            if (lv.Chapter == Chapter.Sluice)
            {
                // Riveted iron mains run across each bay and drop towards the outlets.
                float y = r.MinY + 7f;
                bg.Quad(r.MinX, y, r.MaxX, y + 0.75f, Palette.RockSide);
                bg.Quad(r.MinX, y + 0.5f, r.MaxX, y + 0.625f, Palette.Water);
                for (float x = r.MinX + 3f; x < r.MaxX; x += 8f)
                {
                    bg.Quad(x, y - 0.1875f, x + 0.5f, y + 0.9375f, Palette.Pillar);
                    bg.Quad(x + 0.125f, y + 0.625f, x + 0.25f, y + 0.75f, Palette.RockEdge);
                }
                foreach (Vec2 vent in lv.Vents)
                {
                    if (!r.Contains(vent)) continue;
                    bg.Quad(vent.X - 0.25f, vent.Y, vent.X + 0.25f, y, Palette.RockSide);
                    bg.Quad(vent.X - 0.375f, y - 0.375f, vent.X + 0.375f, y, Palette.Pillar);
                }
            }
            if (lv.Chapter == Chapter.Archive)
            {
                for (float x = r.MinX + 2; x + 5 < r.MaxX; x += 7)
                {
                    for (float y = r.MinY + 3; y + 6 < r.MaxY; y += 8)
                    {
                        // Green-glass lancet and a pair of suspended catalogue shelves.
                        bg.Quad(x + 1, y + 2, x + 4, y + 5, Palette.Hex(0x23413e));
                        bg.Quad(x + 1.5f, y + 5, x + 3.5f, y + 5.5f, Palette.Hex(0x23413e));
                        bg.Quad(x + 2.375f,y + 2,x + 2.5625f,y + 5.5f, Palette.Pillar);
                        for (int tier = 0; tier < 2; tier++)
                        {
                            float shelf = y + tier * 2f;
                            bg.Quad(x,shelf,x+5,shelf+0.1875f,Palette.RockSide);
                            for (int book = 0; book < 9; book++)
                            {
                                float spine = x + 0.25f + book * 0.5f;
                                float top = shelf + 0.75f + (book * 7 + tier * 3) % 5 * 0.125f;
                                bg.Quad(spine,shelf+0.1875f,spine+0.375f,top,book % 3 == 0 ? Palette.Hex(0x354344) : Palette.Pillar);
                                bg.Quad(spine,top-0.1875f,spine+0.25f,top-0.125f,Palette.RockSide);
                            }
                        }
                    }
                }
            }
            bg.Build("backdrop " + r.Name, Root, -20);

            var mb = new Art.MeshBuilder();
            int x0 = (int)r.MinX, x1 = (int)r.MaxX, y0 = (int)r.MinY, y1 = (int)r.MaxY;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    Tile t = lv.At(x, y);
                    if (t == Tile.Rock)
                    {
                        bool open = !Solidish(x, y + 1) || !Solidish(x, y - 1) || !Solidish(x - 1, y) || !Solidish(x + 1, y);
                        // Rock next to open space is lighter; deep rock fades to near-black so rooms read as shapes.
                        mb.Quad(x, y, x + 1, y + 1, open ? Palette.Rock : Palette.RockDeep);
                        if (lv.At(x, y + 1) != Tile.Rock && lv.At(x, y + 1) != Tile.Grate) mb.Quad(x, y + 0.82f, x + 1, y + 1, Palette.RockEdge);
                        if (!Solidish(x - 1, y)) mb.Quad(x, y, x + 0.1f, y + 1, Palette.RockSide);
                        if (!Solidish(x + 1, y)) mb.Quad(x + 0.9f, y, x + 1, y + 1, Palette.RockSide);
                        if (open && (x + y * 13) % 3 != 0)
                        {
                            mb.Quad(x + 0.125f, y + 0.4375f, x + 0.6875f, y + 0.5f, Palette.RockDeep);
                            mb.Quad(x + 0.625f, y + 0.1875f, x + 0.6875f, y + 0.4375f, Palette.RockDeep);
                        }
                    }
                    else if (t == Tile.OneWay)
                    {
                        if (lv.Chapter == Chapter.Archive)
                        {
                            mb.Quad(x, y + 0.625f, x + 1, y + 0.75f, Palette.RockSide);
                            mb.Quad(x + 0.625f,y + 0.5f,x + 0.8125f,y + 0.75f,Palette.BellRim);
                        }
                        mb.Quad(x, y + 0.75f, x + 1, y + 1, Palette.LedgeUnder);
                        mb.Quad(x, y + 0.9375f, x + 1, y + 1, Palette.Ledge);
                        mb.Quad(x + 0.125f, y + 0.4375f, x + 0.25f, y + 0.75f, Palette.LedgeUnder);
                    }
                    else if (t == Tile.Spikes)
                    {
                        // Three barnacle spikes per tile, coral so they never read as floor.
                        for (int k = 0; k < 3; k++)
                        {
                            float sx = x + k / 3f;
                            mb.Tri(new Vector2(sx + 0.02f, y), new Vector2(sx + 0.31f, y), new Vector2(sx + 0.165f, y + 0.72f), Palette.Spike);
                        }
                        mb.Quad(x, y, x + 1, y + 0.12f, Palette.SpikeDark);
                    }
                }
            mb.Build("tiles " + r.Name, Root, 0);
        }

        public void Tick(float dt)
        {
            if (world.ArenaRoom < 0 && !lv.Outlet.HasValue) return;
            if (ledger != null)
            {
                ledger.enabled = !world.LedgerFound;
                drumKey.enabled = !world.Widow.Alive && !world.DrumKey;
                order.enabled = !world.Archivist.Alive && !world.OrderTaken;
                order.transform.position = V(world.Archivist.DropAt) + new Vector3(0, 0.4f);
                chuteLight.color = lv.GrateOpen ? Palette.Warm : Palette.RockEdge;
                foreach (SpriteRenderer k in keyDoors) k.enabled = !lv.KeyOpen;
            }
            time += dt;
            if (world.Warden != null)
            {
                recordPlate.enabled = !world.GaugeFound;
                liftLight.enabled = world.LiftOnline;
                pumpKey.enabled = !world.Warden.Alive && !world.PumpKey;
            }
            foreach (var vent in vents)
            {
                VentState state = SluiceWorks.VentAt(world.Time, vent.Feet);
                bool active = state == VentState.Blowing;
                vent.Gauge.color = state == VentState.Quiet ? Palette.RockEdge : Palette.Danger;
                vent.Jet.enabled = state != VentState.Quiet;
                float height = active ? 3f : 0.5f;
                vent.Jet.transform.localScale = new Vector3(active ? 1f : 0.5f, height / 3f, 1);
                vent.Jet.transform.position = V(vent.Feet) + new Vector3(0, height * 0.5f);
                vent.Jet.flipX = ((int)(world.Time * 7f) & 1) == 1;
            }
            foreach (var view in ripples)
            {
                float windup = view.Bell.Windup;
                view.Shell.transform.rotation = Quaternion.Euler(0, 0, windup * 22f * Mathf.Sin(time * 17f));
                view.Shell.color = Color.Lerp(Color.white, new Color(1f, 0.74f, 0.43f), windup);
                view.Signal.enabled = windup > 0f;
                view.Signal.transform.localScale = new Vector3(0.25f + windup * 2f, 0.125f, 1f);
                while (view.Waves.Count < view.Bell.Waves.Count)
                    view.Waves.Add(Art.NewSprite("ripple wave", Art.Dome, Palette.Wave, 26, Root));
                for (int i = 0; i < view.Waves.Count; i++)
                {
                    SpriteRenderer sprite = view.Waves[i];
                    sprite.enabled = i < view.Bell.Waves.Count;
                    if (sprite.enabled) Art.DrawWave(sprite, view.Bell.Waves[i], world.BossT.WaveHeight, time);
                }
            }
            foreach (SpriteRenderer g in grate) g.enabled = !lv.GrateOpen;
            foreach (SpriteRenderer d in doors) d.enabled = lv.DoorsLocked;

            float flicker = 1f + 0.08f * Mathf.Sin(time * 11f) + 0.05f * Mathf.Sin(time * 4.3f);
            bool lit = world.LampLit;
            lampFlame.color = lit ? Palette.Warm : Palette.A(Palette.WarmDeep, 0.6f);
            lampFlame.transform.localScale = new Vector3(0.25f, lit ? 0.25f : 0.125f, 1f);
            lampGlow.color = Palette.A(Palette.Warm, lit ? 0.32f * flicker : 0.1f);

            float turn = lv.GrateOpen ? 140f : 0f;
            winchArm.transform.rotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(winchArm.transform.eulerAngles.z, turn, dt * 4f));
            winchGlow.enabled = !lv.GrateOpen;
            winchGlow.color = Palette.A(Palette.Warm, 0.18f + 0.1f * Mathf.Sin(time * 3f));

            if (lung != null)
            {
                lung.enabled = lungGlow.enabled = !world.LungTaken;
                lung.transform.localPosition = V(lv.Heart) + new Vector3(0, 0.55f + 0.08f * Mathf.Sin(time * 2.4f));
            }
            foreach (SpriteRenderer a in archGlows) a.color = Palette.A(Palette.Warm, 0.3f * flicker);
        }
    }
}
