using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Kadaya
{
    public partial class Game1
    {
        // ---------- primitives ----------
        static float Hs(float i) { float v = MathF.Sin(i * 12.9898f) * 43758.5453f; return v - MathF.Floor(v); }
        void Box(float x, float y, float w, float h, float rot, Color c) => sb.Draw(px, new Vector2(x, y), null, c, rot, new Vector2(.5f), new Vector2(w, h), SpriteEffects.None, 0);
        void Box(Vector2 p, float w, float h, float rot, Color c) => Box(p.X, p.Y, w, h, rot, c);
        void Ln(float x1, float y1, float x2, float y2, float th, Color c) { float dx = x2 - x1, dy = y2 - y1; sb.Draw(px, new Vector2(x1, y1), null, c, MathF.Atan2(dy, dx), new Vector2(0, .5f), new Vector2(MathF.Sqrt(dx * dx + dy * dy), th), SpriteEffects.None, 0); }
        void Ln(Vector2 a, Vector2 b, float th, Color c) => Ln(a.X, a.Y, b.X, b.Y, th, c);
        void Circ(float x, float y, float r, Color c) => sb.Draw(circ, new Vector2(x, y), null, c, 0, new Vector2(32), r / 32f, SpriteEffects.None, 0);
        void Circ(Vector2 p, float r, Color c) => Circ(p.X, p.Y, r, c);
        void CircS(float x, float y, float rx, float ry, Color c) => sb.Draw(circ, new Vector2(x, y), null, c, 0, new Vector2(32), new Vector2(rx / 32f, ry / 32f), SpriteEffects.None, 0);
        void Glow(float x, float y, float r, Color c) => sb.Draw(glow, new Vector2(x, y), null, c, 0, new Vector2(64), r / 64f, SpriteEffects.None, 0);
        void Glow(Vector2 p, float r, Color c) => Glow(p.X, p.Y, r, c);
        void GlowS(float x, float y, float rx, float ry, Color c) => sb.Draw(glow, new Vector2(x, y), null, c, 0, new Vector2(64), new Vector2(rx / 64f, ry / 64f), SpriteEffects.None, 0);
        void Tri(float x, float y, float w, float h, float rot, Color c) => sb.Draw(tri, new Vector2(x, y), null, c, rot, new Vector2(32), new Vector2(w / 64f, h / 64f), SpriteEffects.None, 0);
        void Tri(Vector2 p, float w, float h, float rot, Color c) => Tri(p.X, p.Y, w, h, rot, c);
        void Cres(float x, float y, float rx, float ry, float rot, bool flip, Color c) => sb.Draw(cres, new Vector2(x, y), null, c, rot, new Vector2(128), new Vector2(rx / 128f, ry / 128f), flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        void Txt(string s, float x, float y, float size, Color c, int al = 0, Color? gl = null)
        {
            float k = size / 64f; var sz = font.MeasureString(s) * k; float X = al == 1 ? x - sz.X / 2 : al == 2 ? x - sz.X : x; var gc = Ad(gl ?? c, .3f);
            foreach (var o in new[] { new Vector2(-2, 0), new Vector2(2, 0), new Vector2(0, -2), new Vector2(0, 2), new Vector2(-5, 0), new Vector2(5, 0), new Vector2(0, 5) }) sb.DrawString(font, s, new Vector2(X, y) + o * (size / 40f), gc, 0, Vector2.Zero, k, SpriteEffects.None, 0);
            sb.DrawString(font, s, new Vector2(X, y), c, 0, Vector2.Zero, k, SpriteEffects.None, 0);
        }

        // ---------- the Grim Reaper ----------
        static float Ease(float p) => p * p * (3 - 2 * p);
        static float ScytheAng(float at, int cb, bool chg, bool air, bool run, float tt)
        {
            if (at >= 0)
            {
                float p = Cl(at, 0, 1), e = p < .22f ? -.18f * (p / .22f) : -.18f + 1.18f * Ease((p - .22f) / .78f);
                if (cb == 0) return -2.5f + 3.5f * e; if (cb == 1) return 1.3f - 4.0f * e; return -3.5f + 5.9f * e;
            }
            if (chg) return -2.7f; if (air) return -1.35f; if (run) return -.8f + MathF.Sin(tt * 15) * .07f; return -1.15f + MathF.Sin(tt * 2.5f) * .05f;
        }
        Vector2[] ScytheGeom(float X, float Y, int f, float s, float ang, float atp, out Vector2 hand, out Vector2 pole0, out Vector2 pole1)
        {
            Vector2 Lc(float lx, float ly) => new Vector2(X + f * s * lx, Y + s * ly);
            float reach = atp >= 0 ? MathF.Sin(Cl(atp, 0, 1) * MathF.PI) * 16 : 0, ca = MathF.Cos(ang), sa = MathF.Sin(ang);
            float hx = 20 + reach, hy = -82 + (atp >= 0 ? -4 : 0);
            hand = Lc(hx, hy); pole0 = Lc(hx - ca * 72, hy - sa * 72); pole1 = Lc(hx + ca * 104, hy + sa * 104);
            float p0x = hx + ca * 104, p0y = hy + sa * 104, nx = -sa, ny = ca, Rb = 72; var pts = new Vector2[13];
            for (int i = 0; i < 13; i++) { float ph = i / 12f * 2.0f, a = Rb * MathF.Sin(ph), b = Rb * (1 - MathF.Cos(ph)) * .9f; pts[i] = Lc(p0x + nx * a - ca * b, p0y + ny * a - sa * b); }
            return pts;
        }
        Vector3 ScythePoint(Vector2 p)
        {
            float localX = (p.X - P.X) * P.F;
            return new Vector3(P.X + MathF.Cos(P.Yaw) * localX, p.Y, P.Z + MathF.Sin(P.Yaw) * localX);
        }
        // ---------- projectiles & effects ----------
        void DrawProj(Proj p)
        {
            var screen = ScreenAt(p.X, G - p.Y, p.Z); float sx = screen.X, sy = screen.Y;
            if (p.K == "pillar")
            {
                if (p.T0 > 0) { float k = 1 - p.T0 / 1.1f; CircS(sx, sy, 46, 8, Ad(OR, .4f + .5f * k)); Ring(sx, sy - 2, 50 * (1 - k * .3f), OR, .05f); }
                else { float k = Cl(-p.T0 / .45f, 0, 1); GlowS(sx, sy - 150, 70 * (1 - k * .4f), 190, Ad(OR, 1 - k)); Box(sx, sy - 150, 50 * (1 - k), 320, 0, Ad(WH, (1 - k) * .9f)); GlowS(sx, sy, 120, 30, Ad(OR, 1 - k)); }
                return;
            }
            if (p.K == "ras") { float r = p.R; Glow(sx, sy, r * 3, Ad(CY, .9f)); Glow(sx, sy, r * 1.6f, Ad(WH, 1)); for (int i = 0; i < 3; i++) Cres(sx, sy, r * (.7f + i * .25f), r * (.7f + i * .25f), t * 14 + i * 2.1f, false, Ad(WH, .8f)); }
            else if (p.K == "wave") { Tri(sx, sy - 40, 64, 100, 0, Ad(new Color(255, 64, 32), .9f)); Glow(sx, sy - 30, 100, Ad(new Color(255, 64, 32), .6f)); Tri(sx, sy - 30, 28, 60, 0, Ad(WH, .6f)); }
            else if (p.K == "orb") { Glow(sx, sy, 60, Ad(VI, .9f)); Circ(sx, sy, 8, new Color(240, 220, 255)); }
            else if (p.K == "arrow") { var tail = ScreenAt(p.X - p.Vx * .045f, G - (p.Y - p.Vy * .045f), p.Z - p.Vz * .045f); Ln(tail, screen, 3, new Color(220, 200, 160)); Glow(sx, sy, 34, Ad(OR, .95f)); Glow(sx, sy, 14, Ad(WH, 1)); }
            else if (p.Own) { float a = MathF.Atan2(p.Vy, p.Vx); Ln(sx - MathF.Cos(a) * 14, sy - MathF.Sin(a) * 14, sx + MathF.Cos(a) * 16, sy + MathF.Sin(a) * 16, 5, new Color(232, 255, 255)); Glow(sx, sy, 30, Ad(CY, .9f)); }
        }
        void DrawFx()
        {
            foreach (var p in pt) if (p.Dk) { float k = Cl(p.L / p.M, 0, 1); var pos = ScreenAt(p.X, G - p.Y, p.Z); Circ(pos.X, pos.Y, p.Sz, p.C * (k * .55f)); }
            for (int i = 1; i < trail.Count; i++)
            {
                var q = trail[i]; if (q.Brk) continue; var q0 = trail[i - 1]; float k = Cl(q.L / .3f, 0, 1); Color c = Color.Lerp(CY, VI, .35f);
                Ln(ScreenAt(q0.B.X, G - q0.B.Y, q0.B.Z), ScreenAt(q.B.X, G - q.B.Y, q.B.Z), 22 * k, Ad(c, .55f * k)); Ln(ScreenAt(q0.B.X, G - q0.B.Y, q0.B.Z), ScreenAt(q.B.X, G - q.B.Y, q.B.Z), 8 * k, Ad(WH, .9f * k)); Ln(ScreenAt(q0.A.X, G - q0.A.Y, q0.A.Z), ScreenAt(q.A.X, G - q.A.Y, q.A.Z), 12 * k, Ad(c, .3f * k));
            }
            foreach (var f in fx)
            {
                float p = 1 - f.L / f.M; var pos = ScreenAt(f.X, G - f.Y, f.Z);
                if (f.K == "arc") { float r = f.R; Cres(pos.X, pos.Y, r * (.7f + .4f * p) * .95f, r * .95f, f.F * (MathF.PI + f.A), f.F < 0, Ad(f.C, 1 - p)); Cres(pos.X, pos.Y, r * (.7f + .4f * p) * .8f, r * .8f, f.F * (MathF.PI + f.A), f.F < 0, Ad(WH, (1 - p) * .8f)); }
                else if (f.K == "ring") { float r = f.R * MathF.Sqrt(p), th = 10 * (1 - p) + 1; for (int i = 0; i < 40; i++) { float a = i * 6.283f / 40; Box(pos.X + MathF.Cos(a) * r, pos.Y + MathF.Sin(a) * r, th * 2.4f, th, a + 1.57f, Ad(f.C, 1 - p)); } }
                else if (f.K == "ghost") { if (f.E) Box(pos.X, pos.Y - 50, 28, 100, 0, Ad(f.C, .35f * (1 - p))); else Box(pos.X, pos.Y - 60, 40, 120, 0, Ad(f.C, .3f * (1 - p))); }
            }
            foreach (var p in pt) if (!p.Dk) { float k = Cl(p.L / p.M, 0, 1); var pos = ScreenAt(p.X, G - p.Y, p.Z); Glow(pos.X, pos.Y, p.Sz * (.6f + k) * 2.4f, Ad(p.C, k)); }
        }

        // ---------- HUD ----------
        void Bar(float x, float y, float w, float h, float v, Color c1, Color c2)
        {
            Box(x + w / 2, y + h / 2, w + 6, h + 6, 0, Color.Black * .65f); float ww = w * Cl(v, 0, 1);
            for (int i = 0; i < 24; i++) { float u = i / 23f; Box(x + ww * (i + .5f) / 24, y + h / 2, ww / 24 + 1, h, 0, Color.Lerp(c1, c2, u)); }
            Glow(x + ww, y + h / 2, h * 3.2f, Ad(c2, .45f)); Box(x + w / 2, y + 2, w, 2, 0, Color.White * .12f);
        }
        void DrawHud()
        {
            Bar(40, 36, 340, 18, P.Hp / P.Mh, new Color(138, 10, 34), new Color(255, 51, 85)); Bar(40, 64, 260, 10, P.En / 100, new Color(42, 108, 255), CY); Txt("THE REAPER", 40, 4, 26, WH);
            Txt("LEVEL " + (lv + 1), W - 40, 10, 34, new Color(255, 207, 90), 2); Txt("SOULS " + kills, W - 40, 50, 24, VI, 2);
            float prog = Cl(P.X / L.GateX, 0, 1); Box(W / 2, 22, 300, 4, 0, Color.White * .15f); Box(W / 2 - 150 + 150 * prog, 22, 300 * prog, 4, 0, Ad(L.Th.Accent, .9f)); Glow(W / 2 - 150 + 300 * prog, 22, 18, Ad(L.Th.Accent, 1));
            var sk = new[] { ("LMB", "SCYTHE", 0), ("E", "CLONES", 40), ("SHIFT", "DODGE", 0) };
            for (int i = 0; i < sk.Length; i++)
            {
                float X = 40 + i * 94; bool ok = P.En >= sk[i].Item3; float a = ok ? 1 : .35f; Color bc = new Color(255, 255, 255, 90);
                Box(X + 43, H - 66, 86, 2, 0, bc * a); Box(X + 43, H - 22, 86, 2, 0, bc * a); Box(X, H - 44, 2, 44, 0, bc * a); Box(X + 86, H - 44, 2, 44, 0, bc * a);
                Txt(sk[i].Item1, X + 43, H - 64, sk[i].Item1.Length > 1 ? 18 : 26, ok ? WH : Color.Gray, 1); Txt(sk[i].Item2, X + 43, H - 38, 14, ok ? CY : Color.DimGray, 1);
            }
            foreach (var e in en) if (e.Boss && e.Aggro) { Bar(W / 2 - 260, 96, 520, 12, e.Hp / e.Mh, new Color(160, 16, 32), new Color(255, 96, 48)); Txt(e.Kind == 0 ? "ONI WARLORD" : e.Kind == 1 ? "THE WRAITH LORD" : "FIRE SHOGUN", W / 2, 58, 30, OR, 1); }
            if (banT > 0) Txt(banS, W / 2, H / 2 - 190, 54, WH * Cl(banT, 0, 1), 1, RD);
        }
    }
}

