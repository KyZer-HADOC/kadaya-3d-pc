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
            s = SpriteFontText.ToSupportedGlyphs(s);
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
        Vector3 ScythePoint(Vector2 p, float bob)
        {
            float localX = (p.X - P.X) * P.F;
            return new Vector3(P.X + MathF.Cos(P.Yaw) * localX, p.Y - bob, P.Z + MathF.Sin(P.Yaw) * localX);
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
            else if (p.K == "fire") { Glow(sx, sy, p.R * 3.2f, Ad(new Color(255, 82, 24), .95f)); Glow(sx, sy, p.R * 1.5f, Ad(new Color(255, 196, 72), 1)); Circ(sx, sy, p.R * .65f, new Color(255, 230, 145)); }
            else if (p.K == "water") { Glow(sx, sy, p.R * 2.4f, Ad(new Color(55, 160, 255), .85f)); Cres(sx, sy, p.R * 1.1f, p.R * .7f, t * 9, false, Ad(new Color(205, 245, 255), .9f)); }
            else if (p.K == "lightning") { Glow(sx, sy, p.R * 2.7f, Ad(new Color(150, 215, 255), 1)); for (int i = 0; i < 3; i++) Ln(sx - 24 + i * 10, sy - 20, sx + 24 - i * 8, sy + 18, 3, i == 1 ? WH : new Color(110, 195, 255)); }
            else if (p.K == "shuriken") { float a = t * 13; for (int i = 0; i < 4; i++) { float q = a + i * MathHelper.PiOver2; Ln(sx - MathF.Cos(q) * 15, sy - MathF.Sin(q) * 15, sx + MathF.Cos(q) * 15, sy + MathF.Sin(q) * 15, 3, new Color(220, 232, 250)); } Glow(sx, sy, 20, Ad(new Color(175, 205, 255), .7f)); }
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

        // ---------- title and mission selection ----------
        void DrawMenuThemeArt(Theme theme, float x, float y, float w, float h)
        {
            for (int i = 0; i < 14; i++)
            {
                float k = i / 13f;
                Color sky = k < .68f ? Color.Lerp(theme.Sky0, theme.Sky1, k / .68f) : Color.Lerp(theme.Sky1, theme.Sky2, (k - .68f) / .32f);
                Box(x + w * .5f, y + h * (i + .5f) / 14f, w, h / 14f + 1, 0, sky);
            }
            float moonX = x + w * .76f, moonY = y + h * .28f;
            Glow(moonX, moonY, h * .22f, Ad(theme.Moon, .4f));
            Circ(moonX, moonY, h * .095f, theme.Moon);
            for (int i = 0; i < 7; i++)
            {
                float starX = x + w * (.08f + (i * .137f % .82f));
                float starY = y + h * (.12f + (i * .193f % .48f));
                Circ(starX, starY, i % 3 == 0 ? 2 : 1.2f, Color.White * .8f);
            }
            float groundY = y + h * .78f;
            Box(x + w / 2, groundY + (y + h - groundY) / 2, w, y + h - groundY, 0, theme.Ground);
            Tri(x + w * .18f, groundY - h * .11f, w * .4f, h * .32f, 0, theme.Far);
            Tri(x + w * .53f, groundY - h * .13f, w * .56f, h * .39f, 0, theme.Mid);
            if (theme.Style == 0)
            {
                for (int i = 0; i < 7; i++)
                {
                    float tx = x + w * (.08f + i * .145f), trunkH = h * (.32f + (i % 3) * .07f);
                    Box(tx, groundY - trunkH * .42f, w * .025f, trunkH, 0, theme.Near);
                    Tri(tx, groundY - trunkH * .68f, w * .18f, h * .27f, MathHelper.Pi, theme.Near);
                    Tri(tx, groundY - trunkH * .84f, w * .13f, h * .22f, MathHelper.Pi, theme.Mid);
                }
            }
            else if (theme.Style == 1)
            {
                for (int i = 0; i < 4; i++)
                {
                    float gx = x + w * (.15f + i * .22f), gh = h * (.13f + (i % 2) * .04f);
                    Box(gx, groundY - gh * .45f, w * .085f, gh, 0, Color.Lerp(theme.Near, theme.Fog, .12f));
                    Box(gx, groundY - gh, w * .13f, h * .025f, 0, theme.Near);
                    Ln(gx - w * .025f, groundY - gh * .72f, gx + w * .025f, groundY - gh * .72f, 2, theme.Accent);
                    Ln(gx, groundY - gh * .83f, gx, groundY - gh * .62f, 2, theme.Accent);
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    float tx = x + w * (.24f + i * .25f), top = groundY - h * (.40f + (i % 2) * .07f);
                    Box(tx, (top + groundY) * .5f, w * .035f, groundY - top, 0, theme.Near);
                    Box(tx, top, w * .17f, h * .035f, 0, theme.Accent);
                    Tri(tx, top - h * .035f, w * .23f, h * .12f, 0, theme.Mid);
                }
                for (int i = 0; i < 8; i++)
                    Circ(x + w * (.08f + i * .12f), y + h * (.70f + (i % 2) * .06f), 2 + i % 3, theme.Accent * .7f);
            }
            Box(x + w / 2, y + h - 2, w, 4, 0, theme.Accent);
        }

        void DrawMenuButton(string label, string detail, float x, float y, bool selected, bool hovered)
        {
            bool active = selected || hovered;
            Box(x + 245, y + 25, 490, 50, 0, active ? new Color(52, 38, 72, 245) : new Color(6, 8, 18, 210));
            Box(x + 245, y + 2, 490, 2, 0, active ? CY : Color.White * .22f);
            Box(x + 245, y + 48, 490, 2, 0, active ? VI : Color.White * .13f);
            Box(x + 2, y + 25, 4, 50, 0, active ? CY : Color.White * .2f);
            Txt(label, x + 24, y + 10, 23, active ? WH : new Color(190, 185, 204));
            Txt(detail, x + 465, y + 16, 13, active ? CY : new Color(133, 128, 149), 2);
        }

        void DrawMainMenu()
        {
            Box(W / 2, H / 2, W, H, 0, Color.Black * .48f);
            Box(348, H / 2, 590, 590, 0, Color.Black * .52f);
            Box(68, 90, 560, 3, 0, new Color(200, 110, 255));
            Txt("KADAYA", 84, 100, 112, new Color(234, 218, 255), 0, new Color(138, 92, 255));
            Txt("THE AGE OF SHADOWS", 93, 224, 24, new Color(255, 110, 139));
            Txt("A 3D SHINOBI ACTION JOURNEY", 94, 259, 15, new Color(202, 192, 222));

            string[] labels = { "CONTINUE", "MISSION SELECT", "NEW CAMPAIGN", "QUIT" };
            string[] details =
            {
                "LEVEL " + (lv + 1),
                "" + (Math.Min(progression.Profile.UnlockedLevel + 1, LevelGen.Themes.Length)) + " / " + LevelGen.Themes.Length + " OPEN",
                "KEEP YOUR SHOP ITEMS",
                "EXIT TO DESKTOP"
            };
            Point mouse = MenuVirtualMouse();
            for (int i = 0; i < labels.Length; i++)
            {
                float y = 330 + i * 65;
                bool hovered = mouse.X >= 76 && mouse.X <= 572 && mouse.Y >= y && mouse.Y <= y + 52;
                DrawMenuButton(labels[i], details[i], 76, y, mainMenuIndex == i, hovered);
            }

            float cardX = 706, cardY = 100, cardW = 500, artY = 160, artH = 245;
            Box(cardX + cardW / 2, 365, cardW, 520, 0, new Color(4, 6, 14, 225));
            Box(cardX + cardW / 2, cardY, cardW, 3, 0, L.Th.Accent);
            Txt("CURRENT FRONTIER", cardX + 26, 121, 15, Color.LightGray);
            DrawMenuThemeArt(L.Th, cardX + 16, artY, cardW - 32, artH);
            Txt("MISSION  " + (lv + 1).ToString("00"), cardX + 27, 420, 15, L.Th.Accent);
            Txt(L.Th.Name, cardX + 27, 447, 27, WH);
            Txt("BOSS: " + (lv == 0 ? "ONI WARLORD" : lv == 1 ? "THE WRAITH LORD" : "FIRE SHOGUN"), cardX + 27, 490, 15, new Color(255, 157, 125));
            Txt("UNLOCKED MISSIONS  " + (Math.Min(progression.Profile.UnlockedLevel + 1, LevelGen.Themes.Length)) + " / " + LevelGen.Themes.Length,
                cardX + 27, 527, 14, new Color(184, 174, 207));
            Box(68, 618, 1140, 1, 0, Color.White * .28f);
            Txt("↑ / ↓ SELECT     ENTER CONFIRM     MISSION SELECT TO EXPLORE THE MAPS", 70, 636, 14, Color.LightGray);
            Txt("WASD MOVE  ·  MOUSE CAMERA  ·  SPACE JUMP  ·  SCYTHE COMBAT", 70, 666, 12, new Color(160, 149, 184));
        }

        void DrawMissionSelect()
        {
            Box(W / 2, H / 2, W, H, 0, Color.Black * .75f);
            Txt("CHOOSE YOUR MISSION", W / 2, 52, 45, WH, 1, VI);
            Txt("THE MAP PREVIEW CHANGES WITH YOUR SELECTION", W / 2, 108, 15, new Color(199, 189, 216), 1);
            string[] descriptions =
            {
                "Hunt the Oni Warlord through the blood moon forest.",
                "Cross the fog and break the Wraith Lord's guard.",
                "Climb the burning temple and face the Fire Shogun."
            };
            Point mouse = MenuVirtualMouse();
            for (int i = 0; i < LevelGen.Themes.Length; i++)
            {
                float x = 70 + i * 388, y = 178, w = 370, h = 382;
                bool unlocked = i <= progression.Profile.UnlockedLevel;
                bool hovered = mouse.X >= x && mouse.X <= x + w && mouse.Y >= y && mouse.Y <= y + h;
                bool active = selectedMenuLevel == i;
                Box(x + w / 2, y + h / 2, w, h, 0, new Color((byte)5, (byte)8, (byte)16, unlocked ? (byte)235 : (byte)205));
                Box(x + w / 2, y + 2, w, 3, 0, active ? CY : Color.White * .2f);
                Box(x + w / 2, y + h - 2, w, 3, 0, active ? VI : Color.White * .2f);
                Box(x + 2, y + h / 2, 3, h, 0, active ? CY : Color.White * .2f);
                Box(x + w - 2, y + h / 2, 3, h, 0, active ? CY : Color.White * .2f);
                DrawMenuThemeArt(LevelGen.Themes[i], x + 12, y + 12, w - 24, 164);
                Txt("MISSION " + (i + 1).ToString("00"), x + 20, y + 190, 13, LevelGen.Themes[i].Accent);
                Txt(LevelGen.Themes[i].Name, x + 20, y + 216, 20, WH);
                Txt(descriptions[i], x + 20, y + 250, 12, new Color(196, 190, 208));
                Txt(unlocked ? "AVAILABLE" : "LOCKED — DEFEAT THE PREVIOUS BOSS", x + 20, y + 292, unlocked ? 14 : 11,
                    unlocked ? new Color(140, 255, 184) : new Color(255, 141, 145));
                if (hovered) Box(x + w / 2, y + h / 2, w - 8, h - 8, 0, new Color(255, 255, 255, 14));
                if (active) Box(x + 22, y + h - 22, 38, 3, 0, CY);
            }
            Box(66, 586, 230, 54, 0, new Color(20, 16, 30, 245));
            Txt("ESC  ·  BACK", 181, 603, 17, Color.LightGray, 1);
            bool canStart = selectedMenuLevel <= progression.Profile.UnlockedLevel;
            Box(888, 586, 324, 54, 0, canStart ? new Color(41, 34, 65, 245) : new Color(25, 25, 32, 240));
            Box(888, 588, 324, 3, 0, canStart ? CY : Color.Gray);
            Txt(canStart ? "ENTER  ·  DEPLOY MISSION" : "MISSION LOCKED", 1050, 602, 16, canStart ? WH : Color.Gray, 1);
            Txt("← / → CHANGE MAP     CLICK A CARD TO PREVIEW", W / 2, 666, 13, new Color(185, 175, 203), 1);
        }

        Point MenuVirtualMouse()
        {
            float scale = Math.Min(Window.ClientBounds.Width / (float)W, Window.ClientBounds.Height / (float)H);
            if (scale <= 0) return new Point(-1, -1);
            float offsetX = (Window.ClientBounds.Width - W * scale) * .5f;
            float offsetY = (Window.ClientBounds.Height - H * scale) * .5f;
            return new Point((int)((mouseCur.X - offsetX) / scale), (int)((mouseCur.Y - offsetY) / scale));
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
            Txt("P PREMIUM SHOP  ·  M SUMMON VAULT  ·  G CALL  ·  X DISMISS", 275, 12, 12, new Color(190, 174, 224));
            if (summonedCompanion != null)
            {
                Bar(40, 88, 220, 6, summonedCompanion.Hp / Math.Max(1, summonedCompanion.Mh), new Color(55, 120, 70), new Color(170, 255, 150));
                Txt(summonedCompanion.Definition.Name.ToUpperInvariant() + "  " + (int)Math.Ceiling(summonedCompanion.Remaining) + "s", 40, 98, 12, new Color(185, 242, 190));
            }
            Txt("LEVEL " + (lv + 1), W - 40, 10, 34, new Color(255, 207, 90), 2); Txt("SOULS " + kills, W - 40, 50, 24, VI, 2);
            Txt("DIAMONDS " + CurrencyBalance("diamonds"), W - 40, 78, 16, new Color(95, 243, 255), 2);
            Txt("SHARDS " + CurrencyBalance("shards"), W - 40, 99, 16, new Color(192, 107, 255), 2);
            Txt("MONEY " + CurrencyBalance("money"), W - 40, 120, 14, new Color(255, 207, 90), 2);
            if (!string.IsNullOrEmpty(progression.Profile.EquippedEyeId))
                Txt(progression.Profile.EquippedEyeId.ToUpperInvariant() + " EYE  ·  Q POWER  ·  F MODE  ·  E POWER", 40, 112, 13, new Color(212, 174, 255));
            if (futureVision) Txt("JOUGAN FUTURE VISION — HAZARDS / OBJECTIVE MARKED", W / 2, 88, 16, new Color(125, 245, 255), 1);
            if (freezeT > 0) Txt("TIME FROZEN  " + freezeT.ToString("0.0") + "s", W / 2, 116, 19, new Color(150, 245, 255), 1);
            if (susanooT > 0) Txt("SUSANOO  " + susanooT.ToString("0.0") + "s", W / 2, 142, 19, new Color(200, 150, 255), 1);
            if (byakuganScout) Txt("BYAKUGAN SCOUT — WASD TO MOVE · F TO RETURN", W / 2, 88, 17, new Color(150, 245, 255), 1);
            if (progression.Profile.EquippedEyeId == "kakashi")
                Txt("CHAKRA: " + progression.Profile.EquippedChakraId.ToUpperInvariant() + "  ·  F SWITCH  ·  Q CAST", 40, 132, 13, new Color(190, 230, 255));
            Txt("Q EYE / SHURIKEN  ·  F MODE / KENJUTSU  ·  E POWER / CLONE  ·  C HIDDEN  ·  R KUNAI  ·  Z SAGE", 40, 153, 11, Color.LightGray);
            if (sageT > 0) Txt("SAGE MODE  " + sageT.ToString("0.0") + "s", W / 2, 166, 16, new Color(160, 255, 170), 1);
            float prog = Cl(P.X / L.GateX, 0, 1); Box(W / 2, 22, 300, 4, 0, Color.White * .15f); Box(W / 2 - 150 + 150 * prog, 22, 300 * prog, 4, 0, Ad(L.Th.Accent, .9f)); Glow(W / 2 - 150 + 300 * prog, 22, 18, Ad(L.Th.Accent, 1));
            string[] ids = { "scythe", "clone", "rasengan", string.Empty };
            string[] keys = { "LMB", "E", "RMB", "SHIFT" };
            string[] labels = { "SCYTHE", "CLONE", "RASENGAN", "DODGE" };
            for (int i = 0; i < ids.Length; i++)
            {
                float X = 40 + i * 100;
                AbilityDefinition ability = ids[i].Length > 0 ? AbilityCatalog.Get(ids[i]) : null;
                bool unlocked = ability == null || progression.IsUsable(ability.Id);
                string readiness = ability == null ? "READY" : AbilityReadiness(ability);
                bool ready = readiness == "READY" || readiness == "ACTIVE";
                float alpha = unlocked ? 1 : .45f;
                Color border = new Color(255, 255, 255, 90);
                Box(X + 46, H - 76, 92, 2, 0, border * alpha); Box(X + 46, H - 12, 92, 2, 0, border * alpha);
                Box(X, H - 44, 2, 64, 0, border * alpha); Box(X + 92, H - 44, 2, 64, 0, border * alpha);
                Txt(keys[i], X + 46, H - 72, keys[i].Length > 2 ? 15 : 22, unlocked ? WH : Color.Gray, 1);
                Txt(labels[i], X + 46, H - 48, 13, unlocked ? CY : Color.DimGray, 1);
                Txt(readiness, X + 46, H - 28, 10, ready ? CY : OR, 1);
            }
            foreach (var e in en) if (e.Boss && e.Aggro) { Bar(W / 2 - 260, 96, 520, 12, e.Hp / e.Mh, new Color(160, 16, 32), new Color(255, 96, 48)); Txt(e.Kind == 0 ? "ONI WARLORD" : e.Kind == 1 ? "THE WRAITH LORD" : "FIRE SHOGUN", W / 2, 58, 30, OR, 1); }
            if (banT > 0) Txt(banS, W / 2, H / 2 - 190, 54, WH * Cl(banT, 0, 1), 1, RD);
#if DEBUG
            if (testMode) Txt("TEST MODE", 40, 84, 18, RD);
#endif
            if (abilityMenuOpen) DrawAbilityMenu();
            if (summoningMenuOpen) DrawSummoningMenu();
            if (shopMenuOpen) DrawShopMenu();
        }

        string AbilityReadiness(AbilityDefinition ability)
        {
            if (!progression.IsUsable(ability.Id)) return "LOCKED";
            if (ability.Id == "clone" && clones.Count > 0) return "ACTIVE";
            float remaining = ability.Id == "clone" ? P.CloneCd : ability.Id == "rasengan" ? P.Kcd : 0;
            if (remaining > 0) return "CD " + Math.Ceiling(remaining) + "s";
            if (ability.ResourceCost > 0 && P.En < ability.ResourceCost) return "NEED ENERGY";
            return "READY";
        }

        float AbilityCooldownRemaining(string abilityId)
        {
            if (abilityId == "clone") return Math.Max(0, P.CloneCd);
            if (abilityId == "rasengan") return Math.Max(0, P.Kcd);
            if (abilityId == "shuriken") return shurikenCd;
            if (abilityId == "kenjutsu") return kenjutsuCd;
            if (abilityId == "hidden") return hiddenCd;
            if (abilityId == "kunai") return kunaiCd;
            return 0;
        }

        void DrawAbilityMenu()
        {
            float x = 74, y = 70, w = 1132, h = 574;
            Box(x + w / 2, y + h / 2, w, h, 0, Color.Black * .76f);
            Box(x + w / 2, y + 2, w, 3, 0, Ad(CY, .85f));
            Box(x + w / 2, y + h - 2, w, 3, 0, Ad(VI, .8f));
            Txt("ABILITY ARCHIVE", x + 30, y + 18, 32, WH);
            Txt("TAB CLOSES  ·  ↑/↓ SELECT  ·  U UPGRADE WITH SHARDS", x + w - 30, y + 30, 13, CY, 2);
            Txt("UNLOCKS FOLLOW YOUR ACTIVE PROFILE  ·  SHARDS " + CurrencyBalance("shards"), x + 30, y + 56, 13, VI);
            IReadOnlyList<AbilityMenuRow> rows = AbilityMenuModel.Build(progression);
            for (int i = 0; i < rows.Count; i++)
            {
                AbilityMenuRow row = rows[i];
                AbilityDefinition ability = row.Definition;
                float rowY = y + 94 + i * 64;
                if (i == selectedAbilityIndex) Box(x + w / 2, rowY + 28, w - 42, 54, 0, new Color(45, 34, 65, 200));
                Box(x + w / 2, rowY + 27, w - 42, 1, 0, Color.White * .1f);
                string status = row.Status;
                if (!ability.IsImplemented) status += row.IsUnlocked ? "  ·  UNLOCKED" : "  ·  LOCKED";
                Color statusColor = row.Status == "Available" ? CY : row.Status == "Locked" ? OR : VI;
                Txt(ability.Name, x + 28, rowY, 20, WH);
                Txt("[" + ability.Binding + "]", x + 320, rowY + 2, 16, CY, 1);
                Txt(status, x + 366, rowY + 3, 14, statusColor);

                string cooldown = ability.CooldownSeconds > 0
                    ? "CD " + (AbilityCooldownRemaining(ability.Id) > 0
                        ? Math.Ceiling(AbilityCooldownRemaining(ability.Id)) + "s"
                        : "READY")
                    : "CD —";
                string cost = ability.ResourceCost > 0 ? "COST " + ability.ResourceCost + " ENERGY" : "FREE";
                string level = row.UpgradeLevel >= ability.MaximumUpgradeLevel ? "MAX LEVEL" : "LV " + row.UpgradeLevel + "/" + ability.MaximumUpgradeLevel + "  ·  " + progression.GetUpgradeShardCost(ability.Id) + " SHARDS";
                Txt(cooldown + "  ·  " + cost + "  ·  " + level, x + w - 28, rowY + 3, 11, WH, 2);
                Txt(row.RequirementText, x + 30, rowY + 32, 12, Color.LightGray);
            }
        }

        void DrawSummoningMenu()
        {
            float x = 58, y = 54, w = 1164, h = 612;
            Box(x + w / 2, y + h / 2, w, h, 0, Color.Black * .91f);
            Box(x + w / 2, y + 2, w, 3, 0, Ad(RD, .9f));
            Box(x + w / 2, y + h - 2, w, 3, 0, Ad(VI, .85f));
            Txt("PREMIUM SUMMON VAULT", x + 28, y + 18, 31, WH);
            Txt("M CLOSE  ·  ↑/↓ SELECT  ·  B BUY  ·  ENTER EQUIP  ·  U UPGRADE  ·  G SUMMON  ·  X DISMISS", x + w - 26, y + 27, 13, CY, 2);
            Txt("DIAMONDS " + CurrencyBalance("diamonds") + "     SHARDS " + CurrencyBalance("shards") + "     SOUL ENERGY " + (int)P.En,
                x + 30, y + 58, 16, new Color(190, 174, 224));

            int visibleRows = 10;
            int first = Math.Max(0, Math.Min(SummoningCatalog.All.Count - visibleRows, selectedSummonIndex - visibleRows / 2));
            for (int rowIndex = 0; rowIndex < visibleRows; rowIndex++)
            {
                int itemIndex = first + rowIndex;
                if (itemIndex >= SummoningCatalog.All.Count) break;
                SummoningDefinition item = SummoningCatalog.All[itemIndex];
                bool selected = itemIndex == selectedSummonIndex;
                bool owned = summoningService.IsOwned(item.Id);
                bool equipped = progression.Profile.EquippedSummonId == item.Id;
                float rowY = y + 98 + rowIndex * 47;
                if (selected) Box(x + 225, rowY + 19, 420, 39, 0, new Color(42, 28, 64, 230));
                Box(x + 18, rowY + 42, 414, 1, 0, Color.White * .12f);
                Txt((itemIndex + 1).ToString("00"), x + 29, rowY + 3, 12, VI);
                Txt(item.Name, x + 70, rowY + 1, 15, selected ? WH : Color.LightGray);
                string itemStatus = equipped ? "EQUIPPED" : owned ? "OWNED" : item.DiamondPrice + " DIAMONDS";
                Color itemColor = owned ? CY : OR;
                Txt(itemStatus, x + 430, rowY + 5, 11, itemColor, 2);
            }

            SummoningDefinition definition = SelectedSummon;
            if (definition == null) return;
            bool isOwned = summoningService.IsOwned(definition.Id);
            bool isEquipped = progression.Profile.EquippedSummonId == definition.Id;
            int level = summoningService.GetUpgradeLevel(definition.Id);
            float rightX = x + 493;
            Box(rightX + 321, y + 337, 626, 1, 0, Color.White * .18f);
            Color primary = RgbColor(definition.PrimaryRgb), accent = RgbColor(definition.AccentRgb);
            Glow(rightX + 310, y + 194, 132, Ad(primary, .8f));
            if (definition.Category == SummonCategory.Bird || definition.Category == SummonCategory.Dragon)
            {
                Tri(rightX + 310, y + 190, 190, 112, -.12f, Ad(accent, .88f));
                Tri(rightX + 310, y + 190, 150, 78, .18f, Ad(primary, .95f));
            }
            else if (definition.Category == SummonCategory.Snake)
            {
                Cres(rightX + 310, y + 193, 128, 100, .2f, false, Ad(primary, .95f));
                Circ(rightX + 352, y + 161, 20, accent);
            }
            else
            {
                Circ(rightX + 310, y + 196, definition.Category == SummonCategory.Legendary ? 54 : 43, primary);
                Circ(rightX + 330, y + 177, 23, accent);
                Tri(rightX + 293, y + 152, 48, 52, -.2f, Ad(accent, .95f));
            }
            string category = definition.Category.ToString().ToUpperInvariant() + "  ·  " + definition.Role.ToString().ToUpperInvariant();
            Txt(category, rightX + 310, y + 96, 17, accent, 1);
            Txt(definition.Name, rightX + 310, y + 120, 31, WH, 1);
            string access = isOwned ? (isEquipped ? "OWNED  ·  EQUIPPED" : "OWNED  ·  PERMANENT") : "PREMIUM  ·  " + definition.DiamondPrice + " DIAMONDS";
            Txt(access, rightX + 310, y + 275, 18, isOwned ? CY : OR, 1);
            Txt(definition.Description, rightX + 310, y + 311, 12, Color.LightGray, 1);
            Txt("DURATION " + (int)(definition.DurationSeconds * summoningService.GetStats(definition.Id).DurationMultiplier) + "s   ·   COOLDOWN " + (int)(definition.CooldownSeconds * summoningService.GetStats(definition.Id).CooldownMultiplier) + "s", rightX + 310, y + 365, 15, WH, 1);
            Txt("HP " + (int)(definition.MaximumHealth * summoningService.GetStats(definition.Id).HealthMultiplier) + "   ·   DAMAGE " + (int)(definition.Damage * summoningService.GetStats(definition.Id).DamageMultiplier) + (definition.HealingPerSecond > 0 ? "   ·   HEAL " + (int)(definition.HealingPerSecond * summoningService.GetStats(definition.Id).HealingMultiplier) + "/s" : ""), rightX + 310, y + 391, 14, Color.LightGray, 1);
            string upgrade = level >= definition.MaximumUpgradeLevel ? "MAX UPGRADE" : "UPGRADE " + level + "/" + definition.MaximumUpgradeLevel + "  ·  " + summoningService.GetUpgradeShardCost(definition.Id) + " SHARDS";
            Txt(upgrade, rightX + 310, y + 425, 16, VI, 1);
#if DEBUG
            if (summoningService.IsTestMode) Txt("TEST MODE  ·  ALL SUMMONS OWNED / MAX LEVEL  ·  NO NORMAL PROFILE GRANTS", rightX + 310, y + 463, 12, RD, 1);
            else
#endif
            Txt("LOCKED SUMMONS REQUIRE VAULT PURCHASE; PROGRESSION ALONE NEVER UNLOCKS THEM.", rightX + 310, y + 463, 11, OR, 1);
            Txt("G ACTIVATE SELECTED  ·  X DISMISS ACTIVE  ·  ACTIVE: " + (summonedCompanion == null ? "NONE" : summonedCompanion.Definition.Name.ToUpperInvariant()), rightX + 310, y + 500, 13, CY, 1);
        }

        void DrawShopMenu()
        {
            float x = 58, y = 54, w = 1164, h = 612;
            Box(x + w / 2, y + h / 2, w, h, 0, Color.Black * .94f);
            Box(x + w / 2, y + 2, w, 3, 0, Ad(new Color(255, 207, 90), .95f));
            Txt("KADAYA PREMIUM SHOP", x + 28, y + 18, 31, WH);
            Txt("P CLOSE · ↑/↓ SELECT · B BUY · ENTER EQUIP · U SUSANOO UPGRADE · Z SAGE MODE", x + w - 26, y + 28, 12, CY, 2);
            Txt("MONEY " + CurrencyBalance("money") + "     DIAMONDS " + CurrencyBalance("diamonds") + "     SHARDS " + CurrencyBalance("shards"),
                x + 30, y + 58, 16, new Color(220, 200, 160));
            int rows = 11, first = Math.Max(0, Math.Min(PremiumShopCatalog.All.Count - rows, selectedShopIndex - rows / 2));
            for (int row = 0; row < rows; row++)
            {
                int index = first + row; if (index >= PremiumShopCatalog.All.Count) break;
                ShopItem item = PremiumShopCatalog.All[index]; bool owned = shopService.IsOwned(item.Id), selected = index == selectedShopIndex;
                float rowY = y + 96 + row * 45;
                if (selected) Box(x + 222, rowY + 19, 418, 39, 0, new Color(52, 39, 73, 235));
                Box(x + 20, rowY + 40, 404, 1, 0, Color.White * .1f);
                Txt((index + 1).ToString("00"), x + 28, rowY + 3, 12, VI);
                Txt(item.Name, x + 68, rowY + 2, 14, selected ? WH : Color.LightGray);
                Txt(owned ? "OWNED" : item.Price + " " + item.Currency.ToUpperInvariant(), x + 426, rowY + 5, 11, owned ? CY : OR, 2);
            }
            ShopItem chosen = PremiumShopCatalog.All[selectedShopIndex]; bool isOwned = shopService.IsOwned(chosen.Id);
            float right = x + 488;
            Box(right + 326, y + 338, 630, 1, 0, Color.White * .18f);
            Color tint = chosen.Kind == ShopItemKind.Eye ? new Color(180, 115, 255) : chosen.Kind == ShopItemKind.Weapon ? CY : new Color(255, 207, 90);
            Glow(right + 326, y + 194, 132, Ad(tint, .8f));
            Circ(right + 326, y + 190, chosen.Kind == ShopItemKind.Eye ? 48 : 40, tint);
            Tri(right + 326, y + 163, 78, 78, -.18f, Ad(WH, .85f));
            Txt(chosen.Kind.ToString().ToUpperInvariant(), right + 326, y + 95, 15, tint, 1);
            Txt(chosen.Name, right + 326, y + 122, 28, WH, 1);
            Txt(chosen.Description, right + 326, y + 290, 14, Color.LightGray, 1);
            Txt(isOwned ? "OWNED PERMANENTLY" : "PRICE  " + chosen.Price + " " + chosen.Currency.ToUpperInvariant(), right + 326, y + 324, 17, isOwned ? CY : OR, 1);
            Txt("EQUIPPED EYE  " + (string.IsNullOrEmpty(progression.Profile.EquippedEyeId) ? "NONE" : progression.Profile.EquippedEyeId.ToUpperInvariant()), right + 326, y + 370, 13, Color.LightGray, 1);
            Txt("SUSANOO DURATION  " + shopService.GetSusanooDuration().ToString("0") + " / 12 SECONDS", right + 326, y + 406, 14, new Color(205, 160, 255), 1);
            Txt(shopService.GetSusanooLevel() >= 9 ? "SUSANOO MAX LEVEL" : "NEXT UPGRADE  " + shopService.GetSusanooUpgradeCost() + " SHARDS", right + 326, y + 434, 13, VI, 1);
            Txt("Z TO ACTIVATE EQUIPPED SAGE MODE  ·  EYE POWERS: Q / E / F", right + 326, y + 472, 12, CY, 1);
#if DEBUG
            if (shopService.IsTestMode) Txt("TEST MODE — ENTER TO EQUIP ANY ITEM · PURCHASES DO NOT GRANT NORMAL PROFILE OWNERSHIP", right + 326, y + 516, 11, RD, 1);
#endif
        }
    }
}
