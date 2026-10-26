using Il2CppNodeCanvas.Framework;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor
{
    /// <summary>
    /// A snapshot of the graph editor state
    /// </summary>
    public class GraphEditorSnapshot(Graph graph, float scaleFactor, Vector2 panOffset)
    {
        /// <summary>
        /// The scale factor of the graph editor
        /// </summary>
        public float ScaleFactor = scaleFactor;
        /// <summary>
        /// The offset of the pan
        /// </summary>
        public Vector2 PanOffset = panOffset;
        /// <summary>
        /// The graph
        /// </summary>
        public Graph Graph = graph;
    }
}
