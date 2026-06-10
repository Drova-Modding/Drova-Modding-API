using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;
using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="SubDialogueTree"/>
    /// </summary>
    internal class SubDialogueTreeEditor : DrawNodeEditor
    {
        private SubDialogueTree _castedNode;
        private readonly GUIContent GUIContent = new("SubDialogueTree", "Execute a Sub Dialogue Tree. When that Dialogue Tree is finished, this node will continue either in Success or Failure if it has any connections. Useful for making reusable and self-contained Dialogue Trees.");
        private DialogueTree[] _dialogueTrees;
        private string[] _dialogueTreeNames;
        private GUIDropdownWithFilter _dialogueTreeDropdown;

        public SubDialogueTreeEditor()
        {
            NodeSizeInternal = new Vector2(850, 110);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<SubDialogueTree>();
            _dialogueTrees = Resources.FindObjectsOfTypeAll<DialogueTree>();
            _dialogueTreeNames = [.. _dialogueTrees.Select(e => e.name)];
            _dialogueTreeDropdown = new GUIDropdownWithFilter(_dialogueTreeNames, Array.FindIndex(_dialogueTreeNames, (e) => e == _castedNode.subGraph?.name), 20);
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;

            GUI.Box(new Rect(position.x, position.y, 850, 110), GUIContent);

            GUI.color = Color.white;

            StringBuilder sb = new();
            if (_castedNode.subGraph == null)
            {
                sb.Append("No Sub Dialogue Tree selected");
            }
            else
                sb.Append("Full name: ").Append(_castedNode.subGraph.Key);

            GUI.Label(new Rect(position.x + 10, position.y + 25, 800, 25), sb.ToString());

            if (_dialogueTreeDropdown.Draw(new Rect(position.x + 10, position.y + 55, 400, 20)))
            {
                _castedNode.subGraph = _dialogueTrees[_dialogueTreeDropdown.SelectedIndex];
            }

            GUI.depth = previousDepth;
            GUI.color = previousColor;

        }

        public override void OnDoubleClick(Vector2 mousePosition)
        {
            base.OnDoubleClick(mousePosition);
            GraphEditorManager.GoIntoSubGraph(_castedNode);
        }
    }
}
