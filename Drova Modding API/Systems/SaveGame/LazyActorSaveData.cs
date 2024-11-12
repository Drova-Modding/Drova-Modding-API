using Il2CppInterop.Runtime.Injection;
using MelonLoader;
namespace Drova_Modding_API.Systems.SaveGame
{
    /**
    * The data for a lazy actor
    */
    [System.Serializable]
    public class LazyActorSaveData : Il2CppSystem.Object
    {

        /**
        * Constructor from il2cpp side
        */
        public LazyActorSaveData(IntPtr ptr) : base(ptr) {
            MelonLogger.Msg("Created LazyActorSaveData Il2cpp");
        }

        /**
         * Constructor from managed side
         */
        public LazyActorSaveData() : base(ClassInjector.DerivedConstructorPointer<LazyActorSaveData>())
        {
            ClassInjector.DerivedConstructorBody(this);
            MelonLogger.Msg("Created LazyActorSaveData Managed");
        }

        public LazyActorSaveData(string actorName, string actorReferenceString, string actorEnitityInfoReferenceString, string actorGuid) : this()
        {
            MelonLogger.Msg("Test");
            ActorEnitityInfoReferenceString = actorEnitityInfoReferenceString;
            ActorGuid = actorGuid;
            ActorReferenceString = actorReferenceString;
            ActorName = actorName;

        }

        /**
         * Deep copy the object
         */
        public Il2CppSystem.Object DeepCopy()
        {
            MelonLogger.Msg("Deep Copy");
            return new LazyActorSaveData
            {
                ActorName = ActorName,
                ActorReferenceString = ActorReferenceString,
                ActorEnitityInfoReferenceString = ActorEnitityInfoReferenceString,
                ActorGuid = ActorGuid
            }; ;
        }
        /**
         * The name of the actor
         */
        public string ActorName = "";
        /**
         * The reference string for the actor
         */
        public string ActorReferenceString = "";
        /**
         * The reference string for the entity info of the actor
         */
        public string ActorEnitityInfoReferenceString = "";
        /**
         * The guid of the actor
         */
        public string ActorGuid = "";
    }
}
