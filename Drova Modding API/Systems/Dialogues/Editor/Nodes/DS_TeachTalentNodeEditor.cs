using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Drova_Modding_API.Systems.Talents;
using Il2CppDrova.Talent;
using Il2CppNodeCanvas.DialogueTrees;
using System.Globalization;
using UnityEngine;
using System.Linq;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
#if DEBUG
    /// <summary>
    /// Node editor for <see cref="DS_TeachTalentNode"/>
    /// </summary>
    internal class DS_TeachTalentNodeEditor : DrawNodeEditor
    {
        private DS_TeachTalentNode? _castedNode;
        private Il2CppSystem.Collections.Generic.List<TalentContainer> _allTalents = new();
        private string[] _allTalentNames = [];
        private List<GUIDropdownWithFilter> _talentDropdowns = new();

        public DS_TeachTalentNodeEditor()
        {
            NodeSizeInternal = new Vector2(400, 200);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_TeachTalentNode>();
            if (_castedNode == null) return;

            if (_castedNode._availableTalents == null)
            {
                _castedNode._availableTalents = new Il2CppSystem.Collections.Generic.List<DS_TeachTalentNode.TeachableTalent>();
            }

            TalentContainerDatabase.InitializeDatabase();
            var grouped = TalentContainerDatabase.GetGroupedTalents();
            var flattened = grouped.Values.SelectMany(x => x).ToList();
            _allTalents = new Il2CppSystem.Collections.Generic.List<TalentContainer>();
            foreach (var t in flattened) _allTalents.Add(t);
            _allTalentNames = flattened.Select(x => x.name).ToArray();

            RefreshDropdowns();
        }

        private void RefreshDropdowns()
        {
            if (_castedNode == null) return;
            
            _talentDropdowns.Clear();
            for (int i = 0; i < _castedNode._availableTalents.Count; i++)
            {
                var teachable = _castedNode._availableTalents[i];
                int selectedIndex = -1;
                if (teachable.talent != null)
                {
                    selectedIndex = Array.FindIndex(_allTalentNames, x => x == teachable.talent.name);
                }
                _talentDropdowns.Add(new GUIDropdownWithFilter(_allTalentNames, selectedIndex, 20));
            }
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

            bool anyDropdownShown = _talentDropdowns.Exists(x => x.IsDropdownShown);
            float dropdownExtraHeight = anyDropdownShown ? 200 : 0;
            float listHeight = _castedNode._availableTalents.Count * 110;
            NodeSizeInternal = new Vector2(400, 100 + listHeight + dropdownExtraHeight);

            GUI.Box(new Rect(position.x, position.y, NodeSizeInternal.x, NodeSizeInternal.y), "DS_TeachTalentNode", EditorBoxStyles.GenericNode);

            GUI.color = Color.white;
            float currentY = position.y + 25;

            GUI.Label(new Rect(position.x + 5, currentY, 120, 20), "Available Time:");
            string timeText = GUI.TextField(new Rect(position.x + 130, currentY, 50, 20), _castedNode.availableTime.ToString(CultureInfo.CurrentCulture));
            if (float.TryParse(timeText, out float time))
            {
                _castedNode.availableTime = time;
            }

            currentY += 30;
            GUI.Label(new Rect(position.x + 5, currentY, 200, 20), "Teachable Talents:");
            if (GUI.Button(new Rect(position.x + 210, currentY, 100, 20), "Add Talent"))
            {
                _castedNode._availableTalents.Add(new DS_TeachTalentNode.TeachableTalent());
                RefreshDropdowns();
            }

            currentY += 25;

            for (int i = 0; i < _castedNode._availableTalents.Count; i++)
            {
                var teachable = _castedNode._availableTalents[i];
                GUI.Box(new Rect(position.x + 5, currentY, 380, 100), $"Talent {i}", EditorBoxStyles.GenericNode);
                
                float internalY = currentY + 20;
                GUI.Label(new Rect(position.x + 10, internalY, 60, 20), "Talent:");
                if (_talentDropdowns[i].Draw(new Rect(position.x + 75, internalY, 250, 20)))
                {
                    if (_talentDropdowns[i].SelectedIndex >= 0 && _talentDropdowns[i].SelectedIndex < _allTalents.Count)
                    {
                        teachable.talent = _allTalents[_talentDropdowns[i].SelectedIndex];
                    }
                }

                if (GUI.Button(new Rect(position.x + 330, internalY, 50, 20), "Rem"))
                {
                    _castedNode._availableTalents.RemoveAt(i);
                    RefreshDropdowns();
                    break;
                }

                internalY += 25;
                teachable.teachExplicit = GUI.Toggle(new Rect(position.x + 10, internalY, 150, 20), teachable.teachExplicit, "Teach Explicit");
                teachable.isEndNode = GUI.Toggle(new Rect(position.x + 170, internalY, 150, 20), teachable.isEndNode, "Is End Node");

                currentY += 110;
            }

            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }
#endif
}
