using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_RestartNode"/>
    /// </summary>
    internal class DS_RestartNodeEditor : DrawNodeEditor
    {
        private DS_RestartNode _castedNode;
        private string[] _dialogueTreeNames;
        private GUIDropdownWithFilter _dialogueTreeDropdown;

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_RestartNode>();
            _dialogueTreeNames = Il2CppNodeCanvas.Framework.Graph._runningGraphs.ToArray().ToList().Select(g => g.name).ToArray();
            _dialogueTreeDropdown = new GUIDropdownWithFilter(_dialogueTreeNames, Array.FindIndex(_dialogueTreeNames, (e) => e == _castedNode.GraphToRestart?.name), 20);
        }

        public override Rect DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;
            var rect = new Rect(position.x, position.y, 275, 90);
            GUI.Box(rect, "DS_RestartNode");
            _castedNode.useRoot = GUI.Toggle(new Rect(position.x + 5, position.y + 20, 250, 25), _castedNode.useRoot, "Use root");

            if (!_castedNode.useRoot)
            {
                if (_dialogueTreeDropdown.Draw(new Rect(position.x + 5, position.y + 50, 200, 20)))
                {
                    _castedNode.GraphToRestart = Il2CppNodeCanvas.Framework.Graph._runningGraphs[_dialogueTreeDropdown.SelectedIndex].TryCast<DialogueTree>();
                }
            }
            GUI.color = previousColor;
            GUI.depth = previousDepth;
            return rect;

        }
    }
}
