using Drova_Modding_API.Access;
using Il2CppCommandTerminal;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.ModdingUI
{
    /// <summary>
    /// One entry point to every modder tool: a window listing the registered tools, opened from the pause menu's
    /// "Modding Tools" button or the <c>modding_tools</c> cheat. Mods register their own tools with <see cref="Register"/>.
    /// </summary>
    public static class ModdingTools
    {
        private const string InputOwner = "ModdingTools";
        private const float Width = 440f;
        private const float RowHeight = 26f;
        private const float Margin = 16f;
        private const float Top = 64f;
        private const float Padding = 10f;

        private static readonly List<ModdingTool> Registered = [];
        private static bool _visible;

        /// <summary>
        /// The registered tools, in registration order.
        /// </summary>
        public static IReadOnlyList<ModdingTool> Tools => Registered;

        /// <summary>
        /// Whether the window is open.
        /// </summary>
        public static bool IsOpen => _visible;

        /// <summary>
        /// Adds a tool to the window, replacing one with the same id.
        /// </summary>
        public static void Register(ModdingTool tool)
        {
            Registered.RemoveAll(existing => existing.Id == tool.Id);
            Registered.Add(tool);
        }

        /// <summary>
        /// Opens or closes the window.
        /// </summary>
        public static void Toggle() => SetVisible(!_visible);

        internal static void Initialize()
        {
            PauseMenuAccess.AddButton("modding_tools", () => "Modding Tools", () => SetVisible(true), () => Registered.Count > 0);
            CheatMenuAccess.RegisterCheat("modding_tools", (Action<Il2CppReferenceArray<CommandArg>>)(_ => Toggle()), 0, 0,
                "modding_tools", "Opens the list of modding tools.");
        }

        /// <summary>
        /// Draws the window. Called from the API melon's <c>OnGUI</c>.
        /// </summary>
        internal static void Draw()
        {
            if (!_visible)
            {
                return;
            }

            Event current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                SetVisible(false);
                current.Use();
                return;
            }

            float height = Padding * 2 + RowHeight + 6f + Registered.Count * (RowHeight * 2 + 6f) + RowHeight;
            Rect panel = new(Margin, Top, Width, height);
            GUI.Box(panel, GUIContent.none, FlatGuiStyles.Panel);

            float x = panel.x + Padding;
            float y = panel.y + Padding;
            float width = Width - Padding * 2;
            GUI.Label(new Rect(x, y, width, RowHeight), "Modding Tools", FlatGuiStyles.Title);
            y += RowHeight + 6f;

            for (int i = 0; i < Registered.Count; i++)
            {
                ModdingTool tool = Registered[i];
                bool available = tool.IsAvailable == null || tool.IsAvailable();
                if (GUI.Button(new Rect(x, y, width, RowHeight), tool.Name, available ? FlatGuiStyles.Button : FlatGuiStyles.DisabledButton) && available)
                {
                    SetVisible(false);
                    Open(tool);
                }

                y += RowHeight;
                GUI.Label(new Rect(x, y, width, RowHeight), tool.Description, FlatGuiStyles.Hint);
                y += RowHeight + 6f;
            }

            if (GUI.Button(new Rect(x, y, width, RowHeight), "Close (Esc)", FlatGuiStyles.Button))
            {
                SetVisible(false);
            }
        }

        private static void Open(ModdingTool tool)
        {
            try
            {
                tool.Open();
            }
            catch (Exception e)
            {
                MelonLogger.Error($"[ModdingTools] Opening '{tool.Id}' failed: {e}");
            }
        }

        private static void SetVisible(bool visible)
        {
            if (_visible == visible)
            {
                return;
            }

            _visible = visible;
            InputAccess.SetGameplayInputBlocked(InputOwner, visible);
        }
    }
}
