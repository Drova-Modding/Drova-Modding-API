using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Drova_Modding_API.Systems.Coop;
using Il2CppDrova;
using MelonLoader;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// What a mod needs to know to behave correctly when somebody else is in the world.
    ///
    /// With no co-op mod registered, <see cref="IsWorldAuthority"/> is true and <see cref="IsMultiplayer"/> is false,
    /// so an authority-gated mod runs normally in single-player. <see cref="NetworkAccess.Role"/> is not a substitute:
    /// it is <c>None</c> without a session, so <c>if (Role == Host) Spawn();</c> spawns nothing in single-player.
    /// <code>
    /// if (!CoopAccess.ShouldRun(CoopBehavior.AuthorityGated)) return;
    /// </code>
    /// </summary>
    public static class CoopAccess
    {
        private static ICoopProvider _provider = NullCoopProvider.Instance;

        private static readonly Dictionary<string, HostSetting> SharedSettings = new();

        /// <summary>Every setting registered with <see cref="ShareHostSetting"/>, for the co-op implementation.</summary>
        public static IReadOnlyCollection<HostSetting> HostSettings => SharedSettings.Values;

        /// <summary>Whether this machine decides what the shared world contains. True when nothing has registered.</summary>
        public static bool IsWorldAuthority => _provider.IsWorldAuthority;

        /// <summary>
        /// Whether a session is live with somebody else in it. False with nothing registered and for a host alone in
        /// a session.
        /// </summary>
        public static bool IsMultiplayer => _provider.IsMultiplayer;

        /// <summary>Whether any co-op implementation has registered itself.</summary>
        public static bool HasProvider => !ReferenceEquals(_provider, NullCoopProvider.Instance);

        /// <summary>
        /// Raised on the main thread when somebody joined or left, or authority moved. A throwing subscriber does not
        /// stop the rest.
        /// </summary>
        public static event Action? OnSessionChanged;

        /// <summary>Raised when a co-op implementation registers or unregisters. Drop cached answers then.</summary>
        public static event Action? OnProviderChanged;

        /// <summary>
        /// Logs how the calling mod behaves in a shared world, once at initialization. Gates nothing, see
        /// <see cref="ShouldRun"/>.
        /// </summary>
        /// <param name="behavior">What this mod does.</param>
        public static void Declare(CoopBehavior behavior)
        {
            Declare(Assembly.GetCallingAssembly().GetName().Name ?? "unknown", behavior);
        }

        /// <summary>Logs how a named mod behaves in a shared world. See <see cref="Declare(CoopBehavior)"/>.</summary>
        /// <param name="modName">Which mod.</param>
        /// <param name="behavior">What it does.</param>
        public static void Declare(string modName, CoopBehavior behavior)
        {
            if (string.IsNullOrEmpty(modName)) return;

            MelonLogger.Msg($"[Coop] {modName} declares itself {behavior}.");
        }

        /// <summary>
        /// Whether work of the given kind should happen on this machine right now.
        /// The behavior is passed in because <c>Assembly.GetCallingAssembly</c> names the wrong caller once inlined.
        /// </summary>
        /// <param name="behavior">How this work behaves in a shared world.</param>
        /// <returns>False only for single-player-only work in a session, and authority-gated work off the
        /// authority.</returns>
        public static bool ShouldRun(CoopBehavior behavior)
        {
            return behavior switch
            {
                CoopBehavior.SinglePlayerOnly => !IsMultiplayer,
                CoopBehavior.AuthorityGated => IsWorldAuthority,
                _ => true,
            };
        }

        /// <summary>
        /// Clears the list and fills it with everybody in the session, the local player included.
        /// With no session it holds the local player alone.
        /// </summary>
        /// <param name="buffer">Filled with the session's players.</param>
        public static void GetPlayers(List<CoopPlayer> buffer)
        {
            if (buffer == null) return;

            _provider.GetPlayers(buffer);
        }

        /// <summary>
        /// Finds the body being shown for a player.
        /// </summary>
        /// <param name="id">Which player.</param>
        /// <param name="actor">Their body, when one exists here.</param>
        /// <returns>False when that player has no body on this machine.</returns>
        public static bool TryGetPlayerActor(CoopPlayerId id, [NotNullWhen(true)] out Actor? actor)
        {
            return _provider.TryGetPlayerActor(id, out actor);
        }

        /// <summary>
        /// Whether this entity is a body being shown for somebody playing elsewhere. Such a body is cloned from the
        /// player prefab, so the game's own "is this the player" tests answer yes for it.
        /// </summary>
        /// <param name="entity">What to ask about. Can be null.</param>
        /// <returns>True when this is somebody else's body. Always false with no session.</returns>
        public static bool IsRemotePlayer(Entity? entity)
        {
            return _provider.IsRemotePlayer(entity);
        }

        /// <summary>
        /// Whether a change from elsewhere is being applied right now. A mod watching the game for changes should
        /// return early while this is true, or it sends the change straight back.
        /// </summary>
        public static bool IsReplicating => _provider.IsReplicating;

        /// <summary>
        /// Whether the installed co-op implementation keeps the named thing in step. Always false with no session.
        /// </summary>
        /// <param name="capability">What the caller needs.</param>
        /// <returns>False for anything not synchronized.</returns>
        public static bool Supports(CoopCapability capability)
        {
            return _provider.Supports(capability);
        }

        /// <summary>
        /// A number every machine in the session agrees on. Not stable across a save and load, since a Drova save is
        /// identified only by hero and slot index. Letting the authority decide is usually the better tool.
        /// </summary>
        public static ulong SessionSeed => _provider.SessionSeed;

        /// <summary>
        /// Derives a number from the session seed that every machine works out identically.
        /// </summary>
        /// <param name="key">What is being decided. Two decisions sharing a key get the same answer.</param>
        /// <returns>The derived number.</returns>
        public static uint Roll(string key) => CoopRandom.Derive(SessionSeed, key);

        /// <summary>
        /// Derives a number in <c>[0, 1)</c>. See <see cref="Roll"/>.
        /// </summary>
        /// <param name="key">What is being decided.</param>
        /// <returns>A number from zero up to but not including one.</returns>
        public static float RollValue(string key) => CoopRandom.DeriveValue(SessionSeed, key);

        /// <summary>
        /// Derives a number in a half-open range. See <see cref="Roll"/>.
        /// </summary>
        /// <param name="key">What is being decided.</param>
        /// <param name="minInclusive">The lowest number that can come out.</param>
        /// <param name="maxExclusive">One past the highest.</param>
        /// <returns>The derived number.</returns>
        public static int RollRange(string key, int minInclusive, int maxExclusive)
        {
            return CoopRandom.DeriveRange(SessionSeed, key, minInclusive, maxExclusive);
        }

        /// <summary>
        /// Makes a setting follow the host while a session is live, for settings that change what the world contains
        /// (spawns, AI, a randomizer seed). The co-op implementation sends the host's value to every client, calls
        /// <paramref name="enforce"/> with it where it differs, reloads the client's savegame once if
        /// <paramref name="needsReload"/>, and calls <paramref name="enforce"/> with <c>null</c> when the session ends.
        /// Enforce as an override, not by writing the player's own config, so leaving gives them their choice back.
        /// </summary>
        /// <param name="key">Unique across mods, e.g. <c>"InsaneMonsters.SpawnGroups"</c>. A second call with the same key replaces the first.</param>
        /// <param name="read">The value in effect on this machine.</param>
        /// <param name="enforce">Makes the host's value the one in effect; <c>null</c> drops the override.</param>
        /// <param name="needsReload">Whether a value only takes effect when a savegame loads.</param>
        public static void ShareHostSetting(string key, Func<string> read, Action<string?> enforce, bool needsReload = false)
        {
            if (string.IsNullOrEmpty(key) || read == null || enforce == null) return;

            SharedSettings[key] = new HostSetting(key, read, enforce, needsReload);
        }

        /// <summary>
        /// Registers a co-op implementation as the answer to everything here. A second registration replaces the
        /// first and logs a warning.
        /// </summary>
        /// <param name="provider">The implementation.</param>
        public static void RegisterProvider(ICoopProvider provider)
        {
            if (provider == null) return;

            if (HasProvider)
            {
                MelonLogger.Warning("[Coop] a second co-op implementation registered and has replaced the " +
                    "first. Only one can decide who the authority is.");
            }

            _provider = provider;

            Raise(OnProviderChanged, nameof(OnProviderChanged));
            Raise(OnSessionChanged, nameof(OnSessionChanged));
        }

        /// <summary>
        /// Takes a registration back, returning every answer here to its single-player value. Ignored unless
        /// <paramref name="provider"/> is the one registered.
        /// </summary>
        /// <param name="provider">The implementation that registered.</param>
        public static void UnregisterProvider(ICoopProvider provider)
        {
            if (provider == null || !ReferenceEquals(_provider, provider)) return;

            _provider = NullCoopProvider.Instance;

            Raise(OnProviderChanged, nameof(OnProviderChanged));
            Raise(OnSessionChanged, nameof(OnSessionChanged));
        }

        /// <summary>
        /// Tells subscribers the session changed. Called by the registered implementation when players
        /// join or leave, or when authority moves.
        /// </summary>
        public static void NotifySessionChanged()
        {
            Raise(OnSessionChanged, nameof(OnSessionChanged));
        }

        private static void Raise(Action? handler, string name)
        {
            if (handler == null) return;

            foreach (Delegate subscriber in handler.GetInvocationList())
            {
                try
                {
                    ((Action)subscriber)();
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"[Coop] a subscriber to {name} failed: " + e);
                }
            }
        }
    }
}
