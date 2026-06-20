using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.Talent;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_HasTalents"/>
    /// </summary>
    public class DS_HasTalentsTaskEditor : DrawTaskEditor
    {
        private DS_HasTalents? _castedTask;
        private TalentContainer[] _allTalents = Array.Empty<TalentContainer>();
        private readonly List<GUIDropdownWithFilter> _talentDropdowns = new();
        
        private float _lastHeight = 100f;

        /// <inheritdoc/>
        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_HasTalents>();
            if (_castedTask == null) return;

            // Fetch all TalentContainers from resources
            _allTalents = Resources.FindObjectsOfTypeAll<TalentContainer>()
                .OrderBy(t => t.name)
                .ToArray();

            _talentDropdowns.Clear();
            for (int i = 0; i < _castedTask.Talents.Count; i++)
            {
                TalentContainer talent = _castedTask.Talents[i];
                int selectedIndex = Array.FindIndex(_allTalents, t => t.Pointer == talent.Pointer);
                _talentDropdowns.Add(new GUIDropdownWithFilter(_allTalents.Select(t => t.name).ToArray(), selectedIndex, 20));
            }
        }

        /// <inheritdoc/>
        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null)
            {
                return default;
            }

            const float width = 340f;
            const float rowH = 22f;
            const float rowStep = 27f;

            float x = position.x;
            float y = position.y;

            // Background box
            Color previousColor = GUI.color;
            GUI.color = Color.cyan;
            Rect drawRect = new(x, y, width, _lastHeight);
            GUI.Box(drawRect, "Has Talents Task");
            GUI.color = previousColor;

            y += rowStep;

            for (int i = 0; i < _castedTask.Talents.Count; i++)
            {
                GUI.Label(new Rect(x + 5, y, 60, rowH), "Talent:");
                
                if (_talentDropdowns[i].Draw(new Rect(x + 70, y, width - 110, rowH)))
                {
                    _castedTask.Talents[i] = _allTalents[_talentDropdowns[i].SelectedIndex];
                }

                if (GUI.Button(new Rect(x + width - 35, y, 30, rowH), "X"))
                {
                    _castedTask.Talents.RemoveAt(i);
                    _talentDropdowns.RemoveAt(i);
                    i--;
                    continue;
                }
                
                y += rowStep;
            }

            if (GUI.Button(new Rect(x + 5, y, 100, rowH), "Add Talent"))
            {
                if (_allTalents.Length > 0)
                {
                    _castedTask.Talents.Add(_allTalents[0]);
                    _talentDropdowns.Add(new GUIDropdownWithFilter(_allTalents.Select(t => t.name).ToArray(), 0, 20));
                }
            }
            y += rowStep + 5;

            _lastHeight = y - position.y;
            drawRect.height = _lastHeight;

            return drawRect;
        }
    }
}