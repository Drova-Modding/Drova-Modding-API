using Il2CppInterop.Runtime.Injection;
using Il2CppSteamworks;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
using MelonLogger = MelonLoader.MelonLogger;

namespace Drova_Modding_API.Systems.Networking.Impl
{
    /// <summary>
    /// Receives the result of an asynchronous Steam call through the game's Steamworks.NET dispatcher. That dispatcher
    /// fetches every finished call's result, registered or not, so asking Steam for it directly would find nothing.
    /// </summary>
    internal sealed class SteamCallResult : CallResult
    {
        private Action<IntPtr, bool>? _handler;

        public SteamCallResult(IntPtr pointer) : base(pointer)
        {
        }

        public SteamCallResult() : base(ClassInjector.DerivedConstructorPointer<SteamCallResult>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public override void OnRunCallResult(IntPtr pvParam, bool bFailed, ulong hSteamAPICall)
        {
            try
            {
                _handler?.Invoke(pvParam, bFailed);
            }
            catch (Exception e)
            {
                // Swallowed: the dispatcher's loop runs the game's own call results too.
                MelonLogger.Error("[Steam] handling a Steam call result failed: " + e);
            }
        }

        public override void SetUnregistered()
        {
        }

        /// <summary>
        /// Runs <paramref name="handler"/> on the main thread once Steam finished <paramref name="call"/>. It gets the
        /// result struct, valid only during the handler, and whether the call failed.
        /// </summary>
        internal static void Await(ulong call, Action<IntPtr, bool> handler)
        {
            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<SteamCallResult>()) ClassInjector.RegisterTypeInIl2Cpp<SteamCallResult>();

            Il2CppCollections.Dictionary<ulong, Il2CppCollections.List<CallResult>> registry = CallbackDispatcher.m_registeredCallResults;

            if (!registry.ContainsKey(call)) registry[call] = new Il2CppCollections.List<CallResult>();

            registry[call].Add(new SteamCallResult { _handler = handler });
        }
    }
}
