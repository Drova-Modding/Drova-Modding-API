﻿using Il2CppDrova.GUI;
using HarmonyLib;
using Drova_Modding_API.Access;

[HarmonyPatch(typeof(GUI_Window_Options), "CloseWindow")]
static class GUI_Window_Patch_Close
{
    private const string WindowName = "GUI_Window_Options(Clone)";
#pragma warning disable IDE0051 // Remove unused private members
    private static void Postfix(GUI_Window __instance)
#pragma warning restore IDE0051 // Remove unused private members
    {
        if (__instance.name == WindowName)
            OptionMenuAccess.OnOptionClose();
    }
}

[HarmonyPatch(typeof(GUI_Window), "ShowWindow")]
static class GUI_Window_Patch_Open
{
    private const string WindowName = "GUI_Window_Options(Clone)";
#pragma warning disable IDE0051 // Remove unused private members
    private static void Postfix(GUI_Window __instance)
#pragma warning restore IDE0051 // Remove unused private members
    {
        if (__instance.name == WindowName)
            OptionMenuAccess.OnOptionOpen();
    }
}

