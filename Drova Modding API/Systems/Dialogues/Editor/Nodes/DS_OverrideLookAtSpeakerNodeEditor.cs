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
        private DS_OverrideLookAtSpeaker? _castedNode;
        private GUIDropdown _styleDropdown;
        private GUIDropdown _tempoDropdown;
        private readonly Dictionary<string, string> _speakersNameToGuid = [];
        private readonly List<LookDirectionEditor> _lookDirectionEditors = [];

        public DS_OverrideLookAtSpeakerNodeEditor()
        {
            NodeSizeInternal = new Vector2(220, 40);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_OverrideLookAtSpeaker>();
            if (_castedNode._availableOptions == null)
            {
                _castedNode._availableOptions = new DialogueRunTimeOptions();
            }
            _styleDropdown = new GUIDropdown(Enum.GetNames<Style>(), (int)_castedNode._availableOptions.Style);
            _tempoDropdown = new GUIDropdown(Enum.GetNames<Tempo>(), (int)_castedNode._availableOptions.TextSpeed);

            GraphEditorManager.DialogueTree.actorParameters.ForEach(new Action<DialogueTree.ActorParameter>(actor => _speakersNameToGuid.TryAdd(actor.name, actor.ID)));
            if (_castedNode._lookDirectionOverrides != null)
            {
                for (int i = 0; i < _castedNode._lookDirectionOverrides._lookDirections.Count; i++)
                {
                    LookDirectionsOverrides.ActorLookParam lookDirection = _castedNode._lookDirectionOverrides._lookDirections[i];
                    _lookDirectionEditors.Add(new LookDirectionEditor(lookDirection, _speakersNameToGuid));
                }
            }
            else
            {
                _castedNode._lookDirectionOverrides = new LookDirectionsOverrides();
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
            int lookDirectionHeight = _lookDirectionEditors.Count * 20;
            int optionsHeight = 160;
            NodeSizeInternal = new Vector2(320, lookDirectionHeight + optionsHeight + 40);

            GUI.Box(new(position.x, position.y + 20, NodeSizeInternal.x, NodeSizeInternal.y), "DS_OverrideLookAtSpeaker");

            GUI.color = Color.white;
            GUI.depth = 10;

            float currentY = position.y + 40;

            _castedNode._availableOptions.WaitForInput = GUI.Toggle(new Rect(position.x + 5, currentY, 250, 20), _castedNode._availableOptions.WaitForInput, "Wait For Input");
            currentY += 20;
            _castedNode._availableOptions.OverrideWaitForInput = GUI.Toggle(new Rect(position.x + 5, currentY, 250, 20), _castedNode._availableOptions.OverrideWaitForInput, "Override Wait For Input");
            currentY += 20;
            _castedNode._availableOptions.OverrideLookAtSpeaker = GUI.Toggle(new Rect(position.x + 5, currentY, 250, 20), _castedNode._availableOptions.OverrideLookAtSpeaker, "Override Look At Speaker");
            currentY += 20;
            _castedNode._availableOptions.OverrideSetAndLockCam = GUI.Toggle(new Rect(position.x + 5, currentY, 250, 20), _castedNode._availableOptions.OverrideSetAndLockCam, "Override Set And Lock Cam");
            currentY += 20;
            _castedNode._availableOptions.PlayerToNormaleState = GUI.Toggle(new Rect(position.x + 5, currentY, 250, 20), _castedNode._availableOptions.PlayerToNormaleState, "Player To Normal State");
            currentY += 20;

            GUI.Label(new Rect(position.x + 10, currentY, 85, 20), "Style:");
            if (_styleDropdown.Draw(new Rect(position.x + 100, currentY, 200, 20)))
            {
                _castedNode._availableOptions.Style = (Style)_styleDropdown.SelectedIndex;
            }
            currentY += 20;

            GUI.Label(new Rect(position.x + 10, currentY, 85, 20), "Tempo:");
            if (_tempoDropdown.Draw(new Rect(position.x + 100, currentY, 200, 20)))
            {
                _castedNode._availableOptions.TextSpeed = (Tempo)_tempoDropdown.SelectedIndex;
            }
            currentY += 30;

            for (int i = 0; i < _lookDirectionEditors.Count; i++)
            {
                LookDirectionEditor lookDirectionEditor = _lookDirectionEditors[i];
                lookDirectionEditor.DrawLookDirectionEditor(new Vector2(position.x, currentY + (20 * i)));
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
            List<string> nameList = [.. _speakersNameToGuid.Keys];
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