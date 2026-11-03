using System.Runtime.InteropServices;
using Drova_Modding_API.Access;
using Il2CppInterop.Runtime.Injection;
using Il2CppSteamworks;
using MelonLogger = MelonLoader.MelonLogger;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Receives <c>GameRichPresenceJoinRequested_t</c> through the game's own Steamworks.NET dispatcher, which the game
    /// pumps every frame. The game's IL2CPP build has no <c>Callback&lt;T&gt;</c> for this struct, so an injected
    /// <see cref="Callback"/> is put straight into the dispatcher's registry.
    /// </summary>
    internal sealed class SteamJoinCallback : Callback
    {
        /// <summary>CSteamID, then char[256], packed to 8.</summary>
        private const int ConnectOffset = 8;

        public SteamJoinCallback(IntPtr pointer) : base(pointer)
        {
        }

        public SteamJoinCallback() : base(ClassInjector.DerivedConstructorPointer<SteamJoinCallback>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public override void OnRunCallback(IntPtr pvParam)
        {
            try
            {
                string? joinString = Marshal.PtrToStringUTF8(pvParam + ConnectOffset);
                if (!string.IsNullOrEmpty(joinString)) SteamAccess.RaiseJoinRequested(joinString);
            }
            catch (Exception e)
            {
                // Swallowed: the dispatcher's loop runs the game's own callbacks too.
                MelonLogger.Error("[Steam] handling a join request failed: " + e);
            }
        }

        public override void SetUnregistered()
        {
        }

        internal static void Install()
        {
            ClassInjector.RegisterTypeInIl2Cpp<SteamJoinCallback>();

            Il2CppSystem.Collections.Generic.Dictionary<int, Il2CppSystem.Collections.Generic.List<Callback>> registry =
                CallbackDispatcher.m_registeredCallbacks;
            int id = GameRichPresenceJoinRequested_t.k_iCallback;

            if (!registry.ContainsKey(id)) registry[id] = new Il2CppSystem.Collections.Generic.List<Callback>();

            registry[id].Add(new SteamJoinCallback());
        }
    }
}
