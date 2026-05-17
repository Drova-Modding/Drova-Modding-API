#if DEBUG
using Drova_Modding_API.Access;
using Il2CppDrova.GlobalVarSystem;
using System;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.QuestSystem;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.GlobalVars
{
    /// <summary>
    /// Runtime inspector for browsing and editing all global vars from SubDatabase_GVars.
    /// Toggle with F6.
    /// </summary>
    [RegisterTypeInIl2Cpp]
    internal class GlobalVarInspectorSystem(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private const float DefaultWindowWidth = 1560f;
        private const float DefaultWindowHeight = 900f;
        private const float MinScrollHeight = 220f;
        private const float RowHeight = 24f;
        
        private readonly record struct ListRow(GVarList List, string Name);
        private readonly record struct VarRow(AGVarBase Var, string TypeName, string Name);

        private readonly List<GVarList> _gvarLists = [];
        private readonly List<AGVarBase> _selectedListVars = [];
        private readonly List<ListRow> _listRows = [];
        private readonly List<ListRow> _filteredListRows = [];
        private readonly List<VarRow> _varRows = [];
        private readonly List<VarRow> _filteredVarRows = [];

        private bool _isVisible;
        private Rect _windowRect = new(20f, 20f, DefaultWindowWidth, DefaultWindowHeight);
        private Vector2 _listScroll;
        private Vector2 _varsScroll;

        private string _listFilter = string.Empty;
        private string _varFilter = string.Empty;
        private string _lastAppliedListFilter = string.Empty;
        private string _lastAppliedVarFilter = string.Empty;
        private string _pendingValue = string.Empty;
        private string _statusMessage = string.Empty;
        private GUIDropdown? _enumDropdown;
        private Rect _dropdownRect;

        private GVarList _selectedList;
        private AGVarBase? _selectedVar;

        internal void Awake()
        {
            RefreshData();
        }

        internal void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                _isVisible = !_isVisible;
                if (_isVisible)
                {
                    RefreshData();
                }
            }
        }

        internal void OnGUI()
        {
            if (!_isVisible)
            {
                return;
            }

            _windowRect = GUI.Window(923451, _windowRect, new Action<int>(DrawWindow), "GVar Inspector (F6)");
        }

        [HideFromIl2Cpp]
        private void DrawWindow(int id)
        {
            float scrollHeight = Mathf.Max(MinScrollHeight, _windowRect.height - 160f);
            float contentWidth = Mathf.Max(640f, _windowRect.width - 40f);
            float listWidth = Mathf.Clamp(contentWidth * 0.24f, 280f, 520f);
            float varWidth = Mathf.Clamp(contentWidth * 0.42f, 420f, 760f);

            GUILayout.BeginVertical();

            // Draw dropdown options first to handle input before other buttons in this window
            // They will still render on top because of GUI.depth = -2000 in GUIDropdown
            if (_enumDropdown != null)
            {
                if (_enumDropdown.DrawOptions(_dropdownRect))
                {
                    _pendingValue = _enumDropdown.SelectedOption;
                    ApplyValue(_pendingValue);
                }
            }

            GUILayout.Label($"Lists: {_gvarLists.Count}  Vars in list: {_selectedListVars.Count}");

            if (!string.IsNullOrWhiteSpace(_statusMessage))
            {
                GUILayout.Label(_statusMessage);
            }

            GUILayout.BeginHorizontal();
            DrawListColumn(scrollHeight, listWidth);
            DrawVarColumn(scrollHeight, varWidth);
            DrawEditorColumn();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
            GUILayout.EndVertical();
        }

        [HideFromIl2Cpp]
        private void DrawListColumn(float scrollHeight, float listWidth)
        {
            GUILayout.BeginVertical(GUILayout.Width(listWidth));
            GUILayout.Label("GVar Lists");
            string updatedFilter = GUILayout.TextField(_listFilter);
            if (!string.Equals(updatedFilter, _listFilter, StringComparison.Ordinal))
            {
                _listFilter = updatedFilter;
            }

            EnsureListFilterCache();
            _listScroll = GUILayout.BeginScrollView(_listScroll, GUILayout.Height(scrollHeight));

            int totalRows = _filteredListRows.Count;
            float totalHeight = totalRows * RowHeight;
            Rect virtualizedRect = GUILayoutUtility.GetRect(0f, totalHeight, GUILayout.ExpandWidth(true));

            int firstVisible = Mathf.Max(0, Mathf.FloorToInt(_listScroll.y / RowHeight) - 2);
            int lastVisible = Mathf.Min(totalRows - 1, Mathf.CeilToInt((_listScroll.y + scrollHeight) / RowHeight) + 2);

            for (int i = firstVisible; i <= lastVisible; i++)
            {
                ListRow row = _filteredListRows[i];
                Rect rowRect = new(virtualizedRect.x, virtualizedRect.y + (i * RowHeight), virtualizedRect.width, RowHeight - 2f);

                bool isSelected = _selectedList == row.List;
                if (GUI.Button(rowRect, (isSelected ? "> " : string.Empty) + row.Name))
                {
                    _selectedList = row.List;
                    RefreshSelectedListVars();
                }
            }

            GUILayout.EndScrollView();
            if (GUILayout.Button("Reload lists"))
            {
                RefreshData();
            }
            GUILayout.EndVertical();
        }

        [HideFromIl2Cpp]
        private void DrawVarColumn(float scrollHeight, float varWidth)
        {
            GUILayout.BeginVertical(GUILayout.Width(varWidth));
            GUILayout.Label("Variables");
            string updatedFilter = GUILayout.TextField(_varFilter);
            if (!string.Equals(updatedFilter, _varFilter, StringComparison.Ordinal))
            {
                _varFilter = updatedFilter;
            }

            EnsureVarFilterCache();
            _varsScroll = GUILayout.BeginScrollView(_varsScroll, GUILayout.Height(scrollHeight));

            int totalRows = _filteredVarRows.Count;
            float totalHeight = totalRows * RowHeight;
            Rect virtualizedRect = GUILayoutUtility.GetRect(0f, totalHeight, GUILayout.ExpandWidth(true));

            int firstVisible = Mathf.Max(0, Mathf.FloorToInt(_varsScroll.y / RowHeight) - 2);
            int lastVisible = Mathf.Min(totalRows - 1, Mathf.CeilToInt((_varsScroll.y + scrollHeight) / RowHeight) + 2);

            for (int index = firstVisible; index <= lastVisible; index++)
            {
                VarRow row = _filteredVarRows[index];
                Rect rowRect = new(virtualizedRect.x, virtualizedRect.y + (index * RowHeight), virtualizedRect.width, RowHeight - 2f);
                string value = SafeValueToString(row.Var);
                string label = BuildVarLabel(row.TypeName, row.Name, value);

                bool isSelected = _selectedVar == row.Var;
                if (GUI.Button(rowRect, (isSelected ? "> " : string.Empty) + label))
                {
                    _selectedVar = row.Var;
                    _pendingValue = value;
                    _statusMessage = string.Empty;
                    UpdateDropdownForSelectedVar();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        [HideFromIl2Cpp]
        private void DrawEditorColumn()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Editor");

            if (_selectedVar == null)
            {
                GUILayout.Label("Select a variable to edit.");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label($"Name: {_selectedVar.name}");
            GUILayout.Label($"Id: {SafeId(_selectedVar)}");
            GUILayout.Label($"Type: {GetTypeName(_selectedVar)}");
            GUILayout.Label($"Current: {SafeValueToString(_selectedVar)}");
            GUILayout.Label("New value:");
            
            if (_enumDropdown != null)
            {
                _dropdownRect = GUILayoutUtility.GetRect(200f, 24f);
                _enumDropdown.RenderSelectedOption(_dropdownRect);
            }
            else
            {
                _pendingValue = GUILayout.TextField(_pendingValue);
            }

            if (GUILayout.Button("Apply"))
            {
                ApplyValue(_pendingValue);
            }

            if (GUILayout.Button("Reset (initial value)"))
            {
                _selectedVar.ResetValue(GVarContext.Empty);
                _pendingValue = SafeValueToString(_selectedVar);
                _statusMessage = "Value reset to initial value.";
                RebuildVarRows();
                UpdateDropdownForSelectedVar();
            }

            GUILayout.EndVertical();
        }

        [HideFromIl2Cpp]
        private void ApplyValue(string rawValue)
        {
            if (_selectedVar == null)
            {
                return;
            }

            if (!_selectedVar.TryParse(rawValue, out Il2CppSystem.Object parsedValue))
            {
                _statusMessage = $"Could not parse '{rawValue}' for {GetTypeName(_selectedVar)}.";
                return;
            }

            _selectedVar.SetGenericValue(parsedValue, GVarContext.Empty);
            _pendingValue = SafeValueToString(_selectedVar);
            _statusMessage = "Value applied.";
            RebuildVarRows();
            UpdateDropdownForSelectedVar();
        }

        [HideFromIl2Cpp]
        private void UpdateDropdownForSelectedVar()
        {
            _enumDropdown = null;
            if (_selectedVar == null)
            {
                return;
            }

            GBool boolVar = _selectedVar.TryCast<GBool>();
            if (boolVar != null)
            {
                string[] options = ["false", "true"];
                int selected = string.Equals(_pendingValue, "true", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                _enumDropdown = new GUIDropdown(options, selected);
                return;
            }

            GQuestState questStateVar = _selectedVar.TryCast<GQuestState>();
            if (questStateVar != null)
            {
                string[] options = Enum.GetNames<QuestState>();
                int selected = Array.IndexOf(options, _pendingValue);
                _enumDropdown = new GUIDropdown(options, selected);
            }
        }

        [HideFromIl2Cpp]
        private void RefreshData()
        {
            _gvarLists.Clear();
            SubDatabase_GVars db = ProviderAccess.GVarDatabase;
            if (db == null)
            {
                _statusMessage = "GVar database not available.";
                return;
            }

            Il2CppArrayBase<GVarList>? @base = db.AllGVars.ToArray();
            for (int index = 0; index < @base.Count; index++)
            {
                GVarList list = @base[index];
                if (list != null)
                {
                    _gvarLists.Add(list);
                }
            }

            _gvarLists.Sort((left, right) => string.Compare(left?.name, right?.name, StringComparison.OrdinalIgnoreCase));
            RebuildListRows();

            if (_selectedList == null || !_gvarLists.Contains(_selectedList))
            {
                _selectedList = _gvarLists.FirstOrDefault();
            }

            RefreshSelectedListVars();
            _statusMessage = string.Empty;
        }

        [HideFromIl2Cpp]
        private void RefreshSelectedListVars()
        {
            _selectedListVars.Clear();
            _selectedVar = null;
            _enumDropdown = null;

            if (_selectedList == null)
            {
                return;
            }

            HashSet<string> seenIds = [];
            AddVarsOfType<GInt>(seenIds);
            AddVarsOfType<GFloat>(seenIds);
            AddVarsOfType<GString>(seenIds);
            AddVarsOfType<GBool>(seenIds);
            AddVarsOfType<GQuestState>(seenIds);

            if (_selectedList.IsQuestVarList && _selectedList._questState != null)
            {
                AGVarBase questState = _selectedList._questState;
                if (seenIds.Add(SafeId(questState)))
                {
                    _selectedListVars.Add(questState);
                }
            }

            _selectedListVars.Sort((left, right) => string.Compare(left?.name, right?.name, StringComparison.OrdinalIgnoreCase));
            RebuildVarRows();
        }

        [HideFromIl2Cpp]
        private void AddVarsOfType<T>(HashSet<string> seenIds) where T : AGVarBase
        {
            Il2CppArrayBase<T>? bases = _selectedList.GetVarsOfType<T>().ToArray();
            for (int index = 0; index < bases.Count; index++)
            {
                T gvar = bases[index];
                if (gvar == null)
                {
                    continue;
                }

                if (seenIds.Add(SafeId(gvar)))
                {
                    _selectedListVars.Add(gvar);
                }
            }
        }

        [HideFromIl2Cpp]
        private static bool PassesFilter(string value, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return value?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [HideFromIl2Cpp]
        private void RebuildListRows()
        {
            _listRows.Clear();
            for (int i = 0; i < _gvarLists.Count; i++)
            {
                GVarList list = _gvarLists[i];
                if (list == null)
                {
                    continue;
                }

                _listRows.Add(new ListRow(list, list.name ?? "<null>"));
            }

            _lastAppliedListFilter = string.Empty;
            EnsureListFilterCache(forceRebuild: true);
        }

        [HideFromIl2Cpp]
        private void RebuildVarRows()
        {
            _varRows.Clear();
            for (int i = 0; i < _selectedListVars.Count; i++)
            {
                AGVarBase gvar = _selectedListVars[i];
                if (gvar == null)
                {
                    continue;
                }

                string typeName = GetTypeName(gvar);
                string varName = gvar.name ?? "<null>";
                _varRows.Add(new VarRow(gvar, typeName, varName));
            }

            _lastAppliedVarFilter = string.Empty;
            EnsureVarFilterCache(forceRebuild: true);
        }

        [HideFromIl2Cpp]
        private void EnsureListFilterCache(bool forceRebuild = false)
        {
            if (!forceRebuild && string.Equals(_listFilter, _lastAppliedListFilter, StringComparison.Ordinal))
            {
                return;
            }

            _filteredListRows.Clear();
            for (int i = 0; i < _listRows.Count; i++)
            {
                ListRow row = _listRows[i];
                if (PassesFilter(row.Name, _listFilter))
                {
                    _filteredListRows.Add(row);
                }
            }

            _lastAppliedListFilter = _listFilter;
        }

        [HideFromIl2Cpp]
        private void EnsureVarFilterCache(bool forceRebuild = false)
        {
            if (!forceRebuild && string.Equals(_varFilter, _lastAppliedVarFilter, StringComparison.Ordinal))
            {
                return;
            }

            _filteredVarRows.Clear();
            if (string.IsNullOrWhiteSpace(_varFilter))
            {
                _filteredVarRows.AddRange(_varRows);
                _lastAppliedVarFilter = _varFilter;
                return;
            }

            for (int i = 0; i < _varRows.Count; i++)
            {
                VarRow row = _varRows[i];
                string label = BuildVarLabel(row.TypeName, row.Name, SafeValueToString(row.Var));
                if (PassesFilter(label, _varFilter))
                {
                    _filteredVarRows.Add(row);
                }
            }

            _lastAppliedVarFilter = _varFilter;
        }

        [HideFromIl2Cpp]
        private static string BuildVarLabel(string typeName, string varName, string value)
        {
            return $"{typeName} | {varName} | {value}";
        }
            
        [HideFromIl2Cpp]
        private static string GetTypeName(AGVarBase gvar)
        {
            if (gvar.TryCast<GInt>() != null)
            {
                return nameof(GInt);
            }

            if (gvar.TryCast<GFloat>() != null)
            {
                return nameof(GFloat);
            }

            if (gvar.TryCast<GString>() != null)
            {
                return nameof(GString);
            }

            if (gvar.TryCast<GBool>() != null)
            {
                return nameof(GBool);
            }

            if (gvar.TryCast<GQuestState>() != null)
            {
                return nameof(GQuestState);
            }

            return gvar.GetIl2CppType().Name;
        }

        [HideFromIl2Cpp]
        private static string SafeValueToString(AGVarBase gvar)
        {
            GInt intVar = gvar.TryCast<GInt>();
            if (intVar != null)
            {
                return intVar.GetValue().ToString();
            }

            GBool boolVar = gvar.TryCast<GBool>();
            if (boolVar != null)
            {
                return boolVar.GetValue() ? "true" : "false";
            }

            GFloat floatVar = gvar.TryCast<GFloat>();
            if (floatVar != null)
            {
                return floatVar.GetValue().ToString();
            }

            GString stringVar = gvar.TryCast<GString>();
            if (stringVar != null)
            {
                return stringVar.GetValue() ?? "null";
            }

            GQuestState questStateVar = gvar.TryCast<GQuestState>();
            if (questStateVar != null)
            {
                QuestState value = questStateVar.GetValue();
                return value.ToString();
            }

            return gvar.GetGenericValue()?.ToString() ?? "null";
        }

        [HideFromIl2Cpp]
        private static string SafeId(AGVarBase gvar)
        {
            if (gvar == null)
            {
                return "null";
            }

            GInt intVar = gvar.TryCast<GInt>();
            if (intVar != null)
            {
                return intVar.GetGVarId();
            }

            GBool boolVar = gvar.TryCast<GBool>();
            if (boolVar != null)
            {
                return boolVar.GetGVarId();
            }

            GFloat floatVar = gvar.TryCast<GFloat>();
            if (floatVar != null)
            {
                return floatVar.GetGVarId();
            }

            GString stringVar = gvar.TryCast<GString>();
            if (stringVar != null)
            {
                return stringVar.GetGVarId();
            }

            GQuestState questStateVar = gvar.TryCast<GQuestState>();
            if (questStateVar != null)
            {
                return questStateVar.GetGVarId();
            }

            return gvar.GetGVarId();
        }
    }
}
#endif
