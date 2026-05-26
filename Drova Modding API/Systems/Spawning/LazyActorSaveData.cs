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
         * The reference string of the entity info of the actor. Null or empty if using custom EntityInfo.
         */
        public string ActorEnitityInfoReferenceString { get; set; }
        /**
         * The guid of the actor
         */
        public string ActorGuid { get; set; }

        // ── Custom EntityInfo Support ──────────────────────────────────────
        /**
         * Whether this lazy actor uses custom EntityInfo instead of an addressable reference
         */
        public bool HasCustomEntityInfo { get; set; }

        /**
         * Whether this lazy actor is Npc
         */
        public bool IsNpc { get; set; }

        /**
         * The GUID of the custom EntityInfo (this is crucial for identification)
         */
        public string CustomEntityInfoGuid { get; set; }

        /**
         * Optional identifier when this lazy actor was created from an external NPC definition file.
         */
        public string? ExternalDefinitionId { get; set; }

        /// <summary>
        /// Empty constructor
        /// </summary>
        public LazyActorSaveData()
        {
        }

        /// <summary>
        /// Constructor for addressable-based EntityInfo 
        /// </summary>
        /// <param name="actorName">The name of the actor</param>
        /// <param name="actorReferenceString">The reference string of the actor</param>
        /// <param name="actorEnitityInfoReferenceString">The reference string of the entity info of the actor</param>
        /// <param name="actorGuid">The guid of the actor</param>
        /// <param name="isNpc">If the actor is a Npc</param>
        public LazyActorSaveData(string actorName, string actorReferenceString, string actorEnitityInfoReferenceString, string actorGuid, bool isNpc = false)
        {
            ActorName = actorName;
            ActorReferenceString = actorReferenceString;
            ActorEnitityInfoReferenceString = actorEnitityInfoReferenceString;
            ActorGuid = actorGuid;
            HasCustomEntityInfo = false;
            IsNpc = isNpc;
        }

        /// <summary>
        /// Constructor for custom EntityInfo
        /// </summary>
        /// <param name="actorName">The name of the actor</param>
        /// <param name="actorReferenceString">The reference string of the actor</param>
        /// <param name="actorGuid">The guid of the actor</param>
        /// <param name="entityInfoGuid">The GUID of the EntityInfo</param>
        /// <param name="isNpc">If the actor is a Npc</param>
        public static LazyActorSaveData FromCustom(
            string actorName,
            string actorReferenceString,
            string actorGuid,
            string entityInfoGuid,
            bool isNpc = false)
        {
            var lazy = new LazyActorSaveData();
            lazy.ActorName = actorName;
            lazy.ActorReferenceString = actorReferenceString;
            lazy.ActorGuid = actorGuid;
            lazy.HasCustomEntityInfo = true;
            lazy.CustomEntityInfoGuid = entityInfoGuid;
            lazy.IsNpc = isNpc;
            return lazy;
        }
    }
}