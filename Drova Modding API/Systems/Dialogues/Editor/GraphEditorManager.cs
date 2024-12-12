using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor
{
    /// <summary>
    /// Class that manages the context menu for the graph editor window
    /// </summary>
    /// <param name="ptr">Do not try to create this object with new()!</param>
    [RegisterTypeInIl2Cpp]
    internal class GraphEditorManager(IntPtr ptr): MonoBehaviour(ptr)
    {
        // Track whether the context menu is open
        private bool _showContextMenu = false;

        // Position where the right-click occurred
        private Vector2 _contextMenuPosition;

        // List of context menu options
        private readonly LocalizedString[] _contextMenuOptions = [new("Modding_API/GraphEditor", "Create"), new("Modding_API/GraphEditor", "Delete"), new("Modding_API/GraphEditor", "Duplicate")];

        internal void Update()
        {
            // Detect right-click (mouse button 1)
            if (Input.GetMouseButtonDown(1))
            {
                _showContextMenu = true;
                _contextMenuPosition = Input.mousePosition;
                // Convert to GUI coordinates (flip the Y-axis)
                _contextMenuPosition.y = Screen.height - _contextMenuPosition.y;
                // Ensure the menu doesn't go off the screen
                _contextMenuPosition.x = Mathf.Clamp(_contextMenuPosition.x, 0, Screen.width - 10) + 10;
            }

            // Close the context menu when clicking elsewhere
            if (Input.GetMouseButtonDown(0))
            {
                _showContextMenu = false;
            }
        }

        internal void OnGUI()
        {
            // Draw the context menu if it's open
            if (_showContextMenu)
            {
                DrawContextMenu();
            }
        }

        // Method to draw the context menu
        void DrawContextMenu()
        {
            float menuWidth = 100;
            float menuHeight = 20 * _contextMenuOptions.Length;

            // Draw the background box
            GUI.Box(new Rect(_contextMenuPosition.x, _contextMenuPosition.y, menuWidth, menuHeight), "");

            // Display each option as a button
            for (int i = 0; i < _contextMenuOptions.Length; i++)
            {
                if (GUI.Button(new Rect(_contextMenuPosition.x, _contextMenuPosition.y + i * 20, menuWidth, 20), _contextMenuOptions[i].GetLocalizedString(null)))
                {
                    HandleContextMenuSelection((GraphEditorAction) i);
                    _showContextMenu = false; // Close the menu after selection
                }
            }
        }

        // Handle the context menu option selection
        void HandleContextMenuSelection(GraphEditorAction option)
        {
            switch (option)
            {
                case GraphEditorAction.Create:
                    Debug.Log("Create selected");
                    break;
                case GraphEditorAction.Delete:
                    Debug.Log("Delete selected");
                    break;
                case GraphEditorAction.Duplicate:
                    Debug.Log("Duplicate selected");
                    break;
            }
        }
        enum GraphEditorAction
        {
            Create,
            Delete,
            Duplicate
        }
    }
}
