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
        /// The graph editor manager
        /// </summary>
        public GraphEditorManager GraphEditorManager;


        /// <summary>
        /// Size of the node
        /// </summary>
        protected Vector2 NodeSizeInternal;

        /// <summary>
        /// Size of the node
        /// </summary>
        public Vector2 NodeSize => NodeSizeInternal;

        /// <summary>
        /// Position of the node
        /// </summary>
        public Vector2 Position;
        /// <summary>
        /// The node that is being drawn
        /// </summary>
        public DTNode Node;

        /// <summary>
        /// The max amount of out connections
        /// </summary>
        protected int MaxOutConnections = 1;

        /// <summary>
        /// Draw the node
        /// </summary>
        /// <param name="position">Position to add</param>
        public abstract void DrawNode(Vector2 position);

        /// <summary>
        /// Initialize the node editor when the <see cref="Node"/> is set and on the first draw
        /// </summary>
        public virtual void Init() { }

        /// <summary>
        /// Called when the node is double clicked
        /// </summary>
        /// <param name="mousePosition">The mousePositíon when it was clicked (translated in gui cords)</param>
        public virtual void OnDoubleClick(Vector2 mousePosition) { }

        /// <summary>
        /// Called when the node is right clicked
        /// </summary>
        /// <param name="mousePosition">The mousePositíon when it was right clicked (translated in gui cords)</param>
        public virtual void OnRightClick(Vector2 mousePosition) { }
    }
}
