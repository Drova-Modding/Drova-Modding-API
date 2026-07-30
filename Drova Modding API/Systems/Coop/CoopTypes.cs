using Il2CppDrova;

namespace Drova_Modding_API.Systems.Coop
{
    /// <summary>
    /// What a mod has decided about its own behavior in a shared world.
    ///
    /// **Declared rather than inferred**, because no amount of inspection can tell whether a mod that
    /// spawns a bear meant one bear in the world or one bear per player. Only the author knows, and a mod
    /// that has never thought about it should read as exactly that rather than as either answer.
    /// </summary>
    public enum CoopBehavior
    {
        /// <summary>
        /// Nobody has said. The default, and deliberately the zero value so that "never declared" is a
        /// state the API can see rather than a state indistinguishable from a choice.
        /// </summary>
        Unreviewed = 0,

        /// <summary>
        /// This mod does not work in a shared world and would rather do nothing than do it wrong.
        /// <see cref="Access.CoopAccess.ShouldRun"/> answers false for it while a session is live.
        /// </summary>
        SinglePlayerOnly,

        /// <summary>
        /// This mod changes the shared world and only the authority should do it.
        /// <see cref="Access.CoopAccess.ShouldRun"/> answers false for it on a machine that is not the
        /// authority, which with no session is nobody, so it runs normally in single-player.
        /// </summary>
        AuthorityGated,

        /// <summary>
        /// This mod replicates its own effects and wants to run everywhere.
        /// <see cref="Access.CoopAccess.ShouldRun"/> always answers true for it.
        /// </summary>
        FullyReplicated,
    }

    /// <summary>
    /// Something a co-op implementation may or may not keep in step between machines.
    ///
    /// **A queryable frontier rather than folklore.** What a co-op mod synchronizes changes release by
    /// release, and a mod that guesses is either needlessly disabled or quietly broken. Asking means a mod
    /// can say "I need spawned creatures to be shared, and they are not, so I will not roll one".
    /// </summary>
    public enum CoopCapability
    {
        /// <summary>Other players are visible as bodies in the world.</summary>
        PlayerBodies,

        /// <summary>Players' health is kept in step, including damage between them.</summary>
        PlayerHealth,

        /// <summary>Global variables and quest state are kept in step.</summary>
        GlobalVariables,

        /// <summary>Status effects applied to a player reach that player's own game.</summary>
        StatusEffects,

        /// <summary>Recipes and writings learned by one player reach the others.</summary>
        Knowledge,

        /// <summary>Creatures spawned at runtime exist on every machine.</summary>
        SpawnedNpcs,

        /// <summary>Enemy health and death are shared rather than simulated per machine.</summary>
        EnemyHealth,

        /// <summary>Loot is decided once rather than rolled per machine.</summary>
        Loot,

        /// <summary>
        /// Experience from a kill reaches every player, not only whoever landed the blow.
        ///
        /// Worth asking before scaling anything by player level. With this unsupported, the players in a
        /// session drift apart in level for as long as they play, so "the player's level" is not one number
        /// and a difficulty curve built on it fits at most one of them.
        /// </summary>
        Experience,
    }

    /// <summary>
    /// Identifies one player in a session. Opaque on purpose: a mod should compare and pass these around,
    /// never construct one or assume what the number means.
    /// </summary>
    public readonly struct CoopPlayerId : IEquatable<CoopPlayerId>
    {
        /// <summary>Nobody. What an unset or unknown player reads as.</summary>
        public static readonly CoopPlayerId None = new(byte.MaxValue);

        /// <summary>
        /// Wraps a raw id. Called by a co-op implementation, and a mod has no reason to.
        /// </summary>
        /// <param name="value">The implementation's own id for this player.</param>
        public CoopPlayerId(byte value)
        {
            Value = value;
        }

        /// <summary>The raw id, for an implementation that needs to translate.</summary>
        public byte Value { get; }

        /// <inheritdoc/>
        public bool Equals(CoopPlayerId other) => Value == other.Value;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is CoopPlayerId other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => Value;

        /// <inheritdoc/>
        public override string ToString() => Value == byte.MaxValue ? "nobody" : "player " + Value;

        /// <summary>Whether two ids name the same player.</summary>
        public static bool operator ==(CoopPlayerId left, CoopPlayerId right) => left.Equals(right);

        /// <summary>Whether two ids name different players.</summary>
        public static bool operator !=(CoopPlayerId left, CoopPlayerId right) => !left.Equals(right);
    }

