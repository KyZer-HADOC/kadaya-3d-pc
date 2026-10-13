using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Kadaya
{
    class Mesh { public VertexBuffer VB; public IndexBuffer IB; public int Tris; }
    struct Rig { public Matrix R; public float S; }
    struct GlowReq { public float X, Y, R; public Color C; }

    public partial class Game1
    {
        BasicEffect be; Mesh mCube, mSph, mCyl, mCone, mPyr, mFrus; Matrix view, projM; float specK;
        List<GlowReq> glows = new List<GlowReq>();
        static Color Em(Color c, float k) => new Color(c.R * k / 255f, c.G * k / 255f, c.B * k / 255f);

        // ---------- procedural meshes ----------
        void Init3D()
        {
            be = new BasicEffect(GraphicsDevice) { TextureEnabled = false, VertexColorEnabled = false, SpecularColor = Vector3.Zero, SpecularPower = 48 };
            mCube = MkCube(); mSph = MkSphere(24, 32); mCyl = MkRound(24, 1f); mCone = MkRound(24, 0f); mPyr = MkRound(4, 0f); mFrus = MkRound(24, .8f);
        }
        Mesh Build(List<VertexPositionNormalTexture> v, List<ushort> ix)
        {
            var m = new Mesh { Tris = ix.Count / 3 };
            m.VB = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTexture), v.Count, BufferUsage.WriteOnly); m.VB.SetData(v.ToArray());
            m.IB = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, ix.Count, BufferUsage.WriteOnly); m.IB.SetData(ix.ToArray()); return m;
        }
        Mesh MkCube()
        {
            var v = new List<VertexPositionNormalTexture>(); var ix = new List<ushort>();
            Vector3[] ns = { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ };
            foreach (var n in ns)
            {
                Vector3 u = MathF.Abs(n.Y) > .5f ? Vector3.UnitX : Vector3.UnitY, w = Vector3.Cross(n, u); int b = v.Count;
                v.Add(new VertexPositionNormalTexture(n * .5f - u * .5f - w * .5f, n, Vector2.Zero)); v.Add(new VertexPositionNormalTexture(n * .5f + u * .5f - w * .5f, n, Vector2.Zero));
                v.Add(new VertexPositionNormalTexture(n * .5f + u * .5f + w * .5f, n, Vector2.Zero)); v.Add(new VertexPositionNormalTexture(n * .5f - u * .5f + w * .5f, n, Vector2.Zero));
                ix.AddRange(new ushort[] { (ushort)b, (ushort)(b + 1), (ushort)(b + 2), (ushort)b, (ushort)(b + 2), (ushort)(b + 3) });
            }
            return Build(v, ix);
        }
        Mesh MkSphere(int st, int sl)
        {
            var v = new List<VertexPositionNormalTexture>(); var ix = new List<ushort>();
            for (int i = 0; i <= st; i++) { float ph = MathF.PI * i / st; for (int j = 0; j <= sl; j++) { float th = MathHelper.TwoPi * j / sl; var n = new Vector3(MathF.Sin(ph) * MathF.Cos(th), MathF.Cos(ph), MathF.Sin(ph) * MathF.Sin(th)); v.Add(new VertexPositionNormalTexture(n * .5f, n, Vector2.Zero)); } }
            for (int i = 0; i < st; i++) for (int j = 0; j < sl; j++) { int a = i * (sl + 1) + j, b = a + sl + 1; ix.AddRange(new ushort[] { (ushort)a, (ushort)b, (ushort)(a + 1), (ushort)(a + 1), (ushort)b, (ushort)(b + 1) }); }
            return Build(v, ix);
        }
        Mesh MkRound(int seg, float topR)
        {
            var v = new List<VertexPositionNormalTexture>(); var ix = new List<ushort>(); float slope = .5f * (1 - topR);
            for (int j = 0; j <= seg; j++)
            {
                float a = j * MathHelper.TwoPi / seg, c = MathF.Cos(a), s = MathF.Sin(a); var n = Vector3.Normalize(new Vector3(c, slope, s));
                v.Add(new VertexPositionNormalTexture(new Vector3(c * .5f, -.5f, s * .5f), n, Vector2.Zero)); v.Add(new VertexPositionNormalTexture(new Vector3(c * .5f * topR, .5f, s * .5f * topR), n, Vector2.Zero));
            }
            for (int j = 0; j < seg; j++) { int b = j * 2; ix.AddRange(new ushort[] { (ushort)b, (ushort)(b + 1), (ushort)(b + 2), (ushort)(b + 1), (ushort)(b + 3), (ushort)(b + 2) }); }
            void Cap(float y, float rad, float ny)
            {
                int c0 = v.Count; v.Add(new VertexPositionNormalTexture(new Vector3(0, y, 0), new Vector3(0, ny, 0), Vector2.Zero));
                for (int j = 0; j <= seg; j++) { float a = j * MathHelper.TwoPi / seg; v.Add(new VertexPositionNormalTexture(new Vector3(MathF.Cos(a) * rad, y, MathF.Sin(a) * rad), new Vector3(0, ny, 0), Vector2.Zero)); }
                for (int j = 0; j < seg; j++) { ix.Add((ushort)c0); ix.Add((ushort)(c0 + 1 + j)); ix.Add((ushort)(c0 + 2 + j)); }
            }
            Cap(-.5f, .5f, -1); if (topR > 0) Cap(.5f, .5f * topR, 1);
            return Build(v, ix);
        }

        // ---------- camera, light, draw helpers ----------
        Vector3 MoonPos() => new Vector3(cam + W / 2f + 340, 560, -3300);
        Vector2 Sc(float gx, float gy, float gz = 110) { var vp = new Viewport(0, 0, (int)(W * renderScale), (int)(H * renderScale)); var p = vp.Project(new Vector3(gx, G - gy, gz), projM, view, Matrix.Identity); return new Vector2(p.X / renderScale, p.Y / renderScale); }
        Vector2 ScreenAt(float x, float worldY, float z) { var vp = new Viewport(0, 0, (int)(W * renderScale), (int)(H * renderScale)); var p = vp.Project(new Vector3(x, worldY, z), projM, view, Matrix.Identity); return new Vector2(p.X / renderScale, p.Y / renderScale); }
        Vector2 MoonScr() { var vp = new Viewport(0, 0, (int)(W * renderScale), (int)(H * renderScale)); var p = vp.Project(MoonPos(), projM, view, Matrix.Identity); return new Vector2(p.X / renderScale, p.Y / renderScale); }
        void SetCam(float sx, float sy)
        {
            var tgt = new Vector3(cam + W / 2f, 190 - camY, camZ);
            float pitch = camPitch;
            var offset = new Vector3(MathF.Sin(camYaw) * MathF.Cos(pitch), MathF.Sin(pitch), MathF.Cos(camYaw) * MathF.Cos(pitch)) * camDistance;
            var pos = tgt + offset + new Vector3(sx, -sy, 0);
            view = Matrix.CreateLookAt(pos, tgt, Vector3.Up); projM = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, W / (float)H, 30, 14000);
            be.View = view; be.Projection = projM;
        }
        void Lighting()
        {
            var th = L.Th; be.LightingEnabled = true; be.PreferPerPixelLighting = true;
            be.AmbientLightColor = Color.Lerp(th.Sky1, Color.Black, .2f).ToVector3() * .5f + new Vector3(.06f);
            be.DirectionalLight0.Enabled = true; be.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(.45f, -.55f, -.7f)); be.DirectionalLight0.DiffuseColor = Color.Lerp(th.Moon, WH, .3f).ToVector3() * .9f; be.DirectionalLight0.SpecularColor = th.Moon.ToVector3() * .6f;
            be.DirectionalLight1.Enabled = true; be.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-.8f, -.15f, .5f)); be.DirectionalLight1.DiffuseColor = th.Accent.ToVector3() * .5f; be.DirectionalLight1.SpecularColor = Vector3.Zero;
            be.DirectionalLight2.Enabled = true; be.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(0, .5f, -.8f)); be.DirectionalLight2.DiffuseColor = th.Fog.ToVector3() * .14f; be.DirectionalLight2.SpecularColor = Vector3.Zero;
            be.FogEnabled = true; be.FogColor = Color.Lerp(th.Sky1, th.Sky2, .6f).ToVector3(); be.FogStart = 950; be.FogEnd = 5400;
        }
        void Shape(Mesh m, Matrix w, Color c, Color? em = null, float al = 1)
        {
            be.World = w; be.DiffuseColor = c.ToVector3(); be.EmissiveColor = em.HasValue ? em.Value.ToVector3() : Vector3.Zero; be.Alpha = al; be.SpecularColor = new Vector3(specK);
            be.CurrentTechnique.Passes[0].Apply(); GraphicsDevice.SetVertexBuffer(m.VB); GraphicsDevice.Indices = m.IB; GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, m.Tris);
        }
        static Matrix Mx(float x, float y, float z, float sx, float sy, float sz, float ry = 0, float rz = 0) => Matrix.CreateScale(sx, sy, sz) * Matrix.CreateRotationZ(rz) * Matrix.CreateRotationY(ry) * Matrix.CreateTranslation(x, y, z);
        void Bx(float x, float y, float z, float sx, float sy, float sz, Color c, Color? em = null, float ry = 0, float rz = 0) => Shape(mCube, Mx(x, y, z, sx, sy, sz, ry, rz), c, em);
        void BeamXY(Mesh m, float x1, float y1, float x2, float y2, float z, float w, float dz, Color c, Color? em = null)
        { float dx = x2 - x1, dy = y2 - y1, len = Math.Max(.01f, MathF.Sqrt(dx * dx + dy * dy)); Shape(m, Matrix.CreateScale(w, len, dz) * Matrix.CreateRotationZ(MathF.Atan2(-dx, dy)) * Matrix.CreateTranslation((x1 + x2) / 2, (y1 + y2) / 2, z), c, em); }

        // ---------- rigs (characters built from shapes) ----------
        Rig MkRig(float X, float Y, float Z, float s, float yaw) => new Rig { S = s, R = Matrix.CreateScale(s) * Matrix.CreateRotationY(yaw) * Matrix.CreateTranslation(X, G - Y, Z) };
        void Pt(Rig r, Mesh m, float x, float y, float z, float sx, float sy, float sz, float rz, Color c, Color? em = null, float al = 1) => Shape(m, Matrix.CreateScale(sx, sy, sz) * Matrix.CreateRotationZ(rz) * Matrix.CreateTranslation(x, y, z) * r.R, c, em, al);
        void Bm(Rig r, Mesh m, Vector2 a, Vector2 b, float z, float w, float dz, Color c, Color? em = null, float al = 1)
        { var d = b - a; float len = Math.Max(.01f, d.Length()); var mid = (a + b) / 2; Shape(m, Matrix.CreateScale(w, len, dz) * Matrix.CreateRotationZ(MathF.Atan2(-d.X, d.Y)) * Matrix.CreateTranslation(mid.X, mid.Y, z) * r.R, c, em, al); }
        Vector3 GP(Rig r, float x, float y, float z = 0) => Vector3.Transform(new Vector3(x, y, z), r.R);
        void GlowG(Rig r, float x, float y, float z, float rad, Color c) { var g = GP(r, x, y, z); var p = ScreenAt(g.X, g.Y, g.Z); glows.Add(new GlowReq { X = p.X, Y = p.Y, R = rad * r.S, C = c }); }
        void GlowAt(float x, float worldY, float z, float rad, Color c) { var p = ScreenAt(x, worldY, z); glows.Add(new GlowReq { X = p.X, Y = p.Y, R = rad, C = c }); }
        void Blob(float X, float Y, float Z, float rad, float al) { float fy = FloorBelow(X, Y, Z); if (fy < 9999) Shape(mCyl, Mx(X, G - fy + 1.2f, Z, rad * 2, 1, rad * 1.4f), Color.Black, null, al * (1 - Cl((fy - Y) / 300, 0, .8f)) * .6f); }

        // ---------- sky backdrop (2D) ----------
        void Backdrop()
        {
            var th = L.Th; sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
            for (int i = 0; i < 56; i++) { float u = i / 55f; Color c = u < .6f ? Color.Lerp(th.Sky0, th.Sky1, u / .6f) : Color.Lerp(th.Sky1, th.Sky2, (u - .6f) / .4f); Box(W / 2, i * (H / 56f) + H / 112f, W, H / 56f + 1, 0, c); }
            if (th.Style != 2) for (int i = 0; i < 140; i++) { float a = .3f + .7f * MathF.Abs(MathF.Sin(t * 1.4f + stars[i, 3])); Box((stars[i, 0] - cam * .02f + W * 3) % W, stars[i, 1] * .8f, stars[i, 2], stars[i, 2], 0, Ad(WH, a * .8f)); }
            var mp = MoonScr(); Glow(mp.X, mp.Y, 760, Ad(th.Moon, .4f)); Glow(mp.X, mp.Y, 340, Ad(th.Moon, .45f)); sb.End();
        }

        // ---------- level geometry ----------
        void Seg3(float a, float b)
        {
            var th = L.Th; float x0 = Math.Max(a, cam - 1400), x1 = Math.Min(b, cam + W + 1400); if (x1 <= x0) return; float cx = (x0 + x1) / 2, w = x1 - x0;
            Bx(cx, -200, 110, w, 400, 840, th.Ground); Bx(cx, -.5f, 110, w, 3, 840, Color.Lerp(th.Ground, th.Accent, .12f), Em(th.Accent, .1f)); Bx(cx, 0, 520, w, 4, 4, th.Accent, Em(th.Accent, 1f));
            for (float tileX = (float)Math.Floor(x0 / 170) * 170; tileX < x1; tileX += 170)
                for (float z = -220; z <= 410; z += 150)
                {
                    float left = Math.Max(tileX, x0), right = Math.Min(tileX + 170, x1), width = right - left - 2;
                    if (width <= 0) continue;
                    float x = (left + right) / 2;
                    Color stone = Color.Lerp(th.Ground, th.Accent, Hs(tileX * .13f + z * .07f) * .08f);
                    Bx(x, 1.1f, z + 72, width, 1.1f, 142, stone);
                    if (Hs(tileX + z) > .74f) Bx(x, 1.8f, z + 72, Math.Min(36, width), .8f, 2, Color.Lerp(stone, th.Moon, .14f));
                }
            Color gc = Color.Lerp(th.Ground, Color.Black, .55f);
            for (float x = (float)Math.Floor(x0 / 55) * 55; x < x1; x += 55) { float h = 14 + Hs(x) * 34, zz = -150 + Hs(x * 1.3f) * 520, sw = MathF.Sin(t * 1.5f + x * .05f) * .12f; Shape(mCone, Mx(x, h / 2, zz, 9, h, 9, 0, sw), gc); }
            if (a > 0) { Bx(a + 3, -80, 110, 6, 320, 840, Color.Black); } Bx(b - 3, -80, 110, 6, 320, 840, Color.Black);
        }
        void Ground3()
        {
            float x0 = 0; foreach (var g in L.Gaps) { Seg3(x0, g.X); x0 = g.Y; if (g.Y > cam - 600 && g.X < cam + W + 600) Bx((g.X + g.Y) / 2, -480, 110, g.Y - g.X, 900, 840, new Color(2, 1, 4)); }
            Seg3(x0, L.GateX + 1200);
        }
        void Plats3()
        {
            var th = L.Th;
            foreach (var p in L.Plats)
            {
                if (p.X > cam + W + 400 || p.X + p.W < cam - 400) continue; float cx = p.X + p.W / 2, y = G - p.Y;
                Bx(cx, y - 15, 86, p.W, 30, 170, new Color(26, 20, 34)); Bx(cx, y - 1, 86, p.W + 4, 5, 174, new Color(54, 44, 66), Em(th.Accent, .12f)); Bx(cx, y, 86, p.W, 3, 3, th.Accent, Em(th.Accent, 1f));
                for (float x = p.X + 12; x < p.X + p.W; x += 24) { float h = 16 + Hs(x) * 24; Shape(mCone, Mx(x, y - 30 - h / 2, 86, 14, h, 14, 0, MathF.PI), new Color(14, 10, 20)); }
                for (float x = p.X + 20; x < p.X + p.W; x += 40) Bx(x, y - 14, 87, 10, 10, 2, th.Accent, Em(th.Accent, .8f));
            }
        }
        void Spikes3()
        {
            foreach (var s in L.Spikes)
            {
                if (s.X > cam + W + 400 || s.Y < cam - 400) continue;
                for (float x = s.X; x < s.Y; x += 28)
                    for (float zz = -250; zz <= 450; zz += 70)
                    {
                        float h = 44 + Hs(x + zz) * 8;
                        Shape(mCone, Mx(x + 14, h / 2, zz, 20, h, 20), new Color(30, 26, 38));
                        Shape(mSph, Mx(x + 14, h - 3, zz, 8, 8, 8), new Color(90, 10, 20), Em(RD, .9f));
                    }
                GlowAt((s.X + s.Y) / 2, G - 10, 110, (s.Y - s.X) / 2 + 50, Ad(RD, .18f));
            }
        }
        void Checks3()
        {
            for (int i = 0; i < L.Checks.Count; i++)
            {
                float x = L.Checks[i]; if (x < cam - 400 || x > cam + W + 400) continue; bool on = lit[i]; var ac = L.Th.Accent;
                Bx(x, 14, 110, 58, 28, 58, new Color(30, 26, 38)); Shape(mCyl, Mx(x, 58, 110, 22, 60, 22), new Color(38, 34, 48)); Bx(x, 98, 110, 44, 38, 44, new Color(24, 20, 32), on ? Em(ac, .15f) : (Color?)null); Shape(mPyr, Mx(x, 128, 110, 70, 26, 70, MathF.PI / 4), new Color(20, 16, 26));
                if (on) { Bx(x, 98, 110, 28, 24, 46, ac, Em(ac, 1f)); GlowAt(x, G - 98, 110, 250 * (1 + .12f * MathF.Sin(t * 9 + x)), Ad(ac, .5f)); for (int k = 0; k < 6; k++) { float yy = (t * 45 + k * 28) % 170; GlowAt(x + MathF.Sin(t * 2 + k * 1.7f) * 22, G - 120 - yy, 110, 10, Ad(ac, 1 - yy / 170f)); } }
            }
        }
        void Gate3()
        {
            float x = L.GateX; if (x < cam - 600 || x > cam + W + 600) return; var ac = L.Th.Accent; Color st = new Color(24, 18, 30);
            Shape(mCyl, Mx(x - 110, 190, 110, 40, 380, 40), st, Em(ac, .1f)); Shape(mCyl, Mx(x + 110, 190, 110, 40, 380, 40), st, Em(ac, .1f)); Bx(x, 385, 110, 310, 26, 40, st); Bx(x, 345, 110, 250, 12, 30, st); Shape(mPyr, Mx(x, 425, 110, 360, 50, 120, MathF.PI / 4), st);
            Bx(x, 380, 132, 300, 4, 4, ac, Em(ac, bossDead ? 1f : .3f));
            if (bossDead) { GlowAt(x, G - 190, 110, 260, Ad(ac, .8f)); for (int k = 0; k < 10; k++) { float yy = (t * 70 + k * 40) % 380; GlowAt(x + MathF.Sin(t * 2 + k) * 60, G - yy, 110, 14, Ad(WH, .8f)); } }
        }
        void GatePortal3()
        {
            float x = L.GateX; if (x < cam - 600 || x > cam + W + 600) return; var ac = L.Th.Accent;
            if (bossDead) Bx(x, 190, 110, 176, 360, 6, ac, Em(ac, 1f), 0, 0); else { Bx(x, 190, 110, 176, 360, 8, new Color(30, 0, 10), Em(RD, .35f + .1f * MathF.Sin(t * 3))); BeamXY(mCube, x - 88, 10, x + 88, 370, 116, 5, 5, RD, Em(RD, .8f)); BeamXY(mCube, x + 88, 10, x - 88, 370, 116, 5, 5, RD, Em(RD, .8f)); }
        }
        void Souls3() { foreach (var s in L.Souls) { if (s.Got || s.X < cam - 300 || s.X > cam + W + 300) continue; float by = G - s.Y + MathF.Sin(t * 3 + s.X) * 8; Shape(mSph, Mx(s.X, by, 110, 18, 18, 18), new Color(240, 220, 255), Em(VI, 1f)); Shape(mCone, Mx(s.X - 4, by - 20, 110, 12, 28, 12, 0, MathF.PI + MathF.Sin(t * 4) * .3f), VI, Em(VI, .8f)); GlowAt(s.X, by, 110, 60, Ad(VI, .8f)); } }

        // ---------- background scenery in true 3D depth ----------
        void BgLayer(float z, float sp, int kind, float sc, Color col, float jz)
        {
            float hw = (880 - z) * .7364f + 300, cx = cam + W / 2f; int i0 = (int)MathF.Floor((cx - hw) / sp), i1 = (int)MathF.Ceiling((cx + hw) / sp);
            for (int i = i0; i <= i1; i++)
            {
                float h = Hs(i * 1.7f + z * .01f), x = i * sp + (h - .5f) * sp * .6f, zz = z + (Hs(i * 5.3f) - .5f) * jz;
                if (kind == 0) Bamboo3(x, zz, sc, col, h); else if (kind == 1) Grave3(x, zz, sc, col, h); else Temple3(x, zz, sc, col, h);
            }
        }
        void Bamboo3(float x, float zz, float sc, Color col, float h)
        {
            for (int j = 0; j < 2; j++)
            {
                float bx = x + j * 26 * sc, hh = (450 + Hs(h * 10 + j) * 300) * sc, sw = MathF.Sin(t * .8f + bx * .01f) * .02f; Shape(mCyl, Mx(bx, hh / 2, zz, 10 * sc, hh, 10 * sc, 0, sw), col);
                for (float y = 70 * sc; y < hh; y += 85 * sc) Shape(mCyl, Mx(bx, y, zz, 14 * sc, 5 * sc, 14 * sc), Color.Lerp(col, WH, .08f));
                for (int l = 0; l < 4; l++) { float sd = l % 2 == 0 ? 1 : -1; Shape(mCone, Mx(bx + sd * 28 * sc, hh - l * 40 * sc, zz, 14 * sc, 80 * sc, 14 * sc, 0, -sd * 1.2f), col); }
            }
        }
        void Grave3(float x, float zz, float sc, Color col, float h)
        {
            if (h < .34f) { Bx(x, 50 * sc, zz, 42 * sc, 100 * sc, 16 * sc, col); Shape(mSph, Mx(x, 100 * sc, zz, 42 * sc, 34 * sc, 16 * sc), col); }
            else if (h < .6f) { Bx(x, 90 * sc, zz, 14 * sc, 180 * sc, 14 * sc, col); Bx(x, 130 * sc, zz, 80 * sc, 14 * sc, 14 * sc, col); }
            else
            {
                Shape(mFrus, Mx(x, 130 * sc, zz, 24 * sc, 260 * sc, 24 * sc), col);
                for (int b = 0; b < 5; b++) { float sd = b % 2 == 0 ? 1 : -1, y0 = (100 + b * 36) * sc; BeamXY(mCyl, x, y0, x + sd * (60 + b * 24) * sc, (170 + b * 50) * sc, zz, 6 * sc, 6 * sc, col); BeamXY(mCyl, x + sd * (60 + b * 24) * sc, (170 + b * 50) * sc, x + sd * (90 + b * 24) * sc, (160 + b * 60) * sc, zz, 3 * sc, 3 * sc, col); }
            }
            Shape(mSph, Mx(x, 80 * sc, zz + 20, 14 * sc, 14 * sc, 14 * sc), Color.Black, Em(L.Th.Accent, .25f + .15f * MathF.Sin(t * 2 + x * .01f)));
        }
        void Temple3(float x, float zz, float sc, Color col, float h)
        {
            if (h < .5f)
            {
                for (int i = 0; i < 4; i++) { float w = (110 - i * 20) * sc, hh = 46 * sc; Bx(x, i * hh + hh * .35f, zz, w - 16 * sc, hh * .7f, w - 16 * sc, col); Shape(mPyr, Mx(x, i * hh + hh * .85f, zz, (w + 56 * sc) * 1.2f, hh * .55f, (w + 56 * sc) * 1.2f, MathF.PI / 4), col); Bx(x, i * hh + hh * .35f, zz + (w - 16 * sc) / 2, 14 * sc, 18 * sc, 2, col, Em(L.Th.Accent, .9f)); }
                Bx(x, 4 * 46 * sc + 17 * sc, zz, 4 * sc, 34 * sc, 4 * sc, col);
            }
            else { Shape(mCyl, Mx(x - 70 * sc, 110 * sc, zz, 16 * sc, 220 * sc, 16 * sc), col); Shape(mCyl, Mx(x + 70 * sc, 110 * sc, zz, 16 * sc, 220 * sc, 16 * sc), col); Bx(x, 215 * sc, zz, 210 * sc, 18 * sc, 18 * sc, col, Em(L.Th.Accent, .15f)); Bx(x, 180 * sc, zz, 160 * sc, 9 * sc, 12 * sc, col); }
        }
        void Scenery3()
        {
            var th = L.Th; float cx = cam + W / 2f;
            Shape(mSph, Mx(MoonPos().X, MoonPos().Y, MoonPos().Z, 420, 420, 420), Color.Lerp(th.Moon, Color.Black, .5f), Em(th.Moon, .85f));
            for (int i = (int)MathF.Floor((cx - 4200) / 1100); i <= (int)MathF.Ceiling((cx + 4200) / 1100); i++) { float h = Hs(i * 3.3f), x = i * 1100 + (h - .5f) * 500, hh = 900 + Hs(i * 7.7f) * 700; Shape(mCone, Mx(x, hh / 2 - 60, -2700, 1500, hh, 1500), th.Far); }
            BgLayer(-1500, 380, th.Style, .85f, Color.Lerp(th.Far, th.Mid, .6f), 500); BgLayer(-650, 330, th.Style, 1.05f, th.Mid, 300); BgLayer(-230, 520, th.Style, 1.4f, th.Near, 120);
        }
        void Foreground3()
        {
            Color b = new Color(1, 0, 3); float cx = cam + W / 2f, hw = (880 - 330) * .7364f + 260;
            for (float x = (float)Math.Floor((cx - hw) / 45) * 45; x < cx + hw; x += 45) { float h = 40 + Hs(x) * 90, zz = 300 + Hs(x * 2.1f) * 120, sw = MathF.Sin(t * 1.4f + x * .03f) * .2f; Shape(mCone, Mx(x, h / 2 - 8, zz, 13, h, 13, 0, sw), b); }
            float hw2 = (880 - 380) * .7364f + 320;
            for (int i = (int)MathF.Floor((cx - hw2) / 1400); i <= (int)MathF.Ceiling((cx + hw2) / 1400); i++) { float x = i * 1400 + Hs(i * 9.1f) * 500; Shape(mCyl, Mx(x, 520, 380, 110 + Hs(i * 2.2f) * 70, 1200, 110), b); BeamXY(mCyl, x, 900, x + 180, 1000, 380, 16, 16, b); BeamXY(mCyl, x, 760, x - 160, 840, 380, 12, 12, b); }
        }

        // ---------- THE GRIM REAPER (3D, built from shapes) ----------
        void Reaper3D(float X, float Y, float Z, int f, float a, float tt, float vx, bool air, float at, int cb, bool chg, Color ti, float s = 1, float yaw = 0)
        {
            Rig r = MkRig(X, Y, Z, s, yaw); bool run = Math.Abs(vx) > 60 && !air; float flow = Cl(-vx * f * .055f, -12, 40) + (air ? 10 : 0), bob = (run ? MathF.Abs(MathF.Sin(tt * 13)) * 4 : MathF.Sin(tt * 2.2f) * 3) + 4;
            Color dk = new Color(7, 5, 12), dk2 = new Color(16, 11, 26), bone = new Color(216, 208, 190), eye = ti == CY ? new Color(170, 255, 255) : new Color(224, 200, 255);
            if (s == 1) Blob(X, Y, Z, 38, a);
            GlowG(r, 0, 65 + bob, 0, 110 * s, Ad(ti, .28f * a));
            for (int i = 0; i < 6; i++)
            {
                float u = 1 - i / 5f, rad = 46 * MathF.Pow(.8f, i), ox = -4 + flow * u * u * 1.1f + MathF.Sin(tt * 3.2f + u * 5) * (1 + 4 * u);
                Pt(r, mFrus, ox, bob + 14 + i * 20, 0, rad * 2, 26, rad * 1.6f, 0, Color.Lerp(dk2, dk, u), Em(ti, .05f * u), a);
            }
            for (int j = 0; j < 10; j++)
            {
                float ang = j * MathHelper.TwoPi / 10, len = 26 + Hs(j * 3.3f) * 36, sway = MathF.Sin(tt * 4 + j * .9f) * (5 + flow * .25f) + flow * .5f;
                Pt(r, mCone, MathF.Cos(ang) * 40 + flow * .6f, bob + 10 - len / 2 + 6, MathF.Sin(ang) * 30, 11, len, 6, MathF.PI + sway * .03f, dk, Em(ti, .08f), a);
            }
            Pt(r, mSph, -2, bob + 108, 0, 54, 36, 42, 0, dk2, null, a); Pt(r, mSph, 4, bob + 132, 0, 46, 48, 44, 0, dk2, null, a); Pt(r, mCone, -22, bob + 124, 0, 26, 52, 26, 1.9f, dk2, Em(ti, .05f), a);
            Pt(r, mSph, 14, bob + 128, 5, 24, 30, 20, 0, Color.Black, null, a);
            foreach (float ez in new[] { 6f, -3f }) { Pt(r, mSph, 22, bob + 130, ez, 7, 7, 7, 0, eye, Em(eye, 1f), a); GlowG(r, 22, bob + 130, ez, 18, Ad(ti, a)); }
            float ang0 = ScytheAng(at, cb, chg, air, run, tt), ca = MathF.Cos(ang0), sa = MathF.Sin(ang0); var d = new Vector2(ca, -sa); var nrm = new Vector2(-sa, -ca);
            float reach = at >= 0 ? MathF.Sin(Cl(at, 0, 1) * MathF.PI) * 16 : 0; var hand = new Vector2(20 + reach, bob + 82 + (at >= 0 ? 4 : 0)); var pole0 = hand - d * 72; var pole1 = hand + d * 104; var rear = hand - d * 52;
            var sh1 = new Vector2(12, bob + 104); var sh2 = new Vector2(-6, bob + 102); var el1 = (sh1 + hand) / 2 + new Vector2(0, -14); var el2 = (sh2 + rear) / 2 + new Vector2(0, -14);
            Bm(r, mCyl, sh2, el2, 2, 9, 9, dk, null, a); Bm(r, mCyl, el2, rear, 3, 8, 8, dk, null, a); Pt(r, mSph, rear.X, rear.Y, 4, 11, 11, 11, 0, bone, null, a);
            specK = .7f; Bm(r, mCyl, pole0, pole1, 12, 5.5f, 5.5f, new Color(52, 44, 68), Em(ti, .08f), a);
            for (int k = 1; k <= 3; k++) { var q = Vector2.Lerp(pole0, pole1, k * .22f); Pt(r, mSph, q.X, q.Y, 12, 6, 6, 6, 0, ti, Em(ti, 1f), a); }
            Pt(r, mSph, pole1.X, pole1.Y, 12, 12, 12, 12, 0, new Color(96, 86, 116), null, a);
            Vector2 P0 = pole1, prev = P0; float Rb = 72;
            for (int i = 1; i <= 12; i++)
            {
                float ph = i / 12f * 2.0f, aa = Rb * MathF.Sin(ph), bb = Rb * (1 - MathF.Cos(ph)) * .9f; var cur2 = P0 + nrm * aa - d * bb; float u = i / 12f, w = 14 * MathF.Pow(1 - u, .8f) + 1.6f;
                Bm(r, mCube, prev, cur2, 12, w, 3.5f, Color.Lerp(new Color(64, 60, 84), new Color(236, 240, 255), u), Em(ti, (at >= 0 ? .55f : .3f) + .6f * u), a);
                if (i == 6) GlowG(r, cur2.X, cur2.Y, 12, at >= 0 ? 60 : 30, Ad(ti, (at >= 0 ? .55f : .25f) * a)); if (i == 12) GlowG(r, cur2.X, cur2.Y, 12, 30, Ad(ti, .9f * a)); prev = cur2;
            }
            specK = 0;
            Bm(r, mCyl, sh1, el1, 14, 9, 9, dk, null, a); Bm(r, mCyl, el1, hand, 14, 8, 8, dk, null, a); Pt(r, mSph, hand.X, hand.Y, 12, 11, 11, 11, 0, bone, null, a);
        }

        // ---------- enemies and bosses (3D shapes) ----------
        void Foe3D(Foe e)
        {
            int T = e.Type, f = e.F; bool sm = T == 1, ar = T == 2, fl = e.Flash > 0; float s = e.S, al = e.Al * (e.Boss && e.Kind == 1 ? .88f : 1);
            Color col, ac, trim;
            if (e.Boss) { if (e.Kind == 0) { col = new Color(78, 18, 22); ac = new Color(255, 70, 30); trim = OR; } else if (e.Kind == 1) { col = new Color(18, 32, 54); ac = CY; trim = new Color(160, 220, 255); } else { col = new Color(56, 14, 8); ac = new Color(255, 140, 30); trim = new Color(255, 210, 90); } }
            else if (sm) { col = new Color(52, 26, 34); ac = OR; trim = new Color(255, 200, 110); } else if (ar) { col = new Color(36, 28, 60); ac = new Color(164, 92, 255); trim = new Color(200, 160, 255); } else { col = new Color(22, 26, 46); ac = RD; trim = new Color(110, 130, 180); }
            if (fl) col = WH; Color dk = Color.Lerp(col, Color.Black, .5f), acE = Em(ac, .9f), trE = Em(trim, .45f);
            Rig r = MkRig(e.X, e.Y, e.Z, s, e.Yaw); bool air = e.Y < G - 6; float w = MathF.Sin(e.Walk * 12) * (e.Mv != 0 ? 1 : 0), idle = MathF.Sin(e.Ph * 2.4f) * 2;
            Blob(e.X, e.Y, e.Z, 42 * s, al); GlowAt(e.X, G - e.Y + 60 * s, e.Z, 90 * s, Ad(ac, (fl ? .6f : .2f) * al));
            if (e.Boss) { GlowAt(e.X, G - e.Y + 95 * s, e.Z, (170 + MathF.Sin(t * 3) * 18) * s * .7f, Ad(ac, .25f * al)); if (e.Kind == 2) for (int k = 0; k < 6; k++) { float yy = (t * 90 + k * 40) % 240; GlowAt(e.X + MathF.Sin(t * 3 + k) * 40 * s, G - e.Y + yy * s, e.Z, 10 * s, Ad(ac, 1 - yy / 240f)); } }
            for (int side = -1; side <= 1; side += 2)
            {
                float ph = w * side; var hip = new Vector2(side * 6, 44); var knee = air ? new Vector2(side * 10, 30) : new Vector2(side * 6 + ph * 10, 24 + MathF.Abs(ph) * 4); var foot = air ? new Vector2(side * 14, 8) : new Vector2(side * 6 + ph * 20, Math.Max(0, ph) * 7);
                Bm(r, mCyl, hip, knee, side * 6, 10, 10, col, null, al); Bm(r, mCyl, knee, foot, side * 6, 9, 9, col, null, al); Pt(r, mCube, foot.X + 4, foot.Y + 3, side * 6, 18, 6, 11, 0, dk, null, al);
            }
            float bw = sm || e.Boss ? 52 : 38;
            Pt(r, mCube, idle * .3f, 68, 0, bw, 50, 26, 0, col, null, al); Pt(r, mFrus, 0, 34, 0, bw + 10, 26, 30, 0, dk, null, al); Pt(r, mCube, 0, 46, 0, bw * .95f, 5, 28, 0, trim, trE, al);
            for (int k = 0; k < 4; k++) if (sm || e.Boss) Pt(r, mCube, 0, 90 - k * 11, 0, bw + 1, 2.4f, 27, 0, trim, trE, al);
            if (sm || e.Boss) foreach (int sd in new[] { -1, 1 }) Pt(r, mCube, sd * (bw / 2 + 3), 90, 0, 20, 14, 34, 0, Color.Lerp(col, trim, .3f), null, al);
            if (ar) Pt(r, mCube, -15, 80, -12, 9, 36, 9, -.3f, dk, null, al);
            if (T != 1 && !e.Boss || e.Kind == 1) for (int k = 0; k < 6; k++) { var p = new Vector2(-8 - k * 9, 92 + MathF.Sin(t * 9 + k) * 5 - k * 3); var q = new Vector2(-8 - (k + 1) * 9, 92 + MathF.Sin(t * 9 + k + 1) * 5 - (k + 1) * 3); Bm(r, mCube, p, q, -2, 4.5f, 3, ac, acE, al); }
            Pt(r, mSph, idle * .3f, 106, 0, 27, 27, 27, 0, col, null, al);
            if (sm || e.Boss && e.Kind != 1) { Pt(r, mSph, 0, 114, 0, 38, 24, 36, 0, Color.Lerp(col, trim, .2f), null, al); Pt(r, mCyl, 0, 104, 0, 46, 4, 44, 0, trim, trE, al); foreach (int sd in new[] { -1, 1 }) Pt(r, mCone, sd * 12, 134, 0, 9, 32, 9, -sd * .5f, ac, acE, al); Pt(r, mCube, 8, 98, 0, 17, 10, 26, 0, new Color(80, 12, 22), null, al); }
            else if (ar) Pt(r, mCone, 0, 124, 0, 68, 30, 68, 0, Color.Lerp(col, trim, .25f), null, al); else Pt(r, mCube, 0, 109, 0, 28, 4.5f, 29, 0, ac, acE, al);
            if (e.Boss) foreach (int sd in new[] { -1, 1 }) Pt(r, mCone, sd * 14, 140, 0, 13, 48, 13, -sd * .45f, ac, acE, al);
            Pt(r, mCube, 10, 108, 11, 12, 3.6f, 3, 0, ac, Em(ac, 1f), al); GlowG(r, 10, 108, 11, 22, Ad(ac, al));
            var sh = new Vector2(8, 92); var gp = new Vector2(16, 72); specK = .8f;
            if (T == 0 && !e.Boss)
            {
                float a1 = e.St == 1 ? -2f : e.St == 2 ? t * 22 : .6f;
                for (int k = 0; k < 2; k++) { var g = new Vector2(14 + k * 3, 72 - k * 4); float aa = a1 + k * 1.2f; var tp = g + new Vector2(MathF.Cos(aa), -MathF.Sin(aa)) * 46; Bm(r, mCube, g, tp, 12 + k * 4, 3.6f, 2, new Color(232, 240, 255), Em(ac, e.St == 2 ? .9f : .4f), al); GlowG(r, tp.X, tp.Y, 12, 22, Ad(ac, (e.St == 2 ? .8f : .4f) * al)); }
                Bm(r, mCyl, sh, gp, 8, 9, 9, col, null, al);
            }
            else if (T == 1 || e.Boss)
            {
                float wang = e.St == 1 ? -2.1f : (e.St == 3 && e.Tm > .35f && !e.Boss) || e.St == 5 ? 1.1f : .5f, len = e.Boss ? 125 : 100; var dd = new Vector2(MathF.Cos(wang), -MathF.Sin(wang)); var tp = gp + dd * len;
                Bm(r, mCyl, sh, gp, 10, 10, 10, col, null, al); Bm(r, mCube, gp - dd * 20, tp, 12, 5, 2.4f, new Color(228, 236, 255), Em(ac, .7f), al); Pt(r, mCube, gp.X, gp.Y, 12, 7, 16, 9, 0, trim, trE, al); GlowG(r, tp.X, tp.Y, 12, e.St == 1 ? 44 : 26, Ad(ac, (e.St == 1 ? 1f : .5f) * al));
            }
            else
            {
                Vector2 pr = new Vector2(28, 76); float pull = e.St == 1 ? 14 : 0; Vector2 b0 = new Vector2(28, 114), b1 = new Vector2(38, 96), b2 = new Vector2(40, 76), b3 = new Vector2(38, 56), b4 = new Vector2(28, 38);
                Bm(r, mCube, b0, b1, 12, 4, 4, new Color(86, 62, 44), null, al); Bm(r, mCube, b1, b2, 12, 4, 4, new Color(86, 62, 44), null, al); Bm(r, mCube, b2, b3, 12, 4, 4, new Color(86, 62, 44), null, al); Bm(r, mCube, b3, b4, 12, 4, 4, new Color(86, 62, 44), null, al);
                Bm(r, mCube, b0, new Vector2(28 - pull, 76), 12, 1.6f, 1.6f, trim, trE, al); Bm(r, mCube, new Vector2(28 - pull, 76), b4, 12, 1.6f, 1.6f, trim, trE, al); Bm(r, mCyl, sh, gp, 8, 8, 8, col, null, al);
                if (e.St == 1) { var tp = new Vector2(84, 76); Bm(r, mCube, new Vector2(14, 76), tp, 12, 3, 3, new Color(220, 200, 160), null, al); GlowG(r, tp.X, tp.Y, 12, 32, Ad(ac, al)); }
            }
            specK = 0;
        }

        // ---------- full scene ----------
        void World3D()
        {
            Scenery3(); Ground3(); Plats3(); Spikes3(); Checks3(); Gate3(); Souls3();
            foreach (var k in clones) Reaper3D(k.X, G, k.Z, 1, Cl(k.L, 0, .7f), k.T, k.Mv * 300, false, k.At >= 0 ? k.At / .28f : -1, 0, false, CY, 1, k.Yaw);
            foreach (var e in en) if (e.X > cam - 500 && e.X < cam + W + 500) Foe3D(e);
            if (!P.Dead && !(P.Inv > 0 && (int)(t * 30) % 2 == 0))
            {
                float atp = P.Atk > 0 ? P.AtkT / (P.Cb == 2 ? .42f : .28f) : -1;
                Reaper3D(P.X, P.Y, P.Z, 1, P.Dash > 0 ? .35f : 1f, t, P.Vx, !P.Ground, atp, P.Cb, P.Chg > 0, PU, 1, P.Yaw);
            }
            if (ult > 0) Reaper3D(P.X, G + 40, P.Z, 1, .5f, t, 0, false, Cl((1 - ult / 1.4f) * 1.3f, 0, 1), 0, false, VI, 3.8f, P.Yaw);
            Foreground3(); GatePortal3();
        }

        // ---------- screen-space effects projected from the 3D scene ----------
        void Overlay()
        {
            var mp = MoonScr();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Matrix.CreateScale(renderScale));
            for (int k = 0; k < 7; k++) Tri(mp.X + (k - 3) * 110, mp.Y + 360, 150 + (k % 3) * 50, 780, (k - 3) * .07f + MathF.Sin(t * .3f + k) * .02f, Ad(L.Th.Moon, .035f));
            foreach (var g in glows) Glow(g.X, g.Y, g.R, g.C);
            foreach (var e in en)
            {
                if (e.X < cam - 300 || e.X > cam + W + 300) continue;
                var top = ScreenAt(e.X, G - e.Y + 150 * e.S, e.Z);
                if (e.St == 1) { Glow(top.X, top.Y + 14, 55, Ad(RD, .9f)); Txt("!", top.X - 8, top.Y - 12, 46, RD, 0); }
                if (!e.Boss && e.Hp < e.Mh && e.Aggro) { Box(top.X, top.Y, 48, 5, 0, Color.Black * .55f); Box(top.X - 24 + 24 * e.Hp / e.Mh, top.Y, 48 * e.Hp / e.Mh, 5, 0, RD); }
            }
            foreach (var p in proj) DrawProj(p);
            DrawFx();
            for (int i = 0; i < 120; i++) { bool a = i % 4 != 0; Glow(amb[i, 0], amb[i, 1], amb[i, 4] * (a ? 3 : 2), Ad(L.Th.Style == 0 ? new Color(200, 255, 120) : L.Th.Style == 1 ? new Color(150, 220, 255) : new Color(255, 150, 50), a ? .6f : .35f)); }
            sb.End();
        }

        protected override void Draw(GameTime gt)
        {
            var ms = Mouse.GetState();
            if (ms.RightButton == ButtonState.Pressed)
            {
                if (cameraDragging) { var delta = ms.Position - cameraMouse; camYaw -= delta.X * .006f; camPitch = Cl(camPitch + delta.Y * .004f, -.12f, .72f); }
                cameraDragging = true; cameraMouse = ms.Position;
            }
            else cameraDragging = false;
            int wheel = ms.ScrollWheelValue;
            if (wheel != cameraWheel)
            {
                camDistance = Cl(camDistance - (wheel - cameraWheel) * .45f, 540, 1500); cameraWheel = wheel;
            }
            cam += (Cl(P.X - W * .5f, 0, Math.Max(0, L.GateX + 400 - W)) - cam) * .08f; camZ += (P.Z - camZ) * .08f; camY += (Cl(P.Y - 500, -220, 0) - camY) * .06f;
            float sx = Rf(-1, 1) * shake, sy = Rf(-1, 1) * shake;
            SetCam(sx, sy); Lighting();
            GraphicsDevice.SetRenderTarget(rt); GraphicsDevice.Viewport = new Viewport(0, 0, rt.Width, rt.Height); GraphicsDevice.Clear(Color.Black);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Matrix.CreateScale(renderScale));
            for (int i = 0; i < 56; i++) { float u = i / 55f; Color c = u < .6f ? Color.Lerp(L.Th.Sky0, L.Th.Sky1, u / .6f) : Color.Lerp(L.Th.Sky1, L.Th.Sky2, (u - .6f) / .4f); Box(W / 2, i * (H / 56f) + H / 112f, W, H / 56f + 1, 0, c); }
            if (L.Th.Style != 2) for (int i = 0; i < 140; i++) { float a = .3f + .7f * MathF.Abs(MathF.Sin(t * 1.4f + stars[i, 3])); Box((stars[i, 0] - cam * .02f + W * 3) % W, stars[i, 1] * .8f, stars[i, 2], stars[i, 2], 0, Ad(WH, a * .8f)); }
            var mp = MoonScr(); Glow(mp.X, mp.Y, 760, Ad(L.Th.Moon, .4f)); Glow(mp.X, mp.Y, 340, Ad(L.Th.Moon, .45f)); sb.End();
            GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1f, 0);
            GraphicsDevice.DepthStencilState = DepthStencilState.Default; GraphicsDevice.RasterizerState = RasterizerState.CullNone; GraphicsDevice.BlendState = BlendState.AlphaBlend; GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
            glows.Clear(); World3D();
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Overlay();
            if (ult > 0) { sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Matrix.CreateScale(renderScale)); sb.Draw(px, new Rectangle(0, 0, W, H), new Color(10, 0, 25) * (.55f * Math.Min(1, ult))); sb.End(); }
            GraphicsDevice.SetRenderTarget(small); GraphicsDevice.Viewport = new Viewport(0, 0, small.Width, small.Height); sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp); sb.Draw(rt, new Rectangle(0, 0, small.Width, small.Height), WH); sb.End();
            GraphicsDevice.SetRenderTarget(null); GraphicsDevice.Viewport = GraphicsDevice.PresentationParameters.Bounds; GraphicsDevice.Clear(Color.Black);
            var pp = GraphicsDevice.PresentationParameters; float scl = Math.Min(pp.BackBufferWidth / (float)W, pp.BackBufferHeight / (float)H);
            var m = Matrix.CreateScale(scl) * Matrix.CreateTranslation((pp.BackBufferWidth - W * scl) / 2, (pp.BackBufferHeight - H * scl) / 2, 0);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, m);
            sb.Draw(rt, new Rectangle(0, 0, W, H), WH); sb.Draw(small, new Rectangle(0, 0, W, H), new Color(110, 110, 110, 0));
            Box(W / 2, H / 2, W, H, 0, Ad(L.Th.Accent, .035f)); sb.Draw(vig, new Rectangle(0, 0, W, H), WH); sb.Draw(vig, new Rectangle(0, 0, W, H), Color.White * .5f);
            for (int i = 0; i < 70; i++) Box(Rf(0, W), Rf(0, H), 2, 2, 0, Color.White * Rf(.03f, .09f));
            if (state == "play" && P.Hp < 35) Box(W / 2, H / 2, W, H, 0, new Color(120, 0, 20) * (.12f + .1f * MathF.Sin(t * 8)));
            if (flash > 0) Box(W / 2, H / 2, W, H, 0, WH * (flash * .6f));
            Box(W / 2, 14, W, 28, 0, Color.Black * .85f); Box(W / 2, H - 14, W, 28, 0, Color.Black * .0f);
            if (state == "play") DrawHud();
            if (state == "title")
            {
                Box(W / 2, H / 2, W, H, 0, Color.Black * .5f); Txt("KADAYA", W / 2, 80, 190, new Color(225, 195, 255), 1, PU);
                Txt("THE GRIM REAPER", W / 2, 290, 44, new Color(255, 106, 128), 1, RD);
                if ((int)(t * 1.6f) % 2 == 0) Txt(lv > 0 ? "ENTER  -  CONTINUE  (LEVEL " + (lv + 1) + ")" : "ENTER  -  START", W / 2, 370, 38, WH, 1);
                Txt("N  -  NEW GAME", W / 2, 425, 24, new Color(180, 160, 220), 1);
                Txt("WASD MOVE   SPACE JUMP   LEFT CLICK SCYTHE   SHIFT DODGE   E SHADOW CLONES", W / 2, H - 120, 22, new Color(207, 192, 255), 1, PU);
                Txt("RIGHT MOUSE DRAG CAMERA   MOUSE WHEEL ZOOM   F7 RESET CAMERA   F11 FULLSCREEN", W / 2, H - 82, 20, new Color(207, 192, 255), 1, PU);
                Txt("F6 RENDER QUALITY", W / 2, H - 48, 18, new Color(207, 192, 255), 1, PU);
            }
            if (state == "dead") { Box(W / 2, H / 2, W, H, 0, new Color(20, 0, 10) * .65f); Txt("YOU HAVE BEEN REAPED", W / 2, 200, 84, RD, 1); Txt("LEVEL " + (lv + 1) + "     SOULS " + kills, W / 2, 320, 38, WH, 1); if ((int)(t * 1.6f) % 2 == 0) Txt("ENTER  -  RISE AGAIN FROM THE LAST SHRINE", W / 2, 400, 32, VI, 1); }
            if (state == "win") { Box(W / 2, H / 2, W, H, 0, new Color(10, 0, 20) * .6f); Txt("LEVEL " + (lv + 1) + " COMPLETE", W / 2, 190, 86, new Color(255, 220, 120), 1, OR); Txt("SOULS REAPED  " + kills, W / 2, 310, 38, WH, 1); if (winT > 1 && (int)(t * 1.6f) % 2 == 0) Txt(lv >= 2 ? "ENTER  -  FINISH" : "ENTER  -  NEXT LEVEL", W / 2, 390, 34, VI, 1); }
            if (state == "end") { Box(W / 2, H / 2, W, H, 0, Color.Black * .85f); Txt("THE REAPER'S JOURNEY ENDS", W / 2, 200, 70, new Color(225, 195, 255), 1, PU); Txt("THANK YOU FOR PLAYING KADAYA", W / 2, 310, 36, WH, 1); if ((int)(t * 1.6f) % 2 == 0) Txt("ENTER  -  RETURN TO TITLE", W / 2, 390, 30, VI, 1); }
            sb.End(); base.Draw(gt);
        }
    }
}

