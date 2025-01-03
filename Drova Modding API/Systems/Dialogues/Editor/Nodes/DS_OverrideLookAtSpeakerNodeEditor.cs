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

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_OverrideLookAtSpeaker>();
            GraphEditorManager.DialogueTree.actorParameters.ForEach(new Action<DialogueTree.ActorParameter>(actor => _speakersNameToGuid.Add(actor.name, actor.ID)));
            for (int i = 0; i < _castedNode._lookDirectionOverrides._lookDirections.Count; i++)
            {
                var lookDirection = _castedNode._lookDirectionOverrides._lookDirections[i];
                _lookDirectionEditors.Add(new LookDirectionEditor(lookDirection, _speakersNameToGuid));
            }
        }

        public override Rect DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;

            GUI.color = Color.white;
            GUI.depth = 10;

            Rect rect = new(position.x, position.y + 20, 220, 20);

            for (int i = 0; i < _lookDirectionEditors.Count; i++)
            {
                var lookDirectionEditor = _lookDirectionEditors[i];
                lookDirectionEditor.DrawLookDirectionEditor(new Vector2(position.x, position.y + 20 * (i + 1)));
                rect.height += lookDirectionEditor.Size.y;
            }

            GUI.color = Color.green;
            GUI.Box(new Rect(position.x, position.y, 220, rect.height + 20), "DS_OverrideLookAtSpeaker");

            GUI.color = previousColor;
            GUI.depth = previousDepth;

            return rect;
        }

    }

    internal class LookDirectionEditor
    {
        private readonly List<GUIDropdown> _actorsDropdown = [];
        private readonly Dictionary<string, string> _speakersNameToGuid;
        private readonly LookDirectionsOverrides.ActorLookParam _actorLookParam;
        private Vector2 _size = new(200, 20);
        private readonly int _countOfNames;

        public Vector2 Size => _size;

        public LookDirectionEditor(LookDirectionsOverrides.ActorLookParam actorLookParam, Dictionary<string, string> speakersNameToGuid)
        {
            _actorLookParam = actorLookParam;
            _speakersNameToGuid = speakersNameToGuid;
            var nameList = _speakersNameToGuid.Keys.ToList();
            _countOfNames = nameList.Count;
            _actorsDropdown.Add(new GUIDropdown([.. nameList], nameList.FindIndex(name => name == _actorLookParam._fromActor.name)));
            _actorsDropdown.Add(new GUIDropdown([.. nameList], nameList.FindIndex(name => name == _actorLookParam._toActor.name)));
        }

        public void DrawLookDirectionEditor(Vector2 position)
        {
            if (_actorsDropdown[1].Draw(new Rect(position.x + 10, position.y + 40, 200, 20)))
            {
                var selectedName = _actorsDropdown[1].SelectedOption;
                if (_speakersNameToGuid.TryGetValue(selectedName, out string id))
                {
                    _actorLookParam._toActor._actorParameterID = id;
                    _actorLookParam._toActor.name = selectedName;
                }
            }

            if (_actorsDropdown[0].Draw(new Rect(position.x + 10, position.y + 20, 200, 20)))
            {
                var selectedName = _actorsDropdown[0].SelectedOption;
                if (_speakersNameToGuid.TryGetValue(selectedName, out string id))
                {
                    _actorLookParam._fromActor._actorParameterID = id;
                    _actorLookParam._fromActor.name = selectedName;
                }
            }

            var anyDrodpownOpen = _actorsDropdown.Any(dropdown => dropdown.IsDropdownShown);
            _size = new Vector2(200, 20 * (anyDrodpownOpen ? _countOfNames : 1));
        }


    }
}
