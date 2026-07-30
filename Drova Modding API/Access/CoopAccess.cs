using System.Reflection;
using Drova_Modding_API.Systems.Coop;
using Il2CppDrova;
using MelonLoader;

namespace Drova_Modding_API.Access
{
    /// <summary>
    /// What a mod needs to know to behave correctly when somebody else is in the world.
    ///
    /// **The problem this solves.** A mod that spawns creatures, rolls dice or writes world state has to
    /// know whether it is the one that should be doing it. Without an answer, every such mod on every
    /// machine does the work independently. Two players get two unrelated encounters, each visible to one
    /// of them, and both take the loot. Asking is one call.
    ///
    /// **The defaults are inverted from what a networking API would choose, and that is the whole
    /// design.** <see cref="IsWorldAuthority"/> is true and <see cref="IsMultiplayer"/> is false when
    /// nothing has registered, so a mod that gates on authority runs normally on the overwhelming
    /// majority of machines, which have no co-op mod at all. Any other default would silently disable
    /// working single-player mods for everyone who installed this API. With these, the only machine where
    /// an unconverted mod misbehaves is a joining client, which is also the only machine where a second
    /// person is present to notice and say so.
    ///
    /// **Do not reach for <see cref="NetworkAccess.Role"/> instead.** It answers a different question and
    /// fails closed. With no networking build it is <c>None</c>, so <c>if (Role == Host) Spawn();</c>
    /// spawns nothing in single-player. A transport role is also not world authority, because a peer is a
    /// client while it is still connecting and before it has an identity.
    ///
    /// Typical use is one line wherever a mod is about to act on the world:
    /// <code>
    /// if (!CoopAccess.ShouldRun(CoopBehavior.AuthorityGated)) return;
    /// </code>
    /// </summary>
    public static class CoopAccess
    {
        private static ICoopProvider _provider = NullCoopProvider.Instance;

        /// <summary>
        /// Whether this machine decides what the shared world contains.
        ///
        /// **True when nothing has registered**, which is what makes an authority check safe to write in a
        /// mod that has never heard of co-op.
        /// </summary>
        public static bool IsWorldAuthority => _provider.IsWorldAuthority;

        /// <summary>
        /// Whether a session is live with somebody else in it.
        ///
        /// False when nothing has registered, and false for a host sitting alone in a session nobody has
        /// joined. The question is whether there is another player, not whether a socket is open.
        /// </summary>
        public static bool IsMultiplayer => _provider.IsMultiplayer;

        /// <summary>Whether any co-op implementation has registered itself.</summary>
        public static bool HasProvider => !ReferenceEquals(_provider, NullCoopProvider.Instance);

        /// <summary>
        /// Raised when the session changes: somebody joined or left, or authority moved.
        ///
        /// On the main thread, and each subscriber is isolated, so one that throws does not stop the rest
        /// being told.
        /// </summary>
        public static event Action? OnSessionChanged;

        /// <summary>
        /// Raised when a co-op implementation registers or unregisters. Rare, but a mod that caches an
        /// answer from here should drop the cache when this fires.
        /// </summary>
        public static event Action? OnProviderChanged;

        /// <summary>
        /// Announces how the calling mod behaves in a shared world. Say it once, at initialization.
        ///
        /// **This gates nothing.** Gating is <see cref="ShouldRun"/>, which is told the behavior at the
        /// point of the question. This writes a log line, so that a player sending in a log says along
        /// with it what every installed mod believes about itself.
        ///
        /// The caller is identified by its own assembly, so a mod cannot announce for another.
        /// </summary>
        /// <param name="behavior">What this mod does.</param>
        public static void Declare(CoopBehavior behavior)
        {
            Declare(Assembly.GetCallingAssembly().GetName().Name ?? "unknown", behavior);
        }

        /// <summary>
        /// Announces how a named mod behaves in a shared world, for a caller that spans assemblies or
        /// wants to be explicit. See <see cref="Declare(CoopBehavior)"/>.
        /// </summary>
        /// <param name="modName">Which mod.</param>
        /// <param name="behavior">What it does.</param>
        public static void Declare(string modName, CoopBehavior behavior)
        {
            if (string.IsNullOrEmpty(modName)) return;

            MelonLogger.Msg($"[Coop] {modName} declares itself {behavior}.");
        }

