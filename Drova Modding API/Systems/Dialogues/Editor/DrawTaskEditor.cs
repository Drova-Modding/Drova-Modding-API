using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor
{
    /// <summary>
    /// Abstract class that helps creating custom task editors for the graph editor
    /// They are used in the nodes to draw the tasks
    /// </summary>
    public abstract class DrawTaskEditor
    {
        /// <summary>
        /// The task that is being drawn
        /// </summary>
        public Il2CppNodeCanvas.Framework.Task Task { get; set; }

        /// <summary>
        /// The graph editor manager
        /// </summary>
        public GraphEditorManager GraphEditorManager;

        /// <summary>
        /// Init the task, when the <see cref="Task"/> is set and on the first draw
        /// </summary>
        public virtual void Init()
        {
        }

        /// <summary>
        /// Draw the task
        /// </summary>
        /// <param name="position">position of the task</param>
        /// <returns>Rect of its position</returns>
        public abstract Rect DrawTask(Vector2 position);
    }
}
