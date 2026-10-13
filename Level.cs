using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Kadaya
{
    class Plat { public float X, Y, W; }
    class Soul { public float X, Y; public bool Got; }
    class Theme
    {
        public string Name; public int Style; // 0 bamboo forest, 1 graveyard, 2 burning temple
        public Color Sky0, Sky1, Sky2, Moon, Far, Mid, Near, Fog, Accent, Ground;
    }
    class Level
    {
        public Theme Th; public int Num;
        public List<Vector2> Gaps = new List<Vector2>(), Spikes = new List<Vector2>();
        public List<Plat> Plats = new List<Plat>(); public List<Soul> Souls = new List<Soul>();
        public List<float> Checks = new List<float>(); public List<Foe> Foes = new List<Foe>();
        public float GateX;
    }

    static class LevelGen
    {
        const float G = 600;
        public static readonly Theme[] Themes =
        {
            new Theme{ Name="BLOOD MOON FOREST", Style=0, Sky0=new Color(8,2,10), Sky1=new Color(60,10,32), Sky2=new Color(150,30,48), Moon=new Color(255,70,70),
                Far=new Color(30,8,26), Mid=new Color(18,5,20), Near=new Color(8,2,10), Fog=new Color(200,60,100), Accent=new Color(255,60,90), Ground=new Color(30,12,28) },
            new Theme{ Name="HAUNTED GRAVEYARD", Style=1, Sky0=new Color(2,6,12), Sky1=new Color(10,32,48), Sky2=new Color(40,90,110), Moon=new Color(190,235,255),
                Far=new Color(8,24,34), Mid=new Color(5,15,24), Near=new Color(2,6,10), Fog=new Color(120,200,220), Accent=new Color(95,243,255), Ground=new Color(10,26,34) },
            new Theme{ Name="BURNING TEMPLE", Style=2, Sky0=new Color(10,2,0), Sky1=new Color(70,18,4), Sky2=new Color(200,80,10), Moon=new Color(255,170,60),
                Far=new Color(36,10,4), Mid=new Color(22,6,3), Near=new Color(10,3,1), Fog=new Color(255,120,40), Accent=new Color(255,150,40), Ground=new Color(34,12,6) },
        };

        static Foe Mk(int type, float x, int lv, bool boss = false)
        {
            float[] hp = { 40, 100, 34 }, sp = { 250, 120, 130 };
            float h = hp[type] * (1 + lv * .25f); if (boss) { h = 650 + lv * 450; }
            return new Foe { Type = type, Boss = boss, Kind = lv % 3, X = x, Y = G, Hp = h, Mh = h, Sp = boss ? 150 + lv * 15 : sp[type], S = boss ? 1.9f : 1, F = -1, Tm = .6f, Ph = x * .013f };
        }

        public static Level Make(int lv)
        {
            var r = new Random(1000 + lv * 77); var L = new Level { Th = Themes[lv % 3], Num = lv };
            float x = 700; L.Checks.Add(250); int chunks = 6 + lv * 2;
            void Pack(float px, int n)
            {
                for (int j = 0; j < n; j++)
                {
                    int roll = r.Next(10); int ty = roll < 5 - lv ? 0 : roll < 8 ? 1 : 2; if (lv == 0 && ty == 1 && j == 0) ty = 0;
                    L.Foes.Add(Mk(ty, px + j * 140 + r.Next(60), lv));
                }
            }
            for (int i = 0; i < chunks; i++)
            {
                int k = i == 0 ? 0 : r.Next(5);
                switch (k)
                {
                    case 0: Pack(x + 350, 2 + r.Next(2) + lv); x += 1100; break;
                    case 1:
                        { float gw = 240 + r.Next(130); L.Gaps.Add(new Vector2(x, x + gw)); L.Plats.Add(new Plat { X = x + gw / 2 - 90, Y = G - 150, W = 180 }); L.Souls.Add(new Soul { X = x + gw / 2, Y = G - 220 }); x += gw + 550; L.Foes.Add(Mk(2, x - 100, lv)); break; }
                    case 2:
                        { float sw = 240 + r.Next(140); L.Spikes.Add(new Vector2(x, x + sw)); L.Plats.Add(new Plat { X = x + sw / 2 - 110, Y = G - 170, W = 220 }); L.Souls.Add(new Soul { X = x + sw / 2, Y = G - 240 }); x += sw + 600; L.Foes.Add(Mk(0, x - 150, lv)); break; }
                    case 3:
                        { L.Plats.Add(new Plat { X = x, Y = G - 120, W = 150 }); L.Plats.Add(new Plat { X = x + 230, Y = G - 230, W = 150 }); L.Plats.Add(new Plat { X = x + 460, Y = G - 300, W = 170 }); L.Plats.Add(new Plat { X = x + 700, Y = G - 200, W = 150 });
                            L.Souls.Add(new Soul { X = x + 545, Y = G - 370 }); L.Souls.Add(new Soul { X = x + 300, Y = G - 300 }); Pack(x + 300, 2 + lv); x += 1000; break; }
                    default: Pack(x + 300, 3 + lv + r.Next(2)); x += 1200; break;
                }
                if (i % 3 == 2) { L.Checks.Add(x + 100); x += 400; }
            }
            L.Checks.Add(x + 100); float bx = x + 1200; L.GateX = x + 2100;
            L.Foes.Add(Mk(0, bx, lv, true));
            foreach (var f in L.Foes)
            {
                float a = 0, b = L.GateX + 600;
                foreach (var g in L.Gaps) { if (g.Y <= f.X) a = Math.Max(a, g.Y); else if (g.X >= f.X) { b = Math.Min(b, g.X); } }
                f.A = a; f.B = b;
            }
            return L;
        }
    }
}
