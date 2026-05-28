using MelonLoader;
using System.Text.Json;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning;

/// <summary>
/// Owns window rectangle state and layout constraints for the wizard UI windows.
/// This isolates fit-to-screen and resizes logic from gameplay/editor actions.
/// </summary>
internal sealed class NpcWizardWindowLayout
{
    private const float WindowWidthRatio = 0.62f;
    private const float WindowHeightRatio = 0.80f;
    private const float MinWindowWidth = 820f;
    private const float MinWindowHeight = 620f;
    private const float DefinitionsWindowWidth = 760f;
    private const float DefinitionsWindowHeight = 520f;
    private const float ScreenPadding = 16f;
    private const string LayoutSettingsFileName = "npc_wizard_layout.json";
    private const float PersistDebounceSeconds = 0.35f;

    private bool _mainWindowRectInitialized;
    private bool _definitionsWindowRectInitialized;
    private bool _usingCompactDialogueLayout;
    private bool _isResizing;
    private bool _pendingPersist;
    private bool _layoutLoadedFromDisk;
    private float _persistAtRealtime;
    private Vector2 _resizeStartMouse;
    private Vector2 _resizeStartSize;
    private Rect _preDialogueMainWindowRect;

    internal Rect MainWindowRect { get; private set; } = new(40f, 40f, 700f, 760f);

    internal Rect DefinitionsWindowRect { get; private set; } = new(70f, 70f, DefinitionsWindowWidth, DefinitionsWindowHeight);

    internal void SetMainWindowRect(Rect rect)
    {
        MainWindowRect = rect;
        if (!_usingCompactDialogueLayout)
            QueuePersist();
    }

    internal void SetDefinitionsWindowRect(Rect rect)
        => DefinitionsWindowRect = rect;

    internal void EnsureMainWindowRectFitsScreen()
    {
        TryLoadMainWindowLayoutFromDisk();

        if (!_mainWindowRectInitialized)
        {
            ResetMainWindowRectToScreen();
            return;
        }

        float availableWidth = Mathf.Max(360f, Screen.width - (ScreenPadding * 2f));
        float availableHeight = Mathf.Max(320f, Screen.height - (ScreenPadding * 2f));
        float minWidth = Mathf.Min(MinWindowWidth, availableWidth);
        float minHeight = Mathf.Min(MinWindowHeight, availableHeight);
        float maxWidth = Mathf.Max(minWidth, Screen.width - (MainWindowRect.x + ScreenPadding));
        float maxHeight = Mathf.Max(minHeight, Screen.height - (MainWindowRect.y + ScreenPadding));

        Rect rect = MainWindowRect;
        rect.width = Mathf.Clamp(rect.width, minWidth, maxWidth);
        rect.height = Mathf.Clamp(rect.height, minHeight, maxHeight);
        rect.x = Mathf.Clamp(rect.x, ScreenPadding, Mathf.Max(ScreenPadding, Screen.width - rect.width - ScreenPadding));
        rect.y = Mathf.Clamp(rect.y, ScreenPadding, Mathf.Max(ScreenPadding, Screen.height - rect.height - ScreenPadding));
        MainWindowRect = rect;

        PersistIfDue();
    }

    internal void ResetMainWindowRectToScreen()
    {
        float availableWidth = Mathf.Max(360f, Screen.width - (ScreenPadding * 2f));
        float availableHeight = Mathf.Max(320f, Screen.height - (ScreenPadding * 2f));
        float minWidth = Mathf.Min(MinWindowWidth, availableWidth);
        float minHeight = Mathf.Min(MinWindowHeight, availableHeight);
        float width = Mathf.Clamp(Screen.width * WindowWidthRatio, minWidth, availableWidth);
        float height = Mathf.Clamp(Screen.height * WindowHeightRatio, minHeight, availableHeight);
        float x = Mathf.Max(ScreenPadding, (Screen.width - width) * 0.5f);
        float y = Mathf.Max(ScreenPadding, (Screen.height - height) * 0.5f);

        MainWindowRect = new Rect(x, y, width, height);
        _mainWindowRectInitialized = true;
        if (!_usingCompactDialogueLayout)
            QueuePersist();
    }

    internal void EnterCompactDialogueLayout()
    {
        if (_usingCompactDialogueLayout)
            return;

        EnsureMainWindowRectFitsScreen();
        _preDialogueMainWindowRect = MainWindowRect;

        float availableWidth = Mathf.Max(360f, Screen.width - (ScreenPadding * 2f));
        float availableHeight = Mathf.Max(320f, Screen.height - (ScreenPadding * 2f));
        float minWidth = Mathf.Min(MinWindowWidth, availableWidth);
        float minHeight = Mathf.Min(MinWindowHeight, availableHeight);
        float x = ScreenPadding;
        float y = Mathf.Max(ScreenPadding, Screen.height - minHeight - ScreenPadding);

        MainWindowRect = new Rect(x, y, minWidth, minHeight);
        _usingCompactDialogueLayout = true;
    }

    internal void ExitCompactDialogueLayout()
    {
        if (!_usingCompactDialogueLayout)
            return;

        _usingCompactDialogueLayout = false;
        MainWindowRect = _preDialogueMainWindowRect;
        EnsureMainWindowRectFitsScreen();
    }

