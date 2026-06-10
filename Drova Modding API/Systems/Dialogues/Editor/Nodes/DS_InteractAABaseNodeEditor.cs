using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.ActorActions;
using Il2CppNodeCanvas.DialogueTrees;
using System.Collections.Immutable;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_InteractAABaseNodeEditor : DrawNodeEditor
    {
        private DS_InteractAABaseNode? _castedNode;

        private readonly GUIContent _hideDialogueWindowContent = new("Hide Dialogue Window");
        private readonly GUIContent _waitForFinishContent = new("Wait for finish");

        private ImmutableList<AA_ABase> _interactions;
        private GUIDropdown _interactableDropdown;

        public DS_InteractAABaseNodeEditor()
        {
            NodeSizeInternal = new Vector2(435, 100);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_InteractAABaseNode>();
            _interactions = Resources.FindObjectsOfTypeAll<AA_ABase>().Where(e => e.name.StartsWith("AA_Interact_NPC") && !e.name.EndsWith("(Clone)")).ToImmutableList();
            int selectionIndex = _interactions.FindIndex(AA_ABase => AA_ABase.name == _castedNode._interactPrefab?.name);
            if (selectionIndex == -1)
            {
                selectionIndex = 0;
            }
            _interactableDropdown = new GUIDropdown([.. _interactions.Select(e => e.name)], selectionIndex);
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = Color.green;

            int extraHeight = (!_castedNode._waitForFinish) ? 20 : 0;
            int rectHeight = 100 + extraHeight;

            Rect rect = new(position.x, position.y, 435, rectHeight);
            NodeSizeInternal = new Vector2(435, rectHeight);
            GUI.Box(rect, "DS_InteractAABaseNode");

            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 10, position.y + 20, 100, 20), "Interactable:");
            if (_interactableDropdown.Draw(new Rect(position.x + 155, position.y + 20, 275, 20)))
            {
                _castedNode._interactPrefab = _interactions[_interactableDropdown.SelectedIndex];
            }
            _castedNode._hideDialogueWindow = GUI.Toggle(new Rect(position.x + 10, position.y + 40, 200, 20), _castedNode._hideDialogueWindow, _hideDialogueWindowContent);
            _castedNode._waitForFinish = GUI.Toggle(new Rect(position.x + 10, position.y + 60, 200, 20), _castedNode._waitForFinish, _waitForFinishContent);
            if (!_castedNode._waitForFinish)
            {
                GUI.Label(new Rect(position.x + 10, position.y + 80, 100, 20), "Wait time");
                string value = GUI.TextField(new Rect(position.x + 110, position.y + 80, 200, 20), _castedNode._waitTime.ToString());
                if (float.TryParse(value, out float result))
                {
                    _castedNode._waitTime = result;
                }
            }
            GUI.color = previousColor;
        }
    }
}
