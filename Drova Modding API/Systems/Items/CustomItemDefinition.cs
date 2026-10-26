namespace Drova_Modding_API.Systems.Items
{
    /// <summary>
    /// One custom item from <c>Mods/Modding_API/Items/*.json</c>: a copy of a vanilla item with its own id, texts and values.
    /// </summary>
    public sealed class CustomItemDefinition
    {
        /// <summary>
        /// Readable id, used by dialogues, traders, placements and the cheat console. Never rename it: the save guid comes from it.
        /// </summary>
        public string ReadableId { get; set; } = string.Empty;

        /// <summary>
        /// Optional fixed guid. Left out, it is derived from <see cref="ReadableId"/>, which is what saves store.
        /// </summary>
        public string? Guid { get; set; }

        /// <summary>
        /// Readable id of the vanilla item that is copied: its look, behavior and effects.
        /// </summary>
        public string Base { get; set; } = string.Empty;

        /// <summary>
        /// Localization file with <c>&lt;ReadableId&gt;_name</c> and <c>&lt;ReadableId&gt;_description</c>.
        /// </summary>
        public string LocaPath { get; set; } = string.Empty;

        /// <summary>
        /// Trader buy price, or the base item's when null.
        /// </summary>
        public int? BuyValue { get; set; }

        /// <summary>
        /// Trader sell price, or the base item's when null.
        /// </summary>
        public int? SellValue { get; set; }

        /// <summary>
        /// Most items in one stack, or the base item's when null.
        /// </summary>
        public int? MaxStackSize { get; set; }

        /// <summary>
        /// Stacks of the base item's status effect the item applies, instead of the effect's own count. The effect acts once
        /// when applied and once per stack, so a heal over time heals <c>EffectStacks + 1</c> times.
        /// </summary>
        public int? EffectStacks { get; set; }

        /// <summary>
        /// Ability values to change, by key (<c>ABI_HealthValue</c>, <c>ABI_Lifetime</c>, <c>Dmg_MaxHitCount</c>...). The item
        /// gets its own copies of the base item's abilities, so the base item keeps its values. Bools take 0 or 1.
        /// </summary>
        public Dictionary<string, double>? Ability { get; set; }

        /// <summary>
        /// PNG file next to the item file, shown in the inventory and on placed pickups. The base item's icon when null.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Makes the item a letter with its own text and journal entry. The base must be a vanilla letter.
        /// </summary>
        public CustomLetterDefinition? Letter { get; set; }

        /// <summary>
        /// Readable id of the spell this item teaches, a vanilla or custom one. The base must be a vanilla spell recipe
        /// (<c>recipe_flow_*</c>), which brings its runestone rule.
        /// </summary>
        public string? LearnSpell { get; set; }

        /// <summary>
        /// Makes the spell a morph into a creature. The base must be a vanilla spell (<c>flow_*</c>).
        /// </summary>
        public CustomMorphDefinition? Morph { get; set; }
    }
}
