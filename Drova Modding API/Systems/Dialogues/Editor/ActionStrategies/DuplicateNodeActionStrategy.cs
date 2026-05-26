using Il2CppNodeCanvas.Framework;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies
{
    internal class DuplicateNodeActionStrategy : IActionStrategy
    {
        private static readonly Vector2 DuplicateOffset = new(40f, 40f);

        public void OnStart(GraphEditorActionContext context)
        {
            GraphEditorManager editorManager = context.Manager;
            DrawNodeEditor? selection = context.Selection;
            if (editorManager.DialogueTree == null || selection == null) return;

            Il2CppSystem.Collections.Generic.List<Node> toClone = new();
            toClone.Add(selection.Node.Cast<Node>());

            Il2CppSystem.Collections.Generic.List<Node> cloned = Graph.CloneNodes(toClone, editorManager.DialogueTree, selection.Position + DuplicateOffset);
            if (cloned == null || cloned.Count == 0) return;

            DTNode duplicatedNode = cloned[0].Cast<DTNode>();
            DrawNodeEditor duplicatedEditor = editorManager.DrawNodeEditorFactory.GetDrawNodeEditorFromType(duplicatedNode.GetIl2CppType());
            if (duplicatedEditor == null) return;

            duplicatedEditor.Node = duplicatedNode;
            duplicatedEditor.GraphEditorManager = editorManager;
            duplicatedEditor.Position = selection.Position + DuplicateOffset;
            editorManager.DrawNodeEditors[duplicatedNode.UID] = duplicatedEditor;
            duplicatedEditor.Init();
            context.SetSelection(duplicatedEditor);
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




