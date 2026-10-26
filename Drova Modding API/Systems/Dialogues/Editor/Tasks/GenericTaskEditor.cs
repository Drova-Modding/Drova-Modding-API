using System.Diagnostics.CodeAnalysis;
using System.Text;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.Framework;
using MelonLoader;
using UnityEngine;
using BindingFlags = Il2CppSystem.Reflection.BindingFlags;
using CultureInfo = Il2CppSystem.Globalization.CultureInfo;
using FieldInfo = Il2CppSystem.Reflection.FieldInfo;
using Il2CppType = Il2CppSystem.Type;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Tasks
{
    /// <summary>
    /// Fallback editor for tasks without a dedicated one. Edits bool, number, string and enum fields, also inside a
    /// <see cref="BBParameter"/>, and shows other fields read-only. NodeCanvas framework fields are skipped, except invert.
    /// </summary>
    public class GenericTaskEditor : DrawTaskEditor
    {
        private const float Width = 380;
        private const float RowHeight = 22;
        private const float LabelWidth = 170;

        private static readonly Il2CppSystem.IFormatProvider Invariant = CultureInfo.InvariantCulture.Cast<Il2CppSystem.IFormatProvider>();

        private readonly List<Row> _rows = [];
        private Il2CppSystem.Object _target = null!;
        private string _title = string.Empty;

        /// <summary>
        /// The listed fields as <c>name=value</c> (<c>!</c> marks read-only ones), for tests and diagnostics.
        /// </summary>
        public IEnumerable<string> FieldSummary => _rows.Select(row => (row.Editable ? string.Empty : "!") + row.Name + "=" + row.Text);

        /// <summary>
        /// Whether any field is listed.
        /// </summary>
        public bool HasFields => _rows.Count > 0;

        /// <inheritdoc/>
        public override void Init()
        {
            InitFor(Task, false);

            ConditionTask? condition = Task.TryCast<ConditionTask>();
            if (condition == null) return;

            FieldInfo? invert = Il2CppInterop.Runtime.Il2CppType.Of<ConditionTask>().GetField("_invert", BindingFlags.Instance | BindingFlags.NonPublic);
            if (invert != null) _rows.Add(CreateRow(invert, "Invert (pass when false)"));
        }

        /// <summary>
        /// Lists the fields of any object instead of <see cref="DrawTaskEditor.Task"/>, such as a node's own settings.
        /// </summary>
        /// <param name="target">The object whose fields are edited</param>
        /// <param name="publicOnly">Lists only public fields, which leaves out runtime state</param>
        internal void InitFor(Il2CppSystem.Object target, bool publicOnly)
        {
            _target = target;
            _rows.Clear();
            Il2CppType? type = target.GetIl2CppType();
            _title = type.Name;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            if (!publicOnly) flags |= BindingFlags.NonPublic;
            while (type != null && type.Namespace != "NodeCanvas.Framework")
            {
                foreach (FieldInfo field in type.GetFields(flags))
                {
                    if (!field.Name.StartsWith('<')) _rows.Add(CreateRow(field, null));
                }
                type = type.BaseType;
            }
        }

        /// <inheritdoc/>
        public override Rect DrawTask(Vector2 position)
        {
            Rect box = new(position.x, position.y, Width, 25 + (_rows.Count * RowHeight));
            GUI.Box(box, _title);

            for (int i = 0; i < _rows.Count; i++)
            {
                DrawRow(_rows[i], new Rect(position.x + 10, position.y + 22 + (i * RowHeight), Width - 20, 20));
            }

            return box;
        }

        /// <summary>
        /// Turns a field name into a label: <c>_repeatTimes</c> or <c>m_repeatTimes</c> becomes <c>Repeat times</c>.
        /// </summary>
        internal static string ToLabel(string fieldName)
        {
            string name = fieldName.StartsWith("m_", StringComparison.Ordinal) ? fieldName[2..] : fieldName.TrimStart('_');
            if (name.Length == 0) return fieldName;

            StringBuilder builder = new(name.Length + 4);
            builder.Append(char.ToUpperInvariant(name[0]));
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '_')
                {
                    builder.Append(' ');
                    continue;
                }
                if (char.IsUpper(c) && !char.IsUpper(name[i - 1]) && name[i - 1] != '_')
                {
                    builder.Append(' ').Append(char.ToLowerInvariant(c));
                    continue;
                }
                builder.Append(c);
            }
            return builder.ToString();
        }

        private Row CreateRow(FieldInfo field, string? label)
        {
            Row row = new(field, field.Name, label ?? ToLabel(field.Name));
            try
            {
                Il2CppType fieldType = field.FieldType;
                if (fieldType.IsSubclassOf(Il2CppInterop.Runtime.Il2CppType.Of<BBParameter>()))
                {
                    row.Parameter = field.GetValue(_target)?.TryCast<BBParameter>();
                    row.ValueType = row.Parameter?.varType;
                }
                else
                {
                    row.ValueType = fieldType;
                }

                bool linked = row.Parameter != null && row.Parameter.useBlackboard;
                row.Editable = row.ValueType != null && IsSupported(row.ValueType) && !linked;
                row.IsBool = row.ValueType?.FullName == "System.Boolean";
                Il2CppSystem.Object? value = Read(row);
                row.Text = value == null ? string.Empty : Il2CppSystem.Convert.ToString(value, Invariant);
                if (row.Editable && row.ValueType!.IsEnum) SetUpEnum(row, value);

                if (linked) row.Note = $"linked to variable '{row.Parameter!.name}'";
                else if (!row.Editable) row.Note = (row.Text.Length == 0 ? "empty" : row.Text) + " (not editable here)";
            }
            catch (Exception e)
            {
                row.Editable = false;
                row.Note = "unreadable";
                MelonLogger.Warning($"GenericTaskEditor: field '{field.Name}' of {_title}: {e.Message}");
            }
            return row;
        }

        /// <summary>
        /// Enum values are not their position in the name list (flag enums go 1, 2, 4, ...), so the dropdown maps
        /// positions to the real values. Flag enums and values that combine flags are typed as comma separated names.
        /// </summary>
        private static void SetUpEnum(Row row, Il2CppSystem.Object? value)
        {
            Il2CppType type = row.ValueType!;
            if (type.IsDefined(Il2CppInterop.Runtime.Il2CppType.Of<Il2CppSystem.FlagsAttribute>(), false))
            {
                row.Label += " (names, comma separated)";
                return;
            }

            Il2CppSystem.Array values = Il2CppSystem.Enum.GetValues(type);
            long current = value == null ? long.MinValue : Il2CppSystem.Convert.ToInt64(value, Invariant);
            Il2CppSystem.Object[] enumValues = new Il2CppSystem.Object[values.Length];
            string[] options = new string[values.Length];
            int index = -1;
            for (int i = 0; i < values.Length; i++)
            {
                Il2CppSystem.Object enumValue = values.GetValue(i);
                enumValues[i] = enumValue;
                options[i] = Il2CppSystem.Convert.ToString(enumValue, Invariant);
                if (index < 0 && Il2CppSystem.Convert.ToInt64(enumValue, Invariant) == current) index = i;
            }

            if (index < 0)
            {
                row.Label += " (names, comma separated)";
                return;
            }
            row.EnumValues = enumValues;
            row.EnumOptions = options;
            row.EnumIndex = index;
        }

        private void DrawRow(Row row, Rect rect)
        {
            Rect label = new(rect.x, rect.y, LabelWidth, rect.height);
            Rect value = new(rect.x + LabelWidth + 5, rect.y, rect.width - LabelWidth - 5, rect.height);
            GUI.Label(label, row.Label);

            if (!row.Editable)
            {
                GUI.Label(value, row.Note);
                return;
            }

            if (row.EnumValues != null)
            {
                row.EnumDropdown ??= new GUIDropdown(row.EnumOptions!, row.EnumIndex);
                if (row.EnumDropdown.Draw(value) && row.EnumDropdown.SelectedIndex >= 0)
                {
                    Write(row, row.EnumValues[row.EnumDropdown.SelectedIndex]);
                }
                return;
            }

            if (row.IsBool)
            {
                bool current = row.Text == "True";
                bool next = GUI.Toggle(value, current, string.Empty);
                if (next != current && TryParse(next.ToString(), row.ValueType!, out Il2CppSystem.Object? parsedBool) && Write(row, parsedBool))
                {
                    row.Text = next.ToString();
                }
                return;
            }

            string text = GUI.TextField(value, row.Text);
            if (text == row.Text) return;

            row.Text = text;
            if (TryParse(text, row.ValueType!, out Il2CppSystem.Object? parsed)) Write(row, parsed);
        }

        private Il2CppSystem.Object? Read(Row row)
        {
            return row.Parameter != null ? row.Parameter.value : row.Field.GetValue(_target);
        }

        private bool Write(Row row, Il2CppSystem.Object value)
        {
            try
            {
                if (row.Parameter != null) row.Parameter.value = value;
                else row.Field.SetValue(_target, value);
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"GenericTaskEditor: could not write '{row.Name}': {e.Message}");
                return false;
            }
        }

        private static bool TryParse(string text, Il2CppType type, [NotNullWhen(true)] out Il2CppSystem.Object? value)
        {
            try
            {
                if (type.IsEnum)
                {
                    value = Il2CppSystem.Enum.Parse(type, text.Trim(), true);
                    return value != null;
                }
                string normalized = type.FullName == "System.String" ? text : text.Replace(',', '.');
                value = Il2CppSystem.Convert.ChangeType((Il2CppSystem.String)normalized, type, Invariant);
                return value != null;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        private static bool IsSupported(Il2CppType type)
        {
            return type.IsEnum || type.FullName is "System.Boolean" or "System.Byte" or "System.Int16" or "System.Int32" or "System.Int64"
                or "System.Single" or "System.Double" or "System.String";
        }

        private sealed class Row(FieldInfo field, string name, string label)
        {
            public FieldInfo Field { get; } = field;
            public string Name { get; } = name;
            public string Label { get; set; } = label;
            public string Note { get; set; } = string.Empty;
            public BBParameter? Parameter { get; set; }
            public Il2CppType? ValueType { get; set; }
            public bool Editable { get; set; }
            public bool IsBool { get; set; }
            public string Text { get; set; } = string.Empty;
            public Il2CppSystem.Object[]? EnumValues { get; set; }
            public string[]? EnumOptions { get; set; }
            public int EnumIndex { get; set; }

            /// <summary>
            /// Created on the first draw: building a dropdown touches GUI styles, which only exist inside OnGUI.
            /// </summary>
            public GUIDropdown? EnumDropdown { get; set; }
        }
    }
}
