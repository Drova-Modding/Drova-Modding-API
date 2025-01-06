using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova;
using Il2CppNodeCanvas.DialogueTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_SetAcquaintance"/>
    /// </summary>
    internal class DS_SetAcquaintanceNodeEditor : DrawNodeEditor
    {
        private DS_SetAcquaintance _castedNode;
        private EntityInfo[] _entityInfos;
        private GUIDropdownWithFilter _entityInfoDropdown;

        public override void Init()
        {
            _castedNode = Node.TryCast<DS_SetAcquaintance>();
            _entityInfos = Resources.FindObjectsOfTypeAll<EntityInfo>()
                                    .GroupBy(e => e.name)
                                    .Select(g => g.First())
                                    .ToArray();
            var entityInfo = _castedNode.entity;
            _entityInfoDropdown = new GUIDropdownWithFilter(_entityInfos.Select(e => e.name).ToArray(), Array.FindIndex(_entityInfos, e => e.GUID == entityInfo.GUID), 20);
        }

        public override Rect DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;
            var rect = new Rect(position.x, position.y, 350, 60);
            GUI.Box(rect, "DS_SetAcquaintance");
            GUI.color = Color.white;
            if (_entityInfoDropdown.Draw(new Rect(position.x + 5, position.y + 20, 250, 25)))
            {
                _castedNode.entity = _entityInfos[_entityInfoDropdown.SelectedIndex];
            }
            GUI.depth = previousDepth;
            GUI.color = previousColor;
            return rect;

        }
    }
}
