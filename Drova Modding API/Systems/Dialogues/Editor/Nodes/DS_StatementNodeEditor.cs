using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;

using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_StatementNodeEditor : DrawNodeEditor
    {
        protected Vector2 nodeSize = new(200, 50);
        DS_StatementNode? CastedNode;
        GUIContent? _cachedNodeContent;
        string? _cachedLocaPath;
        string? _cachedLocaKey;

        public DS_StatementNodeEditor()
        {
            NodeSizeInternal = new Vector2(200, 125);
        }

        public override void Init()
        {
            CastedNode ??= Node.TryCast<DS_StatementNode>();
        }

        public override void DrawNode(Vector2 position)
        {
            if (CastedNode == null) return;

            Color previousColor = GUI.color;
            Color previousBackgroundColor = GUI.backgroundColor;

            string locaPath = CastedNode.statement.useGlobalLoca ? CastedNode.statement._globalPath : CastedNode.DLGTree.LocaPath;
            string locaKey = CastedNode.statement._locaKey;
            if (_cachedNodeContent == null
                || !string.Equals(_cachedLocaPath, locaPath, StringComparison.Ordinal)
                || !string.Equals(_cachedLocaKey, locaKey, StringComparison.Ordinal))
            {
                _cachedLocaPath = locaPath;
                _cachedLocaKey = locaKey;
                _cachedNodeContent = new GUIContent("DS_StatementNode", CastedNode.GetLocalizedString());
            }

            GUI.color = Color.white;
            GUI.Box(new(position.x, position.y, nodeSize.x, nodeSize.y + 75), _cachedNodeContent, EditorBoxStyles.StatementNode);

            CastedNode.statement.useGlobalLoca = GUI.Toggle(
                new Rect(position.x + 5, position.y + 25, nodeSize.x - 10, 20),
                CastedNode.statement.useGlobalLoca,
                "Use Global Loca");

            if (CastedNode.statement.useGlobalLoca)
            {
                CastedNode.statement._globalPath = GUI.TextField(new Rect(position.x + 5, position.y + 50, nodeSize.x - 10, 20), CastedNode.statement._globalPath);
            }
            else
            {
                GUI.Label(new Rect(position.x + 5, position.y + 50, nodeSize.x - 10, 20), "LocaPath: " + CastedNode.DLGTree.LocaPath);
            }

            CastedNode.statement._locaKey = GUI.TextField(new Rect(position.x + 5, position.y + 75, nodeSize.x - 10, 20), CastedNode.statement._locaKey);

            GUI.color = previousColor;
            GUI.backgroundColor = previousBackgroundColor;
        }
    }
}
