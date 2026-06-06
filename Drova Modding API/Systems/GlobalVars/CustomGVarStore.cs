#pragma warning disable 300
using System.Globalization;
using System.Text.Json;
using Drova_Modding_API.Access;
using Il2CppDrova.GlobalVarSystem;
using Il2CppInterop.Runtime;
using MelonLoader;

namespace Drova_Modding_API.Systems.GlobalVars
{
    internal enum CustomGVarType
    {
        GBool,
        GInt,
        GFloat,
        GString
    }

    internal static class CustomGVarStore
    {
        private const string FolderName = "GlobalVars";
        private const string DefaultFileName = "custom_gvar_lists.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private static readonly HashSet<string> CustomListGuids = [];
        private static readonly HashSet<string> CustomVarIds = [];
        private static readonly Dictionary<string, string> CustomListSourceFiles = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> KnownStoreFiles = new(StringComparer.OrdinalIgnoreCase);
        private static bool _isLoaded;

        private sealed class CustomGVarStoreData
        {
            public int Version { get; set; } = 1;
            public List<CustomGVarListData> Lists { get; set; } = [];
        }

        private sealed class CustomGVarListData
        {
            public string Guid { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public bool IsQuestVarList { get; set; }
            public List<CustomGVarData> Vars { get; set; } = [];
        }

        private sealed class CustomGVarData
        {
            public string Id { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string InitialValue { get; set; } = string.Empty;
        }

        public static bool IsCustomList(GVarList? list)
        {
            if (list == null)
            {
                return false;
            }

            string guid = list.Guid;
            return !string.IsNullOrWhiteSpace(guid) && CustomListGuids.Contains(guid);
        }

        public static bool IsCustomVar(AGVarBase? gvar)
        {
            if (gvar == null)
            {
                return false;
            }

            string id = gvar.Id;
            return !string.IsNullOrWhiteSpace(id) && CustomVarIds.Contains(id);
        }

        public static void LoadIntoDatabaseOnce()
        {
            if (_isLoaded)
            {
                return;
            }

            _isLoaded = true;
            CustomListGuids.Clear();
            CustomVarIds.Clear();
            CustomListSourceFiles.Clear();
            KnownStoreFiles.Clear();

            try
            {
                string folderPath = GetStoreFolderPath();
                EnsureStoreFolder(folderPath);

                string[] files = Directory.GetFiles(folderPath, "*.json");
                if (files.Length == 0)
                {
                    return;
                }

                SubDatabase_GVars database = ProviderAccess.GVarDatabase;
                if (database == null)
                {
                    return;
                }

                for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
                {
                    string filePath = files[fileIndex];
                    CustomGVarStoreData? data = ReadStoreData(filePath);
                    if (data == null || data.Lists.Count == 0)
                    {
                        continue;
                    }

                    KnownStoreFiles.Add(filePath);
                    for (int i = 0; i < data.Lists.Count; i++)
                    {
                        ApplyListDefinition(database, data.Lists[i], filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"CustomGVarStore: failed to load custom GVars: {ex.Message}");
            }
        }

        public static string GetCustomListSourceFileName(GVarList? list)
        {
            string filePath = GetCustomListSourceFilePath(list);
            return Path.GetFileName(filePath);
        }

        public static bool TrySaveCustomListToModFile(GVarList? list, string modName, out string message)
        {
            message = string.Empty;
            if (list == null)
            {
                message = "Select a list first.";
                return false;
            }

            if (!IsCustomList(list))
            {
                message = "Only custom lists can be saved to a custom mod file.";
                return false;
            }

            if (!TryBuildStorePath(modName, out string filePath, out string fileName, out string error))
            {
                message = error;
                return false;
            }

            SetCustomListSourceFile(list, filePath);
            PersistCustomDefinitions();
            message = $"Saved custom list '{list.name}' to '{fileName}'.";
            return true;
        }

        public static bool TryCreateCustomList(string listName, bool isQuestVarList, string? modFileStem, out GVarList? list, out string message)
        {
            list = null;
            message = string.Empty;

            string trimmedName = listName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                message = "List name is required.";
                return false;
            }

            SubDatabase_GVars database = ProviderAccess.GVarDatabase;
            if (database == null)
            {
                message = "GVar database is not available.";
                return false;
            }

            if (database.GetGVarListByName(trimmedName) != null)
            {
                message = $"A list named '{trimmedName}' already exists.";
                return false;
            }

            list = isQuestVarList
                ? GVarList.CreateQuestListInstance(trimmedName)
                : GVarList.CreateVarListInstance(trimmedName, false);

            if (list == null)
            {
                message = "Could not create list instance.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(list._guid))
            {
                list._guid = Guid.NewGuid().ToString();
            }

            bool added = database.TryAddNewGVarList(ProviderAccess.GetGameDatabase(), list);
            if (!added)
            {
                message = "Could not insert list into GVar database.";
                return false;
            }

            MarkListAsCustom(list);
            if (TryBuildStorePath(modFileStem ?? string.Empty, out string modFilePath, out _, out _))
                SetCustomListSourceFile(list, modFilePath);
            else
                SetCustomListSourceFile(list, GetDefaultStorePath());

            PersistCustomDefinitions();
            message = "Custom list created.";
            return true;
        }

        public static bool TryCreateCustomVar(GVarList list, CustomGVarType type, string id, string initialValue, out AGVarBase? createdVar, out string message)
        {
            createdVar = null;
            message = string.Empty;

            if (list == null)
            {
                message = "Select a list first.";
                return false;
            }

            if (!IsCustomList(list))
            {
                message = "Variables can only be added to custom lists.";
                return false;
            }

            string trimmedId = id?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedId))
            {
                message = "Variable id is required.";
                return false;
            }

            if (list.GetGVarById(trimmedId) != null)
            {
                message = $"A variable with id '{trimmedId}' already exists in this list.";
                return false;
            }

            AGVarBase gvar = list.AddNewGVar(GetIl2CppType(type), trimmedId, false);
            if (gvar == null)
            {
                message = "Could not create variable.";
                return false;
            }

            gvar._id = trimmedId;
            gvar.Rename(trimmedId);

            if (!TryParseAndApplyInitialValue(gvar, initialValue, out string parseError))
            {
                message = parseError;
                list.DeleteScriptableChild_Editor(gvar);
                return false;
            }

            MarkListAsCustom(list);
            MarkVarAsCustom(gvar);
            createdVar = gvar;
            PersistCustomDefinitions();
            message = "Custom variable created.";
            return true;
        }

