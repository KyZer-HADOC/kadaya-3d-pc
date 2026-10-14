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
        public Theme Th; public int Num; public MissionDefinition Mission;
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

        static Foe Mk(int type, float x, int difficulty, int bossKind, bool boss = false)
        {
            float[] hp = { 40, 100, 34 }, sp = { 250, 120, 130 };
            float h = hp[type] * (1 + Math.Max(0, difficulty - 1) * .22f); if (boss) { h = 650 + difficulty * 390; }
            return new Foe { Type = type, Boss = boss, Kind = boss ? bossKind : (difficulty + type) % 3, X = x, Y = G, Hp = h, Mh = h, Sp = boss ? 150 + difficulty * 5 : sp[type], S = boss ? 1.9f : 1, F = -1, Tm = .6f, Ph = x * .013f };
        }

        public static Level Make(int lv)
        {
            MissionDefinition mission = MissionCatalog.Get(lv);
            var r = new Random(1000 + lv * 77);
            var L = new Level { Th = Themes[mission.MapIndex], Num = lv, Mission = mission };
            int difficulty = mission.Difficulty;
            float x = 700; L.Checks.Add(250);
            void Pack(float px, int n)
            {
                for (int j = 0; j < n; j++)
                {
                    int roll = r.Next(10); int ty = roll < 4 ? 0 : roll < 7 ? 1 : 2;
                    if (lv == 0 && j == 0) ty = 0;
                    L.Foes.Add(Mk(ty, px + j * 140 + r.Next(60), difficulty, mission.BossKind));
                }
            }
            string route = mission.RouteCode.Substring(2);
            for (int i = 0; i < route.Length; i++)
            {
                switch (route[i])
                {
                    case 'E':
                        Pack(x + 280, 2 + difficulty / 6 + r.Next(2)); x += 860 + r.Next(120); break;
                    case 'G':
                        {
                            float gw = 230 + r.Next(150); float platformY = G - 125 - (mission.MapIndex == 2 ? 55 : 0);
                            L.Gaps.Add(new Vector2(x, x + gw));
                            L.Plats.Add(new Plat { X = x + gw / 2 - 95, Y = platformY, W = 190 });
                            L.Souls.Add(new Soul { X = x + gw / 2, Y = platformY - 75 });
                            x += gw + 520; L.Foes.Add(Mk(2, x - 95, difficulty, mission.BossKind)); break;
                        }
                    case 'S':
                        {
                            float sw = 230 + r.Next(160); float platformY = G - 145 - (mission.MapIndex == 1 ? 35 : 0);
                            L.Spikes.Add(new Vector2(x, x + sw));
                            L.Plats.Add(new Plat { X = x + sw / 2 - 108, Y = platformY, W = 216 });
                            L.Souls.Add(new Soul { X = x + sw / 2, Y = platformY - 78 });
                            x += sw + 560; L.Foes.Add(Mk(0, x - 135, difficulty, mission.BossKind)); break;
                        }
                    case 'C':
                        {
                            int rise = 150 + r.Next(110); int count = 3 + r.Next(2);
                            for (int step = 0; step < count; step++)
                            {
                                float px = x + step * 205, py = G - 90 - step * rise / Math.Max(1, count - 1);
                                L.Plats.Add(new Plat { X = px, Y = py, W = 150 + r.Next(35) });
                                if (step > 0 && step < count - 1) L.Souls.Add(new Soul { X = px + 65, Y = py - 72 });
                            }
                            Pack(x + 310, 1 + difficulty / 7); x += count * 205 + 330; break;
                        }
                    case 'A':
                        {
                            int count = 2 + difficulty / 7;
                            Pack(x + 250, count);
                            L.Foes.Add(Mk((difficulty + 1) % 3, x + 250 + count * 140 + 45, difficulty, mission.BossKind));
                            x += 900 + count * 105; break;
                        }
                    case 'R':
                        {
                            float baseY = G - (115 + mission.MapIndex * 25);
                            L.Plats.Add(new Plat { X = x + 180, Y = baseY, W = 180 });
                            L.Plats.Add(new Plat { X = x + 470, Y = baseY - 115, W = 190 });
                            L.Souls.Add(new Soul { X = x + 260, Y = baseY - 75 });
                            L.Souls.Add(new Soul { X = x + 555, Y = baseY - 190 });
                            Pack(x + 760, 1 + difficulty / 8); x += 1120; break;
                    }
                }
                if ((i + 1) % mission.CheckpointInterval == 0 && i + 1 < route.Length) { L.Checks.Add(x + 80); x += 160; }
            }
            L.Checks.Add(x + 80); float bx = x + 650; L.GateX = x + 1480;
            L.Foes.Add(Mk(0, bx, difficulty, mission.BossKind, true));
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
