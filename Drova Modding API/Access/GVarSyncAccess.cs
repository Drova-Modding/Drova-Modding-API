namespace Drova_Modding_API.Access
{
    /// <summary>
    /// Where a mod declares global variables that belong to one player rather than to the world.
    ///
    /// A coop session synchronizes GVar writes so quest and world state converge, and that default is
    /// wrong for anything per-player a mod stores in a GVar. A class choice is the canonical case: two
    /// players need not both be mages, so the variable that says so must not cross. Marking it here
    /// keeps it at home. Marking costs nothing when no coop mod is installed.
    ///
    /// Names are the asset names the declaring mod authored, compared ordinally. Declare during melon
    /// initialization, before any session can exist.
    /// </summary>
    public static class GVarSyncAccess
    {
        private static readonly HashSet<string> _localLists = new(StringComparer.Ordinal);
        private static readonly HashSet<string> _localVars = new(StringComparer.Ordinal);
        private static readonly HashSet<string> _sharedJourneys = new(StringComparer.Ordinal);
        private static readonly HashSet<string> _storySequences = new(StringComparer.Ordinal);
        private static readonly HashSet<string> _loudVars = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> _sequenceQuestLists = new(StringComparer.Ordinal);

        /// <summary>
        /// Declares every variable in a GVar list as local to this player.
        /// </summary>
        /// <param name="listName">The list's asset name.</param>
        public static void MarkListLocal(string listName)
        {
            if (string.IsNullOrEmpty(listName)) return;

            _localLists.Add(listName);
        }

        /// <summary>
        /// Declares one variable as local to this player.
        /// </summary>
        /// <param name="listName">The owning list's asset name.</param>
        /// <param name="varName">The variable's asset name.</param>
        public static void MarkVarLocal(string listName, string varName)
        {
            if (string.IsNullOrEmpty(listName) || string.IsNullOrEmpty(varName)) return;

            _localVars.Add(Key(listName, varName));
        }

        /// <summary>
        /// Declares that a variable's transition should play out on every machine, not only the one
        /// that triggered it.
        ///
        /// A synchronised variable normally crosses as state alone: the far machine gets the value and
        /// none of the listeners, because a quest transition's listeners can be a cutscene and a
        /// teleport that were authored for the player standing at the trigger. A forced story sequence
        /// is the exception. When the story captures one player, it should capture all of them, and
        /// that only happens if the transition runs everywhere. A join catch-up skips these variables
        /// entirely: their value alone re-stages the gate when its scene loads, so writing it into a
        /// joining player's world would capture them retroactively.
        /// </summary>
        /// <param name="listName">The owning list's asset name.</param>
        /// <param name="varName">The variable's asset name.</param>
        public static void MarkJourneyShared(string listName, string varName)
        {
            if (string.IsNullOrEmpty(listName) || string.IsNullOrEmpty(varName)) return;

            _sharedJourneys.Add(Key(listName, varName));
        }

        /// <summary>
        /// Declares a GVar list as the state of a scripted story sequence.
        ///
        /// Such a list is a stage script, not a record: its variables say where the player is held,
        /// which actors stand where, and which stage directions are armed, and the game's graphs poll
        /// those values continuously. Copied into another player's world they re-enact the whole
        /// scene there - the player is ported, actors move, the set is rebuilt. A finished sequence
        /// is no safer than a running one, because the graph's own progress lives in the scene and
        /// never crosses: a fresh graph handed a finished sequence's values runs the staging from the
        /// top. So a sequence list never rides in a join catch-up. Live changes still cross while
        /// players experience the sequence together.
        /// </summary>
        /// <param name="listName">The list's asset name.</param>
        /// <param name="questListName">
        /// The list whose quest-state variable says whether this sequence is running, for stage
        /// lists that carry none of their own. A quest arc keeps parallel per-day lists, and whether
        /// their live stage values may land is decided by the arc's quest, not by the day list.
        /// </param>
        public static void MarkStorySequence(string listName, string? questListName = null)
        {
            if (string.IsNullOrEmpty(listName)) return;

            _storySequences.Add(listName);

            if (!string.IsNullOrEmpty(questListName))
            {
                _sequenceQuestLists[listName] = questListName!;
            }
        }

        /// <summary>
        /// The list whose quest state governs a stage list, when one was declared.
        /// </summary>
        /// <param name="listName">The stage list's asset name.</param>
        /// <param name="questListName">The governing list's asset name.</param>
        public static bool TryGetSequenceQuestList(string listName, out string questListName)
        {
            questListName = string.Empty;

            return !string.IsNullOrEmpty(listName)
                && _sequenceQuestLists.TryGetValue(listName, out questListName!);
        }

        /// <summary>
        /// Whether a list has been declared a scripted story sequence.
        /// </summary>
        /// <param name="listName">The list's asset name.</param>
        public static bool IsStorySequence(string listName)
        {
            return !string.IsNullOrEmpty(listName) && _storySequences.Contains(listName);
        }

        /// <summary>
        /// Every declared story-sequence list, by asset name.
        /// </summary>
        public static IEnumerable<string> StorySequenceLists => _storySequences;

        /// <summary>
        /// Declares that a variable's live transition should play out loudly on every machine, while
        /// a join catch-up still applies its value silently.
        ///
        /// The mark for world milestones like a chapter change: the moment should be experienced by
        /// everyone connected, but a joining player receives it as settled state, not as an event.
        /// Journey-shared is the stronger cousin - its values never ride in a catch-up at all,
        /// because they enforce a player state on arrival.
        /// </summary>
        /// <param name="listName">The owning list's asset name.</param>
        /// <param name="varName">The variable's asset name.</param>
        public static void MarkLoud(string listName, string varName)
        {
            if (string.IsNullOrEmpty(listName) || string.IsNullOrEmpty(varName)) return;

            _loudVars.Add(Key(listName, varName));
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
                && _loudVars.Contains(Key(listName, varName));
        }

        /// <summary>
        /// Every declared journey-shared variable, as (list name, variable name) pairs.
        /// </summary>
        public static IEnumerable<(string ListName, string VarName)> JourneySharedVars
        {
            get
            {
                foreach (string key in _sharedJourneys)
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
                && _sharedJourneys.Contains(Key(listName, varName));
        }

        /// <summary>
        /// Whether a variable has been declared local and must not be synchronized.
        /// </summary>
        /// <param name="listName">The owning list's asset name.</param>
        /// <param name="varName">The variable's asset name.</param>
        public static bool IsLocal(string listName, string varName)
        {
            if (string.IsNullOrEmpty(listName)) return false;

            if (_localLists.Contains(listName)) return true;

            return !string.IsNullOrEmpty(varName) && _localVars.Contains(Key(listName, varName));
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