        public static bool TryDeleteCustomVar(AGVarBase? gvar, out string message)
        {
            message = string.Empty;
            if (gvar == null)
            {
                message = "Select a variable first.";
                return false;
            }

            GVarList? parent = gvar.GetParent();
            if (parent == null || !IsCustomList(parent))
            {
                message = "Only variables inside custom lists can be deleted here.";
                return false;
            }

            string customVarId = GetCustomVarId(gvar);
            parent.DeleteScriptableChild_Editor(gvar);
            if (!string.IsNullOrWhiteSpace(customVarId))
            {
                CustomVarIds.Remove(customVarId);
            }

            PersistCustomDefinitions();
            message = "Custom variable deleted.";
            return true;
        }

        public static bool TryDeleteCustomList(GVarList? list, out string message)
        {
            message = string.Empty;
            if (list == null)
            {
                message = "Select a list first.";
                return false;
            }

            if (!IsCustomList(list))
            {
                message = "Only custom lists can be deleted here.";
                return false;
            }

            RemoveCustomVarsFromList(list);

            SubDatabase_GVars database = ProviderAccess.GVarDatabase;
            if (database == null)
            {
                message = "GVar database is not available.";
                return false;
            }

            bool removed = database.AllGVars.Remove(list);
            if (!removed)
            {
                message = "Could not remove list from GVar database.";
                return false;
            }

            string guid = list.Guid;
            if (string.IsNullOrWhiteSpace(guid))
            {
                guid = list._guid;
            }

            if (!string.IsNullOrWhiteSpace(guid))
            {
                CustomListGuids.Remove(guid);
                CustomListSourceFiles.Remove(guid);
            }

            PersistCustomDefinitions();
            message = "Custom list deleted.";
            return true;
        }

        private static void ApplyListDefinition(SubDatabase_GVars database, CustomGVarListData listData, string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(listData.Guid) && string.IsNullOrWhiteSpace(listData.Name))
            {
                return;
            }

