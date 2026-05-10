using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_HealActor"/>
    /// </summary>
    internal class DS_HealActorNodeEditor : DrawNodeEditor
    {
        private DS_HealActor _castedNode;
        private EntityInfo[] _entityInfos;
        private GUIDropdownWithFilter _entityInfoDropdown;

        public DS_HealActorNodeEditor()
        {
            NodeSizeInternal = new Vector2(400, 60);
        }

        public override void Init()
        {
            _castedNode = Node.TryCast<DS_HealActor>();
            _entityInfos = Resources.FindObjectsOfTypeAll<EntityInfo>()
                                    .GroupBy(e => e.name)
                                    .Select(g => g.First())
                                    .ToArray();
            EntityInfo entityInfo = _castedNode._entityInfo;
            _entityInfoDropdown = new GUIDropdownWithFilter(_entityInfos.Select(e => e.name).ToArray(), Array.FindIndex(_entityInfos, e => e.GUID == entityInfo.GUID), 20);
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
            GUI.Box(new Rect(position.x, position.y, 400, 60), "DS_HealActor");
            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 10, position.y + 20, 130, 20), "Entity to heal:");
            if (_entityInfoDropdown.Draw(new Rect(position.x + 145, position.y + 20, 250, 25)))
            {
                _castedNode._entityInfo = _entityInfos[_entityInfoDropdown.SelectedIndex];
            }
            GUI.depth = previousDepth;
            GUI.color = previousColor;

        }
    }
}
