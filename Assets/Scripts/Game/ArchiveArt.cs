using UnityEngine;

namespace DrownedBell.Game
{
    enum ArchiveProp { PracticeDrum, LiveDrum, Ledger, Key, Order, Sill, Lamp, Exit, Bars, Weight, Clapper, Mark, LedgerDetail }

    static class ArchiveArt
    {
        static readonly Sprite[,] actors = new Sprite[4,12];
        static readonly Sprite[] props = new Sprite[13];
        static Texture2D actorSheet, propSheet;
        static readonly Vector2Int[] sizes =
        {
            new Vector2Int(20,16), new Vector2Int(20,16), new Vector2Int(16,16), new Vector2Int(14,14),
            new Vector2Int(18,14), new Vector2Int(32,6), new Vector2Int(8,10), new Vector2Int(24,38),
            new Vector2Int(16,16), new Vector2Int(16,20), new Vector2Int(10,12), new Vector2Int(24,4), new Vector2Int(42,38)
        };

        public static Sprite Actor(int row, int frame)
        {
            if (actorSheet == null) actorSheet = Resources.Load<Texture2D>("ArchiveActors");
            return actors[row,frame] ??= Sprite.Create(actorSheet, new Rect(frame*64, (3-row)*64, 64,64),
                new Vector2(0.5f,0),16f,0,SpriteMeshType.FullRect);
        }

        public static Sprite Prop(ArchiveProp prop)
        {
            if (propSheet == null) propSheet = Resources.Load<Texture2D>("ArchiveObjects");
            int i = (int)prop;
            Vector2Int n = sizes[i];
            return props[i] ??= Sprite.Create(propSheet, new Rect(i%8*64,128-i/8*64-n.y,n.x,n.y),
                new Vector2(0.5f,0.5f),16f,0,SpriteMeshType.FullRect);
        }
    }
}
