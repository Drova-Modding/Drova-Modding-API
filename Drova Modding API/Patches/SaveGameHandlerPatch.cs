using Il2CppDrova;
using HarmonyLib;
using Drova_Modding_API.Systems.SaveGame;
using Il2CppDrova.Saveables;
using Il2Cpp;
using UnityEngine.SceneManagement;

[HarmonyPatch(typeof(SavegameGameHandler), nameof(SavegameGameHandler.LoadSavegameFromSavegame), [typeof(SavegameIdentifier), typeof(bool), typeof(Savegame)])]
internal static class SaveGameHandlerPatch
{
    private static void Prefix(SavegameIdentifier id, bool sceneLoad, ref Savegame savegame)
    {
        SaveGameSystem.TriggerBeforeSaveGameLoaded(savegame);
    }

    private static void Postfix(SavegameIdentifier id, bool sceneLoad, ref Savegame savegame, SavegameGameHandler __instance)
    {
        if (!sceneLoad)
        {
            string loadedSceneName = "";
            if (savegame.Data.GetActiveSceneName(ref loadedSceneName))
            {
                var loadedScene = SceneManager.GetSceneByName(loadedSceneName);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                {
                    sceneLoad = true;
                }
            }
        }
        if (sceneLoad)
        {
            SaveGameSystem.TriggerAfterSaveGameLoaded(__instance._currentSavegame);
        }
    }
}

internal class SaveGameHandlerSavePatch
{
    [HarmonyPatch(typeof(SavegameGameHandler), nameof(SavegameGameHandler.SaveCurrent), [])]
    private static class SaveGameHandlerSavePatch_Save
    {
        private static void Prefix(SavegameGameHandler __instance)
        {
            SaveGameSystem.TriggerBeforeSaveGameSaving(__instance.CurrentSavegame);
        }
    }
}

internal class SaveGameHandlerSavePatch2
{
    [HarmonyPatch(typeof(SavegameGameHandler), nameof(SavegameGameHandler.SaveCurrent), [typeof(SaveOptions)])]
    private static class SaveGameHandlerSavePatch_Save
    {
        private static void Prefix(SavegameGameHandler __instance)
        {
            SaveGameSystem.TriggerBeforeSaveGameSaving(__instance.CurrentSavegame);
        }
    }
}

internal class SaveGameHandlerSavePatch3
{
    [HarmonyPatch(typeof(SavegameGameHandler), nameof(SavegameGameHandler.SaveCurrentAt), [typeof(SavegameIdentifier)])]
    private static class SaveGameHandlerSavePatch_Save
    {
        private static void Prefix(SavegameGameHandler __instance)
        {
            SaveGameSystem.TriggerBeforeSaveGameSaving(__instance.CurrentSavegame);
        }
    }
}
