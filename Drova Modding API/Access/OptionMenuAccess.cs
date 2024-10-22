using Drova_Modding_API.UI.Builder;
using Il2Cpp;
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
        private const string ScrollBarName = "ScrollRectView";
        private static readonly OptionMenuAccess _instance = new();
        /**
         * The instance of the option menu access.
         */
        public static OptionMenuAccess Instance { get; } = _instance;

        /**
         * Delegate for the option menu open action.
         */
        public delegate void OptionMenuOpenAction();

        /**
        * This event is used to react on option menu open or closes, this can happen from the pause menu and the game main menu.
        */
        public event OptionMenuOpenAction OnOptionMenuOpen;

        internal static void OnOptionRoot()
        {
            _instance.OnOptionMenuOpen.Invoke();
            var manager = _instance.GetGUIWindow().GetComponent<GUI_Window_Options>();
            // Workaround for Update 1.0.2.1 where the modded panel is not activated for whatever reason.
            if (manager._currentPanelIndex == -1)
            {
                manager.ChangePanel(0);
            }
        }

        /**
         * Add a header and panel to the option menu and set a icon. This is not persistent and will be removed when the option menu is closed.
         * @param icon The icon of the header (not the background).
         * @param name The name of the header. It needs to be unique and named liked: GUI_Button_OptionTab_YOUROPTION.
         * @return The created transfrom for the builder or if the Elements already exists null.
         */
        public Transform AddPanel(Sprite icon, string name)
        {
            if(!name.StartsWith("GUI_Button_OptionTab_"))
            {
                MelonLogger.Error("The name of the header needs to start with GUI_Button_OptionTab_");
                return null;
            }
            GameObject root = GetRootOfHeader();
            if (GetHeader(name)) return null;
            GameObject newHeader = GameObject.Instantiate(root.transform.GetChild(0).gameObject, root.transform);
            Image image = newHeader.transform.GetChild(0).GetComponent<Image>();
            newHeader.name = name;
            image.m_OverrideSprite = icon;
            image.sprite = icon;
            image.MarkDirty();
            var panel = this.AddPanel(newHeader);
            var componentToRegister = newHeader.GetComponent<GUI_ButtonNavigationAnimationElement>();

            var scrollbar = GetScrollBar(panel);
            scrollbar.name = ScrollBarName;
            var layoutGroup = scrollbar.transform.GetChild(0);
            layoutGroup.name = $"LayoutGroup{name.AsSpan(name.LastIndexOf('_'))}";
            layoutGroup.DestroyAllChildren();

            root.GetComponent<GUI_ButtonNavigationAnimation>().AddElement(componentToRegister);
            GetGUIWindow().GetComponent<GUI_GameMenu_NavigationSwitcher>().legacyElements.Add(componentToRegister);
            OverrideNavigation(newHeader, panel);
            return layoutGroup;
        }

        private GameObject GetScrollBar(GameObject panel)
        {
            return panel.transform.GetChild(1).gameObject;
        }

        /**
         * Add the header and panel to the navigatio system.
         * @param header The header to add.
         * @param panel The panel to add.
         */
        private void OverrideNavigation(GameObject header, GameObject panel)
        {
            var manager = GetGUIWindow().GetComponent<GUI_Window_Options>();
            var guiPanel = new GUI_Window_ATabManager.GUI_TabPanel
            {
                Instance = panel.GetComponent<GUI_GameMenu_APanel>(),
                Id = header.transform.GetSiblingIndex() + 1
            };
            guiPanel.OverrideNavigationElement(header.GetComponent<GUI_ButtonNavigationAnimationElement>());
            guiPanel.SetButtonName();

            manager.GuiPanels.Add(guiPanel);
            manager.SetupPanelNavigationElements();
            // Rerender panel, otherwise its empty. 
            manager.ChangePanel(0);
        }

        /**
         * Add a panel to the option menu.
         * @param header The header to add the panel to. It needs a name with GUI_Button_OptionTab_YOUROPTION
         * @return The created panel game object.
         */
        private GameObject AddPanel(GameObject header)
        {
            var root = GetRootOfOptionWindow();
            var newPanel = UnityEngine.Object.Instantiate(root.transform.GetChild(4).gameObject, root.transform);
            newPanel.transform.SetSiblingIndex(newPanel.transform.GetSiblingIndex() - 1);
            newPanel.name = string.Concat("Panel", header.name.AsSpan(header.name.LastIndexOf('_')));
            newPanel.SetActive(false);  
            return newPanel;
        }

        /**
         * Get the builder for the option menu.
         * @param panel The panel to add the elements to.
         * @return The builder for the option menu.
         */
        public OptionUIBuilder GetBuilder(Transform panel)
        {
            return new OptionUIBuilder(panel);
        }

        /**
         * Get the GUI window.
         * @return The root of the option window.
         */
        public GameObject GetGUIWindow()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)");
        }

        /**
         * Get the root of the panel where header and panels are.
         * @return The root of the option window.
         */
        public GameObject GetRootOfOptionWindow()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel");
        }

        /**
         * Get the root of the header.
         * @return The root of the header.
         */
        public GameObject GetRootOfHeader()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Header");
        }

        /**
         * Get the header by name.
         * @param name The name of the header.
         * @return The header game object.
         */
        public GameObject GetHeader(string name)
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Header/" + name);
        }

        /**
         * Get the controls panel.
         * @return The controls panel.
         */
        public GameObject GetControlsPanel()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Panel_Controls/SlotScrollRect(VIEW)/LayoutGroup_Controls");
        }

    }
}
