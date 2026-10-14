using System;

namespace Kadaya
{
    public enum PauseAction { None, Resume, RestartMission, MissionSelect, MainMenu }

    // Pause menu selection logic. Actions that discard mission progress need a second confirmation.
    public sealed class PauseMenuModel
    {
        static readonly PauseAction[] actions = { PauseAction.Resume, PauseAction.RestartMission, PauseAction.MissionSelect, PauseAction.MainMenu };
        static readonly string[] labels = { "RESUME", "RESTART MISSION", "MISSION SELECT", "MAIN MENU" };

        public int Count { get { return actions.Length; } }
        public int Index { get; private set; }
        public PauseAction Pending { get; private set; }

        public string Label(int index) { return labels[index]; }
        public PauseAction ActionAt(int index) { return actions[index]; }

        public void Reset() { Index = 0; Pending = PauseAction.None; }

        public void Move(int delta)
        {
            Index = ((Index + delta) % actions.Length + actions.Length) % actions.Length;
            Pending = PauseAction.None;
        }

        public void Select(int index)
        {
            if (index < 0 || index >= actions.Length || index == Index) return;
            Index = index; Pending = PauseAction.None;
        }

        // Returns the action to carry out now, or None when a confirmation is still required.
        public PauseAction Confirm()
        {
            PauseAction action = actions[Index];
            if (action == PauseAction.Resume) { Pending = PauseAction.None; return action; }
            if (Pending == action) { Pending = PauseAction.None; return action; }
            Pending = action;
            return PauseAction.None;
        }

        public void CancelPending() { Pending = PauseAction.None; }
    }
}
