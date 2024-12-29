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

        private GVarList _currentSelectedGvarList;
        private List<AGVarBase> _selecteableGvars;
        private AGVarBase _currentSelectedGvar;

        /// <summary>
        /// The current selected Gvar. Can safe cast to the type of <see cref="GvarType"/>.
        /// </summary>
        public AGVarBase? CurrentSelectedGvar => _currentSelectedGvar;

        /// <summary>
        /// The number of options in the list dropdown.
        /// </summary>
        public int OptionsCountList => _GvarListDropdown.OptionsCount;

        /// <summary>
        /// The number of options in the value dropdown. Can be null if no value dropdown is shown.
        /// </summary>
        public int? OptionsCountValue => _GvarValueDropdown?.OptionsCount;


        private GUIDropdownWithFilter _GvarValueDropdown;

        /// <summary>
        /// Initializes a new instance of the <see cref="GUIGvarSelectionEditor"/> class.
        /// </summary>
        /// <param name="gvarType">Which gvar type to use</param>
        public GUIGvarSelectionEditor(GvarType gvarType)
        {
            _gvarType = gvarType;
            _subDatabaseGVars = ProviderAccess.GetGameDatabase().GVarDatabase;
            _GvarListDropdown = new GUIDropdownWithFilter(_subDatabaseGVars.AllGVars.ToArray().Select(e => e.name).ToArray(), -1, 20);
        }

        private void OnGvarListSelected()
        {
            _currentSelectedGvarList = _subDatabaseGVars.AllGVars[_GvarListDropdown.SelectedIndex];
            switch (_gvarType)
            {
                case GvarType.INT:
                    _selecteableGvars = _currentSelectedGvarList.GetVarsOfType<GInt>().ToArray().ToList().OfType<AGVarBase>().ToList();
                    _GvarValueDropdown = new GUIDropdownWithFilter(_selecteableGvars.Select(e => e.name).ToArray(), -1, 10);
                    break;
                case GvarType.FLOAT:
                    _selecteableGvars = _currentSelectedGvarList.GetVarsOfType<GFloat>().Cast<Il2CppSystem.Collections.Generic.List<AGVarBase>>().ToArray().ToList();
                    _GvarValueDropdown = new GUIDropdownWithFilter(_selecteableGvars.Select(e => e.name).ToArray(), -1, 10);
                    break;
                case GvarType.STRING:
                    _selecteableGvars = _currentSelectedGvarList.GetVarsOfType<GString>().Cast<Il2CppSystem.Collections.Generic.List<AGVarBase>>().ToArray().ToList();
                    _GvarValueDropdown = new GUIDropdownWithFilter(_selecteableGvars.Select(e => e.name).ToArray(), -1, 10);
                    break;
                case GvarType.BOOL:
                    _selecteableGvars = _currentSelectedGvarList.GetVarsOfType<GBool>().Cast<Il2CppSystem.Collections.Generic.List<AGVarBase>>().ToArray().ToList();
                    _GvarValueDropdown = new GUIDropdownWithFilter(_selecteableGvars.Select(e => e.name).ToArray(), -1, 10);
                    break;
            }
        }

        /// <summary>
        /// Draws the Gvar editor.
        /// </summary>
        /// <param name="listDropdown">list dropdown rect</param>
        /// <param name="gvarsDropdown">value dropdown rect</param>
        /// <returns>Whether the gvar value was changed</returns>
        public bool DrawGvarEditor(Rect listDropdown, Rect gvarsDropdown)
        {
            bool result = false;
            GUI.Label(new Rect(0, 0, 100, 20), "Gvarlist Name:");
            if (_GvarListDropdown.Draw(listDropdown))
            {
                OnGvarListSelected();
            }
            GUI.Label(new Rect(0, 30, 100, 20), "Gvar Name:");
            if (_GvarValueDropdown != null && _GvarValueDropdown.Draw(gvarsDropdown))
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
        BOOL
    }
}
