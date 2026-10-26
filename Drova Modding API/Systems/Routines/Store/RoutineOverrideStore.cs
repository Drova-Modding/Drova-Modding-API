using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Drova_Modding_API.Access;
using Drova_Modding_API.Converters;
using Il2CppDrova;
using Il2CppDrova.Routine;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Drova_Modding_API.Systems.Routines.Store
{
    /// <summary>
    /// The routine changes for one NPC, stored as <c>Mods/Modding_API/Routines/&lt;Key&gt;.json</c>.
    /// </summary>
    public sealed class RoutineOverride
    {
        /// <summary>
        /// The GUID of the NPC's <see cref="EntityInfo"/>. Required: new places are created for it.
        /// </summary>
        public string EntityGuid { get; set; } = string.Empty;

        /// <summary>
        /// The actor's GUID, when the override is for exactly this NPC rather than every NPC sharing
        /// <see cref="EntityGuid"/>.
        /// </summary>
        public string? ActorGuid { get; set; }

        /// <summary>
        /// A readable name for the file; not used at runtime.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The <see cref="BehaviourTreeStore"/> id of the NPC's own routine graph, or null for the shared one.
        /// </summary>
        public string? GraphId { get; set; }

        /// <summary>
        /// Whether the NPC's own routine places in the game are switched off when the AI logic scene loads.
        /// </summary>
        public bool ReplaceExisting { get; set; }

        /// <summary>
        /// Routine places added to the NPC when the AI logic scene loads.
        /// </summary>
        public List<RoutinePlaceOverride> Places { get; set; } = [];

        /// <summary>
        /// Changes to routine places that already exist in the game, by place.
        /// </summary>
        public List<RoutinePlaceEdit> PlaceEdits { get; set; } = [];

        /// <summary>
        /// Fields in the file that this version does not know, usually a typo. Reported when the file loads.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? UnknownFields { get; set; }

        /// <summary>
        /// What the override is stored and assigned under: <see cref="ActorGuid"/> when set, else <see cref="EntityGuid"/>.
        /// </summary>
        [JsonIgnore]
        public string Key => string.IsNullOrEmpty(ActorGuid) ? EntityGuid : ActorGuid!;
    }

    /// <summary>
    /// Changes to one existing routine place. The place keeps everything it was authored with (time of day,
    /// conditions, point types and their commands); only what is listed here changes.
    /// </summary>
    public sealed class RoutinePlaceEdit
    {
        /// <summary>
        /// The place's GUID (<c>IRoutinePlace.Key()</c>).
        /// </summary>
        public string PlaceKey { get; set; } = string.Empty;

        /// <summary>
        /// A readable name for the file; not used at runtime.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether the place is switched off.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Changed points, by their index in the place's authored point list.
        /// </summary>
        public List<RoutinePointEdit> Points { get; set; } = [];

        /// <summary>
        /// Wait points appended to the place.
        /// </summary>
        public List<Vector2> AddedPoints { get; set; } = [];

        /// <summary>
        /// Fields in the file that this version does not know, usually a typo. Reported when the file loads.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? UnknownFields { get; set; }
    }

    /// <summary>
    /// A change to one authored routine point.
    /// </summary>
    public sealed class RoutinePointEdit
    {
        /// <summary>
        /// The point's index in the place's authored point list (hierarchy order), starting at 0.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The new position, or null to keep the authored one.
        /// </summary>
        public Vector2? Position { get; set; }

        /// <summary>
        /// Whether the point is removed from the place.
        /// </summary>
        public bool Removed { get; set; }

        /// <summary>
        /// Fields in the file that this version does not know, usually a typo. Reported when the file loads.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? UnknownFields { get; set; }
    }

    /// <summary>
    /// One routine place: the NPC walks between and waits at its points.
    /// </summary>
    public sealed class RoutinePlaceOverride
    {
        /// <summary>
        /// World positions of the wait points.
        /// </summary>
        public List<Vector2> Points { get; set; } = [];

        /// <summary>
        /// A gate the NPC walks to before the points when it is in another region, such as a door into a map editor area.
        /// </summary>
        public Vector2? Gate { get; set; }

        /// <summary>
        /// Where the NPC appears after <see cref="Gate"/>.
        /// </summary>
        public Vector2? GateTarget { get; set; }

        /// <summary>
        /// Fields in the file that this version does not know, usually a typo. Reported when the file loads.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? UnknownFields { get; set; }
    }


    /// <summary>
    /// Stores per-NPC routine overrides and applies them every time the AI logic scene loads. Saving applies graph
    /// assignments and place edits to the running game right away.
    /// </summary>
    public static class RoutineOverrideStore
    {
        private const string FolderName = "Routines";
        private const string LogTag = "[Routines] ";

        private static readonly Dictionary<string, RoutineOverride> Overrides = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The file each override was read from, so saving and deleting touch that file even when it is not named
        /// after the key.
        /// </summary>
        private static readonly Dictionary<string, string> SourceFiles = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The authored state of every place an edit or replacement touched since the AI logic scene loaded, by place
        /// key, so an edit can be re-applied (or taken back) relative to what the game shipped.
        /// </summary>
        private static readonly Dictionary<string, PlaceSnapshot> Snapshots = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Places an override with <see cref="RoutineOverride.ReplaceExisting"/> switched off on this world load.
        /// </summary>
        private static readonly HashSet<string> ReplacedPlaceKeys = new(StringComparer.OrdinalIgnoreCase);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new Vector2JsonConverter() }
        };

        /// <summary>
        /// Gets the absolute path where routine override files are stored.
        /// </summary>
        public static string GetFolderPath()
            => Path.Combine(Utils.SavePath, FolderName);

        /// <summary>
        /// A copy of the override stored under an actor or entity GUID. Change it and pass it to <see cref="Save"/>.
        /// </summary>
        /// <param name="key">The actor's GUID, or the GUID of its <see cref="EntityInfo"/></param>
        /// <param name="routineOverride">A copy of the stored override</param>
        public static bool TryGet(string key, [NotNullWhen(true)] out RoutineOverride? routineOverride)
        {
            routineOverride = !string.IsNullOrWhiteSpace(key) && Overrides.TryGetValue(key.Trim(), out RoutineOverride? stored) ? Clone(stored) : null;
            return routineOverride != null;
        }

        /// <summary>
        /// Writes an override to disk and applies its graph assignment and place edits to the running game. New places
        /// and replacing take effect on the next AI logic scene load.
        /// </summary>
        public static bool Save(RoutineOverride routineOverride)
        {
            Normalize(routineOverride);
            if (string.IsNullOrEmpty(routineOverride.EntityGuid))
            {
                MelonLogger.Warning(LogTag + $"cannot save '{routineOverride.Name}': it has no EntityGuid.");
                return false;
            }

            string key = routineOverride.Key;
            string path = SourceFiles.TryGetValue(key, out string? source) ? source : GetFilePath(key);
            try
            {
                Directory.CreateDirectory(GetFolderPath());
                File.WriteAllText(path, JsonSerializer.Serialize(routineOverride, JsonOptions));
            }
            catch (Exception e)
            {
                MelonLogger.Warning(LogTag + $"failed to save '{path}': {e.Message}");
                return false;
            }

            RoutineOverride stored = Clone(routineOverride);
            Overrides[key] = stored;
            SourceFiles[key] = path;
            RoutineGraphAccess.Assign(key, stored.GraphId);
            ApplyPlaceEditsLive(stored, path);
            RoutineGraphAccess.RefreshRunningRoutines();
            return true;
        }

        /// <summary>
        /// Deletes an override and its graph assignment. Edited places return to their authored state where that is
        /// possible in the running game; removed points and replaced places come back on the next load.
        /// </summary>
        /// <param name="key">The actor's GUID, or the GUID of its <see cref="EntityInfo"/></param>
        public static void Delete(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            key = key.Trim();

            if (Overrides.TryGetValue(key, out RoutineOverride? removed))
            {
                Overrides.Remove(key);
                RoutineOverride revert = new()
                {
                    EntityGuid = removed.EntityGuid,
                    PlaceEdits = removed.PlaceEdits.Select(edit => new RoutinePlaceEdit { PlaceKey = edit.PlaceKey }).ToList()
                };
                ApplyPlaceEditsLive(revert, key);
            }
            RoutineGraphAccess.Assign(key, null);

            string path = SourceFiles.TryGetValue(key, out string? source) ? source : GetFilePath(key);
            SourceFiles.Remove(key);
            if (File.Exists(path)) File.Delete(path);
            RoutineGraphAccess.RefreshRunningRoutines();
        }

        /// <summary>
        /// The routine places the game ships for an NPC, including ones an override switched off. Places bound to the
        /// actor's GUID win; a unique NPC's places are bound to its entity, so those are returned when none name the actor.
        /// </summary>
        /// <param name="actorGuid">The actor's GUID, or null</param>
        /// <param name="entityGuid">The GUID of the NPC's <see cref="EntityInfo"/></param>
        public static List<BaseRoutinePlace> FindExistingPlaces(string? actorGuid, string entityGuid)
            => FindExistingPlaces(actorGuid, entityGuid, GetScenePlaces(), out _);

        /// <summary>
        /// The points a place was authored with, in hierarchy order. These indices are what
        /// <see cref="RoutinePointEdit.Index"/> refers to. A point removed in this session reads as null.
        /// </summary>
        public static IReadOnlyList<RoutinePoint?> GetAuthoredPoints(BaseRoutinePlace place, out IReadOnlyList<Vector2> authoredPositions)
        {
            PlaceSnapshot snapshot = GetSnapshot(place);
            authoredPositions = snapshot.Positions;
            return snapshot.Points;
        }

        /// <summary>
        /// Whether a point's position is its own. Interaction and door points stand where their object (a bed, a work
        /// spot, a door) is, so moving the point changes nothing; move the object instead.
        /// </summary>
        public static bool IsMovable(RoutinePoint point)
        {
            return point.TryCast<InteractionRoutinePoint>() == null
                && point.TryCast<InteractionDialogRoutinePoint>() == null
                && point.TryCast<DoorRoutinePoint>() == null;
        }

        /// <summary>
        /// Reads every override file and registers the graph assignments. Runs every time the main menu loads.
        /// </summary>
        internal static void Load()
        {
            foreach (string key in Overrides.Keys)
            {
                RoutineGraphAccess.Assign(key, null);
            }
            Overrides.Clear();
            SourceFiles.Clear();
            RoutineGraphAccess.ClearLoadedTrees();
            if (!Directory.Exists(GetFolderPath())) return;

            string[] files = Directory.GetFiles(GetFolderPath(), "*.json");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                RoutineOverride? routineOverride;
                try
                {
                    routineOverride = JsonSerializer.Deserialize<RoutineOverride>(File.ReadAllText(file), JsonOptions);
                }
                catch (Exception e)
                {
                    MelonLogger.Warning(LogTag + $"'{file}' could not be read and is ignored: {e.Message}");
                    continue;
                }

                if (routineOverride == null)
                {
                    MelonLogger.Warning(LogTag + $"'{file}' is empty and is ignored.");
                    continue;
                }

                Normalize(routineOverride);
                if (!Validate(routineOverride, file)) continue;

                string key = routineOverride.Key;
                if (SourceFiles.TryGetValue(key, out string? first))
                {
                    MelonLogger.Warning(LogTag + $"'{file}' is for the same NPC ('{key}') as '{first}' and is ignored. Merge the two files.");
                    continue;
                }

                Overrides[key] = routineOverride;
                SourceFiles[key] = file;
                if (routineOverride.GraphId != null && File.Exists(BehaviourTreeStore.GetFilePath(routineOverride.GraphId))) RoutineGraphAccess.Assign(key, routineOverride.GraphId);
            }
        }

        /// <summary>
        /// Applies every override's places once the AI logic scene, which holds every routine place, has loaded.
        /// </summary>
        internal static void ApplyPlaces()
        {
            Snapshots.Clear();
            ReplacedPlaceKeys.Clear();
            if (Overrides.Count == 0) return;

            List<BaseRoutinePlace> scenePlaces = GetScenePlaces();
            Dictionary<string, BaseRoutinePlace> placesByKey = new(StringComparer.OrdinalIgnoreCase);
            foreach (BaseRoutinePlace place in scenePlaces)
            {
                placesByKey[place.Key()] = place;
            }

            Dictionary<RoutineOverride, List<BaseRoutinePlace>> existingByOverride = [];
            ForEachOverride((routineOverride, file) =>
            {
                if (!routineOverride.ReplaceExisting && routineOverride.Places.Count == 0) return;

                List<BaseRoutinePlace> existing = FindExistingPlaces(routineOverride.ActorGuid, routineOverride.EntityGuid, scenePlaces, out bool boundToActor);
                existingByOverride[routineOverride] = existing;
                if (!routineOverride.ReplaceExisting || existing.Count == 0) return;

                if (!string.IsNullOrEmpty(routineOverride.ActorGuid) && !boundToActor && SharesEntity(existing[0]))
                {
                    MelonLogger.Warning(LogTag + $"'{file}': the places of this NPC are shared by every NPC with its EntityInfo, so "
                        + "ReplaceExisting would switch them off for all of them. They stay on. Switch single places off with PlaceEdits instead.");
                    return;
                }

                foreach (BaseRoutinePlace place in existing)
                {
                    GetSnapshot(place);
                    ReplacedPlaceKeys.Add(place.Key());
                }
            });

            Dictionary<string, string> editedBy = new(StringComparer.OrdinalIgnoreCase);
            ForEachOverride((routineOverride, file) =>
            {
                foreach (RoutinePlaceEdit edit in routineOverride.PlaceEdits)
                {
                    if (!placesByKey.TryGetValue(edit.PlaceKey, out BaseRoutinePlace? place))
                    {
                        MelonLogger.Warning(LogTag + $"'{file}': there is no routine place with PlaceKey '{edit.PlaceKey}' ({edit.Name}); this edit is skipped.");
                        continue;
                    }

                    if (editedBy.TryGetValue(edit.PlaceKey, out string? other) && other != file)
                    {
                        MelonLogger.Warning(LogTag + $"'{file}' and '{other}' both edit the place '{edit.PlaceKey}' ({edit.Name}); only the edit in '{file}' applies.");
                    }
                    editedBy[edit.PlaceKey] = file;
                    ApplyPlaceEdit(place, edit, file);
                }
            });

            foreach (string placeKey in ReplacedPlaceKeys)
            {
                if (Snapshots.TryGetValue(placeKey, out PlaceSnapshot? snapshot) && snapshot.Place != null) snapshot.Place.gameObject.SetActive(false);
            }

            EntityInfo[]? allEntityInfos = null;
            ForEachOverride((routineOverride, file) =>
            {
                if (routineOverride.Places.Count == 0) return;

                List<BaseRoutinePlace> existing = existingByOverride.TryGetValue(routineOverride, out List<BaseRoutinePlace>? found) ? found : [];
                EntityInfo? entityInfo = existing.Count > 0 ? existing[0].GetEntityInfo() : null;
                if (entityInfo == null)
                {
                    allEntityInfos ??= Resources.FindObjectsOfTypeAll<EntityInfo>();
                    entityInfo = allEntityInfos.FirstOrDefault(info => string.Equals(info.GUID, routineOverride.EntityGuid, StringComparison.OrdinalIgnoreCase));
                }
                if (entityInfo == null)
                {
                    MelonLogger.Warning(LogTag + $"'{file}': no NPC has the EntityGuid '{routineOverride.EntityGuid}'; its new places are skipped.");
                    return;
                }

                foreach (RoutinePlaceOverride placeOverride in routineOverride.Places)
                {
                    if (placeOverride.Points.Count == 0) continue;

                    DefaultRoutinePlace? created = RoutineSystem.CreateRoutinePlace(null, entityInfo, [.. placeOverride.Points]);
                    if (created == null) continue;

                    if (placeOverride.Gate is { } gate && placeOverride.GateTarget is { } target)
                    {
                        RoutineSystem.AddGatePoint(created, gate, target);
                    }

                    if (!string.IsNullOrEmpty(routineOverride.ActorGuid))
                    {
                        created.SetEntityFromGuid(routineOverride.ActorGuid!);
                    }
                }
            });
        }

        /// <summary>
        /// Trims every GUID and id, and replaces lists a hand-written file set to null with empty ones.
        /// </summary>
        private static void Normalize(RoutineOverride routineOverride)
        {
            routineOverride.EntityGuid = routineOverride.EntityGuid?.Trim() ?? string.Empty;
            routineOverride.ActorGuid = string.IsNullOrWhiteSpace(routineOverride.ActorGuid) ? null : routineOverride.ActorGuid!.Trim();
            routineOverride.GraphId = string.IsNullOrWhiteSpace(routineOverride.GraphId) ? null : routineOverride.GraphId!.Trim();
            routineOverride.Name ??= string.Empty;
            routineOverride.Places = routineOverride.Places?.Where(place => place != null).ToList() ?? [];
            routineOverride.PlaceEdits = routineOverride.PlaceEdits?.Where(edit => edit != null).ToList() ?? [];

            foreach (RoutinePlaceOverride place in routineOverride.Places)
            {
                place.Points ??= [];
            }

            foreach (RoutinePlaceEdit edit in routineOverride.PlaceEdits)
            {
                edit.PlaceKey = edit.PlaceKey?.Trim() ?? string.Empty;
                edit.Name ??= string.Empty;
                edit.Points = edit.Points?.Where(point => point != null).ToList() ?? [];
                edit.AddedPoints ??= [];
            }
        }

        /// <summary>
        /// Logs every mistake in a loaded override that can be found without the world. Returns false when the file
        /// cannot be used at all.
        /// </summary>
        private static bool Validate(RoutineOverride routineOverride, string file)
        {
            if (string.IsNullOrEmpty(routineOverride.EntityGuid))
            {
                MelonLogger.Warning(LogTag + $"'{file}' has no EntityGuid and is ignored. The Edit Routine window shows the NPC's Entity GUID.");
                return false;
            }

            WarnUnknown(routineOverride.UnknownFields, file, string.Empty);
            if (routineOverride.GraphId != null && !File.Exists(BehaviourTreeStore.GetFilePath(routineOverride.GraphId)))
            {
                MelonLogger.Warning(LogTag + $"'{file}': GraphId '{routineOverride.GraphId}' has no file at '{BehaviourTreeStore.GetFilePath(routineOverride.GraphId)}'; "
                    + "the NPC keeps the shared routine graph.");
            }

            for (int i = 0; i < routineOverride.Places.Count; i++)
            {
                WarnUnknown(routineOverride.Places[i].UnknownFields, file, $"Places[{i}].");
                if (routineOverride.Places[i].Points.Count == 0)
                {
                    MelonLogger.Warning(LogTag + $"'{file}': Places[{i}] has no Points and is skipped.");
                }

                if (routineOverride.Places[i].Gate.HasValue != routineOverride.Places[i].GateTarget.HasValue)
                {
                    MelonLogger.Warning(LogTag + $"'{file}': Places[{i}] needs both Gate and GateTarget; the gate is skipped.");
                }
            }

            HashSet<string> placeKeys = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < routineOverride.PlaceEdits.Count; i++)
            {
                RoutinePlaceEdit edit = routineOverride.PlaceEdits[i];
                WarnUnknown(edit.UnknownFields, file, $"PlaceEdits[{i}].");
                if (edit.PlaceKey.Length == 0)
                {
                    MelonLogger.Warning(LogTag + $"'{file}': PlaceEdits[{i}] has no PlaceKey and is skipped.");
                }
                else if (!placeKeys.Add(edit.PlaceKey))
                {
                    MelonLogger.Warning(LogTag + $"'{file}': PlaceEdits[{i}] edits the place '{edit.PlaceKey}' a second time; only the last edit applies.");
                }

                foreach (RoutinePointEdit pointEdit in edit.Points)
                {
                    WarnUnknown(pointEdit.UnknownFields, file, $"PlaceEdits[{i}].Points.");
                    if (pointEdit.Index < 0)
                    {
                        MelonLogger.Warning(LogTag + $"'{file}': PlaceEdits[{i}] has a point with Index {pointEdit.Index}; indices start at 0.");
                    }
                }
            }

            routineOverride.PlaceEdits.RemoveAll(edit => edit.PlaceKey.Length == 0);
            return true;
        }

        private static void WarnUnknown(Dictionary<string, JsonElement>? unknownFields, string file, string path)
        {
            if (unknownFields == null || unknownFields.Count == 0) return;

            MelonLogger.Warning(LogTag + $"'{file}': unknown field(s) {string.Join(", ", unknownFields.Keys.Select(name => path + name))} are ignored. Check the spelling.");
        }

        private static void ForEachOverride(Action<RoutineOverride, string> apply)
        {
            foreach (KeyValuePair<string, RoutineOverride> pair in Overrides)
            {
                string file = SourceFiles.TryGetValue(pair.Key, out string? source) ? source : pair.Key;
                try
                {
                    apply(pair.Value, file);
                }
                catch (Exception e)
                {
                    MelonLogger.Error(LogTag + $"'{file}' could not be applied: {e}");
                }
            }
        }

        /// <summary>
        /// Applies an override's place edits to the running game and restarts the routines standing in those places.
        /// </summary>
        private static void ApplyPlaceEditsLive(RoutineOverride routineOverride, string file)
        {
            Dictionary<string, BaseRoutinePlace> placesByKey = new(StringComparer.OrdinalIgnoreCase);
            if (routineOverride.PlaceEdits.Count > 0)
            {
                foreach (BaseRoutinePlace place in GetScenePlaces())
                {
                    placesByKey[place.Key()] = place;
                }
            }

            List<string> touched = [];
            foreach (RoutinePlaceEdit edit in routineOverride.PlaceEdits)
            {
                if (!placesByKey.TryGetValue(edit.PlaceKey, out BaseRoutinePlace? place))
                {
                    MelonLogger.Warning(LogTag + $"'{file}': there is no routine place with PlaceKey '{edit.PlaceKey}' ({edit.Name}); this edit is skipped.");
                    continue;
                }

                try
                {
                    ApplyPlaceEdit(place, edit, file);
                    touched.Add(edit.PlaceKey);
                }
                catch (Exception e)
                {
                    MelonLogger.Error(LogTag + $"'{file}': the edit of place '{edit.PlaceKey}' could not be applied: {e}");
                }
            }

            if (touched.Count > 0) RoutineGraphAccess.RestartRoutinesAt(touched);
        }

        /// <summary>
        /// Brings a place to its authored state plus <paramref name="edit"/>. Points removed earlier in the session are
        /// gone for good until the next load; everything else can be changed back and forth.
        /// </summary>
        private static void ApplyPlaceEdit(BaseRoutinePlace place, RoutinePlaceEdit edit, string file)
        {
            PlaceSnapshot snapshot = GetSnapshot(place);

            foreach (GameObject added in snapshot.Added)
            {
                if (added == null) continue;
                added.transform.SetParent(null);
                Object.Destroy(added);
            }
            snapshot.Added.Clear();

            for (int i = 0; i < snapshot.Points.Count; i++)
            {
                RoutinePoint? point = snapshot.Points[i];
                if (point == null || !IsMovable(point)) continue;

                Vector2 position = snapshot.Positions[i];
                point.SetPosition(new Vector3(position.x, position.y, point.transform.position.z));
            }

            foreach (RoutinePointEdit pointEdit in edit.Points)
            {
                if (pointEdit.Index < 0 || pointEdit.Index >= snapshot.Points.Count)
                {
                    MelonLogger.Warning(LogTag + $"'{file}': place '{edit.PlaceKey}' ({edit.Name}) has points 0 to {snapshot.Points.Count - 1}; Index {pointEdit.Index} is skipped.");
                    continue;
                }

                RoutinePoint? point = snapshot.Points[pointEdit.Index];
                if (point == null) continue;

                if (pointEdit.Removed)
                {
                    point.transform.SetParent(null);
                    Object.Destroy(point.gameObject);
                    continue;
                }

                if (pointEdit.Position is not { } position) continue;
                if (!IsMovable(point))
                {
                    MelonLogger.Warning(LogTag + $"'{file}': point {pointEdit.Index} of place '{edit.PlaceKey}' stands at its object (a bed, a work spot, a door) and cannot be moved; its Position is ignored.");
                    continue;
                }
                point.SetPosition(new Vector3(position.x, position.y, point.transform.position.z));
            }

            for (int i = 0; i < edit.AddedPoints.Count; i++)
            {
                GameObject added = new("Point (added " + i + ")");
                added.transform.SetParent(place.transform);
                added.AddComponent<WaitRoutinePoint>().SetPosition(edit.AddedPoints[i]);
                snapshot.Added.Add(added);
            }

            place.SynchronizeAllSubRoutinePoints();
            if (place._routinePointsStartLoop != null) place._routinePointsStartLoop.Clear();
            if (place._routinePointsIdleLoop != null) place._routinePointsIdleLoop.Clear();

            bool active = snapshot.WasActive && !edit.Disabled && !ReplacedPlaceKeys.Contains(edit.PlaceKey);
            if (place.gameObject.activeSelf != active) place.gameObject.SetActive(active);
        }

        private static PlaceSnapshot GetSnapshot(BaseRoutinePlace place)
        {
            string key = place.Key();
            if (Snapshots.TryGetValue(key, out PlaceSnapshot? snapshot) && snapshot.Place != null && snapshot.Place.Pointer == place.Pointer)
            {
                return snapshot;
            }

            place.SynchronizeAllSubRoutinePoints();
            snapshot = new PlaceSnapshot(place);
            foreach (RoutinePoint point in place._routinePlaces)
            {
                snapshot.Points.Add(point);
                snapshot.Positions.Add(point.Position);
            }
            Snapshots[key] = snapshot;
            return snapshot;
        }

        private static List<BaseRoutinePlace> FindExistingPlaces(string? actorGuid, string entityGuid, IEnumerable<BaseRoutinePlace> places, out bool boundToActor)
        {
            List<BaseRoutinePlace> byActor = [];
            List<BaseRoutinePlace> byEntity = [];
            foreach (BaseRoutinePlace place in places)
            {
                if (!string.IsNullOrEmpty(actorGuid) && string.Equals(place.GetEntityGuid(), actorGuid, StringComparison.OrdinalIgnoreCase)) byActor.Add(place);
                else if (string.Equals(place.GetEntityInfo()?.GUID, entityGuid, StringComparison.OrdinalIgnoreCase)) byEntity.Add(place);
            }

            boundToActor = byActor.Count > 0;
            return boundToActor ? byActor : byEntity;
        }

        /// <summary>
        /// Whether more than one NPC has the place's <see cref="EntityInfo"/>.
        /// </summary>
        private static bool SharesEntity(BaseRoutinePlace place)
        {
            EntityInfo? entityInfo = place.GetEntityInfo();
            EntityGameHandler? handler = ProviderAccess.GetEntityGameHandler();
            if (entityInfo == null || handler == null) return false;

            return handler.GetLazyActorsByInfo(entityInfo)?.Count > 1;
        }

        /// <summary>
        /// Every routine place in a loaded scene, active or not, except the ones the API created.
        /// </summary>
        private static List<BaseRoutinePlace> GetScenePlaces()
        {
            Transform? apiRoot = RoutineSystem.RoutineRoot != null ? RoutineSystem.RoutineRoot.transform : null;
            return Resources.FindObjectsOfTypeAll<BaseRoutinePlace>()
                .Where(place => place.gameObject.scene.IsValid() && (apiRoot == null || !place.transform.IsChildOf(apiRoot)))
                .ToList();
        }

        private static RoutineOverride Clone(RoutineOverride routineOverride)
            => JsonSerializer.Deserialize<RoutineOverride>(JsonSerializer.Serialize(routineOverride, JsonOptions), JsonOptions)!;

        private static string GetFilePath(string key)
        {
            string fileName = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(GetFolderPath(), fileName + ".json");
        }

        private sealed class PlaceSnapshot(BaseRoutinePlace place)
        {
            public BaseRoutinePlace Place { get; } = place;
            public bool WasActive { get; } = place.gameObject.activeSelf;
            public List<RoutinePoint?> Points { get; } = [];
            public List<Vector2> Positions { get; } = [];
            public List<GameObject> Added { get; } = [];
        }
    }
}
