using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_HubNode"/>
    /// </summary>
    internal class DS_HubNodeEditor : DS_MultipleChoiceNodeEditor
    {
        private DS_HubNode _castedNode;

        private GUIDropdown _hubRatingDropdown;

        public override void Init()
        {
            base.Init();
            _castedNode ??= Node.TryCast<DS_HubNode>();
            _hubRatingDropdown ??= new GUIDropdown(Enum.GetNames<DS_HubNode.HubRating>(), (int)_castedNode.ratingCache);
            TransparentOuterBox = true;
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;

            base.DrawNode(new Vector2(position.x, position.y + 30));

            float innerHeight = NodeSizeInternal.y;
            float outerHeight = innerHeight + 80;

            GUI.depth = 20;
            GUI.color = Color.green;
            GUI.Box(new Rect(position.x, position.y, NodeSizeInternal.x, outerHeight), "DS_HubNode");

            GUI.depth = 10;
            GUI.color = Color.white;

            Vector2 ratingPosition = new(position.x, position.y + innerHeight + 40);

            GUI.Label(new Rect(ratingPosition.x + 5, ratingPosition.y, 100, 20), "Rating:");

            if (_hubRatingDropdown.Draw(new Rect(ratingPosition.x + 110, ratingPosition.y, 220, 20)))
            {
                _castedNode.ratingCache = (DS_HubNode.HubRating)_hubRatingDropdown.SelectedIndex;
            }

            NodeSizeInternal = new Vector2(NodeSizeInternal.x, outerHeight);

            GUI.depth = previousDepth;
            GUI.color = previousColor;
        }
    }
}
