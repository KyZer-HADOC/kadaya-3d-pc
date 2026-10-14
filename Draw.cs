using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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
            Box(x + 245, y + 23, 490, 46, 0, active ? new Color(34, 19, 40, 242) : new Color(5, 7, 14, 210));
            Box(x + 245, y + 2, 490, 1, 0, active ? Ad(RD, .8f) : Color.White * .14f);
            Box(x + 245, y + 44, 490, 1, 0, active ? Ad(VI, .8f) : Color.White * .1f);
            Box(x + 3, y + 23, active ? 4 : 2, 40, 0, active ? CY : Color.White * .16f);
            if (active) { Tri(x + 15, y + 23, 17, 26, MathHelper.PiOver2, Ad(RD, .9f)); Glow(x + 35, y + 23, 36, Ad(VI, .22f)); }
            Txt(label, x + 28, y + 8, 21, active ? WH : new Color(174, 170, 185));
            Txt(detail, x + 466, y + 13, 12, active ? CY : new Color(121, 120, 140), 2);
        }

        void DrawMainMenu()
        {
            Box(W / 2, H / 2, W, H, 0, Color.Black * .58f);
            Glow(920, 380, 840, Ad(L.Th.Accent, .17f));
            Glow(1030, 220, 530, Ad(VI, .11f));
            for (int i = 0; i < 36; i++)
            {
                float x = Hs(i + 12) * W;
                float y = (Hs(i + 174) * H + t * (10 + i % 7) * (i % 2 == 0 ? 1 : -1) + H) % H;
                float radius = 2 + Hs(i + 322) * 4;
                Circ(x, y, radius, Ad(i % 3 == 0 ? RD : VI, .12f + .1f * MathF.Sin(t * 1.4f + i)));
            }
            Box(340, H / 2, 560, 618, 0, new Color(3, 5, 12, 224));
            Box(68, 92, 560, 2, 0, Ad(VI, .95f));
            Box(70, 96, 4, 168, 0, Ad(RD, .95f));
            Glow(104, 132, 118, Ad(VI, .22f));
            Txt("KADAYA", 92, 98, 102, new Color(238, 228, 250), 0, VI);
            Txt("THE LAST SHADOW", 100, 207, 22, new Color(255, 112, 146));
            Txt("A REAPER'S JOURNEY THROUGH THE SHADOW REALMS", 100, 245, 12, new Color(185, 178, 202));

            var labels = new List<string>();
            var details = new List<string>();
            if (HasContinueSave)
            { labels.Add("CONTINUE"); details.Add("LEVEL " + (progression.Profile.UnlockedLevel + 1).ToString("00")); }
            labels.Add("START"); details.Add("MAP AND LEVEL SELECT");
            labels.Add("SHOP"); details.Add("EYES  /  SWORDS  /  SUMMONS");
            labels.Add("UPGRADE"); details.Add("ABILITIES  /  COMPANIONS");
            labels.Add("SETTINGS"); details.Add("AUDIO  /  DISPLAY  /  CONTROLS");
            labels.Add("EXIT"); details.Add("RETURN TO DESKTOP");
            Point mouse = MenuVirtualMouse();
            for (int i = 0; i < labels.Count; i++)
            {
                float y = 311 + i * 53;
                bool hovered = mouse.X >= 76 && mouse.X <= 572 && mouse.Y >= y && mouse.Y <= y + 46;
                DrawMenuButton(labels[i], details[i], 76, y, mainMenuIndex == i, hovered);
            }

            float cardX = 680, cardY = 86, cardW = 540;
            MissionDefinition frontier = MissionCatalog.Get(Math.Max(0, Math.Min(MissionCatalog.All.Count - 1, lv)));
            Box(cardX + cardW / 2, 360, cardW, 600, 0, new Color(3, 5, 12, 207));
            Box(cardX + 1, cardY + 3, 2, 548, 0, Ad(L.Th.Accent, .8f));
            Txt("THE REAPER RETURNS", cardX + 32, 110, 14, new Color(209, 201, 220));
            DrawMenuThemeArt(L.Th, cardX + 22, 150, cardW - 44, 240);
            Txt("CURRENT FRONTIER", cardX + 30, 415, 13, Color.LightGray);
            Txt("MISSION  " + (lv + 1).ToString("00"), cardX + 30, 442, 15, L.Th.Accent);
            Txt(frontier.Name, cardX + 30, 468, 24, WH);
            Txt("BOSS  /  " + frontier.BossName.ToUpperInvariant(), cardX + 30, 512, 13, new Color(255, 157, 125));
            Txt("MISSIONS CLEARED  " + progression.Profile.CompletedMissionIds.Count + " / " + MissionCatalog.All.Count,
                cardX + 30, 544, 13, new Color(184, 174, 207));
            Box(cardX + cardW / 2, 582, cardW - 60, 1, 0, Color.White * .16f);
            Txt("3D DARK-FANTASY ACTION  ·  OFFLINE PROGRESSION", cardX + 30, 601, 12, new Color(180, 171, 197));
            Box(68, 652, 1140, 1, 0, Color.White * .22f);
            Txt("UP / DOWN SELECT     ENTER CONFIRM     CLICK TO NAVIGATE", 70, 666, 12, Color.LightGray);
            Txt("WASD MOVE  ·  MOUSE CAMERA  ·  SPACE JUMP  ·  LMB SCYTHE", 70, 687, 10, new Color(160, 149, 184));
        }

        void DrawMissionSelect()
        {
            Box(W / 2, H / 2, W, H, 0, Color.Black * .75f);
            Point mouse = MenuVirtualMouse();
            if (titlePage == "maps")
            {
                Txt("CHOOSE A REALM", W / 2, 52, 45, WH, 1, VI);
                Txt("THREE DISTINCT MAPS  ·  FIFTEEN MISSIONS EACH  ·  ALL OPEN FOR REPLAY", W / 2, 108, 15, new Color(199, 189, 216), 1);
                for (int i = 0; i < MissionCatalog.Maps.Count; i++)
                {
                    MissionMapDefinition map = MissionCatalog.Maps[i];
                    float x = 70 + i * 388, y = 178, w = 370, h = 382;
                    bool active = selectedMenuMap == i;
                    bool hovered = mouse.X >= x && mouse.X <= x + w && mouse.Y >= y && mouse.Y <= y + h;
                    int cleared = 0;
                    foreach (MissionDefinition mission in MissionCatalog.All)
                        if (mission.MapIndex == i && progression.Profile.CompletedMissionIds.Contains(mission.Id)) cleared++;
                    Box(x + w / 2, y + h / 2, w, h, 0, new Color(5, 8, 16, 235));
                    Box(x + w / 2, y + 2, w, 3, 0, active ? CY : Color.White * .2f);
                    Box(x + w / 2, y + h - 2, w, 3, 0, active ? VI : Color.White * .2f);
                    DrawMenuThemeArt(LevelGen.Themes[i], x + 12, y + 12, w - 24, 180);
                    Txt("REALM  0" + (i + 1), x + 22, y + 208, 13, LevelGen.Themes[i].Accent);
                    Txt(map.Name, x + 22, y + 234, 20, WH);
                    Txt(map.Description, x + 22, y + 272, 12, new Color(196, 190, 208));
                    Txt("15 SELECTABLE MISSIONS", x + 22, y + 315, 14, CY);
                    Txt("CLEARED  " + cleared.ToString("00") + " / 15", x + 22, y + 346, 13, new Color(184, 174, 207));
                    if (hovered) Box(x + w / 2, y + h / 2, w - 8, h - 8, 0, new Color(255, 255, 255, 14));
                }
                Box(66, 586, 230, 54, 0, new Color(20, 16, 30, 245));
                Txt("ESC  ·  BACK", 181, 603, 17, Color.LightGray, 1);
                Box(592, 613, 300, 54, 0, new Color(31, 21, 39, 245));
                Txt("N  ·  NEW CAMPAIGN", 592, 603, 15, new Color(221, 187, 238), 1);
                Box(888, 586, 324, 54, 0, new Color(41, 34, 65, 245));
                Box(888, 588, 324, 3, 0, CY);
                Txt("ENTER  ·  VIEW MISSIONS", 1050, 602, 16, WH, 1);
                Txt("← / → CHOOSE REALM     ENTER VIEW LEVELS", W / 2, 666, 13, new Color(185, 175, 203), 1);
                return;
            }

            MissionDefinition selected = MissionCatalog.Get(selectedMenuLevel);
            bool completed = progression.Profile.CompletedMissionIds.Contains(selected.Id);
            float firstX = 55, firstY = 158, cardW = 164, cardH = 112, gapX = 10, gapY = 16;
            Txt(MissionCatalog.Maps[selectedMenuMap].Name, 55, 46, 32, WH, 0, VI);
            Txt("SELECT ANY MISSION  ·  CLEARED " + progression.Profile.CompletedMissionIds.Count + " / 45", 55, 91, 13, new Color(199, 189, 216));
            for (int i = 0; i < MissionCatalog.MissionsPerMap; i++)
            {
                MissionDefinition mission = MissionCatalog.Get(selectedMenuMap * MissionCatalog.MissionsPerMap + i);
                float x = firstX + (i % 5) * (cardW + gapX), y = firstY + (i / 5) * (cardH + gapY);
                bool active = mission.Index == selectedMenuLevel;
                bool isComplete = progression.Profile.CompletedMissionIds.Contains(mission.Id);
                bool hovered = mouse.X >= x && mouse.X <= x + cardW && mouse.Y >= y && mouse.Y <= y + cardH;
                Box(x + cardW / 2, y + cardH / 2, cardW, cardH, 0, new Color(7, 9, 17, active ? (byte)248 : (byte)225));
                Box(x + cardW / 2, y + 2, cardW, 3, 0, active ? CY : isComplete ? new Color(112, 220, 155) : Color.White * .18f);
                Txt("MISSION " + mission.LevelNumber.ToString("00"), x + 10, y + 12, 11, active ? CY : LevelGen.Themes[selectedMenuMap].Accent);
                Txt(mission.Name, x + 10, y + 38, 12, WH);
                Txt(isComplete ? "CLEARED" : "AVAILABLE", x + 10, y + 67, 10, isComplete ? new Color(140, 255, 184) : new Color(191, 183, 208));
                Txt(mission.IsBossMilestone ? "MAJOR BOSS" : "BOSS", x + 10, y + 88, 9, new Color(255, 157, 125));
                if (hovered) Box(x + cardW / 2, y + cardH / 2, cardW - 8, cardH - 8, 0, new Color(255, 255, 255, 12));
            }
            float right = 949;
            Box(right + 138, 350, 276, 390, 0, new Color(5, 8, 16, 240));
            Box(right + 2, 352, 3, 388, 0, Ad(LevelGen.Themes[selectedMenuMap].Accent, .9f));
            DrawMenuThemeArt(LevelGen.Themes[selectedMenuMap], right + 14, 160, 248, 142);
            Txt("MISSION " + selected.LevelNumber.ToString("00"), right + 18, 319, 13, LevelGen.Themes[selectedMenuMap].Accent);
            Txt(selected.Name, right + 18, 346, 19, WH);
            Txt("DIFFICULTY  " + selected.Difficulty + " / 15", right + 18, 382, 12, OR);
            Txt(selected.IsBossMilestone ? "MAJOR BOSS ENCOUNTER" : "BOSS ENCOUNTER", right + 18, 408, 11, new Color(255, 157, 125));
            Txt("BOSS  " + selected.BossName.ToUpperInvariant(), right + 18, 436, 11, WH);
            Txt(selected.Objective, right + 18, 470, 11, new Color(196, 190, 208));
            Txt("MISSION CLEAR REWARD", right + 18, 511, 10, VI);
            Txt("M " + selected.RewardMoney + "  ·  D " + selected.RewardDiamonds + "  ·  S " + selected.RewardShards, right + 18, 528, 10, VI);
            float bestTime;
            string progress = completed ? "MISSION CLEARED" : "READY TO DEPLOY";
            if (progression.Profile.BestMissionTimes.TryGetValue(selected.Id, out bestTime)) progress += "  ·  BEST " + FormatMissionTime(bestTime);
            Txt(progress, right + 18, 550, 9, completed ? new Color(140, 255, 184) : new Color(184, 174, 207));
            Box(66, 586, 230, 54, 0, new Color(20, 16, 30, 245));
            Txt("ESC  ·  MAPS", 181, 603, 17, Color.LightGray, 1);
            Box(592, 613, 300, 54, 0, new Color(31, 21, 39, 245));
            Txt("N  ·  NEW CAMPAIGN", 592, 603, 15, new Color(221, 187, 238), 1);
            Box(888, 586, 324, 54, 0, new Color(41, 34, 65, 245));
            Box(888, 588, 324, 3, 0, CY);
            Txt("ENTER  ·  PLAY MISSION", 1050, 602, 16, WH, 1);
            Txt("ARROWS SELECT  ·  ENTER PLAY  ·  ESC MAPS", W / 2, 666, 13, new Color(185, 175, 203), 1);
        }

        string FormatMissionTime(float seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return string.Format("{0:00}:{1:00}", (int)time.TotalMinutes, time.Seconds);
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
            if (settings.ShowVitals)
            {
                Bar(40, 36, 340, 18, P.Hp / P.Mh, new Color(138, 10, 34), new Color(255, 51, 85));
                Bar(40, 64, 260, 10, P.En / 100, new Color(42, 108, 255), CY);
                Txt("THE REAPER", 40, 4, 26, WH);
                if (summonedCompanion != null)
                {
                    Bar(40, 88, 220, 6, summonedCompanion.Hp / Math.Max(1, summonedCompanion.Mh), new Color(55, 120, 70), new Color(170, 255, 150));
                    Txt(summonedCompanion.Definition.Name.ToUpperInvariant() + "  " + (int)Math.Ceiling(summonedCompanion.Remaining) + "s", 40, 98, 12, new Color(185, 242, 190));
                }
            }
            Txt("LEVEL " + (lv + 1), W - 40, 10, 34, new Color(255, 207, 90), 2); Txt("SOULS " + kills, W - 40, 50, 24, VI, 2);
            if (settings.ShowWallet)
            {
                Txt("DIAMONDS " + CurrencyBalance("diamonds"), W - 40, 78, 16, new Color(95, 243, 255), 2);
                Txt("SHARDS " + CurrencyBalance("shards"), W - 40, 99, 16, new Color(192, 107, 255), 2);
                Txt("MONEY " + CurrencyBalance("money"), W - 40, 120, 14, new Color(255, 207, 90), 2);
            }
            if (settings.ShowHints && !string.IsNullOrEmpty(progression.Profile.EquippedEyeId))
                Txt(progression.Profile.EquippedEyeId.ToUpperInvariant() + " EYE  ·  " + BoundKey("EyePrimary", Keys.Q) + " POWER  ·  " + BoundKey("ModeOrKenjutsu", Keys.F) + " MODE", 40, 112, 13, new Color(212, 174, 255));
            if (futureVision) Txt("JOUGAN FUTURE VISION — HAZARDS / OBJECTIVE MARKED", W / 2, 88, 16, new Color(125, 245, 255), 1);
            if (freezeT > 0) Txt("TIME FROZEN  " + freezeT.ToString("0.0") + "s", W / 2, 116, 19, new Color(150, 245, 255), 1);
            if (susanooT > 0) Txt("SUSANOO  " + susanooT.ToString("0.0") + "s", W / 2, 142, 19, new Color(200, 150, 255), 1);
            if (byakuganScout) Txt("BYAKUGAN SCOUT — WASD TO MOVE · F TO RETURN", W / 2, 88, 17, new Color(150, 245, 255), 1);
            if (settings.ShowHints && progression.Profile.EquippedEyeId == "kakashi")
                Txt("CHAKRA: " + progression.Profile.EquippedChakraId.ToUpperInvariant() + "  ·  " + BoundKey("ModeOrKenjutsu", Keys.F) + " SWITCH  ·  " + BoundKey("EyePrimary", Keys.Q) + " CAST", 40, 132, 13, new Color(190, 230, 255));
            if (settings.ShowHints)
            {
                Txt(BoundKey("EyePrimary", Keys.Q) + " EYE / SHURIKEN  ·  " + BoundKey("ModeOrKenjutsu", Keys.F) + " MODE  ·  " + BoundKey("PowerSecondary", Keys.E) + " POWER / CLONE  ·  " + BoundKey("HiddenJutsu", Keys.C) + " HIDDEN  ·  " + BoundKey("Kunai", Keys.R) + " KUNAI  ·  " + BoundKey("SageMode", Keys.Z) + " SAGE", 40, 153, 10, Color.LightGray);
                Txt(BoundKey("Shop", Keys.P) + " SHOP  ·  " + BoundKey("Summons", Keys.M) + " SUMMONS  ·  " + BoundKey("SummonCall", Keys.G) + " CALL  ·  " + BoundKey("SummonDismiss", Keys.X) + " DISMISS  ·  " + BoundKey("Abilities", Keys.Tab) + " ABILITIES", 40, 170, 10, new Color(190, 174, 224));
            }
            if (sageT > 0) Txt("SAGE MODE  " + sageT.ToString("0.0") + "s", W / 2, 166, 16, new Color(160, 255, 170), 1);
            float prog = Cl(P.X / L.GateX, 0, 1); Box(W / 2, 22, 300, 4, 0, Color.White * .15f); Box(W / 2 - 150 + 150 * prog, 22, 300 * prog, 4, 0, Ad(L.Th.Accent, .9f)); Glow(W / 2 - 150 + 300 * prog, 22, 18, Ad(L.Th.Accent, 1));
            string[] ids = { "scythe", "clone", "rasengan", string.Empty };
            string[] keys = { "LMB", BoundKey("PowerSecondary", Keys.E).ToString(), "RMB", BoundKey("Dodge", Keys.LeftShift).ToString() };
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
            if (settings.ShowVitals) foreach (var e in en) if (e.Boss && e.Aggro) { Bar(W / 2 - 260, 96, 520, 12, e.Hp / e.Mh, new Color(160, 16, 32), new Color(255, 96, 48)); Txt(L.Mission.BossName.ToUpperInvariant(), W / 2, 58, 24, OR, 1); }
            if (settings.ShowVitals && (HasEye("sharingan") || HasEye("mangekyo") || HasEye("ems")))
            {
                int shown = 0;
                foreach (Foe foe in en)
                {
                    if (foe.Dead || shown >= 24) continue;
                    Vector2 marker = ScreenAt(foe.X, G - foe.Y - 120 * foe.S, foe.Z);
                    if (marker.X < 0 || marker.X > W || marker.Y < 0 || marker.Y > H) continue;
                    float width = foe.Boss ? 74 : 44, fill = width * Cl(foe.Hp / Math.Max(1, foe.Mh), 0, 1);
                    Box(marker.X, marker.Y, width + 4, 5, 0, Color.Black * .75f);
                    Box(marker.X - width / 2 + fill / 2, marker.Y, fill, 3, 0, foe.Boss ? OR : RD);
                    shown++;
                }
            }
            if (banT > 0) Txt(banS, W / 2, H / 2 - 190, 54, WH * Cl(banT, 0, 1), 1, RD);
#if DEBUG
            if (testMode) Txt("TEST MODE", 40, 84, 18, RD);
#endif
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

        string AbilityBinding(AbilityDefinition ability)
        {
            if (ability.Id == "scythe") return "LMB";
            if (ability.Id == "shuriken") return BoundKey("EyePrimary", Keys.Q).ToString();
            if (ability.Id == "kenjutsu") return BoundKey("ModeOrKenjutsu", Keys.F).ToString();
            if (ability.Id == "hidden") return BoundKey("HiddenJutsu", Keys.C).ToString();
            if (ability.Id == "clone") return BoundKey("PowerSecondary", Keys.E).ToString();
            if (ability.Id == "kunai") return BoundKey("Kunai", Keys.R).ToString();
            if (ability.Id == "rasengan") return "RMB";
            return ability.Binding;
        }

        void DrawAbilityMenu()
        {
            float x = 74, y = 70, w = 1132, h = 574;
            Box(x + w / 2, y + h / 2, w, h, 0, Color.Black * .76f);
            Box(x + w / 2, y + 2, w, 3, 0, Ad(CY, .85f));
            Box(x + w / 2, y + h - 2, w, 3, 0, Ad(VI, .8f));
            Txt("ABILITY ARCHIVE", x + 30, y + 18, 32, WH);
            Txt(BoundKey("Abilities", Keys.Tab) + " CLOSES  ·  " + BoundKey("MenuUp", Keys.Up) + "/" + BoundKey("MenuDown", Keys.Down) + " SELECT  ·  " + BoundKey("Upgrade", Keys.U) + " UPGRADE WITH SHARDS", x + w - 30, y + 30, 13, CY, 2);
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
                Txt("[" + AbilityBinding(ability) + "]", x + 320, rowY + 2, 16, CY, 1);
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
            DrawShopMenu();
        }

        void DrawShopMenu()
        {
            float x = 48, y = 42, w = 1184, h = 636;
            ShopCategory category = (ShopCategory)Math.Max(0, Math.Min(4, selectedShopCategory));
            IReadOnlyList<ShopMenuEntry> entries = CurrentShopEntries;
            if (entries.Count == 0) return;
            selectedShopIndex = (int)Cl(selectedShopIndex, 0, entries.Count - 1);
            Box(x + w / 2, y + h / 2, w, h, 0, new Color(3, 4, 10, 246));
            Box(x + w / 2, y + 2, w, 2, 0, Ad(category == ShopCategory.Eyes ? VI : category == ShopCategory.Summons ? RD : CY, .9f));
            Box(x + 10, y + 4, 3, h - 8, 0, Ad(RD, .75f));
            Txt("FIELD ARCHIVE  /  SHOP", x + 28, y + 17, 29, WH);
            Txt("EARNED CURRENCY ONLY  ·  NO REAL-MONEY LOCKS", x + w - 24, y + 28, 11, new Color(194, 184, 211), 2);
            string[] categories = { "EYES", "SWORDS", "SUSANO", "SPECIAL ABILITIES", "SUMMONS" };
            for (int i = 0; i < categories.Length; i++)
            {
                float tabX = 72 + i * 226;
                bool active = i == selectedShopCategory;
                Box(tabX + 104, y + 79, 208, 38, 0, active ? new Color(44, 26, 57, 238) : new Color(12, 13, 22, 238));
                Box(tabX + 104, y + 61, 208, 2, 0, active ? CY : Color.White * .12f);
                Txt(categories[i], tabX + 104, y + 68, 14, active ? WH : Color.Gray, 1);
            }
            Txt("MONEY " + CurrencyBalance("money") + "     DIAMONDS " + CurrencyBalance("diamonds") + "     SHARDS " + CurrencyBalance("shards"),
                x + 28, y + 124, 13, new Color(220, 200, 160));
            int visibleRows = 9;
            int first = Math.Max(0, Math.Min(entries.Count - visibleRows, selectedShopIndex - visibleRows / 2));
            for (int row = 0; row < visibleRows; row++)
            {
                int index = first + row;
                if (index >= entries.Count) break;
                ShopMenuEntry entry = entries[index];
                bool selected = index == selectedShopIndex;
                float rowY = y + 151 + row * 44;
                if (selected) Box(282, rowY + 18, 420, 38, 0, new Color(39, 24, 54, 242));
                Box(75, rowY + 39, 414, 1, 0, Color.White * .1f);
                Txt((index + 1).ToString("00"), 88, rowY + 3, 12, VI);
                Txt(entry.Name, 124, rowY + 2, 14, selected ? WH : Color.LightGray);
                string status = ShopEntryStatus(entry);
                Txt(status, 485, rowY + 5, 10, status.Contains("OWNED") || status.Contains("READY") || status.Contains("EQUIPPED") ? CY : OR, 2);
            }

            ShopMenuEntry chosen = entries[selectedShopIndex];
            float right = 772;
            Box(730, y + 145, 1, 424, 0, Color.White * .16f);
            Color tint = chosen.Kind == ShopMenuEntryKind.Ability ? VI : chosen.Kind == ShopMenuEntryKind.Summon ? RD : CY;
            Glow(right + 202, y + 211, 150, Ad(tint, .65f));
            if (chosen.Summon != null)
            {
                SummoningDefinition summon = chosen.Summon;
                Color primary = RgbColor(summon.PrimaryRgb), accent = RgbColor(summon.AccentRgb);
                if (summon.Category == SummonCategory.Bird || summon.Category == SummonCategory.Dragon)
                { Tri(right + 202, y + 213, 176, 102, -.12f, Ad(accent, .88f)); Tri(right + 202, y + 212, 136, 70, .18f, Ad(primary, .95f)); }
                else if (summon.Category == SummonCategory.Snake)
                { Cres(right + 202, y + 211, 120, 94, .2f, false, Ad(primary, .95f)); Circ(right + 240, y + 178, 18, accent); }
                else { Circ(right + 202, y + 215, summon.Category == SummonCategory.Legendary ? 55 : 43, primary); Circ(right + 224, y + 193, 23, accent); Tri(right + 185, y + 170, 48, 52, -.2f, Ad(accent, .95f)); }
                Txt(summon.Category.ToString().ToUpperInvariant() + "  /  " + summon.Role.ToString().ToUpperInvariant(), right + 202, y + 111, 14, accent, 1);
                Txt(summon.Name, right + 202, y + 134, 27, WH, 1);
                bool owned = summoningService.IsOwned(summon.Id);
                bool equipped = progression.Profile.EquippedSummonId == summon.Id;
                int level = summoningService.GetUpgradeLevel(summon.Id);
                SummonUpgradeStats stats = summoningService.GetStats(summon.Id);
                Txt(owned ? equipped ? "OWNED  /  EQUIPPED" : "OWNED  /  READY" : "COST  " + summon.DiamondPrice + " EARNED DIAMONDS", right + 202, y + 286, 15, owned ? CY : OR, 1);
                Txt(summon.Description, right + 202, y + 318, 12, Color.LightGray, 1);
                Txt("DURATION " + (int)(summon.DurationSeconds * stats.DurationMultiplier) + "s  /  COOLDOWN " + (int)(summon.CooldownSeconds * stats.CooldownMultiplier) + "s", right + 202, y + 356, 13, WH, 1);
                Txt("HP " + (int)(summon.MaximumHealth * stats.HealthMultiplier) + "  /  DAMAGE " + (int)(summon.Damage * stats.DamageMultiplier) + (summon.HealingPerSecond > 0 ? "  /  HEAL " + summon.HealingPerSecond.ToString("0.0") + "/s" : ""), right + 202, y + 381, 12, Color.LightGray, 1);
                Txt("UPGRADE LEVEL " + level + " / " + summon.MaximumUpgradeLevel + "  ·  " + (level >= summon.MaximumUpgradeLevel ? "MAX" : summoningService.GetUpgradeShardCost(summon.Id) + " SHARDS"), right + 202, y + 407, 12, VI, 1);
                Txt("UNLOCK WITH DIAMONDS EARNED FROM ENEMIES AND BOSSES.", right + 202, y + 439, 11, new Color(202, 192, 216), 1);
            }
            else if (chosen.Ability != null)
            {
                AbilityDefinition ability = chosen.Ability;
                AbilityMenuRow row = FindAbilityRow(ability.Id);
                Txt("COMBAT ABILITY  /  " + (row.IsUnlocked ? "AVAILABLE" : "PROGRESSION LOCKED"), right + 202, y + 111, 14, row.IsUnlocked ? CY : OR, 1);
                Txt(ability.Name, right + 202, y + 139, 28, WH, 1);
                Txt(ability.Description, right + 202, y + 199, 13, Color.LightGray, 1);
                Txt("CONTROL  " + AbilityBinding(ability) + "  /  COST " + ability.ResourceCost + " SOUL ENERGY", right + 202, y + 252, 13, WH, 1);
                Txt("COOLDOWN  " + ability.CooldownSeconds.ToString("0.0") + "s  /  LEVEL " + row.UpgradeLevel + " / " + ability.MaximumUpgradeLevel, right + 202, y + 280, 13, VI, 1);
                Txt(row.IsUnlocked ? "UNLOCKED THROUGH GAMEPLAY" : row.RequirementText.ToUpperInvariant(), right + 202, y + 325, 11, row.IsUnlocked ? CY : OR, 1);
                Txt("UPGRADES USE SHARDS ON THE UPGRADE SCREEN.", right + 202, y + 355, 11, new Color(202, 192, 216), 1);
            }
            else if (chosen.Kind == ShopMenuEntryKind.StarterWeapon)
            {
                Txt("STARTING WEAPON", right + 202, y + 111, 14, CY, 1);
                Txt(chosen.Name, right + 202, y + 139, 28, WH, 1);
                Txt(chosen.Description, right + 202, y + 199, 13, Color.LightGray, 1);
                Txt(string.IsNullOrEmpty(progression.Profile.EquippedWeaponId) ? "EQUIPPED" : "AVAILABLE TO EQUIP", right + 202, y + 257, 15, CY, 1);
                Txt("THE THREE-HIT SCYTHE COMBO REMAINS THE STARTING MELEE ACTION.", right + 202, y + 292, 11, new Color(202, 192, 216), 1);
            }
            else if (chosen.ShopItem != null)
            {
                ShopItem item = chosen.ShopItem;
                bool owned = shopService.IsOwned(item.Id), equipped = shopService.IsEquipped(item.Id);
                Txt(item.Kind.ToString().ToUpperInvariant(), right + 202, y + 111, 14, tint, 1);
                Txt(item.Name, right + 202, y + 139, 28, WH, 1);
                Txt(item.Description, right + 202, y + 199, 13, Color.LightGray, 1);
                Txt(owned ? equipped ? "OWNED  /  EQUIPPED" : "OWNED  /  READY TO EQUIP" : "COST  " + item.Price + " " + item.Currency.ToUpperInvariant(), right + 202, y + 257, 15, owned ? CY : OR, 1);
                if (item.Kind == ShopItemKind.SageMode) Txt("ACTIVATE THE EQUIPPED MODE WITH " + BoundKey("SageMode", Keys.Z), right + 202, y + 294, 12, new Color(202, 192, 216), 1);
                else if (item.Kind == ShopItemKind.Eye) Txt("EQUIP THE EYE TO USE THE LISTED EYE / POWER / MODE KEYS.", right + 202, y + 294, 12, new Color(202, 192, 216), 1);
                else if (item.Kind == ShopItemKind.Weapon) Txt("PURCHASES CHANGE THE EXISTING MELEE WEAPON AND DAMAGE.", right + 202, y + 294, 12, new Color(202, 192, 216), 1);
                else Txt("COSMETIC AURA VARIANT FOR THE EXISTING SUSANOO FORM.", right + 202, y + 294, 12, new Color(202, 192, 216), 1);
            }
            bool canBuy = chosen.Kind == ShopMenuEntryKind.Summon ? !summoningService.IsOwned(chosen.Id) : chosen.Kind == ShopMenuEntryKind.ShopItem && !shopService.IsOwned(chosen.Id);
            bool canEquip = chosen.Kind == ShopMenuEntryKind.StarterWeapon || chosen.Kind == ShopMenuEntryKind.Summon && summoningService.IsOwned(chosen.Id) || chosen.Kind == ShopMenuEntryKind.ShopItem && shopService.IsOwned(chosen.Id);
            bool equippedChoice = chosen.Kind == ShopMenuEntryKind.StarterWeapon ? string.IsNullOrEmpty(progression.Profile.EquippedWeaponId) : chosen.Kind == ShopMenuEntryKind.Summon ? progression.Profile.EquippedSummonId == chosen.Id : chosen.ShopItem != null && shopService.IsEquipped(chosen.Id);
            DrawActionButton(672, 590, 170, canBuy ? "B  BUY" : chosen.Kind == ShopMenuEntryKind.Ability ? "GAMEPLAY UNLOCK" : "OWNED", canBuy ? new Color(40, 30, 58) : new Color(31, 31, 39));
            DrawActionButton(850, 590, 170, canEquip ? equippedChoice ? "ENTER  UNEQUIP" : "ENTER  EQUIP" : "LOCKED", canEquip ? new Color(31, 42, 51) : new Color(31, 31, 39));
            DrawActionButton(1028, 590, 176, chosen.Kind == ShopMenuEntryKind.Summon ? "G  SUMMON" : "TAB  BACK", new Color(53, 27, 39));
            Txt("LEFT / RIGHT CATEGORY   ·   UP / DOWN ITEM   ·   TAB / ESC BACK", 83, y + h - 20, 10, new Color(163, 154, 180));
        }

        string ShopEntryStatus(ShopMenuEntry entry)
        {
            if (entry.Kind == ShopMenuEntryKind.Ability)
            { AbilityMenuRow row = FindAbilityRow(entry.Id); return row.IsUnlocked ? "READY" : "PROGRESSION"; }
            if (entry.Kind == ShopMenuEntryKind.StarterWeapon) return string.IsNullOrEmpty(progression.Profile.EquippedWeaponId) ? "EQUIPPED" : "STARTER";
            if (entry.Kind == ShopMenuEntryKind.Summon)
            { bool owned = summoningService.IsOwned(entry.Id); return progression.Profile.EquippedSummonId == entry.Id ? "EQUIPPED" : owned ? "OWNED" : entry.Summon.DiamondPrice + " DIAMONDS"; }
            bool isOwned = shopService.IsOwned(entry.Id);
            return shopService.IsEquipped(entry.Id) ? "EQUIPPED" : isOwned ? "OWNED" : entry.ShopItem.Price + " " + entry.ShopItem.Currency.ToUpperInvariant();
        }

        void DrawActionButton(float x, float y, float width, string label, Color color)
        {
            Point mouse = MenuVirtualMouse();
            bool hover = mouse.X >= x && mouse.X <= x + width && mouse.Y >= y && mouse.Y <= y + 44;
            Box(x + width / 2, y + 21, width, 42, 0, hover ? new Color(57, 35, 73, 250) : color);
            Box(x + width / 2, y + 2, width, 2, 0, hover ? CY : Color.White * .2f);
            Txt(label, x + width / 2, y + 11, 12, hover ? WH : Color.LightGray, 1);
        }

        void DrawUpgradeScreen()
        {
            float x = 48, y = 42, w = 1184, h = 636;
            Box(x + w / 2, y + h / 2, w, h, 0, new Color(3, 4, 10, 246));
            Box(x + w / 2, y + 2, w, 2, 0, Ad(VI, .9f));
            Txt("UPGRADE  /  PERMANENT FIELD ENHANCEMENTS", x + 28, y + 17, 27, WH);
            Txt("SHARDS " + CurrencyBalance("shards") + "  ·  EFFECTS ARE SAVED TO YOUR PROFILE", x + w - 24, y + 28, 12, CY, 2);
            string[] tabs = { "ABILITIES", "SUMMONS", "SUSANOO" };
            for (int i = 0; i < tabs.Length; i++)
            {
                float tabX = 85 + i * 365;
                bool active = selectedUpgradeCategory == i;
                Box(tabX + 170, y + 92, 340, 40, 0, active ? new Color(47, 30, 62, 245) : new Color(12, 13, 22, 245));
                Box(tabX + 170, y + 73, 340, 2, 0, active ? CY : Color.White * .12f);
                Txt(tabs[i], tabX + 170, y + 81, 14, active ? WH : Color.Gray, 1);
            }
            Txt("UP / DOWN SELECT  ·  ENTER OR U UPGRADE  ·  ESC BACK", 84, y + 125, 11, new Color(180, 171, 197));
            int count = UpgradeEntryCount;
            if (selectedUpgradeCategory < 2)
            {
                int first = Math.Max(0, Math.Min(count - 8, selectedUpgradeIndex - 4));
                for (int row = 0; row < 8; row++)
                {
                    int index = first + row;
                    if (index >= count) break;
                    float rowY = y + 157 + row * 45;
                    string name, status;
                    bool allowed;
                    if (selectedUpgradeCategory == 0)
                    {
                        AbilityDefinition ability = AbilityCatalog.All[index];
                        int level = progression.GetUpgradeLevel(ability.Id);
                        bool unlocked = progression.IsUnlocked(ability.Id);
                        name = ability.Name;
                        status = !unlocked ? "LOCKED  ·  " + AbilityMenuModel.Build(progression)[index].RequirementText : level >= ability.MaximumUpgradeLevel ? "MAX LEVEL  " + level : "LEVEL " + level + " / " + ability.MaximumUpgradeLevel + "  ·  " + progression.GetUpgradeShardCost(ability.Id) + " SHARDS";
                        allowed = unlocked && level < ability.MaximumUpgradeLevel;
                    }
                    else
                    {
                        SummoningDefinition summon = SummoningCatalog.All[index];
                        int level = summoningService.GetUpgradeLevel(summon.Id);
                        bool owned = summoningService.IsOwned(summon.Id);
                        name = summon.Name;
                        status = !owned ? "UNLOCK IN SHOP  ·  " + summon.DiamondPrice + " DIAMONDS : " + level + "/" + summon.MaximumUpgradeLevel : level >= summon.MaximumUpgradeLevel ? "MAX LEVEL  " + level : "LEVEL " + level + " / " + summon.MaximumUpgradeLevel + "  ·  " + summoningService.GetUpgradeShardCost(summon.Id) + " SHARDS";
                        allowed = owned && level < summon.MaximumUpgradeLevel;
                    }
                    bool selected = index == selectedUpgradeIndex;
                    if (selected) Box(451, rowY + 18, 740, 38, 0, new Color(43, 26, 58, 244));
                    Box(86, rowY + 39, 740, 1, 0, Color.White * .1f);
                    Txt(name, 102, rowY + 3, 14, selected ? WH : Color.LightGray);
                    Txt(status, 1156, rowY + 6, 11, allowed ? CY : OR, 2);
                }
                if (selectedUpgradeCategory == 0)
                {
                    AbilityDefinition selected = AbilityCatalog.All[selectedUpgradeIndex];
                    int level = progression.GetUpgradeLevel(selected.Id);
                    Txt(selected.Description + "  ·  EFFECT: LEVELS IMPROVE DAMAGE, DURATION OR COOLDOWN WHERE USED.", 86, 537, 11, Color.LightGray);
                    Txt("MAX LEVEL " + selected.MaximumUpgradeLevel + (level < selected.MaximumUpgradeLevel ? "   ·   NEXT COST " + progression.GetUpgradeShardCost(selected.Id) + " SHARDS" : "   ·   MAXIMUM REACHED"), 86, 559, 12, VI);
                }
                else
                {
                    SummoningDefinition selected = SummoningCatalog.All[selectedUpgradeIndex];
                    SummonUpgradeStats stats = summoningService.GetStats(selected.Id);
                    Txt(selected.Description, 86, 537, 11, Color.LightGray);
                    Txt("LEVEL " + stats.Level + "  ·  HP x" + stats.HealthMultiplier.ToString("0.00") + "  ·  DAMAGE x" + stats.DamageMultiplier.ToString("0.00") + "  ·  TIME x" + stats.DurationMultiplier.ToString("0.00"), 86, 559, 12, VI);
                }
                DrawActionButton(872, 590, 332, selectedUpgradeCategory == 0 && progression.IsUnlocked(AbilityCatalog.All[selectedUpgradeIndex].Id) && progression.GetUpgradeLevel(AbilityCatalog.All[selectedUpgradeIndex].Id) < AbilityCatalog.All[selectedUpgradeIndex].MaximumUpgradeLevel || selectedUpgradeCategory == 1 && summoningService.IsOwned(SummoningCatalog.All[selectedUpgradeIndex].Id) && summoningService.GetUpgradeLevel(SummoningCatalog.All[selectedUpgradeIndex].Id) < SummoningCatalog.All[selectedUpgradeIndex].MaximumUpgradeLevel ? "U / ENTER  APPLY UPGRADE" : "LOCKED OR MAX LEVEL", new Color(40, 30, 58));
            }
            else
            {
                int level = shopService.GetSusanooLevel();
                bool ownsEms = shopService.IsOwned("eye.ems");
                Txt("SUSANOO DURATION", W / 2, y + 200, 20, VI, 1);
                Glow(W / 2, y + 290, 120, Ad(VI, .45f));
                Circ(W / 2, y + 287, 47, new Color(40, 28, 66));
                Tri(W / 2, y + 260, 78, 74, -.15f, Ad(VI, .9f));
                Txt(shopService.GetSusanooDuration().ToString("0") + " / 12 SEC", W / 2, y + 356, 27, WH, 1);
                Txt("LEVEL " + level + " / 9  ·  EACH UPGRADE ADDS ONE SECOND", W / 2, y + 391, 14, Color.LightGray, 1);
                Txt(!ownsEms ? "EQUIPABLE AFTER UNLOCKING ETERNAL MANGEKYO SHARINGAN IN THE SHOP" : level >= 9 ? "MAXIMUM DURATION REACHED" : "NEXT UPGRADE COST  " + shopService.GetSusanooUpgradeCost() + " SHARDS", W / 2, y + 430, 12, ownsEms ? CY : OR, 1);
                DrawActionButton(872, 590, 332, ownsEms && level < 9 ? "U / ENTER  APPLY UPGRADE" : "LOCKED OR MAX LEVEL", new Color(40, 30, 58));
            }
        }

        void DrawSettingsScreen()
        {
            float x = 48, y = 42, w = 1184, h = 636;
            Box(x + w / 2, y + h / 2, w, h, 0, new Color(3, 4, 10, 246));
            Box(x + w / 2, y + 2, w, 2, 0, Ad(CY, .85f));
            Txt("SETTINGS  /  SAVED AUTOMATICALLY", x + 28, y + 17, 27, WH);
            string[] sections = { "SOUND", "HUD", "CONTROLS", "GRAPHICS", "CAMERA" };
            for (int i = 0; i < sections.Length; i++)
            {
                float tabX = 74 + i * 226;
                bool active = i == settingsSection;
                Box(tabX + 104, y + 92, 208, 40, 0, active ? new Color(34, 36, 55, 245) : new Color(12, 13, 22, 245));
                Box(tabX + 104, y + 73, 208, 2, 0, active ? CY : Color.White * .12f);
                Txt(sections[i], tabX + 104, y + 81, 13, active ? WH : Color.Gray, 1);
            }
            Txt("PAGE UP / DOWN CATEGORIES  ·  LEFT / RIGHT CHANGE  ·  ENTER TO TOGGLE OR REBIND  ·  ESC BACK", 80, y + 128, 10, new Color(180, 171, 197));
            int visible = 8;
            int first = Math.Max(0, Math.Min(SettingsRowCount - visible, settingsRow - visible / 2));
            for (int row = 0; row < visible; row++)
            {
                int index = first + row;
                if (index >= SettingsRowCount) break;
                float rowY = y + 158 + row * 48;
                string label = SettingsLabel(index), value = SettingsValue(index);
                bool selected = index == settingsRow;
                if (selected) Box(W / 2, rowY + 19, w - 64, 40, 0, new Color(35, 28, 48, 241));
                Box(80, rowY + 41, w - 160, 1, 0, Color.White * .1f);
                Txt(label, 98, rowY + 6, 15, selected ? WH : Color.LightGray);
                if (settingsSection == 0 && index < 3)
                {
                    float amount = index == 0 ? settings.MasterVolume : index == 1 ? settings.MusicVolume : settings.EffectsVolume;
                    float sx = 680, sw = 275;
                    Box(sx + sw / 2, rowY + 20, sw, 7, 0, new Color(36, 33, 47));
                    Box(sx + sw * amount / 2, rowY + 20, sw * amount, 7, 0, index == 1 ? VI : CY);
                    Circ(sx + sw * amount, rowY + 20, 9, WH);
                    Txt(value, 1158, rowY + 8, 14, CY, 2);
                }
                else Txt(value, 1158, rowY + 7, 14, selected ? CY : new Color(165, 158, 180), 2);
            }
            if (capturingSettingBinding)
                Txt("PRESS A KEY FOR  " + SettingActionLabel(capturingSettingAction).ToUpperInvariant() + "   ·   ESC CANCELS", W / 2, 601, 14, OR, 1);
            else if (settingsSection == 2)
                Txt("MOUSE  LMB MELEE  ·  RMB RASENGAN     F6 QUALITY  ·  F7 CAMERA RESET  ·  F11 FULLSCREEN", W / 2, 601, 10, Color.LightGray, 1);
            else Txt("CHANGES TAKE EFFECT IMMEDIATELY AND STAY SEPARATE FROM STORY PROGRESSION.", W / 2, 601, 11, Color.LightGray, 1);
            DrawActionButton(1004, 626, 192, settingsSection == 2 ? "RESTORE DEFAULTS" : "ESC  BACK", new Color(35, 27, 43));
        }

        string SettingsLabel(int index)
        {
            if (settingsSection == 0) return new[] { "Master Volume", "Music Volume", "Sound Effects Volume", "Mute Audio" }[index];
            if (settingsSection == 1) return new[] { "Show HUD", "Health / Energy", "Currency Wallet", "Gameplay Hints", "HUD Scale", "Horizontal Offset", "Vertical Offset" }[index];
            if (settingsSection == 2) return index < RebindableLabels.Length ? RebindableLabels[index] : "Restore Default Key Binds";
            if (settingsSection == 3) return new[] { "Resolution", "Fullscreen", "VSync", "Render Quality" }[index];
            return "Camera Sensitivity";
        }

        string SettingsValue(int index)
        {
            if (settingsSection == 0)
            {
                if (index == 0) return (int)(settings.MasterVolume * 100) + "%";
                if (index == 1) return (int)(settings.MusicVolume * 100) + "%";
                if (index == 2) return (int)(settings.EffectsVolume * 100) + "%";
                return settings.Muted ? "MUTED" : "ON";
            }
            if (settingsSection == 1)
            {
                if (index == 0) return settings.ShowHud ? "ON" : "OFF";
                if (index == 1) return settings.ShowVitals ? "ON" : "OFF";
                if (index == 2) return settings.ShowWallet ? "ON" : "OFF";
                if (index == 3) return settings.ShowHints ? "ON" : "OFF";
                if (index == 4) return settings.HudScale.ToString("0.00") + "x";
                if (index == 5) return settings.HudOffsetX.ToString("+0;-0;0");
                return settings.HudOffsetY.ToString("+0;-0;0");
            }
            if (settingsSection == 2) return index < RebindableActions.Length ? settings.GetBinding(RebindableActions[index]) : "ENTER TO RESTORE";
            if (settingsSection == 3)
            {
                if (index == 0) return ResolutionWidths[settings.ResolutionIndex] + " x " + ResolutionHeights[settings.ResolutionIndex];
                if (index == 1) return settings.Fullscreen ? "FULLSCREEN" : "WINDOWED";
                if (index == 2) return settings.VSync ? "ON" : "OFF";
                return new[] { "100%", "125%", "150%" }[settings.GraphicsQuality];
            }
            return settings.CameraSensitivity.ToString("0.0") + "x";
        }

        string SettingActionLabel(string action)
        {
            for (int i = 0; i < RebindableActions.Length; i++) if (RebindableActions[i] == action) return RebindableLabels[i];
            return action;
        }
    }
}
