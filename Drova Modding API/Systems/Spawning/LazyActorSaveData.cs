namespace Drova_Modding_API.Systems.Spawning
{

    /// <summary>
    /// Contains the data of a lazy actor that can be saved and restored
    /// </summary>
    public class LazyActorSaveData
    {
        /**
         * The name of the actor
         */
        public string ActorName { get; set; }
        /**
         * The reference string of the actor
         */
        public string ActorReferenceString { get; set; }
        /**
         * The reference string of the entity info of the actor
         */
        public string ActorEnitityInfoReferenceString { get; set; }
        /**
         * The guid of the actor
         */
        public string ActorGuid { get; set; }

        /// <summary>
        /// Empty constructor
        /// </summary>
        public LazyActorSaveData()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="actorName">The name of the actor</param>
        /// <param name="actorReferenceString">The reference string of the actor</param>
        /// <param name="actorEnitityInfoReferenceString">The reference string of the entity info of the actor</param>
        /// <param name="actorGuid">The guid of the actor</param>

        public LazyActorSaveData(string actorName, string actorReferenceString, string actorEnitityInfoReferenceString, string actorGuid)
        {
            ActorName = actorName;
            ActorReferenceString = actorReferenceString;
            ActorEnitityInfoReferenceString = actorEnitityInfoReferenceString;
            ActorGuid = actorGuid;
        }
    }
}