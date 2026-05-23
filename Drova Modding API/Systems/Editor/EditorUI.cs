#if DEBUG
using Drova_Modding_API.Systems.Dialogues.Editor;
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
        private Button _npcEditButton;
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
            gameObject.transform.position = new Vector3(50, 500, 0);
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

            GameObject graphEditor = new("GraphEditor");
            graphEditor.transform.parent = gameObject.transform;
            _graphEditorManager = graphEditor.AddComponent<GraphEditorManager>();

            npcDisplay.SetActive(false);
            graphEditor.SetActive(false);
            EditorManager.OnNpcSelected += OnNpcSelected;
        }

        internal void OnDestroy()
        {
            EditorManager.OnNpcSelected -= OnNpcSelected;
            _npcEditButton?.onClick.RemoveAllListeners();
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
                return;
            }

            _npcDisplay.gameObject.SetActive(true);
            _npcDisplay.text = "NPC Selected: " + actor.name;
            _npcEditButton.gameObject.SetActive(true);
        }

        [HideFromIl2Cpp]
        private void SetupNpcDialogEditor()
        {
            GameObject npcEdit = new("NpcEditDialog")
            {
                layer = 5
            };
            npcEdit.transform.parent = gameObject.transform;
            _npcEditButton = npcEdit.AddComponent<Button>();
            Image image = npcEdit.AddComponent<Image>();
            image.sprite = TextureToSprite(Texture2D.grayTexture);
            RectTransform npcEditRectTransform = npcEdit.GetComponent<RectTransform>() ?? npcEdit.AddComponent<RectTransform>();
            SetRectTransform(npcEditRectTransform, new Vector2(300, 110));

            GameObject text = new("TextForButton");
            text.transform.parent = npcEdit.transform;
            TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
            textComponent.text = "Edit NPC";
            RectTransform textRectTransform = textComponent.GetComponent<RectTransform>() ?? text.AddComponent<RectTransform>();
            SetRectTransform(textRectTransform, new Vector2(300, 100));
            _npcEditButton.onClick.AddListener(new Action(OnEditNpc));
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
        private static void SetRectTransform(RectTransform rectTransform, Vector2 sizeDelta)
        {
            rectTransform.sizeDelta = sizeDelta;
        }
    }
}
#endif
