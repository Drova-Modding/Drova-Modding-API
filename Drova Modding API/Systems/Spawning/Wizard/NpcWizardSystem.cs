using Drova_Modding_API.Access;
using Il2CppCommandTerminal;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Runtime NPC wizard that can be toggled by cheat command and writes external NPC definitions.
    /// </summary>
    internal static class NpcWizardSystem
    {
        private const string RuntimeObjectName = "ModdingAPI_NpcWizard";
        private static bool _initialized;

        internal static void Initialize()
        {
            if (_initialized)
                return;

            GameObject root = GameObject.Find(RuntimeObjectName);
            if (root == null)
            {
                root = new GameObject(RuntimeObjectName);
                UnityEngine.Object.DontDestroyOnLoad(root);
            }

            if (root.GetComponent<NpcWizardUI>() == null)
                root.AddComponent<NpcWizardUI>();

            CheatMenuAccess.RegisterCheat(
                "npc_wizard",
                ToggleWizardCommand,
                0,
                0,
                "Opens or closes the NPC placement wizard.",
                "npc_wizard");

            _initialized = true;
        }

        [HideFromIl2Cpp]
        private static void ToggleWizardCommand(Il2CppReferenceArray<CommandArg> _)
        {
            NpcWizardUI.Toggle();
        }
    }
}






