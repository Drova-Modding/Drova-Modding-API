
public class LazyActorSaveDataManaged
{
    public string ActorName = "";
    public string ActorReferenceString = "";
    public string ActorEnitityInfoReferenceString = "";
    public string ActorGuid = "";

    public LazyActorSaveDataManaged()
    {
    }

    public LazyActorSaveDataManaged(string actorName, string actorReferenceString, string actorEnitityInfoReferenceString, string actorGuid)
    {
        ActorName = actorName;
        ActorReferenceString = actorReferenceString;
        ActorEnitityInfoReferenceString = actorEnitityInfoReferenceString;
        ActorGuid = actorGuid;
    }
}
