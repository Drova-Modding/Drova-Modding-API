using Drova_Modding_API.Access;
using ModdingUIOptions = Drova_Modding_API.Systems.ModdingUI.ModdingUI;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Continuously adjusts the volume of registered dialogue <see cref="AudioSource"/>s
    /// based on the 2D distance between the speaking actor and the player.
    /// Sources are automatically unregistered once they stop playing.
    /// </summary>
    /// <param name="ptr">Do not instantiate manually.</param>
    [RegisterTypeInIl2Cpp]
    public class DialogueAudioDistanceManager(IntPtr ptr) : MonoBehaviour(ptr)
    {
        /// <summary>
        /// Grace time in frames for the playback to start
        /// </summary>
        private const int START_PLAYBACK_GRACE_FRAMES = 30;
        private const float DEFAULT_MIN_DISTANCE = 100f;
        private const float DEFAULT_MAX_DISTANCE = 425f;
        private const float DEFAULT_VOLUME_LERP_SPEED = 5f;
        private const float VOLUME_SNAP_EPSILON = 0.0001f;

        private sealed class RegisteredDialogueSource(AudioSource source, Transform actorTransform)
        {
            public AudioSource Source { get; } = source;
            public Transform ActorTransform { get; } = actorTransform;
            public bool HasStartedPlaying { get; set; }
            public int FramesSinceRegistration { get; set; }
        }

        /// <summary>Full volume up to this distance (world units).</summary>
        public float MinDistance = DEFAULT_MIN_DISTANCE;
        /// <summary> The speed </summary>
        public float VolumeLerpSpeed  = DEFAULT_VOLUME_LERP_SPEED;

        /// <summary>Fully silent at this distance (world units) and beyond.</summary>
        public float MaxDistance = DEFAULT_MAX_DISTANCE;

        private static DialogueAudioDistanceManager? _instance;

        /// <summary>The singleton instance of the manager.</summary>
        public static DialogueAudioDistanceManager? Instance => _instance;

        // Each entry pairs an AudioSource with the Transform of the speaking actor.
        private readonly List<RegisteredDialogueSource> _registeredSources = [];

        internal void Awake()
        {
            AudioLog.Msg("Initializing DialogueAudioDistanceManager");
            if (_instance != null)
            {
                AudioLog.Warning("DialogueAudioDistanceManager already exists, destroying duplicate.");
                Destroy(this);
                return;
            }
            _instance = this;
            ApplyConfiguredSettings();
        }

        private void ApplyConfiguredSettings()
        {
            MinDistance = GetConfiguredFloat(ModdingUIOptions.DialogueAudioMinDistanceOptionKey, MinDistance);
            MaxDistance = GetConfiguredFloat(ModdingUIOptions.DialogueAudioMaxDistanceOptionKey, MaxDistance);
            VolumeLerpSpeed = GetConfiguredFloat(ModdingUIOptions.DialogueAudioVolumeLerpSpeedOptionKey, VolumeLerpSpeed);

            MinDistance = Mathf.Max(0f, MinDistance);
            MaxDistance = Mathf.Max(MinDistance + 1f, MaxDistance);
            VolumeLerpSpeed = Mathf.Max(0.01f, VolumeLerpSpeed);
        }

        private static float GetConfiguredFloat(string key, float fallback)
        {
            if (ConfigAccessor.TryGetConfigValue(key, out int configuredInt))
            {
                return configuredInt;
            }

            if (ConfigAccessor.TryGetConfigValue(key, out float configuredFloat))
            {
                return configuredFloat;
            }

            return fallback;
        }

        /// <summary>
        /// Register an <see cref="AudioSource"/> to have its volume adjusted every frame.
        /// If the same source is already registered, it will be re-registered with the new transform.
        /// </summary>
        [HideFromIl2Cpp]
        public void Register(AudioSource source, Transform actorTransform)
        {
            if (source == null || actorTransform == null) return;
            AudioLog.Msg($"Registering dialogue audio source for actor {actorTransform.name}");
            Unregister(source);
            _registeredSources.Add(new RegisteredDialogueSource(source, actorTransform));
        }

        /// <summary>
        /// Manually unregister an <see cref="AudioSource"/>. Not required – sources are
        /// removed automatically when they stop playing.
        /// </summary>
        [HideFromIl2Cpp]
        public void Unregister(AudioSource source)
        {
            for (int i = _registeredSources.Count - 1; i >= 0; i--)
            {
                if (_registeredSources[i].Source == source)
                {
                    _registeredSources.RemoveAt(i);
                }
            }
        }

        internal void Update()
        {
            if (_registeredSources.Count == 0) return;
            ApplyConfiguredSettings();
            if (!PlayerAccess.TryGetPlayer(out Il2CppDrova.Actor player)) return;
            Vector2 playerPos = player.transform.position;

            for (int i = _registeredSources.Count - 1; i >= 0; i--)
            {
                RegisteredDialogueSource entry = _registeredSources[i];
                AudioSource source = entry.Source;
                Transform actorTransform = entry.ActorTransform;

                if (source == null || actorTransform == null)
                {
                    List<string> reasons = [];
                    if (source == null)
                    {
                        reasons.Add("AudioSource is null");
                    }
                    if (actorTransform == null)
                    {
                        reasons.Add("actor Transform is null");
                    }
                    AudioLog.Msg($"Unregistering dialogue audio source for actor {actorTransform?.name ?? "<unknown>"}: {string.Join(", ", reasons)}");
                    _registeredSources.RemoveAt(i);
                    continue;
                }

                if (source.isPlaying)
                {
                    if (!entry.HasStartedPlaying)
                    {
                        AudioLog.Msg($"Dialogue audio source for actor {actorTransform.name} started playing after {entry.FramesSinceRegistration} update(s)");
                    }

                    entry.HasStartedPlaying = true;
                }
                else if (!entry.HasStartedPlaying)
                {
                    entry.FramesSinceRegistration++;
                    if (entry.FramesSinceRegistration <= START_PLAYBACK_GRACE_FRAMES)
                    {
                        continue;
                    }

                    AudioLog.Msg($"Unregistering dialogue audio source for actor {actorTransform.name}: AudioSource never started playing within {START_PLAYBACK_GRACE_FRAMES} update(s)");
                    _registeredSources.RemoveAt(i);
                    continue;
                }
                else
                {
                    AudioLog.Msg($"Unregistering dialogue audio source for actor {actorTransform.name}: AudioSource stopped playing");
                    _registeredSources.RemoveAt(i);
                    continue;
                }

                Vector2 actorPos = actorTransform.position;
                float distance = Vector2.Distance(actorPos, playerPos);
                float normalized = Mathf.InverseLerp(MinDistance, MaxDistance, distance);
                float targetVolume = Mathf.Clamp01(1f - (normalized * normalized));

                // Snap very low values to zero to avoid endless tiny exponential decay updates.
                if (targetVolume <= VOLUME_SNAP_EPSILON)
                {
                    source.volume = 0f;
                    continue;
                }

                float lerpFactor = Mathf.Clamp01(Time.deltaTime * VolumeLerpSpeed);
                float volume = Mathf.Lerp(source.volume, targetVolume, lerpFactor);
                source.volume = Mathf.Abs(volume - targetVolume) <= VOLUME_SNAP_EPSILON ? targetVolume : volume;
            }
        }
    }
}


