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
         * Add a header to the option menu and set a icon. This is not persistent and will be removed when the option menu is closed.
         * @param icon The icon of the header (not the background).
         * @return The created header game object.
         */
        public GameObject AddHeader(Sprite icon)
        {
            GameObject root = GetRootOfHeader();
            GameObject newHeader = GameObject.Instantiate(root.transform.GetChild(0).gameObject, root.transform);
            Image image = newHeader.transform.GetChild(0).GetComponent<Image>();
            image.m_OverrideSprite = icon;
            image.sprite = icon;
            image.MarkDirty();
            return newHeader;
            
        }

        /**
         * Get the root of the option window.
         * @return The root of the option window.
         */
        public GameObject GetRootOfOptionWindow()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel");
        }

        public GameObject GetRootOfHeader()
        {
            return GameObject.Find("SceneRoot/GUI_Window_Options(Clone)/Panel/Header");
        }

    }
}
