#if DEBUG
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Dialogues.Editor;
using Drova_Modding_API.Systems.Routines.Editor;
using Il2CppDrova;
using Il2CppDrova.GUI.Cheat;
using Il2CppInterop.Runtime.Attributes;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace Drova_Modding_API.Systems.Editor
{
    [RegisterTypeInIl2Cpp]
    internal class EditorUI(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private TextMeshProUGUI _npcDisplay;
        private TextMeshProUGUI _playerPositionDisplay;
        private Button _npcEditButton = null!;
        private Button _routineEditButton = null!;
        private RoutineEditorWindow _routineEditorWindow = null!;
        private Actor _currentSelectedActor;
        private GraphEditorManager _graphEditorManager;

        [HideFromIl2Cpp]
        internal static void Init()
        {
            GUI_CheatCanvas cheatCanvas = FindFirstObjectByType<GUI_CheatCanvas>();
            if (cheatCanvas == null)
            {
                MelonLogger.Error("CheatCanvas not found");
                return;
            }

            GameObject gameObject = new("DebugUI");
            gameObject.transform.parent = cheatCanvas.transform;
            gameObject.transform.position = new Vector3(50, 600, 0);
            gameObject.AddComponent<EditorUI>();
            gameObject.AddComponent<NpcMouseRaycast>();
            gameObject.AddComponent<GraphicRaycaster>();
            VerticalLayoutGroup verticalLayoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childControlHeight = false;
            verticalLayoutGroup.childControlWidth = false;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childAlignment = TextAnchor.LowerLeft;
        }

        internal void Awake()
        {
            GameObject npcDisplay = new("NpcDisplay");
            npcDisplay.transform.parent = gameObject.transform;
            _npcDisplay = npcDisplay.AddComponent<TextMeshProUGUI>();
            RectTransform npcDisplayRectTransform = _npcDisplay.GetComponent<RectTransform>() ?? npcDisplay.AddComponent<RectTransform>();
            SetRectTransform(npcDisplayRectTransform, new Vector2(750, 50));
            
            GameObject playerPositionDisplay = new("PlayerPositionDisplay");
            playerPositionDisplay.transform.parent = gameObject.transform;
            _playerPositionDisplay = playerPositionDisplay.AddComponent<TextMeshProUGUI>();
            RectTransform playerPositionDisplayRectTransform = _playerPositionDisplay.GetComponent<RectTransform>() ?? playerPositionDisplay.AddComponent<RectTransform>();
            SetRectTransform(playerPositionDisplayRectTransform, new Vector2(750, 50));

            GameObject graphEditor = new("GraphEditor");
            graphEditor.transform.parent = gameObject.transform;
            _graphEditorManager = graphEditor.AddComponent<GraphEditorManager>();
            _routineEditorWindow = gameObject.AddComponent<RoutineEditorWindow>();

            npcDisplay.SetActive(false);
            graphEditor.SetActive(false);
            EditorManager.OnNpcSelected += OnNpcSelected;
            OptionMenuAccess.Instance.OnOptionMenuOpen += OnOptionMenuOpen;
            OptionMenuAccess.Instance.OnOptionMenuClose += OnOptionMenuClose;
        }
        
        private void OnOptionMenuOpen()
        {
            SetDisplaysActive(false);
        }

        private void OnOptionMenuClose()
        {
            SetDisplaysActive(true);
        }

        /// <summary>
        /// Guards against option menu events firing on an instance destroyed by a scene reload.
        /// </summary>
        [HideFromIl2Cpp]
        private void SetDisplaysActive(bool active)
        {
            if (!this) return;
            if (_npcDisplay) _npcDisplay.gameObject.SetActive(active);
            if (_playerPositionDisplay) _playerPositionDisplay.gameObject.SetActive(active);
        }

        internal void Update()
        {
            if (_playerPositionDisplay == null)
            {
                return;
            }

            if (PlayerAccess.TryGetPlayer(out Actor? player))
            {
                Vector3 position = player.transform.position;
                _playerPositionDisplay.text = $"Player Position (2D): X={position.x:F2}, Y={position.y:F2}";
                return;
            }

            _playerPositionDisplay.text = "Player Position (2D): unavailable";
        }

        internal void OnDestroy()
        {
            EditorManager.OnNpcSelected -= OnNpcSelected;
            OptionMenuAccess.Instance.OnOptionMenuOpen -= OnOptionMenuOpen;
            OptionMenuAccess.Instance.OnOptionMenuClose -= OnOptionMenuClose;
            if (_npcEditButton != null) _npcEditButton.onClick.RemoveAllListeners();
            if (_routineEditButton != null) _routineEditButton.onClick.RemoveAllListeners();
        }

        [HideFromIl2Cpp]
        private void OnNpcSelected(Actor actor)
        {
            _currentSelectedActor = actor;
            if (_npcDisplay == null)
            {
                _npcDisplay = gameObject.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (_npcEditButton == null)
            {
                SetupNpcDialogEditor();
            }
            if (actor == null)
            {
                _graphEditorManager.gameObject.SetActive(false);
                _npcDisplay.gameObject.SetActive(false);
                _npcEditButton.gameObject.SetActive(false);
                _routineEditButton.gameObject.SetActive(false);
                return;
            }

            _npcDisplay.gameObject.SetActive(true);
            _npcDisplay.text = "NPC Selected: " + actor.name;
            _npcEditButton.gameObject.SetActive(true);
            _routineEditButton.gameObject.SetActive(true);
        }

        [HideFromIl2Cpp]
        private void SetupNpcDialogEditor()
        {
            _npcEditButton = CreateButton("NpcEditDialog", "Edit NPC", OnEditNpc);
            _routineEditButton = CreateButton("NpcEditRoutine", "Edit Routine", OnEditRoutine);
        }

        [HideFromIl2Cpp]
        private Button CreateButton(string objectName, string label, Action onClick)
        {
            GameObject npcEdit = new(objectName)
            {
                layer = 5
            };
            npcEdit.transform.parent = gameObject.transform;
            Button button = npcEdit.AddComponent<Button>();
            Image image = npcEdit.AddComponent<Image>();
            image.sprite = TextureToSprite(Texture2D.grayTexture);
            RectTransform npcEditRectTransform = npcEdit.GetComponent<RectTransform>() ?? npcEdit.AddComponent<RectTransform>();
            SetRectTransform(npcEditRectTransform, new Vector2(300, 110));

            GameObject text = new("TextForButton");
            text.transform.parent = npcEdit.transform;
            TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
            textComponent.text = label;
            RectTransform textRectTransform = textComponent.GetComponent<RectTransform>() ?? text.AddComponent<RectTransform>();
            SetRectTransform(textRectTransform, new Vector2(300, 100));
            button.onClick.AddListener(onClick);
            return button;
        }

        [HideFromIl2Cpp]
        private static Sprite TextureToSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        
        [HideFromIl2Cpp]
        private void OnEditNpc()
        {
            _npcEditButton.gameObject.SetActive(false);
            _graphEditorManager.gameObject.SetActive(true);
            _graphEditorManager.Init(_currentSelectedActor);
            EditorManager.InEditor = true;
        }

        [HideFromIl2Cpp]
        private void OnEditRoutine()
        {
            if (_currentSelectedActor == null)
            {
                ToastAccess.Show("Select an NPC first.");
                return;
            }
            EntityInfo? entityInfo = _currentSelectedActor.GetEntityInfo();
            if (entityInfo == null || string.IsNullOrEmpty(entityInfo.GUID))
            {
                ToastAccess.Show($"'{_currentSelectedActor.name}' has no routine data (no EntityInfo), so its routine cannot be edited.");
                return;
            }

            _routineEditButton.gameObject.SetActive(false);
            _routineEditorWindow.Open(_currentSelectedActor, _graphEditorManager);
        }

        [HideFromIl2Cpp]
        private static void SetRectTransform(RectTransform rectTransform, Vector2 sizeDelta)
        {
            rectTransform.sizeDelta = sizeDelta;
        }
    }
}
#endif
