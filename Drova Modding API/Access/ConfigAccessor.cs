namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Wrapper around the config file to get your config values in game.
    /// </summary>
    public static class ConfigAccessor
    {
        /// <summary>
        /// Sets the value of a config key to the specified value.
        /// </summary>
        /// <typeparam name="T">Can be float, int, string, bool or a Enum</typeparam>
        /// <param name="key">Name of the option</param>
        /// <param name="value"></param>
        /// <returns>Returns your value or default if not found</returns>
        public static bool TryGetConfigValue<T>(string key, out T value)
        {
            try
            {
                if (typeof(T) == typeof(float))
                {
                    if (ProviderAccess.GetConfigGameHandler().GameplayConfig.ConfigFile.TryGetString(key, out string stringifiedValue))
                    {
                        value = (T)Convert.ChangeType(float.Parse(stringifiedValue), typeof(T));
                        return true;
                    }
                }
                else if (typeof(T) == typeof(int))
                {
                    if (ProviderAccess.GetConfigGameHandler().GameplayConfig.ConfigFile.TryGetInt(key, out int configValue))
                    {
                        value = (T)Convert.ChangeType(configValue, typeof(T));
                        return true;
                    }
                }
                else if (typeof(T) == typeof(bool))
                {
                    if (ProviderAccess.GetConfigGameHandler().GameplayConfig.ConfigFile.TryGetBool(key, out bool configValue))
                    {
                        value = (T)Convert.ChangeType(configValue, typeof(T));
                        return true;
                    }
                }
                else if (typeof(T) == typeof(string))
                {
                    if (ProviderAccess.GetConfigGameHandler().GameplayConfig.ConfigFile.TryGetString(key, out string stringifiedValue))
                    {
                        value = (T)Convert.ChangeType(stringifiedValue, typeof(T));
                        return true;
                    }
                }
                else if (typeof(T).IsEnum)
                {
                    if (ProviderAccess.GetConfigGameHandler().GameplayConfig.ConfigFile.TryGetString(key, out string stringifiedValue))
                    {
                        if (Enum.TryParse(typeof(T), stringifiedValue, out object result)) {
                            value = (T)result;
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error($"Failed to get config value for key {key} with type {typeof(T)}: {e}");
            }
            value = default;
            return false;
        }
    }
}
