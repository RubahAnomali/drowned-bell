using System;
using System.Collections.Generic;

namespace DrownedBell.Core
{
    public enum Tile : byte { Empty, Rock, OneWay, Spikes, Grate, Door }

    public sealed class RoomInfo
    {
        public int Index;
        public string Name;
        public string Purpose;
        /// <summary>World-space bounds (tiles, y up).</summary>
        public float MinX, MinY, MaxX, MaxY;
        public bool Contains(Vec2 p) => p.X >= MinX && p.X < MaxX && p.Y >= MinY && p.Y < MaxY;
    }

    public struct Spawn
    {
        public char Kind;
        public Vec2 Pos;
        public int Room;
    }

    /// <summary>
    /// Tile grid in world space: tile (x, y) covers [x, x+1] x [y, y+1], y up. The grate and the arena door
    /// are solid or open depending on run state.
    /// </summary>
    public sealed class Level
    {
        public readonly int W, H;
        readonly Tile[] tiles;
        public bool GrateOpen;
        public bool DoorsLocked;
        public Vec2 PlayerStart, Checkpoint, Lever, Heart, BossSpawn;
        public bool HasBoss { get; private set; }
        public readonly List<Spawn> Spawns = new List<Spawn>();
        public readonly List<Vec2> Landmarks = new List<Vec2>();
        public readonly List<Vec2> ArchLamps = new List<Vec2>();
        public readonly List<RoomInfo> Rooms = new List<RoomInfo>();

        Level(int w, int h)
        {
            W = w;
            H = h;
            tiles = new Tile[w * h];
        }

        public static Level Build() => Parse(RoomData.Rows, RoomData.Rooms);

        public static Level Parse(string[] rows, (string Name, string Purpose, int Col, int Row, int W, int H)[] rooms)
        {
            int h = rows.Length, w = rows[0].Length;
            var lv = new Level(w, h);
            for (int i = 0; i < rooms.Length; i++)
            {
                var r = rooms[i];
                lv.Rooms.Add(new RoomInfo
                {
                    Index = i, Name = r.Name, Purpose = r.Purpose,
                    MinX = r.Col, MaxX = r.Col + r.W, MaxY = h - r.Row, MinY = h - r.Row - r.H,
                });
            }
            for (int row = 0; row < h; row++)
            {
                if (rows[row].Length != w) throw new ArgumentException("row " + row + " has width " + rows[row].Length);
                int y = h - 1 - row;
                for (int x = 0; x < w; x++)
                {
                    char c = rows[row][x];
                    // Markers stand on the tile below them: their position is the bottom-centre of the cell.
                    var at = new Vec2(x + 0.5f, y);
                    Tile t = Tile.Empty;
                    switch (c)
                    {
                        case '#': t = Tile.Rock; break;
                        case '=': t = Tile.OneWay; break;
                        case '^': t = Tile.Spikes; break;
                        case 'G': t = Tile.Grate; break;
                        case 'D': t = Tile.Door; break;
                        case 'P': lv.PlayerStart = at; break;
                        case 'C': lv.Checkpoint = at; break;
                        case 'L': lv.Lever = at; break;
                        case 'H': lv.Heart = at; break;
                        case 'B': lv.BossSpawn = at; lv.HasBoss = true; break;
                        case 'b': lv.Landmarks.Add(at); break;
                        case 'a': lv.ArchLamps.Add(at); break;
                        case 'c':
                        case 'j':
                        case 'T':
                        case 'W':
                            lv.Spawns.Add(new Spawn { Kind = c, Pos = at, Room = lv.RoomIndexAt(new Vec2(at.X, at.Y + 0.5f)) });
                            break;
                        case '.': break;
                        default: throw new ArgumentException("unknown tile '" + c + "'");
                    }
                    lv.tiles[y * w + x] = t;
                }
            }
            return lv;
        }

        public Tile At(int x, int y) => x < 0 || y < 0 || x >= W || y >= H ? Tile.Rock : tiles[y * W + x];

        /// <summary>Blocks movement from every side.</summary>
        public bool Solid(int x, int y)
        {
            Tile t = At(x, y);
            return t == Tile.Rock || (t == Tile.Grate && !GrateOpen) || (t == Tile.Door && DoorsLocked);
        }

        public bool OneWay(int x, int y) => At(x, y) == Tile.OneWay;
        public bool Spiky(int x, int y) => At(x, y) == Tile.Spikes;

        public int RoomIndexAt(Vec2 p)
        {
            for (int i = 0; i < Rooms.Count; i++) if (Rooms[i].Contains(p)) return i;
            return -1;
        }

        /// <summary>True when any tile overlapping the box is spikes.</summary>
        public bool TouchesSpikes(Aabb b)
        {
            int x0 = (int)MathF.Floor(b.MinX), x1 = (int)MathF.Floor(b.MaxX - 1e-4f);
            int y0 = (int)MathF.Floor(b.MinY), y1 = (int)MathF.Floor(b.MaxY - 1e-4f);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (Spiky(x, y)) return true;
            return false;
        }

        /// <summary>True when the box overlaps any solid tile.</summary>
        public bool OverlapsSolid(Aabb b)
        {
            int x0 = (int)MathF.Floor(b.MinX), x1 = (int)MathF.Floor(b.MaxX - 1e-4f);
            int y0 = (int)MathF.Floor(b.MinY), y1 = (int)MathF.Floor(b.MaxY - 1e-4f);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (Solid(x, y)) return true;
            return false;
        }

        /// <summary>Clear line between two points (sampled), used for enemy sight.</summary>
        public bool LineClear(Vec2 a, Vec2 b)
        {
            float d = Vec2.Distance(a, b);
            int n = Math.Max(1, (int)(d / 0.25f));
            for (int i = 1; i < n; i++)
            {
                Vec2 p = a + (b - a) * (i / (float)n);
                if (Solid((int)MathF.Floor(p.X), (int)MathF.Floor(p.Y))) return false;
            }
            return true;
        }

        /// <summary>Highest floor surface at or below `y` in column x (rock, one-way, or closed grate/door), or -1.</summary>
        public float FloorBelow(float x, float y)
        {
            int cx = (int)MathF.Floor(x);
            for (int ty = (int)MathF.Floor(y) - 1; ty >= 0; ty--)
                if (Solid(cx, ty) || OneWay(cx, ty)) return ty + 1;
            return -1f;
        }
    }
}
