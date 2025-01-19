using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies
{
    internal class ConnectActionStrategy : IActionStrategy
    {
        public void OnCancel(GraphEditorManager editorManager, DrawNodeEditor selection)
        {
        }

        public void OnEnd(GraphEditorManager editorManager, DrawNodeEditor selection, Vector2 clickPosition)
        {
            foreach (var editor in editorManager.DrawNodeEditors)
            {
                Rect nodeRect = new(editor.Value.Position, editor.Value.NodeSize);
                if (nodeRect.Contains(clickPosition))
                {
                    if (editor.Value != selection && selection.Node.CanConnectToTarget(editor.Value.Node))
                    {
                        editorManager.DialogueTree.ConnectNodes(selection.Node, editor.Value.Node);
                        selection.ConnectedWith.Add(editor.Value);
                    }
                    break;
                }
            }
        }

        public void OnGui(GraphEditorManager editorManager, DrawNodeEditor selection, Vector2 mousePosition)
        {
            var nodeCenter = selection.Position + selection.NodeSize / 2;
            var nodeEdge = GraphEditorManager.GetEdgePoint(nodeCenter, selection.NodeSize, mousePosition);
            editorManager.DrawLine(nodeEdge, mousePosition, Color.magenta, 0);
        }

        public void OnStart(GraphEditorManager editorManager, DrawNodeEditor selection, Vector2 clickPosition)
        {
        }
    }
}
