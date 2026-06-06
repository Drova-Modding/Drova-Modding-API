#if DEBUG
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Drova_Modding_API.Systems.Editor;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.QuestSystem;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.GlobalVars
{
    /// <summary>
    /// Runtime inspector for browsing and editing all global vars from SubDatabase_GVars.
    /// Toggle with F6.
    /// </summary>
    [RegisterTypeInIl2Cpp]
    internal partial class GlobalVarInspectorSystem(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private const float DefaultWindowWidth = 1560f;
        private const float DefaultWindowHeight = 900f;
        private const float CreateListWindowWidth = 360f;
        private const float CreateListWindowHeight = 180f;
        private const float MinScrollHeight = 220f;
        private const float RowHeight = 24f;
        private static readonly string[] VarTypeOptions = new string[] { "GBool", "GInt", "GFloat", "GString" };

        private bool _isVisible;
        private bool _isCreateListWindowVisible;
        private bool _gameplayInputBlockedByInspector;
        private Rect _windowRect = new(20f, 20f, DefaultWindowWidth, DefaultWindowHeight);
        private Rect _createListWindowRect = new(80f, 80f, CreateListWindowWidth, CreateListWindowHeight);
        private string _pendingValue = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _showOnlyCustom;
        private GUIDropdown? _enumDropdown;
        private Rect _dropdownRect;

        internal void Awake()
        {
            RefreshData();
        }

        internal void OnDestroy()
        {
            SetGameplayInputBlocked(false);
        }

        internal void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                SetInspectorVisible(!_isVisible);
            }

            if (_isVisible && _isCreateListWindowVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseCreateCustomListWindow();
            }
        }

        internal void OnGUI()
        {
            if (!_isVisible)
            {
                return;
            }

            _windowRect = GUI.Window(923451, _windowRect, new Action<int>(DrawWindow), "GVar Inspector (F6)");

            if (_isCreateListWindowVisible)
            {
                CenterCreateListWindowOnScreen();
                _createListWindowRect = GUI.ModalWindow(923452, _createListWindowRect, new Action<int>(DrawCreateListWindow), "Create custom GVar list");
            }
        }

        [HideFromIl2Cpp]
        private void SetInspectorVisible(bool visible)
        {
            if (_isVisible == visible)
            {
                return;
            }

            _isVisible = visible;
            if (_isVisible)
            {
                RefreshData();
            }
            else
            {
                _isCreateListWindowVisible = false;
            }

            SetGameplayInputBlocked(_isVisible);
        }

        [HideFromIl2Cpp]
        private void SetGameplayInputBlocked(bool blocked)
        {
            if (_gameplayInputBlockedByInspector == blocked)
            {
                return;
            }

            InputAccess.ToggleGameplayActionMaps(!blocked);
            _gameplayInputBlockedByInspector = blocked;
        }

        [HideFromIl2Cpp]
        private void CenterCreateListWindowOnScreen()
        {
            _createListWindowRect.width = CreateListWindowWidth;
            _createListWindowRect.height = CreateListWindowHeight;
            _createListWindowRect.x = Mathf.Max(0f, (Screen.width - CreateListWindowWidth) * 0.5f);
            _createListWindowRect.y = Mathf.Max(0f, (Screen.height - CreateListWindowHeight) * 0.5f);
        }

        [HideFromIl2Cpp]
        private void DrawWindow(int id)
        {
            float scrollHeight = Mathf.Max(MinScrollHeight, _windowRect.height - 160f);
            float contentWidth = Mathf.Max(640f, _windowRect.width - 40f);
            float listWidth = Mathf.Clamp(contentWidth * 0.24f, 280f, 520f);
            float varWidth = Mathf.Clamp(contentWidth * 0.42f, 420f, 760f);

            GUILayout.BeginVertical();

            // Draw dropdown options first to handle input before other buttons in this window.
            if (_enumDropdown != null && _enumDropdown.DrawOptions(_dropdownRect))
            {
                _pendingValue = _enumDropdown.SelectedOption;
                ApplyValue(_pendingValue);
            }

            GUILayout.Label($"Lists: {_gvarLists.Count}  Vars in list: {_selectedListVars.Count}");
            GUILayout.Label(BuildSharedModBadgeText());
            _showOnlyCustom = GUILayout.Toggle(_showOnlyCustom, "Show only custom created");

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
        private static bool PassesFilter(string value, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return value?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [HideFromIl2Cpp]
        private static string BuildSharedModBadgeText()
        {
            string modStem = ModCreationContext.GetCurrentModFileStem();
            return string.IsNullOrWhiteSpace(modStem)
                ? "Shared Mod: <not set>"
                : $"Shared Mod: {modStem}";
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
