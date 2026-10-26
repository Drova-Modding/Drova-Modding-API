using System.Diagnostics;
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Editor;
using Drova_Modding_API.Systems.GlobalVars;
using Drova_Modding_API.Systems.Items;
using Drova_Modding_API.Systems.MapEditor;
using Drova_Modding_API.Systems.Networking.Diagnostics;
using Drova_Modding_API.Systems.Spawning;
using Il2CppDrova;

namespace Drova_Modding_API.Systems.ModdingUI
{
    /// <summary>
    /// The API's own editors in the <see cref="ModdingTools"/> window. Debug builds only, like the editors.
    /// </summary>
    internal static class BuiltInModdingTools
    {
        internal static void Register()
        {
            ModdingTools.Register(new ModdingTool("npc_wizard", "NPC Wizard", "Place and configure NPCs, saved as JSON.", NpcWizardUI.Toggle)
            {
                IsAvailable = InWorld
            });
            ModdingTools.Register(new ModdingTool("edit_npc", "Edit an NPC", "Dialogue or routine of the NPC you click next.", EditNextClickedNpc)
            {
                IsAvailable = InWorld
            });
            ModdingTools.Register(new ModdingTool("gvar_inspector", "Global Variables (F6)", "Browse, edit and create game variables.", GlobalVarInspectorSystem.Toggle)
            {
                IsAvailable = InWorld
            });
            ModdingTools.Register(new ModdingTool("localization", "Localization", "Edit the texts your mod ships.", LocalizationEntriesEditorSystem.Toggle)
            {
                IsAvailable = InWorld
            });
            ModdingTools.Register(new ModdingTool("map_editor", "Map Editor", "Place environment props, saved as JSON.", MapEditorWindow.Toggle)
            {
                IsAvailable = InWorld
            });
            ModdingTools.Register(new ModdingTool("reload_content", "Reload Content", "Re-read items, texts and map layouts.", ReloadContent));
            ModdingTools.Register(new ModdingTool("network_stats", "Network Stats", "Co-op ping, throughput and dropped packets.", NetworkStatsView.Toggle));
            ModdingTools.Register(new ModdingTool("content_folder", "Open Content Folder", "Quests, NPCs, dialogues, placements and texts as files.",
                () => Process.Start("explorer.exe", Utils.SavePath)));
        }

        private static bool InWorld() => PlayerAccess.TryGetPlayer(out _);

        private static void ReloadContent()
        {
            LocalizationAccess.CreateLocalizationEntriesFromFolder();
            CustomItemSystem.Reload();
            MapLayoutSystem.Reload();
            ToastAccess.Show($"Reloaded {CustomItemSystem.Items.Count} item(s), the texts and {MapLayoutSystem.Objects.Count} map prop(s).");
        }

        /// <summary>
        /// NPC editing starts from a click on the NPC, which the editor HUD only takes while cheat mode is on.
        /// </summary>
        private static void EditNextClickedNpc()
        {
            CheatGameHandler? cheats = ProviderAccess.GetCheatGameHandler();
            if (cheats != null)
            {
                cheats.EnableCheatMode(true);
            }

            ToastAccess.Show("Click an NPC to edit its dialogue or routine.");
        }
    }
}
