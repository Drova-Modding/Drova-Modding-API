using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.DialogueNew;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_OverrideLookAtSpeaker"/>
    /// </summary>
    internal class DS_OverrideLookAtSpeakerNodeEditor : DrawNodeEditor
    {
        private DS_OverrideLookAtSpeaker _castedNode;
        private readonly Dictionary<string, string> _speakersNameToGuid = [];
        private readonly List<LookDirectionEditor> _lookDirectionEditors = [];

        public DS_OverrideLookAtSpeakerNodeEditor()
        {
            NodeSizeInternal = new Vector2(220, 40);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_OverrideLookAtSpeaker>();
            GraphEditorManager.DialogueTree.actorParameters.ForEach(new Action<DialogueTree.ActorParameter>(actor => _speakersNameToGuid.Add(actor.name, actor.ID)));
            for (int i = 0; i < _castedNode._lookDirectionOverrides._lookDirections.Count; i++)
            {
                LookDirectionsOverrides.ActorLookParam lookDirection = _castedNode._lookDirectionOverrides._lookDirections[i];
                _lookDirectionEditors.Add(new LookDirectionEditor(lookDirection, _speakersNameToGuid));
            }
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            GUI.color = Color.green;
            NodeSizeInternal = new Vector2(220, (_lookDirectionEditors.Count * 20) + 20);

            GUI.Box(new(position.x, position.y + 20, 220, NodeSizeInternal.y), "DS_OverrideLookAtSpeaker");

            GUI.color = Color.white;
            GUI.depth = 10;



            for (int i = 0; i < _lookDirectionEditors.Count; i++)
            {
                LookDirectionEditor lookDirectionEditor = _lookDirectionEditors[i];
                lookDirectionEditor.DrawLookDirectionEditor(new Vector2(position.x, position.y + 20 * (i + 1)));
            }

            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

    }

    internal class LookDirectionEditor
    {
        private readonly List<GUIDropdown> _actorsDropdown = [];
        private readonly Dictionary<string, string> _speakersNameToGuid;
        private readonly LookDirectionsOverrides.ActorLookParam _actorLookParam;
        private Vector2 _size = new(200, 20);

        public Vector2 Size => _size;

        public LookDirectionEditor(LookDirectionsOverrides.ActorLookParam actorLookParam, Dictionary<string, string> speakersNameToGuid)
        {
            _actorLookParam = actorLookParam;
            _speakersNameToGuid = speakersNameToGuid;
            List<string> nameList = _speakersNameToGuid.Keys.ToList();
            _actorsDropdown.Add(new GUIDropdown([.. nameList], nameList.FindIndex(name => name == _actorLookParam._fromActor.name)));
            _actorsDropdown.Add(new GUIDropdown([.. nameList], nameList.FindIndex(name => name == _actorLookParam._toActor.name)));
        }

        public void DrawLookDirectionEditor(Vector2 position)
        {
            if (_actorsDropdown[1].Draw(new Rect(position.x + 10, position.y + 40, 200, 20)))
            {
                string selectedName = _actorsDropdown[1].SelectedOption;
                if (_speakersNameToGuid.TryGetValue(selectedName, out string id))
                {
                    _actorLookParam._toActor._actorParameterID = id;
                    _actorLookParam._toActor.name = selectedName;
                }
            }

            if (_actorsDropdown[0].Draw(new Rect(position.x + 10, position.y + 20, 200, 20)))
            {
                string selectedName = _actorsDropdown[0].SelectedOption;
                if (_speakersNameToGuid.TryGetValue(selectedName, out string id))
                {
                    _actorLookParam._fromActor._actorParameterID = id;
                    _actorLookParam._fromActor.name = selectedName;
                }
            }
        }


    }
}
