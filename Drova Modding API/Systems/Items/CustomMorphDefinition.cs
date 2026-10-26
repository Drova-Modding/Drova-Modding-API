namespace Drova_Modding_API.Systems.Items
{
    /// <summary>
    /// Turns a spell into a morph: casting it turns the player into a creature. The item's base must be a vanilla spell
    /// (<c>flow_*</c>) that spawns an ability, whose cast animation and costs it keeps. See <see cref="Morphing.MorphManager"/>.
    /// </summary>
    public sealed class CustomMorphDefinition
    {
        /// <summary>
        /// Addressable guid of the creature prefab, as listed in <see cref="Access.AddressableAccess.Creatures"/>
        /// (Ripper: <c>8074729ab27467d42bbb9c783c2f9758</c>).
        /// </summary>
        public string Creature { get; set; } = string.Empty;

        /// <summary>
        /// Seconds the morph lasts. 0 lasts until the creature dies.
        /// </summary>
        public float Duration { get; set; } = 30f;
    }
}
