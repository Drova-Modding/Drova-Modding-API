namespace Drova_Modding_API.Systems.Audio.Dialogue
{
    /// <summary>
    /// Decides which dialogue lines belong to the narrator instead of the actor the node carries.
    /// Drova has no narrator actor parameter. Narration is authored under the name of whatever prop
    /// or placeholder holds the tree, and a normally voiced actor also delivers narrated lines when
    /// the text is wrapped in asterisks or parentheses.
    /// </summary>
    internal static class NarratorActors
    {
        /// <summary>
        /// Actor name the narrator's clips are shipped under. <see cref="AssetBundleAudioProvider"/>
        /// lower-cases it into the bundle file name.
        /// </summary>
        public const string NARRATOR = "Narrator";

        // Actor parameter names the game authors narration under. 
        private static readonly HashSet<string> _narratorActors = new(StringComparer.OrdinalIgnoreCase)
        {
            "Actor",
            "Aldo_Corpse",
            "Beard_Red",
            "BeeHive",
            "BluePoo",
            "BlueVomit",
            "BrokenFence",
            "BrokenPlant",
            "Canary",
            "Critter_Cat_01",
            "Critter_Dog_01",
            "EntityInfo_Corpse_Citydungeon_0",
            "EntityInfo_Corpse_Citydungeon_1",
            "EntityInfo_Corpse_Citydungeon_2",
            "EntityInfo_Corpse_Citydungeon_3",
            "EntityInfo_Corpse_Cobweb_Wall",
            "EntityInfo_Corpse_Fleur",
            "EntityInfo_Corpse_Generic",
            "EntityInfo_Crime_Judge",
            "EntityInfo_GrowthEnhancer",
            "EntityInfo_HumanFight",
            "EntityInfo_MusicMachine",
            "EntityInfo_MysteriousLantern",
            "EntityInfo_MysteriousPodest_Fossil",
            "EntityInfo_Obsidian_CentralStation",
            "EntityInfo_Poo_Liesl",
            "EntityInfo_RedTowerPedestral",
            "EntityInfo_Schlund_Skeleton_Fossil",
            "EntityInfo_SkeletonWithChild",
            "EntityInfo_Template_PerceptibleObject",
            "FriedelsTable",
            "INSTIGATOR",
            "LeynasTable",
            "Mikesch",
            "MineCookingFire",
            "Oskar",
            "PigletAtRoot",
            "Piglet_Cay",
            "Piglet_Chris_Unknown",
            "Piglet_Han_Unknown",
            "Piglet_Julian_Unknown",
            "Piglet_Kai_Unknown",
            "SecondRoot",
            "Sheep",
            "SheepTrace",
            "SheepWool",
            "Sheep_Liesl",
            "Table_Brutus",
            "Unknown",
            "WRONG",
            "WellLadder_Name",
            "item_alraune_name",
        };

        /// <summary>
        /// Whether every line of this actor is narration.
        /// </summary>
        /// <param name="actorName">Actor name of the node, may be null</param>
        public static bool IsNarratorActor(string? actorName)
        {
            return actorName != null && _narratorActors.Contains(actorName);
        }

        /// <summary>
        /// Whether this line reads as narration regardless of who says it. Narrated text is wrapped
        /// in asterisks or parentheses, and a line carrying two asterisks embeds a narrated part.
        /// </summary>
        /// <param name="text">Localized line, may be null</param>
        public static bool IsNarratorText(string? text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            string trimmed = text.Trim();
            if (trimmed.Length < 2) return false;
            if (trimmed[0] == '(' && trimmed[^1] == ')') return true;

            return trimmed.Count(c => c == '*') >= 2;
        }

        /// <summary>
        /// Returns <see cref="NARRATOR"/> when the line is narration, otherwise the actor unchanged.
        /// Only the voice changes, never the clip ID, so narration keeps the ID its own actor built.
        /// </summary>
        /// <param name="actorName">Actor name of the node</param>
        /// <param name="text">Localized line</param>
        public static string Resolve(string actorName, string text)
        {
            return IsNarratorActor(actorName) || IsNarratorText(text) ? NARRATOR : actorName;
        }
    }
}
