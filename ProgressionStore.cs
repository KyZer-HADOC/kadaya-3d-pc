using System;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Kadaya
{
    public static class ProgressionStore
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public static ProgressionProfile Load(string profilePath, string legacyPath)
        {
            if (!string.IsNullOrWhiteSpace(profilePath) && File.Exists(profilePath))
            {
                try
                {
                    string json = File.ReadAllText(profilePath);
                    ProgressionProfile saved = JsonSerializer.Deserialize<ProgressionProfile>(json, jsonOptions);
                    return saved ?? CreateReadOnlyRecoveryProfile();
                }
                catch
                {
                    // A present JSON profile takes precedence over legacy data, even when it is invalid.
                    // Keep the damaged source in place and prevent the recovered defaults from overwriting it.
                    return CreateReadOnlyRecoveryProfile();
                }
            }

            if (string.IsNullOrWhiteSpace(profilePath) || string.IsNullOrWhiteSpace(legacyPath) || !File.Exists(legacyPath))
                return new ProgressionProfile();

            try
            {
                string legacyText = File.ReadAllText(legacyPath);
                int legacyLevel;
                if (!int.TryParse(legacyText, NumberStyles.Integer, CultureInfo.InvariantCulture, out legacyLevel))
                    return new ProgressionProfile();

                int clampedLevel = Math.Max(0, Math.Min(2, legacyLevel));
                var migrated = new ProgressionProfile
                {
                    UnlockedLevel = clampedLevel,
                    CompletedMissions = clampedLevel
                };
                Save(migrated, profilePath);
                return migrated;
            }
            catch
            {
                return new ProgressionProfile();
            }
        }

        public static void Save(ProgressionProfile profile, string profilePath)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.IsReadOnlyRecovery)
                throw new InvalidOperationException("This profile was recovered from an unreadable save and cannot overwrite its source file.");
            if (string.IsNullOrWhiteSpace(profilePath)) throw new ArgumentException("A profile path is required.", nameof(profilePath));

            string fullPath = Path.GetFullPath(profilePath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string json = JsonSerializer.Serialize(profile, jsonOptions);
            File.WriteAllText(fullPath, json);
        }

        private static ProgressionProfile CreateReadOnlyRecoveryProfile()
        {
            return new ProgressionProfile { IsReadOnlyRecovery = true };
        }

        public static string GetNormalProfilePath(string appDataRoot)
        {
            return Path.Combine(appDataRoot ?? string.Empty, "Kadaya", "save.json");
        }

        public static string GetTestProfilePath(string appDataRoot)
        {
            return Path.Combine(appDataRoot ?? string.Empty, "Kadaya", "test-save.json");
        }

        public static string GetLegacySavePath(string appDataRoot)
        {
            return Path.Combine(appDataRoot ?? string.Empty, "Kadaya", "save.txt");
        }
    }
}
