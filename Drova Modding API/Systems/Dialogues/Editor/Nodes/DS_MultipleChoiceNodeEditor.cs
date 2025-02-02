using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_MultipleChoiceNodeEditor : DrawNodeEditor
    {
        private DS_MultipleChoiceNode _castedNode;
        private readonly Dictionary<int, DrawTaskEditor> _choices = [];

        public DS_MultipleChoiceNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 150);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_MultipleChoiceNode>();
            for (int i = 0; i < _castedNode.availableChoices.Count; i++)
            {
                DS_MultipleChoiceNode.Choice choice = _castedNode.availableChoices[i];
                if (choice.condition != null)
                {
                    DrawTaskEditor taskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(choice.condition.GetIl2CppType());
                    taskEditor.Task = choice.condition;
                    taskEditor.GraphEditorManager = GraphEditorManager;
                    taskEditor.Init();
                    _choices.TryAdd(i, taskEditor);
                }
            }
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }
            Color previousColor = GUI.color;
            _castedNode.availableChoices ??= new Il2CppSystem.Collections.Generic.List<DS_MultipleChoiceNode.Choice>();
            int additionalHeight = (_castedNode.availableChoices.Count * 75) + 65;
            Color previousBackgroundColor = GUI.backgroundColor;

            GUI.color = Color.green;
            Rect rect = new(position.x, position.y, 350, additionalHeight);
            NodeSizeInternal = new Vector2(350, additionalHeight);
            GUI.Box(rect, "DS_MultipleChoiceNode");

            GUI.backgroundColor = Color.black;
            GUI.color = Color.white;
            int step = 20;
            for (int i = 0; i < _castedNode.availableChoices.Count; i++)
            {
                DS_MultipleChoiceNode.Choice choice = _castedNode.availableChoices[i];
                GUI.Box(new Rect(position.x + 5, position.y + step, 340, 70), new GUIContent($"Choice {i}:", _castedNode.GetLocalizedString(choice.statement)));
                step += 5;
                if (choice.statement.useGlobalLoca)
                {
                    GUI.Label(new Rect(position.x + 5, position.y + step, 100, 20), "Globapath:");
                    choice.statement._globalPath = GUI.TextField(new Rect(position.x + 110, position.y + step, 200, 20), choice.statement._globalPath);
                }
                else
                {
                    GUI.Label(new Rect(position.x + 5, position.y + step, 300, 20), "LocaPath: " + _castedNode.DLGTree.LocaPath);
                }
                step += 35;
                GUI.Label(new Rect(position.x + 5, position.y + step, 100, 20), "Statement:");
                choice.statement._locaKey = GUI.TextField(new Rect(position.x + 110, position.y + step, 200, 20), choice.statement._locaKey);
                step += 35;
                if (_choices.TryGetValue(i, out DrawTaskEditor editor))
                {
                    Rect size = editor.DrawTask(new Vector2(position.x + 5, position.y + step));
                    step += (int)size.height + 35;
                }
            }
            // Add choice button if there are less than 8 choices
            if (_castedNode.availableChoices.Count < 8)
            {
                if (GUI.Button(new Rect(position.x, position.y + (_castedNode.availableChoices.Count * 70) + 20, 200, 20), "Add Choice"))
                {
                    _castedNode.availableChoices.Add(new DS_MultipleChoiceNode.Choice(new DS_Statement()));
                }
            }

            GUI.Label(new Rect(position.x + 5, position.y + step + 40, 50, 20), "Tag:");
            _castedNode.tag = GUI.TextField(new Rect(position.x + 110, position.y + step + 40, 200, 20), _castedNode.tag);
            GUI.backgroundColor = previousBackgroundColor;

            GUI.color = previousColor;
        }
    }
}
