using Drova_Modding_API.Systems.Dialogues.Editor.Nodes;
using Il2CppNodeCanvas.Framework;

namespace Drova_Modding_API.Systems.Routines.Editor
{
    /// <summary>
    /// The dialogue condition node editor, pointed at behaviour tree nodes that hold one condition: the
    /// <see cref="Il2CppNodeCanvas.BehaviourTrees.ConditionNode"/> and the condition decorators (Interruptor,
    /// ConditionalEvaluator, WaitUntil).
    /// </summary>
    internal class BTConditionNodeEditor : ConditionNodeEditor
    {
        private ITaskAssignable? _assignable;
        private string? _title;

        protected override bool IsSupported => (_assignable ??= Node.TryCast<ITaskAssignable>()) != null;

        protected override string Title => _title ??= Node.GetIl2CppType().Name;

        protected override ConditionTask? GetCondition()
        {
            return IsSupported ? _assignable!.task?.TryCast<ConditionTask>() : null;
        }

        protected override void SetCondition(ConditionTask condition)
        {
            if (IsSupported) _assignable!.task = condition;
        }
    }
}
