using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kadaya
{
    // Pause menu and story scenes. The "paused" and "story" states freeze the simulation because
    // UpdatePlay only runs while state == "play".
    public partial class Game1
    {
        readonly PauseMenuModel pauseMenu = new PauseMenuModel();
        readonly HashSet<string> seenStory = new HashSet<string>();
        DialogueRunner dialogue;
        bool storyIsEnding;

        bool LeftClicked { get { return mouseCur.LeftButton == ButtonState.Pressed && mousePrev.LeftButton == ButtonState.Released; } }

        // ---------- pause ----------
        void OpenPause()
        {
            pauseMenu.Reset(); state = "paused";
        }

        static float PauseButtonY(int index) { return 300 + index * 74; }

        void UpdatePause()
        {
            if (PrAction("MenuUp")) pauseMenu.Move(-1);
            if (PrAction("MenuDown")) pauseMenu.Move(1);
            Point m = MenuVirtualMouse(); bool click = LeftClicked, moved = mouseCur.X != mousePrev.X || mouseCur.Y != mousePrev.Y, confirm = false;
            for (int i = 0; i < pauseMenu.Count; i++)
            {
                if (m.X >= W / 2 - 210 && m.X <= W / 2 + 210 && Math.Abs(m.Y - PauseButtonY(i)) <= 29)
                {
                    if (moved || click) pauseMenu.Select(i);
                    if (click) confirm = true;
                }
            }
            if (PrAction("Confirm") || confirm) ApplyPauseAction(pauseMenu.Confirm());
        }

        void ApplyPauseAction(PauseAction action)
        {
            switch (action)
            {
                case PauseAction.Resume:
                    state = "play"; break;
                case PauseAction.RestartMission:
                    {
                        MissionDefinition mission = MissionCatalog.Get(lv);
                        LoadLevel(lv, mission.SpawnX, mission.SpawnZ); state = "play"; break;
                    }
                case PauseAction.MissionSelect:
                    DismissSummon(false); abilityMenuOpen = shopMenuOpen = summoningMenuOpen = false;
                    state = "title"; titlePage = "levels"; levelSelectOpen = true; selectedMenuMap = lv / MissionCatalog.MissionsPerMap; PreviewMenuLevel(lv); break;
                case PauseAction.MainMenu:
                    DismissSummon(false); abilityMenuOpen = shopMenuOpen = summoningMenuOpen = false;
                    state = "title"; titlePage = "main"; levelSelectOpen = false; PreviewMenuLevel(lv); break;
            }
        }

        // ---------- story ----------
        // Plays a scene once per session unless always is set. Returns false when nothing was started.
        bool TryBeginStory(StoryScene scene, bool always = false)
        {
            if (scene == null) return false;
            if (!always && !seenStory.Add(scene.Id)) return false;
            dialogue = new DialogueRunner(scene); storyIsEnding = scene == StoryCatalog.Ending; state = "story";
            return true;
        }

        void UpdateStory(float dt)
        {
            if (dialogue == null) { state = "play"; return; }
            dialogue.Update(dt);
            if (PrAction("Confirm") || Pr(Keys.Space) || LeftClicked) dialogue.Advance();
            if (dialogue.Finished) FinishStory();
        }

        void SkipStory()
        {
            if (dialogue != null) dialogue.Skip();
            FinishStory();
        }

        void FinishStory()
        {
            bool ending = storyIsEnding;
            dialogue = null; storyIsEnding = false;
            if (ending) { state = "end"; winT = 0; } else state = "play";
        }

        void GoToEnd()
        {
            SaveProgression();
            if (!TryBeginStory(StoryCatalog.Ending, true)) { state = "end"; winT = 0; }
        }

        // ---------- drawing ----------
        List<string> WrapText(string text, float size, float maxWidth)
        {
            var lines = new List<string>(); string line = string.Empty;
            foreach (string word in SpriteFontText.ToSupportedGlyphs(text).Split(' '))
            {
                string trial = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && font.MeasureString(trial).X * size / 64f > maxWidth) { lines.Add(line); line = word; }
                else line = trial;
            }
            if (line.Length > 0) lines.Add(line);
            return lines;
        }

        static Color SpeakerColor(string speaker)
        {
            if (speaker == StoryCatalog.Kadaya) return CY;
            if (speaker == StoryCatalog.Krayoo) return OR;
            if (speaker == StoryCatalog.Raizen) return RD;
            return Color.LightGray;
        }

        void DrawStory()
        {
            if (dialogue == null) return;
            DialogueLine current = dialogue.Current; Color accent = SpeakerColor(current.Speaker);
            Box(W / 2, 36, W, 72, 0, Color.Black * .88f);
            Txt(dialogue.Scene.Title, W / 2, 20, 28, OR, 1);
            Box(W / 2, H - 128, W - 90, 210, 0, new Color(8, 4, 14) * .9f);
            Box(W / 2, H - 232, W - 90, 3, 0, Ad(accent, .9f));
            if (current.Speaker.Length > 0) Txt(current.Speaker, 90, H - 222, 32, accent, 0);
            List<string> lines = WrapText(current.Text, 24, W - 220);
            int remaining = dialogue.VisibleText.Length;
            for (int i = 0; i < lines.Count && remaining > 0; i++)
            {
                int take = Math.Min(lines[i].Length, remaining);
                Txt(lines[i].Substring(0, take), 90, H - 178 + i * 34, 24, WH, 0);
                remaining -= lines[i].Length + 1;
            }
            Txt("ESC - SKIP SCENE", 90, H - 44, 16, Color.Gray, 0);
            Txt((dialogue.LineIndex + 1) + " / " + dialogue.Scene.Lines.Count, W - 90, H - 44, 16, Color.Gray, 2);
            if (dialogue.LineComplete && (int)(t * 1.6f) % 2 == 0) Txt("ENTER - CONTINUE", W / 2, H - 52, 20, VI, 1);
        }

        void DrawPause()
        {
            Box(W / 2, H / 2, W, H, 0, new Color(6, 2, 12) * .74f);
            Txt("PAUSED", W / 2, 90, 78, WH, 1, VI);
            if (L != null && L.Mission != null)
                Txt(MissionCatalog.Maps[L.Mission.MapIndex].Name + "  /  " + L.Mission.Name, W / 2, 192, 22, CY, 1);
            for (int i = 0; i < pauseMenu.Count; i++)
            {
                bool selected = i == pauseMenu.Index; float y = PauseButtonY(i);
                Box(W / 2, y, 420, 58, 0, selected ? new Color(70, 20, 60) * .95f : new Color(14, 8, 22) * .9f);
                Box(W / 2, y + 28, 420, 2, 0, selected ? Ad(VI, .95f) : new Color(255, 255, 255, 60));
                bool pending = pauseMenu.Pending != PauseAction.None && pauseMenu.ActionAt(i) == pauseMenu.Pending;
                Txt(pending ? "CONFIRM " + pauseMenu.Label(i) : pauseMenu.Label(i), W / 2, y - 14, 26, selected ? WH : Color.Gray, 1, selected ? VI : (Color?)null);
            }
            Txt(pauseMenu.Pending != PauseAction.None ? "PRESS ENTER AGAIN - PROGRESS IN THIS MISSION WILL BE LOST" : "UP / DOWN - SELECT     ENTER - CONFIRM     ESC - RESUME", W / 2, 612, 18, pauseMenu.Pending != PauseAction.None ? RD : Color.LightGray, 1);
        }
    }
}
