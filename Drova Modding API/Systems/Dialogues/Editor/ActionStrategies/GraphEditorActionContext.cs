using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies
{
    /// <summary>
    /// Mutable context passed to action strategies so they can operate on editor state
    /// without owning manager internals.
    /// </summary>
    public class GraphEditorActionContext(GraphEditorManager manager, DrawNodeEditor? selection, Vector2 pointerPosition, string? hintText)
    {
        /// <summary>
        /// Graph editor manager.
        /// </summary>
        public GraphEditorManager Manager { get; } = manager;

        /// <summary>
        /// Current selection at the moment this context was created.
        /// </summary>
        public DrawNodeEditor? Selection { get; private set; } = selection;

        /// <summary>
        /// Position in graph-space coordinates.
        /// </summary>
        public Vector2 PointerPosition { get; } = pointerPosition;

        /// <summary>
        /// Optional hint text rendered by the manager.
        /// </summary>
        public string? HintText { get; private set; } = hintText;

        /// <summary>
        /// Update the selected editor.
        /// </summary>
        public void SetSelection(DrawNodeEditor? editor)
        {
            Manager.SetSelectedNode(editor);
            Selection = editor;
        }

        /// <summary>
        /// Update action hint text.
        /// </summary>
        public void SetHint(string? hint)
        {
            HintText = hint;
        }
    }
}

