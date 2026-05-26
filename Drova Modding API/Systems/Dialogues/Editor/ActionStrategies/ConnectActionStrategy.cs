using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies
{
    internal class ConnectActionStrategy : IActionStrategy
    {
        private const string ConnectHint = "Click a target node to connect. Press Esc or right-click to cancel.";

        // Source node captured at OnStart — independent of selection changes during the action.
        private DrawNodeEditor? _sourceNode;

        public void OnCancel(GraphEditorActionContext context)
        {
            _sourceNode = null;
            context.SetHint(null);
        }

        public void OnEnd(GraphEditorActionContext context)
        {
            GraphEditorManager editorManager = context.Manager;
            Vector2 clickPosition = context.PointerPosition;
            DrawNodeEditor? selection = _sourceNode ?? context.Selection;
            if (selection == null || editorManager.DialogueTree == null) return;

            foreach (KeyValuePair<string, DrawNodeEditor> editor in editorManager.DrawNodeEditors)
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

            _sourceNode = null;
            context.SetHint(null);
        }

        public void OnGui(GraphEditorActionContext context)
        {
            DrawNodeEditor? selection = _sourceNode ?? context.Selection;
            if (selection == null || selection.NodeSize.x <= 0) return;

            GraphEditorManager editorManager = context.Manager;
            // Read mouse in raw screen space to avoid GUI.matrix transformed coordinates.
            Vector2 mouseScreen = Input.mousePosition;
            mouseScreen.y = Screen.height - mouseScreen.y;
            Vector2 mousePosition = editorManager.ScreenToGraph(mouseScreen);
            Vector2 nodeCenter = selection.Position + (selection.NodeSize / 2);
            Vector2 nodeEdge = GraphEditorManager.GetEdgePoint(nodeCenter, selection.NodeSize, mousePosition);
            Vector2 nodeEdgeScreen = editorManager.GraphToScreen(nodeEdge);
            
            // Reset GUI.matrix to screen space before drawing the line
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            GraphEditorManager.DrawLineScreenSpace(nodeEdgeScreen, mouseScreen, Color.magenta);
            GUI.matrix = previousMatrix;
            
            context.SetHint(ConnectHint);
        }

        public void OnStart(GraphEditorActionContext context)
        {
            _sourceNode = context.Selection;
            context.SetHint(ConnectHint);
        }
    }
}
