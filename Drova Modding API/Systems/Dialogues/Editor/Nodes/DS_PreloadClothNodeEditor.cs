using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.Items;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{

    /// <summary>
    /// Node editor for <see cref="DS_PreloadCloth"/>
    /// </summary>
    internal class DS_PreloadClothNodeEditor : DrawNodeEditor
    {
        private DS_PreloadCloth _castedNode;
        private GUIDropdown _clothModeDropdown;
        private Item[] _clothItems;
        private string[] _clothItemNames;
        private readonly List<GUIDropdownWithFilter> _clothItemDropdowns = [];

        public DS_PreloadClothNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 180);
        }

        public override void Init()
        {
            _castedNode = Node.TryCast<DS_PreloadCloth>();
            _clothItems = Resources.FindObjectsOfTypeAll<Item>()
                                    .Where(i => i.TryGetItemBehaviour(out ItemBhvr_EquipEffect_Cloth _))
                                    .ToArray();
            _clothModeDropdown = new GUIDropdown(Enum.GetNames<DS_PreloadCloth.Mode>(), (int)_castedNode.PreloadMode);
            _clothItemNames = _clothItems.Select(e => e.name).ToArray();
            for (int i = 0; i < _castedNode.ClothItems.Count; i++)
            {
                Item clothItem = _castedNode.ClothItems[i];
                _clothItemDropdowns.Add(new GUIDropdownWithFilter(_clothItemNames, Array.FindIndex(_clothItems, (e) => e.Guid == clothItem.Guid), 20));
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
            GUI.depth = 10;
            GUI.color = Color.green;
            NodeSizeInternal = new Vector2(350, 80 + 100 * _castedNode.ClothItems.Count);
            GUI.Box(new Rect(position.x, position.y, 350, 80 + 100 * _castedNode.ClothItems.Count), "DS_PreloadCloth");
            GUI.color = Color.white;
            if (_clothModeDropdown.Draw(new Rect(position.x + 5, position.y + 20, 250, 25)))
            {
                _castedNode.PreloadMode = (DS_PreloadCloth.Mode)_clothModeDropdown.SelectedIndex;
            }
            for (int i = 0; i < _castedNode.ClothItems.Count; i++)
            {
                Item clothItem = _castedNode.ClothItems[i];
                GUIDropdownWithFilter clothItemDropdown = _clothItemDropdowns[i];
                if (GUI.Button(new Rect(position.x + 220, position.y + 80 + 20 * i, 20, 20), "X"))
                {
                    _castedNode.ClothItems.RemoveAt(i);
                    _clothItemDropdowns.RemoveAt(i);
                    i--;
                    continue;
                }

                if (clothItemDropdown.Draw(new Rect(position.x + 10, position.y + 40 + 20 * i, 200, 20)))
                {
                    _castedNode.ClothItems[i] = _clothItems[clothItemDropdown.SelectedIndex];
                }
            }
            if (GUI.Button(new Rect(position.x, position.y + 40 + 100 * _castedNode.ClothItems.Count, 120, 20), "Add Cloth"))
            {
                _castedNode.ClothItems.Add(_clothItems[0]);
                _clothItemDropdowns.Add(new GUIDropdownWithFilter(_clothItemNames, 0, 20));
            }
            GUI.depth = previousDepth;
            GUI.color = previousColor;
        }
    }
}
