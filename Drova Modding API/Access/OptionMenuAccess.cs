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
        private static readonly OptionMenuAccess _instance = new();
        public static OptionMenuAccess Instance { get; } = _instance;

        public delegate void OptionMenuOpenAction();
        /**
        * This event is used to react on option menu open or closes, this can happen from the pause menu and the game main menu.
        */
        public event OptionMenuOpenAction OnOptionMenuOpen;

        internal static void OnOptionRoot()
        {
            _instance.OnOptionMenuOpen.Invoke();
        }

        /**
         * Add a header and panel to the option menu and set a icon. This is not persistent and will be removed when the option menu is closed.
         * @param icon The icon of the header (not the background).
         * @param name The name of the header. It needs to be unique and named liked: GUI_Button_OptionTab_YOUROPTION.
         * @return The created header game object.
         */
        public GameObject AddPanel(Sprite icon, string name)
        {
            if(!name.StartsWith("GUI_Button_OptionTab_"))
            {
                MelonLogger.Error("The name of the header needs to start with GUI_Button_OptionTab_");
                return null;
            }
            GameObject root = GetRootOfHeader();
            GameObject newHeader = GameObject.Instantiate(root.transform.GetChild(0).gameObject, root.transform);
            Image image = newHeader.transform.GetChild(0).GetComponent<Image>();
            newHeader.name = name;
            image.m_OverrideSprite = icon;
            image.sprite = icon;
            image.MarkDirty();
            var panel = this.AddPanel(newHeader);
            var componentToRegister = newHeader.GetComponent<GUI_ButtonNavigationAnimationElement>();
            
            root.GetComponent<GUI_ButtonNavigationAnimation>().AddElement(componentToRegister);
            GetGUIWindow().GetComponent<GUI_GameMenu_NavigationSwitcher>().legacyElements.Add(componentToRegister);
            OverrideNavigation(newHeader, panel);
            return newHeader;
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
            var newPanel = GameObject.Instantiate(root.transform.GetChild(4).gameObject, root.transform);
            newPanel.name = "Panel" + header.name.Substring(header.name.LastIndexOf('_'));
            newPanel.SetActive(false);
            return newPanel;
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

    }
}
