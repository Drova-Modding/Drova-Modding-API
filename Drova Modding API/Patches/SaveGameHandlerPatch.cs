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
/// <c>LoadSavegameFromSavegame</c>. <c>SetCurrentSavegameIntern</c>, shared by both paths, is inlined, so it cannot be patched.
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
/// Fires <see cref="SaveGameSystem.AfterSaveGameLoaded"/> and loads the registered stores. Every load path, sync, async and
/// after a scene change, runs <c>LoadFloatVars</c> directly before the game's own after-loaded event.
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
/// Fires <see cref="SaveGameSystem.BeforeSaveGameSaving"/> once per save. <c>SaveCurrentAs</c> is the private funnel
/// every save, autosave included, passes through; the public entry points miss autosaves and chain into each other.
/// </summary>
[HarmonyPatch(typeof(SavegameGameHandler), nameof(SavegameGameHandler.SaveCurrentAs), [typeof(SavegameIdentifier), typeof(SaveOptions), typeof(bool)])]
internal static class SaveGameHandlerSaveFunnelPatch
{
    private static void Prefix(SavegameGameHandler __instance)
    {
        SaveGameSystem.TriggerBeforeSaveGameSaving(__instance.CurrentSavegame);
    }
}
