using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning
{
    /// <summary>
    /// Watches the dialogue editor root object lifecycle and emits a close signal when it
    /// becomes inactive or is destroyed.
    /// </summary>
    /// <remarks>
    /// The close signal is raised once per enabled cycle to avoid duplicate handling when both
    /// disable and destroy callbacks occur.
    /// </remarks>
    [RegisterTypeInIl2Cpp]
    internal sealed class DialogueEditorLifecycleWatcher(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private bool _wasEnabled;
        private bool _closedRaised;

        /// <summary>
        /// Fired when the watched editor closes (disabled or destroyed) after having been enabled.
        /// </summary>
        [HideFromIl2Cpp]
        internal event Action? Closed;
        
        internal void OnEnable()
        {
            _wasEnabled = true;
            _closedRaised = false;
        }
        
        internal void OnDisable()
        {
            RaiseClosedIfNeeded();
        }
        
        internal void OnDestroy()
        {
            RaiseClosedIfNeeded();
        }

        [HideFromIl2Cpp]
        private void RaiseClosedIfNeeded()
        {
            if (!_wasEnabled || _closedRaised)
                return;

            _closedRaised = true;
            Closed?.Invoke();
        }
    }
}