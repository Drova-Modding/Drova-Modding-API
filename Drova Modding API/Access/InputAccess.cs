using Il2CppRewired;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// This class is used to access inputs. It works with both controller and keyboard inputs.
    /// </summary>
    public class InputAccess
    {
        /// <summary>
        /// Gets the current device in use
        /// </summary>
        public static Il2CppCustomFramework.InputHandling.InputSystem.EDevice CurrentDevice => Il2CppCustomFramework.InputHandling.InputSystem.CurrentDevice;

        /// <summary>
        /// True, if any button is down
        /// </summary>
        /// <returns></returns>
        public static bool AnyButtonDown()
        {
            return Il2CppCustomFramework.InputHandling.InputSystem.AnyButtonDown();
        }

        /// <summary>
        /// Returns the axis value of the given action
        /// </summary>
        public static float GetAxis(string actionName)
        {
            return Il2CppCustomFramework.InputHandling.InputSystem.GetAxis(actionName);
        }

        /// <summary>
        /// True, if the action with the given name is clicked
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static bool IsActionClicked(string actionName)
        {
            return Il2CppCustomFramework.InputHandling.InputSystem.IsActionClicked(actionName);
        }

        /// <summary>
        /// True, if the action with the given name is pressed
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static bool IsActionPressed(string actionName)
        {
            return Il2CppCustomFramework.InputHandling.InputSystem.IsActionPressed(actionName);
        }

        /// <summary>
        /// True, if the action with the given name is released
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static bool IsActionReleased(string actionName)
        {
            return Il2CppCustomFramework.InputHandling.InputSystem.IsActionReleased(actionName);
        }

        /// <summary>
        /// Returns a localized text for the given action name. For controller and mouse buttons it will return a formatted string
        /// which shows the action as an icon. For keyboard keys it will return the key name.
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static string GetLocalizedAnimText(string actionName)
        {
            return Il2CppCustomFramework.InputHandling.InputSystem.GetLocalizedAnimText(actionName);
        }

        /// <summary>
        /// Start rebinding the input action with the given name. The action will be rebound with the next input clicked.
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="pole"></param>
        /// <param name="axisRange"></param>
        /// <param name="onInputMappedFinishedCallback"></param>
        public static void RebindKeyboardInputAction(string actionName, Pole? pole = null, AxisRange axisRange = AxisRange.Full, Il2CppSystem.Action<InputMapper.InputMappedEventData> onInputMappedFinishedCallback = null)
        {
            var _inputMapper = new InputMapper();
            _inputMapper.add_InputMappedEvent(onInputMappedFinishedCallback);
            _inputMapper.add_InputMappedEvent(new System.Action<InputMapper.InputMappedEventData>(SaveRemapping));
            _inputMapper.options = new InputMapper.Options
            {
                checkForConflicts = false,
                allowKeyboardKeysWithModifiers = false,
                allowKeyboardModifierKeyAsPrimary = false,
                ignoreMouseXAxis = true,
                ignoreMouseYAxis = true,
                timeout = 0.0f,
                allowAxes = false
            };

            var actionMap = GetActionKeyboardElementMap(actionName, ConvertPole(pole));

            var context = new InputMapper.Context
            {
                actionName = actionName,
                actionElementMapToReplace = actionMap,
                controllerMap = actionMap.controllerMap,
                actionRange = axisRange
            };

            _inputMapper.Start(context);
        }

        /// <summary>
        /// Get the action element map for the given action name and pole from the keyboard
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="pole">Null, if action is a button. Use Pole.Positive or Pole.Negative to get the ActionElementMap for a specific axis direction</param>
        /// <returns></returns>
        internal static ActionElementMap GetActionKeyboardElementMap(string actionName, Il2CppSystem.Nullable<Pole> pole)
        {
            return GetActionElementMap(actionName, pole, new Il2CppSystem.Nullable<ControllerType>(ControllerType.Keyboard));
        }

        internal static ActionElementMap GetActionElementMap(string actionName, Il2CppSystem.Nullable<Pole> pole, Il2CppSystem.Nullable<ControllerType> controllerType)
        {
            return Il2CppCustomFramework.InputHandling.InputSystem.GetActionElementMapFromActionName(actionName, pole, controllerType);
        }

        /// <summary>
        /// Callback after the input is remapped to save the data to the player prefs
        /// </summary>
        /// <param name="data"></param>
        private static void SaveRemapping(InputMapper.InputMappedEventData data)
        {
            // Save the remapping
            ReInput.userDataStore.Save();
        }

        /// <summary>
        /// Convert Function from Pole? to IL2CppSystem.Nullable{Pole}
        /// </summary>
        /// <param name="pole"></param>
        /// <returns></returns>
        private static Il2CppSystem.Nullable<Pole> ConvertPole(Pole? pole)
        {
            if (pole == null)
            {
                return new Il2CppSystem.Nullable<Pole>();
            }

            switch (pole)
            {
                case Pole.Positive:
                    return new Il2CppSystem.Nullable<Pole>(Pole.Positive);
                case Pole.Negative:
                    return new Il2CppSystem.Nullable<Pole>(Pole.Negative);
                default:
                    return new Il2CppSystem.Nullable<Pole>();
            }
        }
    }
}
