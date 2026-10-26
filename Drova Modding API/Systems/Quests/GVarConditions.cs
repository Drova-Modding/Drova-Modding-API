using System.Globalization;
using Drova_Modding_API.Access;
using Drova_Modding_API.GlobalFields;
using Drova_Modding_API.Systems.GlobalVars;
using Drova_Modding_API.Systems.SaveGame;
using Il2CppDrova.GlobalVarSystem;
using Il2CppDrova.QuestSystem;
using MelonLoader;

namespace Drova_Modding_API.Systems.Quests
{
    /// <summary>
    /// Reads and writes game variables by list and id, and tells placements when the variables they wait for may have changed.
    /// </summary>
    public static class GVarConditions
    {
        /// <summary>
        /// A quest state or bool changed in a ready world, outside save loading. Placements re-check their conditions on it.
        /// Int changes have no event in the game and are only seen on the next change or world load.
        /// </summary>
        public static event Action? Changed;

        private static bool _worldReady;
        private static bool _dirty;

        /// <summary>
        /// Whether every condition holds. No conditions always hold.
        /// </summary>
        public static bool AllMet(IReadOnlyList<GVarCondition>? conditions)
        {
            if (conditions == null)
            {
                return true;
            }

            foreach (GVarCondition condition in conditions)
            {
                if (!IsMet(condition))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Whether a placement with these conditions should exist now. One without conditions always does. One with
        /// conditions waits for the world to be ready, because a save's values are only restored by then.
        /// </summary>
        public static bool AreMetForPlacement(IReadOnlyList<GVarCondition>? conditions)
            => conditions == null || conditions.Count == 0 || (_worldReady && AllMet(conditions));

        /// <summary>
        /// Whether the variable has the condition's value. A missing variable never matches.
        /// </summary>
        public static bool IsMet(GVarCondition condition)
        {
            bool negate = condition.Value.StartsWith('!');
            string expected = negate ? condition.Value[1..].Trim() : condition.Value.Trim();
            string? actual = ReadAsString(Find(condition.GVarList, condition.Var));
            if (actual == null)
            {
                return false;
            }

            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase) != negate;
        }

        /// <summary>
        /// The variable with that id in the named list, or null.
        /// </summary>
        public static AGVarBase? Find(string gvarList, string var)
        {
            GVarList? list = ProviderAccess.GVarDatabase?.GetGVarListByName(gvarList);
            if (list == null)
            {
                return null;
            }

            return string.Equals(var, GVarList.QUESTSTATE_VARNAME, StringComparison.OrdinalIgnoreCase) && list.IsQuestVarList
                ? list.GetQuestState()
                : list.GetGVarById(var);
        }

        /// <summary>
        /// Sets a bool through the game's own setter, so observers, dialogues and saves see it like a story write.
        /// </summary>
        /// <returns>False when the list has no such bool.</returns>
        public static bool TrySetBool(string gvarList, string var, bool value)
        {
            AGVarBase? gvar = Find(gvarList, var);
            AGVar<bool>? gbool = gvar != null ? gvar.TryCast<AGVar<bool>>() : null;
            if (gbool == null)
            {
                MelonLogger.Warning($"[GVarConditions] No bool '{var}' in list '{gvarList}'.");
                return false;
            }

            gbool.SetValue(value, GVarContext.Empty);
            return true;
        }

        internal static void Initialize()
        {
            GameEvents.OnQuestStateChanged += (_, _) => Raise();
            GvarBusSystem.OnGBoolValueChanged += _ => Raise();
            PlayerAccess.OnPlayerFound += _ =>
            {
                _worldReady = true;
                Raise();
            };

            // Leaving a world fires variable changes while its objects are torn down. A placement spawned then
            // wakes up in the dying world and is never cleaned up, so placements stop before the load begins.
            SaveGameSystem.BeforeSaveGameLoaded += _ => _worldReady = false;
            SceneStreamAccess.OnSceneUnloaded += sceneName =>
            {
                if (sceneName == SceneNames.Actor)
                {
                    _worldReady = false;
                }
            };
        }

        /// <summary>
        /// A world starts loading: conditional placements wait until the player is found.
        /// </summary>
        internal static void BeginWorldLoad() => _worldReady = false;

        /// <summary>
        /// Many variables can change in one frame (a dialogue node, a quest step), so placements refresh once per frame.
        /// </summary>
        private static void Raise() => _dirty = true;

        /// <summary>
        /// Fires <see cref="Changed"/> once for all changes since the last frame. Called from the API melon's update.
        /// </summary>
        internal static void Tick()
        {
            if (!_dirty || !_worldReady || AGVarBase.IsGVarLoading)
            {
                return;
            }

            _dirty = false;
            try
            {
                Changed?.Invoke();
            }
            catch (Exception e)
            {
                MelonLogger.Error("[GVarConditions] A placement refresh failed: " + e);
            }
        }

        private static string? ReadAsString(AGVarBase? gvar)
        {
            if (gvar == null)
            {
                return null;
            }

            if (gvar.TryCast<GQuestState>() != null)
            {
                return QuestStateReader.TryRead(gvar, out QuestState state) ? state.ToString() : null;
            }

            AGVar<bool>? gbool = gvar.TryCast<AGVar<bool>>();
            if (gbool != null)
            {
                return gbool.GetValue() ? "true" : "false";
            }

            AGVar<int>? gint = gvar.TryCast<AGVar<int>>();
            return gint != null ? gint.GetValue().ToString(CultureInfo.InvariantCulture) : null;
        }
    }
}