    internal void EnsureDefinitionsWindowRectFitsScreen()
    {
        if (!_definitionsWindowRectInitialized)
        {
            float width = Mathf.Min(DefinitionsWindowWidth, Screen.width - (ScreenPadding * 2f));
            float height = Mathf.Min(DefinitionsWindowHeight, Screen.height - (ScreenPadding * 2f));
            float x = Mathf.Max(ScreenPadding, (Screen.width - width) * 0.5f + 40f);
            float y = Mathf.Max(ScreenPadding, (Screen.height - height) * 0.5f + 20f);
            DefinitionsWindowRect = new Rect(x, y, width, height);
            _definitionsWindowRectInitialized = true;
            return;
        }

        float maxX = Mathf.Max(ScreenPadding, Screen.width - DefinitionsWindowRect.width - ScreenPadding);
        float maxY = Mathf.Max(ScreenPadding, Screen.height - DefinitionsWindowRect.height - ScreenPadding);
        DefinitionsWindowRect = new Rect(
            Mathf.Clamp(DefinitionsWindowRect.x, ScreenPadding, maxX),
            Mathf.Clamp(DefinitionsWindowRect.y, ScreenPadding, maxY),
            DefinitionsWindowRect.width,
            DefinitionsWindowRect.height);
    }

    internal void HandleMainResize(Rect resizeHandleRect)
    {
        Event evt = Event.current;
        if (evt == null)
            return;

        if (evt.type == EventType.MouseDown && evt.button == 0 && resizeHandleRect.Contains(evt.mousePosition))
        {
            _isResizing = true;
            _resizeStartMouse = evt.mousePosition;
            _resizeStartSize = new Vector2(MainWindowRect.width, MainWindowRect.height);
            evt.Use();
            return;
        }

        if (evt.type == EventType.MouseDrag && _isResizing)
        {
            Vector2 delta = evt.mousePosition - _resizeStartMouse;
            float availableWidth = Mathf.Max(360f, Screen.width - (ScreenPadding * 2f));
            float availableHeight = Mathf.Max(320f, Screen.height - (ScreenPadding * 2f));
            float minWidth = Mathf.Min(MinWindowWidth, availableWidth);
            float minHeight = Mathf.Min(MinWindowHeight, availableHeight);
            float maxWidth = Mathf.Max(minWidth, Screen.width - (MainWindowRect.x + ScreenPadding));
            float maxHeight = Mathf.Max(minHeight, Screen.height - (MainWindowRect.y + ScreenPadding));
            MainWindowRect = new Rect(
                MainWindowRect.x,
                MainWindowRect.y,
                Mathf.Clamp(_resizeStartSize.x + delta.x, minWidth, maxWidth),
                Mathf.Clamp(_resizeStartSize.y + delta.y, minHeight, maxHeight));

            if (!_usingCompactDialogueLayout)
                QueuePersist();

            evt.Use();
            return;
        }

        if (evt.type == EventType.MouseUp && _isResizing)
        {
            _isResizing = false;
            evt.Use();
        }
    }

    internal void PersistNow()
    {
        if (_usingCompactDialogueLayout)
            return;

        WriteMainWindowLayoutToDisk();
    }

    private void QueuePersist()
    {
        _pendingPersist = true;
        _persistAtRealtime = Time.realtimeSinceStartup + PersistDebounceSeconds;
    }

    private void PersistIfDue()
    {
        if (!_pendingPersist || _usingCompactDialogueLayout)
            return;

        if (Time.realtimeSinceStartup < _persistAtRealtime)
            return;

        WriteMainWindowLayoutToDisk();
    }

    private static string GetLayoutSettingsPath()
        => Path.Combine(Utils.SavePath, LayoutSettingsFileName);

    private void TryLoadMainWindowLayoutFromDisk()
    {
        if (_layoutLoadedFromDisk)
            return;

        _layoutLoadedFromDisk = true;
        string settingsPath = GetLayoutSettingsPath();
        try
        {
            if (!File.Exists(settingsPath))
                return;

            string raw = File.ReadAllText(settingsPath);
            if (string.IsNullOrWhiteSpace(raw))
                return;

            NpcWizardLayoutFile? layout = JsonSerializer.Deserialize<NpcWizardLayoutFile>(raw);
            if (layout?.MainWindowRect == null)
                return;

            MainWindowRect = layout.MainWindowRect.ToRect();
            _mainWindowRectInitialized = true;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"Failed to load NPC wizard layout settings '{settingsPath}': {ex.Message}");
        }
    }

    private void WriteMainWindowLayoutToDisk()
    {
        string settingsPath = GetLayoutSettingsPath();
        try
        {
            string? parent = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrWhiteSpace(parent) && !Directory.Exists(parent))
                Directory.CreateDirectory(parent);

            NpcWizardLayoutFile file = new()
            {
                Version = 1,
                MainWindowRect = NpcWizardRectData.FromRect(MainWindowRect)
            };
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true }));
            _pendingPersist = false;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"Failed to persist NPC wizard layout settings '{settingsPath}': {ex.Message}");
        }
    }

    private sealed class NpcWizardLayoutFile
    {
        public int Version { get; set; }

        public NpcWizardRectData? MainWindowRect { get; set; }
    }

    private sealed class NpcWizardRectData
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        public Rect ToRect() => new(X, Y, Width, Height);

        public static NpcWizardRectData FromRect(Rect rect)
            => new()
            {
                X = rect.x,
                Y = rect.y,
                Width = rect.width,
                Height = rect.height
            };
    }
}


