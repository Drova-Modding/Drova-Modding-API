using Il2CppDrova;
using Il2CppDrova.Alignment;

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
            if (actor == null || health == null) return;

            var alignmentModule = actor._alignmentModule;
            alignmentModule._defaultAlignment = _alignment;
            health._canDieInCombat = _alignment._isEnemyWithPlayer;

            if (alignmentModule._service != null)
            {
                var casted = _alignment.Cast<IAlignment>();
                alignmentModule._service._cachedAlignment = casted;
                alignmentModule._service._baseAlignment = casted;
            }
        }
    }
}