            GVarList? list = null;
            if (!string.IsNullOrWhiteSpace(listData.Guid))
            {
                list = database.GetGVarListByGuid(listData.Guid);
            }

            if (list == null && !string.IsNullOrWhiteSpace(listData.Name))
            {
                list = database.GetGVarListByName(listData.Name);
            }

            if (list == null)
            {
                string listName = string.IsNullOrWhiteSpace(listData.Name) ? "CustomGVarList" : listData.Name;
                list = listData.IsQuestVarList
                    ? GVarList.CreateQuestListInstance(listName)
                    : GVarList.CreateVarListInstance(listName, false);
                if (list == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(listData.Guid))
                {
                    list._guid = listData.Guid;
                }

                if (!database.TryAddNewGVarList(ProviderAccess.GetGameDatabase(), list))
                {
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(listData.Name))
            {
                list.name = listData.Name;
            }

            if (!string.IsNullOrWhiteSpace(listData.Guid))
            {
                list._guid = listData.Guid;
            }

            MarkListAsCustom(list);
            SetCustomListSourceFile(list, sourceFilePath);

            for (int i = 0; i < listData.Vars.Count; i++)
            {
                ApplyVarDefinition(list, listData.Vars[i]);
            }
        }

        private static void ApplyVarDefinition(GVarList list, CustomGVarData varData)
        {
            string id = varData.Id?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (!TryParseType(varData.Type, out CustomGVarType type))
            {
                return;
            }

            AGVarBase gvar = list.GetGVarById(id);
            if (gvar == null)
            {
                gvar = list.AddNewGVar(GetIl2CppType(type), id, false);
                if (gvar == null)
                {
                    return;
                }
            }

            gvar._id = id;
            gvar.Rename(id);
            TryParseAndApplyInitialValue(gvar, varData.InitialValue, out _);
            MarkVarAsCustom(gvar);
        }

        private static void PersistCustomDefinitions()
        {
            try
            {
                SubDatabase_GVars database = ProviderAccess.GVarDatabase;
                if (database == null)
                {
                    return;
                }

                string folderPath = GetStoreFolderPath();
                EnsureStoreFolder(folderPath);

                Dictionary<string, CustomGVarStoreData> dataByFile = new(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < database.AllGVars.Count; i++)
                {
                    GVarList list = database.AllGVars[i];
                    if (list == null || !IsCustomList(list))
                    {
                        continue;
                    }

                    string filePath = GetCustomListSourceFilePath(list);
                    KnownStoreFiles.Add(filePath);
                    if (!dataByFile.TryGetValue(filePath, out CustomGVarStoreData data))
                    {
                        data = new CustomGVarStoreData();
                        dataByFile[filePath] = data;
                    }

                    CustomGVarListData listData = new()
                    {
                        Guid = list.Guid ?? list._guid ?? string.Empty,
                        Name = list.name ?? string.Empty,
                        IsQuestVarList = list.IsQuestVarList
                    };

                    AddVarsByType<GBool>(list, CustomGVarType.GBool, listData.Vars);
                    AddVarsByType<GInt>(list, CustomGVarType.GInt, listData.Vars);
                    AddVarsByType<GFloat>(list, CustomGVarType.GFloat, listData.Vars);
                    AddVarsByType<GString>(list, CustomGVarType.GString, listData.Vars);
                    data.Lists.Add(listData);
                }

                foreach (string knownFile in KnownStoreFiles)
                {
                    if (!dataByFile.ContainsKey(knownFile))
                    {
                        dataByFile[knownFile] = new CustomGVarStoreData();
                    }
                }

                foreach ((string filePath, CustomGVarStoreData data) in dataByFile)
                {
                    string json = JsonSerializer.Serialize(data, JsonOptions);
                    File.WriteAllText(filePath, json);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"CustomGVarStore: failed to persist custom GVars: {ex.Message}");
            }
        }

        private static void AddVarsByType<TVar>(GVarList list, CustomGVarType type, List<CustomGVarData> target)
            where TVar : AGVarBase
        {
            var vars = list.GetVarsOfType<TVar>();
            for (int i = 0; i < vars.Count; i++)
            {
                AGVarBase gvar = vars[i];
                if (gvar == null || !IsCustomVar(gvar))
                {
                    continue;
                }

                target.Add(new CustomGVarData
                {
                    Id = gvar.Id ?? gvar.GetGVarId(),
                    Type = type.ToString(),
                    InitialValue = GetInitialValueAsString(gvar)
                });
            }
        }