        /// <summary>
        /// Whether work of the given kind should happen on this machine right now.
        ///
        /// **The behavior is named here rather than looked up, and that is the whole point.** An earlier
        /// version took no argument and worked out who was asking with <c>Assembly.GetCallingAssembly</c>.
        /// The runtime is free to answer that differently once it has inlined the call, naming whoever
        /// called the caller, so the lookup missed. A missing declaration fails open, so a mod that had
        /// declared itself authority-gated was told yes on every machine, and the symptom was two players
        /// each getting their own copy of whatever it spawned.
        ///
        /// Saying it at the point of the question costs the caller a word and cannot be wrong.
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
        /// Fills the list with everybody in the session, the local player included.
        ///
        /// The list is cleared first, and reusing one across calls allocates nothing. With no session it
        /// holds the local player alone, so a mod that walks it does not need a special case.
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
        public static bool TryGetPlayerActor(CoopPlayerId id, out Actor actor)
        {
            return _provider.TryGetPlayerActor(id, out actor);
        }

        /// <summary>
        /// Whether this entity is a body being shown for somebody playing elsewhere.
        ///
        /// **Worth asking before acting on anything that looks like the player.** Such a body is cloned
        /// from the player prefab, so it answers yes to the game's own "is this the player" tests while
        /// being driven entirely from the wire.
        /// </summary>
        /// <param name="entity">What to ask about. Can be null.</param>
        /// <returns>True when this is somebody else's body. Always false with no session.</returns>
        public static bool IsRemotePlayer(Entity? entity)
        {
            return _provider.IsRemotePlayer(entity);
        }

        /// <summary>
        /// Whether a change from elsewhere is being applied right now.
        ///
        /// A mod that patches the game to watch for changes should return early while this is true,
        /// otherwise it treats a replicated change as a local one, and if it reacts by sending something,
        /// hands it straight back to where it came from.
        /// </summary>
        public static bool IsReplicating => _provider.IsReplicating;

        /// <summary>
        /// Whether the installed co-op implementation keeps the named thing in step.
        ///
        /// Always false with no session, which reads correctly: there is nobody to be out of step with.
        /// </summary>
        /// <param name="capability">What the caller needs.</param>
        /// <returns>False for anything not synchronized.</returns>
        public static bool Supports(CoopCapability capability)
        {
            return _provider.Supports(capability);
        }

        /// <summary>
        /// A number every machine in the session agrees on, for a mod that must make the same random
        /// decision everywhere.
        ///
        /// **Not stable across a save and load**, and there is no way to make it so. A Drova save is
        /// identified by hero index and slot index alone, which are the same numbers on every machine and
        /// in every playthrough. A mod needing randomness that survives a reload has to persist its own.
        ///
        /// **And it is the second-best tool.** Sharing a seed makes two machines agree only if everything
        /// else feeding the decision already agrees, which is rarer than it looks. Anything filtered by
        /// the local player's level or placed relative to their position diverges regardless of the roll.
        /// The first answer is nearly always to let the authority decide.
        /// </summary>
        public static ulong SessionSeed => _provider.SessionSeed;

        /// <summary>
        /// Derives a number from the session seed that every machine works out identically.
        /// </summary>
        /// <param name="key">What is being decided. Name it specifically enough that two different
        /// decisions do not share a key, or they will produce the same answer.</param>
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
        /// Registers a co-op implementation as the answer to everything here.
        ///
        /// One at a time. A second registration replaces the first and says so, because two
        /// implementations disagreeing about who the authority is would be worse than either of them being
        /// wrong alone.
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
        /// Takes a registration back, returning every answer here to its single-player value.
        ///
        /// Identity-checked, so an implementation shutting down after being replaced cannot unregister the
        /// one that replaced it.
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
                    // One at a time, so a mod that throws in its handler does not stop the others being
                    // told. They are making decisions about whether to act on the world.
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
