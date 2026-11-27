using System.Collections.Generic;
using UnityEngine;

namespace DrownedBell.Game
{
    /// <summary>
    /// The finished campaign and Tidal Engine art (Assets/Resources/Art). Textures are point-sampled and uncompressed
    /// (see their .meta files). Rectangles are given in pixels from the TOP-LEFT of the PNG, the way the art guides
    /// (art/Campaign/README.md, art/TidalEngine/Art guide.md) describe them.
    /// </summary>
    static class CampaignArt
    {
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static Texture2D Tex(string name)
        {
            if (!textures.TryGetValue(name, out Texture2D t) || t == null)
            {
                t = Resources.Load<Texture2D>("Art/" + name);
                if (t != null) t.filterMode = FilterMode.Point;
                textures[name] = t;
            }
            return t;
        }

        /// <summary>Draws the pixel rectangle `px` (from the top-left) of a texture into a GUI rectangle.</summary>
        public static void Draw(Rect to, string name, Rect px)
        {
            Texture2D t = Tex(name);
            if (t == null) return;
            GUI.DrawTextureWithTexCoords(to, t, new Rect(px.x / t.width, 1f - (px.y + px.height) / t.height, px.width / t.width, px.height / t.height));
        }

        /// <summary>A sprite over a pixel rectangle (from the top-left) of a texture, 16 pixels per tile.</summary>
        public static Sprite Cell(string name, float x, float y, float w, float h, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            string key = name + ":" + x + "," + y + "," + w + "," + h + "," + pivotX + "," + pivotY;
            if (sprites.TryGetValue(key, out Sprite s) && s != null) return s;
            Texture2D t = Tex(name);
            if (t == null) return null;
            s = Sprite.Create(t, new Rect(x, t.height - y - h, w, h), new Vector2(pivotX, pivotY), 16f, 0, SpriteMeshType.FullRect);
            sprites[key] = s;
            return s;
        }
    }
}
