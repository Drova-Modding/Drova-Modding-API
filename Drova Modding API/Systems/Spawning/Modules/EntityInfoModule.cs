using Il2CppDrova;

namespace Drova_Modding_API.Systems.Spawning.Modules
{
    /// <summary>
    /// A module that applies entity info to the Actor component.
    /// </summary>
    /// <param name="info">Entity info to apply</param>
    public class EntityInfoModule(EntityInfo info) : INpcModule
    {
        /// <inheritdoc />
        public void Apply(ModuleContext context)
        {
            Actor actor = context.GetComponent<Actor>();
            EntityGameHandler.TryGet(out EntityGameHandler? entityGameHandler);

            // The game looks an entity up under its entity info on destroy, so unregister before the info changes.
            if (entityGameHandler != null && actor._hasRegistered)
            {
                entityGameHandler.UnregisterEntity(actor);
            }

            actor._entityInfo = info;
            if (entityGameHandler != null)
            {
                entityGameHandler.RegisterEntity(actor);
                actor._hasRegistered = true;
            }
        }
    }
}
