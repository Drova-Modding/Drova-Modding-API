namespace Drova_Modding_API.Systems.Spawning;

/// <summary>
/// Central mutable state for the NPC wizard session.
/// </summary>
internal sealed class NpcWizardState
{
    internal NpcWizardState(
        ExternalNpcPlacementSystem.ExternalNpcDefinition definition,
        string status)
    {
        Definition = definition;
        Status = status;
    }

    internal ExternalNpcPlacementSystem.ExternalNpcDefinition Definition { get; set; }

    internal string Status { get; set; }

    internal string PositionXInput { get; set; } = string.Empty;

    internal string PositionYInput { get; set; } = string.Empty;

    internal string? LoadedDefinitionId { get; private set; }

    internal string? LoadedDialogueDefinitionId { get; private set; }

    internal string? LoadedDialogueId { get; private set; }

    internal void SyncPositionInputs()
    {
        PositionXInput = Definition.PositionX.ToString(System.Globalization.CultureInfo.InvariantCulture);
        PositionYInput = Definition.PositionY.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal void MarkCurrentDefinitionAsLoaded()
        => LoadedDefinitionId = Definition.Id;

    internal void MarkCurrentDefinitionAsUnloaded()
    {
        LoadedDefinitionId = null;
        ClearDialogueLoadedMarker();
    }

    internal void MarkCurrentDialogueAsLoaded(string dialogueId)
    {
        LoadedDialogueDefinitionId = Definition.Id;
        LoadedDialogueId = dialogueId;
    }

    internal void ClearDialogueLoadedMarker()
    {
        LoadedDialogueDefinitionId = null;
        LoadedDialogueId = null;
    }

    internal bool IsDefinitionLoadedFromCurrentSession()
        => string.Equals(LoadedDefinitionId, Definition.Id, StringComparison.OrdinalIgnoreCase);

    internal bool IsDialogueLoadedForCurrentDefinition(string dialogueId)
        => string.Equals(LoadedDialogueDefinitionId, Definition.Id, StringComparison.OrdinalIgnoreCase)
           && string.Equals(LoadedDialogueId, dialogueId, StringComparison.OrdinalIgnoreCase);
}

