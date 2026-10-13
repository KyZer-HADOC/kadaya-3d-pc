using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kadaya
{
    class Foe { public int Type, F, St, Kind; public bool Boss, Dead, Did, Aggro; public float X, Y, Z, Vx, Vy, Vz, Yaw, Hp, Mh, Sp, S, Tm, Flash, Walk, Mv, A, B, Ph, Cnt, Air, Al = 1; }
    class Proj { public string K; public bool Own, Spent; public float X, Y, Z, Vx, Vy, Vz, L, R, Dmg, T0; }
    class Clone { public float X, Z, Yaw, L, Cd, At = -1, T, Mv; public int F; }
    class Part { public float X, Y, Z, Vx, Vy, Vz, L, M, Sz, G; public Color C; public bool Dk; }
    class Fx { public string K; public float X, Y, Z, R, L, M, A; public int F; public Color C; public bool E; }
    class Trl { public Vector2 A, B; public float Z, L; public bool Brk; }
    class Pl { public float X, Y, Z, Vx, Vy, Vz, Yaw, Hp, Mh = 100, En, CbT, Atk, AtkT, Inv, Dash, Dcd, Chg, Kcd, Coy; public int F, J, Cb; public bool Hit, Dead, Run, Ground; }

    public partial class Game1 : Game
    {
        const int W = 1280, H = 720; const float G = 600;
        static readonly Color CY = new Color(95, 243, 255), PU = new Color(138, 92, 255), VI = new Color(192, 107, 255), RD = new Color(255, 45, 85), OR = new Color(255, 179, 71), WH = Color.White;

        GraphicsDeviceManager gdm; SpriteBatch sb; RenderTarget2D rt, small; Texture2D px, circ, glow, tri, cres, vig; SpriteFont font;
        static Random rnd = new Random();
        KeyboardState cur, prev;
        Point cameraMouse; int cameraWheel;
        MouseState mouseCur, mousePrev;

        string state = "title", banS = "";
        float t, shake, hitstop, cam, camY, camZ, flash, ult, banT, winT, camYaw = -MathHelper.PiOver2, camPitch = .19f, camDistance = 880, renderScale = 1; bool ultHit = true, bossDead, cameraDragging;
        int lv, kills; Level L; float checkX, checkZ = 110; bool[] lit;
        Pl P = new Pl();
        List<Foe> en = new List<Foe>(); List<Proj> proj = new List<Proj>(); List<Clone> clones = new List<Clone>();
        List<Part> pt = new List<Part>(); List<Fx> fx = new List<Fx>(); List<Trl> trail = new List<Trl>();
        float[,] stars = new float[140, 4]; float[,] amb = new float[120, 5];

        public Game1()
        {
            gdm = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = W, PreferredBackBufferHeight = H };
            Content.RootDirectory = "Content"; IsMouseVisible = true; Window.AllowUserResizing = true; Window.Title = "KADAYA";
        }

        static float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static float Cl(float v, float a, float b) => Math.Max(a, Math.Min(b, v));
        static int Sg(float v) => v >= 0 ? 1 : -1;
        static Color Ad(Color c, float k) => new Color(c.R / 255f * k, c.G / 255f * k, c.B / 255f * k, 0f);
        static Color Pm(float a) => new Color(a, a, a, a);

        protected override void LoadContent()
        {
            sb = new SpriteBatch(GraphicsDevice); font = Content.Load<SpriteFont>("Font");
            CreateRenderTargets();
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
        void CreateRenderTargets()
        {
            if (rt != null) rt.Dispose(); if (small != null) small.Dispose();
            int rw = (int)(W * renderScale), rh = (int)(H * renderScale);
            rt = new RenderTarget2D(GraphicsDevice, rw, rh, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
            small = new RenderTarget2D(GraphicsDevice, Math.Max(1, rw / 4), Math.Max(1, rh / 4));
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

        void LoadLevel(int n, float sx, float sz = 110)
        {
            lv = n; L = LevelGen.Make(n); L.Foes.RemoveAll(f => f.X < sx + 150 && !f.Boss); en = L.Foes;
            foreach (var e in en) e.Z = 110;
            proj.Clear(); clones.Clear(); pt.Clear(); fx.Clear(); trail.Clear();
            P = new Pl { X = sx, Y = G, Z = sz, F = 1, Hp = 100, En = 50 }; checkX = sx; checkZ = sz; lit = new bool[L.Checks.Count]; for (int i = 0; i < lit.Length; i++) lit[i] = L.Checks[i] <= sx;
            bossDead = false; ult = 0; ultHit = true; cam = Cl(sx - W * .5f, 0, L.GateX + 400 - W); camY = 0; camZ = sz; banS = "LEVEL " + (n + 1) + "  -  " + L.Th.Name; banT = 3.2f;
            PlaySong(L.Th.Style);
        }

        // ---------- world queries ----------
        bool InGap(float x) { foreach (var g in L.Gaps) if (x > g.X && x < g.Y) return true; return false; }
        float FloorBelow(float x, float y, float z) { float best = InGap(x) ? 99999 : G; foreach (var p in L.Plats) if (x >= p.X && x <= p.X + p.W && z >= 1 && z <= 171 && p.Y >= y - 4 && p.Y < best) best = p.Y; return best; }
        static float DistXZ(float x1, float z1, float x2, float z2) => MathF.Sqrt((x2 - x1) * (x2 - x1) + (z2 - z1) * (z2 - z1));

        // ---------- effects ----------
        void Burst(float X, float Y, int n, Color col, float sp = 300, float sz = 3, float g = 600, float life = .6f, float z = 110)
        { for (int i = 0; i < n && pt.Count < 2200; i++) { float a = Rf(0, 6.283f), s = Rf(.2f, 1) * sp; pt.Add(new Part { X = X, Y = Y, Z = z, Vx = MathF.Cos(a) * s, Vy = MathF.Sin(a) * s, Vz = Rf(-.35f, .35f) * s, L = Rf(.4f, 1) * life, M = life, Sz = Rf(.5f, 1) * sz, C = col, G = g }); } }
        void Arc(float X, float Y, int f, float r, Color c, float l = .22f, float a = 0, float z = 110) => fx.Add(new Fx { K = "arc", X = X, Y = Y, Z = z, F = f, R = r, L = l, M = l, C = c, A = a });
        void Ring(float X, float Y, float r, Color c, float l = .5f, float z = 110) => fx.Add(new Fx { K = "ring", X = X, Y = Y, Z = z, R = r, L = l, M = l, C = c });
        void Smoke(float X, float Y, float vx, float z = 110) { if (pt.Count < 2200) pt.Add(new Part { X = X, Y = Y, Z = z, Vx = vx + Rf(-20, 20), Vy = Rf(-70, -15), L = Rf(.6f, 1.1f), M = 1.1f, Sz = Rf(12, 24), C = new Color(10, 5, 18), G = -30, Dk = true }); }

        // ---------- combat ----------
        void Die()
        {
            if (P.Dead) return; P.Dead = true; P.Hp = 0; state = "dead"; Burst(P.X, P.Y - 60, 90, VI, 650, 6, 200, 1.7f, P.Z); for (int i = 0; i < 18; i++) Smoke(P.X, P.Y - 40, Rf(-120, 120), P.Z); shake = 30; Sfx("kill", P.X); if (musInst != null) musInst.Volume = .15f;
        }
        void HurtP(float d, int dir)
        {
            if (P.Inv > 0 || P.Dash > 0 || P.Dead || ult > 0) return;
            P.Hp -= d; P.Inv = .9f; P.Vx = dir * 380; P.Vy = -300; P.Ground = false; P.Cb = 0; P.Atk = 0; P.Chg = 0; shake = 16; flash = .35f; hitstop = .06f;
            Burst(P.X, P.Y - 70, 24, RD, 420, 4, 600, .6f, P.Z); Sfx("hurt", P.X); if (P.Hp <= 0) Die();
        }
        void HitE(Foe e, float d, float kx)
        {
            if (e.Dead) return;
            e.Aggro = true; e.Hp -= d; e.Flash = .1f; e.Vx = kx * (e.Boss ? .15f : 1); if (!e.Boss) e.Vy = -220; if (e.St == 1 && !e.Boss) { e.St = 3; e.Tm = .5f; }
            P.En = Math.Min(100, P.En + d * .3f); hitstop = Math.Max(hitstop, .05f); shake = Math.Max(shake, d > 30 ? 18 : 8);
            Burst(e.X, e.Y - 60 * e.S, 16, WH, 450, 3, 500, .35f, e.Z); Burst(e.X, e.Y - 60 * e.S, 12, RD, 320, 4, 700, .5f, e.Z); Sfx("hit", e.X);
            if (e.Hp <= 0)
            {
                e.Dead = true; kills++; Burst(e.X, e.Y - 60 * e.S, 30, VI, 160, 6, -260, 1.5f, e.Z); Burst(e.X, e.Y - 60 * e.S, 34, WH, 520, 3, 400, .5f, e.Z);
                Ring(e.X, e.Y - 60, 100, VI, .6f, e.Z); shake = Math.Max(shake, e.Boss ? 38 : 14); Sfx("kill", e.X);
                if (e.Boss) { bossDead = true; hitstop = .4f; flash = .9f; banS = "BOSS DEFEATED - THE SOUL GATE OPENS"; banT = 3.5f; Ring(e.X, e.Y - 100, 900, OR, 1f, e.Z); Sfx("boom", e.X); }
            }
        }
        void SlashHit()
        {
            float r = P.Cb == 2 ? 240 : 185, dm = new[] { 12f, 14f, 28f }[P.Cb];
            Arc(P.X, P.Y - 70, P.F, r * (P.Cb == 2 ? 1.1f : .9f), P.Cb == 2 ? VI : CY, .25f, new[] { -.35f, .35f, 0f }[P.Cb], P.Z);
            if (P.Cb == 2) { Ring(P.X, P.Y - 60, 190, VI, .4f, P.Z); Burst(P.X + P.F * 90, P.Y - 70, 28, VI, 520, 4, 600, .6f, P.Z); }
            Sfx(P.Cb == 2 ? "slash3" : "slash", P.X);
            foreach (var e in en) if (DistXZ(e.X, e.Z, P.X, P.Z) < r + 20 * e.S && Math.Abs(e.Y - P.Y) < 160) HitE(e, dm * (1 + lv * .05f), P.F * (P.Cb == 2 ? 700 : 380));
            foreach (var p in proj) if (!p.Own && p.K != "pillar" && DistXZ(p.X, p.Z, P.X, P.Z) < r && Math.Abs(p.Y - (P.Y - 70)) < 120) { p.L = 0; P.En = Math.Min(100, P.En + 6); Burst(p.X, p.Y, 14, CY, 300, 3, 600, .6f, p.Z); }
        }
        void Explode(Proj p)
        {
            float R = 210 * p.R / 30; Ring(p.X, p.Y, R, CY, .5f, p.Z); Ring(p.X, p.Y, R * .6f, WH, .35f, p.Z); Burst(p.X, p.Y, 80, CY, 800, 5, 100, .8f, p.Z); Burst(p.X, p.Y, 30, WH, 600, 3, 0, .5f, p.Z);
            shake = 24; flash = .2f; hitstop = .08f; Sfx("ras", p.X);
            foreach (var e in en) if (DistXZ(e.X, e.Z, p.X, p.Z) < R + 20 && Math.Abs(e.Y - p.Y) < R) HitE(e, p.Dmg, Sg(e.X - p.X) * 600);
        }
        void Wave(float x, float z, int dir, float zdir, float spd = 480) => proj.Add(new Proj { K = "wave", X = x, Y = G - 30, Z = z, Vx = dir * spd, Vz = zdir * spd, L = 2.2f, R = 34, Dmg = 16 });
        void Slam(Foe e)
        {
            Wave(e.X, e.Z, 1, 0); Wave(e.X, e.Z, -1, 0); Ring(e.X, G - 10, 280, e.Kind == 2 ? OR : RD, .5f, e.Z); Burst(e.X, G - 10, 40, OR, 500, 5, 500, .6f, e.Z); shake = 24; Sfx("boom", e.X);
            if (DistXZ(P.X, P.Z, e.X, e.Z) < 180 && P.Ground) HurtP(22, Sg(P.X - e.X));
        }

        // ---------- enemies ----------
        void UpdE(Foe e, float dt)
        {
            float d = P.X - e.X, dz = P.Z - e.Z, ad = DistXZ(P.X, P.Z, e.X, e.Z); e.Ph += dt; e.Flash -= dt;
            if (!e.Aggro) { if (ad < (e.Boss ? 950 : 680) && !P.Dead) e.Aggro = true; else { e.F = d > 0 ? 1 : -1; return; } }
            if (e.St != 2 && e.St != 5) { e.F = d >= 0 ? 1 : -1; e.Yaw = MathF.Atan2(dz, d); }
            e.Vy += 2300 * dt; e.Y += e.Vy * dt; if (e.Y >= G) { e.Y = G; if (e.Vy > 0) e.Vy = 0; }
            e.X = Cl(e.X + e.Vx * dt, e.A + 30, e.B - 30); e.Z = Cl(e.Z + e.Vz * dt, -270, 490); if (e.St != 2) { e.Vx *= MathF.Pow(.02f, dt); e.Vz *= MathF.Pow(.02f, dt); }
            e.Tm -= dt; e.Mv = 0; int T = e.Type;
            if (e.Boss) { UpdBoss(e, dt, d, dz, ad); return; }
            if (e.St == 0)
            {
                if (T == 2) { int w = ad < 360 ? -1 : ad > 560 ? 1 : 0; float step = w * e.Sp * dt / Math.Max(1, ad); e.X = Cl(e.X + d * step, e.A + 30, e.B - 30); e.Z = Cl(e.Z + dz * step, -270, 490); e.Mv = w; if (e.Tm <= 0 && ad < 720) { e.St = 1; e.Tm = .6f; Sfx("tele", e.X); } }
                else
                {
                    float rg = T == 1 ? 125 : 260;
                    if (ad > (T == 1 ? 100 : 120) && (ad > rg || e.Tm > 0)) { e.X = Cl(e.X + d / ad * e.Sp * dt, e.A + 30, e.B - 30); e.Z = Cl(e.Z + dz / ad * e.Sp * dt, -270, 490); e.Mv = 1; }
                    if (e.Tm <= 0 && ad <= rg) { e.St = 1; e.Tm = T == 1 ? .65f : .4f; Sfx("tele", e.X); }
                }
            }
            else if (e.St == 1 && e.Tm <= 0)
            {
                if (T == 0) { e.St = 2; e.Air = 0; e.Vy = -680; e.Vx = Cl(d / .59f, -720, 720); e.Vz = Cl(dz / .59f, -520, 520); Sfx("dash", e.X); }
                else if (T == 1)
                {
                    Arc(e.X + e.F * 20, e.Y - 70, e.F, 130, new Color(255, 64, 32), .22f, 0, e.Z); Sfx("slash", e.X);
                    if (ad < 160 && Math.Abs(P.Y - e.Y) < 130) HurtP(16, e.F); e.St = 3; e.Tm = .8f;
                }
                else
                {
                    float len = Math.Max(1, MathF.Sqrt(d * d + dz * dz + 900)); proj.Add(new Proj { K = "arrow", X = e.X + e.F * 30, Y = e.Y - 80, Z = e.Z, Vx = d / len * 640, Vy = ((P.Y - 70) - (e.Y - 80)) / len * 640, Vz = dz / len * 640, L = 2.5f, R = 10, Dmg = 10 });
                    e.St = 3; e.Tm = .7f; Sfx("arrow", e.X);
                }
            }
            else if (e.St == 2)
            {
                e.Air += dt; fx.Add(new Fx { K = "ghost", X = e.X, Y = e.Y, Z = e.Z, F = e.F, L = .2f, M = .2f, C = RD, E = true });
                if (!e.Did && ad < 70 && Math.Abs(P.Y - e.Y) < 130) { e.Did = true; HurtP(13, e.F); }
                if (e.Air > .15f && e.Y >= G) { e.St = 3; e.Tm = .6f; e.Vx = 0; e.Did = false; }
            }
            else if (e.St == 3 && e.Tm <= 0) { e.St = 0; e.Tm = Rf(.7f, 1.5f); }
            e.Walk += dt * (e.Mv != 0 ? 1 : 0);
        }
        void UpdBoss(Foe e, float dt, float d, float dz, float ad)
        {
            int k = e.Kind;
            if (e.St == 0)
            {
                float rg = k == 1 ? 470 : 200; if (ad > rg) { e.X = Cl(e.X + d / ad * e.Sp * dt, e.A + 30, e.B - 30); e.Z = Cl(e.Z + dz / ad * e.Sp * dt, -270, 490); e.Mv = 1; } else if (k == 1 && ad < 230) { e.X = Cl(e.X - d / ad * e.Sp * .8f * dt, e.A + 30, e.B - 30); e.Z = Cl(e.Z - dz / ad * e.Sp * .8f * dt, -270, 490); e.Mv = -1; }
                if (e.Tm <= 0) { e.St = 1; e.Cnt++; e.Tm = .85f; Sfx("tele", e.X); }
            }
            else if (e.St == 1 && e.Tm <= 0)
            {
                int v = (int)e.Cnt % 3;
                if (k == 0) { if (v == 0) { e.Vy = -950; e.Vx = Cl(d * 1.1f, -650, 650); e.Vz = Cl(dz * 1.1f, -520, 520); e.St = 2; e.Air = 0; Sfx("slash3", e.X); } else { Slam(e); e.St = 3; e.Tm = 1.1f; } }
                else if (k == 1)
                {
                    if (v == 0) { e.St = 4; e.Tm = .45f; Sfx("tele", e.X); }
                    else { float aim = MathF.Atan2(dz, d); for (int i = -2; i <= 2; i++) { float a = aim + i * .22f; float len = Math.Max(1, MathF.Sqrt(ad * ad + 900)); proj.Add(new Proj { K = "orb", X = e.X + e.F * 30, Y = e.Y - 110, Z = e.Z, Vx = MathF.Cos(a) * 430, Vy = ((P.Y - 70) - (e.Y - 110)) / len * 430, Vz = MathF.Sin(a) * 430, L = 3, R = 16, Dmg = 12 }); } Sfx("arrow", e.X); e.St = 3; e.Tm = 1.2f; }
                }
                else
                {
                    if (v == 0) { Wave(e.X, e.Z, 1, 0, 600); Wave(e.X, e.Z, -1, 0, 600); Ring(e.X, G, 300, OR, .5f, e.Z); shake = 18; Sfx("boom", e.X); e.St = 3; e.Tm = 1.1f; }
                    else if (v == 1) { for (int i = -2; i <= 2; i++) proj.Add(new Proj { K = "pillar", X = P.X + i * 140, Y = G, Z = P.Z, L = 1.7f, T0 = .95f + Math.Abs(i) * .08f, R = 40, Dmg = 20 }); Sfx("tele", e.X); e.St = 3; e.Tm = 1.4f; }
                    else { e.Vy = -950; e.Vx = Cl(d * 1.1f, -650, 650); e.Vz = Cl(dz * 1.1f, -520, 520); e.St = 2; e.Air = 0; Sfx("slash3", e.X); }
                }
            }
            else if (e.St == 2) { e.Air += dt; if (e.Air > .25f && e.Y >= G) { Slam(e); e.St = 3; e.Tm = 1f; e.Vx = 0; } }
            else if (e.St == 4) { e.Al = Math.Max(0, e.Al - dt * 4); if (e.Tm <= 0) { e.X = Cl(P.X - P.F * 150, e.A + 30, e.B - 30); e.Z = Cl(P.Z + Rf(-120, 120), -240, 460); e.F = P.F; e.Yaw = MathF.Atan2(P.Z - e.Z, P.X - e.X); e.St = 5; e.Tm = .4f; e.Al = 1; Burst(e.X, e.Y - 80, 36, VI, 400, 5, 0, .6f, e.Z); } }
            else if (e.St == 5 && e.Tm <= 0) { Arc(e.X + e.F * 20, e.Y - 90, e.F, 200, VI, .25f, 0, e.Z); Sfx("slash3", e.X); if (DistXZ(P.X, P.Z, e.X, e.Z) < 220 && Math.Abs(P.Y - e.Y) < 140) HurtP(22, e.F); e.St = 3; e.Tm = .9f; }
            else if (e.St == 3 && e.Tm <= 0) { e.St = 0; e.Tm = Rf(.4f, .9f); }
            e.Walk += dt * (e.Mv != 0 ? 1 : 0);
        }

        // ---------- player update ----------
        bool Dn(Keys k) => cur.IsKeyDown(k);
        bool Pr(Keys k) => cur.IsKeyDown(k) && prev.IsKeyUp(k);

        void UpdatePlay(float dt)
        {
            if (banT > 0) banT -= dt;
            float side = (Dn(Keys.D) ? 1 : 0) - (Dn(Keys.A) ? 1 : 0), forward = (Dn(Keys.W) ? 1 : 0) - (Dn(Keys.S) ? 1 : 0);
            float fwdX = -MathF.Sin(camYaw), fz = -MathF.Cos(camYaw), rx = MathF.Cos(camYaw), rz = -MathF.Sin(camYaw);
            float mx = rx * side + fwdX * forward, mz = rz * side + fz * forward, ml = MathF.Sqrt(mx * mx + mz * mz);
            if (ml > 1) { mx /= ml; mz /= ml; }
            if (!P.Dead)
            {
                P.Inv -= dt; P.Dcd -= dt; P.Kcd -= dt; P.CbT -= dt; bool was = P.Ground; float pvy = P.Vy;
                if (P.Dash > 0) { P.Dash -= dt; P.X += MathF.Cos(P.Yaw) * 1200 * dt; P.Z += MathF.Sin(P.Yaw) * 1200 * dt; P.Vy = 0; fx.Add(new Fx { K = "ghost", X = P.X, Y = P.Y, Z = P.Z, F = P.F, L = .3f, M = .3f, C = PU }); }
                else
                {
                    float sl = P.Atk > 0 ? .72f : 1, accel = Math.Min(1, dt * (P.Ground ? 14 : 6));
                    P.Vx += (mx * 420 * sl - P.Vx) * accel; P.Vz += (mz * 420 * sl - P.Vz) * accel;
                    if (ml > .1f && P.Atk <= 0) { P.Yaw = MathF.Atan2(mz, mx); if (Math.Abs(mx) > .15f) P.F = mx > 0 ? 1 : -1; }
                    if (P.Ground) P.Coy = .1f; else { P.Coy -= dt; if (P.Coy <= 0 && P.J == 0) P.J = 1; }
                    if (Pr(Keys.Space) && P.J < 2) { P.Vy = -850; P.J++; P.Ground = false; Burst(P.X, P.Y, 14, PU, 240, 3, 300, .4f, P.Z); Sfx("jump", P.X); }
                    float prevY = P.Y; P.Vy += 2300 * dt; P.X += P.Vx * dt; P.Z += P.Vz * dt; P.Y += P.Vy * dt; P.Ground = false;
                    if (P.Vy >= 0)
                    {
                        float best = 1e9f; if (!InGap(P.X) && prevY <= G + 1 && P.Y >= G) best = G;
                        foreach (var pl in L.Plats) if (P.X >= pl.X && P.X <= pl.X + pl.W && P.Z >= 1 && P.Z <= 171 && prevY <= pl.Y + 1 && P.Y >= pl.Y && pl.Y < best) best = pl.Y;
                        if (best < 1e8f) { P.Y = best; P.Vy = 0; P.Ground = true; P.J = 0; }
                    }
                }
                if (P.Ground && !was && pvy > 450) { Sfx("land", P.X); for (int i = 0; i < 6; i++) pt.Add(new Part { X = P.X + Rf(-20, 20), Y = P.Y - 4, Z = P.Z + Rf(-20, 20), Vx = Rf(-80, 80), Vy = Rf(-70, -15), L = .8f, M = .8f, Sz = 12, C = new Color(10, 5, 18), G = -30, Dk = true }); }
                if (P.Y > G + 380) { Sfx("fall", P.X); Die(); }
                P.X = Cl(P.X, 40, bossDead ? L.GateX + 60 : L.GateX - 90); P.Z = Cl(P.Z, -260, 480); P.Run = P.Ground && MathF.Sqrt(P.Vx * P.Vx + P.Vz * P.Vz) > 60;
                if (P.Ground && P.Y >= G - 6) foreach (var sp in L.Spikes) if (P.X > sp.X && P.X < sp.Y)
                    for (float z = -250; z <= 450; z += 70) if (Math.Abs(P.Z - z) < 34) { HurtP(30, -P.F); P.Vy = -620; break; }
                if ((P.Run || P.Dash > 0 || P.Atk > 0) && rnd.NextDouble() < .6) pt.Add(new Part { X = P.X - MathF.Cos(P.Yaw) * Rf(10, 34), Y = P.Y - Rf(0, 50), Z = P.Z - MathF.Sin(P.Yaw) * Rf(10, 34), Vx = Rf(-20, 20), Vy = Rf(-70, -15), L = .8f, M = .8f, Sz = 12, C = new Color(10, 5, 18), G = -30, Dk = true });
                if ((Pr(Keys.LeftShift) || Pr(Keys.RightShift)) && P.Dcd <= 0) { P.Dash = .17f; P.Dcd = .65f; P.Inv = Math.Max(P.Inv, .3f); Burst(P.X, P.Y - 60, 22, PU, 300, 4, 600, .6f, P.Z); Sfx("dash", P.X); }
                if (P.Atk > 0)
                {
                    float dur = P.Cb == 2 ? .42f : .28f; P.AtkT += dt; P.Atk -= dt; float atp = P.AtkT / dur;
                    var pts = ScytheGeom(P.X, P.Y, P.F, 1, ScytheAng(atp, P.Cb, false, false, false, t), atp, out _, out _, out _);
                    trail.Add(new Trl { A = pts[6], B = pts[12], Z = P.Z, L = .3f, Brk = P.AtkT <= dt * 1.5f });
                    if (rnd.NextDouble() < .8) pt.Add(new Part { X = pts[12].X, Y = pts[12].Y, Z = P.Z, Vx = Rf(-40, 40), Vy = Rf(-80, -20), L = .5f, M = .5f, Sz = 5, C = P.Cb == 2 ? VI : CY, G = -60 });
                    if (P.Hit && P.AtkT > (P.Cb == 2 ? .17f : .11f)) { P.Hit = false; SlashHit(); }
                }
                if (mouseCur.LeftButton == ButtonState.Pressed && mousePrev.LeftButton == ButtonState.Released && P.Dash <= 0 && (P.Atk <= 0 || P.AtkT > .18f)) { P.Cb = P.CbT > 0 ? (P.Cb + 1) % 3 : 0; P.Atk = P.Cb == 2 ? .42f : .28f; P.AtkT = 0; P.Hit = true; P.CbT = .75f; }
                if (Pr(Keys.E) && P.En >= 40)
                {
                    P.En -= 40;
                    foreach (int s in new[] { -1, 1 })
                    {
                        clones.Add(new Clone { X = P.X - MathF.Sin(P.Yaw) * s * 90, Z = P.Z + MathF.Cos(P.Yaw) * s * 90, Yaw = P.Yaw, F = P.F, L = 7, Cd = .2f });
                        Burst(P.X, G - 60, 24, new Color(223, 233, 255), 300, 6, -100, .7f, P.Z);
                    }
                    Sfx("clone", P.X);
                }
                // checkpoints
                for (int i = 0; i < L.Checks.Count; i++) if (!lit[i] && P.X > L.Checks[i]) { lit[i] = true; checkX = L.Checks[i]; checkZ = P.Z; P.Hp = Math.Min(P.Mh, P.Hp + 40); Burst(checkX, G - 60, 50, L.Th.Accent, 400, 5, -200, 1.2f, P.Z); Ring(checkX, G - 60, 200, L.Th.Accent, .8f, P.Z); Sfx("check", checkX); banS = "CHECKPOINT"; banT = 1.6f; }
                // soul pickups
                foreach (var s in L.Souls) if (!s.Got && DistXZ(P.X, P.Z, s.X, 110) < 70 && Math.Abs(P.Y - 60 - s.Y) < 90) { s.Got = true; P.En = Math.Min(100, P.En + 15); Burst(s.X, s.Y, 20, VI, 300, 4, -100, .6f, 110); Sfx("soul", s.X); }
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
                foreach (var e in en) { float d = DistXZ(e.X, e.Z, k.X, k.Z); if (d < bd) { bd = d; tg = e; } }
                if (tg != null && bd < 900)
                {
                    float dx = tg.X - k.X, dz = tg.Z - k.Z; k.F = dx >= 0 ? 1 : -1; k.Yaw = MathF.Atan2(dz, dx);
                    if (bd > 110) { k.X += dx / bd * 330 * dt; k.Z = Cl(k.Z + dz / bd * 330 * dt, -260, 480); k.Mv = 1; }
                    else if (k.Cd <= 0) { k.Cd = .55f; k.At = 0; Arc(k.X + k.F * 30, G - 70, k.F, 130, new Color(223, 233, 255), .2f, 0, k.Z); HitE(tg, 10, k.F * 300); }
                }
                else if (DistXZ(P.X, P.Z, k.X, k.Z) > 140) { float dx = P.X - k.X, dz = P.Z - k.Z, d = DistXZ(P.X, P.Z, k.X, k.Z); k.X += dx / d * 330 * dt; k.Z = Cl(k.Z + dz / d * 330 * dt, -260, 480); k.Yaw = MathF.Atan2(dz, dx); k.Mv = 1; }
                if (k.At >= 0) { k.At += dt; if (k.At > .28f) k.At = -1; }
                if (k.L <= 0) Burst(k.X, G - 60, 25, new Color(223, 233, 255), 250, 5, -100, .6f, k.Z);
            }
            clones.RemoveAll(k => k.L <= 0);
            foreach (var p in proj)
            {
                p.L -= dt;
                if (p.K == "pillar") { p.T0 -= dt; if (p.T0 <= 0 && p.T0 > -.45f && DistXZ(P.X, P.Z, p.X, p.Z) < 45 && P.Y > G - 320) HurtP(p.Dmg, Sg(P.X - p.X)); continue; }
                p.X += p.Vx * dt; p.Y += p.Vy * dt; p.Z += p.Vz * dt;
                if (p.Own)
                {
                    pt.Add(new Part { X = p.X, Y = p.Y, Z = p.Z, Vx = Rf(-30, 30), Vy = Rf(-30, 30), L = .3f, M = .3f, Sz = p.K == "ras" ? p.R * .5f : 3, C = CY });
                    foreach (var e in en)
                        if (!e.Dead && p.L > 0 && DistXZ(p.X, p.Z, e.X, e.Z) < 26 * e.S + p.R && p.Y > e.Y - 125 * e.S && p.Y < e.Y + 10)
                        { if (p.K == "ras") { Explode(p); p.Spent = true; } else HitE(e, p.Dmg, Sg(p.Vx) * 250); p.L = 0; }
                }
                else
                {
                    if (p.K == "wave") Burst(p.X, G - 5, 2, new Color(255, 64, 32), 100, 5, -300, .4f, p.Z);
                    if (DistXZ(p.X, p.Z, P.X, P.Z) < p.R + 16 && p.Y > P.Y - 115 && p.Y < P.Y + 10 && p.L > 0) { HurtP(p.Dmg, Sg(p.Vx)); p.L = 0; }
                }
                if (p.X < -100 || p.X > L.GateX + 900 || p.Y > G + 50) p.L = 0;
            }
            foreach (var p in proj) if (p.L <= 0 && p.K == "ras" && !p.Spent) { p.Spent = true; Explode(p); }
            proj.RemoveAll(p => p.L <= 0);
        }

        void UpdFx(float dt)
        {
            foreach (var p in pt) { p.L -= dt; p.Vy += p.G * dt; p.X += p.Vx * dt; p.Y += p.Vy * dt; p.Z += p.Vz * dt; if (p.Dk) { p.Vx *= .98f; p.Vz *= .98f; p.Sz += dt * 8; } }
            pt.RemoveAll(p => p.L <= 0); foreach (var f in fx) f.L -= dt; fx.RemoveAll(f => f.L <= 0); foreach (var q in trail) q.L -= dt; trail.RemoveAll(q => q.L <= 0);
            float dy = L.Th.Style == 0 ? .15f : -1;
            for (int i = 0; i < 120; i++) { amb[i, 0] += amb[i, 2] * dt + MathF.Sin(t + amb[i, 1] * .02f) * .4f; amb[i, 1] += amb[i, 3] * dy * dt; if (amb[i, 1] > H) amb[i, 1] = -5; if (amb[i, 1] < -5) amb[i, 1] = H; if (amb[i, 0] < 0) amb[i, 0] = W; if (amb[i, 0] > W) amb[i, 0] = 0; }
            shake = Math.Max(0, shake - 70 * dt); flash = Math.Max(0, flash - dt * 1.5f);
        }

        protected override void Update(GameTime gt)
        {
            prev = cur; cur = Keyboard.GetState(); mousePrev = mouseCur; mouseCur = Mouse.GetState(); float dt = Math.Min(.033f, (float)gt.ElapsedGameTime.TotalSeconds); t += dt;
            if (Pr(Keys.Escape)) Exit();
            if (Pr(Keys.F11)) gdm.ToggleFullScreen();
            if (Pr(Keys.F6)) { renderScale = renderScale >= 1.49f ? 1 : renderScale + .25f; CreateRenderTargets(); banS = "RENDER QUALITY  " + (int)(renderScale * 100) + "%"; banT = 1.5f; }
            if (Pr(Keys.F7)) { camYaw = -MathHelper.PiOver2; camPitch = .19f; camDistance = 880; banS = "CAMERA RESET"; banT = 1.2f; }
            if (state == "title") { if (Pr(Keys.Enter)) state = "play"; if (Pr(Keys.N)) { lv = 0; SaveLv(); LoadLevel(0, 250); } }
            else if (state == "dead") { if (Pr(Keys.Enter)) { LoadLevel(lv, checkX, checkZ); state = "play"; } }
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

