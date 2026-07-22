using Drova_Modding_API.Systems.SaveGame;
using HarmonyLib;
using Il2CppDrova;
using Il2CppDrova.Saveables;

[HarmonyPatch(typeof(SavegameGameHandler), nameof(SavegameGameHandler.LoadSavegameFromSavegame), [typeof(SavegameIdentifier), typeof(bool), typeof(Savegame)])]
internal static class SaveGameHandlerPatch
{
    private static void Prefix(SavegameIdentifier id, bool sceneLoad, ref Savegame savegame)
    {
        SaveGameSystem.TriggerBeforeSaveGameLoaded(savegame);
    }

    private static void Postfix(SavegameIdentifier id, bool sceneLoad, ref Savegame savegame, SavegameGameHandler __instance)
    {
        SaveGameSystem.TriggerAfterSaveGameLoaded(savegame);
    }
}

/// <summary>
/// Fires <see cref="SaveGameSystem.BeforeSaveGameSaving"/> for EVERY save. The patch target is
/// <c>SavegameGameHandler.SaveCurrentAs</c>, the private funnel that all public entry points
/// (<c>SaveCurrent()</c>, <c>SaveCurrent(SaveOptions)</c>, <c>SaveCurrentAt(...)</c>) and the
/// AUTOSAVE flow into, right before the savegame is serialized to disk.
///
/// The previous approach patched three public entry points instead, which had two field-verified
/// bugs: the autosave path (<c>SaveCurrentAt(id, options, ignoreRestriction)</c>) was not
/// covered, so registered stores were silently stale after an autosave-only session; and
/// <c>SaveCurrent()</c> chains into <c>SaveCurrent(SaveOptions)</c>, so manual saves fired the
/// event twice.
/// </summary>
[HarmonyPatch(typeof(SavegameGameHandler), nameof(SavegameGameHandler.SaveCurrentAs), [typeof(SavegameIdentifier), typeof(SaveOptions), typeof(bool)])]
internal static class SaveGameHandlerSaveFunnelPatch
{
    private static void Prefix(SavegameGameHandler __instance)
    {
        SaveGameSystem.TriggerBeforeSaveGameSaving(__instance.CurrentSavegame);
    }
}
