using Il2CppDrova.GlobalVarSystem;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Utils
{
    internal class GvarContainerInt(GInt value)
    {
        public GInt _Gvar = value;

        public void SetValue(int value)
        {
            _Gvar.SetValue(value, GVarContext.Empty);
        }

        public void Draw()
        {
            if (int.TryParse(GUILayout.TextField(_Gvar.GetValue().ToString()), out int newValue) && newValue != GetValue())
            {
                SetValue(newValue);
            }
        }

        public int GetValue()
        {
            return _Gvar.GetValue();
        }
    }
}
