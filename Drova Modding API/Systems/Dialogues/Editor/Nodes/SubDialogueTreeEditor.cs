using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;
using System.Text;
using UnityEngine;
using static Drova_Modding_API.Systems.Dialogues.ActorParameterHelper;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="SubDialogueTree"/>.
    /// <para>
    /// Besides selecting the sub-graph, this exposes the otherwise hidden
    /// <c>_actorParametersMap</c>. That map binds each sub-graph actor slot to a
    /// parent (containing) tree actor. The game stores it as
    /// <c>sub-graph slot ID -&gt; parent slot ID</c> (both GUIDs) and consumes it in
    /// <c>TryWriteMappedActorParameters()</c> via <c>GetParameterByID</c>. Vanilla's
    /// <c>SetParametersMap()</c> only ever fills this positionally (slot i -&gt; slot i),
    /// so we mirror that as the default but let each row be reassigned freely.
    /// </para>
    /// </summary>
    internal class SubDialogueTreeEditor : DrawNodeEditor
    {
        private SubDialogueTree? _castedNode;
        private readonly GUIContent GUIContent = new("SubDialogueTree", "Execute a Sub Dialogue Tree. When that Dialogue Tree is finished, this node will continue either in Success or Failure if it has any connections. Useful for making reusable and self-contained Dialogue Trees.");
        private DialogueTree[] _dialogueTrees;
        private string[] _dialogueTreeNames;
        private GUIDropdownWithFilter _dialogueTreeDropdown;

        // ── Actor parameter mapping (sub-graph slot -> parent actor) ──────────
        private const float BaseHeight = 110f;
        private const float MappingHeaderY = 80f;
        private const float MappingRowY = 100f;
        private const float MappingRowHeight = 24f;

        // The sub-graph the mapping rows were last built for; used to detect changes.
        private DialogueTree _mappingSubGraph;
        // _actorParametersMap key per sub-graph actor slot (the slot's ID/GUID).
        private string[] _subGraphActorIds = [];
        // Display label per sub-graph actor slot.
        private string[] _subGraphActorLabels = [];
        // _actorParametersMap value per parent actor option (the parent slot's ID/GUID).
        private string[] _parentActorIds = [];
        // Display label per parent actor dropdown option.
        private string[] _parentActorLabels = [];
        // One dropdown per sub-graph actor slot.
        private GUIDropdown[] _actorMappingDropdowns = [];

        public SubDialogueTreeEditor()
        {
            NodeSizeInternal = new Vector2(850, BaseHeight);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<SubDialogueTree>();
            _dialogueTrees = Resources.FindObjectsOfTypeAll<DialogueTree>();
            _dialogueTreeNames = [.. _dialogueTrees.Select(e => e.name)];
            _dialogueTreeDropdown = new GUIDropdownWithFilter(_dialogueTreeNames, Array.FindIndex(_dialogueTreeNames, (e) => e == _castedNode.subGraph?.name), 20);
            BuildActorMapping();
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            // Rebuild mapping rows when the selected sub-graph changed.
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

        /// <summary>
        /// Rebuilds the mapping rows from the selected sub-graph's actor parameters
        /// and the parent dialogue tree's actor parameters.
        /// </summary>
        private void BuildActorMapping()
        {
            _mappingSubGraph = _castedNode?.subGraph;

            BuildParentActorOptions();

            DialogueTree subGraph = _castedNode?.subGraph;
            if (subGraph != null && (subGraph.IsLazyLoading || subGraph.allNodes.Count == 0))
            {
                subGraph.SelfDeserialize();
            }
            subGraph?.DeserializeIfNotDoneYet(true);

            int subCount = subGraph?.actorParameters?.Count ?? 0;
            _subGraphActorIds = new string[subCount];
            _subGraphActorLabels = new string[subCount];
            _actorMappingDropdowns = new GUIDropdown[subCount];

            for (int i = 0; i < subCount; i++)
            {
                DialogueTree.ActorParameter actorParameter = subGraph.actorParameters[i];
                string slotId = GetActorParameterId(actorParameter);
                _subGraphActorIds[i] = slotId;
                _subGraphActorLabels[i] = GetActorParameterName(actorParameter);

                string mappedValue = GetMapping(slotId);
                int selectedIndex = Array.IndexOf(_parentActorIds, mappedValue);

                // No saved binding for this slot: default positionally (slot i ->
                // parent slot i), mirroring the game's SetParametersMap, and persist
                // it so the saved map is complete.
                if (selectedIndex < 0 && i < _parentActorIds.Length)
                {
                    selectedIndex = i;
                    SetMapping(slotId, _parentActorIds[i]);
                }

                _actorMappingDropdowns[i] = new GUIDropdown(_parentActorLabels, selectedIndex);
            }

            // Resize the node so the mapping rows fit inside the box.
            float height = subCount == 0
                ? BaseHeight
                : MappingRowY + (subCount * MappingRowHeight) + 10f;
            NodeSizeInternal = new Vector2(NodeSizeInternal.x, height);
        }

        private void BuildParentActorOptions()
        {
            DialogueTree parent = GraphEditorManager?.DialogueTree;
            int parentCount = parent?.actorParameters?.Count ?? 0;
            _parentActorIds = new string[parentCount];
            _parentActorLabels = new string[parentCount];
            for (int i = 0; i < parentCount; i++)
            {
                DialogueTree.ActorParameter actorParameter = parent.actorParameters[i];
                _parentActorIds[i] = GetActorParameterId(actorParameter);
                _parentActorLabels[i] = GetActorParameterName(actorParameter);
            }
        }

        private string GetMapping(string key)
        {
            Il2CppSystem.Collections.Generic.Dictionary<string, string> map = _castedNode._actorParametersMap;
            if (map != null && key != null && map.ContainsKey(key))
            {
                return map[key];
            }
            return string.Empty;
        }

        private void SetMapping(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;

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
