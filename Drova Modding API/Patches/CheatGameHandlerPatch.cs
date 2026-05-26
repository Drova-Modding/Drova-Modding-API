using Drova_Modding_API.Access;
using HarmonyLib;
using Il2CppDrova;

namespace Drova_Modding_API.Patches
{
    /// <summary>
    /// Hooks into <see cref="CheatGameHandler.EnableCheatMode"/> so that cheats pre-registered
    /// via <see cref="CheatMenuAccess.RegisterCheat"/> are flushed the moment cheat mode
    /// becomes active.
    /// </summary>
    [HarmonyPatch(typeof(CheatGameHandler), nameof(CheatGameHandler.EnableCheatMode))]
    internal static class CheatGameHandlerPatch
    {
        static void Postfix(bool enable)    
        {
            if (enable)
            {
                CheatMenuAccess.NotifyCheatModeEnabled();
            }
        }
    }
}

