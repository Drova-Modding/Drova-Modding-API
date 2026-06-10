using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova;
using Il2CppDrova.GUI;
using Il2CppDrova.Items.Stats;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppNodeCanvas.DialogueTrees;
using System.Globalization;
using UnityEngine;
using System.Linq;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_TeachStatsNode"/>
    /// </summary>
    internal class DS_TeachStatsNodeEditor : DrawNodeEditor
    {
        private DS_TeachStatsNode? _castedNode;
        private string[] _allStatNames = [];
        private Il2CppArrayBase<GenericStatDesc> _allStats;
        private List<GUIDropdownWithFilter> _statDropdowns = [];
        private List<GUIDropdownWithFilter> _choiceStatDropdowns = [];

        public DS_TeachStatsNodeEditor()
        {
            NodeSizeInternal = new Vector2(400, 200);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_TeachStatsNode>();
            if (_castedNode == null) return;

            if (_castedNode._stats == null)
            {
                _castedNode._stats = new Il2CppSystem.Collections.Generic.List<DS_TeachStatsNode.TeachableStat>();
            }
            if (_castedNode._availableStats == null)
            {
                _castedNode._availableStats = new Il2CppSystem.Collections.Generic.List<DS_TeachStatsNode.TeachableStatChoice>();
            }
            if (_castedNode._attributeCostTable == null)
            {
                _castedNode._attributeCostTable = ScriptableObject.CreateInstance<ScriptableObj_IntTable>();
                _castedNode._attributeCostTable._data = new Il2CppSystem.Collections.Generic.List<ScriptableObj_IntTable.Data>();
            }

            _allStats = Resources.FindObjectsOfTypeAll<GenericStatDesc>();
            _allStatNames = _allStats.Select(x => x.name).ToArray();

            RefreshDropdowns();
        }

        private void RefreshDropdowns()
        {
            if (_castedNode == null) return;

            _statDropdowns.Clear();
            for (int i = 0; i < _castedNode._stats.Count; i++)
            {
                var teachable = _castedNode._stats[i];
                int selectedIndex = -1;
                if (teachable.statDesc != null)
                {
                    selectedIndex = System.Array.FindIndex(_allStatNames, x => x == teachable.statDesc.name);
                }
                _statDropdowns.Add(new GUIDropdownWithFilter(_allStatNames, selectedIndex, 20));
            }

            _choiceStatDropdowns.Clear();
            for (int i = 0; i < _castedNode._availableStats.Count; i++)
            {
                var choice = _castedNode._availableStats[i];
                // statID in TeachableStatChoice matches statID in TeachableStat
                // Finding the name based on statID
                string currentStatName = "";
                var statInfo = _castedNode._stats.ToArray().FirstOrDefault(s => s.statID == choice.statID);
                if (statInfo != null && statInfo.statDesc != null)
                {
                    currentStatName = statInfo.statDesc.name;
                }

                int selectedIndex = System.Array.FindIndex(_allStatNames, x => x == currentStatName);
                _choiceStatDropdowns.Add(new GUIDropdownWithFilter(_allStatNames, selectedIndex, 20));
            }
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null) return;

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            GUI.depth = 10;
            GUI.color = Color.green;

            bool anyDropdownShown = _statDropdowns.Exists(x => x.IsDropdownShown) || _choiceStatDropdowns.Exists(x => x.IsDropdownShown);
            float dropdownExtraHeight = anyDropdownShown ? 200 : 0;
            float statsHeight = _castedNode._stats.Count * 110;
            float choicesHeight = _castedNode._availableStats.Count * 110;
            float costTableHeight = 30 + (_castedNode._attributeCostTable._data != null ? _castedNode._attributeCostTable._data.Count * 30 : 0);
            
            NodeSizeInternal = new Vector2(400, 150 + statsHeight + choicesHeight + costTableHeight + dropdownExtraHeight);

            GUI.Box(new Rect(position.x, position.y, NodeSizeInternal.x, NodeSizeInternal.y), "DS_TeachStatsNode", EditorBoxStyles.GenericNode);

            GUI.color = Color.white;
            float currentY = position.y + 25;

            GUI.Label(new Rect(position.x + 5, currentY, 120, 20), "Available Time:");
            string timeText = GUI.TextField(new Rect(position.x + 130, currentY, 50, 20), _castedNode.availableTime.ToString(CultureInfo.CurrentCulture));
            if (float.TryParse(timeText, out float time))
            {
                _castedNode.availableTime = time;
            }

            currentY += 30;
            GUI.Label(new Rect(position.x + 5, currentY, 150, 20), "Stats Definitions:");
            if (GUI.Button(new Rect(position.x + 160, currentY, 100, 20), "Add Stat"))
            {
                var newStat = new DS_TeachStatsNode.TeachableStat();
                newStat.statID = _castedNode.IDCounter++;
                _castedNode._stats.Add(newStat);
                RefreshDropdowns();
            }

            currentY += 25;
            for (int i = 0; i < _castedNode._stats.Count; i++)
            {
                var stat = _castedNode._stats[i];
                GUI.Box(new Rect(position.x + 5, currentY, 380, 100), $"Stat Definition {i} (ID: {stat.statID})", EditorBoxStyles.GenericNode);
                
                float internalY = currentY + 20;
                GUI.Label(new Rect(position.x + 10, internalY, 60, 20), "Stat:");
                if (_statDropdowns[i].Draw(new Rect(position.x + 75, internalY, 250, 20)))
                {
                    if (_statDropdowns[i].SelectedIndex >= 0)
                    {
                        stat.statDesc = _allStats[_statDropdowns[i].SelectedIndex];
                    }
                }

                if (GUI.Button(new Rect(position.x + 330, internalY, 50, 20), "Rem"))
                {
                    _castedNode._stats.RemoveAt(i);
                    RefreshDropdowns();
                    break;
                }

                internalY += 25;
                GUI.Label(new Rect(position.x + 10, internalY, 80, 20), "Teach Limit:");
                string limitText = GUI.TextField(new Rect(position.x + 100, internalY, 50, 20), stat.teachLimit.ToString());
                if (int.TryParse(limitText, out int limit)) stat.teachLimit = limit;

                currentY += 110;
            }

            currentY += 10;
            GUI.Label(new Rect(position.x + 5, currentY, 150, 20), "Available Choices:");
            if (GUI.Button(new Rect(position.x + 160, currentY, 100, 20), "Add Choice"))
            {
                _castedNode._availableStats.Add(new DS_TeachStatsNode.TeachableStatChoice());
                RefreshDropdowns();
            }

            currentY += 25;
            for (int i = 0; i < _castedNode._availableStats.Count; i++)
            {
                var choice = _castedNode._availableStats[i];
                GUI.Box(new Rect(position.x + 5, currentY, 380, 100), $"Choice {i}", EditorBoxStyles.GenericNode);

                float internalY = currentY + 20;
                GUI.Label(new Rect(position.x + 10, internalY, 60, 20), "Stat:");
                if (_choiceStatDropdowns[i].Draw(new Rect(position.x + 75, internalY, 250, 20)))
                {
                    if (_choiceStatDropdowns[i].SelectedIndex >= 0)
                    {
                        string selectedName = _allStatNames[_choiceStatDropdowns[i].SelectedIndex];
                        var statDef = _castedNode._stats.ToArray().FirstOrDefault(s => s.statDesc != null && s.statDesc.name == selectedName);
                        if (statDef != null)
                        {
                            choice.statID = statDef.statID;
                        }
                    }
                }

                if (GUI.Button(new Rect(position.x + 330, internalY, 50, 20), "Rem"))
                {
                    _castedNode._availableStats.RemoveAt(i);
                    RefreshDropdowns();
                    break;
                }

                internalY += 25;
                GUI.Label(new Rect(position.x + 10, internalY, 80, 20), "Increment:");
                string incText = GUI.TextField(new Rect(position.x + 100, internalY, 50, 20), choice.increment.ToString());
                if (int.TryParse(incText, out int inc)) choice.increment = inc;

                choice.isEndNode = GUI.Toggle(new Rect(position.x + 170, internalY, 150, 20), choice.isEndNode, "Is End Node");

                currentY += 110;
            }

            currentY += 10;
            GUI.Label(new Rect(position.x + 5, currentY, 150, 20), "Attribute Cost Table:");
            if (GUI.Button(new Rect(position.x + 160, currentY, 100, 20), "Add Entry"))
            {
                if (_castedNode._attributeCostTable._data == null)
                {
                    _castedNode._attributeCostTable._data = new Il2CppSystem.Collections.Generic.List<ScriptableObj_IntTable.Data>();
                }
                _castedNode._attributeCostTable._data.Add(new ScriptableObj_IntTable.Data());
            }

            currentY += 25;
            if (_castedNode._attributeCostTable._data != null)
            {
                for (int i = 0; i < _castedNode._attributeCostTable._data.Count; i++)
                {
                    var data = _castedNode._attributeCostTable._data[i];
                    
                    GUI.Label(new Rect(position.x + 10, currentY, 35, 20), "Min:");
                    string minText = GUI.TextField(new Rect(position.x + 45, currentY, 40, 20), data.KeyMin.ToString());
                    if (int.TryParse(minText, out int min)) data.KeyMin = min;

                    GUI.Label(new Rect(position.x + 90, currentY, 35, 20), "Max:");
                    string maxText = GUI.TextField(new Rect(position.x + 125, currentY, 40, 20), data.KeyMax.ToString());
                    if (int.TryParse(maxText, out int max)) data.KeyMax = max;

                    GUI.Label(new Rect(position.x + 170, currentY, 40, 20), "Cost:");
                    string valText = GUI.TextField(new Rect(position.x + 215, currentY, 40, 20), data.Value.ToString());
                    if (int.TryParse(valText, out int val)) data.Value = val;

                    if (GUI.Button(new Rect(position.x + 330, currentY, 50, 20), "Remove"))
                    {
                        _castedNode._attributeCostTable._data.RemoveAt(i);
                        break;
                    }

                    currentY += 30;
                }
            }

            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }
}
