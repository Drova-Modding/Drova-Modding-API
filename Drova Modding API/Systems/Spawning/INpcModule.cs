namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Represents a module that can be applied to an NPC during creation.
    /// </summary>
    public interface INpcModule
    {
        /// <summary>
        /// Gets the module priority used when applying NPC modules.
        /// Lower values run first; modules with the same priority keep their insertion order.
        /// </summary>
        int Priority => 0;

        /// <summary>
        /// Applies the module's logic to the given NPC using the provided context.
        /// </summary>
        /// <param name="context">The module context for component resolution and caching</param>
        void Apply(ModuleContext context);

        /// <summary>
        /// Cleans up resources previously created by this module.
        /// Called when the owning lazy actor is destroyed.
        /// </summary>
        /// <param name="context">The module context for cleanup resolution and metadata</param>
        void Cleanup(ModuleContext context) { }
    }
}

