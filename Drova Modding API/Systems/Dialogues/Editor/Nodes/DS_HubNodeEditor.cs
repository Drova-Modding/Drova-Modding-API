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
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }

            base.DrawNode(new Vector2(position.x, position.y + 30));

            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;

            GUI.depth = 10;
            GUI.color = Color.white;

            Vector2 nextPosition = new(position.x, position.y + NodeSizeInternal.y + 40);

            GUI.Label(new Rect(nextPosition.x + 5, nextPosition.y, 100, 20), "Rating:");

            if (_hubRatingDropdown.Draw(new Rect(nextPosition.x + 110, nextPosition.y, 220, 20)))
            {
                _castedNode.ratingCache = (DS_HubNode.HubRating)_hubRatingDropdown.SelectedIndex;            
            }
            
            GUI.color = Color.green;
            GUI.depth = 20;

            GUI.Box(new Rect(position.x, position.y, NodeSizeInternal.x, NodeSizeInternal.y + 50), "DS_HubNode");


            GUI.depth = previousDepth;
            GUI.color = previousColor;


        }
    }
}
