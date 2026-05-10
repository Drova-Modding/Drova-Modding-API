using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_DefineActiveActorsNodeEditor : DrawNodeEditor
    {

        private DS_DefineActiveActors _castedNode;
        private EntityInfo[] _entityInfos;
        private List<GUIDropdownWithFilter> _entityInfoDropdowns;
        private const int _maxEntityInfos = 20;

        public DS_DefineActiveActorsNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 180);
        }

        public override void Init()
        {
            _castedNode = Node.TryCast<DS_DefineActiveActors>();
            _entityInfos = Resources.FindObjectsOfTypeAll<EntityInfo>()
                                    .GroupBy(e => e.name)
                                    .Select(g => g.First())
                                    .ToArray();
            _entityInfoDropdowns = [];
            for (int i = 0; i < _castedNode.entityInfos.Count; i++)
            {
                EntityInfo entityInfo = _castedNode.entityInfos[i];
                _entityInfoDropdowns.Add(new GUIDropdownWithFilter(_entityInfos.Select(e => e.name).ToArray(), Array.FindIndex(_entityInfos, (e) => e.GUID == entityInfo.GUID), _maxEntityInfos));
            }
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }
            Rect rect = new(position.x, position.y, 350, 70 + _entityInfoDropdowns.Sum(d => 20 + 30));
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.green;
            GUI.Box(rect, "DS_DefineActiveActors");
            GUI.color = Color.white;

            GUI.Label(new Rect(position.x + 10, position.y + 30, 130, 20), "Active Actors:");
            for (int i = 0; i < _castedNode.entityInfos.Count; i++)
            {
                if (_entityInfoDropdowns[i].Draw(new Rect(position.x + 120, position.y + 30 + (20 * i), 200, 20)))
                {
                    _castedNode.entityInfos[i] = _entityInfos[_entityInfoDropdowns[i].SelectedIndex];
                }

                if (GUI.Button(new Rect(position.x + 330, position.y + 30 + (20 * i), 20, 20), "X"))
                {
                    _castedNode.entityInfos.RemoveAt(i);
                    _entityInfoDropdowns.RemoveAt(i);
                    i--;
                }
            }

            if (GUI.Button(new Rect(position.x + 10, position.y + 30 + (20 * (_castedNode.entityInfos.Count + 1)), 130, 20), "Add Actor"))
            {
                _castedNode.entityInfos.Add(EntityGameHandler.TryGet()._undefinedEntityInfo);
                _entityInfoDropdowns.Add(new GUIDropdownWithFilter(_entityInfos.Select(e => e.name).ToArray(), -1, 20));
            }

            GUI.depth = previousDepth;
            GUI.color = previousColor;

            NodeSizeInternal = new Vector2(rect.width, rect.height);
        }
    }
}
