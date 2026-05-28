namespace Drova_Modding_API.Systems.Spawning;

/// <summary>
/// Tracks one pending overwrite confirmation action and its prompt text.
/// The UI can queue, cancel, or consume that action without duplicating state handling.
/// </summary>
internal sealed class NpcWizardOverwriteState<TAction> where TAction : struct, Enum
{
    internal bool HasPending { get; private set; }

    internal string Message { get; private set; } = string.Empty;

    internal TAction PendingAction { get; private set; }

    internal void Queue(TAction action, string message)
    {
        PendingAction = action;
        Message = message;
        HasPending = true;
    }

    internal void Clear()
    {
        HasPending = false;
        Message = string.Empty;
        PendingAction = default;
    }

    internal bool TryConsume(out TAction action)
    {
        if (!HasPending)
        {
            action = default;
            return false;
        }

        action = PendingAction;
        Clear();
        return true;
    }
}

