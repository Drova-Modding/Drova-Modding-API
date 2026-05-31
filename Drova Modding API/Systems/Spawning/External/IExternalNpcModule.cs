namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Extensibility contract for adding configurable external NPC modules.
    /// </summary>
    public interface IExternalNpcModule
    {
        /// <summary>
        /// Unique module key used in definition <c>moduleConfig</c>.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Human-friendly module name displayed in the wizard.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Creates a default serialized payload for this module.
        /// </summary>
        string CreateDefaultPayload();

        /// <summary>
        /// Draws module wizard controls and returns updated serialized payload.
        /// </summary>
        string DrawWizardUiAndSerialize(string payload);

        /// <summary>
        /// Applies this module configuration to an <see cref="NpcCreator"/>.
        /// </summary>
        void ApplyToCreator(NpcCreator creator, string? payload);

        /// <summary>
        /// Called every frame while the wizard is visible and this module is expanded.
        /// Override to handle runtime input or per-frame logic. No-op by default.
        /// </summary>
        void OnWizardUpdate() { }
    }
}

