using Drova_Modding_API.Systems.SaveGame;
using HarmonyLib;
using Il2CppDrova;
using Il2CppDrova.Saveables;

/// <summary>
/// Fires <see cref="SaveGameSystem.BeforeSaveGameLoaded"/> for the async loads (the menus): <c>LoadSavegameFromSavegame</c>
/// is where they hand the read savegame over.
/// </summary>
[HarmonyPatch(typeof(SavegameGameHandler), nameof(SavegameGameHandler.LoadSavegameFromSavegame), [typeof(SavegameIdentifier), typeof(bool), typeof(Savegame)])]
internal static class SaveGameHandlerPatch
{
    private static void Prefix(Savegame savegame)
    {
        SaveGameSystem.TriggerBeforeSaveGameLoaded(savegame);
    }
}

/// <summary>
/// Fires <see cref="SaveGameSystem.BeforeSaveGameLoaded"/> for the synchronous load, which never passes through
/// <c>LoadSavegameFromSavegame</c>: its only way to a savegame is this read, right before the game's own
/// before-loading event. (<c>SetCurrentSavegameIntern</c>, which both paths call, is inlined - a patch there
/// applies and never runs.)
/// </summary>
[HarmonyPatch(typeof(SavegameLoader), nameof(SavegameLoader.TryLoadSavegameSync), [typeof(SavegameIdentifier)])]
internal static class SaveGameLoaderSyncPatch
{
    private static void Postfix(Savegame __result)
    {
        if (__result != null) SaveGameSystem.TriggerBeforeSaveGameLoaded(__result);
    }
}

/// <summary>
/// Fires <see cref="SaveGameSystem.AfterSaveGameLoaded"/> (and loads the registered stores) at the moment the game
/// raises its own after-loaded event: every load path runs <c>LoadFloatVars</c> directly before it - the synchronous
/// and async loads without a scene change, and the scene listener once a loaded scene arrives. The old postfix on
/// <c>LoadSavegameFromSavegame</c> missed the synchronous loads entirely and fired before a loaded scene existed.
/// </summary>
[HarmonyPatch(typeof(SavegameGameHandler), "LoadFloatVars")]
internal static class SaveGameHandlerLoadedPatch
{
    private static void Postfix(SavegameGameHandler __instance)
    {
        Savegame? savegame = __instance.CurrentSavegame;
        if (savegame != null) SaveGameSystem.TriggerAfterSaveGameLoaded(savegame);
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
