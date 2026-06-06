#if DEBUG
using System.Text.Json;
using MelonLoader;

namespace Drova_Modding_API.Systems.Editor
{
    /// <summary>
    /// Stores the shared mod file stem used by debug-only custom creation tools.
    /// </summary>
    internal static class ModCreationContext
    {
        private const string ContextFileName = "mod_authoring_context.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private static bool _loaded;
        private static string _currentModFileStem = string.Empty;

        private sealed class ModCreationContextData
        {
            public int Version { get; set; } = 1;

            public string CurrentModFileStem { get; set; } = string.Empty;
        }

        public static bool HasConfiguredModFileStem()
            => !string.IsNullOrWhiteSpace(GetCurrentModFileStem());

        public static string GetCurrentModFileStem()
        {
            EnsureLoaded();
            return _currentModFileStem;
        }

        public static string GetCurrentModFileStemOrDefault(string fallback)
        {
            string current = GetCurrentModFileStem();
            return string.IsNullOrWhiteSpace(current) ? fallback : current;
        }

        public static void SetCurrentModFileStem(string rawValue)
        {
            EnsureLoaded();
            string sanitized = SanitizeFileStem(rawValue);
            if (string.Equals(_currentModFileStem, sanitized, StringComparison.Ordinal))
                return;

            _currentModFileStem = sanitized;
            Persist();
        }

        public static string SanitizeFileStem(string rawValue)
        {
            string sanitized = rawValue.Trim();
            if (sanitized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                sanitized = Path.GetFileNameWithoutExtension(sanitized);

            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
                sanitized = sanitized.Replace(invalidChars[i], '_');

            return sanitized;
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;
            string filePath = GetContextFilePath();
            try
            {
                if (!File.Exists(filePath))
                    return;

                string raw = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(raw))
                    return;

                ModCreationContextData? data = JsonSerializer.Deserialize<ModCreationContextData>(raw, JsonOptions);
                if (data == null)
                    return;

                _currentModFileStem = SanitizeFileStem(data.CurrentModFileStem);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"ModCreationContext: failed to read context file '{filePath}': {ex.Message}");
            }
        }

        private static void Persist()
        {
            string filePath = GetContextFilePath();
            try
            {
                string? parent = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(parent) && !Directory.Exists(parent))
                    Directory.CreateDirectory(parent);

                ModCreationContextData data = new()
                {
                    CurrentModFileStem = _currentModFileStem
                };

                string json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"ModCreationContext: failed to persist context file '{filePath}': {ex.Message}");
            }
        }

        private static string GetContextFilePath()
            => Path.Combine(Utils.SavePath, ContextFileName);
    }
}
#endif


