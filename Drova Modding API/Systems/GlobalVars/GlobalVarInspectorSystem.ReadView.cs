#if DEBUG
using Drova_Modding_API.Access;
using Il2CppDrova.GlobalVarSystem;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Drova_Modding_API.Systems.GlobalVars
{
    internal partial class GlobalVarInspectorSystem
    {
        private readonly record struct ListRow(GVarList List, string Name, bool IsCustom);
        private readonly record struct VarRow(AGVarBase Var, string TypeName, string Name, bool IsCustom);

        private readonly List<GVarList> _gvarLists = [];
        private readonly List<AGVarBase> _selectedListVars = [];
        private readonly List<ListRow> _listRows = [];
        private readonly List<ListRow> _filteredListRows = [];
        private readonly List<VarRow> _varRows = [];
        private readonly List<VarRow> _filteredVarRows = [];

        private Vector2 _listScroll;
        private Vector2 _varsScroll;

        private string _listFilter = string.Empty;
        private string _varFilter = string.Empty;
        private string _lastAppliedListFilter = string.Empty;
        private string _lastAppliedVarFilter = string.Empty;
        private bool _lastAppliedCustomOnlyListFilter;
        private bool _lastAppliedCustomOnlyVarFilter;

        private GVarList? _selectedList;
        private AGVarBase? _selectedVar;

        [HideFromIl2Cpp]
        private void DrawListColumn(float scrollHeight, float listWidth)
        {
            GUILayout.BeginVertical(GUILayout.Width(listWidth));
            GUILayout.Label("GVar Lists");

            if (GUILayout.Button("Create custom list..."))
            {
                OpenCreateCustomListWindow();
            }

            GUILayout.Space(6f);
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
            SyncCustomListModFileNameFromSelectedList();
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

                bool isCustom = CustomGVarStore.IsCustomList(list);
                string displayName = isCustom ? $"{list.name ?? "<null>"} [Custom]" : list.name ?? "<null>";
                _listRows.Add(new ListRow(list, displayName, isCustom));
            }

            _lastAppliedListFilter = string.Empty;
            _lastAppliedCustomOnlyListFilter = !_showOnlyCustom;
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
                bool isCustom = CustomGVarStore.IsCustomVar(gvar);
                _varRows.Add(new VarRow(gvar, typeName, varName, isCustom));
            }

            _lastAppliedVarFilter = string.Empty;
            _lastAppliedCustomOnlyVarFilter = !_showOnlyCustom;
            EnsureVarFilterCache(forceRebuild: true);
        }

        [HideFromIl2Cpp]
        private void EnsureListFilterCache(bool forceRebuild = false)
        {
            if (!forceRebuild
                && string.Equals(_listFilter, _lastAppliedListFilter, StringComparison.Ordinal)
                && _showOnlyCustom == _lastAppliedCustomOnlyListFilter)
            {
                return;
            }

            _filteredListRows.Clear();
            for (int i = 0; i < _listRows.Count; i++)
            {
                ListRow row = _listRows[i];
                if (_showOnlyCustom && !row.IsCustom)
                {
                    continue;
                }

                if (PassesFilter(row.Name, _listFilter))
                {
                    _filteredListRows.Add(row);
                }
            }

            _lastAppliedListFilter = _listFilter;
            _lastAppliedCustomOnlyListFilter = _showOnlyCustom;
        }

        [HideFromIl2Cpp]
        private void EnsureVarFilterCache(bool forceRebuild = false)
        {
            if (!forceRebuild
                && string.Equals(_varFilter, _lastAppliedVarFilter, StringComparison.Ordinal)
                && _showOnlyCustom == _lastAppliedCustomOnlyVarFilter)
            {
                return;
            }

            _filteredVarRows.Clear();
            if (string.IsNullOrWhiteSpace(_varFilter))
            {
                for (int i = 0; i < _varRows.Count; i++)
                {
                    VarRow row = _varRows[i];
                    if (_showOnlyCustom && !row.IsCustom)
                    {
                        continue;
                    }

                    _filteredVarRows.Add(row);
                }

                _lastAppliedVarFilter = _varFilter;
                _lastAppliedCustomOnlyVarFilter = _showOnlyCustom;
                return;
            }

            for (int i = 0; i < _varRows.Count; i++)
            {
                VarRow row = _varRows[i];
                if (_showOnlyCustom && !row.IsCustom)
                {
                    continue;
                }

                string label = BuildVarLabel(row.TypeName, row.Name, SafeValueToString(row.Var));
                if (PassesFilter(label, _varFilter))
                {
                    _filteredVarRows.Add(row);
                }
            }

            _lastAppliedVarFilter = _varFilter;
            _lastAppliedCustomOnlyVarFilter = _showOnlyCustom;
        }
    }
}
#endif


