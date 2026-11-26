using System.Collections.Generic;
using UnityEngine;

namespace DrownedBell.Game
{
    static class PixelArt
    {
        static readonly Dictionary<char, Color32> Inks = new Dictionary<char, Color32>
        {
            ['x'] = Palette.Hex(0x0b0f14), ['n'] = Palette.Hex(0x142028), ['s'] = Palette.Hex(0x1f3340),
            ['t'] = Palette.Hex(0x2f4b55), ['w'] = Palette.Hex(0x4b7378), ['f'] = Palette.Hex(0x8fb3a8),
            ['i'] = Palette.Hex(0xe6e1cf), ['a'] = Palette.Hex(0xd79a3b), ['e'] = Palette.Hex(0xf2c14e),
            ['d'] = Palette.Hex(0xe0584a), ['r'] = Palette.Hex(0x8c4a2f), ['v'] = Palette.Hex(0x6e5a9e),
            ['l'] = Palette.Hex(0xb7a6e0),
        };

        static Sprite diver, diverStride, diverAir, hook, crab, jelly, boss, crack, mask;

        static string[] DiverTop => new[]
        {
            "....xxxx....",
            "...xiiaax...",
            "..xiiiiaax..",
            "..xixtttax..",
            "..xixfwaax..",
            "..xixttxax..",
            "...xaaaxx...",
            "...xtatx....",
            "..xxatatx...",
            "..xwtttsx...",
            "..xaattxx...",
            "...xttsx....",
        };

        static Sprite DiverPose(params string[] legs)
        {
            var rows = new List<string>(DiverTop);
            rows.AddRange(legs);
            return Art.PixelSprite(rows.ToArray(), Inks);
        }

        public static Sprite Diver => diver ??= DiverPose(
            "...xtxtx....", "...xsxsx....", "...xx.xx....", "............");
        public static Sprite DiverStride => diverStride ??= DiverPose(
            "..xtxxsx....", ".xtx..xtx...", ".xxx...xxx..", "............");
        public static Sprite DiverAir => diverAir ??= DiverPose(
            "...xtxx.....", "..xtxxtx....", "..xxx.xx....", "............");
        public static Sprite Hook => hook ??= Art.PixelSprite(new[]
        {
            "...xaa..", "..xa..x.", "..xa....", "...xa...", "...xt...", "...xt...", "...xt...", "...xx...",
        }, Inks);

        public static Sprite Crab => crab ??= Art.PixelSprite(new[]
        {
            "....................",
            "......xxxxxx........",
            "....xxrrrrrrxx......",
            "...xrrdaaddrrrx.....",
            "..xrradddrddrrrx....",
            "..xrddrrrdrddrrx....",
            ".xxrddrrrddrrrrxx...",
            ".xrxxxxxxrrxxfrxx...",
            "xdrxxtttxxxxfxxxrx..",
            "xaxx.xttxx...xrxrx..",
            ".xx.xtx.xtxx.xaxrx..",
            "...xxx...xxx..xxx...",
        }, Inks);

        public static Sprite Jelly => jelly ??= Art.PixelSprite(new[]
        {
            ".....xxxxxx.....",
            "...xxllllvvxx...",
            "..xllillvvvvvx..",
            ".xllilvlvvvvvvx.",
            ".xllvlvvlvvvvvx.",
            "xvlvlllllvvvvvvx",
            "xvvlvlvvvvvvlvvx",
            "xvvllvlvvvvvvvxx",
            ".xxvvvxxvvvxxvx.",
            "..xlvx..xlvx.xx.",
            "..xvx...xvx..xv.",
            "..xlx...xvx..xv.",
            "...x....xlx.xv..",
            "...v.....x..x...",
            "..v.......v.....",
            "..........v.....",
        }, Inks);

        static Sprite tideLamp;
        public static Sprite TideLamp => tideLamp ??= Art.PixelSprite(new[]
        {
            ".......xx.......",
            "......xtwx......",
            ".....xwaatx.....",
            "....xaaaaaax....",
            "...xrrraarrrx...",
            "....xwt..twx....",
            "....xwt..twx....",
            "....xwt..twx....",
            "....xwt..twx....",
            "...xraaaaaaax...",
            "....xrrarrxx....",
            "......xttx......",
            "......xwtx......",
            "......xwtx......",
            ".....xaaaarx....",
            "......xttx......",
            "......xwtx......",
            "......xwtx......",
            "......xttx......",
            ".....xttttx.....",
            "....xwwtttsx....",
            "...xttttssssx...",
        }, Inks);

        static Sprite winch;
        public static Sprite Winch => winch ??= Art.PixelSprite(new[]
        {
            ".....xxxxxx.....",
            "...xxeaaaarxx...",
            "..xeaarrrraarx..",
            ".xaarx....xrarx.",
            ".xarxax..xaxrax.",
            "xeax.xaxxaax.rax",
            "xarx..xeerx..rax",
            "xarraaeaaraaarax",
            "xarrraaraarrrrax",
            "xarx..xaarx..rax",
            "xrax.xarxax..rax",
            ".xarxax..xaxarx.",
            ".xraax....xaarx.",
            "..xraarrrraarx..",
            "...xxrraaarrx...",
            ".....xxxxxx.....",
        }, Inks);

        static Sprite brassLung;
        public static Sprite BrassLung => brassLung ??= Art.PixelSprite(new[]
        {
            ".....xxxxx......",
            "....xfwiatx.....",
            ".....xttax......",
            "...xxaaaarxx....",
            "..xeearraaarx...",
            ".xieaaxxraaarx..",
            ".xeaarrxraaaarx.",
            ".xaaattxraattarx",
            ".xeaaaxxraaaaarx",
            ".xaaattxraatttrx",
            ".xaaaaxxraaaaarx",
            "..xaarx..xraarx.",
            "...xxx....xxxx..",
        }, Inks);