    /// <summary>
    /// One player in the session, as much of them as a mod has any business knowing.
    /// </summary>
    public readonly struct CoopPlayer
    {
        /// <summary>
        /// Builds a description of a player. Called by a co-op implementation.
        /// </summary>
        /// <param name="id">Who they are.</param>
        /// <param name="name">What they are called.</param>
        /// <param name="isLocal">Whether this is the person at this machine.</param>
        /// <param name="isAuthority">Whether their machine decides what the shared world contains.</param>
        /// <param name="level">Their character level, or zero when it is not known yet.</param>
        public CoopPlayer(CoopPlayerId id, string name, bool isLocal, bool isAuthority, int level)
        {
            Id = id;
            Name = name;
            IsLocal = isLocal;
            IsAuthority = isAuthority;
            Level = level;
        }

        /// <summary>Who they are.</summary>
        public CoopPlayerId Id { get; }

        /// <summary>What they are called. Never null, but it comes from a stranger, so treat it as text.</summary>
        public string Name { get; }

        /// <summary>Whether this is the person at this machine.</summary>
        public bool IsLocal { get; }

        /// <summary>Whether their machine decides what the shared world contains.</summary>
        public bool IsAuthority { get; }

        /// <summary>
        /// Their character level, or zero when it is not known. **Zero is not level zero.** It means the
        /// answer has not arrived, which is a real state for a player who has just joined.
        /// </summary>
        public int Level { get; }
    }

    /// <summary>
    /// What a co-op implementation registers with <see cref="Access.CoopAccess"/> so that mods can ask
    /// about the session without depending on it.
    ///
    /// **The direction matters.** The API ships to everybody and cannot reference a co-op mod, while a
    /// co-op mod already references the API. So the seam faces this way, and a mod that wants to know
    /// whether it is the authority asks the API, which asks whoever answered.
    /// </summary>
    public interface ICoopProvider
    {
        /// <summary>Whether a session is live and somebody else is in it.</summary>
        bool IsMultiplayer { get; }

        /// <summary>Whether this machine decides what the shared world contains.</summary>
        bool IsWorldAuthority { get; }

        /// <summary>
        /// A number both machines agree on, for a mod that must make the same random decision everywhere.
        /// Stable for the length of a session and no longer.
        /// </summary>
        ulong SessionSeed { get; }

        /// <summary>
        /// Fills the list with everybody in the session. Implementations clear it first and are expected
        /// not to allocate when the caller reuses the list.
        /// </summary>
        /// <param name="buffer">Filled with the session's players.</param>
        void GetPlayers(List<CoopPlayer> buffer);

        /// <summary>
        /// Finds the body being shown for a player.
        /// </summary>
        /// <param name="id">Which player.</param>
        /// <param name="actor">Their body, when one exists on this machine.</param>
        /// <returns>False when that player has no body here yet.</returns>
        bool TryGetPlayerActor(CoopPlayerId id, out Actor actor);

        /// <summary>
        /// Whether this entity is a body being shown for somebody playing elsewhere.
        ///
        /// **Worth asking before almost anything.** Such a body is a clone of the player prefab, so it
        /// answers yes to most of the game's own "is this the player" tests while being driven entirely
        /// from the wire, so a mod that treats it as the player acts on a character it does not own.
        /// </summary>
        /// <param name="entity">What to ask about. Can be null.</param>
        /// <returns>True when this is somebody else's body.</returns>
        bool IsRemotePlayer(Entity? entity);

        /// <summary>
        /// Whether this machine is currently applying a change that arrived from elsewhere.
        ///
        /// A mod that hooks the game to observe changes should do nothing while this is true, or it will
        /// react to a replicated change as though the local game had made it, and if it reports what it
        /// sees, hand it straight back.
        /// </summary>
        bool IsReplicating { get; }

        /// <summary>
        /// Whether this implementation keeps the named thing in step between machines.
        /// </summary>
        /// <param name="capability">What the caller needs.</param>
        /// <returns>False for anything this implementation does not synchronize.</returns>
        bool Supports(CoopCapability capability);
    }
}
