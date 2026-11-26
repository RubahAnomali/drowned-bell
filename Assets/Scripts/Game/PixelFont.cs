using System.Collections.Generic;
using UnityEngine;

namespace DrownedBell.Game
{
    static class PixelFont
    {
        const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!?.,:;+-/()[]%<>=_'";
        static readonly string[] Columns =
        {
            "7E1111117E", "7F49494936", "3E41414122", "7F4141221C", "7F49494941", "7F09090901",
            "3E41495132", "7F0808087F", "00417F4100", "2040413F01", "7F08142241", "7F40404040",
            "7F020C027F", "7F0408107F", "3E4141413E", "7F09090906", "3E4151215E", "7F09192946",
            "2649494932", "01017F0101", "3F4040403F", "1F2040201F", "7F2018207F", "6314081463",
            "0304780403", "6151494543", "3E5149453E", "00427F4000", "6249514946", "2241494936",
            "1814127F10", "2745454539", "3E49494932", "0101710D03", "3649494936", "264949493E",
            "00005F0000", "0201510906", "0000606000", "0080600000", "0000363600", "0080563600",
            "08083E0808", "0808080808", "4020100804", "001C224100", "0041221C00", "007F414100",
            "0041417F00", "232D126864", "0814224100", "0041221408", "1414141414", "4040404040",
            "0000030000",
        };

        static Texture2D atlas;

        static Texture2D Atlas()
        {
            if (atlas != null) return atlas;
            atlas = new Texture2D(Letters.Length * 6, 8, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[atlas.width * atlas.height];
            for (int i = 0; i < Letters.Length; i++)
                for (int x = 0; x < 5; x++)
                {
                    int bits = System.Convert.ToInt32(Columns[i].Substring(x * 2, 2), 16);
                    for (int y = 0; y < 8; y++)
                        if ((bits & (1 << y)) != 0) pixels[(7 - y) * atlas.width + i * 6 + x] = Color.white;
                }
            atlas.SetPixels32(pixels);
            atlas.Apply();
            return atlas;
        }

        public static void Draw(Rect rect, string content, int size, bool center, Color color)
        {
            if (string.IsNullOrEmpty(content)) return;
            Texture2D texture = Atlas();
            int lineWidth = Mathf.Max(1, (int)(rect.width / (6 * size)));
            var lines = new List<string>();
            foreach (string line in content.ToUpperInvariant().Split('\n'))
            {
                string remainder = line.TrimEnd();
                while (remainder.Length > lineWidth)
                {
                    int end = remainder.LastIndexOf(' ', lineWidth);
                    if (end <= 0) end = lineWidth;
                    lines.Add(remainder.Substring(0, end).TrimEnd());
                    remainder = remainder.Substring(end).TrimStart();
                }
                lines.Add(remainder);
            }
            float oldY = rect.y;
            float y = Mathf.Round(center ? oldY + (rect.height - lines.Count * 9 * size) * 0.5f : oldY);
            Color old = GUI.color;
            GUI.color = color;
            foreach (string line in lines)
            {
                float x = Mathf.Round(center ? rect.x + (rect.width - line.Length * 6 * size) * 0.5f : rect.x);
                foreach (char ch in line)
                {
                    int i = Letters.IndexOf(ch);
                    if (i >= 0)
                        GUI.DrawTextureWithTexCoords(new Rect(x, y, 6 * size, 8 * size), texture,
                            new Rect(i * 6f / texture.width, 0f, 6f / texture.width, 1f));
                    x += 6 * size;
                }
                y += 9 * size;
            }
            GUI.color = old;
        }
    }
}
