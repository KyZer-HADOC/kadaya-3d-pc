using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Kadaya
{
    public sealed class GameSettings
    {
        private static readonly Dictionary<string, string> defaultBindings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MoveForward"] = "W", ["MoveLeft"] = "A", ["MoveBack"] = "S", ["MoveRight"] = "D",
            ["Jump"] = "Space", ["Dodge"] = "LeftShift", ["EyePrimary"] = "Q", ["PowerSecondary"] = "E",
            ["ModeOrKenjutsu"] = "F", ["HiddenJutsu"] = "C", ["Kunai"] = "R", ["SageMode"] = "Z",
            ["Shop"] = "P", ["Summons"] = "M", ["Abilities"] = "Tab", ["SummonCall"] = "G", ["SummonDismiss"] = "X",
            ["MenuUp"] = "Up", ["MenuDown"] = "Down", ["MenuLeft"] = "Left", ["MenuRight"] = "Right",
            ["Confirm"] = "Enter", ["Back"] = "Escape", ["Buy"] = "B", ["Upgrade"] = "U"
        };

        private static readonly HashSet<string> validKeys = BuildValidKeySet();
        private static readonly HashSet<string> reservedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "Escape", "F6", "F7", "F9", "F11" };

        public float MasterVolume { get; set; } = 1f;
        public float MusicVolume { get; set; } = .7f;
        public float EffectsVolume { get; set; } = .8f;
        public bool Muted { get; set; }
        public bool ShowHud { get; set; } = true;
        public bool ShowVitals { get; set; } = true;
        public bool ShowWallet { get; set; } = true;
        public bool ShowHints { get; set; } = true;
        public float HudScale { get; set; } = 1f;
        public int HudOffsetX { get; set; }
        public int HudOffsetY { get; set; }
        public float CameraSensitivity { get; set; } = 1f;
        public int ResolutionIndex { get; set; }
        public bool Fullscreen { get; set; }
        public bool VSync { get; set; } = true;
        public int GraphicsQuality { get; set; } = 3;
        public Dictionary<string, string> KeyBindings { get; set; } = CreateDefaultBindings();

        public static IReadOnlyDictionary<string, string> Defaults { get { return defaultBindings; } }

        public string GetBinding(string action)
        {
            string key;
            return KeyBindings != null && KeyBindings.TryGetValue(action ?? string.Empty, out key) ? key :
                defaultBindings.TryGetValue(action ?? string.Empty, out key) ? key : string.Empty;
        }

        public bool TrySetBinding(string action, string key)
        {
            if (!defaultBindings.ContainsKey(action ?? string.Empty) || !IsValidKey(key) || reservedKeys.Contains(key)) return false;
            if (KeyBindings == null) KeyBindings = CreateDefaultBindings();
            foreach (KeyValuePair<string, string> binding in KeyBindings)
                if (binding.Key != action && string.Equals(binding.Value, key, StringComparison.OrdinalIgnoreCase)) return false;
            KeyBindings[action] = key;
            return true;
        }

        public void RestoreDefaults()
        {
            KeyBindings = CreateDefaultBindings();
        }

        public void Normalize()
        {
            MasterVolume = Clamp(MasterVolume, 0, 1);
            MusicVolume = Clamp(MusicVolume, 0, 1);
            EffectsVolume = Clamp(EffectsVolume, 0, 1);
            HudScale = Clamp(HudScale, .65f, 1.4f);
            HudOffsetX = Math.Max(-180, Math.Min(180, HudOffsetX));
            HudOffsetY = Math.Max(-120, Math.Min(120, HudOffsetY));
            CameraSensitivity = Clamp(CameraSensitivity, .25f, 2.5f);
            ResolutionIndex = Math.Max(0, Math.Min(2, ResolutionIndex));
            GraphicsQuality = Math.Max(0, Math.Min(3, GraphicsQuality));

            Dictionary<string, string> saved = KeyBindings;
            KeyBindings = CreateDefaultBindings();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> defaultBinding in defaultBindings)
            {
                string candidate = defaultBinding.Value;
                string savedCandidate;
                if (saved != null && saved.TryGetValue(defaultBinding.Key, out savedCandidate) && IsValidKey(savedCandidate) &&
                    !used.Contains(savedCandidate) && (!reservedKeys.Contains(savedCandidate) || defaultBinding.Key == "Back"))
                    candidate = savedCandidate;
                else if (used.Contains(candidate))
                {
                    candidate = string.Empty;
                    foreach (string fallback in validKeys)
                        if (!used.Contains(fallback) && !reservedKeys.Contains(fallback)) { candidate = fallback; break; }
                    if (candidate.Length == 0) candidate = defaultBinding.Value;
                }
                KeyBindings[defaultBinding.Key] = candidate;
                used.Add(candidate);
            }
        }

        public static bool IsValidKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && validKeys.Contains(key);
        }

        private static Dictionary<string, string> CreateDefaultBindings()
        {
            return new Dictionary<string, string>(defaultBindings, StringComparer.Ordinal);
        }

        private static HashSet<string> BuildValidKeySet()
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Space", "Enter", "Escape", "Tab", "LeftShift", "RightShift", "LeftControl", "RightControl", "LeftAlt", "RightAlt", "Up", "Down", "Left", "Right", "Home", "End", "PageUp", "PageDown", "Insert", "Delete", "Back", "CapsLock" };
            for (char c = 'A'; c <= 'Z'; c++) keys.Add(c.ToString());
            for (int i = 0; i <= 9; i++) { keys.Add("D" + i); keys.Add("NumPad" + i); }
            for (int i = 1; i <= 12; i++) keys.Add("F" + i);
            return keys;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return min;
            return Math.Max(min, Math.Min(max, value));
        }
    }

    public static class GameSettingsStore
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

        public static string GetPath(string appDataRoot)
        {
            return Path.Combine(appDataRoot ?? string.Empty, "Kadaya", "settings.json");
        }

        public static GameSettings Load(string path)
        {
            GameSettings settings = null;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try { settings = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(path), jsonOptions); }
                catch { }
            }
            if (settings == null) settings = new GameSettings();
            settings.Normalize();
            return settings;
        }

        public static void Save(GameSettings settings, string path)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A settings path is required.", nameof(path));
            settings.Normalize();
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporaryPath = fullPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, jsonOptions));
            File.Move(temporaryPath, fullPath, true);
        }
    }
}
