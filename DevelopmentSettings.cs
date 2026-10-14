using System;
using System.IO;
using System.Text.Json;

namespace Kadaya
{
    public static class DevelopmentSettings
    {
        public static string GetPath(string appDataRoot)
        {
            return Path.Combine(appDataRoot ?? string.Empty, "Kadaya", "development.json");
        }

        public static bool Load(string settingsPath)
        {
            if (string.IsNullOrWhiteSpace(settingsPath) || !File.Exists(settingsPath)) return false;
            try
            {
                using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(settingsPath)))
                {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object) return false;
                    JsonElement setting;
                    return root.TryGetProperty("development_test_mode", out setting)
                        && setting.ValueKind == JsonValueKind.True;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
