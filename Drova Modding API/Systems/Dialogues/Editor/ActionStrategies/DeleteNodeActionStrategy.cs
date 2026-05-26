using Il2CppNodeCanvas.Framework;

namespace Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies
{
    internal class DeleteNodeActionStrategy : IActionStrategy
    {
        public void OnStart(GraphEditorActionContext context)
        {
            GraphEditorManager editorManager = context.Manager;
            DrawNodeEditor? selection = context.Selection;
            if (editorManager.DialogueTree == null || selection == null) return;

            editorManager.DialogueTree.RemoveNode(selection.Node.Cast<Node>());
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