        private static void RemoveCustomVarsFromList(GVarList list)
        {
            RemoveCustomVarsByType<GBool>(list);
            RemoveCustomVarsByType<GInt>(list);
            RemoveCustomVarsByType<GFloat>(list);
            RemoveCustomVarsByType<GString>(list);
        }

        private static void RemoveCustomVarsByType<TVar>(GVarList list)
            where TVar : AGVarBase
        {
            var vars = list.GetVarsOfType<TVar>();
            for (int i = vars.Count - 1; i >= 0; i--)
            {
                AGVarBase gvar = vars[i];
                if (gvar == null || !IsCustomVar(gvar))
                {
                    continue;
                }

                list.DeleteScriptableChild_Editor(gvar);
                string id = GetCustomVarId(gvar);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    CustomVarIds.Remove(id);
                }
            }
        }

        private static string GetCustomVarId(AGVarBase gvar)
        {
            string id = gvar.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                id = gvar.GetGVarId();
            }

            return id ?? string.Empty;
        }

        private static string GetInitialValueAsString(AGVarBase gvar)
        {
            GBool boolVar = gvar.TryCast<GBool>();
            if (boolVar != null)
            {
                return boolVar.GetInitialValue() ? "true" : "false";
            }

            GInt intVar = gvar.TryCast<GInt>();
            if (intVar != null)
            {
                return intVar.GetInitialValue().ToString(CultureInfo.InvariantCulture);
            }

            GFloat floatVar = gvar.TryCast<GFloat>();
            if (floatVar != null)
            {
                return floatVar.GetInitialValue().ToString(CultureInfo.InvariantCulture);
            }

            GString stringVar = gvar.TryCast<GString>();
            if (stringVar != null)
            {
                return stringVar.GetInitialValue() ?? string.Empty;
            }

            return string.Empty;
        }

        private static bool TryParseAndApplyInitialValue(AGVarBase gvar, string rawValue, out string error)
        {
            error = string.Empty;
            if (gvar == null)
            {
                error = "Variable is invalid.";
                return false;
            }

            string normalized = rawValue ?? string.Empty;

            GBool boolVar = gvar.TryCast<GBool>();
            if (boolVar != null)
            {
                if (TryParseBoolInvariant(normalized, out bool boolValue))
                {
                    boolVar._initialValue = boolValue;
                    boolVar.ResetValue(GVarContext.Empty);
                    return true;
                }

                error = $"Could not parse '{normalized}' for {nameof(GBool)}.";
                return false;
            }

            GInt intVar = gvar.TryCast<GInt>();
            if (intVar != null)
            {
                if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue)
                    || int.TryParse(normalized, NumberStyles.Integer, CultureInfo.CurrentCulture, out intValue))
                {
                    intVar._initialValue = intValue;
                    intVar.ResetValue(GVarContext.Empty);
                    return true;
                }

                error = $"Could not parse '{normalized}' for {nameof(GInt)}.";
                return false;
            }

            GFloat floatVar = gvar.TryCast<GFloat>();
            if (floatVar != null)
            {
                if (TryParseFloatInvariant(normalized, out float floatValue))
                {
                    floatVar._initialValue = floatValue;
                    floatVar.ResetValue(GVarContext.Empty);
                    return true;
                }

                error = $"Could not parse '{normalized}' for {nameof(GFloat)}.";
                return false;
            }

            GString stringVar = gvar.TryCast<GString>();
            if (stringVar != null)
            {
                stringVar._initialValue = normalized;
                stringVar.ResetValue(GVarContext.Empty);
                return true;
            }

            if (!gvar.TryParse(normalized, out Il2CppSystem.Object parsedValue))
            {
                error = $"Could not parse '{normalized}' for {gvar.GetIl2CppType().Name}.";
                return false;
            }

