using Drova_Modding_API.Access;
using Il2CppDrova.GlobalVarSystem;
using System.Linq;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Utils
{
    /// <summary>
    /// Editor for Gvar selection.
    /// </summary>
    public class GUIGvarSelectionEditor
    {
        private readonly GvarType _gvarType;
        private readonly SubDatabase_GVars _subDatabaseGVars;
        private readonly GUIDropdownWithFilter _GvarListDropdown;
        private readonly bool _showOnlyList;

        private GVarList _currentSelectedGvarList;
        private List<AGVarBase> _selecteableGvars;
        private AGVarBase _currentSelectedGvar;
        private GUIDropdownWithFilter _GvarValueDropdown;

        /// <summary>
        /// The list dropdown.
        /// </summary>
        public GUIDropdownWithFilter GvarListDropdown => _GvarListDropdown;

        /// <summary>
        /// The value dropdown.
        /// </summary>
        public GUIDropdownWithFilter? GvarValueDropdown => _GvarValueDropdown;


        /// <summary>
        /// The current selected Gvar. Can safe cast to the type of <see cref="GvarType"/>.
        /// </summary>
        public AGVarBase? CurrentSelectedGvar => _currentSelectedGvar;

        /// <summary>
        /// The current selected GvarList.
        /// </summary>
        public GVarList? CurrentSelectedGvarList => _currentSelectedGvarList;

        /// <summary>
        /// The number of options in the list dropdown.
        /// </summary>
        public int OptionsCountList => _GvarListDropdown.OptionsCount;

        /// <summary>
        /// The number of options in the value dropdown. Can be null if no value dropdown is shown.
        /// </summary>
        public int? OptionsCountValue => _GvarValueDropdown?.OptionsCount;



        /// <summary>
        /// Initializes a new instance of the <see cref="GUIGvarSelectionEditor"/> class.
        /// </summary>
        /// <param name="gvarType">Which gvar type to use</param>
        /// <param name="nameOfList">Name of the list</param>
        /// <param name="showOnlyList">Whether only the list should be shown</param>
        /// <param name="selected">The selected gvar</param>
        public GUIGvarSelectionEditor(GvarType gvarType, string nameOfList = null, bool showOnlyList = false, AGVarBase selected = null)
        {
            _currentSelectedGvar = selected;
            _gvarType = gvarType;
            _showOnlyList = showOnlyList;
            _subDatabaseGVars = ProviderAccess.GetGameDatabase().GVarDatabase;

            var gvarLists = _subDatabaseGVars.AllGVars.ToArray().Select(e => e.name).ToArray();
            var selectedIndex = -1;
            if (nameOfList != null)
            {
                selectedIndex = gvarLists.ToList().IndexOf(nameOfList);
                _currentSelectedGvarList = _subDatabaseGVars.AllGVars[selectedIndex];
            }
            _GvarListDropdown = new GUIDropdownWithFilter(gvarLists, selectedIndex, 20);

            if (_showOnlyList || selectedIndex == -1) return;

            OnGvarListSelected();

            if (selected != null)
            {
                int selectionIndex = _selecteableGvars.FindIndex(gvar => gvar.Id == selected.Id);
                _GvarValueDropdown.SetSelectedIndex(selectionIndex);
            }
        }

        private void OnGvarListSelected()
        {
            _currentSelectedGvarList = _subDatabaseGVars.AllGVars[_GvarListDropdown.SelectedIndex];

            if (_showOnlyList) return;
            _selecteableGvars = _gvarType switch
            {
                GvarType.INT => _currentSelectedGvarList.GetVarsOfType<GInt>().ToArray().ToList().OfType<AGVarBase>().ToList(),
                GvarType.FLOAT => _currentSelectedGvarList.GetVarsOfType<GFloat>().ToArray().ToList().OfType<AGVarBase>().ToList(),
                GvarType.STRING => _currentSelectedGvarList.GetVarsOfType<GString>().ToArray().ToList().OfType<AGVarBase>().ToList(),
                GvarType.BOOL => _currentSelectedGvarList.GetVarsOfType<GBool>().ToArray().ToList().OfType<AGVarBase>().ToList(),
                _ => [],
            };
            _GvarValueDropdown = new GUIDropdownWithFilter(_selecteableGvars.Select(e => e.name).ToArray(), -1, 10);

        }

        /// <summary>
        /// Draws the Gvar editor.
        /// </summary>
        /// <param name="listDropdown">list dropdown rect</param>
        /// <param name="gvarsDropdown">value dropdown rect</param>
        /// <returns>Whether the gvar value was changed, if <see cref="_showOnlyList"/> is true, than wheter the list changed</returns>
        public bool DrawGvarEditor(Rect listDropdown, Rect gvarsDropdown = default)
        {
            bool result = false;
            GUI.Label(new Rect(listDropdown.x, listDropdown.y, 100, 20), "Gvarlist Name:");
            if (_GvarListDropdown.Draw(new Rect(listDropdown.x + 110, listDropdown.y, listDropdown.width, listDropdown.height)))
            {
                OnGvarListSelected();
                result = _showOnlyList;
            }
            if (_showOnlyList) return result;
            GUI.Label(new Rect(gvarsDropdown.x, gvarsDropdown.y, 100, 20), "Gvar Name:");
            if (_GvarValueDropdown != null && _GvarValueDropdown.Draw(new Rect(gvarsDropdown.x + 110, gvarsDropdown.y, gvarsDropdown.width, gvarsDropdown.height)))
            {
                _currentSelectedGvar = _selecteableGvars[_GvarValueDropdown.SelectedIndex];
                result = true;
            }
            return result;
        }
    }

    /**
     * Represents the type of a Gvar which can be used in the editor.
     */
    public enum GvarType
    {
        /// <summary>
        /// integer type
        /// </summary>
        INT,
        /// <summary>
        /// float type
        /// </summary>
        FLOAT,
        /// <summary>
        /// string type
        /// </summary>
        STRING,
        /// <summary>
        /// boolean type
        /// </summary>
        BOOL,
        /// <summary>
        /// no type
        /// </summary>
        NONE
    }
}
