using Il2CppDrova;
using Il2CppDrova.InventorySystem;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// Applies MaxHealth to an NPC's Health component. Also sets MaxHealthInsane to 1.5x the set health for a more reasonable insane mode scaling.
    /// </summary>
    public class HealthPresetModule : INpcModule
    {
        /// <summary>
        /// Runs after the other NPC modules so health adjustments are applied last.
        /// </summary>
        public int Priority => 1000;

        private int _health = 100;

        /// <summary>
        /// Sets the health for the NPC.
        /// </summary>
        /// <param name="health">The health value to set</param>
        /// <returns>This preset for chaining</returns>
        public HealthPresetModule With(int health)
        {
            _health = health;
            return this;
        }

        /// <inheritdoc />
        public void Apply(ModuleContext context)
        {
            var health = context.GetComponentInChildren<Health>();
            if (health == null) return;

            health._maxHealth = _health;
            health._maxHealthInsane = (int)MathF.Round(_health * 1.5f);
            
            var inventory = context.GetComponentInChildren<Inventory_StartupEquipSettings>();
            if (inventory == null) return;

            var equipPreset = EquipPresetHelper.EnsureInitialized(inventory);
            equipPreset._health = _health;
        }
    }
}