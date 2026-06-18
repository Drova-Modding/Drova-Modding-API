namespace Drova_Modding_API.Systems.GlobalVars
{
    /// <summary>
    /// Event bus for global variable (Gvar) changes. Mods can subscribe to be notified when a tracked global variable value changes.
    /// </summary>
    public static class GvarBusSystem
    {
        /// <summary>
        /// Raised when a global bool variable changes. The argument describes the affected variable.
        /// </summary>
        public static Action<GvarChangeEvent>? OnGBoolValueChanged;

        internal static void RaiseGBoolChanged(string name, bool value)
        {
            OnGBoolValueChanged?.Invoke(new GvarChangeEvent()
            {
                Name = name,
                Value = value
            });
        }
        
        /// <summary>
        /// Describes a change to a global variable.
        /// </summary>
        public struct GvarChangeEvent
        {
            /// <summary>
            /// The new value of the variable.
            /// </summary>
            public bool Value;

            /// <summary>
            /// The name of the variable that changed.
            /// </summary>
            public string Name;
        }
    }
}