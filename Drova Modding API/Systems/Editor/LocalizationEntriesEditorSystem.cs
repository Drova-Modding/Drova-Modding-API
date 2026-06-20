#if DEBUG
using Drova_Modding_API.Access;
using Il2CppCommandTerminal;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Drova_Modding_API.Systems.Editor
{
    /// <summary>
    /// Debug-only editor for mod localization files under Mods/Modding_API/Localization.
    /// Supports selecting an entry so other tools can autofill LocalizedString path/key fields.
    /// </summary>
    internal static class LocalizationEntriesEditorSystem
    {
        private const string RuntimeObjectName = "ModdingAPI_LocalizationEntriesEditor";
        private const string ToggleCommandName = "loc_editor";
        private const string GlobalSelectionOwner = "__global__";

        private static readonly Dictionary<string, SelectedLocalizationEntry> PendingSelectionsByOwner = new(StringComparer.Ordinal);

        private static bool _initialized;
        private static string _requestedSelectionOwner = string.Empty;
        private static LocalizationEntriesEditorUI? _ui;

        internal static void Initialize()
        {
            if (_initialized)
                return;

            GameObject root = GameObject.Find(RuntimeObjectName);
            if (root == null)
            {
                root = new GameObject(RuntimeObjectName);
                UnityEngine.Object.DontDestroyOnLoad(root);
            }

            _ui = root.GetComponent<LocalizationEntriesEditorUI>();
            if (_ui == null)
                _ui = root.AddComponent<LocalizationEntriesEditorUI>();

            CheatMenuAccess.RegisterCheat(
                ToggleCommandName,
                ToggleCommand,
                0,
                0,
                "Opens or closes the localization entries editor.",
                ToggleCommandName);

            _initialized = true;
        }

        public static void Toggle()
        {
            EnsureInitialized();
            if (_ui == null)
                return;

            _ui.SetVisible(!_ui.IsVisible);
        }

        public static void Show()
        {
            EnsureInitialized();
            _ui?.SetVisible(true);
        }

        public static void RequestSelection(string ownerToken)
        {
            if (string.IsNullOrWhiteSpace(ownerToken))
                return;

            EnsureInitialized();
            _requestedSelectionOwner = ownerToken;
            _ui?.SetVisible(true);
            _ui?.SetStatus($"Select a localization entry for '{ownerToken}'.");
        }

        public static bool TryConsumeSelection(string ownerToken, out string locaPath, out string locaKey)
        {
            locaPath = string.Empty;
            locaKey = string.Empty;

            if (string.IsNullOrWhiteSpace(ownerToken))
                return false;

            if (!TryConsumeSelectionInternal(ownerToken, out locaPath, out locaKey)
                && !TryConsumeSelectionInternal(GlobalSelectionOwner, out locaPath, out locaKey))
            {
                return false;
            }

            return true;
        }

        [HideFromIl2Cpp]
        private static void ToggleCommand(Il2CppReferenceArray<CommandArg> _)
            => Toggle();

        internal static string GetRequestedSelectionOwner()
            => _requestedSelectionOwner;

        internal static void PublishSelection(string ownerToken, string locaPath, string locaKey)
        {
            string normalizedOwner = string.IsNullOrWhiteSpace(ownerToken)
                ? GlobalSelectionOwner
                : ownerToken;
            PendingSelectionsByOwner[normalizedOwner] = new SelectedLocalizationEntry(locaPath, locaKey);

            if (string.Equals(_requestedSelectionOwner, normalizedOwner, StringComparison.Ordinal))
                _requestedSelectionOwner = string.Empty;
        }

        private static bool TryConsumeSelectionInternal(string ownerToken, out string locaPath, out string locaKey)
        {
            locaPath = string.Empty;
            locaKey = string.Empty;
            if (!PendingSelectionsByOwner.Remove(ownerToken, out SelectedLocalizationEntry selection))
                return false;

            locaPath = selection.LocaPath;
            locaKey = selection.LocaKey;
            return true;
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
                Initialize();
        }

        private readonly struct SelectedLocalizationEntry(string locaPath, string locaKey)
        {
            public string LocaPath { get; } = locaPath;
            public string LocaKey { get; } = locaKey;
        }
    }

    [RegisterTypeInIl2Cpp]
    internal sealed class LocalizationEntriesEditorUI(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private const float DefaultWindowWidth = 1400f;
        private const float DefaultWindowHeight = 860f;
        private const float FileColumnWidth = 390f;

        private static readonly Regex EntryRegex = new(
            "^\\s*(?<key>[^\\s\\{]+)\\s*\\{\\s*(?<value>.*?)\\s*\\}\\s*$",
            RegexOptions.Compiled);

        private static readonly string[] KnownLanguages =
        [
            "de", "en", "fr", "es", "it", "pt", "ru", "zh_CN", "zh_TW", "ja", "ko", "nl", "pl", "tr", "ar", "sv", "da",
            "fi", "no", "hu", "cs", "el", "he", "th", "vi", "hi", "id", "ms", "ro", "bg", "uk", "sr", "hr", "sk",
            "tl", "fa", "be"
        ];

        private bool _visible;
        private bool _gameplayInputBlocked;
        private Rect _windowRect = new(40f, 40f, DefaultWindowWidth, DefaultWindowHeight);
        private Vector2 _filesScroll;
        private Vector2 _entriesScroll;
        private Vector2 _knownLanguagesScroll;
        private string _statusMessage = string.Empty;
        private string _fileFilter = string.Empty;
        private int _selectedKnownLanguageIndex = 1;
        private bool _useCustomLanguage;
        private bool _showKnownLanguagesDropdown;
        private string _newLanguage = "en";
        private string _newRelativeFilePath = "my_mod";

        private readonly List<LocalizationFileModel> _files = [];
        private string _selectedFilePath = string.Empty;

        internal bool IsVisible => _visible;

        internal void Awake()
        {
            ReloadFiles();
        }

        internal void OnDestroy()
        {
            SetGameplayInputBlocked(false);
        }

        internal void Update()
        {
            if (_visible && Input.GetKeyDown(KeyCode.Escape))
                SetVisible(false);
        }

        internal void OnGUI()
        {
            if (!_visible)
                return;

            EnsureWindowFitsScreen();
            _windowRect = GUI.Window(925501, _windowRect, new Action<int>(DrawWindow), "Localization Entries Editor");
        }

        [HideFromIl2Cpp]
        internal void SetVisible(bool visible)
        {
            if (_visible == visible)
                return;

            _visible = visible;
            if (_visible)
            {
                ReloadFiles();
                _statusMessage = "Loaded localization files from mod folder.";
            }

            SetGameplayInputBlocked(_visible);
        }

        [HideFromIl2Cpp]
        internal void SetStatus(string message)
            => _statusMessage = message;

        [HideFromIl2Cpp]
        private void DrawWindow(int _)
        {
            GUILayout.BeginVertical();

            DrawToolbar();

            GUILayout.BeginHorizontal();
            DrawFilesColumn();
            DrawDetailsColumn();
            GUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(_statusMessage))
                GUILayout.Label(_statusMessage);

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        [HideFromIl2Cpp]
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(110f)))
                ReloadFiles();

            if (GUILayout.Button("Apply Changes", GUILayout.Width(130f)))
                ApplyChangesToGame();

            GUILayout.Label("Filter", GUILayout.Width(44f));
            _fileFilter = GUILayout.TextField(_fileFilter, GUILayout.Width(320f));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close", GUILayout.Width(110f)))
                SetVisible(false);
            GUILayout.EndHorizontal();

            GUILayout.Label($"Root: {GetLocalizationRootPath()}");

            string requestedOwner = LocalizationEntriesEditorSystem.GetRequestedSelectionOwner();
            if (!string.IsNullOrWhiteSpace(requestedOwner))
                GUILayout.Label($"Selection request active for: {requestedOwner}");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Language", GUILayout.Width(70f));
            _useCustomLanguage = GUILayout.Toggle(_useCustomLanguage, "Custom", GUILayout.Width(80f));
            if (_useCustomLanguage)
            {
                _showKnownLanguagesDropdown = false;
                _newLanguage = GUILayout.TextField(_newLanguage, GUILayout.Width(120f));
            }
            else
            {
                _selectedKnownLanguageIndex = Mathf.Clamp(_selectedKnownLanguageIndex, 0, KnownLanguages.Length - 1);
                _newLanguage = KnownLanguages[_selectedKnownLanguageIndex];
                if (GUILayout.Button($"v {_newLanguage}", GUILayout.Width(140f)))
                    _showKnownLanguagesDropdown = !_showKnownLanguagesDropdown;
            }

            GUILayout.Label("New File", GUILayout.Width(70f));
            _newRelativeFilePath = GUILayout.TextField(_newRelativeFilePath, GUILayout.Width(240f));
            if (GUILayout.Button("Create File", GUILayout.Width(120f)))
                TryCreateNewFile();
            GUILayout.EndHorizontal();

            if (!_useCustomLanguage && _showKnownLanguagesDropdown)
            {
                GUILayout.BeginVertical("box", GUILayout.Width(160f));
                _knownLanguagesScroll = GUILayout.BeginScrollView(_knownLanguagesScroll, GUILayout.Height(180f), GUILayout.Width(150f));
                for (int i = 0; i < KnownLanguages.Length; i++)
                {
                    string language = KnownLanguages[i];
                    string prefix = i == _selectedKnownLanguageIndex ? "> " : "  ";
                    if (GUILayout.Button(prefix + language, GUILayout.Width(130f)))
                    {
                        _selectedKnownLanguageIndex = i;
                        _newLanguage = KnownLanguages[_selectedKnownLanguageIndex];
                        _showKnownLanguagesDropdown = false;
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
        }

        [HideFromIl2Cpp]
        private void ApplyChangesToGame()
        {
            if (!SaveAllDirtyFiles())
            {
                _statusMessage = "Apply canceled: at least one dirty file failed to save.";
                return;
            }

            try
            {
                LocalizationAccess.CreateLocalizationEntriesFromFolder();
                _statusMessage = "Applied localization changes: files copied and current language reloaded.";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Failed to apply localization changes: {ex.Message}";
            }
        }

        [HideFromIl2Cpp]
        private bool SaveAllDirtyFiles()
        {
            bool allSaved = true;
            for (int i = 0; i < _files.Count; i++)
            {
                LocalizationFileModel file = _files[i];
                if (!file.Dirty)
                    continue;

                if (!SaveFile(file))
                    allSaved = false;
            }

            return allSaved;
        }

        [HideFromIl2Cpp]
        private void DrawFilesColumn()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(FileColumnWidth), GUILayout.ExpandHeight(true));
            GUILayout.Label($"Localization Files ({_files.Count})");

            _filesScroll = GUILayout.BeginScrollView(_filesScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < _files.Count; i++)
            {
                LocalizationFileModel file = _files[i];
                if (!PassesFilter(file.DisplayName, _fileFilter))
                    continue;

                bool isSelected = string.Equals(_selectedFilePath, file.AbsolutePath, StringComparison.OrdinalIgnoreCase);
                string dirtySuffix = file.Dirty ? " *" : string.Empty;
                string label = isSelected
                    ? $"> {file.DisplayName}{dirtySuffix}"
                    : $"  {file.DisplayName}{dirtySuffix}";

                if (GUILayout.Button(label, GUILayout.ExpandWidth(true)))
                    _selectedFilePath = file.AbsolutePath;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        [HideFromIl2Cpp]
        private void DrawDetailsColumn()
        {
            GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

            LocalizationFileModel? selected = GetSelectedFile();
            if (selected == null)
            {
                GUILayout.Label("Select a localization file to edit entries.");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label($"Language: {selected.Language}");
            GUILayout.Label($"File: {selected.RelativePathWithinLanguage}");
            GUILayout.Label($"LocalizedString path: {selected.LocaPath}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Width(120f)))
                SaveFile(selected);
            if (GUILayout.Button("Reload", GUILayout.Width(120f)))
                ReloadSingleFile(selected);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label($"Entries ({selected.Entries.Count})");

            _entriesScroll = GUILayout.BeginScrollView(_entriesScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < selected.Entries.Count; i++)
            {
                LocalizationEntryModel entry = selected.Entries[i];
                GUILayout.BeginHorizontal();

                string updatedKey = GUILayout.TextField(entry.Key, GUILayout.Width(260f));
                string updatedValue = GUILayout.TextField(entry.Value, GUILayout.ExpandWidth(true));

                if (!string.Equals(updatedKey, entry.Key, StringComparison.Ordinal)
                    || !string.Equals(updatedValue, entry.Value, StringComparison.Ordinal))
                {
                    entry.Key = updatedKey;
                    entry.Value = updatedValue;
                    selected.Dirty = true;
                }

                if (GUILayout.Button("Select", GUILayout.Width(80f)))
                    PublishEntrySelection(selected, entry);

                if (GUILayout.Button("Delete", GUILayout.Width(80f)))
                {
                    selected.Entries.RemoveAt(i);
                    selected.Dirty = true;
                    i--;
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4f);
            GUILayout.Label("Add Entry");
            GUILayout.BeginHorizontal();
            selected.PendingNewKey = GUILayout.TextField(selected.PendingNewKey, GUILayout.Width(260f));
            selected.PendingNewValue = GUILayout.TextField(selected.PendingNewValue, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Add", GUILayout.Width(80f)))
                AddEntry(selected);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        [HideFromIl2Cpp]
        private void AddEntry(LocalizationFileModel file)
        {
            string key = file.PendingNewKey.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                _statusMessage = "Entry key is required.";
                return;
            }

            bool duplicateKey = file.Entries.Exists(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            if (duplicateKey)
            {
                _statusMessage = $"Entry key '{key}' already exists in this file.";
                return;
            }

            file.Entries.Add(new LocalizationEntryModel
            {
                Key = key,
                Value = file.PendingNewValue
            });
            file.PendingNewKey = string.Empty;
            file.PendingNewValue = string.Empty;
            file.Dirty = true;
        }

        [HideFromIl2Cpp]
        private void PublishEntrySelection(LocalizationFileModel file, LocalizationEntryModel entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                _statusMessage = "Cannot select an empty key.";
                return;
            }

            string owner = LocalizationEntriesEditorSystem.GetRequestedSelectionOwner();
            LocalizationEntriesEditorSystem.PublishSelection(owner, file.LocaPath, entry.Key);
            _statusMessage = string.IsNullOrWhiteSpace(owner)
                ? $"Selected '{file.LocaPath}:{entry.Key}'."
                : $"Selected '{file.LocaPath}:{entry.Key}' for '{owner}'.";
        }

        [HideFromIl2Cpp]
        private void ReloadFiles()
        {
            string previouslySelected = _selectedFilePath;
            _files.Clear();

            string root = GetLocalizationRootPath();
            EnsureDirectory(root);

            string[] files;
            try
            {
                files = Directory.GetFiles(root, "*.loc", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                _statusMessage = $"Failed to enumerate localization files: {ex.Message}";
                return;
            }

            for (int i = 0; i < files.Length; i++)
            {
                LocalizationFileModel? model = BuildFileModel(root, files[i]);
                if (model != null)
                    _files.Add(model);
            }

            _files.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            if (_files.Count == 0)
            {
                _selectedFilePath = string.Empty;
                return;
            }

            LocalizationFileModel? existingSelection = _files.FirstOrDefault(file =>
                string.Equals(file.AbsolutePath, previouslySelected, StringComparison.OrdinalIgnoreCase));
            _selectedFilePath = existingSelection?.AbsolutePath ?? _files[0].AbsolutePath;
        }

        [HideFromIl2Cpp]
        private static LocalizationFileModel? BuildFileModel(string rootPath, string filePath)
        {
            string relativePath = Path.GetRelativePath(rootPath, filePath);
            string[] segments = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                return null;

            string language = segments[0];
            string relativeWithinLanguage = segments.Length > 1
                ? string.Join(Path.DirectorySeparatorChar, segments.Skip(1))
                : Path.GetFileName(filePath);
            string locaPath = BuildLocaPath(relativeWithinLanguage, language);

            LocalizationFileModel model = new()
            {
                AbsolutePath = filePath,
                Language = language,
                RelativePathWithinLanguage = relativeWithinLanguage,
                LocaPath = locaPath,
                DisplayName = $"{language}/{relativeWithinLanguage.Replace('\\', '/')}"
            };

            model.Entries.AddRange(ParseEntries(filePath));
            return model;
        }

        [HideFromIl2Cpp]
        private void ReloadSingleFile(LocalizationFileModel file)
        {
            List<LocalizationEntryModel> entries = ParseEntries(file.AbsolutePath);
            file.Entries.Clear();
            file.Entries.AddRange(entries);
            file.PendingNewKey = string.Empty;
            file.PendingNewValue = string.Empty;
            file.Dirty = false;
            _statusMessage = $"Reloaded '{file.DisplayName}'.";
        }

        [HideFromIl2Cpp]
        private bool SaveFile(LocalizationFileModel file)
        {
            StringBuilder sb = new();
            for (int i = 0; i < file.Entries.Count; i++)
            {
                LocalizationEntryModel entry = file.Entries[i];
                if (string.IsNullOrWhiteSpace(entry.Key))
                    continue;

                string key = entry.Key.Trim();
                string value = entry.Value.TrimEnd();
                sb.AppendLine($"{key} {{ {value} }}");
                sb.AppendLine();
            }

            try
            {
                EnsureDirectory(Path.GetDirectoryName(file.AbsolutePath));
                File.WriteAllText(file.AbsolutePath, sb.ToString());
                file.Dirty = false;
                _statusMessage = $"Saved '{file.DisplayName}'.";
                return true;
            }
            catch (Exception ex)
            {
                _statusMessage = $"Failed to save '{file.DisplayName}': {ex.Message}";
                return false;
            }
        }

        [HideFromIl2Cpp]
        private void TryCreateNewFile()
        {
            string language = SanitizePathSegment(_newLanguage);
            string relativeFilePath = SanitizeRelativePath(_newRelativeFilePath);

            if (string.IsNullOrWhiteSpace(language) || string.IsNullOrWhiteSpace(relativeFilePath))
            {
                _statusMessage = "Language and file name are required.";
                return;
            }

            string fileName = BuildLocalizedFileName(relativeFilePath, language);

            string targetPath = Path.Combine(GetLocalizationRootPath(), language, fileName);
            try
            {
                EnsureDirectory(Path.GetDirectoryName(targetPath));
                if (!File.Exists(targetPath))
                    File.WriteAllText(targetPath, string.Empty);

                ReloadFiles();
                _selectedFilePath = targetPath;
                _statusMessage = $"Created '{language}/{fileName.Replace('\\', '/')}'; add entries and save.";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Failed to create file: {ex.Message}";
            }
        }

        [HideFromIl2Cpp]
        private LocalizationFileModel? GetSelectedFile()
        {
            if (string.IsNullOrWhiteSpace(_selectedFilePath))
                return null;

            return _files.FirstOrDefault(file =>
                string.Equals(file.AbsolutePath, _selectedFilePath, StringComparison.OrdinalIgnoreCase));
        }

        [HideFromIl2Cpp]
        private void SetGameplayInputBlocked(bool blocked)
        {
            if (_gameplayInputBlocked == blocked)
                return;

            InputAccess.ToggleGameplayActionMaps(!blocked);
            _gameplayInputBlocked = blocked;
        }

        [HideFromIl2Cpp]
        private static bool PassesFilter(string value, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [HideFromIl2Cpp]
        private static List<LocalizationEntryModel> ParseEntries(string filePath)
        {
            List<LocalizationEntryModel> entries = [];
            if (!File.Exists(filePath))
                return entries;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch
            {
                return entries;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                Match match = EntryRegex.Match(line);
                if (!match.Success)
                    continue;

                entries.Add(new LocalizationEntryModel
                {
                    Key = match.Groups["key"].Value.Trim(),
                    Value = match.Groups["value"].Value.Trim()
                });
            }

            return entries;
        }

        [HideFromIl2Cpp]
        private void EnsureWindowFitsScreen()
        {
            float minWidth = Mathf.Min(760f, Screen.width - 24f);
            float minHeight = Mathf.Min(520f, Screen.height - 24f);
            _windowRect.width = Mathf.Clamp(_windowRect.width, minWidth, Mathf.Max(minWidth, Screen.width - 12f));
            _windowRect.height = Mathf.Clamp(_windowRect.height, minHeight, Mathf.Max(minHeight, Screen.height - 12f));
            _windowRect.x = Mathf.Clamp(_windowRect.x, 6f, Mathf.Max(6f, Screen.width - _windowRect.width - 6f));
            _windowRect.y = Mathf.Clamp(_windowRect.y, 6f, Mathf.Max(6f, Screen.height - _windowRect.height - 6f));
        }

        [HideFromIl2Cpp]
        private static string GetLocalizationRootPath()
            => Path.Combine(Utils.SavePath, "Localization");

        [HideFromIl2Cpp]
        private static void EnsureDirectory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        [HideFromIl2Cpp]
        private static string SanitizePathSegment(string value)
        {
            string sanitized = value.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
                sanitized = sanitized.Replace(invalidChars[i], '_');

            return sanitized;
        }

        [HideFromIl2Cpp]
        private static string SanitizeRelativePath(string value)
        {
            string normalized = value.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).Trim();
            string[] segments = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                return string.Empty;

            for (int i = 0; i < segments.Length; i++)
                segments[i] = SanitizePathSegment(segments[i]);

            return string.Join(Path.DirectorySeparatorChar, segments);
        }

        [HideFromIl2Cpp]
        private static string BuildLocalizedFileName(string relativeFilePath, string language)
        {
            string directory = Path.GetDirectoryName(relativeFilePath) ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(relativeFilePath);
            string suffix = $"_{language}";

            if (baseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                baseName = baseName[..^suffix.Length];

            string fileName = $"{baseName}{suffix}.loc";
            return string.IsNullOrWhiteSpace(directory)
                ? fileName
                : Path.Combine(directory, fileName);
        }

        [HideFromIl2Cpp]
        private static string BuildLocaPath(string relativeWithinLanguage, string language)
        {
            string pathWithoutExtension = Path.ChangeExtension(relativeWithinLanguage, null);
            string suffix = $"_{language}";

            if (pathWithoutExtension.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                pathWithoutExtension = pathWithoutExtension[..^suffix.Length];

            return pathWithoutExtension.Replace('\\', '/').Replace(' ', '_');
        }

        private sealed class LocalizationFileModel
        {
            public string AbsolutePath { get; set; } = string.Empty;
            public string Language { get; set; } = string.Empty;
            public string RelativePathWithinLanguage { get; set; } = string.Empty;
            public string LocaPath { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public List<LocalizationEntryModel> Entries { get; } = [];
            public string PendingNewKey { get; set; } = string.Empty;
            public string PendingNewValue { get; set; } = string.Empty;
            public bool Dirty { get; set; }
        }

        private sealed class LocalizationEntryModel
        {
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }
    }
}
#endif




