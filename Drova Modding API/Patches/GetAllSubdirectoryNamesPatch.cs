using System.IO;
using HarmonyLib;
using Il2CppFileHandling.FileSystems;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Drova_Modding_API.Patches
{
    /// <summary>
    /// Returns no subdirectories for a missing folder instead of letting Drova throw, catch and log a
    /// <see cref="DirectoryNotFoundException"/>. A new hero has no <c>Hero_NN</c> folder until its first save,
    /// so every pause menu open logged that warning until then.
    /// </summary>
    [HarmonyPatch(typeof(DefaultFileSaveSystemHelper), nameof(DefaultFileSaveSystemHelper.GetAllSubdirectoryNames))]
    internal static class GetAllSubdirectoryNamesPatch
    {
        static bool Prefix(string directory, ref Il2CppStringArray __result)
        {
            if (Directory.Exists(directory)) return true;

            __result = new Il2CppStringArray(0);
            return false;
        }
    }
}
