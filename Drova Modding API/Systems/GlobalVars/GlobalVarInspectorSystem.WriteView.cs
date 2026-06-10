#if DEBUG
using System.Globalization;
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Drova_Modding_API.Systems.Editor;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.QuestSystem;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace Drova_Modding_API.Systems.GlobalVars
{
    internal partial class GlobalVarInspectorSystem
    {
        private string _newListName = string.Empty;
        private string _newVarName = string.Empty;
        private string _newVarInitialValue = string.Empty;
        private string _customListModFileName = "my_mod";
        private string _sharedModFileName = "my_mod";
        private bool _newListIsQuestVarList;
        private string _createListWindowErrorMessage = string.Empty;
        private CustomGVarType _newVarType = CustomGVarType.GInt;
        private static readonly string[] VarTypeLabels = ["Bool", "Int", "Float", "String"];

        [HideFromIl2Cpp]
        private void OpenCreateCustomListWindow()
        {
            CenterCreateListWindowOnScreen();
            _createListWindowErrorMessage = string.Empty;
            _sharedModFileName = ModCreationContext.GetCurrentModFileStemOrDefault(_sharedModFileName);
            _isCreateListWindowVisible = true;
        }

        [HideFromIl2Cpp]
        private void CloseCreateCustomListWindow()
        {
            _createListWindowErrorMessage = string.Empty;
            _isCreateListWindowVisible = false;
        }

        [HideFromIl2Cpp]
        private void DrawCreateListWindow(int id)
        {
            GUI.Box(new Rect(8f, 24f, CreateListWindowWidth - 16f, CreateListWindowHeight - 32f), GUIContent.none);

            GUILayout.BeginVertical();
            GUILayout.Label("Name");
            _newListName = GUILayout.TextField(_newListName);
            _newListIsQuestVarList = GUILayout.Toggle(_newListIsQuestVarList, "Quest var list");
            GUILayout.Space(6f);
            GUILayout.Label("Shared mod file (used by custom creation)");
            _sharedModFileName = GUILayout.TextField(_sharedModFileName);
            if (GUILayout.Button("Apply shared mod file", GUILayout.Width(180f)))
            {
                ApplySharedModFileName();
            }

            if (!ModCreationContext.HasConfiguredModFileStem())
            {
                GUILayout.Label("Set and apply a shared mod file before creating custom lists.");
            }

            if (!string.IsNullOrWhiteSpace(_createListWindowErrorMessage))
            {
                GUILayout.Label(_createListWindowErrorMessage);
            }

            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            bool canCreate = !string.IsNullOrWhiteSpace(_newListName) && ModCreationContext.HasConfiguredModFileStem();
            GUI.enabled = canCreate;
            if (GUILayout.Button("Create"))
            {
                CreateCustomList();
            }

            GUI.enabled = true;

            if (GUILayout.Button("Cancel"))
            {
                CloseCreateCustomListWindow();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        [HideFromIl2Cpp]
        private void DrawEditorColumn()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Editor");

            string requestedSelectionOwner = GetRequestedSelectionOwner();
            if (!string.IsNullOrWhiteSpace(requestedSelectionOwner))
            {
                GUILayout.BeginVertical("box");
                string requestedSelectionTypeName = GetRequestedSelectionTypeName();
                GUILayout.Label($"Selection request active for: {requestedSelectionOwner}");
                if (!string.IsNullOrWhiteSpace(requestedSelectionTypeName))
                {
                    GUILayout.Label($"Requested type: {requestedSelectionTypeName}");
                }
                if (_selectedVar == null)
                {
                    GUILayout.Label("Select a variable from the list first.");
                }
                else
                {
                    GUILayout.Label($"Currently selected: {_selectedVar.name} ({GetTypeName(_selectedVar)})");
                    GUI.enabled = !string.IsNullOrWhiteSpace(requestedSelectionTypeName)
                        && string.Equals(GetTypeName(_selectedVar), requestedSelectionTypeName, StringComparison.Ordinal);
                    if (GUILayout.Button($"Use selected {GetTypeName(_selectedVar)} for '{requestedSelectionOwner}'"))
                    {
                        PublishSelection(requestedSelectionOwner, _selectedVar);
                    }

                    GUI.enabled = true;

                    if (!string.IsNullOrWhiteSpace(requestedSelectionTypeName)
                        && !string.Equals(GetTypeName(_selectedVar), requestedSelectionTypeName, StringComparison.Ordinal))
                    {
                        GUILayout.Label($"The current selection is not a {requestedSelectionTypeName}.");
                    }
                }

                if (GUILayout.Button("Cancel selection request"))
                {
                    _requestedSelectionOwner = string.Empty;
                    _statusMessage = "Selection request canceled.";
                }

                GUILayout.EndVertical();
                GUILayout.Space(6f);
            }

            if (_selectedVar == null)
            {
                GUILayout.Label("Select a variable to edit.");
            }

            if (_selectedVar != null)
            {
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
                    string typedValue = GUILayout.TextField(_pendingValue);
                    if (_selectedVar.TryCast<GInt>() != null)
                    {
                        _pendingValue = KeepWholeNumberCharactersOnly(typedValue);
                        if (_pendingValue.Length == 0)
                        {
                            GUILayout.Label("Whole numbers only (example: 0, -12, 42)");
                        }
                    }
                    else
                    {
                        _pendingValue = typedValue;
                    }
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
            }

            GUILayout.Space(10f);
            GUILayout.Label("Create custom variable");
            if (_selectedList == null)
            {
                GUILayout.Label("Select a custom list first.");
            }
            else if (!CustomGVarStore.IsCustomList(_selectedList))
            {
                GUILayout.Label("Variable creation is limited to custom lists.");
            }
            else
            {
                DrawCustomListHeader();
                DrawCustomVarSection();
                GUILayout.Label("Name");
                _newVarName = GUILayout.TextField(_newVarName);
                GUILayout.Label("Initial value");
                DrawInitialValueInputForType();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Create variable"))
                {
                    CreateCustomVar();
                }

                if (GUILayout.Button("Delete selected variable"))
                {
                    DeleteSelectedCustomVar();
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        [HideFromIl2Cpp]
        private void DrawCustomListHeader()
        {
            if (_selectedList == null) return;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"Custom list: {_selectedList.name}");
            GUILayout.Label($"Type: {(_selectedList.IsQuestVarList ? "Quest state" : "Regular")}");
            GUILayout.Label($"Source file: {CustomGVarStore.GetCustomListSourceFileName(_selectedList)}");
            GUILayout.Label("Save to mod file");
            _customListModFileName = GUILayout.TextField(_customListModFileName);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save list to mod file"))
            {
                SaveSelectedCustomListToModFile();
            }

            if (GUILayout.Button("Delete selected list"))
            {
                DeleteSelectedCustomList();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(4f);
        }

        [HideFromIl2Cpp]
        private void DrawCustomVarSection()
        {
            GUILayout.Label("Variable type:");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < VarTypeLabels.Length; i++)
            {
                CustomGVarType optionType = (CustomGVarType)i;
                bool isSelected = optionType == _newVarType;
                string buttonText = isSelected ? $"[{VarTypeLabels[i]}]" : VarTypeLabels[i];
                if (GUILayout.Button(buttonText))
                {
                    _newVarType = optionType;
                    NormalizeInitialValueInputForType();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
        
        }

        [HideFromIl2Cpp]
        private void DrawInitialValueInputForType()
        {
            if (_newVarType == CustomGVarType.GBool)
            {
                bool currentValue = ParseBoolInitialValueForUi();
                GUILayout.BeginHorizontal();
                string falseLabel = currentValue ? "false" : "[false]";
                string trueLabel = currentValue ? "[true]" : "true";
                if (GUILayout.Button(falseLabel))
                {
                    _newVarInitialValue = "false";
                }

                if (GUILayout.Button(trueLabel))
                {
                    _newVarInitialValue = "true";
                }

                GUILayout.EndHorizontal();
                return;
            }

            if (_newVarType == CustomGVarType.GInt)
            {
                string typedValue = GUILayout.TextField(_newVarInitialValue);
                _newVarInitialValue = KeepWholeNumberCharactersOnly(typedValue);
                if (_newVarInitialValue.Length == 0)
                {
                    GUILayout.Label("Whole numbers only (example: 0, -12, 42)");
                }

                return;
            }

            _newVarInitialValue = GUILayout.TextField(_newVarInitialValue);
        }

        [HideFromIl2Cpp]
        private bool ParseBoolInitialValueForUi()
        {
            if (bool.TryParse(_newVarInitialValue, out bool parsedBool))
            {
                return parsedBool;
            }

            return string.Equals(_newVarInitialValue, "1", StringComparison.Ordinal);
        }

        [HideFromIl2Cpp]
        private void NormalizeInitialValueInputForType()
        {
            if (_newVarType == CustomGVarType.GBool)
            {
                _newVarInitialValue = ParseBoolInitialValueForUi() ? "true" : "false";
                return;
            }

            if (_newVarType == CustomGVarType.GInt)
            {
                string normalized = KeepWholeNumberCharactersOnly(_newVarInitialValue);
                _newVarInitialValue = string.IsNullOrWhiteSpace(normalized) || normalized == "-" ? "0" : normalized;
                return;
            }

            if (_newVarType == CustomGVarType.GFloat && string.IsNullOrWhiteSpace(_newVarInitialValue))
            {
                _newVarInitialValue = 0f.ToString(CultureInfo.InvariantCulture);
            }
        }

        [HideFromIl2Cpp]
        private static string KeepWholeNumberCharactersOnly(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsDigit(character))
                {
                    builder.Append(character);
                    continue;
                }

                if (character == '-' && i == 0)
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        [HideFromIl2Cpp]
        private void CreateCustomList()
        {
            string trimmedListName = _newListName.Trim();
            _createListWindowErrorMessage = string.Empty;

            if (!ModCreationContext.HasConfiguredModFileStem())
            {
                _createListWindowErrorMessage = "Apply a shared mod file first.";
                return;
            }

            SubDatabase_GVars gVarDatabase = ProviderAccess.GVarDatabase;
            if (gVarDatabase.GetGVarListByName(trimmedListName) != null)
            {
                _createListWindowErrorMessage = $"A list named '{trimmedListName}' already exists.";
                return;
            }

            if (!CustomGVarStore.TryCreateCustomList(trimmedListName, _newListIsQuestVarList, ModCreationContext.GetCurrentModFileStem(), out GVarList? createdList, out string message))
            {
                if (message.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _createListWindowErrorMessage = message;
                }
                else
                {
                    _statusMessage = message;
                }

                return;
            }

            if (createdList == null)
            {
                _statusMessage = "Could not create custom list.";
                return;
            }

            _newListName = string.Empty;
            _newListIsQuestVarList = false;
            _createListWindowErrorMessage = string.Empty;
            _selectedList = createdList;
            RefreshData();
            SyncCustomListModFileNameFromSelectedList();
            _statusMessage = message;
            CloseCreateCustomListWindow();
        }

        [HideFromIl2Cpp]
        private void SaveSelectedCustomListToModFile()
        {
            ApplySharedModFileName();
            if (!CustomGVarStore.TrySaveCustomListToModFile(_selectedList, _customListModFileName, out string message))
            {
                _statusMessage = message;
                return;
            }

            RefreshData();
            SyncCustomListModFileNameFromSelectedList();
            _statusMessage = message;
        }

        [HideFromIl2Cpp]
        private void CreateCustomVar()
        {
            if (_selectedList == null)
            {
                _statusMessage = "Select a list first.";
                return;
            }

            NormalizeInitialValueInputForType();
            if (!CustomGVarStore.TryCreateCustomVar(_selectedList, _newVarType, Guid.NewGuid().ToString(), _newVarName, _newVarInitialValue, out AGVarBase? createdVar, out string message))
            {
                _statusMessage = message;
                return;
            }

            if (createdVar == null)
            {
                _statusMessage = "Could not create custom variable.";
                return;
            }

            _newVarName = string.Empty;
            _newVarInitialValue = string.Empty;
            RefreshSelectedListVars();
            _selectedVar = createdVar;
            _pendingValue = SafeValueToString(createdVar);
            UpdateDropdownForSelectedVar();
            _statusMessage = message;
        }

        [HideFromIl2Cpp]
        private void DeleteSelectedCustomVar()
        {
            if (_selectedVar == null)
            {
                _statusMessage = "Select a variable first.";
                return;
            }

            if (!CustomGVarStore.TryDeleteCustomVar(_selectedVar, out string message))
            {
                _statusMessage = message;
                return;
            }

            _selectedVar = null;
            _pendingValue = string.Empty;
            RefreshSelectedListVars();
            _statusMessage = message;
        }

        [HideFromIl2Cpp]
        private void DeleteSelectedCustomList()
        {
            if (_selectedList == null)
            {
                _statusMessage = "Select a list first.";
                return;
            }

            GVarList removedList = _selectedList;
            if (!CustomGVarStore.TryDeleteCustomList(removedList, out string message))
            {
                _statusMessage = message;
                return;
            }

            _selectedList = _gvarLists.FirstOrDefault(list => list != removedList);
            RefreshData();
            if (_selectedList != null)
            {
                RefreshSelectedListVars();
            }

            _statusMessage = message;
        }

        [HideFromIl2Cpp]
        private void SyncCustomListModFileNameFromSelectedList()
        {
            if (_selectedList == null)
            {
                return;
            }

            string sourceFileName = CustomGVarStore.GetCustomListSourceFileName(_selectedList);
            string sourceStem = Path.GetFileNameWithoutExtension(sourceFileName);
            if (!string.IsNullOrWhiteSpace(sourceStem))
            {
                _customListModFileName = sourceStem;
                _sharedModFileName = sourceStem;
            }
        }

        [HideFromIl2Cpp]
        private void ApplySharedModFileName()
        {
            string sanitized = ModCreationContext.SanitizeFileStem(_sharedModFileName);
            _sharedModFileName = sanitized;
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return;
            }

            ModCreationContext.SetCurrentModFileStem(sanitized);
            _customListModFileName = sanitized;
        }

        [HideFromIl2Cpp]
        private void ApplyValue(string rawValue)
        {
            if (_selectedVar == null)
            {
                return;
            }

            if (_selectedVar.TryCast<GInt>() != null)
            {
                string normalizedValue = KeepWholeNumberCharactersOnly(rawValue);
                if (string.IsNullOrWhiteSpace(normalizedValue) || normalizedValue == "-")
                {
                    _statusMessage = "Whole numbers only for GInt (example: 0, -12, 42).";
                    return;
                }

                rawValue = normalizedValue;
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
    }
}
#endif