        static Sprite ventPlate;
        public static Sprite VentPlate => ventPlate ??= Art.PixelSprite(new[]
        {
            "..xxxxxxxxxxxx..",
            "xawwttwttwttwaax",
            "xtrrarrarrarratx",
            ".xxxxxxxxxxxxxx.",
        }, Inks);

        static Sprite steamJet;
        public static Sprite SteamJet => steamJet ??= Art.PixelSprite(new[]
        {
            ".f......",
            "..w...f.",
            "..f...w.",
            "...w.f..",
            ".f.f.w..",
            ".w..f.w.",
            "..f.f.w.",
            "..w.i.f.",
            ".w.f..w.",
            ".f.i.f..",
            "..wff.w.",
            "..f.w.f.",
            "...fiff.",
            "..wifw..",
            "..fwif..",
            ".wifwfw.",
            ".ffifw..",
            "..fifwf.",
            "..wfiif.",
            ".fififw.",
            "..iifif.",
            "..fiiwf.",
            "..ififi.",
            "...iif..",
        }, Inks);

        static Sprite outletHatch;
        public static Sprite OutletHatch => outletHatch ??= Art.PixelSprite(new[]
        {
            "....xxxxxxxx....",
            "..xxttwwttssxx..",
            ".xttwfffwwttssx.",
            "xtwftttssstttssx",
            "xtwtxxxxxxxxtssx",
            "xtwtxnnnnnnxtssx",
            "xtwtxnnnnnnxtssx",
            "xtwtxnnnnnnxtssx",
            "xtwtxnnnnnnxtssx",
            "xtwtxnnaannxtssx",
            "xtwtxnnnaannxtss",
            "xtwtxnaaaannxtss",
            "xtwtxnnnaannxtsx",
            "xtwtxnnaannxtssx",
            "xtwtxnnnnnnxtssx",
            "xtwtxnnnnnnxtssx",
            "xtwtxnnnnnnxtssx",
            "xtwtxnnnnnnxtssx",
            "xtwtxnnnnnnxtssx",
            "xtwtxnnnnnnxtssx",
            "xtwtxnnnnnnxtssx",
            "xtwttwwwwwwttssx",
            "xtwwwtttttttsssx",
            "xxxxxxxxxxxxxxxx",
        }, Inks);

        static Sprite pumpKey;
        public static Sprite PumpKey => pumpKey ??= Art.PixelSprite(new[]
        {
            "..xxxx......",
            ".xaeiax.....",
            "xat..fax....",
            "xat..wax....",
            ".xaraaax....",
            "..xxxxrax...",
            "......xrax..",
            ".......xrax.",
            "........xrax",
            ".......aaax.",
            ".......xax..",
            "........x...",
        }, Inks);

        static Sprite gaugePlate;
        public static Sprite GaugePlate => gaugePlate ??= Art.PixelSprite(new[]
        {
            "..xxxxxxxxxx..",
            ".xaaaaaaaaarx.",
            "xawfffffffwrax",
            "xawt.t.t.twrax",
            "xawt..d..twrax",
            "xawt...d.twrax",
            "xawsssssstwrax",
            ".xraaaaaaarxx.",
            "..xaarraaarx..",
            "...xxxxxxxx...",
        }, Inks);

        public static Sprite Mask => mask ??= Art.PixelSprite(new[]
        {
            "....xxxx....", "...xiiaax...", "..xiiiiaax..", "..xixtttax..",
            "..xixfwaax..", "..xixttxax..", "...xaaaxx...", "...xxxxxx...",
        }, new Dictionary<char, Color32>
        {
            ['x'] = new Color32(45, 45, 45, 255), ['a'] = new Color32(190, 190, 190, 255),
            ['i'] = Color.white, ['t'] = new Color32(112, 112, 112, 255),
            ['f'] = new Color32(238, 238, 238, 255), ['w'] = new Color32(158, 158, 158, 255),
        });

        public static Sprite Boss => boss ??= Undertoll(false);
        public static Sprite Crack => crack ??= Undertoll(true);

        static Sprite Undertoll(bool overlay)
        {
            var rows = new string[48];
            for (int y = 0; y < rows.Length; y++)
            {
                var row = new string('.', 48).ToCharArray();
                int half = y < 5 ? 4 : y < 9 ? 8 : y < 14 ? 11 : y < 28 ? 13 : 13 + (y - 28) / 2;
                if (overlay)
                {
                    if (y >= 17 && y < 43) row[29 - (y - 17) / 4 + (y / 4 % 2) * 3] = y < 25 ? 'r' : 'e';
                }
                else
                {
                    for (int x = 24 - half; x < 24 + half; x++)
                    {
                        int edge = x - (24 - half);
                        bool border = y == 0 || y == 47 || edge <= 1 || edge >= half * 2 - 2;
                        bool rim = y >= 40 || y == 11 || y == 12;
                        row[x] = border ? 'x' : rim ? (x < 24 ? 'a' : 'r') :
                            edge <= 4 ? 'f' : x < 20 ? 'w' : x < 31 ? 't' : 's';
                        if (!border && y > 18 && y < 37 && (x * 3 + y * 5) % 17 == 0)
                            row[x] = x < 24 ? 't' : 'n';
                    }
                }
                rows[y] = new string(row);
            }
            return Art.PixelSprite(rows, Inks);
        }
    }
}
