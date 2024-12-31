using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class MultipleConditionNodeEditor : DrawNodeEditor
    {
        private MultipleConditionNode _castedNode;
        private readonly List<DrawTaskEditor> _drawTaskEditors = [];

        public override void Init()
        {
            _castedNode ??= Node.TryCast<MultipleConditionNode>();
            for (int i = 0; i < _castedNode.Conditions.Count; i++)
            {
                var task = _castedNode.Conditions[i].TryCast<Il2CppNodeCanvas.Framework.Task>();
                _drawTaskEditors.Add(GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(task?.GetIl2CppType()));
            }
        }

        public override Rect DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return default;
            }
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = 10;
            GUI.color = Color.white;
            var rect = new Rect(position.x, position.y, 350, 70 + _drawTaskEditors.Sum(d => d.DrawTask(position).height));
           
            GUI.color = Color.green;
            GUI.Box(rect, "MultipleConditionNode");

            GUI.depth = previousDepth;
            GUI.color = previousColor;

            return rect;
        }
    }
}
