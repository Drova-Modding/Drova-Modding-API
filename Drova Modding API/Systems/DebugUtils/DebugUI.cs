using Il2CppDrova;
using Il2CppDrova.GUI.Cheat;
using Il2CppInterop.Runtime.Attributes;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader;

namespace Drova_Modding_API.Systems.DebugUtils
{
    [RegisterTypeInIl2Cpp]
    internal class DebugUI(IntPtr ptr) : MonoBehaviour(ptr)
    {
#if DEBUG
        private TextMeshProUGUI _npcDisplay;

        [HideFromIl2Cpp]
        internal static void Init()
        {
            var cheatCanvas = FindFirstObjectByType<GUI_CheatCanvas>();
            if (cheatCanvas == null)
            {
                Debug.LogError("CheatCanvas not found");
                return;
            }

            GameObject gameObject = new("DebugUI");
            gameObject.transform.parent = cheatCanvas.transform;
            gameObject.transform.position = new Vector3(100, 500, 0);
            gameObject.AddComponent<DebugUI>();
            var verticalLayoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.m_ChildControlHeight = false;
            verticalLayoutGroup.m_ChildControlWidth = false;
            verticalLayoutGroup.m_ChildForceExpandHeight = false;
            verticalLayoutGroup.m_ChildForceExpandWidth = false;
            verticalLayoutGroup.m_ChildScaleHeight = false;
            verticalLayoutGroup.m_ChildScaleWidth = false;
            verticalLayoutGroup.m_TotalPreferredSize = new(300, 120);
            verticalLayoutGroup.m_TotalMinSize = new(300, 120);
            verticalLayoutGroup.m_TotalFlexibleSize = new(0, 0);
            verticalLayoutGroup.m_ChildAlignment = TextAnchor.LowerLeft;
        }

        internal void Awake()
        {
            GameObject npcDisplay = new("NpcDisplay");
            npcDisplay.transform.parent = gameObject.transform;
            _npcDisplay = npcDisplay.AddComponent<TextMeshProUGUI>();
            npcDisplay.SetActive(false);
            DebugManager.OnNpcSelected += OnNpcSelected;
        }

        internal void OnDestroy()
        {
            DebugManager.OnNpcSelected -= OnNpcSelected;
        }

        private void OnNpcSelected(Actor actor)
        {
            if (_npcDisplay == null)
            {
                _npcDisplay = gameObject.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (actor == null)
            {
                _npcDisplay.gameObject.SetActive(false);
                return;
            }

            _npcDisplay.gameObject.SetActive(true);
            _npcDisplay.text = "NPC Selected: " + actor.name;
        }
#endif
    }
}
