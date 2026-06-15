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
        private readonly record struct PendingSelection(string TypeName, string GVarListGuid, string GVarGuid);

        private const float DefaultWindowWidth = 1560f;
        private const float DefaultWindowHeight = 900f;
        private const float CreateListWindowWidth = 360f;
        private const float CreateListWindowHeight = 260f;
        private const float MinScrollHeight = 220f;
        private const float RowHeight = 24f;
        private static readonly Dictionary<string, PendingSelection> PendingSelectionsByOwner = new(StringComparer.Ordinal);

        private static GlobalVarInspectorSystem? _instance;
        private static string _requestedSelectionOwner = string.Empty;
        private static string _requestedSelectionTypeName = string.Empty;

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
            _instance = this;
            RefreshData();
        }

        internal void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

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

        internal static void RequestSelection(string ownerToken, string expectedTypeName)
        {
            if (string.IsNullOrWhiteSpace(ownerToken) || string.IsNullOrWhiteSpace(expectedTypeName))
            {
                return;
            }

            _requestedSelectionOwner = ownerToken;
            _requestedSelectionTypeName = expectedTypeName;
            if (_instance == null)
            {
                return;
            }

            _instance.SetInspectorVisible(true);
            _instance._statusMessage = $"Select a {expectedTypeName} in the inspector and click the publish button for '{ownerToken}'.";
        }

        internal static string GetRequestedSelectionOwner()
            => _requestedSelectionOwner;

        internal static string GetRequestedSelectionTypeName()
            => _requestedSelectionTypeName;

        internal static bool TryConsumeSelection(string ownerToken, string expectedTypeName, out string gvarListGuid, out string gvarGuid)
        {
            gvarListGuid = string.Empty;
            gvarGuid = string.Empty;

            if (string.IsNullOrWhiteSpace(ownerToken) || string.IsNullOrWhiteSpace(expectedTypeName))
            {
                return false;
            }

            if (!PendingSelectionsByOwner.TryGetValue(ownerToken, out PendingSelection selection))
            {
                return false;
            }

            if (!string.Equals(selection.TypeName, expectedTypeName, StringComparison.Ordinal))
            {
                return false;
            }

            PendingSelectionsByOwner.Remove(ownerToken);

            gvarListGuid = selection.GVarListGuid;
            gvarGuid = selection.GVarGuid;
            return true;
        }

        internal static void PublishSelection(string ownerToken, AGVarBase selectedVar)
        {
            if (string.IsNullOrWhiteSpace(ownerToken) || selectedVar == null)
            {
                return;
            }

            GVarList? parent = selectedVar.GetParent();
            if (parent == null)
            {
                return;
            }

            string typeName = GetTypeName(selectedVar);
            PendingSelectionsByOwner[ownerToken] = new PendingSelection(typeName, parent.Guid, selectedVar.GetGVarId());
            if (string.Equals(_requestedSelectionOwner, ownerToken, StringComparison.Ordinal))
            {
                _requestedSelectionOwner = string.Empty;
                _requestedSelectionTypeName = string.Empty;
            }

            if (_instance != null)
            {
                _instance._statusMessage = $"Selected '{parent.name}/{selectedVar.name}' ({typeName}) for '{ownerToken}'.";
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

            return value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
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
                return floatVar.GetValue().ToString(System.Globalization.CultureInfo.InvariantCulture);
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
        private static string SafeId(AGVarBase? gvar)
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
