using Il2Cpp;
using Il2CppDrova;
using Il2CppDrova.Alignment;
using Il2CppDrova.DeathBehaviours;
using Il2CppDrova.HealthComponents;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// A module that sets the alignment of an NPC (friendly, hostile, neutral, etc.).
    /// </summary>
    public class AlignmentModule : INpcModule
    {
        private readonly AlignmentContainer _alignment;

        /// <summary>
        /// Creates a new AlignmentModule with the given alignment container.
        /// </summary>
        /// <param name="alignment">The alignment to apply to the NPC</param>
        public AlignmentModule(AlignmentContainer alignment)
        {
            _alignment = alignment;
        }

        /// <inheritdoc />
        public void Apply(ModuleContext context)
        {
            var actor = context.GetComponent<Actor>();
            var health = context.GetComponentInChildren<Health>();
            var deathBhvrSetActive = context.GetComponentInChildren<DeathBhvr_SetActive>();
            var healthBhvrReviveBhvr = context.GetComponentInChildren<HealthBhvr_ReviveBhvr>();
            if (actor == null || health == null) return;

            var alignmentModule = actor._alignmentModule;
            alignmentModule._defaultAlignment = _alignment;
            health.SetCanDie(_alignment._isEnemyWithPlayer);
            health.ChangedCanDieInCombatEvent.InvokeEvent(_alignment._isEnemyWithPlayer);
            if (healthBhvrReviveBhvr != null && deathBhvrSetActive != null)
            {
                deathBhvrSetActive._activation = _alignment._isEnemyWithPlayer ? EGameObjectActivation.Activate : EGameObjectActivation.Deactivate;
                healthBhvrReviveBhvr._healAfter = !_alignment._isEnemyWithPlayer;
                healthBhvrReviveBhvr._isActive = !_alignment._isEnemyWithPlayer;
                healthBhvrReviveBhvr.enabled = !_alignment._isEnemyWithPlayer;
            }
            if (alignmentModule._service != null)
            {
                var casted = _alignment.Cast<IAlignment>();
                alignmentModule._service._cachedAlignment = casted;
                alignmentModule._service._baseAlignment = casted;
            }
        }
    }
}