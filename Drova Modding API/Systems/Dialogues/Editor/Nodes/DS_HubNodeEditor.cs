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
        private DS_HubNode? _castedNode;

        private GUIDropdown? _hubRatingDropdown;

        // Outer height from the previous frame so the opaque wrapper can be drawn
        // BEFORE the inner content without occluding it.
        private float _cachedOuterHeight = 230f;

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

            Color previousColor = GUI.color;

            GUI.color = Color.white;
            GUI.Box(new Rect(position.x, position.y, NodeSizeInternal.x, _cachedOuterHeight), "DS_HubNode", EditorBoxStyles.HubNode);

            GUI.color = Color.white;
            base.DrawNode(new Vector2(position.x, position.y + 30));

            float innerHeight = NodeSizeInternal.y;
            float outerHeight = innerHeight + 80;
            _cachedOuterHeight = outerHeight;

            GUI.color = Color.white;
            Vector2 ratingPosition = new(position.x, position.y + innerHeight + 40);

            GUI.Label(new Rect(ratingPosition.x + 5, ratingPosition.y, 100, 20), "Rating:");

            if (_hubRatingDropdown.Draw(new Rect(ratingPosition.x + 110, ratingPosition.y, 220, 20)))
            {
                _castedNode.ratingCache = (DS_HubNode.HubRating)_hubRatingDropdown.SelectedIndex;
            }

            NodeSizeInternal = new Vector2(NodeSizeInternal.x, outerHeight);

            GUI.color = previousColor;
        }
    }
}
