using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.GlobalVarSystem;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Task editor for <see cref="DS_HasAttribute"/>
    /// </summary>
    internal class DS_HasAttributeTaskEditor : DrawTaskEditor
    {
        private DS_HasAttribute? _castedTask;
        private GenericStatDesc[] _allStats = [];
        private readonly List<GUIDropdownWithFilter> _attributeDropdowns = [];
        private readonly List<GUIDropdown> _attributeComparers = [];
        private readonly List<GUIDropdown> _attributeMode = [];
        private readonly Dictionary<int, GUIGvarSelectionEditor> _gvarSelectionEditors = [];

        public override void Init()
        {
            _castedTask ??= Task.TryCast<DS_HasAttribute>();
            if (_castedTask == null) return;

            _allStats = Resources.FindObjectsOfTypeAll<GenericStatDesc>();
            for (int i = 0; i < _castedTask.Attributes.Count; i++)
            {
                DialogAttributeStat attribute = _castedTask.Attributes[i];
                int selectedIndex = attribute.Attribute != null ? Array.FindIndex(_allStats, id => id.Guid == attribute.Attribute.Guid) : -1;
                _attributeDropdowns.Add(new GUIDropdownWithFilter(_allStats.Select(e => e.GetLocaName()).ToArray(), selectedIndex, 20));
                _attributeComparers.Add(new GUIDropdown(Enum.GetNames<DialogAttributeStat.AttributeComparer>(), (int)attribute.Comparison));
                _attributeMode.Add(new GUIDropdown(Enum.GetNames<DialogAttributeStat.ValueMode>(), (int)attribute.Mode));
                if (attribute.Mode == DialogAttributeStat.ValueMode.GInt)
                {
                    _gvarSelectionEditors.TryAdd(i, new GUIGvarSelectionEditor(GvarType.INT, null, false, attribute.AmountVar));
                }
            }
        }

        public override Rect DrawTask(Vector2 position)
        {
            if (_castedTask == null) return default;

            for (int i = 0; i < _castedTask.Attributes.Count; i++)
            {
                DialogAttributeStat attribute = _castedTask.Attributes[i];
                GUIDropdownWithFilter attributeDropdown = _attributeDropdowns[i];
                GUIDropdown attributeComparer = _attributeComparers[i];
                GUIDropdown attributeMode = _attributeMode[i];

                if (GUI.Button(new Rect(position.x, position.y + 100, 120, 20), "Remove"))
                {
                    _castedTask.Attributes.RemoveAt(i);
                    _attributeDropdowns.RemoveAt(i);
                    _attributeComparers.RemoveAt(i);
                    _attributeMode.RemoveAt(i);
                    // Editors are keyed by index, so the ones after i would now be off by one. They are rebuilt on draw.
                    _gvarSelectionEditors.Clear();
                    i--;
                    continue;
                }
                Rect attributeRect = new(position.x, position.y + 20, 400, 20);
                if (attributeDropdown.Draw(attributeRect))
                {
                    attribute.Attribute = _allStats[attributeDropdown.SelectedIndex];
                }
                attributeRect.y += 20;
                if (attributeComparer.Draw(attributeRect))
                {
                    attribute.Comparison = (DialogAttributeStat.AttributeComparer)attributeComparer.SelectedIndex;
                }
                attributeRect.y += 20;
                if (attributeMode.Draw(attributeRect))
                {
                    attribute.Mode = (DialogAttributeStat.ValueMode)attributeMode.SelectedIndex;
                }
                attributeRect.y += 20;
                if (attribute.Mode == DialogAttributeStat.ValueMode.GInt)
                {
                    if (!_gvarSelectionEditors.TryGetValue(i, out GUIGvarSelectionEditor? gvarEditor) || gvarEditor == null)
                    {
                        gvarEditor = new GUIGvarSelectionEditor(GvarType.INT, null, false, attribute.AmountVar);
                        _gvarSelectionEditors[i] = gvarEditor;
                    }
                    if (gvarEditor.DrawGvarEditor(attributeRect, attributeRect) && gvarEditor.CurrentSelectedGvar != null)
                    {
                        attribute.AmountVar = gvarEditor.CurrentSelectedGvar.TryCast<GInt>();
                    }
                }
                else
                {
                    GUI.Label(new Rect(attributeRect.x + 5, attributeRect.y, 70, 20), "Amount:");
                    string tempInputValue = GUI.TextField(new Rect(attributeRect.x + 80, attributeRect.y, 220 - 70, 20), attribute.Amount.ToString());
                    if (int.TryParse(tempInputValue, out int result))
                    {
                        attribute.Amount = result;
                    }
                }
            }

            if (GUI.Button(new Rect(position.x, position.y + 20 + (_castedTask.Attributes.Count * 120), 120, 20), "Add Attribute"))
            {
                _castedTask.Attributes.Add(new DialogAttributeStat());
                _attributeDropdowns.Add(new GUIDropdownWithFilter(_allStats.Select(e => e.GetLocaName()).ToArray(), 0, 20));
                _attributeComparers.Add(new GUIDropdown(Enum.GetNames<DialogAttributeStat.AttributeComparer>(), 0));
                _attributeMode.Add(new GUIDropdown(Enum.GetNames<DialogAttributeStat.ValueMode>(), 0));
            }

            Rect drawRect = new(position.x, position.y, 450, 40 + (_castedTask.Attributes.Count * 120));
            Color previousColor = GUI.color;
            GUI.color = Color.green;
            GUI.Box(drawRect, "DS_HasAttribute");

            GUI.color = previousColor;

            return drawRect;
        }
    }
}