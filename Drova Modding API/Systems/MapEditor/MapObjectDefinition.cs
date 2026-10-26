namespace Drova_Modding_API.Systems.MapEditor
{
    /// <summary>
    /// One prop placed in the world by a map layout file.
    /// </summary>
    public sealed class MapObjectDefinition
    {
        /// <summary>
        /// Unique id across all layout files.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Addressable guid of an environment prefab. With no <see cref="Prefab"/>, <see cref="Scene"/> or
        /// <see cref="Dynamic"/> the entry is an invisible blocker of <see cref="Width"/> by <see cref="Height"/>.
        /// </summary>
        public string Prefab { get; set; } = string.Empty;

        /// <summary>
        /// Chunk scene of a prop that is baked into the world, copied from there. Used with <see cref="Path"/>.
        /// </summary>
        public string Scene { get; set; } = string.Empty;

        /// <summary>
        /// Hierarchy path of that prop in <see cref="Scene"/>, as in <see cref="MapRemovalDefinition.Path"/>.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Asset id of one of the game's dynamic save objects, for example <c>Trap_BearTrap</c>.
        /// </summary>
        public string Dynamic { get; set; } = string.Empty;

        /// <summary>
        /// Position x.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Position y.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Uniform scale. 1 is the prefab's own size.
        /// </summary>
        public float Scale { get; set; } = 1f;

        /// <summary>
        /// Mirrors the prop horizontally.
        /// </summary>
        public bool FlipX { get; set; }

        /// <summary>
        /// Blocker width before <see cref="Scale"/>.
        /// </summary>
        public float Width { get; set; } = 64f;

        /// <summary>
        /// Blocker height before <see cref="Scale"/>.
        /// </summary>
        public float Height { get; set; } = 64f;

        /// <summary>
        /// Contents of a copied container, replacing what the original would start with. Null keeps the original's loot.
        /// </summary>
        public List<MapLootDefinition>? Loot { get; set; }

        /// <summary>
        /// Seed of the save guids of a copied object. The editor fills it in, so a new object never inherits the saved state
        /// of a deleted one with the same id. Without it the id is the seed.
        /// </summary>
        public string? Seed { get; set; }

        /// <summary>
        /// Locks or unlocks a copied chest or door. Null keeps the original's lock.
        /// </summary>
        public bool? Locked { get; set; }

        /// <summary>
        /// Keeps the crime reactions of a copied object. Off by default: the owners of the original live where it stands,
        /// so taking from a copy elsewhere is no theft.
        /// </summary>
        public bool Crime { get; set; }

        /// <summary>
        /// Rotation in degrees, counterclockwise.
        /// </summary>
        public float Rotation { get; set; }

        /// <summary>
        /// Paints the ground: only the source's sprites are placed, without behaviours or colliders, on the floor sorting
        /// layer by default. Use decals (dirt, moss, puddles, trails) as the source.
        /// </summary>
        public bool Stamp { get; set; }

        /// <summary>
        /// Sorting layer of a stamp: <c>Floor</c> (the default, under water, bridges and decals) or <c>OnFloor</c>.
        /// </summary>
        public string? SortingLayer { get; set; }

        /// <summary>
        /// Sorting order of a stamp within its layer. Defaults to -10, above the baked ground at -16.
        /// </summary>
        public int? SortingOrder { get; set; }

        /// <summary>
        /// Tint of a stamp, <c>#RRGGBB</c> or <c>#RRGGBBAA</c>.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Footstep sound inside a stamp's bounds: Grass, Dirt, Stone, Water, Wood, Bones, Mud or Sand.
        /// </summary>
        public string? Terrain { get; set; }

        /// <summary>
        /// Where a copied gate (a vanilla teleporter, such as <c>Scene_Teleporters</c> / <c>Teleporter_Atlanterdungeon_Enter</c>)
        /// sends the player. Used with <see cref="TargetY"/>.
        /// </summary>
        public float? TargetX { get; set; }

        /// <summary>
        /// See <see cref="TargetX"/>.
        /// </summary>
        public float? TargetY { get; set; }

        internal bool IsBlocker => Prefab.Length == 0 && Scene.Length == 0 && Dynamic.Length == 0;
    }
}
