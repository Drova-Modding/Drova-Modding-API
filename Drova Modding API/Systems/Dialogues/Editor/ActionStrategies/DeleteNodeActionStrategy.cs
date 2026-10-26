using Il2CppNodeCanvas.Framework;

namespace Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies
{
    internal class DeleteNodeActionStrategy : IActionStrategy
    {
        public void OnStart(GraphEditorActionContext context)
        {
            GraphEditorManager editorManager = context.Manager;
            DrawNodeEditor? selection = context.Selection;
            if (editorManager.Graph == null || selection == null) return;

            bool wasStartNode = editorManager.Graph.primeNode == selection.Node;
            editorManager.Graph.RemoveNode(selection.Node);
            if (wasStartNode)
            {
                editorManager.ShowStatus("You deleted the start node. Pick a new one with 'MainNode' before saving.", true);
            }
            editorManager.DrawNodeEditors.Remove(selection.Node.UID);

            foreach (DrawNodeEditor editor in editorManager.DrawNodeEditors.Values)
            {
                editor.ConnectedWith.RemoveAll(x => x == null || x == selection || x.Node == selection.Node);
            }

            context.SetSelection(null);
            context.SetHint(null);
        }

        public void OnEnd(GraphEditorActionContext context)
        {
        }

        public void OnCancel(GraphEditorActionContext context)
        {
        }

        public void OnGui(GraphEditorActionContext context)
        {
        }
    }
}



