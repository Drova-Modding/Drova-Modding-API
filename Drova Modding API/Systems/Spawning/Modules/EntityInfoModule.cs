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
            var actor = context.GetComponent<Actor>();
            actor._entityInfo = info;
            if(EntityGameHandler.TryGet(out var entityGameHandler))
            {
                entityGameHandler.RegisterEntity(actor);
            }
        }
    }
}