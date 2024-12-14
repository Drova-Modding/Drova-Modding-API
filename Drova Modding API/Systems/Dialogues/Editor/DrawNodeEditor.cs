using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor
{
    /// <summary>
    /// Abstract class that helps creating custom node editors for the graph editor
    /// </summary>
    public abstract class DrawNodeEditor
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public DrawNodeEditor() { }

        /// <summary>
        /// List of connected nodes
        /// </summary>
        public List<DrawNodeEditor> ConnectedWith = [];

        /// <summary>
        /// last Size of the node
        /// </summary>
        internal Vector2 NodeSize;
        /// <summary>
        /// last Position of the node
        /// </summary>
        internal Vector2 Position;
        /// <summary>
        /// The node that is being drawn
        /// </summary>
        internal DTNode Node;

        /// <summary>
        /// Draw the node
        /// </summary>
        /// <param name="position">Position to add</param>
        /// <returns>Rect of its position</returns>
        public abstract Rect DrawNode(Vector2 position);
    }
}
