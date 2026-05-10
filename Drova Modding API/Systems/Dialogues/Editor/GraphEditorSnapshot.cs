using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor
{
    /// <summary>
    /// A snapshot of the graph editor state
    /// </summary>
    /// <param name="dialogueTree">The last dialogueTree</param>
    /// <param name="scaleFactor">The last scale factor</param>
    /// <param name="panOffset">The last offset of the pan</param>
    public class GraphEditorSnapshot(DialogueTree dialogueTree, float scaleFactor, Vector2 panOffset)
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
        /// The Dialogue tree
        /// </summary>
        public DialogueTree DialogueTree = dialogueTree;
    }
}
