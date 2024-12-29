using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.ActorActions;
using Il2CppNodeCanvas.DialogueTrees;
using System.Collections.Immutable;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_InteractAABaseNodeEditor : DrawNodeEditor
    {
        DS_InteractAABaseNode CastedNode;

        readonly GUIContent HideDialogueWindowContent = new("Hide Dialogue Window");
        readonly GUIContent WaitForFinishContent = new("Wait for finish");

        ImmutableList<AA_ABase> interactions;
        GUIDropdown InteractableDropdown;

        public override void Init()
        {
            CastedNode ??= Node.TryCast<DS_InteractAABaseNode>();
            interactions = Resources.FindObjectsOfTypeAll<AA_ABase>().Where(e => e.name.StartsWith("AA_Interact_NPC") && !e.name.EndsWith("(Clone)")).ToImmutableList();
            var selectionIndex = interactions.FindIndex(AA_ABase => AA_ABase.name == CastedNode._interactPrefab.name);
            if(selectionIndex == -1)
            {
                selectionIndex = 0;
            }
            InteractableDropdown = new GUIDropdown(interactions.Select(e => e.name).ToArray(), selectionIndex);
        }

        public override Rect DrawNode(Vector2 position)
        {
            if (CastedNode == null)
            {
                return default;
            }

            Color previousColor = GUI.color;
            GUI.color = Color.green;

            var extraHeight = (!CastedNode._waitForFinish) ? 20 : 0;
            var dropdownHeight = InteractableDropdown.IsDropdownShown ? 20 * InteractableDropdown.OptionsCount : 0;
            var rectHeight = 100 + dropdownHeight + extraHeight;

            var rect = new Rect(position.x, position.y, 435, rectHeight);
            GUI.Box(rect, "DS_InteractAABaseNode");

            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 10, position.y + 20, 100, 20), "Interactable:");
            if (InteractableDropdown.Draw(new Rect(position.x + 155, position.y + 20, 275, 20)))
            {
                CastedNode._interactPrefab = interactions[InteractableDropdown.SelectedIndex];
            }
            CastedNode._hideDialogueWindow = GUI.Toggle(new Rect(position.x + 10, position.y + 40, 200, 20), CastedNode._hideDialogueWindow, HideDialogueWindowContent);
            CastedNode._waitForFinish = GUI.Toggle(new Rect(position.x + 10, position.y + 60, 200, 20), CastedNode._waitForFinish, WaitForFinishContent);
            if (!CastedNode._waitForFinish)
            {
                GUI.Label(new Rect(position.x + 10, position.y + 80, 100, 20), "Wait time");
                var value = GUI.TextField(new Rect(position.x + 110, position.y + 80, 200, 20), CastedNode._waitTime.ToString());
                if (float.TryParse(value, out float result))
                {
                    CastedNode._waitTime = result;
                }
            }
            GUI.color = previousColor;
            return rect;
        }
    }
}
