using Drova_Modding_API.UI;
using Drova_Modding_API.UI.Builder;
using Il2Cpp;
using Il2CppDrova;
using Il2CppDrova.GUI;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace Drova_Modding_API.Access
{
    /**
     * This class is used to access the option menu in scene RuntimeScene_GUI.
     */
    public class OptionMenuAccess
    {
        private OptionMenuAccess() { }
        private GUI_Window _GUI_Window;
        private Transform moddingPanel;
        private readonly List<string> idsAdded = [];
        private GUI_Window_ATabManager.GUI_TabPanel guiPanel;
        private const string ScrollBarName = "ScrollRectView";
        /**
         * The name of the scene where the option menu is located.
         */
        public const string OptionSceneName = "RuntimeScene_GUI";
        private static readonly OptionMenuAccess _instance = new();
        /**
         * The instance of the option menu access.
         */
        public static OptionMenuAccess Instance { get; } = _instance;

        /**
         * Delegate for the option menu open/close action.
         */
        public delegate void OptionMenuAction();

        /**
        * This event is used to react on option menu open, this can happen from the pause menu and the game main menu.
        */
        public event OptionMenuAction OnOptionMenuOpen;

        /**
        * This event is used to react on option menu close, you can use this Event to save your configs globally.
        */
        public event OptionMenuAction OnOptionMenuClose;

        internal static void OnOptionOpen()
        {
            var guiWindow = _instance.GetGUIWindow();
            if (!guiWindow)
            {
                return;
            }
            _instance.OnOptionMenuOpen.Invoke();
            var manager = guiWindow.GetComponent<GUI_Window_Options>();
            // Workaround for Update 1.0.2.1 where the modded panel is not activated for whatever reason.
            if (manager._currentPanelIndex == -1 || manager._currentPanelIndex >= 5)
            {
                manager.ChangePanel(0);
            }
        }

        internal static void OnOptionClose()
        {
            var window = _instance.GetGUIWindow();
            if (!window)
            {
                _instance.idsAdded.Clear();
                return;
            }
            if (!window.gameObject.activeSelf)
            {
                window.GetComponent<GUI_Window_Options>()._currentPanelIndex = 0;
                _instance.OnOptionMenuClose.Invoke();
            }
        }

        /**
         * Add a header and panel to the option menu and set a icon. This is not persistent and will be removed when the option menu is closed.
         * @param icon The icon of the header (not the background). If null, a random icon will be used.
         * @param name The name of the header. It needs to be unique and named liked: GUI_Button_OptionTab_YOUROPTION.
         * @return The created transfrom for the builder or if the Elements already exists null.
         */
        public Transform AddPanel(Sprite icon, string name)
        {
            if (!name.StartsWith("GUI_Button_OptionTab_"))
            {
                MelonLogger.Error("The name of the header needs to start with GUI_Button_OptionTab_");
                return null;
            }
            GameObject root = GetRootOfHeader();
            if (!root) return null;
            if (GetHeader(name)) return null;

            var prefabForHeader = root.transform.GetChild(0);
            if (!prefabForHeader) { MelonLogger.Error("Failed to get prefabForHeader"); return null; }
            GameObject newHeader = UnityEngine.Object.Instantiate(prefabForHeader.gameObject, root.transform);

            if (icon)
            {
                var headerChildIcon = newHeader.transform.GetChild(0);
                if (!headerChildIcon) { MelonLogger.Error("Failed to get headerChildIcon"); return null; }
                Image image = headerChildIcon.GetComponent<Image>();
                if (!image) { MelonLogger.Error("Failed to get Image"); return null; }
                image.m_OverrideSprite = icon;
                image.sprite = icon;
                image.MarkDirty();
            }
            newHeader.name = name;
            var panel = AddPanel(newHeader);
            if (!panel)
            {
                MelonLogger.Error("Failed to get panel"); return null;
            }
            var componentToRegister = newHeader.GetComponent<GUI_ButtonNavigationAnimationElement>();
            if (!componentToRegister) MelonLogger.Error("Failed to get GUI_ButtonNavigationAnimationElement");

            var scrollbar = GetScrollBar(panel);
            if (!scrollbar) MelonLogger.Error("Failed to get scrollbar");
            scrollbar.name = ScrollBarName;

            var layoutGroup = scrollbar.transform.GetChild(0);
            if (!layoutGroup) MelonLogger.Error("Failed to get LayoutGroup");
            layoutGroup?.DestroyAllChildren();

            var nagivation = root.GetComponent<GUI_ButtonNavigationAnimation>();
            if (!nagivation) MelonLogger.Error("Failed to get GUI_ButtonNavigationAnimation");
            nagivation?.AddElement(componentToRegister);

            var window = GetGUIWindow();
            if (window)
            {
                window.GetComponent<GUI_GameMenu_NavigationSwitcher>().legacyElements.Add(componentToRegister);
                OverrideNavigation(newHeader, panel);
            }
            else
            {
                MelonLogger.Error("Failed to get GetGUIWindow");
            }
            return layoutGroup;
        }

        private static GameObject GetScrollBar(GameObject panel)
        {
            return panel.transform.GetChild(1)?.gameObject;
        }

        /**
         * Add the header and panel to the navigatio system.
         * @param header The header to add.
         * @param panel The panel to add.
         */
        private void OverrideNavigation(GameObject header, GameObject panel)
        {
            var manager = GetGUIWindow().GetComponent<GUI_Window_Options>();
            if (!manager) { MelonLogger.Error("Failed to get manager to override the navigation"); return; }
            guiPanel = new GUI_Window_ATabManager.GUI_TabPanel
            {
                Instance = panel.GetComponent<GUI_GameMenu_APanel>(),
                Id = header.transform.GetSiblingIndex() + 1,
                NavigationElement = header.GetComponent<GUI_ButtonNavigationAnimationElement>()
            };
            manager.GuiPanels.Add(guiPanel);
            manager.SetupPanelNavigationElements();
            // Rerender panel, otherwise its empty. 
            manager.ChangePanelDefault(0);
        }

        /**
         * Add a panel to the option menu.
         * @param header The header to add the panel to. It needs a name with GUI_Button_OptionTab_YOUROPTION
         * @return The created panel game object.
         */
        private static GameObject AddPanel(GameObject header)
        {
            var root = GetRootOfOptionWindow();
            var panelPrefab = root.transform.GetChild(4);
            if (!panelPrefab) { MelonLogger.Error("Failed to get panelPrefab"); return null; }
            var newPanel = UnityEngine.Object.Instantiate(panelPrefab.gameObject, root.transform);
            newPanel.transform.SetSiblingIndex(newPanel.transform.GetSiblingIndex() - 1);
            newPanel.name = string.Concat("Panel", header.name.AsSpan(header.name.LastIndexOf('_')));
            var nextKey = root.transform.FindChild("NextKey");
            if (nextKey)
                nextKey.localPosition += new Vector3(20f, 0f);
            else MelonLogger.Warning("Failed to adjust position of NextKey");
            newPanel.SetActive(false);
            return newPanel;
        }

        /**
         * Get the builder for the option menu.
         * @param panel The panel to add the elements to.
         * @return The builder for the option menu.
         */
        public static OptionUIBuilder GetBuilder(Transform panel)
        {
            return new OptionUIBuilder(panel);
        }

        /// <summary>
        /// Create your Options in the general modding panel
        /// </summary>
        /// <param name="id">Unique identifier for your mod</param>
        /// <returns></returns>
        public OptionUIBuilder GetBuilder(string id)
        {
            if (!moddingPanel)
            {
                moddingPanel = AddPanel(null, "GUI_Button_OptionTab_Modding");
            }
            if (!moddingPanel) return null;
            if (idsAdded.Contains(id)) return null;
            idsAdded.Add(id);
            return new OptionUIBuilder(moddingPanel);
        }

        /**
         * Get the GUI window.
         * @return The root of the option window.
         */
        public GameObject GetGUIWindow()
        {
            if (_GUI_Window)
            {
                return _GUI_Window.gameObject;
            }
            var allWindows = UnityEngine.Object.FindObjectsByType<GUI_Window>(FindObjectsSortMode.None);
            foreach (var window in allWindows)
            {
                if (window.gameObject.scene.name == OptionSceneName && window.name == "GUI_Window_Options(Clone)")
                {
                    _GUI_Window = window;
                    return window.gameObject;
                }
            }
            return null;
        }

        /**
         * Get the root of the panel where header and panels are.
         * @return The root of the option window.
         */
        public static GameObject GetRootOfOptionWindow()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel");
        }

        /**
         * Get the root of the header.
         * @return The root of the header.
         */
        public static GameObject GetRootOfHeader()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Header");
        }

        /**
         * Get the header by name.
         * @param name The name of the header.
         * @return The header game object.
         */
        public static GameObject GetHeader(string name)
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Header/" + name);
        }

        /**
         * Get the controls panel.
         * @return The controls panel.
         */
        public static GameObject GetControlsPanel()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Panel_Controls/SlotScrollRect(VIEW)/LayoutGroup_Controls");
        }

    }
}
