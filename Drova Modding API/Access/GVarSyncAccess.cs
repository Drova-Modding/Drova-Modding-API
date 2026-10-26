using System.Diagnostics.CodeAnalysis;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Declares how global variables cross in a co-op session, which synchronizes GVar writes by default. Mark
    /// per-player state, like a class choice, as local. Names are asset names, compared ordinally. Declare during
    /// melon initialization. Marking costs nothing when no co-op mod is installed.
    /// </summary>
    public static class GVarSyncAccess
    {
        private static readonly HashSet<string> LocalLists = new(StringComparer.Ordinal);
        private static readonly HashSet<string> LocalVars = new(StringComparer.Ordinal);
        private static readonly HashSet<string> SharedJourneys = new(StringComparer.Ordinal);
        private static readonly HashSet<string> StorySequences = new(StringComparer.Ordinal);
        private static readonly HashSet<string> LoudVars = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> SequenceQuestLists = new(StringComparer.Ordinal);

        /// <summary>
        /// Declares every variable in a GVar list as local to this player.
        /// </summary>
        /// <param name="listName">The list's asset name.</param>
        public static void MarkListLocal(string listName)
        {
            if (string.IsNullOrEmpty(listName)) return;

            LocalLists.Add(listName);
        }

        /// <summary>
        /// Declares one variable as local to this player.
        /// </summary>
        /// <param name="listName">The owning list's asset name.</param>
        /// <param name="varName">The variable's asset name.</param>
        public static void MarkVarLocal(string listName, string varName)
        {
            if (string.IsNullOrEmpty(listName) || string.IsNullOrEmpty(varName)) return;

            LocalVars.Add(Key(listName, varName));
        }

        /// <summary>
        /// Declares that a variable's transition, listeners included, plays out on every machine, for forced story
        /// sequences that should capture every player. A synchronized variable otherwise crosses as its value alone.
        /// A join catch-up skips these, since the value alone would re-stage the gate around the joining player.
        /// </summary>
        /// <param name="listName">The owning list's asset name.</param>
        /// <param name="varName">The variable's asset name.</param>
        public static void MarkJourneyShared(string listName, string varName)
        {
            if (string.IsNullOrEmpty(listName) || string.IsNullOrEmpty(varName)) return;

            SharedJourneys.Add(Key(listName, varName));
        }

        /// <summary>
        /// Declares a GVar list as the state of a scripted story sequence. The game's graphs poll such a list
        /// continuously, so copied into a joining player's world it re-enacts the scene from the top, even when
        /// finished. It never rides in a join catch-up, while live changes still cross.
        /// </summary>
        /// <param name="listName">The list's asset name.</param>
        /// <param name="questListName">
        /// The list whose quest-state variable says whether this sequence is running, for stage lists that carry
        /// none of their own, such as a quest arc's per-day lists.
        /// </param>
        public static void MarkStorySequence(string listName, string? questListName = null)
        {
            if (string.IsNullOrEmpty(listName)) return;

            StorySequences.Add(listName);

            if (!string.IsNullOrEmpty(questListName))
            {
                SequenceQuestLists[listName] = questListName!;
            }
        }

        /// <summary>
        /// The list whose quest state governs a stage list, when one was declared.
        /// </summary>
        /// <param name="listName">The stage list's asset name.</param>
        /// <param name="questListName">The governing list's asset name.</param>
        public static bool TryGetSequenceQuestList(string listName, [NotNullWhen(true)] out string? questListName)
        {
            questListName = null;

            return !string.IsNullOrEmpty(listName)
                && SequenceQuestLists.TryGetValue(listName, out questListName);
        }

        /// <summary>
        /// Whether a list has been declared a scripted story sequence.
        /// </summary>
        /// <param name="listName">The list's asset name.</param>
        public static bool IsStorySequence(string listName)
        {
            return !string.IsNullOrEmpty(listName) && StorySequences.Contains(listName);
        }

        /// <summary>
        /// Every declared story-sequence list, by asset name.
        /// </summary>
        public static IEnumerable<string> StorySequenceLists => StorySequences;

        /// <summary>
        /// Declares that a variable's live transition plays out loudly on every machine, while a join catch-up
        /// applies its value silently. For world milestones like a chapter change. Unlike
        /// <see cref="MarkJourneyShared"/>, the value still rides in a catch-up.
        /// </summary>
        /// <param name="listName">The owning list's asset name.</param>
        /// <param name="varName">The variable's asset name.</param>
        public static void MarkLoud(string listName, string varName)
        {
            if (string.IsNullOrEmpty(listName) || string.IsNullOrEmpty(varName)) return;

            LoudVars.Add(Key(listName, varName));
        }

        /// <summary>
        /// Whether a variable's live transition should play out loudly on every machine.
        /// </summary>
        /// <param name="listName">The owning list's asset name.</param>
        /// <param name="varName">The variable's asset name.</param>
        public static bool IsLoud(string listName, string varName)
        {
            return !string.IsNullOrEmpty(listName)
                && !string.IsNullOrEmpty(varName)
                && LoudVars.Contains(Key(listName, varName));
        }

        /// <summary>
        /// Every declared journey-shared variable, as (list name, variable name) pairs.
        /// </summary>
        public static IEnumerable<(string ListName, string VarName)> JourneySharedVars
        {
            get
            {
                foreach (string key in SharedJourneys)
                {
                    int split = key.IndexOf('\n');
                    yield return (key[..split], key[(split + 1)..]);
                }
            }
        }

        /// <summary>
        /// Whether a variable's transition should play out on every machine.
        /// </summary>
        /// <param name="listName">The owning list's asset name.</param>
        /// <param name="varName">The variable's asset name.</param>
        public static bool IsJourneyShared(string listName, string varName)
        {
            return !string.IsNullOrEmpty(listName)
                && !string.IsNullOrEmpty(varName)
                && SharedJourneys.Contains(Key(listName, varName));
        }

        /// <summary>
        /// Whether a variable has been declared local and must not be synchronized.
        /// </summary>
        /// <param name="listName">The owning list's asset name.</param>
        /// <param name="varName">The variable's asset name.</param>
        public static bool IsLocal(string listName, string varName)
        {
            if (string.IsNullOrEmpty(listName)) return false;

            if (LocalLists.Contains(listName)) return true;

            return !string.IsNullOrEmpty(varName) && LocalVars.Contains(Key(listName, varName));
        }

        /// <summary>
        /// The pair as one key. A separator no asset name contains, so two names cannot collide into
        /// the key of a different pair.
        /// </summary>
        private static string Key(string listName, string varName)
        {
            return listName + "\n" + varName;
        }
    }
}
