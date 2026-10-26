using Drova_Modding_API.Systems.Dialogues.Editor.Factories;
using Il2CppNodeCanvas.BehaviourTrees;
using Il2CppNodeCanvas.Framework;
using Timeout = Il2CppNodeCanvas.BehaviourTrees.Timeout;

namespace Drova_Modding_API.Systems.Routines.Editor
{
    /// <summary>
    /// Node editors and the create menu for behaviour trees. Nodes without a dedicated editor get a
    /// <see cref="GenericNodeEditor"/>. Sub trees are not offered because their nested graph cannot be assigned here.
    /// </summary>
    public class BehaviourTreeNodeEditorFactory() : DrawNodeEditorFactory(
        new Dictionary<string, Type>
        {
            { nameof(ConditionNode), typeof(BTConditionNodeEditor) },
            { nameof(Interruptor), typeof(BTConditionNodeEditor) },
            { nameof(ConditionalEvaluator), typeof(BTConditionNodeEditor) },
            { nameof(WaitUntil), typeof(BTConditionNodeEditor) },
            { nameof(ActionNode), typeof(BTActionNodeEditor) }
        },
        new Dictionary<string, Func<Graph, Node>>
        {
            { nameof(Sequencer), graph => CreateNode<Sequencer>(graph) },
            { nameof(Selector), graph => CreateNode<Selector>(graph) },
            { nameof(PrioritySelector), graph => CreateNode<PrioritySelector>(graph) },
            { nameof(ProbabilitySelector), graph => CreateNode<ProbabilitySelector>(graph) },
            { nameof(StepIterator), graph => CreateNode<StepIterator>(graph) },
            { nameof(ActionNode), graph => CreateNode<ActionNode>(graph) },
            { nameof(ConditionNode), graph => CreateNode<ConditionNode>(graph) },
            { nameof(Inverter), graph => CreateNode<Inverter>(graph) },
            { nameof(Repeater), graph => CreateNode<Repeater>(graph) },
            { nameof(Interruptor), graph => CreateNode<Interruptor>(graph) },
            { nameof(ConditionalEvaluator), graph => CreateNode<ConditionalEvaluator>(graph) },
            { nameof(Optional), graph => CreateNode<Optional>(graph) },
            { nameof(Timeout), graph => CreateNode<Timeout>(graph) },
            { nameof(WaitUntil), graph => CreateNode<WaitUntil>(graph) }
        })
    {
    }
}
