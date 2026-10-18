using Il2CppRewired;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// This class is used to access inputs. It works with both controller and keyboard inputs.
    /// </summary>
    public static class InputAccess
    {
        /// <summary>
        /// Gets the current device in use
        /// </summary>
        public static Il2CppCustomFramework.InputHandling.InputSystem.EDevice CurrentDevice => Il2CppCustomFramework.InputHandling.InputSystem.CurrentDevice;

        /// <summary>
        /// True, if any button is down
        /// </summary>
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
        public static bool IsActionClicked(string actionName)
        {
            return Il2CppCustomFramework.InputHandling.InputSystem.IsActionClicked(actionName);
        }

        /// <summary>
        /// True, if the action with the given name is pressed
        /// </summary>
        public static bool IsActionPressed(string actionName)
        {
            return Il2CppCustomFramework.InputHandling.InputSystem.IsActionPressed(actionName);
        }

        /// <summary>
        /// True, if the action with the given name is released
        /// </summary>
        public static bool IsActionReleased(string actionName)
        {
            return Il2CppCustomFramework.InputHandling.InputSystem.IsActionReleased(actionName);
        }

        /// <summary>
        /// Returns a localized text for the given action name. For controller and mouse buttons it will return a formatted string
        /// which shows the action as an icon. For keyboard keys it will return the key name.
        /// </summary>
        public static string GetLocalizedAnimText(string actionName)
        {
            return Il2CppCustomFramework.InputHandling.InputSystem.GetLocalizedAnimText(actionName);
        }

        /// <summary>
        /// Toggles the gameplay action maps on or off
        /// </summary>
        /// <param name="state">true or false for on and off</param>
        public static void ToggleGameplayActionMaps(bool state)
        {
            Player player = ReInput.players.GetPlayer(0);
            player.controllers.maps.SetMapsEnabled(state, 0);
            player.controllers.maps.SetMapsEnabled(state, 2);
            player.controllers.maps.SetMapsEnabled(state, 3);
        }

        /// <summary>
        /// Start rebinding the input action with the given name. The action will be rebounded with the next input clicked.
        /// </summary>
        public static void RebindKeyboardInputAction(string actionName, Pole? pole = null, AxisRange axisRange = AxisRange.Full, Il2CppSystem.Action<InputMapper.InputMappedEventData>? onInputMappedFinishedCallback = null)
        {
            InputMapper inputMapper = new();
            inputMapper.add_InputMappedEvent(onInputMappedFinishedCallback);
            inputMapper.add_InputMappedEvent(new Action<InputMapper.InputMappedEventData>(SaveRemapping));
            inputMapper.options = new InputMapper.Options
            {
                checkForConflicts = false,
                allowKeyboardKeysWithModifiers = false,
                allowKeyboardModifierKeyAsPrimary = false,
                ignoreMouseXAxis = true,
                ignoreMouseYAxis = true,
                timeout = 0.0f,
                allowAxes = false
            };

            ActionElementMap actionMap = GetActionKeyboardElementMap(actionName, ConvertPole(pole));

            InputMapper.Context context = new()
            {
                actionName = actionName,
                actionElementMapToReplace = actionMap,
                controllerMap = actionMap.controllerMap,
                actionRange = axisRange
            };

            inputMapper.Start(context);
        }

        /// <summary>
        /// Get the action element map for the given action name and pole from the keyboard
        /// </summary>
        /// <param name="actionName">The registered action's name.</param>
        /// <param name="pole">Null, if action is a button. Use Pole.Positive or Pole.Negative to get the ActionElementMap for a specific axis direction</param>
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
        private static void SaveRemapping(InputMapper.InputMappedEventData data)
        {
            // Save the remapping
            ReInput.userDataStore.Save();
        }

        /// <summary>
        /// Convert Function from Pole? to IL2CppSystem.Nullable{Pole}
        /// </summary>
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
