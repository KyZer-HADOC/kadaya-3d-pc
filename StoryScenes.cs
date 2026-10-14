using System;
using System.Collections.Generic;

namespace Kadaya
{
    public sealed class DialogueLine
    {
        public string Speaker { get; private set; }
        public string Text { get; private set; }
        internal DialogueLine(string speaker, string text) { Speaker = speaker; Text = text; }
    }

    public sealed class StoryScene
    {
        public string Id { get; private set; }
        public string Title { get; private set; }
        public IReadOnlyList<DialogueLine> Lines { get; private set; }
        internal StoryScene(string id, string title, params DialogueLine[] lines) { Id = id; Title = title; Lines = lines; }
    }

    // Story beats for the campaign. Text is plain ASCII so it renders with the existing sprite font.
    public static class StoryCatalog
    {
        public const string Kadaya = "KADAYA", Krayoo = "KRAYOO", Raizen = "RAIZEN", Narrator = "";

        static DialogueLine D(string who, string text) { return new DialogueLine(who, text); }

        public static readonly StoryScene Prologue = new StoryScene("prologue", "PROLOGUE - THE LAST PEACEFUL NIGHT",
            D(Narrator, "Under a quiet moon, the village of Kadaya's family sleeps. The border between the living and the Shadow Realm has never felt so thin."),
            D(Krayoo, "Kadaya, you are staring at the sky again. The elders say a red moon means nothing."),
            D(Kadaya, "The elders also said the old scythe was only a story. It has been humming since sunset, Krayoo."),
            D(Narrator, "The sky tears open. Shadows pour through the woods and the village burns."),
            D(Raizen, "So the last of the old lineage still draws breath. Give me the Shadow Realm's key, boy, and I will let your friends live."),
            D(Kadaya, "The scythe... it knows my name. Krayoo, run! I will hold them in the forest."),
            D(Krayoo, "Not alone. Go forward, I will find a way around and catch up. Do not die before I get there."),
            D(Narrator, "Controls: move with the movement keys, jump with Space, fight with the mouse. Press Esc at any time to pause."));

        public static readonly StoryScene ForestChapterMid = new StoryScene("forest-mid", "CHAPTER ONE - BLOOD MOON FOREST",
            D(Krayoo, "Kadaya! These shrines are marked with Raizen's sigil. He has been feeding them for years."),
            D(Kadaya, "Then every shrine we break weakens him. Stay behind me, Krayoo."),
            D(Krayoo, "I am no use behind anyone. But I will watch your back. Move!"));

        public static readonly StoryScene ForestBoss = new StoryScene("forest-boss", "THE FIRST RECKONING",
            D(Raizen, "The forest is mine. Meet the Warlord I bound to its roots."),
            D(Kadaya, "Every champion you send only teaches me what the scythe can do."),
            D(Raizen, "Then learn quickly. The dead below are waiting."));

        public static readonly StoryScene GraveyardChapter = new StoryScene("graveyard-intro", "CHAPTER TWO - HAUNTED GRAVEYARD",
            D(Narrator, "Beyond the forest, a burial ground older than any kingdom. The graves here are open from the inside."),
            D(Krayoo, "These are warriors of your lineage, Kadaya. Raizen is waking them to guard the way."),
            D(Kadaya, "Using the scythe's power here feels like drowning. Every strike pulls me closer to the dark."),
            D(Krayoo, "Then do not fight alone. Remember who you are when it grows quiet.")); 

        public static readonly StoryScene GraveyardMid = new StoryScene("graveyard-mid", "THE COST OF POWER",
            D(Kadaya, "My eyes keep turning violet when I swing. I cannot tell where the scythe ends and I begin."),
            D(Krayoo, "That is why I am here. If you lose yourself, I will drag you back. Even if I have to hit you."));

        public static readonly StoryScene GraveyardBoss = new StoryScene("graveyard-boss", "THE CURSED SOVEREIGN",
            D(Raizen, "You wear your ancestors' power like a borrowed coat. See what became of the last wielder who trusted it."),
            D(Kadaya, "I am not him."),
            D(Raizen, "No. He fell faster."));

