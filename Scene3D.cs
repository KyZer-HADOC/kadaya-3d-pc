using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kadaya
{
    class Mesh { public VertexBuffer VB; public IndexBuffer IB; public int Tris; }
    struct Rig { public Matrix R; public float S; }
    struct GlowReq { public float X, Y, R; public Color C; }

    public partial class Game1
    {
        BasicEffect be; Mesh mCube, mSph, mCyl, mCone, mPyr, mFrus; Matrix view, projM; float specK; int appliedMeshQuality = -1;
        List<GlowReq> glows = new List<GlowReq>();
        static Color Em(Color c, float k) => new Color(c.R * k / 255f, c.G * k / 255f, c.B * k / 255f);

        // ---------- procedural meshes ----------
        void Init3D()
        {
            if (be == null) be = new BasicEffect(GraphicsDevice) { TextureEnabled = false, VertexColorEnabled = false, SpecularColor = Vector3.Zero, SpecularPower = 48 };
            RebuildQualityMeshes();
        }
        void RebuildQualityMeshes()
        {
            int quality = settings == null ? 0 : settings.GraphicsQuality;
            if (appliedMeshQuality == quality && mCube != null) return;
            DisposeMesh(mCube); DisposeMesh(mSph); DisposeMesh(mCyl); DisposeMesh(mCone); DisposeMesh(mPyr); DisposeMesh(mFrus);
            int segments = quality == 0 ? 16 : quality == 1 ? 24 : quality == 2 ? 32 : 48;
            int sphereStacks = quality == 0 ? 16 : quality == 1 ? 24 : quality == 2 ? 36 : 48;
            int sphereSlices = quality == 0 ? 24 : quality == 1 ? 32 : quality == 2 ? 48 : 64;
            mCube = MkCube(); mSph = MkSphere(sphereStacks, sphereSlices); mCyl = MkRound(segments, 1f);
            mCone = MkRound(segments, 0f); mPyr = MkRound(4, 0f); mFrus = MkRound(segments, .8f);
            appliedMeshQuality = quality;
        }
        static void DisposeMesh(Mesh mesh)
        {
            if (mesh == null) return;
            if (mesh.VB != null) mesh.VB.Dispose();
            if (mesh.IB != null) mesh.IB.Dispose();
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
        Rig MkRig(float X, float Y, float Z, float s, float yaw) => new Rig { S = s, R = Matrix.CreateScale(s) * Matrix.CreateRotationY(-yaw) * Matrix.CreateTranslation(X, G - Y, Z) };
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
            for (int spikeIndex = 0; spikeIndex < L.Spikes.Count; spikeIndex++)
            {
                if (disabledSpikeIndices.Contains(spikeIndex)) continue;
                var s = L.Spikes[spikeIndex];
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
        void BgLayer(float z, float sp, float sc, Color col, float jz)
        {
            float hw = (880 - z) * .7364f + 300, cx = cam + W / 2f; int i0 = (int)MathF.Floor((cx - hw) / sp), i1 = (int)MathF.Ceiling((cx + hw) / sp);
            for (int i = i0; i <= i1; i++)
            {
                float h = Hs(i * 1.7f + z * .01f), x = i * sp + (h - .5f) * sp * .6f, zz = z + (Hs(i * 5.3f) - .5f) * jz;
                RockCluster3(x, zz, sc, col, h, i);
            }
        }
        void RockCluster3(float x, float zz, float sc, Color col, float h, int seed)
        {
            float coreH = (180 + h * 170) * sc, coreW = (240 + Hs(seed * 3.1f) * 100) * sc;
            Color shadow = Color.Lerp(col, Color.Black, .36f), light = Color.Lerp(col, L.Th.Moon, .12f);
            Shape(mFrus, Mx(x, coreH * .31f, zz, coreW, coreH * .7f, coreW * .72f), shadow);
            for (int j = -2; j <= 2; j++)
            {
                float u = Hs(seed * 7.7f + j * 11.3f), side = j * 43 * sc;
                float hh = (145 + u * 260) * sc, w = (112 + Hs(seed + j * 3.9f) * 80) * sc;
                float rz = (Hs(seed * 2 + j) - .5f) * .28f, ry = (Hs(seed + j * 9.1f) - .5f) * .8f;
                var mesh = j % 2 == 0 ? mPyr : mCone;
                var stone = Color.Lerp(shadow, light, .2f + Hs(seed + j * 2.7f) * .36f);
                Shape(mesh, Mx(x + side, hh * .48f, zz + (j % 2) * 42 * sc, w, hh, w * .82f, ry, rz), stone);
                if (j == -1 || j == 2)
                    Shape(mPyr, Mx(x + side * 1.3f, hh * .72f, zz + 10 * sc, w * .42f, hh * .58f, w * .48f, -ry, -rz * 1.4f), Color.Lerp(stone, col, .3f));
            }
            for (int k = 0; k < 3; k++)
            {
                float by = (18 + k * 25) * sc, bw = (105 - k * 16) * sc;
                Shape(mPyr, Mx(x + (k - 1) * 42 * sc, by, zz + (k - 1) * 26 * sc, bw, 28 * sc, bw * .62f, (k - 1) * .12f), Color.Lerp(shadow, light, .25f));
            }
        }
        void Scenery3()
        {
            var th = L.Th; float cx = cam + W / 2f;
            Shape(mSph, Mx(MoonPos().X, MoonPos().Y, MoonPos().Z, 420, 420, 420), Color.Lerp(th.Moon, Color.Black, .5f), Em(th.Moon, .85f));
            for (int i = (int)MathF.Floor((cx - 4200) / 1100); i <= (int)MathF.Ceiling((cx + 4200) / 1100); i++)
            {
                float h = Hs(i * 3.3f), x = i * 1100 + (h - .5f) * 500, hh = 900 + Hs(i * 7.7f) * 700;
                Shape(mPyr, Mx(x, hh / 2 - 60, -2700, 1500, hh, 1500, 0, (Hs(i * 9.4f) - .5f) * .2f), th.Far);
                Shape(mCone, Mx(x - 560, hh * .32f, -2650, 940, hh * .66f, 1050), Color.Lerp(th.Far, th.Mid, .18f));
                Shape(mCone, Mx(x + 600, hh * .27f, -2740, 1020, hh * .56f, 1140), Color.Lerp(th.Far, Color.Black, .2f));
            }
            RealmLandmarks3(cx, th);
            BgLayer(-1500, 380, .85f, Color.Lerp(th.Far, th.Mid, .6f), 500); BgLayer(-650, 330, 1.05f, th.Mid, 300); BgLayer(-230, 520, 1.4f, th.Near, 120);
        }

        void RealmLandmarks3(float cx, Theme th)
        {
            // Lightweight, map-specific silhouettes at a separate depth keep the three generated realms readable.
            int first = (int)MathF.Floor((cx - 3200) / 1500), last = (int)MathF.Ceiling((cx + 3200) / 1500);
            for (int i = first; i <= last; i++)
            {
                float x = i * 1500 + (Hs(i * 4.2f) - .5f) * 340;
                float z = -850 - Hs(i * 8.1f) * 220;
                if (th.Style == 0)
                {
                    float h = 720 + Hs(i * 2.3f) * 500;
                    Color bark = new Color(24, 13, 22), leaf = Color.Lerp(th.Far, new Color(85, 18, 38), .42f);
                    Shape(mFrus, Mx(x, h * .42f, z, 108, h * .86f, 100, (Hs(i + 7) - .5f) * .09f), bark);
                    for (int branch = 0; branch < 4; branch++)
                    {
                        float side = branch % 2 == 0 ? -1 : 1, by = h * (.49f + branch * .08f);
                        Shape(mCone, Mx(x + side * 150, by, z, 80, 350 + Hs(i + branch) * 100, 90, side * .6f), bark);
                    }
                    Shape(mCone, Mx(x, h * .78f, z, 510, 390, 480), leaf, Em(th.Accent, .08f));
                    Shape(mCone, Mx(x - 105, h * .63f, z + 55, 380, 340, 390), Color.Lerp(leaf, th.Mid, .3f));
                    Shape(mCone, Mx(x + 125, h * .66f, z - 25, 390, 360, 410), Color.Lerp(leaf, th.Far, .2f));
                }
                else if (th.Style == 1)
                {
                    float h = 250 + Hs(i * 5.7f) * 170, w = 350 + Hs(i * 3.1f) * 190;
                    Color stone = new Color(17, 29, 39), edge = Color.Lerp(stone, th.Mid, .65f);
                    Shape(mCube, Mx(x, h * .38f, z, w, h * .76f, 220), stone);
                    Shape(mPyr, Mx(x, h * .86f, z, w * 1.22f, 125, 260, 0, MathF.PI / 4), edge);
                    Shape(mCube, Mx(x, 92, z + 112, 108, 184, 10), new Color(5, 11, 19));
                    Shape(mCyl, Mx(x - w * .42f, 105, z + 195, 36, 210, 36), edge);
                    Shape(mCyl, Mx(x + w * .42f, 105, z + 195, 36, 210, 36), edge);
                    for (int tomb = -2; tomb <= 2; tomb++)
                    {
                        float tx = x + tomb * 95;
                        Shape(mCube, Mx(tx, 39 + Hs(i + tomb * 3) * 25, z + 285 + Hs(tomb * i) * 35, 52, 76, 34, 0, (Hs(i + tomb) - .5f) * .14f), Color.Lerp(stone, th.Far, .45f));
                        Shape(mCube, Mx(tx, 79 + Hs(i - tomb * 4) * 25, z + 285 + Hs(tomb * i) * 35, 42, 12, 38), edge);
                    }
                    GlowAt(x, h + 40, z, 115, Ad(th.Moon, .12f));
                }
                else
                {
                    float h = 500 + Hs(i * 6.3f) * 180, w = 620 + Hs(i * 1.9f) * 250;
                    Color stone = new Color(42, 20, 13), roof = new Color(77, 25, 13);
                    Shape(mCube, Mx(x, h * .42f, z, w, h * .84f, 300), stone);
                    for (int col = -2; col <= 2; col++)
                        Shape(mCyl, Mx(x + col * w * .19f, h * .42f, z + 166, 46, h * .82f, 46), Color.Lerp(stone, th.Mid, .42f), Em(th.Accent, .09f));
                    Shape(mCube, Mx(x, h * .86f, z + 85, w * 1.2f, 36, 430), Color.Lerp(stone, roof, .4f));
                    Shape(mPyr, Mx(x, h + 118, z, w * 1.42f, 210, 510, 0, MathF.PI / 4), roof, Em(th.Accent, .13f));
                    Shape(mCone, Mx(x, h + 248, z, 44, 170, 44), th.Accent, Em(th.Accent, .6f));
                    for (int brazier = -1; brazier <= 1; brazier += 2)
                    {
                        Shape(mCyl, Mx(x + brazier * (w * .64f), 60, z + 310, 48, 120, 48), new Color(35, 18, 11));
                        Shape(mCone, Mx(x + brazier * (w * .64f), 160, z + 310, 54, 130, 54), th.Accent, Em(th.Accent, .45f));
                    }
                }
            }
        }
        void Foreground3()
        {
            Color b = new Color(1, 0, 3); float cx = cam + W / 2f, hw = (880 - 330) * .7364f + 260;
            for (float x = (float)Math.Floor((cx - hw) / 100) * 100; x < cx + hw; x += 100)
            {
                float h = 55 + Hs(x) * 110, zz = 300 + Hs(x * 2.1f) * 120;
                Shape(mPyr, Mx(x, h * .42f - 8, zz, 95 + Hs(x * .8f) * 50, h, 115, 0, (Hs(x * .4f) - .5f) * .24f), b);
                Shape(mCone, Mx(x + 42, h * .25f - 8, zz - 22, 54, h * .58f, 66, .3f, -.15f), Color.Lerp(b, L.Th.Near, .16f));
            }
            float hw2 = (880 - 380) * .7364f + 320;
            for (int i = (int)MathF.Floor((cx - hw2) / 1400); i <= (int)MathF.Ceiling((cx + hw2) / 1400); i++)
            {
                float x = i * 1400 + Hs(i * 9.1f) * 500, h = 780 + Hs(i * 2.2f) * 360;
                Shape(mFrus, Mx(x, h * .34f, 380, 360, h * .72f, 280, .15f, -.08f), b);
                Shape(mPyr, Mx(x - 110, h * .54f, 345, 260, h, 250, -.2f, .1f), b);
                Shape(mPyr, Mx(x + 145, h * .45f, 420, 290, h * .82f, 270, .24f, -.08f), b);
            }
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
            Pt(r, mFrus, 0, bob + 70, 0, 54, 56, 34, 0, Color.Lerp(dk2, new Color(48, 42, 62), .5f), null, a);
            Pt(r, mFrus, -27, bob + 87, 0, 24, 20, 34, -.08f, new Color(57, 48, 69), Em(ti, .12f), a);
            Pt(r, mFrus, 27, bob + 87, 0, 24, 20, 34, .08f, new Color(57, 48, 69), Em(ti, .12f), a);
            Pt(r, mFrus, 0, bob + 73, 18, 38, 37, 10, 0, new Color(37, 31, 49), Em(ti, .14f), a);
            Pt(r, mPyr, 0, bob + 80, 25, 17, 20, 8, 0, new Color(147, 130, 171), Em(ti, .34f), a);
            for (int rib = 0; rib < 3; rib++)
            {
                float yy = bob + 60 + rib * 10;
                Bm(r, mCube, new Vector2(-22 + rib * 2, yy), new Vector2(22 - rib * 2, yy - 2), 25, 2.2f, 2, new Color(119, 107, 139), Em(ti, .2f), a);
            }
            foreach (int side in new[] { -1, 1 })
            {
                Pt(r, mFrus, side * 39, bob + 102, 0, 29, 19, 35, -side * .12f, new Color(49, 42, 65), Em(ti, .2f), a);
                Pt(r, mPyr, side * 49, bob + 107, 2, 14, 25, 18, 0, new Color(91, 78, 113), Em(ti, .28f), a);
                Pt(r, mCone, side * 49, bob + 124, 0, 10, 22, 10, -side * .25f, dk2, Em(ti, .1f), a);
                Pt(r, mCyl, side * 27, bob + 45, 15, 18, 10, 7, 0, new Color(61, 52, 73), Em(ti, .16f), a);
            }
            foreach (int side in new[] { -1, 1 })
            {
                Pt(r, mPyr, side * 31, bob + 92, 1, 11, 18, 15, 0, Color.Lerp(dk2, ti, .18f), Em(ti, .16f), a);
                Pt(r, mFrus, side * 7, bob + 47, 13, 13, 18, 5, 0, Color.Lerp(dk2, new Color(68, 56, 80), .35f), null, a);
            }
            Bm(r, mCube, new Vector2(-18, bob + 88), new Vector2(18, bob + 59), 18, 4, 3, Color.Lerp(new Color(56, 48, 69), ti, .22f), Em(ti, .12f), a);
            Pt(r, mCyl, 0, bob + 43, 0, 57, 7, 36, 0, new Color(35, 28, 44), Em(ti, .1f), a);
            Pt(r, mCube, 0, bob + 43, 19, 13, 12, 5, 0, new Color(177, 146, 92), Em(OR, .25f), a);
            Pt(r, mSph, -2, bob + 108, 0, 54, 36, 42, 0, dk2, null, a); Pt(r, mSph, 4, bob + 132, 0, 46, 48, 44, 0, dk2, null, a); Pt(r, mCone, -22, bob + 124, 0, 26, 52, 26, 1.9f, dk2, Em(ti, .05f), a);
            Pt(r, mFrus, 3, bob + 147, 0, 36, 15, 34, 0, new Color(44, 37, 58), Em(ti, .18f), a);
            foreach (int side in new[] { -1, 1 }) Pt(r, mCone, 3 + side * 15, bob + 151, 1, 10, 23, 10, -side * .34f, new Color(92, 78, 114), Em(ti, .16f), a);
            Pt(r, mCyl, 22, bob + 129, 13, 17, 4, 4, 0, new Color(164, 153, 181), Em(ti, .2f), a);
            Pt(r, mSph, 14, bob + 128, 5, 24, 30, 20, 0, Color.Black, null, a);
            foreach (float ez in new[] { 6f, -3f })
            {
                Color eyeColor = eye;
                if (s == 1 && progression != null && progression.Profile.EquippedEyeId == "kakashi")
                    eyeColor = ez > 0 ? new Color(255, 58, 72) : new Color(150, 240, 255);
                Pt(r, mSph, 22, bob + 130, ez, 7, 7, 7, 0, eyeColor, Em(eyeColor, 1f), a);
                GlowG(r, 22, bob + 130, ez, 18, Ad(eyeColor, a));
            }
            float ang0 = ScytheAng(at, cb, chg, air, run, tt), ca = MathF.Cos(ang0), sa = MathF.Sin(ang0); var d = new Vector2(ca, -sa); var nrm = new Vector2(-sa, -ca);
            float reach = at >= 0 ? MathF.Sin(Cl(at, 0, 1) * MathF.PI) * 16 : 0; var hand = new Vector2(20 + reach, bob + 82 + (at >= 0 ? 4 : 0)); var pole0 = hand - d * 72; var pole1 = hand + d * 104; var rear = hand - d * 52;
            var sh1 = new Vector2(12, bob + 104); var sh2 = new Vector2(-6, bob + 102); var el1 = (sh1 + hand) / 2 + new Vector2(0, -14); var el2 = (sh2 + rear) / 2 + new Vector2(0, -14);
            Bm(r, mCyl, sh2, el2, 2, 9, 9, dk, null, a); Bm(r, mCyl, el2, rear, 3, 8, 8, dk, null, a); Pt(r, mSph, rear.X, rear.Y, 4, 11, 11, 11, 0, bone, null, a);
            specK = .7f;
            bool equippedSword = s == 1 && progression != null && !string.IsNullOrEmpty(progression.Profile.EquippedWeaponId);
            if (equippedSword)
            {
                Color blade = progression.Profile.EquippedWeaponId == "lightning" ? new Color(172, 225, 255) : progression.Profile.EquippedWeaponId == "chakra" ? CY : new Color(230, 235, 250);
                float swing = at >= 0 ? MathF.Sin(Cl(at, 0, 1) * MathF.PI) * .8f - .35f : -.18f;
                Vector2 swordDir = new Vector2(MathF.Cos(swing), -MathF.Sin(swing));
                Vector2 swordBase = hand + swordDir * 10, swordTip = hand + swordDir * 128;
                Bm(r, mCyl, hand - swordDir * 27, hand + swordDir * 5, 12, 8, 8, new Color(70, 45, 34), Em(OR, .15f), a);
                Bm(r, mCube, swordBase, swordTip, 12, progression.Profile.EquippedWeaponId == "katana" ? 8 : 10, 4, blade, Em(blade, .65f), a);
                Bm(r, mCube, hand + swordDir * 6 + new Vector2(-swordDir.Y, swordDir.X) * 18, hand + swordDir * 6 - new Vector2(-swordDir.Y, swordDir.X) * 18, 12, 5, 7, OR, Em(OR, .25f), a);
                GlowG(r, swordTip.X, swordTip.Y, 12, at >= 0 ? 64 : 34, Ad(blade, .75f));
            }
            else
            {
                Bm(r, mCyl, pole0, pole1, 12, 5.5f, 5.5f, new Color(52, 44, 68), Em(ti, .08f), a);
                for (int k = 1; k <= 3; k++) { var q = Vector2.Lerp(pole0, pole1, k * .22f); Pt(r, mSph, q.X, q.Y, 12, 6, 6, 6, 0, ti, Em(ti, 1f), a); }
                Pt(r, mSph, pole1.X, pole1.Y, 12, 12, 12, 12, 0, new Color(96, 86, 116), null, a);
                Vector2 P0 = pole1, prev = P0; float Rb = 72;
                for (int i = 1; i <= 12; i++)
                {
                    float ph = i / 12f * 2.0f, aa = Rb * MathF.Sin(ph), bb = Rb * (1 - MathF.Cos(ph)) * .9f; var cur2 = P0 + nrm * aa - d * bb; float u = i / 12f, bladeWidth = 14 * MathF.Pow(1 - u, .8f) + 1.6f;
                    Bm(r, mCube, prev, cur2, 12, bladeWidth, 3.5f, Color.Lerp(new Color(64, 60, 84), new Color(236, 240, 255), u), Em(ti, (at >= 0 ? .55f : .3f) + .6f * u), a);
                    if (i == 6) GlowG(r, cur2.X, cur2.Y, 12, at >= 0 ? 60 : 30, Ad(ti, (at >= 0 ? .55f : .25f) * a)); if (i == 12) GlowG(r, cur2.X, cur2.Y, 12, 30, Ad(ti, .9f * a)); prev = cur2;
                }
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
            if (e.GenjutsuT > 0) { ac = new Color(190, 85, 255); trim = new Color(225, 170, 255); }
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
            Pt(r, mFrus, 0, 73, 15, bw * .58f, 29, 9, 0, Color.Lerp(col, trim, .24f), Em(ac, .15f), al);
            Pt(r, mPyr, 0, 75, 21, 15, 18, 7, 0, trim, trE, al);
            foreach (int sd in new[] { -1, 1 })
            {
                Pt(r, mFrus, sd * (bw * .43f), 83, 0, 24, 19, 31, -sd * .12f, Color.Lerp(col, trim, .3f), Em(ac, .12f), al);
                Pt(r, mCyl, sd * 23, 49, 4, 17, 12, 6, 0, dk, Em(ac, .1f), al);
            }
            Bm(r, mCube, new Vector2(-bw * .28f, 95), new Vector2(bw * .27f, 53), 17, 4, 3, trim, trE, al);
            for (int k = 0; k < 4; k++) if (sm || e.Boss) Pt(r, mCube, 0, 90 - k * 11, 0, bw + 1, 2.4f, 27, 0, trim, trE, al);
            if (sm || e.Boss) foreach (int sd in new[] { -1, 1 }) Pt(r, mCube, sd * (bw / 2 + 3), 90, 0, 20, 14, 34, 0, Color.Lerp(col, trim, .3f), null, al);
            if (ar) Pt(r, mCube, -15, 80, -12, 9, 36, 9, -.3f, dk, null, al);
            if (T != 1 && !e.Boss || e.Kind == 1) for (int k = 0; k < 6; k++) { var p = new Vector2(-8 - k * 9, 92 + MathF.Sin(t * 9 + k) * 5 - k * 3); var q = new Vector2(-8 - (k + 1) * 9, 92 + MathF.Sin(t * 9 + k + 1) * 5 - (k + 1) * 3); Bm(r, mCube, p, q, -2, 4.5f, 3, ac, acE, al); }
            Pt(r, mSph, idle * .3f, 106, 0, 27, 27, 27, 0, col, null, al);
            if (sm || e.Boss && e.Kind != 1) { Pt(r, mSph, 0, 114, 0, 38, 24, 36, 0, Color.Lerp(col, trim, .2f), null, al); Pt(r, mCyl, 0, 104, 0, 46, 4, 44, 0, trim, trE, al); foreach (int sd in new[] { -1, 1 }) Pt(r, mCone, sd * 12, 134, 0, 9, 32, 9, -sd * .5f, ac, acE, al); Pt(r, mCube, 8, 98, 0, 17, 10, 26, 0, new Color(80, 12, 22), null, al); }
            else if (ar) Pt(r, mCone, 0, 124, 0, 68, 30, 68, 0, Color.Lerp(col, trim, .25f), null, al); else Pt(r, mCube, 0, 109, 0, 28, 4.5f, 29, 0, ac, acE, al);
            if (sm || e.Boss && e.Kind != 1)
            {
                Pt(r, mCube, 0, 104, 14, 25, 10, 7, 0, dk, null, al);
                Pt(r, mCube, 0, 103, 18, 13, 3, 3, 0, ac, acE, al);
                Pt(r, mPyr, 0, 133, 1, 24, 14, 26, 0, Color.Lerp(col, trim, .34f), Em(ac, .12f), al);
            }
            else
            {
                Pt(r, mCube, 0, 103, 14, 22, 8, 6, 0, dk, null, al);
                Pt(r, mCube, 0, 103, 18, 11, 2.5f, 2.5f, 0, ac, acE, al);
            }
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

        void Summon3D(SummonedCompanion summon, float alpha = 1f)
        {
            SummoningDefinition d = summon.Definition;
            Color body = RgbColor(d.PrimaryRgb), accent = RgbColor(d.AccentRgb);
            Color dark = Color.Lerp(body, Color.Black, .48f), glow = Em(accent, .7f);
            float s = d.Size * .55f, bob = MathF.Sin(summon.Animation * (summon.Mv > 0 ? 10 : 2.4f)) * (summon.Mv > 0 ? 4 : 2);
            bool legendaryToad = d.Id == "giant-toad-warrior";
            bool legendarySnake = d.Id == "ancient-serpent";
            bool legendaryBird = d.Id == "sky-king-bird";
            bool legendaryTurtle = d.Id == "colossal-turtle-guardian";
            Rig r = MkRig(summon.X, summon.Y, summon.Z, s, summon.Yaw);
            GlowAt(summon.X, G - summon.Y + 54 * s, summon.Z, 92 * s, Ad(accent, .25f * alpha));
            Color shadowRed = new Color(255, 32, 62);
            if (d.Category == SummonCategory.Toad || legendaryToad)
            {
                Pt(r, mSph, 0, 52 + bob, 0, 82, 52, 64, 0, body, glow, alpha);
                Pt(r, mSph, 35, 72 + bob, 4, 48, 38, 48, 0, Color.Lerp(body, Color.White, .1f), glow, alpha);
                Pt(r, mSph, 47, 79 + bob, 18, 9, 9, 6, 0, Color.White, null, alpha);
                Pt(r, mSph, 49, 79 + bob, 21, 4, 4, 3, 0, accent, Em(accent, .9f), alpha);
                foreach (int side in new[] { -1, 1 })
                {
                    Pt(r, mCube, side * 32, 19, 0, 25, 30, 34, 0, body, null, alpha);
                    Pt(r, mCone, side * 17, 94 + bob, 0, 14, 27, 14, -side * .2f, accent, glow, alpha);
                }
                if (d.Id.Contains("armored") || d.Role == SummonRole.Legendary)
                {
                    Pt(r, mFrus, -10, 64 + bob, -14, 90, 42, 55, 0, dark, glow, alpha);
                    for (int i = -1; i <= 1; i++) Pt(r, mCube, i * 24, 94 + bob, 0, 7, 16, 5, 0, accent, glow, alpha);
                }
            }
            else if (d.Category == SummonCategory.Dog || d.Category == SummonCategory.Wolf || d.Category == SummonCategory.Panther)
            {
                float bodyY = 48 + bob;
                Pt(r, mSph, 0, bodyY, 0, 100, 42, 43, 0, body, glow, alpha);
                Pt(r, mSph, 48, bodyY + 14, 0, 47, 36, 38, 0, body, glow, alpha);
                Pt(r, mSph, 67, bodyY + 10, 19, 7, 7, 5, 0, Color.White, null, alpha);
                Pt(r, mSph, 69, bodyY + 10, 22, 3, 3, 2, 0, accent, Em(accent, .8f), alpha);
                foreach (int side in new[] { -1, 1 })
                {
                    Pt(r, mCube, side * 29, 19, 0, 12, 37, 13, 0, dark, null, alpha);
                    Pt(r, mCone, 48 + side * 12, 83 + bob, 0, 14, 23, 13, 0, accent, glow, alpha);
                }
                Pt(r, mCone, -52, 67 + bob, 0, 13, 45, 13, 0, body, glow, alpha);
                if (d.Category != SummonCategory.Dog) Pt(r, mCone, -22, 72 + bob, 0, 16, 33, 16, 0, accent, glow, alpha);
                if (d.Id.Contains("armored")) Pt(r, mCube, 0, 68 + bob, -5, 68, 24, 45, 0, dark, glow, alpha);
                if (d.Id == "ninja-dog-pack" || d.Id == "wolf-pack")
                {
                    Rig pack = MkRig(summon.X - 52, summon.Y, summon.Z + 34, s * .72f, summon.Yaw - .2f);
                    Pt(pack, mSph, 0, bodyY - 2, 0, 92, 39, 41, 0, body, glow, alpha);
                    Pt(pack, mSph, 45, bodyY + 10, 0, 43, 33, 36, 0, body, glow, alpha);
                    foreach (int side in new[] { -1, 1 }) Pt(pack, mCube, side * 27, 18, 0, 11, 34, 12, 0, dark, null, alpha);
                }
            }
            else if (d.Category == SummonCategory.Bird || legendaryBird)
            {
                float flap = MathF.Sin(t * (summon.Mv > 0 ? 13 : 4)) * .38f;
                Pt(r, mSph, 0, 53 + bob, 0, 54, 65, 44, 0, body, glow, alpha);
                Pt(r, mSph, 28, 89 + bob, 0, 36, 34, 35, 0, body, glow, alpha);
                Pt(r, mCone, 52, 82 + bob, 0, 24, 17, 18, -MathHelper.PiOver2, accent, glow, alpha);
                foreach (int side in new[] { -1, 1 })
                {
                    Pt(r, mCone, -5, 67 + bob, side * 23, 76, 16, 18, side * flap, accent, glow, alpha);
                    Pt(r, mCone, -16, 20 + bob, side * 8, 14, 43, 14, 0, dark, null, alpha);
                }
            }
            else if (d.Category == SummonCategory.Snake || legendarySnake)
            {
                for (int i = 0; i < 5; i++)
                {
                    float xx = -58 + i * 26;
                    float yy = 18 + MathF.Abs(MathF.Sin(summon.Animation * 3 + i * .7f)) * 24;
                    Pt(r, mSph, xx, yy, 0, 40 - i * 2, 25 - i, 28 - i * 2, 0, i % 2 == 0 ? body : dark, glow, alpha);
                }
                Pt(r, mSph, 62, 34 + bob, 0, 44, 33, 35, 0, body, glow, alpha);
                Pt(r, mCone, 80, 29 + bob, 0, 25, 10, 15, -MathHelper.PiOver2, accent, glow, alpha);
                Pt(r, mSph, 74, 41 + bob, 16, 5, 5, 4, 0, new Color(255, 70, 45), Em(shadowRed, .8f), alpha);
            }
            else if (d.Category == SummonCategory.Slug)
            {
                float slim = MathF.Sin(summon.Animation * 4) * 5;
                Pt(r, mSph, 0, 29 + slim, 0, 112, 48, 74, 0, body, glow, alpha);
                Pt(r, mSph, 47, 48 + slim, 0, 39, 50, 40, 0, Color.Lerp(body, Color.White, .18f), glow, alpha);
                foreach (int side in new[] { -1, 1 })
                {
                    Pt(r, mCyl, 46 + side * 11, 86 + slim, side * 8, 5, 33, 5, -side * .18f, accent, glow, alpha);
                    Pt(r, mSph, 46 + side * 11, 105 + slim, side * 8, 9, 9, 9, 0, Color.White, null, alpha);
                }
            }
            else if (d.Category == SummonCategory.Turtle || legendaryTurtle)
            {
                Pt(r, mSph, 0, 47 + bob, 0, 108, 58, 92, 0, body, glow, alpha);
                Pt(r, mFrus, -5, 78 + bob, -4, 90, 52, 80, 0, dark, glow, alpha);
                Pt(r, mSph, 55, 46 + bob, 0, 43, 34, 38, 0, body, glow, alpha);
                Pt(r, mSph, 72, 53 + bob, 19, 5, 5, 4, 0, accent, Em(accent, .7f), alpha);
                foreach (int side in new[] { -1, 1 }) Pt(r, mCube, side * 36, 15, 0, 25, 27, 32, 0, body, null, alpha);
                for (int i = -1; i <= 1; i++) Pt(r, mCube, i * 26, 80 + bob, -8, 4, 45, 4, 0, accent, glow, alpha);
            }
            else if (d.Category == SummonCategory.Dragon)
            {
                Pt(r, mSph, -12, 55 + bob, 0, 105, 45, 55, 0, body, glow, alpha);
                Pt(r, mSph, 43, 81 + bob, 0, 49, 41, 42, 0, body, glow, alpha);
                Pt(r, mCone, 67, 75 + bob, 0, 29, 15, 17, -MathHelper.PiOver2, accent, glow, alpha);
                foreach (int side in new[] { -1, 1 })
                {
                    Pt(r, mCone, -20, 87 + bob, side * 18, 82, 20, 23, side * .28f, accent, glow, alpha);
                    Pt(r, mCone, 43, 109 + bob, side * 12, 13, 32, 13, -side * .4f, dark, glow, alpha);
                }
                for (int i = 0; i < 4; i++) Pt(r, mCone, -52 - i * 17, 52 + bob, 0, 20, 20, 20, 0, body, glow, alpha);
            }
            else
            {
                bool emperor = d.Id == "shadow-emperor";
                Color aura = emperor || d.Category == SummonCategory.Shadow ? shadowRed : accent;
                GlowAt(summon.X, G - summon.Y + 70 * s, summon.Z, 135 * s, Ad(aura, .42f * alpha));
                Pt(r, mSph, 0, 56 + bob, 0, emperor ? 112 : 92, emperor ? 92 : 74, emperor ? 82 : 66, 0, body, Em(aura, .24f), alpha);
                Pt(r, mSph, 42, 94 + bob, 0, 54, 47, 48, 0, dark, Em(aura, .35f), alpha);
                Pt(r, mCone, 60, 85 + bob, 0, 34, 18, 21, -MathHelper.PiOver2, accent, Em(aura, .9f), alpha);
                foreach (int side in new[] { -1, 1 })
                {
                    Pt(r, mCone, side * 23, 131 + bob, 1, 20, emperor ? 58 : 44, 20, -side * .42f, aura, Em(aura, .8f), alpha);
                    Pt(r, mCube, side * 45, 57 + bob, 0, 28, 70, 26, -side * .32f, dark, Em(aura, .35f), alpha);
                    Pt(r, mSph, 51, 99 + bob, side * 18, 9, 8, 6, 0, new Color(255, 54, 66), Em(shadowRed, 1), alpha);
                }
                for (int i = 0; i < (emperor ? 8 : 4); i++)
                {
                    float a = summon.Animation * 1.7f + i * MathHelper.TwoPi / (emperor ? 8 : 4);
                    float px0 = MathF.Cos(a) * (62 + MathF.Sin(t * 4 + i) * 8), py0 = 76 + MathF.Sin(a) * 52;
                    Pt(r, mSph, px0, py0, 38, 9, 9, 9, 0, aura, Em(aura, .9f), alpha);
                }
            }
            specK = 0;
        }

        // ---------- full scene ----------
        void World3D()
        {
            Scenery3(); Ground3(); Plats3(); Spikes3(); Checks3(); Gate3(); Souls3();
            foreach (var k in clones) Reaper3D(k.X, G, k.Z, 1, Cl(k.L, 0, .7f), k.T, k.Mv * 300, false, k.At >= 0 ? k.At / .28f : -1, 0, false, CY, 1, k.Yaw);
            if (summonedCompanion != null && summonedCompanion.X > cam - 500 && summonedCompanion.X < cam + W + 500) Summon3D(summonedCompanion);
            if (summoningMenuOpen && SelectedSummon != null)
            {
                float previewX = Cl(P.X + 300, 40, L.GateX + 40), previewZ = Cl(P.Z + 180, -240, 460);
                float previewY = FloorBelow(previewX, P.Y + 2, previewZ);
                if (previewY < 9999 && (summonedCompanion == null || DistXZ(previewX, previewZ, summonedCompanion.X, summonedCompanion.Z) > 140))
                {
                    var preview = new SummonedCompanion { Definition = SelectedSummon, X = previewX, Y = previewY, Z = previewZ, Yaw = P.Yaw, Animation = t };
                    Summon3D(preview, .72f);
                }
            }
            foreach (var e in en) if (e.X > cam - 500 && e.X < cam + W + 500) Foe3D(e);
            Color playerTint = progression.Profile.EquippedEyeId == "rinnegan" ? new Color(190, 130, 255)
                : progression.Profile.EquippedEyeId == "byakugan" || progression.Profile.EquippedEyeId == "jougan" ? new Color(145, 235, 255)
                : progression.Profile.EquippedEyeId == "sharingan" || progression.Profile.EquippedEyeId == "mangekyo" || progression.Profile.EquippedEyeId == "ems" ? new Color(255, 95, 105)
                : progression.Profile.EquippedEyeId == "kakashi" ? new Color(170, 215, 255) : PU;
            if (!P.Dead && hiddenT <= 0 && !(P.Inv > 0 && (int)(t * 30) % 2 == 0))
            {
                float atp = P.Atk > 0 ? P.AtkT / (P.Cb == 2 ? .42f : .28f) : -1;
                Reaper3D(P.X, P.Y, P.Z, 1, P.Dash > 0 ? .35f : 1f, t, P.Vx, !P.Ground, atp, P.Cb, P.Chg > 0, playerTint, 1, P.Yaw);
            }
            if (susanooT > 0)
            {
                Color armor = progression.Profile.EquippedSusanooVariantId == "susanoo.crimson" ? new Color(255, 70, 105)
                    : progression.Profile.EquippedSusanooVariantId == "susanoo.azure" ? new Color(105, 190, 255) : new Color(180, 120, 255);
                GlowAt(P.X, G - P.Y + 80, P.Z, 190, Ad(armor, .45f));
                Reaper3D(P.X, P.Y, P.Z, 1, .46f, t, P.Vx, !P.Ground, -1, 0, false, armor, 1.65f, P.Yaw);
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
                if (e.GenjutsuT > 0) { GlowS(top.X, top.Y - 20, 46, 210, Ad(new Color(195, 88, 255), .9f)); Txt("GENJUTSU " + e.GenjutsuT.ToString("0.0"), top.X, top.Y - 52, 12, new Color(225, 190, 255), 1); }
            }
            if (futureVision)
            {
                foreach (Vector2 spike in L.Spikes)
                {
                    var hazard = ScreenAt((spike.X + spike.Y) * .5f, G - 18, 110);
                    GlowS(hazard.X, hazard.Y, 48, 190, Ad(new Color(120, 245, 255), .95f));
                    Txt("HAZARD", hazard.X, hazard.Y - 30, 11, new Color(150, 245, 255), 1);
                }
                float objectiveX = bossDead ? L.GateX : en.Count > 0 ? en[en.Count - 1].X : L.GateX;
                var objective = ScreenAt(objectiveX, G - 250, 110);
                Tri(objective.X, objective.Y, 42, 50, MathF.PI, Ad(new Color(255, 220, 115), .95f));
                Txt(bossDead ? "EXIT" : "OBJECTIVE", objective.X, objective.Y - 36, 12, new Color(255, 220, 115), 1);
                foreach (var foe in en) if (!foe.Dead) { var marker = ScreenAt(foe.X, G - foe.Y + 170, foe.Z); CircS(marker.X, marker.Y, 18, 3, Ad(RD, .9f)); }
            }
            if (freezeT > 0) Box(W / 2, H / 2, W, H, 0, new Color(50, 180, 240, 25));
            foreach (var p in proj) DrawProj(p);
            DrawFx();
            for (int i = 0; i < 120; i++) { bool a = i % 4 != 0; Glow(amb[i, 0], amb[i, 1], amb[i, 4] * (a ? 3 : 2), Ad(L.Th.Style == 0 ? new Color(200, 255, 120) : L.Th.Style == 1 ? new Color(150, 220, 255) : new Color(255, 150, 50), a ? .6f : .35f)); }
            sb.End();
        }

        protected override void Draw(GameTime gt)
        {
            var bounds = Window.ClientBounds;
            var ms = Mouse.GetState();
            bool mouseLook = state == "play" && IsActive && bounds.Width > 0 && bounds.Height > 0;
            IsMouseVisible = !mouseLook;
            if (mouseLook)
            {
                int centerX = bounds.Width / 2, centerY = bounds.Height / 2;
                float dt = Math.Min(.05f, (float)gt.ElapsedGameTime.TotalSeconds);
                if (cameraCaptured)
                {
                    float scale = W / (float)bounds.Width;
                    int dx = ms.X - centerX, dy = ms.Y - centerY;
                    float sensitivity = settings == null ? 1f : settings.CameraSensitivity;
                    camYaw += CameraMotion.MouseYawDelta(dx, scale) * sensitivity;
                    camPitch = Cl(camPitch + CameraMotion.MousePitchDelta(dy, scale) * sensitivity, -.12f, .72f);
                    if (Math.Abs(dx) + Math.Abs(dy) > 1) camMouseIdle = 0;
                    else camMouseIdle += dt;
                    if (!byakuganScout && camMouseIdle > .8f && (Math.Abs(P.Vx) + Math.Abs(P.Vz) > 90 || P.Dash > 0))
                    {
                        float followYaw = P.Yaw - MathHelper.PiOver2;
                        camYaw = CameraMotion.ApproachYaw(camYaw, followYaw, dt, 2.4f);
                    }
                }
                cameraCaptured = true;
                if (ms.X != centerX || ms.Y != centerY) Mouse.SetPosition(centerX, centerY);
            }
            else cameraCaptured = false;
            int wheel = ms.ScrollWheelValue;
            if (wheel != cameraWheel)
            {
                camDistance = Cl(camDistance - (wheel - cameraWheel) * .45f, 540, 1500); cameraWheel = wheel;
            }
            float follow = 1 - MathF.Exp(-Math.Min(.05f, (float)gt.ElapsedGameTime.TotalSeconds) * 5f);
            float cameraTargetX = byakuganScout ? scoutX : P.X;
            float cameraTargetZ = byakuganScout ? scoutZ : P.Z;
            float cameraTargetY = byakuganScout ? 0 : Cl(P.Y - 500, -220, 0);
            cam += (Cl(cameraTargetX - W * .5f, 0, Math.Max(0, L.GateX + 400 - W)) - cam) * follow;
            camZ += (cameraTargetZ - camZ) * follow; camY += (cameraTargetY - camY) * follow;
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
            GraphicsDevice.SetRenderTarget(null); var pp = GraphicsDevice.PresentationParameters; GraphicsDevice.Viewport = new Viewport(0, 0, pp.BackBufferWidth, pp.BackBufferHeight); GraphicsDevice.Clear(Color.Black);
            float scl = Math.Min(pp.BackBufferWidth / (float)W, pp.BackBufferHeight / (float)H);
            var m = Matrix.CreateScale(scl) * Matrix.CreateTranslation((pp.BackBufferWidth - W * scl) / 2, (pp.BackBufferHeight - H * scl) / 2, 0);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, m);
            sb.Draw(rt, new Rectangle(0, 0, W, H), WH); sb.Draw(small, new Rectangle(0, 0, W, H), new Color(110, 110, 110, 0));
            Box(W / 2, H / 2, W, H, 0, Ad(L.Th.Accent, .035f)); sb.Draw(vig, new Rectangle(0, 0, W, H), WH); sb.Draw(vig, new Rectangle(0, 0, W, H), Color.White * .5f);
            for (int i = 0; i < 70; i++) Box(Rf(0, W), Rf(0, H), 2, 2, 0, Color.White * Rf(.03f, .09f));
            if (state == "play" && P.Hp < 35) Box(W / 2, H / 2, W, H, 0, new Color(120, 0, 20) * (.12f + .1f * MathF.Sin(t * 8)));
            if (flash > 0) Box(W / 2, H / 2, W, H, 0, WH * (flash * .6f));
            Box(W / 2, 14, W, 28, 0, Color.Black * .85f); Box(W / 2, H - 14, W, 28, 0, Color.Black * .0f);
            sb.End();
            if (state == "play" && settings != null && settings.ShowHud)
            {
                Matrix hudTransform = Matrix.CreateTranslation(-W / 2f, -H / 2f, 0) * Matrix.CreateScale(settings.HudScale) *
                    Matrix.CreateTranslation(W / 2f + settings.HudOffsetX, H / 2f + settings.HudOffsetY, 0) * m;
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, hudTransform);
                DrawHud();
                sb.End();
            }
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, m);
            if (state == "play")
            {
                if (abilityMenuOpen) DrawAbilityMenu();
                if (summoningMenuOpen) DrawSummoningMenu();
                if (shopMenuOpen) DrawShopMenu();
            }
            if (state == "title")
            {
                if (levelSelectOpen) DrawMissionSelect();
                else if (titlePage == "shop") DrawShopMenu();
                else if (titlePage == "upgrade") DrawUpgradeScreen();
                else if (titlePage == "settings") DrawSettingsScreen();
                else DrawMainMenu();
            }
            if (state == "launch")
            {
                Box(W / 2, H / 2, W, H, 0, Color.Black * .7f);
                Txt("PREPARING THE FRONTIER", W / 2, 300, 46, WH, 1, VI);
                MissionDefinition mission = MissionCatalog.Get(queuedMenuLevel);
                Txt(MissionCatalog.Maps[mission.MapIndex].Name + "  /  " + mission.Name, W / 2, 368, 20, CY, 1);
                Txt("ENTERING THE SHADOWS...", W / 2, 430, 15, Color.LightGray, 1);
            }
            if (state == "paused") DrawPause();
            if (state == "story") DrawStory();
            if (state == "dead") { Box(W / 2, H / 2, W, H, 0, new Color(20, 0, 10) * .65f); Txt("YOU HAVE BEEN REAPED", W / 2, 200, 84, RD, 1); Txt("LEVEL " + (lv + 1) + "     SOULS " + kills, W / 2, 320, 38, WH, 1); if ((int)(t * 1.6f) % 2 == 0) Txt("ENTER  -  RISE AGAIN FROM THE LAST SHRINE", W / 2, 400, 32, VI, 1); }
            if (state == "win") { Box(W / 2, H / 2, W, H, 0, new Color(10, 0, 20) * .6f); Txt("LEVEL " + (lv + 1) + " COMPLETE", W / 2, 190, 86, new Color(255, 220, 120), 1, OR); Txt("SOULS REAPED  " + kills, W / 2, 310, 38, WH, 1); if (winT > 1 && (int)(t * 1.6f) % 2 == 0) Txt(lv >= MissionCatalog.All.Count - 1 ? "ENTER  -  FINISH" : "ENTER  -  NEXT LEVEL", W / 2, 390, 34, VI, 1); }
            if (state == "end") { Box(W / 2, H / 2, W, H, 0, Color.Black * .85f); Txt("THE REAPER'S JOURNEY ENDS", W / 2, 200, 70, new Color(225, 195, 255), 1, PU); Txt("THANK YOU FOR PLAYING KADAYA", W / 2, 310, 36, WH, 1); if ((int)(t * 1.6f) % 2 == 0) Txt("ENTER  -  RETURN TO TITLE", W / 2, 390, 30, VI, 1); }
            sb.End(); base.Draw(gt);
        }
    }
}
