using Drova_Modding_API.Systems.Dialogues.Editor.Utils;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    internal class DS_MultipleChoiceNodeEditor : DrawNodeEditor
    {
        private DS_MultipleChoiceNode? _castedNode;
        private readonly Dictionary<int, DrawTaskEditor> _choices = [];
        private readonly Dictionary<int, float> _taskHeights = [];
        private readonly GUICreateConditionTask _createConditionTask = new();
        private int _choiceIndexRequestingCondition = -1;

        protected bool TransparentOuterBox { get; set; }

        public DS_MultipleChoiceNodeEditor()
        {
            NodeSizeInternal = new Vector2(350, 150);
        }

        public override void Init()
        {
            _castedNode ??= Node.TryCast<DS_MultipleChoiceNode>();
            if (_castedNode.availableChoices != null)
            {
                for (int i = 0; i < _castedNode.availableChoices.Count; i++)
                {
                    DS_MultipleChoiceNode.Choice choice = _castedNode.availableChoices[i];
                    if (choice.condition != null)
                    {
                        DrawTaskEditor taskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(choice.condition.GetIl2CppType());
                        taskEditor!.Task = choice.condition;
                        taskEditor.GraphEditorManager = GraphEditorManager;
                        taskEditor.Init();
                        _choices.TryAdd(i, taskEditor);
                    }
                }
            }
            else
            {
                _castedNode.availableChoices = new Il2CppSystem.Collections.Generic.List<DS_MultipleChoiceNode.Choice>();
            }
        }

        public override void DrawNode(Vector2 position)
        {
            if (_castedNode == null)
            {
                return;
            }
            Color previousColor = GUI.color;

            // Pre-calculate total height using cached DrawTask heights from the previous frame
            // Per choice: 20 (header) + 35 (UID) + 25 (loca choice) + 35 (loca) + 35 (statement) + 25 (end node) + 25 (condition buttons) + 10 (gap) = 210 base
            int additionalHeight = 20; // top padding
            for (int i = 0; i < _castedNode.availableChoices.Count; i++)
            {
                additionalHeight += 210;
                if (_taskHeights.TryGetValue(i, out float cachedHeight))
                    additionalHeight += (int)cachedHeight + 10;
            }
            additionalHeight += 25; // Add Choice button
            additionalHeight += 30; // Tag row + bottom padding

            GUI.color = Color.white;
            NodeSizeInternal = new Vector2(350, additionalHeight);
            if (!TransparentOuterBox)
            {
                GUI.Box(new Rect(position.x, position.y, 350, additionalHeight), "DS_MultipleChoiceNode", EditorBoxStyles.MultipleChoice);
            }
            int step = 20;
            for (int i = 0; i < _castedNode.availableChoices.Count; i++)
            {
                DS_MultipleChoiceNode.Choice choice = _castedNode.availableChoices[i];

                // Calculate per-choice box height (base + optional task height from cache)
                int choiceBoxHeight = 210;
                if (_taskHeights.TryGetValue(i, out float prevTaskHeight))
                    choiceBoxHeight += (int)prevTaskHeight + 10;


                GUI.Box(new Rect(position.x + 5, position.y + step, 340, choiceBoxHeight),
                    new GUIContent($"Choice {i}:", _castedNode.GetLocalizedString(choice.statement)),
                    EditorBoxStyles.Choice);
                step += 20; // space for "Choice i:" header label

                GUI.Label(new Rect(position.x + 5, position.y + step, 100, 20), "UID:");
                choice.UID = GUI.TextField(new Rect(position.x + 110, position.y + step, 200, 20), choice.UID);
                step += 35;

                GUI.Label(new Rect(position.x + 5, position.y + step, 150, 20), "Use Global Loca:");
                choice.statement._useGlobalLoca = GUI.Toggle(new Rect(position.x + 160, position.y + step, 100, 20), choice.statement._useGlobalLoca, "");
                step += 25;

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
                GUI.Label(new Rect(position.x + 5, position.y + step, 100, 20), "End Node:");
                choice.isEndNode = GUI.Toggle(new Rect(position.x + 110, position.y + step, 100, 20), choice.isEndNode, "");
                step += 25;

                if (choice.condition == null)
                {
                    if (GUI.Button(new Rect(position.x + 5, position.y + step, 150, 20), "Add Condition"))
                    {
                        // Cancel if already requesting
                        _choiceIndexRequestingCondition = _choiceIndexRequestingCondition == i
                            ? -1
                            : 
                            i;
                    }

                    if (_choiceIndexRequestingCondition == i)
                    {
                        ConditionTask? newTask = _createConditionTask.Draw(new Vector2(position.x + 160, position.y + step));
                        if (newTask != null)
                        {
                            GraphEditorManager.DialogueTree!.allTasks.Add(newTask);
                            choice.condition = newTask;
                            DrawTaskEditor taskEditor = GraphEditorManager.DrawTaskEditorFactory.GetDrawTaskEditorFromType(newTask.GetIl2CppType());
                            taskEditor!.Task = newTask;
                            taskEditor.GraphEditorManager = GraphEditorManager;
                            taskEditor.Init();
                            _choices[i] = taskEditor;
                            _choiceIndexRequestingCondition = -1;
                        }
                    }
                }
                else
                {
                    if (GUI.Button(new Rect(position.x + 5, position.y + step, 150, 20), "Remove Condition"))
                    {
                        GraphEditorManager.DialogueTree!.allTasks.Remove(choice.condition);
                        choice.condition = null;
                        _choices.Remove(i);
                        _taskHeights.Remove(i);
                    }
                }
                step += 25;

                if (_choices.TryGetValue(i, out DrawTaskEditor editor))
                {
                    Rect size = editor.DrawTask(new Vector2(position.x + 5, position.y + step));
                    _taskHeights[i] = size.height; // cache for next frame's height calculation
                    step += (int)size.height + 10;
                }

                // Ensure value-type choices persist modified primitive fields like isEndNode.
                _castedNode.availableChoices[i] = choice;
                step += 10; // gap between choices
            }

            // Add choice button if there are less than 8 choices
            if (_castedNode.availableChoices.Count < 8)
            {
                if (GUI.Button(new Rect(position.x, position.y + step, 200, 20), "Add Choice"))
                {
                    DS_Statement statement = new()
                    {
                        _useGlobalLoca = true
                    };
                    _castedNode.availableChoices.Add(new DS_MultipleChoiceNode.Choice(statement));
                }
                step += 25;
            }

            GUI.Label(new Rect(position.x + 5, position.y + step, 50, 20), "Tag:");
            _castedNode.tag = GUI.TextField(new Rect(position.x + 110, position.y + step, 200, 20), _castedNode.tag);

            GUI.color = previousColor;
        }
    }
}