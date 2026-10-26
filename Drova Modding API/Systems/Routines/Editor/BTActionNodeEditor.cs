using System.Reflection;
using Drova_Modding_API.Systems.Dialogues.Editor;
using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.Framework;
using MelonLoader;
using UnityEngine;
using BTActionNode = Il2CppNodeCanvas.BehaviourTrees.ActionNode;
using DTActionNode = Il2CppNodeCanvas.DialogueTrees.ActionNode;
using Il2CppType = Il2CppInterop.Runtime.Il2CppType;

namespace Drova_Modding_API.Systems.Routines.Editor
{
    /// <summary>
    /// Editor for the behaviour tree <see cref="BTActionNode"/> and the dialogue <see cref="DTActionNode"/>: edits its
    /// action task and replaces it with any <see cref="ActionTask"/> type the game ships.
    /// </summary>
    internal class BTActionNodeEditor : DrawNodeEditor
    {
        private const float Width = 400;

        private static Dictionary<string, Type>? _actionTypes;
        private static string[]? _pickerLabels;
        private static Type[]? _pickerTypes;

        private BTActionNode? _castedNode;
        private DTActionNode? _dialogueNode;
        private DrawTaskEditor? _taskEditor;
        private GUIDropdownWithFilter? _actionPicker;
        private float _lastTaskHeight = 20;

        public BTActionNodeEditor()
        {
            NodeSizeInternal = new Vector2(Width, 80);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<BTActionNode>();
            _dialogueNode ??= Node.TryCast<DTActionNode>();
            if (_castedNode == null && _dialogueNode == null) return;

            ActionTask? action = _castedNode != null ? _castedNode.action : _dialogueNode!.action;
            _taskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(action?.GetIl2CppType());
            if (_taskEditor == null) return;

            if (action != null) _taskEditor.Task = action;
            _taskEditor.GraphEditorManager = GraphEditorManager;
            _taskEditor.Init();
        }

        public override void DrawNode(Vector2 position)
        {
            if ((_castedNode == null && _dialogueNode == null) || _taskEditor == null) return;

            NodeSizeInternal = new Vector2(Width, _lastTaskHeight + 70);
            GUI.Box(new Rect(position.x, position.y, Width, NodeSizeInternal.y), "ActionNode");
            _lastTaskHeight = _taskEditor.DrawTask(new Vector2(position.x, position.y + 20)).height;

            Rect pickerRect = new(position.x + 10, position.y + 30 + _lastTaskHeight, Width - 20, 20);
            if (_actionPicker == null)
            {
                if (GUI.Button(pickerRect, "Change Action (type to search)"))
                {
                    LoadActionTypes();
                    _actionPicker = new GUIDropdownWithFilter(_pickerLabels!, -1, 15);
                }
                return;
            }

            if (_actionPicker.Draw(pickerRect) && _actionPicker.SelectedIndex >= 0)
            {
                SetAction(_pickerTypes![_actionPicker.SelectedIndex]);
                _actionPicker = null;
            }
        }

        private void SetAction(Type type)
        {
            try
            {
                ActionTask? action = Il2CppSystem.Activator.CreateInstance(Il2CppType.From(type))?.TryCast<ActionTask>();
                if (action == null) return;

                GraphEditorManager.Graph?.allTasks.Add(action);
                if (_castedNode != null) _castedNode.action = action;
                else _dialogueNode!.action = action;
                Init();
            }
            catch (Exception e)
            {
                MelonLogger.Error($"BTActionNodeEditor: could not create {type.FullName}: {e}");
                GraphEditorManager.ShowStatus($"{type.Name} cannot be used as an action.", true);
            }
        }

        /// <summary>
        /// Every concrete <see cref="ActionTask"/> in the interop assemblies, by type name. Names shared across
        /// namespaces are keyed by full name.
        /// </summary>
        internal static Dictionary<string, Type> ActionTypes
        {
            get
            {
                LoadActionTypes();
                return _actionTypes!;
            }
        }

        private static void LoadActionTypes()
        {
            if (_actionTypes != null) return;

            List<Type> types = FindActionTypes();
            HashSet<string> sharedNames = types.GroupBy(type => type.Name).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
            Dictionary<string, Type> byKey = new(StringComparer.Ordinal);
            foreach (Type type in types)
            {
                byKey.TryAdd(sharedNames.Contains(type.Name) ? GetDisplayNamespace(type) + "." + type.Name : type.Name, type);
            }

            _pickerTypes = [.. byKey.Values];
            _pickerLabels = _pickerTypes.Select(type => $"{type.Name}   ({GetDisplayNamespace(type)})").ToArray();
            _actionTypes = byKey;
        }

        private static string GetDisplayNamespace(Type type)
        {
            string ns = type.Namespace ?? string.Empty;
            return ns.StartsWith("Il2Cpp", StringComparison.Ordinal) ? ns[6..] : ns;
        }

        /// <summary>
        /// Interop wrappers of abstract classes are not abstract themselves, so the IL2CPP type is asked instead.
        /// FlowCanvas and dialogue tasks are left out, since a routine NPC cannot run them.
        /// </summary>
        private static List<Type> FindActionTypes()
        {
            List<Type> types = [];
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetName().Name!.StartsWith("Il2Cpp", StringComparison.Ordinal)) continue;

                Type?[] candidates;
                try
                {
                    candidates = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    candidates = e.Types;
                }

                foreach (Type? type in candidates)
                {
                    if (type == null || type.ContainsGenericParameters || !type.IsSubclassOf(typeof(ActionTask))) continue;
                    string ns = type.Namespace ?? string.Empty;
                    if (ns.Contains("FlowCanvas", StringComparison.Ordinal) || ns.Contains("Dialogue", StringComparison.Ordinal)) continue;
                    if (IsAbstract(type)) continue;
                    types.Add(type);
                }
            }
            return types.OrderBy(type => type.Name, StringComparer.Ordinal).ToList();
        }

        private static bool IsAbstract(Type type)
        {
            try
            {
                return type.IsAbstract || Il2CppType.From(type, false)?.IsAbstract != false;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
