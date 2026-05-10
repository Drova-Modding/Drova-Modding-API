using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies
{
    /// <summary>
    /// Interface for action strategies
    /// </summary>
    public interface IActionStrategy
    {
        /// <summary>
        /// On start of the action
        /// </summary>
        /// <param name="editorManager">editorManager</param>
        /// <param name="selection">Current selected editor</param>
        /// <param name="clickPosition">Position of the click</param>
        void OnStart(GraphEditorManager editorManager, DrawNodeEditor selection, Vector2 clickPosition);

        /// <summary>
        /// On end of the action with left click
        /// </summary>
        /// <param name="editorManager">editorManager</param>
        /// <param name="selection">Current selected editor</param>
        /// <param name="clickPosition">Position of the click</param>
        void OnEnd(GraphEditorManager editorManager, DrawNodeEditor selection, Vector2 clickPosition);

        /// <summary>
        /// When the users cancels the action, with right click or escape
        /// </summary>
        /// <param name="editorManager"></param>
        /// <param name="selection"></param>
        void OnCancel(GraphEditorManager editorManager, DrawNodeEditor selection);

        /// <summary>
        /// On GUI event
        /// </summary>
        /// <param name="editorManager">editorManager</param>
        /// <param name="selection">Current selected editor</param>
        /// <param name="mousePosition">Position of the mouse</param>
        void OnGui(GraphEditorManager editorManager, DrawNodeEditor selection, Vector2 mousePosition);
    }
}
