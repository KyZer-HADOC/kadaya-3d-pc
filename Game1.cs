using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kadaya
{
    class Foe { public int Type, F, St, Kind; public bool Boss, Dead, Did, Aggro; public float X, Y, Vx, Vy, Hp, Mh, Sp, S, Tm, Flash, Walk, Mv, A, B, Ph, Cnt, Air, Al = 1; }
    class Proj { public string K; public bool Own, Spent; public float X, Y, Vx, Vy, L, R, Dmg, T0; }
    class Clone { public float X, L, Cd, At = -1, T, Mv; public int F; }
    class Part { public float X, Y, Vx, Vy, L, M, Sz, G; public Color C; public bool Dk; }
    class Fx { public string K; public float X, Y, R, L, M, A; public int F; public Color C; public bool E; }
    class Trl { public Vector2 A, B; public float L; public bool Brk; }
    class Pl { public float X, Y, Vx, Vy, Hp, Mh = 100, En, CbT, Atk, AtkT, Inv, Dash, Dcd, Chg, Kcd, Coy; public int F, J, Cb; public bool Hit, Dead, Run, Ground; }

    public partial class Game1 : Game
    {
        const int W = 1280, H = 720; const float G = 600;
        static readonly Color CY = new Color(95, 243, 255), PU = new Color(138, 92, 255), VI = new Color(192, 107, 255), RD = new Color(255, 45, 85), OR = new Color(255, 179, 71), WH = Color.White;

        GraphicsDeviceManager gdm; SpriteBatch sb; RenderTarget2D rt, small; Texture2D px, circ, glow, tri, cres, vig; SpriteFont font;
        static Random rnd = new Random();
        KeyboardState cur, prev;

        string state = "title", banS = "";
        float t, shake, hitstop, cam, camY, flash, ult, banT, winT; bool ultHit = true, bossDead;
        int lv, kills; Level L; float checkX; bool[] lit;
        Pl P = new Pl();
        List<Foe> en = new List<Foe>(); List<Proj> proj = new List<Proj>(); List<Clone> clones = new List<Clone>();
        List<Part> pt = new List<Part>(); List<Fx> fx = new List<Fx>(); List<Trl> trail = new List<Trl>();
        float[,] stars = new float[140, 4]; float[,] amb = new float[120, 5];

        public Game1()
        {
            gdm = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = W, PreferredBackBufferHeight = H };
            Content.RootDirectory = "Content"; IsMouseVisible = false; Window.AllowUserResizing = true; Window.Title = "KADAYA";
        }

        static float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static float Cl(float v, float a, float b) => Math.Max(a, Math.Min(b, v));
        static int Sg(float v) => v >= 0 ? 1 : -1;
        static Color Ad(Color c, float k) => new Color(c.R / 255f * k, c.G / 255f * k, c.B / 255f * k, 0f);
        static Color Pm(float a) => new Color(a, a, a, a);

        protected override void LoadContent()
        {
            sb = new SpriteBatch(GraphicsDevice); font = Content.Load<SpriteFont>("Font");
            rt = new RenderTarget2D(GraphicsDevice, W, H, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents); small = new RenderTarget2D(GraphicsDevice, 320, 180);
            px = new Texture2D(GraphicsDevice, 1, 1); px.SetData(new[] { Color.White });
            circ = Make(64, (u, v) => { float r = MathF.Sqrt(u * u + v * v); return Pm(Cl((1 - r) * 32, 0, 1)); });
            glow = Make(128, (u, v) => { float r = MathF.Sqrt(u * u + v * v); return Pm(MathF.Pow(Cl(1 - r, 0, 1), 2.2f)); });
            tri = Make(64, (u, v) => Pm((v > -1 && MathF.Abs(u) <= (v + 1) / 2) ? 1 : 0));
            cres = Make(256, (u, v) => { float ao = Cl((1 - MathF.Sqrt(u * u + v * v)) * 128, 0, 1); float ai = Cl((.9f - MathF.Sqrt((u - .42f) * (u - .42f) + v * v)) * 128, 0, 1); return Pm(ao * (1 - ai)); });
            vig = Make(256, (u, v) => { float r = MathF.Sqrt(u * u + v * v); return new Color(0, 0, 0, Cl(MathF.Pow(Cl((r - .42f) / .9f, 0, 1), 1.5f) * .95f, 0, 1)); });
            for (int i = 0; i < 140; i++) { stars[i, 0] = Rf(0, W); stars[i, 1] = Rf(0, 420); stars[i, 2] = Rf(.5f, 2); stars[i, 3] = Rf(0, 6); }
            for (int i = 0; i < 120; i++) { amb[i, 0] = Rf(0, W); amb[i, 1] = Rf(0, H); amb[i, 2] = Rf(-20, 20); amb[i, 3] = Rf(15, 60); amb[i, 4] = Rf(1.5f, 4); }
            Init3D(); lv = LoadLv(); LoadLevel(lv, 250); state = "title";
        }
        Texture2D Make(int n, Func<float, float, Color> f)
        {
            var tx = new Texture2D(GraphicsDevice, n, n); var c = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) c[y * n + x] = f((x + .5f) / n * 2 - 1, (y + .5f) / n * 2 - 1);
            tx.SetData(c); return tx;
        }

        // ---------- save ----------
        string SavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kadaya", "save.txt");
        void SaveLv(int v = -1) { try { Directory.CreateDirectory(Path.GetDirectoryName(SavePath)); File.WriteAllText(SavePath, (v < 0 ? lv : v).ToString()); } catch { } }
        int LoadLv() { try { return Math.Max(0, Math.Min(2, int.Parse(File.ReadAllText(SavePath)))); } catch { return 0; } }

        void LoadLevel(int n, float sx)
        {
            lv = n; L = LevelGen.Make(n); L.Foes.RemoveAll(f => f.X < sx + 150 && !f.Boss); en = L.Foes;
            proj.Clear(); clones.Clear(); pt.Clear(); fx.Clear(); trail.Clear();
            P = new Pl { X = sx, Y = G, F = 1, Hp = 100, En = 50 }; checkX = sx; lit = new bool[L.Checks.Count]; for (int i = 0; i < lit.Length; i++) lit[i] = L.Checks[i] <= sx;
            bossDead = false; ult = 0; ultHit = true; cam = Cl(sx - W * .35f, 0, L.GateX + 400 - W); camY = 0; banS = "LEVEL " + (n + 1) + "  -  " + L.Th.Name; banT = 3.2f;
            PlaySong(L.Th.Style);
        }

        // ---------- world queries ----------
        bool InGap(float x) { foreach (var g in L.Gaps) if (x > g.X && x < g.Y) return true; return false; }
        float FloorBelow(float x, float y) { float best = InGap(x) ? 99999 : G; foreach (var p in L.Plats) if (x >= p.X && x <= p.X + p.W && p.Y >= y - 4 && p.Y < best) best = p.Y; return best; }

        // ---------- effects ----------
        void Burst(float X, float Y, int n, Color col, float sp = 300, float sz = 3, float g = 600, float life = .6f)
        { for (int i = 0; i < n && pt.Count < 2200; i++) { float a = Rf(0, 6.283f), s = Rf(.2f, 1) * sp; pt.Add(new Part { X = X, Y = Y, Vx = MathF.Cos(a) * s, Vy = MathF.Sin(a) * s, L = Rf(.4f, 1) * life, M = life, Sz = Rf(.5f, 1) * sz, C = col, G = g }); } }
        void Arc(float X, float Y, int f, float r, Color c, float l = .22f, float a = 0) => fx.Add(new Fx { K = "arc", X = X, Y = Y, F = f, R = r, L = l, M = l, C = c, A = a });
        void Ring(float X, float Y, float r, Color c, float l = .5f) => fx.Add(new Fx { K = "ring", X = X, Y = Y, R = r, L = l, M = l, C = c });
        void Smoke(float X, float Y, float vx) { if (pt.Count < 2200) pt.Add(new Part { X = X, Y = Y, Vx = vx + Rf(-20, 20), Vy = Rf(-70, -15), L = Rf(.6f, 1.1f), M = 1.1f, Sz = Rf(12, 24), C = new Color(10, 5, 18), G = -30, Dk = true }); }

        // ---------- combat ----------
        void Die()
        {
            if (P.Dead) return; P.Dead = true; P.Hp = 0; state = "dead"; Burst(P.X, P.Y - 60, 90, VI, 650, 6, 200, 1.7f); for (int i = 0; i < 18; i++) Smoke(P.X, P.Y - 40, Rf(-120, 120)); shake = 30; Sfx("kill", P.X); if (musInst != null) musInst.Volume = .15f;
        }
        void HurtP(float d, int dir)
        {
            if (P.Inv > 0 || P.Dash > 0 || P.Dead || ult > 0) return;
            P.Hp -= d; P.Inv = .9f; P.Vx = dir * 380; P.Vy = -300; P.Ground = false; P.Cb = 0; P.Atk = 0; P.Chg = 0; shake = 16; flash = .35f; hitstop = .06f;
            Burst(P.X, P.Y - 70, 24, RD, 420, 4); Sfx("hurt", P.X); if (P.Hp <= 0) Die();
        }
        void HitE(Foe e, float d, float kx)
        {
            if (e.Dead) return;
            e.Aggro = true; e.Hp -= d; e.Flash = .1f; e.Vx = kx * (e.Boss ? .15f : 1); if (!e.Boss) e.Vy = -220; if (e.St == 1 && !e.Boss) { e.St = 3; e.Tm = .5f; }
            P.En = Math.Min(100, P.En + d * .3f); hitstop = Math.Max(hitstop, .05f); shake = Math.Max(shake, d > 30 ? 18 : 8);
            Burst(e.X, e.Y - 60 * e.S, 16, WH, 450, 3, 500, .35f); Burst(e.X, e.Y - 60 * e.S, 12, RD, 320, 4, 700, .5f); Sfx("hit", e.X);
            if (e.Hp <= 0)
            {
                e.Dead = true; kills++; Burst(e.X, e.Y - 60 * e.S, 30, VI, 160, 6, -260, 1.5f); Burst(e.X, e.Y - 60 * e.S, 34, WH, 520, 3, 400, .5f);
                Ring(e.X, e.Y - 60, 100, VI, .6f); shake = Math.Max(shake, e.Boss ? 38 : 14); Sfx("kill", e.X);
                if (e.Boss) { bossDead = true; hitstop = .4f; flash = .9f; banS = "BOSS DEFEATED - THE SOUL GATE OPENS"; banT = 3.5f; Ring(e.X, e.Y - 100, 900, OR, 1f); Sfx("boom", e.X); }
            }
        }
        void SlashHit()
        {
            float r = P.Cb == 2 ? 240 : 185, dm = new[] { 12f, 14f, 28f }[P.Cb];
            Arc(P.X, P.Y - 70, P.F, r * (P.Cb == 2 ? 1.1f : .9f), P.Cb == 2 ? VI : CY, .25f, new[] { -.35f, .35f, 0f }[P.Cb]);
            if (P.Cb == 2) { Ring(P.X, P.Y - 60, 190, VI, .4f); Burst(P.X + P.F * 90, P.Y - 70, 28, VI, 520, 4); }
            Sfx(P.Cb == 2 ? "slash3" : "slash", P.X);
            foreach (var e in en) { float dx = (e.X - P.X) * P.F; if (dx > -40 && dx < r + 20 * e.S && Math.Abs(e.Y - P.Y) < 160) HitE(e, dm * (1 + lv * .05f), P.F * (P.Cb == 2 ? 700 : 380)); }
            foreach (var p in proj) if (!p.Own && p.K != "pillar" && Math.Abs(p.X - P.X) < r && Math.Abs(p.Y - (P.Y - 70)) < 120) { p.L = 0; P.En = Math.Min(100, P.En + 6); Burst(p.X, p.Y, 14, CY, 300, 3); }
        }
        void Explode(Proj p)
        {
            float R = 210 * p.R / 30; Ring(p.X, p.Y, R, CY, .5f); Ring(p.X, p.Y, R * .6f, WH, .35f); Burst(p.X, p.Y, 80, CY, 800, 5, 100, .8f); Burst(p.X, p.Y, 30, WH, 600, 3, 0, .5f);
            shake = 24; flash = .2f; hitstop = .08f; Sfx("ras", p.X);
            foreach (var e in en) if (Math.Abs(e.X - p.X) < R + 20 && Math.Abs(e.Y - p.Y) < R) HitE(e, p.Dmg, Sg(e.X - p.X) * 600);
        }
        void Wave(float x, int dir, float spd = 480) => proj.Add(new Proj { K = "wave", X = x, Y = G - 30, Vx = dir * spd, L = 2.2f, R = 34, Dmg = 16 });
        void Slam(Foe e)
        {
            Wave(e.X, 1); Wave(e.X, -1); Ring(e.X, G - 10, 280, e.Kind == 2 ? OR : RD, .5f); Burst(e.X, G - 10, 40, OR, 500, 5, 500, .6f); shake = 24; Sfx("boom", e.X);
            if (Math.Abs(P.X - e.X) < 180 && P.Ground) HurtP(22, Sg(P.X - e.X));
        }

        // ---------- enemies ----------
        void UpdE(Foe e, float dt)
        {
            float d = P.X - e.X, ad = Math.Abs(d); e.Ph += dt; e.Flash -= dt;
            if (!e.Aggro) { if (ad < (e.Boss ? 950 : 680) && !P.Dead) e.Aggro = true; else { e.F = d > 0 ? 1 : -1; return; } }
            if (e.St != 2 && e.St != 5) e.F = d > 0 ? 1 : -1;
            e.Vy += 2300 * dt; e.Y += e.Vy * dt; if (e.Y >= G) { e.Y = G; if (e.Vy > 0) e.Vy = 0; }
            e.X = Cl(e.X + e.Vx * dt, e.A + 30, e.B - 30); if (e.St != 2) e.Vx *= MathF.Pow(.02f, dt);
            e.Tm -= dt; e.Mv = 0; int T = e.Type;
            if (e.Boss) { UpdBoss(e, dt, d, ad); return; }
            if (e.St == 0)
            {
                if (T == 2) { int w = ad < 360 ? -1 : ad > 560 ? 1 : 0; e.X = Cl(e.X + w * e.F * e.Sp * dt, e.A + 30, e.B - 30); e.Mv = w; if (e.Tm <= 0 && ad < 720) { e.St = 1; e.Tm = .6f; Sfx("tele", e.X); } }
                else
                {
                    float rg = T == 1 ? 125 : 260;
                    if (ad > (T == 1 ? 100 : 120) && (ad > rg || e.Tm > 0)) { e.X = Cl(e.X + e.F * e.Sp * dt, e.A + 30, e.B - 30); e.Mv = 1; }
                    if (e.Tm <= 0 && ad <= rg) { e.St = 1; e.Tm = T == 1 ? .65f : .4f; Sfx("tele", e.X); }
                }
            }
            else if (e.St == 1 && e.Tm <= 0)
            {
                if (T == 0) { e.St = 2; e.Air = 0; e.Vy = -680; e.Vx = Cl(d / .59f, -720, 720); Sfx("dash", e.X); }
                else if (T == 1)
                {
                    Arc(e.X + e.F * 20, e.Y - 70, e.F, 130, new Color(255, 64, 32), .22f); Sfx("slash", e.X);
                    if (ad < 160 && Math.Abs(P.Y - e.Y) < 130) HurtP(16, e.F); e.St = 3; e.Tm = .8f;
                }
                else
                {
                    float a = MathF.Atan2((P.Y - 70) - (e.Y - 80), P.X - e.X); proj.Add(new Proj { K = "arrow", X = e.X + e.F * 30, Y = e.Y - 80, Vx = MathF.Cos(a) * 640, Vy = MathF.Sin(a) * 640, L = 2.5f, R = 10, Dmg = 10 });
                    e.St = 3; e.Tm = .7f; Sfx("arrow", e.X);
                }
            }
            else if (e.St == 2)
            {
                e.Air += dt; fx.Add(new Fx { K = "ghost", X = e.X, Y = e.Y, F = e.F, L = .2f, M = .2f, C = RD, E = true });
                if (!e.Did && ad < 70 && Math.Abs(P.Y - e.Y) < 130) { e.Did = true; HurtP(13, e.F); }
                if (e.Air > .15f && e.Y >= G) { e.St = 3; e.Tm = .6f; e.Vx = 0; e.Did = false; }
            }
            else if (e.St == 3 && e.Tm <= 0) { e.St = 0; e.Tm = Rf(.7f, 1.5f); }
            e.Walk += dt * (e.Mv != 0 ? 1 : 0);
        }
        void UpdBoss(Foe e, float dt, float d, float ad)
        {
            int k = e.Kind;
            if (e.St == 0)
            {
                float rg = k == 1 ? 470 : 200; if (ad > rg) { e.X = Cl(e.X + e.F * e.Sp * dt, e.A + 30, e.B - 30); e.Mv = 1; } else if (k == 1 && ad < 230) { e.X = Cl(e.X - e.F * e.Sp * .8f * dt, e.A + 30, e.B - 30); e.Mv = -1; }
                if (e.Tm <= 0) { e.St = 1; e.Cnt++; e.Tm = .85f; Sfx("tele", e.X); }
            }
            else if (e.St == 1 && e.Tm <= 0)
            {
                int v = (int)e.Cnt % 3;
                if (k == 0) { if (v == 0) { e.Vy = -950; e.Vx = Cl(d * 1.1f, -650, 650); e.St = 2; e.Air = 0; Sfx("slash3", e.X); } else { Slam(e); e.St = 3; e.Tm = 1.1f; } }
                else if (k == 1)
                {
                    if (v == 0) { e.St = 4; e.Tm = .45f; Sfx("tele", e.X); }
                    else { for (int i = -2; i <= 2; i++) { float a = MathF.Atan2((P.Y - 70) - (e.Y - 110), P.X - e.X) + i * .22f; proj.Add(new Proj { K = "orb", X = e.X + e.F * 30, Y = e.Y - 110, Vx = MathF.Cos(a) * 430, Vy = MathF.Sin(a) * 430, L = 3, R = 16, Dmg = 12 }); } Sfx("arrow", e.X); e.St = 3; e.Tm = 1.2f; }
                }
                else
                {
                    if (v == 0) { Wave(e.X, 1, 600); Wave(e.X, -1, 600); Ring(e.X, G, 300, OR, .5f); shake = 18; Sfx("boom", e.X); e.St = 3; e.Tm = 1.1f; }
                    else if (v == 1) { for (int i = -2; i <= 2; i++) proj.Add(new Proj { K = "pillar", X = P.X + i * 140, Y = G, L = 1.7f, T0 = .95f + Math.Abs(i) * .08f, R = 40, Dmg = 20 }); Sfx("tele", e.X); e.St = 3; e.Tm = 1.4f; }
                    else { e.Vy = -950; e.Vx = Cl(d * 1.1f, -650, 650); e.St = 2; e.Air = 0; Sfx("slash3", e.X); }
                }
            }
            else if (e.St == 2) { e.Air += dt; if (e.Air > .25f && e.Y >= G) { Slam(e); e.St = 3; e.Tm = 1f; e.Vx = 0; } }
            else if (e.St == 4) { e.Al = Math.Max(0, e.Al - dt * 4); if (e.Tm <= 0) { e.X = Cl(P.X - P.F * 150, e.A + 30, e.B - 30); e.F = P.F; e.St = 5; e.Tm = .4f; e.Al = 1; Burst(e.X, e.Y - 80, 36, VI, 400, 5, 0, .6f); } }
            else if (e.St == 5 && e.Tm <= 0) { Arc(e.X + e.F * 20, e.Y - 90, e.F, 200, VI, .25f); Sfx("slash3", e.X); if (Math.Abs(P.X - e.X) < 220 && Math.Abs(P.Y - e.Y) < 140) HurtP(22, e.F); e.St = 3; e.Tm = .9f; }
            else if (e.St == 3 && e.Tm <= 0) { e.St = 0; e.Tm = Rf(.4f, .9f); }
            e.Walk += dt * (e.Mv != 0 ? 1 : 0);
        }

        // ---------- player update ----------
        bool Dn(Keys k) => cur.IsKeyDown(k);
        bool Pr(Keys k) => cur.IsKeyDown(k) && prev.IsKeyUp(k);

        void UpdatePlay(float dt)
        {
            if (banT > 0) banT -= dt;
            bool Lf = Dn(Keys.A) || Dn(Keys.Left), Rt = Dn(Keys.D) || Dn(Keys.Right); int dir = (Rt ? 1 : 0) - (Lf ? 1 : 0);
            if (!P.Dead)
            {
                P.Inv -= dt; P.Dcd -= dt; P.Kcd -= dt; P.CbT -= dt; bool was = P.Ground; float pvy = P.Vy;
                if (P.Dash > 0) { P.Dash -= dt; P.X += P.F * 1200 * dt; P.Vy = 0; fx.Add(new Fx { K = "ghost", X = P.X, Y = P.Y, F = P.F, L = .3f, M = .3f, C = PU }); }
                else
                {
                    float sl = P.Chg > 0 ? .35f : P.Atk > 0 ? .65f : 1; P.Vx += (dir * 420 * sl - P.Vx) * Math.Min(1, dt * (P.Ground ? 14 : 6)); if (dir != 0 && P.Atk <= 0 && P.Chg <= 0) P.F = dir;
                    if (P.Ground) P.Coy = .1f; else { P.Coy -= dt; if (P.Coy <= 0 && P.J == 0) P.J = 1; }
                    if ((Pr(Keys.Space) || Pr(Keys.W) || Pr(Keys.Up)) && P.J < 2) { P.Vy = -850; P.J++; P.Ground = false; Burst(P.X, P.Y, 14, PU, 240, 3, 300, .4f); Sfx("jump", P.X); }
                    float prevY = P.Y; P.Vy += 2300 * dt; P.X += P.Vx * dt; P.Y += P.Vy * dt; P.Ground = false;
                    if (P.Vy >= 0)
                    {
                        float best = 1e9f; if (!InGap(P.X) && prevY <= G + 1 && P.Y >= G) best = G;
                        foreach (var pl in L.Plats) if (P.X >= pl.X && P.X <= pl.X + pl.W && prevY <= pl.Y + 1 && P.Y >= pl.Y && pl.Y < best) best = pl.Y;
                        if (best < 1e8f) { P.Y = best; P.Vy = 0; P.Ground = true; P.J = 0; }
                    }
                }
                if (P.Ground && !was && pvy > 450) { Sfx("land", P.X); for (int i = 0; i < 6; i++) Smoke(P.X + Rf(-20, 20), P.Y - 4, Rf(-80, 80)); }
                if (P.Y > G + 380) { Sfx("fall", P.X); Die(); }
                P.X = Cl(P.X, 40, bossDead ? L.GateX + 60 : L.GateX - 90); P.Run = P.Ground && Math.Abs(P.Vx) > 60;
                if (P.Ground && P.Y >= G - 6) foreach (var sp in L.Spikes) if (P.X > sp.X && P.X < sp.Y) { HurtP(30, -P.F); P.Vy = -620; }
                if (P.Run || P.Dash > 0 || P.Atk > 0) { if (rnd.NextDouble() < .6) Smoke(P.X - P.F * Rf(10, 34), P.Y - Rf(0, 50), -P.F * Rf(20, 70)); }
                if ((Pr(Keys.LeftShift) || Pr(Keys.RightShift)) && P.Dcd <= 0) { P.Dash = .17f; P.Dcd = .65f; P.Inv = Math.Max(P.Inv, .3f); Burst(P.X, P.Y - 60, 22, PU, 300, 4); Sfx("dash", P.X); }
                if (P.Atk > 0)
                {
                    float dur = P.Cb == 2 ? .42f : .28f; P.AtkT += dt; P.Atk -= dt; float atp = P.AtkT / dur;
                    var pts = ScytheGeom(P.X, P.Y, P.F, 1, ScytheAng(atp, P.Cb, false, false, false, t), atp, out _, out _, out _);
                    trail.Add(new Trl { A = pts[6], B = pts[12], L = .3f, Brk = P.AtkT <= dt * 1.5f });
                    if (rnd.NextDouble() < .8) pt.Add(new Part { X = pts[12].X, Y = pts[12].Y, Vx = Rf(-40, 40), Vy = Rf(-80, -20), L = .5f, M = .5f, Sz = 5, C = P.Cb == 2 ? VI : CY, G = -60 });
                    if (P.Hit && P.AtkT > (P.Cb == 2 ? .17f : .11f)) { P.Hit = false; SlashHit(); }
                }
                if (Pr(Keys.J) && P.Dash <= 0 && P.Chg <= 0 && (P.Atk <= 0 || P.AtkT > .18f)) { P.Cb = P.CbT > 0 ? (P.Cb + 1) % 3 : 0; P.Atk = P.Cb == 2 ? .42f : .28f; P.AtkT = 0; P.Hit = true; P.CbT = .75f; P.Vx = P.F * (P.Cb == 2 ? 450 : 280); }
                if (Pr(Keys.K) && P.Kcd <= 0 && P.Dash <= 0) { P.Kcd = .2f; foreach (var a in new[] { -.12f, 0f, .12f }) proj.Add(new Proj { Own = true, K = "kunai", X = P.X + P.F * 30, Y = P.Y - 72, Vx = P.F * 1150, Vy = a * 1150 * .6f, Dmg = 9, L = 1.1f, R = 8 }); Sfx("kunai", P.X); }
                if (Dn(Keys.L) && P.En >= 25 && P.Atk <= 0 && P.Dash <= 0)
                {
                    P.Chg = Math.Min(1.5f, P.Chg + dt); float a = Rf(0, 6.283f), r = Rf(60, 120);
                    pt.Add(new Part { X = P.X + P.F * 45 + MathF.Cos(a) * r, Y = P.Y - 75 + MathF.Sin(a) * r, Vx = -MathF.Cos(a) * r * 4, Vy = -MathF.Sin(a) * r * 4, L = .25f, M = .25f, Sz = 3, C = CY });
                }
                else if (P.Chg > 0)
                {
                    if (P.Chg > .3f && P.En >= 25) { float sz = .6f + P.Chg * .8f; proj.Add(new Proj { Own = true, K = "ras", X = P.X + P.F * 60, Y = P.Y - 75, Vx = P.F * 560, R = 30 * sz, Dmg = 32 + P.Chg * 22, L = 1.5f }); P.En -= 25; shake = 10; Sfx("ras", P.X); }
                    P.Chg = 0;
                }
                if (Pr(Keys.E) && P.En >= 40) { P.En -= 40; foreach (var s in new[] { -1, 1 }) { clones.Add(new Clone { X = P.X + s * 90, F = P.F, L = 7, Cd = .2f }); Burst(P.X + s * 90, G - 60, 30, new Color(223, 233, 255), 300, 6, -100, .7f); } Sfx("clone", P.X); }
                if (Pr(Keys.Q) && P.En >= 100 && ult <= 0) { P.En = 0; ult = 1.4f; ultHit = false; P.Inv = 2; Sfx("ult", P.X); }
                // checkpoints
                for (int i = 0; i < L.Checks.Count; i++) if (!lit[i] && P.X > L.Checks[i]) { lit[i] = true; checkX = L.Checks[i]; P.Hp = Math.Min(P.Mh, P.Hp + 40); Burst(checkX, G - 60, 50, L.Th.Accent, 400, 5, -200, 1.2f); Ring(checkX, G - 60, 200, L.Th.Accent, .8f); Sfx("check", checkX); banS = "CHECKPOINT"; banT = 1.6f; }
                // soul pickups
                foreach (var s in L.Souls) if (!s.Got && Math.Abs(P.X - s.X) < 46 && Math.Abs(P.Y - 60 - s.Y) < 90) { s.Got = true; P.En = Math.Min(100, P.En + 15); Burst(s.X, s.Y, 20, VI, 300, 4, -100, .6f); Sfx("soul", s.X); }
                // gate
                if (bossDead && P.X >= L.GateX - 20) { state = "win"; winT = 0; SaveLv(Math.Min(2, lv + 1)); Sfx("win", P.X); }
            }
            if (ult > 0)
            {
                ult -= dt;
                if (!ultHit && ult < 1)
                {
                    ultHit = true; flash = 1; shake = 45; foreach (var e in en) if (Math.Abs(e.X - P.X) < 1100) HitE(e, e.Boss ? 120 : 95, Sg(e.X - P.X) * 500);
                    for (int i = 0; i < 6; i++) Arc(P.X + (i - 2.5f) * 220, P.Y - 130, i % 2 == 0 ? -1 : 1, 300 + i * 20, VI, .5f, Rf(-.6f, .6f));
                    Ring(P.X, P.Y - 80, 900, VI, .8f); Burst(P.X, P.Y - 80, 160, VI, 1200, 6, 0, 1);
                }
            }
            if (ult <= .9f) foreach (var e in en) UpdE(e, dt);
            en.RemoveAll(e => e.Dead);
            foreach (var k in clones)
            {
                k.L -= dt; k.T += dt; k.Cd -= dt; k.Mv = 0; Foe tg = null; float bd = 9e9f;
                foreach (var e in en) { float d = Math.Abs(e.X - k.X); if (d < bd) { bd = d; tg = e; } }
                if (tg != null && bd < 900) { k.F = Sg(tg.X - k.X); if (bd > 100) { k.X += k.F * 330 * dt; k.Mv = 1; } else if (k.Cd <= 0) { k.Cd = .55f; k.At = 0; Arc(k.X + k.F * 30, G - 70, k.F, 130, new Color(223, 233, 255), .2f); HitE(tg, 10, k.F * 300); } }
                else { k.F = Sg(P.X - k.X); if (Math.Abs(P.X - k.X) > 140) { k.X += k.F * 330 * dt; k.Mv = 1; } }
                if (k.At >= 0) { k.At += dt; if (k.At > .28f) k.At = -1; }
                if (k.L <= 0) Burst(k.X, G - 60, 25, new Color(223, 233, 255), 250, 5, -100, .6f);
            }
            clones.RemoveAll(k => k.L <= 0);
            foreach (var p in proj)
            {
                p.L -= dt;
                if (p.K == "pillar") { p.T0 -= dt; if (p.T0 <= 0 && p.T0 > -.45f && Math.Abs(P.X - p.X) < 45 && P.Y > G - 320) HurtP(p.Dmg, Sg(P.X - p.X)); continue; }
                p.X += p.Vx * dt; p.Y += p.Vy * dt;
                if (p.Own)
                {
                    pt.Add(new Part { X = p.X, Y = p.Y, Vx = Rf(-30, 30), Vy = Rf(-30, 30), L = .3f, M = .3f, Sz = p.K == "ras" ? p.R * .5f : 3, C = CY });
                    foreach (var e in en)
                        if (!e.Dead && p.L > 0 && Math.Abs(p.X - e.X) < 26 * e.S + p.R && p.Y > e.Y - 125 * e.S && p.Y < e.Y + 10)
                        { if (p.K == "ras") { Explode(p); p.Spent = true; } else HitE(e, p.Dmg, Sg(p.Vx) * 250); p.L = 0; }
                }
                else
                {
                    if (p.K == "wave") Burst(p.X, G - 5, 2, new Color(255, 64, 32), 100, 5, -300, .4f);
                    if (Math.Abs(p.X - P.X) < p.R + 16 && p.Y > P.Y - 115 && p.Y < P.Y + 10 && p.L > 0) { HurtP(p.Dmg, Sg(p.Vx)); p.L = 0; }
                }
                if (p.X < -100 || p.X > L.GateX + 900 || p.Y > G + 50) p.L = 0;
            }
            foreach (var p in proj) if (p.L <= 0 && p.K == "ras" && !p.Spent) { p.Spent = true; Explode(p); }
            proj.RemoveAll(p => p.L <= 0);
        }

        void UpdFx(float dt)
        {
            foreach (var p in pt) { p.L -= dt; p.Vy += p.G * dt; p.X += p.Vx * dt; p.Y += p.Vy * dt; if (p.Dk) { p.Vx *= .98f; p.Sz += dt * 8; } }
            pt.RemoveAll(p => p.L <= 0); foreach (var f in fx) f.L -= dt; fx.RemoveAll(f => f.L <= 0); foreach (var q in trail) q.L -= dt; trail.RemoveAll(q => q.L <= 0);
            float dy = L.Th.Style == 0 ? .15f : -1;
            for (int i = 0; i < 120; i++) { amb[i, 0] += amb[i, 2] * dt + MathF.Sin(t + amb[i, 1] * .02f) * .4f; amb[i, 1] += amb[i, 3] * dy * dt; if (amb[i, 1] > H) amb[i, 1] = -5; if (amb[i, 1] < -5) amb[i, 1] = H; if (amb[i, 0] < 0) amb[i, 0] = W; if (amb[i, 0] > W) amb[i, 0] = 0; }
            shake = Math.Max(0, shake - 70 * dt); flash = Math.Max(0, flash - dt * 1.5f);
        }

        protected override void Update(GameTime gt)
        {
            prev = cur; cur = Keyboard.GetState(); float dt = Math.Min(.033f, (float)gt.ElapsedGameTime.TotalSeconds); t += dt;
            if (Pr(Keys.Escape)) Exit();
            if (Pr(Keys.F11)) gdm.ToggleFullScreen();
            if (state == "title") { if (Pr(Keys.Enter)) state = "play"; if (Pr(Keys.N)) { lv = 0; SaveLv(); LoadLevel(0, 250); } }
            else if (state == "dead") { if (Pr(Keys.Enter)) { LoadLevel(lv, checkX); state = "play"; } }
            else if (state == "win")
            {
                winT += dt; if (Pr(Keys.Enter) && winT > 1) { if (lv >= 2) { lv = 0; SaveLv(); state = "end"; winT = 0; } else { lv++; SaveLv(); LoadLevel(lv, 250); state = "play"; } }
            }
            else if (state == "end") { if (Pr(Keys.Enter)) { LoadLevel(0, 250); state = "title"; } }
            if (state == "play") { if (hitstop > 0) { hitstop -= dt; dt *= .1f; } UpdatePlay(dt); }
            UpdFx(dt); base.Update(gt);
        }
    }
}
