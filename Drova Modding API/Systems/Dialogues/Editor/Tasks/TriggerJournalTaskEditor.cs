using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.JournalSystem;
using Il2CppDrova.QuestSystem.Graphs;
using UnityEngine;
using Il2CppCollections = Il2CppSystem.Collections.Generic;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="TriggerJournalTask"/>. Lists its journal entries and adds loaded ones by name.
    /// </summary>
    internal class TriggerJournalTaskEditor : DrawTaskEditor
    {
        private const float Width = 340f;
        private const float RowHeight = 22f;
        private const float RowStep = 27f;

        private TriggerJournalTask? _castedTask;
        private JournalEntry[] _entries = [];
        private GUIDropdownWithFilter? _entryDropdown;

        /// <summary>
        /// Collects every loaded <see cref="JournalEntry"/>. Vanilla entries live in Resources and only show up once loaded.
        /// </summary>
        public override void Init()
        {
            _castedTask ??= Task.TryCast<TriggerJournalTask>();
            if (_castedTask == null) return;

            _castedTask.Entries ??= new Il2CppCollections.List<JournalEntry>();
            _entries = Resources.FindObjectsOfTypeAll<JournalEntry>()
                                .GroupBy(e => e.name)
                                .Select(g => g.First())
                                .OrderBy(e => e.name, StringComparer.Ordinal)
                                .ToArray();
        }

        /// <inheritdoc/>
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null) return default;

            float x = position.x;
            Il2CppCollections.List<JournalEntry> entries = _castedTask.Entries;
            Rect drawRect = new(x, position.y, Width, RowStep * (entries.Count + 2) + 10);
            GUI.Box(drawRect, "Trigger Journal Task", EditorBoxStyles.Task);

            float y = position.y + RowStep;
            for (int i = 0; i < entries.Count; i++)
            {
                JournalEntry entry = entries[i];
                GUI.Label(new Rect(x + 5, y, Width - 50, RowHeight), entry != null ? entry.name : "(missing)");
                if (GUI.Button(new Rect(x + Width - 40, y, 35, RowHeight), "X"))
                {
                    entries.RemoveAt(i);
                    i--;
                    continue;
                }
                y += RowStep;
            }

            _entryDropdown ??= new GUIDropdownWithFilter(_entries.Select(e => e.name).ToArray(), -1, 20);
            if (_entryDropdown.Draw(new Rect(x + 5, y, Width - 10, RowHeight)) && _entryDropdown.SelectedIndex >= 0)
            {
                entries.Add(_entries[_entryDropdown.SelectedIndex]);
                _entryDropdown = null;
            }

            return drawRect;
        }
    }
}
