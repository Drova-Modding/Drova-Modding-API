using Drova_Modding_API.Access;
using MelonLoader;

namespace Drova_Modding_API.Systems.Audio
{
    internal static class AudioLog
    {
        private static bool ShouldLog()
        {
#if DEBUG
            return ConfigAccessor.TryGetConfigValue(ModdingUI.ModdingUI.EnableDialogueAudioOptionKey, out bool enabled) && enabled;
#else
            return false;
#endif
        }

        internal static void Msg(string message)
        {
            if (ShouldLog()) MelonLogger.Msg(message);
        }

        internal static void Warning(string message)
        {
            if (ShouldLog()) MelonLogger.Warning(message);
        }

        internal static void Warning(string format, params object[] args)
        {
            if (ShouldLog()) MelonLogger.Warning(format, args);
        }

        internal static void Error(string message)
        {
            if (ShouldLog()) MelonLogger.Error(message);
        }
    }
}
