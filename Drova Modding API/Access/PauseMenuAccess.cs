using Il2Cpp;
using Il2CppDrova.GUI;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Buttons a mod adds to the in-game pause menu. Each is a copy of the game's own "Load latest" button, so it looks
    /// and navigates like the rest; it sits below that button, in the order the mods registered them.
    /// </summary>
    public static class PauseMenuAccess
    {
        private sealed record Entry(string Id, Func<string> Label, Action OnClick, Func<bool>? Visible, Func<bool>? Interactable, bool CloseMenu);

        private static readonly List<Entry> Entries = [];

        /// <summary>
        /// Adds a button to the pause menu. Everything but the click is asked again each time the menu opens.
        /// </summary>
        /// <param name="id">Unique per button; registering the same id again replaces it.</param>
        /// <param name="label">The button's text. Plain text, not a localization key.</param>
        /// <param name="onClick">What the button does.</param>
        /// <param name="visible">Whether it shows at all this time; null for always.</param>
        /// <param name="interactable">Whether it can be pressed this time; null for always.</param>
        /// <param name="closeMenu">Close the pause menu before <paramref name="onClick"/> runs.</param>
        public static void AddButton(string id, Func<string> label, Action onClick, Func<bool>? visible = null, Func<bool>? interactable = null, bool closeMenu = true)
        {
            Entries.RemoveAll(entry => entry.Id == id);
            Entries.Add(new Entry(id, label, onClick, visible, interactable, closeMenu));
        }

        /// <summary>Called when any window is shown; acts on the pause menu only.</summary>
        internal static void OnWindowShown(GUI_Window window)
        {
            if (Entries.Count == 0) return;

            GUI_Window_PauseMenu? menu = window.TryCast<GUI_Window_PauseMenu>();
            Button? template = menu == null ? null : menu._loadLatestButton;
            if (menu == null || template == null) return;

            Transform parent = template.transform.parent;
            int index = template.transform.GetSiblingIndex();

            foreach (Entry entry in Entries)
            {
                try
                {
                    index++;
                    Button button = Find(parent, entry.Id) ?? Create(template, parent, entry, menu);
                    button.transform.SetSiblingIndex(index);

                    bool visible = entry.Visible?.Invoke() ?? true;
                    button.gameObject.SetActive(visible);
                    if (!visible) continue;

                    button.interactable = entry.Interactable?.Invoke() ?? true;
                    TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null) text.text = entry.Label();
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"[PauseMenu] button '{entry.Id}' failed: {e}");
                }
            }
        }

        /// <summary>
        /// The menu hides its own buttons when a window opens over it (Load, Options) and shows them again after; the
        /// mod buttons follow, or they stayed up and selectable behind that window.
        /// </summary>
        internal static void OnPauseMenuHidden(GUI_Window_PauseMenu menu, bool hide)
        {
            if (Entries.Count == 0 || menu == null || menu._loadLatestButton == null) return;

            if (!hide)
            {
                OnWindowShown(menu);
                return;
            }

            Transform parent = menu._loadLatestButton.transform.parent;
            foreach (Entry entry in Entries)
            {
                Button? button = Find(parent, entry.Id);
                if (button != null) button.gameObject.SetActive(false);
            }
        }

        private static string NameOf(string id) => "ModButton_" + id;

        private static Button? Find(Transform parent, string id)
        {
            Transform? child = parent.Find(NameOf(id));
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static Button Create(Button template, Transform parent, Entry entry, GUI_Window_PauseMenu menu)
        {
            GameObject copy = Object.Instantiate(template.gameObject, parent);
            copy.name = NameOf(entry.Id);

            // The copy keeps the template's localized label, which would put "Load latest" back on every enable.
            LocalizedTextMeshPro localized = copy.GetComponentInChildren<LocalizedTextMeshPro>();
            if (localized != null) Object.Destroy(localized);

            Button button = copy.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            // Looked up at click time, so registering the id again changes what an existing button does too.
            string id = entry.Id;
            button.onClick.AddListener(new Action(() =>
            {
                Entry? current = Entries.Find(candidate => candidate.Id == id);
                if (current == null) return;
                if (current.CloseMenu) menu.CloseWindow();
                current.OnClick();
            }));

            return button;
        }
    }
}
