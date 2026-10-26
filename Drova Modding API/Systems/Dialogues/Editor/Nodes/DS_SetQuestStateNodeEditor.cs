using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueSystem.DialogueTrees;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.QuestSystem;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_SetQuestStateNode"/>.
    /// The node writes through <c>AGVar&lt;QuestState&gt;.SetValue</c>, so the write raises
    /// <see cref="Access.GameEvents.OnQuestStateChanged"/> and is seen by coop sync like any graph-driven quest write.
    /// </summary>
    internal class DS_SetQuestStateNodeEditor : DrawNodeEditor
    {
        private DS_SetQuestStateNode? _castedNode;
        private GUIGvarSelectionEditor _gvarSelectionEditor = null!;
        private GUIDropdown _valueDropdown = null!;

        public DS_SetQuestStateNodeEditor()
        {
            NodeSizeInternal = new Vector2(450, 110);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_SetQuestStateNode>();
            if (_castedNode == null) return;

            GQuestState? variable = _castedNode.QuestState != null && !_castedNode.QuestState.isNoneOrNull ? _castedNode.QuestState.GetValue() : null;
            _gvarSelectionEditor = new GUIGvarSelectionEditor(GvarType.QUEST, null, false, variable);

            int valueIndex = _castedNode.Value != null ? (int)_castedNode.Value.GetValue() : -1;
            _valueDropdown = new GUIDropdown(Enum.GetNames<QuestState>(), valueIndex);
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            GUI.color = Color.green;
            GUI.Box(new Rect(position.x, position.y, 450, 110), "DS_SetQuestStateNode");
            GUI.color = Color.white;
            GUI.depth = 10;

            GUI.Label(new Rect(position.x + 10, position.y + 60, 100, 20), "Value to set");
            if (_valueDropdown.Draw(new Rect(position.x + 110, position.y + 60, 220, 20)))
            {
                _castedNode.Value = (QuestState)_valueDropdown.SelectedIndex;
            }

            if (_gvarSelectionEditor.DrawGvarEditor(new Rect(position.x, position.y + 20, 220, 20), new Rect(position.x, position.y + 40, 220, 20))
                && _gvarSelectionEditor.CurrentSelectedGvar != null)
            {
                _castedNode.QuestState = _gvarSelectionEditor.CurrentSelectedGvar.TryCast<GQuestState>();
            }

            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }
}
