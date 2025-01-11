using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppDrova;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="DS_ReleaseActiveActors"/>
    /// </summary>
    internal class DS_ReleaseActiveActorsNodeEditor : DrawNodeEditor
    {
        private DS_ReleaseActiveActors _castedNode;
        private EntityInfo[] _entityInfos;
        private string[] _entityInfoNames;
        private readonly List<GUIDropdownWithFilter> _entityInfoDropdowns = [];

        public DS_ReleaseActiveActorsNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 180);
        }

        public override void Init()
        {
            _castedNode = Node.TryCast<DS_ReleaseActiveActors>();
            _entityInfos = Resources.FindObjectsOfTypeAll<EntityInfo>();
            _entityInfoNames = _entityInfos.Select(e => e.name).ToArray();
            for (int i = 0; i < _castedNode._entityInfos.Count; i++)
            {
                var entityInfo = _castedNode._entityInfos[i];
                _entityInfoDropdowns.Add(new GUIDropdownWithFilter(_entityInfoNames, Array.FindIndex(_entityInfos, (e) => e.GUID == entityInfo.GUID), 20));
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
            NodeSizeInternal = new Vector2(350, 80 + 100 * _castedNode._entityInfos.Count);
            GUI.Box(new Rect(position.x, position.y, 350, 80 + 100 * _castedNode._entityInfos.Count), "DS_ReleaseActiveActors");
            GUI.color = Color.white;
            for (int i = 0; i < _castedNode._entityInfos.Count; i++)
            {
                var entityInfoDropdown = _entityInfoDropdowns[i];
                if (GUI.Button(new Rect(position.x + 220, position.y + 50 + 20 * i, 20, 20), "X"))
                {
                    _castedNode._entityInfos.RemoveAt(i);
                    _entityInfoDropdowns.RemoveAt(i);
                }
                if (entityInfoDropdown.Draw(new Rect(position.x + 5, position.y + 20 + 20 * i, 200, 20)))
                {
                    _castedNode._entityInfos[i] = _entityInfos[entityInfoDropdown.SelectedIndex];
                }
            }

            _castedNode._canRemoveOwner = GUI.Toggle(new Rect(position.x + 5, position.y + 60 + 100 * _castedNode._entityInfos.Count, 250, 25), _castedNode._canRemoveOwner, "Can remove owner");
            _castedNode._removeOwner = GUI.Toggle(new Rect(position.x + 5, position.y + 90 + 100 * _castedNode._entityInfos.Count, 250, 25), _castedNode._removeOwner, "Remove owner");

            if (GUI.Button(new Rect(position.x + 5, position.y + 30 + 100 * _castedNode._entityInfos.Count, 200, 20), "Add EntityInfo"))
            {
                _castedNode._entityInfos.Add(_entityInfos[0]);
                _entityInfoDropdowns.Add(new GUIDropdownWithFilter(_entityInfoNames, 0, 20));
            }

            GUI.depth = previousDepth;
            GUI.color = previousColor;
        }
    }
}
