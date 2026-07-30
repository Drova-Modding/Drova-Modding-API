using Il2CppDrova;

namespace Drova_Modding_API.Systems.Coop
{
    /// <summary>
    /// The answers when nobody is playing together: one player, who decides everything.
    ///
    /// **This is what makes the whole contract safe to adopt.** A mod written against
    /// <see cref="Access.CoopAccess"/> has to behave identically on a machine with no co-op mod
    /// installed, which is nearly every machine. The no-session answers are not placeholders, they are
    /// the common case, and they are chosen to make an authority-gated mod run rather than sit idle.
    /// </summary>
    internal sealed class NullCoopProvider : ICoopProvider
    {
        /// <summary>The one instance. Stateless apart from the seed, so there is no reason for more.</summary>
        internal static readonly NullCoopProvider Instance = new();

        private readonly ulong _seed;

        private NullCoopProvider()
        {
            // Randomized per process rather than fixed, because a constant would ship the identical
            // "random" sequence to every installation, so every player would meet the same encounters in
            // the same order. Not stable across a save and load, and the documentation says so: there is
            // nothing in a Drova save to hang a stable seed on, since a save is identified by hero and
            // slot index alone, which are the same numbers on everybody's machine.
            _seed = unchecked((ulong)Guid.NewGuid().GetHashCode() << 32 | (uint)Environment.TickCount);
        }

        /// <inheritdoc/>
        public bool IsMultiplayer => false;

        /// <inheritdoc/>
        public bool IsWorldAuthority => true;

        /// <inheritdoc/>
        public ulong SessionSeed => _seed;

        /// <inheritdoc/>
        public void GetPlayers(List<CoopPlayer> buffer)
        {
            buffer.Clear();

            if (!Access.PlayerAccess.TryGetPlayer(out Actor player) || player == null) return;

            Access.PlayerAccess.TryGetLevel(player, out int level);

            buffer.Add(new CoopPlayer(new CoopPlayerId(0), "you", true, true, level));
        }

        /// <inheritdoc/>
        public bool TryGetPlayerActor(CoopPlayerId id, out Actor actor)
        {
            if (id.Value == 0 && Access.PlayerAccess.TryGetPlayer(out actor))
            {
                return true;
            }

            actor = null!;
            return false;
        }

        /// <inheritdoc/>
        public bool IsRemotePlayer(Entity? entity) => false;

        /// <inheritdoc/>
        public bool IsReplicating => false;

        /// <summary>
        /// Nothing is synchronized, because there is nothing to synchronize with.
        ///
        /// **Not the same as "everything works".** A mod asking whether spawned creatures are shared gets
        /// false here and should read it as "there is nobody to share them with" rather than as a missing
        /// feature. That is why <see cref="Access.CoopAccess.ShouldRun"/> exists, and it is the call most
        /// mods want instead.
        /// </summary>
        public bool Supports(CoopCapability capability) => false;
    }
}
