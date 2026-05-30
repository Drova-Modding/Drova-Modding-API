using MelonLoader;
namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Extensible module registry for external NPC definitions and the runtime NPC wizard.
    /// </summary>
    public static class ExternalNpcModuleRegistry
    {
        private static readonly List<IExternalNpcModule> RegisteredModules = [];

        static ExternalNpcModuleRegistry()
        {
            Register(new ExternalFriendlyModule());
            Register(new ExternalEquipmentModule());
            Register(new ExternalCosmeticsModule());
            Register(new ExternalEntityInfoModule());
            Register(new ExternalDialogueModule());
            Register(new ExternalHealthModule());
            Register(new ExternalTalentModule());
            Register(new ExternalRoutineModule());
        }

        /// <summary>
        /// All registered external NPC modules. The wizard renders these in order.
        /// </summary>
        public static IReadOnlyList<IExternalNpcModule> Modules => RegisteredModules;

        /// <summary>
        /// Registers a new external NPC module. Duplicate keys are ignored.
        /// </summary>
        public static void Register(IExternalNpcModule module)
        {
            if (RegisteredModules.Any(m => string.Equals(m.Key, module.Key, StringComparison.OrdinalIgnoreCase)))
                return;

            RegisteredModules.Add(module);
        }

        /// <summary>
        /// Applies all registered modules to an <see cref="NpcCreator"/> using payloads from a definition.
        /// </summary>
        public static void ApplyModules(NpcCreator creator, ExternalNpcPlacementSystem.ExternalNpcDefinition definition)
        {
            for (int i = 0; i < RegisteredModules.Count; i++)
            {
                IExternalNpcModule module = RegisteredModules[i];
                definition.ModuleConfig.TryGetValue(module.Key, out string? payload);
                try
                {
                    module.ApplyToCreator(creator, payload);
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Failed to apply module '{module.Key}' for NPC '{definition.Id}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Ensures a definition has default payloads for every registered module.
        /// </summary>
        public static void EnsureDefaults(ExternalNpcPlacementSystem.ExternalNpcDefinition definition)
        {
            for (int i = 0; i < RegisteredModules.Count; i++)
            {
                IExternalNpcModule module = RegisteredModules[i];
                if (!definition.ModuleConfig.ContainsKey(module.Key))
                    definition.ModuleConfig[module.Key] = module.CreateDefaultPayload();
            }
        }
    }
}
