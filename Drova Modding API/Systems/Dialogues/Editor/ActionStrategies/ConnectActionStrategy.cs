using Il2CppNodeCanvas.Framework;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies
{
    internal class ConnectActionStrategy : IActionStrategy
    {
        private const string ConnectHint = "Click a target node to connect. Press Esc or right-click to cancel.";

        // Captured at OnStart so a selection change during the action does not move the source.
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
            if (selection == null || editorManager.Graph == null) return;

            foreach (KeyValuePair<string, DrawNodeEditor> editor in editorManager.DrawNodeEditors)
            {
                Rect nodeRect = new(editor.Value.Position, editor.Value.NodeSize);
                if (nodeRect.Contains(clickPosition))
                {
                    if (editor.Value == selection) break;
                    if (selection.Node.CanConnectToTarget(editor.Value.Node)
                        && editorManager.Graph.ConnectNodes(selection.Node, editor.Value.Node) != null)
                    {
                        selection.ConnectedWith.Add(editor.Value);
                    }
                    else
                    {
                        editorManager.ShowStatus(GetRefusalReason(selection.Node, editor.Value.Node), true);
                    }
                    break;
                }
            }

            _sourceNode = null;
            context.SetHint(null);
        }

        private static string GetRefusalReason(Node source, Node target)
        {
            string sourceName = source.GetIl2CppType().Name;
            string targetName = target.GetIl2CppType().Name;
            if (source.maxOutConnections == 0)
                return $"Not connected: {sourceName} cannot have nodes after it.";
            if (source.maxOutConnections > 0 && source.outConnections.Count >= source.maxOutConnections)
                return $"Not connected: {sourceName} already has its {source.maxOutConnections} allowed connection(s). Delete one first.";
            if (target.maxInConnections > 0 && target.inConnections.Count >= target.maxInConnections)
                return $"Not connected: {targetName} already has a node before it. Each node here can only have one.";
            return $"Not connected: {sourceName} cannot connect to {targetName}.";
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
