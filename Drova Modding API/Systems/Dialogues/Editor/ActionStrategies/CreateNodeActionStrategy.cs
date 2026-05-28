using Il2CppNodeCanvas.DialogueTrees;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies
{
    internal class CreateNodeActionStrategy(string nodeName) : IActionStrategy
    {

        public void OnStart(GraphEditorActionContext context)
        {
            GraphEditorManager editorManager = context.Manager;
            Vector2 clickPosition = context.PointerPosition;
            if (editorManager.DialogueTree == null) return;

            DTNode newNode = editorManager.DrawNodeEditorFactory.CreateNodeByName(editorManager.DialogueTree, nodeName);
            if (newNode == null)
            {
                MelonLogger.Warning("Unable to create dialogue node for {0}", nodeName);
                return;
            }

            editorManager.ApplyDefaultActorParameter(newNode);

            DrawNodeEditor nodeEditor = editorManager.DrawNodeEditorFactory.GetDrawNodeEditorFromType(newNode.GetIl2CppType());
            if (nodeEditor == null)
            {
                MelonLogger.Warning("Unable to create editor for node type {0}", newNode.GetIl2CppType().Name);
                return;
            }

            nodeEditor.Node = newNode;
            nodeEditor.GraphEditorManager = editorManager;
            nodeEditor.Position = clickPosition;
            editorManager.DrawNodeEditors[newNode.UID] = nodeEditor;
            nodeEditor.Init();
            context.SetSelection(nodeEditor);
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