        public static readonly StoryScene TempleChapter = new StoryScene("temple-intro", "CHAPTER THREE - BURNING TEMPLE",
            D(Narrator, "The temple where the lineage swore its oath is on fire, and the flames burn cold blue where the Shadow Realm leaks through."),
            D(Krayoo, "The carvings on these walls show your scythe, Kadaya. The first wielder sealed something here."),
            D(Kadaya, "Raizen does not want to destroy the seal. He wants to open it."));

        public static readonly StoryScene TempleMid = new StoryScene("temple-mid", "ECHOES OF THE OATH",
            D(Kadaya, "The oath says the wielder must become the seal. That is why the scythe chose me."),
            D(Krayoo, "Then we find another way. We always do."));

        public static readonly StoryScene TempleBoss = new StoryScene("temple-boss", "THE LAST SHADOW",
            D(Raizen, "My strongest vessel guards the inner altar. Break it and you finish my work for me."),
            D(Kadaya, "Then I will break it and finish yours instead."),
            D(Krayoo, "Kadaya! Whatever happens at the Soul Gate, you are not doing it alone."));

        public static readonly StoryScene Ending = new StoryScene("ending", "EPILOGUE",
            D(Narrator, "The Fire Shogun falls and the flames at the altar go quiet."),
            D(Krayoo, "It is over. Kadaya, you are shaking."),
            D(Kadaya, "The scythe is calm. For the first time it is calm."),
            D(Raizen, "A vessel is only the first lock. The Shadow Realm still has my name, boy. We will meet where the moon never rises."),
            D(Narrator, "Raizen's final confrontation is still ahead. The road of KADAYA: THE LAST SHADOW continues."));

        // Returns the story beat that plays before a mission, or null when the mission has none.
        public static StoryScene ForMissionStart(MissionDefinition mission)
        {
            if (mission == null) return null;
            switch (mission.LevelNumber)
            {
                case 1: return mission.MapIndex == 0 ? Prologue : mission.MapIndex == 1 ? GraveyardChapter : TempleChapter;
                case 8: return mission.MapIndex == 0 ? ForestChapterMid : mission.MapIndex == 1 ? GraveyardMid : TempleMid;
                case 15: return mission.MapIndex == 0 ? ForestBoss : mission.MapIndex == 1 ? GraveyardBoss : TempleBoss;
                default: return null;
            }
        }

        public static IEnumerable<StoryScene> All
        {
            get
            {
                yield return Prologue; yield return ForestChapterMid; yield return ForestBoss;
                yield return GraveyardChapter; yield return GraveyardMid; yield return GraveyardBoss;
                yield return TempleChapter; yield return TempleMid; yield return TempleBoss; yield return Ending;
            }
        }
    }

    // Typewriter-style playback of one scene. Pure logic so it can be unit tested without a window.
    public sealed class DialogueRunner
    {
        public const float CharactersPerSecond = 55f;

        readonly StoryScene scene;
        float revealed;

        public DialogueRunner(StoryScene scene)
        {
            if (scene == null || scene.Lines.Count == 0) throw new ArgumentException("A story scene needs at least one line.");
            this.scene = scene;
        }

        public StoryScene Scene { get { return scene; } }
        public int LineIndex { get; private set; }
        public bool Finished { get; private set; }
        public DialogueLine Current { get { return scene.Lines[Math.Min(LineIndex, scene.Lines.Count - 1)]; } }
        public bool LineComplete { get { return revealed >= Current.Text.Length; } }

        public string VisibleText
        {
            get { int n = Math.Max(0, Math.Min(Current.Text.Length, (int)revealed)); return Current.Text.Substring(0, n); }
        }

        public void Update(float dt)
        {
            if (Finished || dt <= 0) return;
            revealed = Math.Min(Current.Text.Length, revealed + dt * CharactersPerSecond);
        }

        // First press completes the current line, the next press moves on.
        public void Advance()
        {
            if (Finished) return;
            if (!LineComplete) { revealed = Current.Text.Length; return; }
            LineIndex++; revealed = 0;
            if (LineIndex >= scene.Lines.Count) { LineIndex = scene.Lines.Count - 1; Finished = true; }
        }

        public void Skip() { Finished = true; }
    }
}
