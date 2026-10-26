using System.Diagnostics.CodeAnalysis;
using Il2CppDrova;

namespace Drova_Modding_API.Systems.Coop
{
    /// <summary>
    /// The answers when nobody is playing together: one player, who decides everything, so an authority-gated
    /// mod runs on a machine without a co-op mod.
    /// </summary>
    internal sealed class NullCoopProvider : ICoopProvider
    {
        internal static readonly NullCoopProvider Instance = new();

        private readonly ulong _seed;

        private NullCoopProvider()
        {
            // Per process, so not every installation gets the same "random" encounters.
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

            if (!Access.PlayerAccess.TryGetPlayer(out Actor? player)) return;

            Access.PlayerAccess.TryGetLevel(player, out int level);

            buffer.Add(new CoopPlayer(new CoopPlayerId(0), "you", true, true, level));
        }

        /// <inheritdoc/>
        public bool TryGetPlayerActor(CoopPlayerId id, [NotNullWhen(true)] out Actor? actor)
        {
            if (id.Value == 0 && Access.PlayerAccess.TryGetPlayer(out actor))
            {
                return true;
            }

            actor = null;
            return false;
        }

        /// <inheritdoc/>
        public bool IsRemotePlayer(Entity? entity) => false;

        /// <inheritdoc/>
        public bool IsReplicating => false;

        /// <summary>
        /// Nothing is synchronized, because there is nobody to synchronize with. Most mods want
        /// <see cref="Access.CoopAccess.ShouldRun"/> instead.
        /// </summary>
        public bool Supports(CoopCapability capability) => false;
    }
}