            gvar.SetGenericInitialValue(parsedValue, GVarContext.Empty);
            gvar.ResetValue(GVarContext.Empty);
            return true;
        }

        private static bool TryParseBoolInvariant(string value, out bool result)
        {
            if (bool.TryParse(value, out result))
            {
                return true;
            }

            if (string.Equals(value, "1", StringComparison.Ordinal))
            {
                result = true;
                return true;
            }

            if (string.Equals(value, "0", StringComparison.Ordinal))
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }

        private static bool TryParseFloatInvariant(string value, out float result)
        {
            NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands;
            if (float.TryParse(value, style, CultureInfo.InvariantCulture, out result)
                || float.TryParse(value, style, CultureInfo.CurrentCulture, out result))
            {
                return true;
            }

            // Fallback for mismatched decimal separators (e.g. saved with ',' and loaded under '.')
            string normalized = value.Replace(',', '.');
            return float.TryParse(normalized, style, CultureInfo.InvariantCulture, out result);
        }

        private static Il2CppSystem.Type GetIl2CppType(CustomGVarType type)
        {
            return type switch
            {
                CustomGVarType.GBool => Il2CppType.From(typeof(GBool), false),
                CustomGVarType.GInt => Il2CppType.From(typeof(GInt), false),
                CustomGVarType.GFloat => Il2CppType.From(typeof(GFloat), false),
                CustomGVarType.GString => Il2CppType.From(typeof(GString), false),
                _ => Il2CppType.From(typeof(GInt), false)
            };
        }

        private static bool TryParseType(string? typeValue, out CustomGVarType type)
        {
            type = CustomGVarType.GInt;
            if (string.IsNullOrWhiteSpace(typeValue))
            {
                return false;
            }

            return Enum.TryParse(typeValue, true, out type);
        }

        private static void MarkListAsCustom(GVarList list)
        {
            if (list == null)
            {
                return;
            }

            string guid = GetListGuid(list);

            if (!string.IsNullOrWhiteSpace(guid))
            {
                CustomListGuids.Add(guid);
            }
        }

        private static void MarkVarAsCustom(AGVarBase gvar)
        {
            if (gvar == null)
            {
                return;
            }

            string id = gvar.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                id = gvar.GetGVarId();
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                CustomVarIds.Add(id);
            }
        }

        private static string GetCustomListSourceFilePath(GVarList? list)
        {
            string guid = GetListGuid(list);
            if (!string.IsNullOrWhiteSpace(guid)
                && CustomListSourceFiles.TryGetValue(guid, out string filePath)
                && !string.IsNullOrWhiteSpace(filePath))
            {
                return filePath;
            }

            return GetDefaultStorePath();
        }

        private static void SetCustomListSourceFile(GVarList list, string filePath)
        {
            string guid = GetListGuid(list);
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            CustomListSourceFiles[guid] = filePath;
            KnownStoreFiles.Add(filePath);
        }

        private static string GetListGuid(GVarList? list)
        {
            if (list == null)
            {
                return string.Empty;
            }

            string guid = list.Guid;
            if (string.IsNullOrWhiteSpace(guid))
            {
                guid = list._guid;
            }

            return guid ?? string.Empty;
        }

        private static CustomGVarStoreData? ReadStoreData(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            string raw = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return JsonSerializer.Deserialize<CustomGVarStoreData>(raw, JsonOptions);
        }

        private static bool TryBuildStorePath(string rawModName, out string filePath, out string fileName, out string error)
        {
            filePath = string.Empty;
            fileName = string.Empty;
            error = string.Empty;

            string sanitizedName = SanitizeFileName(rawModName);
            if (string.IsNullOrWhiteSpace(sanitizedName))
            {
                error = "Mod file name is required.";
                return false;
            }

            fileName = sanitizedName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? sanitizedName
                : $"{sanitizedName}.json";
            filePath = Path.Combine(GetStoreFolderPath(), fileName);
            return true;
        }

        private static string SanitizeFileName(string rawValue)
        {
            string sanitized = rawValue.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
            {
                sanitized = sanitized.Replace(invalidChars[i], '_');
            }

            return sanitized;
        }

        private static string GetStoreFolderPath()
        {
            return Path.Combine(Utils.SavePath, FolderName);
        }

        private static string GetDefaultStorePath()
        {
            return Path.Combine(GetStoreFolderPath(), DefaultFileName);
        }

        private static void EnsureStoreFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

    }
}
#pragma warning restore 300
