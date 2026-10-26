using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;
using System.Text;
using UnityEngine;
using static Drova_Modding_API.Systems.Dialogues.ActorParameterHelper;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="SubDialogueTree"/>. Also edits the hidden <c>_actorParametersMap</c>
    /// (sub-graph slot GUID to parent slot GUID), which vanilla only ever fills positionally.
    /// </summary>
    internal class SubDialogueTreeEditor : DrawNodeEditor
    {
        private SubDialogueTree? _castedNode;
        private readonly GUIContent GUIContent = new("SubDialogueTree", "Execute a Sub Dialogue Tree. When that Dialogue Tree is finished, this node will continue either in Success or Failure if it has any connections. Useful for making reusable and self-contained Dialogue Trees.");
        private DialogueTree[] _dialogueTrees = [];
        private string[] _dialogueTreeNames;
        private GUIDropdownWithFilter _dialogueTreeDropdown;

        private const float BaseHeight = 110f;
        private const float MappingHeaderY = 80f;
        private const float MappingRowY = 100f;
        private const float MappingRowHeight = 24f;

        private DialogueTree? _mappingSubGraph;
        private string[] _subGraphActorIds = [];
        private string[] _subGraphActorLabels = [];
        private string[] _parentActorIds = [];
        private string[] _parentActorLabels = [];
        private GUIDropdown[] _actorMappingDropdowns = [];

        public SubDialogueTreeEditor()
        {
            NodeSizeInternal = new Vector2(850, BaseHeight);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<SubDialogueTree>();
            if (_castedNode == null) return;

            _dialogueTrees = Resources.FindObjectsOfTypeAll<DialogueTree>();
            _dialogueTreeNames = [.. _dialogueTrees.Select(e => e.name)];
            DialogueTree? subGraph = _castedNode.subGraph;
            string? subGraphName = subGraph == null ? null : subGraph.name;
            _dialogueTreeDropdown = new GUIDropdownWithFilter(_dialogueTreeNames, Array.FindIndex(_dialogueTreeNames, (e) => e == subGraphName), 20);
            BuildActorMapping();
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            if (_castedNode.subGraph != _mappingSubGraph)
            {
                BuildActorMapping();
            }

            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;

            GUI.Box(new Rect(position.x, position.y, NodeSizeInternal.x, NodeSizeInternal.y), GUIContent);

            GUI.color = Color.white;

            StringBuilder sb = new();
            if (_castedNode.subGraph == null)
            {
                sb.Append("No Sub Dialogue Tree selected");
            }
            else
                sb.Append("Full name: ").Append(_castedNode.subGraph.Key);

            GUI.Label(new Rect(position.x + 10, position.y + 25, 800, 25), sb.ToString());

            if (_dialogueTreeDropdown.Draw(new Rect(position.x + 10, position.y + 55, 400, 20)))
            {
                _castedNode.subGraph = _dialogueTrees[_dialogueTreeDropdown.SelectedIndex];
                BuildActorMapping();
            }

            DrawActorMapping(position);

            GUI.depth = previousDepth;
            GUI.color = previousColor;

        }

        public override void OnDoubleClick(Vector2 mousePosition)
        {
            base.OnDoubleClick(mousePosition);
            if (_castedNode == null) return;

            GraphEditorManager.GoIntoSubGraph(_castedNode);
        }

        private void DrawActorMapping(Vector2 position)
        {
            if (_subGraphActorIds.Length == 0) return;

            GUI.Label(new Rect(position.x + 10, position.y + MappingHeaderY, 800, 20), "Actor mapping (sub-graph slot → parent actor):");

            for (int i = 0; i < _subGraphActorIds.Length; i++)
            {
                float rowY = position.y + MappingRowY + (i * MappingRowHeight);
                GUI.Label(new Rect(position.x + 10, rowY, 280, 20), _subGraphActorLabels[i]);

                Rect dropdownRect = new(position.x + 300, rowY, 300, 20);
                if (_actorMappingDropdowns[i].Draw(dropdownRect) && _actorMappingDropdowns[i].SelectedIndex >= 0)
                {
                    SetMapping(_subGraphActorIds[i], _parentActorIds[_actorMappingDropdowns[i].SelectedIndex]);
                }
            }
        }

        private void BuildActorMapping()
        {
            _mappingSubGraph = _castedNode?.subGraph;

            BuildParentActorOptions();

            DialogueTree? subGraph = _castedNode?.subGraph;
            if (subGraph != null)
            {
                if (subGraph.IsLazyLoading || subGraph.allNodes.Count == 0)
                {
                    subGraph.SelfDeserialize();
                }
                subGraph.DeserializeIfNotDoneYet(true);
            }

            Il2CppSystem.Collections.Generic.List<DialogueTree.ActorParameter>? subParameters = subGraph != null ? subGraph.actorParameters : null;
            int subCount = subParameters?.Count ?? 0;
            _subGraphActorIds = new string[subCount];
            _subGraphActorLabels = new string[subCount];
            _actorMappingDropdowns = new GUIDropdown[subCount];

            for (int i = 0; subParameters != null && i < subCount; i++)
            {
                DialogueTree.ActorParameter actorParameter = subParameters[i];
                string slotId = GetActorParameterId(actorParameter);
                _subGraphActorIds[i] = slotId;
                _subGraphActorLabels[i] = GetActorParameterName(actorParameter);

                string mappedValue = GetMapping(slotId);
                int selectedIndex = Array.IndexOf(_parentActorIds, mappedValue);

                // Unbound slots default positionally like the game's SetParametersMap, and are persisted so the saved map is complete.
                if (selectedIndex < 0 && i < _parentActorIds.Length)
                {
                    selectedIndex = i;
                    SetMapping(slotId, _parentActorIds[i]);
                }

                _actorMappingDropdowns[i] = new GUIDropdown(_parentActorLabels, selectedIndex);
            }

            float height = subCount == 0
                ? BaseHeight
                : MappingRowY + (subCount * MappingRowHeight) + 10f;
            NodeSizeInternal = new Vector2(NodeSizeInternal.x, height);
        }

        private void BuildParentActorOptions()
        {
            DialogueTree? parent = GraphEditorManager.DialogueTree;
            Il2CppSystem.Collections.Generic.List<DialogueTree.ActorParameter>? parentParameters = parent != null ? parent.actorParameters : null;
            int parentCount = parentParameters?.Count ?? 0;
            _parentActorIds = new string[parentCount];
            _parentActorLabels = new string[parentCount];
            for (int i = 0; parentParameters != null && i < parentCount; i++)
            {
                DialogueTree.ActorParameter actorParameter = parentParameters[i];
                _parentActorIds[i] = GetActorParameterId(actorParameter);
                _parentActorLabels[i] = GetActorParameterName(actorParameter);
            }
        }

        private string GetMapping(string key)
        {
            if (_castedNode == null) return string.Empty;

            Il2CppSystem.Collections.Generic.Dictionary<string, string> map = _castedNode._actorParametersMap;
            if (map != null && map.ContainsKey(key))
            {
                return map[key];
            }
            return string.Empty;
        }

        private void SetMapping(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || _castedNode == null) return;

            Il2CppSystem.Collections.Generic.Dictionary<string, string> map = _castedNode._actorParametersMap;
            if (map == null)
            {
                map = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();
                _castedNode._actorParametersMap = map;
            }
            map[key] = value;
        }
    }
}
