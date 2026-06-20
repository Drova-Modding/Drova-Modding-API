using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Drova_Modding_API.Systems.Talents;
using Il2CppDrova.Talent;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="Il2CppNodeCanvas.DialogueTrees.DS_LearnTalentNode"/>
    /// </summary>
    internal class DS_LearnTalentNodeEditor : DrawNodeEditor
    {
        private DS_LearnTalentNode? _castedNode;
        private GUIDropdownWithFilter? _talentDropdown;
        private List<TalentContainer> _allTalents = [];
        private string[] _allTalentNames = [];

        public DS_LearnTalentNodeEditor()
        {
            NodeSizeInternal = new Vector2(300, 150);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_LearnTalentNode>();
            if (_castedNode == null) return;

            if (_castedNode._teachableTalent == null)
            {
                _castedNode._teachableTalent = new DS_TeachTalentNode.TeachableTalent();
            }

            TalentContainerDatabase.InitializeDatabase();
            var grouped = TalentContainerDatabase.GetGroupedTalents();
            _allTalents = [.. grouped.Values.SelectMany(x => x)];
            _allTalentNames = [.. _allTalents.Select(x => x.name)];

            int selectedIndex = -1;
            if (_castedNode._teachableTalent.talent != null)
            {
                selectedIndex = Array.FindIndex(_allTalentNames, x => x == _castedNode._teachableTalent.talent.name);
            }

            _talentDropdown = new GUIDropdownWithFilter(_allTalentNames, selectedIndex, 20);
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null || _talentDropdown == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            GUI.depth = 10;
            GUI.color = Color.green;

            // Adjust height if dropdown is shown
            float dropdownExtraHeight = _talentDropdown.IsDropdownShown ? 200 : 0;
            NodeSizeInternal = new Vector2(300, 150 + dropdownExtraHeight);

            GUI.Box(new Rect(position.x, position.y, 300, 150), "DS_LearnTalentNode", EditorBoxStyles.GenericNode);

            GUI.color = Color.white;
            float currentY = position.y + 25;

            GUI.Label(new Rect(position.x + 5, currentY, 80, 20), "Talent:");
            if (_talentDropdown.Draw(new Rect(position.x + 85, currentY, 200, 20)))
            {
                if (_talentDropdown.SelectedIndex >= 0 && _talentDropdown.SelectedIndex < _allTalents.Count)
                {
                    _castedNode._teachableTalent.talent = _allTalents[_talentDropdown.SelectedIndex];
                }
            }

            currentY += 30;
            _castedNode._teachableTalent.teachExplicit = GUI.Toggle(new Rect(position.x + 5, currentY, 200, 20), _castedNode._teachableTalent.teachExplicit, "Teach Explicit");

            currentY += 25;
            _castedNode._teachableTalent.isEndNode = GUI.Toggle(new Rect(position.x + 5, currentY, 200, 20), _castedNode._teachableTalent.isEndNode, "Is End Node");

            currentY += 30;
            GUI.Label(new Rect(position.x + 5, currentY, 290, 40), "Description: Let teacher try to teach player preselected talent");

            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }
}
