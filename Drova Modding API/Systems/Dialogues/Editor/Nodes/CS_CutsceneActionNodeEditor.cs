using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2Cpp;
using Il2CppDrova.Cutscenes;
using Il2CppSystem.Linq;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="CS_CutsceneActionNode"/>
    /// </summary>
    internal class CS_CutsceneActionNodeEditor : DrawNodeEditor
    {
        private CS_CutsceneActionNode _castedNode;
        private CS_CutsceneData[] _cutsceneDatas;
        private ICutsceneAction[] _cutsceneActions;
        private GUIDropdownWithFilter _cutsceneDropdown;
        private GUIDropdownWithFilter _actionIdDropdown;

        public CS_CutsceneActionNodeEditor()
        {
            NodeSizeInternal = new Vector2(340, 70);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<CS_CutsceneActionNode>();
            _cutsceneDatas = Resources.FindObjectsOfTypeAll<CS_CutsceneData>();

            string[] cutsceneNames = _cutsceneDatas.Select(e => e.name).ToArray();
            int cutsceneIndex = Array.FindIndex(_cutsceneDatas, cutscene => cutscene.name == _castedNode.cutscene.name);
            _cutsceneDropdown = new GUIDropdownWithFilter(cutsceneNames, cutsceneIndex, 20);

            if (_castedNode.cutsceneActionID >= 0)
            {
                _cutsceneActions = _castedNode.cutscene.GetCutsceneActions().ToArray();
                int cutsceneActionIndex = Array.FindIndex(_cutsceneActions, action => action.ID == _castedNode.cutsceneActionID);
                _actionIdDropdown = new GUIDropdownWithFilter(_cutsceneActions.Select((act) => act.Name).ToArray(), cutsceneActionIndex, 20);
            }
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = Color.green;
            int additionalHeight = _cutsceneDropdown.IsDropdownShown ? 20 * 20 : 0;
            Rect rect = new(position.x, position.y, 340, 70 + additionalHeight);
            GUI.Box(rect, "CS_CutsceneActionNode");

            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 10, position.y + 30, 100, 20), "Cutscene:");
            if (_cutsceneDropdown.Draw(new Rect(position.x + 120, position.y + 30, 200, 20)))
            {
                _castedNode.cutscene = _cutsceneDatas[_cutsceneDropdown.SelectedIndex];
                _cutsceneActions = _castedNode.cutscene.GetCutsceneActions().ToArray();
                _actionIdDropdown = new GUIDropdownWithFilter(_cutsceneActions.Select((act) => act.Name).ToArray(), 0, 20);
            }

            if (_actionIdDropdown != null)
            {
                GUI.Label(new Rect(position.x + 10, position.y + 50, 100, 20), "Action ID:");
                if (_actionIdDropdown.Draw(new Rect(position.x + 120, position.y + 50, 200, 20)))
                {
                    _castedNode.cutsceneActionID = _cutsceneActions[_actionIdDropdown.SelectedIndex].ID;
                }
            }

            GUI.color = previousColor;
        }
    }
}
