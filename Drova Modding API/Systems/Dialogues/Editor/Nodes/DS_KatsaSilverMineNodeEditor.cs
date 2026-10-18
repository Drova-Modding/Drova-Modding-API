using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_KatsaSilverMine"/>
    /// </summary>
    internal class DS_KatsaSilverMineNodeEditor : DrawNodeEditor
    {
        private DS_KatsaSilverMine? _castedNode;
        private GUIGvarSelectionEditor _playerAmountEditor = null!;
        private GUIGvarSelectionEditor _playerMineSilverAmountEditor = null!;
        private GUIGvarSelectionEditor _friendlyAmountEditor = null!;
        private GUIGvarSelectionEditor _defaultAmountEditor = null!;
        private GUIGvarSelectionEditor _angryAmountEditor = null!;

        public DS_KatsaSilverMineNodeEditor()
        {
            NodeSizeInternal = new Vector2(500, 400);
        }

        public override void Init()
        {
            _castedNode = Node.TryCast<DS_KatsaSilverMine>();
            if (_castedNode == null) return;
            GlobalAssetsGameHandler? globalAssets = GlobalAssetsGameHandler.TryGet();
            if (globalAssets != null)
            {
                _castedNode.SilverOreItem = globalAssets.SilverOreItem;
            }
            _playerAmountEditor = new GUIGvarSelectionEditor(GvarType.INT, null, false, _castedNode.PlayerAmount);
            _playerMineSilverAmountEditor = new GUIGvarSelectionEditor(GvarType.INT, null, false, _castedNode.PlayerMineSilverAmount);
            _friendlyAmountEditor = new GUIGvarSelectionEditor(GvarType.INT, null, false, _castedNode.FriendlyAmount);
            _defaultAmountEditor = new GUIGvarSelectionEditor(GvarType.INT, null, false, _castedNode.DefaultAmount);
            _angryAmountEditor = new GUIGvarSelectionEditor(GvarType.INT, null, false, _castedNode.AngryAmount);
        }
        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;
            GUI.Box(new Rect(position.x, position.y, 500, 400), "DS_KatsaSilverMine");
            GUI.color = Color.white;

            GUI.Label(new Rect(position.x + 5, position.y + 270, 250, 20), "Friendly Percentage:");
            string tempFriendlyPercentage = GUI.TextField(new Rect(position.x + 255, position.y + 270, 200, 20), _castedNode.FriendlyPercentage.ToString());
            if (float.TryParse(tempFriendlyPercentage, out float friendlyPercentage))
            {
                _castedNode.FriendlyPercentage = friendlyPercentage;
            }

            GUI.Label(new Rect(position.x + 5, position.y + 300, 250, 20), "Angry Percentage:");
            string tempAngryPercentage = GUI.TextField(new Rect(position.x + 255, position.y + 300, 200, 20), _castedNode.HatePercentage.ToString());
            if (float.TryParse(tempAngryPercentage, out float angryPercentage))
            {
                _castedNode.HatePercentage = angryPercentage;
            }
            GUI.Label(new Rect(position.x + 5, position.y + 330, 250, 20), "Default Percentage:");
            string tempDefaultPercentage = GUI.TextField(new Rect(position.x + 255, position.y + 330, 200, 20), _castedNode.DefaultPercentage.ToString());
            if (float.TryParse(tempDefaultPercentage, out float defaultPercentage))
            {
                _castedNode.DefaultPercentage = defaultPercentage;
            }

            _angryAmountEditor.DrawGvarEditor(new Rect(position.x + 5, position.y + 220, 250, 25));
            _defaultAmountEditor.DrawGvarEditor(new Rect(position.x + 5, position.y + 170, 250, 25));
            _friendlyAmountEditor.DrawGvarEditor(new Rect(position.x + 5, position.y + 120, 250, 25));
            _playerMineSilverAmountEditor.DrawGvarEditor(new Rect(position.x + 5, position.y + 70, 250, 25));
            _playerAmountEditor.DrawGvarEditor(new Rect(position.x + 5, position.y + 20, 250, 25));

            GUI.depth = previousDepth;
            GUI.color = previousColor;

        }
    }
}
