using Drova_Modding_API.Access;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    internal static class ReadableIdModuleSupport
    {
        public const string AllCategory = "All";

        internal sealed class EditorUiState
        {
            public string AutoCompleteInput = string.Empty;
            public string CategoryFilter = AllCategory;
            public bool ShowCategoryDropdown;
            public Vector2 CategoryDropdownScroll;
            public int CategorySelectionIndex;
            public bool ShowSuggestionDropdown;
            public Vector2 SuggestionDropdownScroll;
            public int SuggestionSelectionIndex;
        }

        internal sealed class EditorDataSource
        {
            public EditorDataSource(IReadOnlyList<string> ids, Func<string, string>? categorySelector = null)
            {
                Ids = ids;
                Func<string, string> selector = categorySelector ?? GetCategoryForId;
                IdsByCategory = BuildCategoryBuckets(Ids, selector);
                Categories = BuildCategories(IdsByCategory.Keys);
            }

            public IReadOnlyList<string> Ids { get; }
            public IReadOnlyList<string> Categories { get; }
            public IReadOnlyDictionary<string, IReadOnlyList<string>> IdsByCategory { get; }
        }

        private static EditorDataSource _defaultDataSource = new([], GetCategoryForId);
        private static bool _cacheInitialized;

        public static IReadOnlyList<string> Search(string? query, string? categoryFilter, int maxResults)
        {
            EnsureCacheUpToDate();
            return Search(_defaultDataSource, query, categoryFilter, maxResults);
        }

        public static void WarmUpCache()
        {
            EnsureCacheUpToDate();
        }

        private static IReadOnlyList<string> CollectMatches(IReadOnlyList<string> sourceIds, string query, int maxResults)
        {
            if (query.Length == 0)
                return TakeFirst(sourceIds, maxResults);

            List<string> matches = new(Math.Min(sourceIds.Count, maxResults));
            for (int i = 0; i < sourceIds.Count; i++)
            {
                string id = sourceIds[i];
                if (!id.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    continue;

                matches.Add(id);
                if (matches.Count == maxResults)
                    return matches;
            }

            for (int i = 0; i < sourceIds.Count; i++)
            {
                string id = sourceIds[i];
                if (id.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                    || !id.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                matches.Add(id);
                if (matches.Count == maxResults)
                    break;
            }

            return matches;
        }

        public static IReadOnlyList<string> GetCategories()
        {
            EnsureCacheUpToDate();
            return _defaultDataSource.Categories;
        }

        public static void InvalidateCache()
        {
            _defaultDataSource = new EditorDataSource([], GetCategoryForId);
            _cacheInitialized = false;
        }

        public static List<string> SplitCsv(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return [];

            return raw
                .Split([',', '\n', '\r', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void DrawEditor(
            string label,
            ref string? rawInput,
            EditorUiState uiState,
            out List<string> selectedIds,
            string uiKey)
        {
            if (uiState == null)
                throw new ArgumentNullException(nameof(uiState));

            DrawEditorInternal(
                label,
                ref rawInput,
                ref uiState.AutoCompleteInput,
                ref uiState.CategoryFilter,
                ref uiState.ShowCategoryDropdown,
                ref uiState.CategoryDropdownScroll,
                ref uiState.CategorySelectionIndex,
                ref uiState.ShowSuggestionDropdown,
                ref uiState.SuggestionDropdownScroll,
                ref uiState.SuggestionSelectionIndex,
                out selectedIds,
                uiKey,
                Search,
                GetCategories);
        }

        public static void DrawEditor(
            string label,
            ref string? rawInput,
            EditorUiState uiState,
            out List<string> selectedIds,
            string uiKey,
            EditorDataSource dataSource)
        {
            if (uiState == null)
                throw new ArgumentNullException(nameof(uiState));
            if (dataSource == null)
                throw new ArgumentNullException(nameof(dataSource));

            DrawEditorInternal(
                label,
                ref rawInput,
                ref uiState.AutoCompleteInput,
                ref uiState.CategoryFilter,
                ref uiState.ShowCategoryDropdown,
                ref uiState.CategoryDropdownScroll,
                ref uiState.CategorySelectionIndex,
                ref uiState.ShowSuggestionDropdown,
                ref uiState.SuggestionDropdownScroll,
                ref uiState.SuggestionSelectionIndex,
                out selectedIds,
                uiKey,
                (query, categoryFilter, maxResults) => Search(dataSource, query, categoryFilter, maxResults),
                () => dataSource.Categories);
        }

        private static void DrawEditorInternal(
            string label,
            ref string? rawInput,
            ref string autoCompleteInput,
            ref string categoryFilter,
            ref bool showCategoryDropdown,
            ref Vector2 categoryDropdownScroll,
            ref int categorySelectionIndex,
            ref bool showSuggestionDropdown,
            ref Vector2 suggestionDropdownScroll,
            ref int suggestionSelectionIndex,
            out List<string> selectedIds,
            string uiKey,
            Func<string?, string?, int, IReadOnlyList<string>> searchProvider,
            Func<IReadOnlyList<string>> categoriesProvider)
        {
            selectedIds = SplitCsv(rawInput);
            rawInput ??= string.Join(Environment.NewLine, selectedIds);
            categoryFilter = string.IsNullOrWhiteSpace(categoryFilter) ? AllCategory : categoryFilter;
            IReadOnlyList<string> categories = categoriesProvider();

            GUILayout.Label($"{label} (one id per line, comma/newline/semicolon supported)");
            rawInput = GUILayout.TextArea(rawInput, GUILayout.MinHeight(120f));
            selectedIds = SplitCsv(rawInput);

            GUILayout.Space(4f);
            DrawCategoryFilter(
                categories,
                ref categoryFilter,
                ref showCategoryDropdown,
                ref categoryDropdownScroll,
                ref categorySelectionIndex);

            GUILayout.Space(2f);
            GUILayout.Label("Search readable ids");
            GUILayout.BeginHorizontal();
            autoCompleteInput = GUILayout.TextField(autoCompleteInput);
            if (GUILayout.Button(showSuggestionDropdown ? "Hide" : "Show", GUILayout.Width(70f)))
                showSuggestionDropdown = !showSuggestionDropdown;
            GUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(autoCompleteInput))
                showSuggestionDropdown = true;

            IReadOnlyList<string> suggestions = [];
            bool shouldRenderSuggestions = showSuggestionDropdown || !string.IsNullOrWhiteSpace(autoCompleteInput);
            if (shouldRenderSuggestions)
            {
                suggestions = searchProvider(autoCompleteInput, categoryFilter, 80);
                HandleSuggestionKeyboardNavigation(
                    suggestions,
                    ref showSuggestionDropdown,
                    ref suggestionSelectionIndex,
                    selectedIds,
                    ref rawInput,
                    ref autoCompleteInput);
            }

            if (showSuggestionDropdown)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label($"Suggestions ({suggestions.Count})");
                if (suggestions.Count == 0)
                {
                    GUILayout.Label("No matches");
                }
                else
                {
                    suggestionDropdownScroll = GUILayout.BeginScrollView(suggestionDropdownScroll, GUILayout.Height(150f));
                    for (int i = 0; i < suggestions.Count; i++)
                    {
                        string suggestion = suggestions[i];
                        string buttonText = i == suggestionSelectionIndex ? $"> + {suggestion}" : $"+ {suggestion}";
                        if (!GUILayout.Button(buttonText, GUILayout.ExpandWidth(true)))
                            continue;

                        AddSuggestionIfMissing(suggestion, selectedIds, out rawInput);
                        suggestionSelectionIndex = i;
                        autoCompleteInput = string.Empty;
                        showSuggestionDropdown = false;
                        break;
                    }
                    GUILayout.EndScrollView();
                }

                GUILayout.EndVertical();
            }

            GUILayout.Label($"{uiKey} ids selected: {selectedIds.Count}");
        }

        public static IReadOnlyList<string> Search(EditorDataSource? dataSource, string? query, string? categoryFilter, int maxResults)
        {
            if (dataSource == null)
                return [];
            if (maxResults <= 0)
                return [];

            string normalizedCategory = NormalizeCategory(categoryFilter);
            IReadOnlyList<string>? sourceIds = string.Equals(normalizedCategory, AllCategory, StringComparison.OrdinalIgnoreCase)
                ? dataSource.Ids
                : (dataSource.IdsByCategory.TryGetValue(normalizedCategory, out IReadOnlyList<string>? ids) ? ids : []);

            if (sourceIds == null || sourceIds.Count == 0)
                return [];

            string normalizedQuery = query?.Trim() ?? string.Empty;
            return CollectMatches(sourceIds, normalizedQuery, maxResults);
        }

        private static void EnsureCacheUpToDate()
        {
            if (_cacheInitialized)
                return;

            CacheSnapshot? snapshot = TryBuildSnapshotFromDatabase();
            if (snapshot == null)
                return;

            _defaultDataSource = snapshot.Value.DataSource;
            _cacheInitialized = true;
        }

        private static CacheSnapshot? TryBuildSnapshotFromDatabase()
        {
            try
            {
                Il2CppStringArray readableIds = ProviderAccess.ItemDatabase.GetItemReadableIds();
                if (readableIds == null || readableIds.Length == 0)
                    return new CacheSnapshot(new EditorDataSource([], GetCategoryForId));

                HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < readableIds.Length; i++)
                {
                    string? readableId = readableIds[i];
                    if (!string.IsNullOrWhiteSpace(readableId))
                        ids.Add(readableId);
                }

                List<string> orderedIds = ids
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return new CacheSnapshot(new EditorDataSource(orderedIds, GetCategoryForId));
            }
            catch
            {
                return null;
            }
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildCategoryBuckets(IReadOnlyList<string> ids, Func<string, string> categorySelector)
        {
            Dictionary<string, List<string>> buckets = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                string category = categorySelector(id);
                if (!buckets.TryGetValue(category, out List<string>? list))
                {
                    list = [];
                    buckets[category] = list;
                }

                list?.Add(id);
            }

            Dictionary<string, IReadOnlyList<string>> result = new(StringComparer.OrdinalIgnoreCase);
            foreach ((string category, List<string> entries) in buckets)
                result[category] = entries;

            return result;
        }

        private static IReadOnlyList<string> BuildCategories(IEnumerable<string> categoryKeys)
        {
            List<string> ordered = categoryKeys
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();
            ordered.Insert(0, AllCategory);
            return ordered;
        }

        private static string NormalizeCategory(string? categoryFilter)
            => string.IsNullOrWhiteSpace(categoryFilter) ? AllCategory : categoryFilter.Trim();

        private static IReadOnlyList<string> TakeFirst(IReadOnlyList<string> ids, int maxResults)
        {
            if (ids.Count <= maxResults)
                return ids;

            List<string> result = new(maxResults);
            for (int i = 0; i < maxResults; i++)
                result.Add(ids[i]);

            return result;
        }

        private readonly record struct CacheSnapshot(EditorDataSource DataSource);

        private static string GetCategoryForId(string id)
        {
            int separatorIndex = id.IndexOf('_');
            if (separatorIndex <= 0)
                return "other";

            return id[..separatorIndex];
        }


        private static void DrawCategoryFilter(
            IReadOnlyList<string> categories,
            ref string categoryFilter,
            ref bool showCategoryDropdown,
            ref Vector2 categoryDropdownScroll,
            ref int categorySelectionIndex)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Category", GUILayout.Width(80f));
            if (GUILayout.Button($"{categoryFilter} v", GUILayout.Width(220f)))
                showCategoryDropdown = !showCategoryDropdown;

            if (GUILayout.Button("All", GUILayout.Width(60f)))
            {
                categoryFilter = AllCategory;
                showCategoryDropdown = false;
            }
            GUILayout.EndHorizontal();

            if (!showCategoryDropdown)
                return;

            string? keyboardSelectedCategory;
            HandleCategoryKeyboardNavigation(
                categories,
                ref showCategoryDropdown,
                ref categorySelectionIndex,
                out keyboardSelectedCategory);
            if (!string.IsNullOrWhiteSpace(keyboardSelectedCategory))
                categoryFilter = keyboardSelectedCategory;

            GUILayout.BeginVertical("box");
            categoryDropdownScroll = GUILayout.BeginScrollView(categoryDropdownScroll, GUILayout.Height(120f));
            for (int i = 0; i < categories.Count; i++)
            {
                string candidate = categories[i];
                string buttonText = i == categorySelectionIndex ? $"> {candidate}" : candidate;
                if (!GUILayout.Button(buttonText, GUILayout.ExpandWidth(true)))
                    continue;

                categoryFilter = candidate;
                categorySelectionIndex = i;
                showCategoryDropdown = false;
                break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static void HandleCategoryKeyboardNavigation(
            IReadOnlyList<string> categories,
            ref bool showCategoryDropdown,
            ref int categorySelectionIndex,
            out string? selectedCategory)
        {
            selectedCategory = null;
            if (categories.Count == 0)
            {
                categorySelectionIndex = 0;
                return;
            }

            categorySelectionIndex = Mathf.Clamp(categorySelectionIndex, 0, categories.Count - 1);
            Event evt = Event.current;
            if (evt == null || evt.type != EventType.KeyDown)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                    categorySelectionIndex = (categorySelectionIndex + 1) % categories.Count;
                    evt.Use();
                    break;
                case KeyCode.UpArrow:
                    categorySelectionIndex = (categorySelectionIndex - 1 + categories.Count) % categories.Count;
                    evt.Use();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    selectedCategory = categories[categorySelectionIndex];
                    showCategoryDropdown = false;
                    evt.Use();
                    break;
                case KeyCode.Escape:
                    showCategoryDropdown = false;
                    evt.Use();
                    break;
            }
        }

        private static void HandleSuggestionKeyboardNavigation(
            IReadOnlyList<string> suggestions,
            ref bool showSuggestionDropdown,
            ref int suggestionSelectionIndex,
            List<string> selectedIds,
            ref string? rawInput,
            ref string autoCompleteInput)
        {
            if (!showSuggestionDropdown)
                return;

            if (suggestions.Count == 0)
            {
                suggestionSelectionIndex = 0;
                return;
            }

            suggestionSelectionIndex = Mathf.Clamp(suggestionSelectionIndex, 0, suggestions.Count - 1);
            Event evt = Event.current;
            if (evt == null || evt.type != EventType.KeyDown)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                    suggestionSelectionIndex = (suggestionSelectionIndex + 1) % suggestions.Count;
                    evt.Use();
                    break;
                case KeyCode.UpArrow:
                    suggestionSelectionIndex = (suggestionSelectionIndex - 1 + suggestions.Count) % suggestions.Count;
                    evt.Use();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    AddSuggestionIfMissing(suggestions[suggestionSelectionIndex], selectedIds, out rawInput);
                    autoCompleteInput = string.Empty;
                    showSuggestionDropdown = false;
                    evt.Use();
                    break;
                case KeyCode.Escape:
                    showSuggestionDropdown = false;
                    evt.Use();
                    break;
            }
        }

        private static void AddSuggestionIfMissing(string suggestion, List<string> selectedIds, out string? rawInput)
        {
            if (!selectedIds.Contains(suggestion, StringComparer.OrdinalIgnoreCase))
                selectedIds.Add(suggestion);

            rawInput = string.Join(Environment.NewLine, selectedIds);
        }
    }
}

