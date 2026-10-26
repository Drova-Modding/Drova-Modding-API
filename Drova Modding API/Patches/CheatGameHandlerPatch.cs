using Drova_Modding_API.Access;
using HarmonyLib;
using Il2CppDrova;

namespace Drova_Modding_API.Patches
{
    [HarmonyPatch(typeof(CheatGameHandler), nameof(CheatGameHandler.EnableCheatMode))]
    internal static class CheatGameHandlerPatch
    {
        static void Postfix(bool value)    
        {
            if (value)
            {
                CheatMenuAccess.NotifyCheatModeEnabled();
            }
        }
    }
}

