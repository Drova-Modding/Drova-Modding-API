using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova.Items;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_UnEquipNode"/>
    /// </summary>
    internal class DS_UnEquipNodeEditor : DrawNodeEditor
    {
        private DS_UnEquipNode? _castedNode;
        private EquipmentSlotType[] equipmentSlotTypes;
        private readonly List<GUIDropdown> _equipmentSlotDropdowns = [];

        public DS_UnEquipNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 100);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_UnEquipNode>();
            equipmentSlotTypes = Resources.FindObjectsOfTypeAll<EquipmentSlotType>();

            for (int i = 0; i < _castedNode._slotTypes.Count; i++)
            {
                EquipmentSlotType slotType = _castedNode._slotTypes[i];
                _equipmentSlotDropdowns.Add(new GUIDropdown(equipmentSlotTypes.Select(e => e.name).ToArray(), Array.FindIndex(equipmentSlotTypes, (e) => e.GUID == slotType.GUID)));
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
            NodeSizeInternal = new Vector2(350, 40 + (60 * _castedNode._slotTypes.Count));
            GUI.Box(new Rect(position.x, position.y, 350, 40 + (60 * _castedNode._slotTypes.Count)), "DS_UnEquipNode");
            GUI.color = Color.white;
            for (int i = 0; i < _castedNode._slotTypes.Count; i++)
            {
                GUIDropdown equipmentSlotDropdown = _equipmentSlotDropdowns[i];
                if (GUI.Button(new Rect(position.x + 220, position.y + 40 + (20 * i), 20, 20), "X"))
                {
                    _castedNode._slotTypes.RemoveAt(i);
                    _equipmentSlotDropdowns.RemoveAt(i);
                    i--;
                    continue;
                }

                if (equipmentSlotDropdown.Draw(new Rect(position.x + 10, position.y + 20 + (20 * i), 200, 20)))
                {
                    _castedNode._slotTypes[i] = equipmentSlotTypes[equipmentSlotDropdown.SelectedIndex];
                }
            }

            if (GUI.Button(new Rect(position.x, position.y + 20 + (60 * _castedNode._slotTypes.Count), 120, 20), "Add Slot"))
            {
                _castedNode._slotTypes.Add(equipmentSlotTypes[0]);
                _equipmentSlotDropdowns.Add(new GUIDropdown(equipmentSlotTypes.Select(e => e.name).ToArray(), 0));
            }

            GUI.depth = previousDepth;
            GUI.color = previousColor;
        }
    }
}
