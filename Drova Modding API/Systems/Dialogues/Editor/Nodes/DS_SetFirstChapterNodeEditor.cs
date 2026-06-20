using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.GlobalVarSystem;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_SetFirstChapterNodeEditor : DrawNodeEditor
    {
        private DS_SetFirstChapter? _castedNode;
        private GUIGvarSelectionEditor _gvarSelectionEditor;

        public DS_SetFirstChapterNodeEditor()
        {
            NodeSizeInternal = new Vector2(400, 60);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_SetFirstChapter>();
            if (_castedNode == null) return;

            var variable = _castedNode.Variable?.value;
            if (variable != null)
            {
                _gvarSelectionEditor = new GUIGvarSelectionEditor(GvarType.INT, variable.GetParent().name, false, variable);
            }
            else
            {
                _castedNode.Variable = new BBParameter<GInt>();
                _gvarSelectionEditor = new GUIGvarSelectionEditor(GvarType.INT);
            }
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null) return;

            Color previousColor = GUI.color;
            GUI.color = Color.green;
            GUI.Box(new(position.x, position.y, NodeSize.x, NodeSize.y), "DS_SetFirstChapter");
            GUI.color = previousColor;

            if (_gvarSelectionEditor.DrawGvarEditor(new Rect(position.x + 10, position.y + 20, 250, 20), new Rect(position.x + 10, position.y + 40, 250, 20)))
            {
                _castedNode.Variable = _gvarSelectionEditor.CurrentSelectedGvar.TryCast<GInt>();
            }
        }
    }
}
