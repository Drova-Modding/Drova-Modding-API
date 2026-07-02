using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.WorldEvents;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Provides dialogue audio clips from per-actor AssetBundles instead of loose .ogg files.
    /// Bundles live under &lt;SavePath&gt;/Audio/bundles/ and are named after the lower-cased actor
    /// (e.g. "jendrik"). Each bundle holds the AudioClips for that actor, addressed by the same key the
    /// dialogue system builds for the file-based provider.
    ///
    /// To use it instead of <see cref="FileAudioProvider"/>, register it once during mod init:
    /// <code>
    /// AudioManager.ReplaceDialogueAudioConnector(
    ///     new DefaultDialogueAudioConnector(new AssetBundleAudioProvider()));
    /// </code>
    /// </summary>
    public class AssetBundleAudioProvider : IAudioProvider
    {                   
        const string AudioFolderName = "Audio";
        const string BundleSubFolder = "bundles";

        // Audio extensions a clip may have been imported from; stripped from the bundle's asset names
        // so the membership set can be compared against the extension-less clip keys.
        private static readonly string[] AudioExtensions = [".ogg", ".wav", ".mp3", ".aiff", ".aif"];

        private AreaNameSystem _areaNameSystem;
        // Cached, lazily loaded bundles keyed by lower-cased actor name. AssetBundle.LoadFromFile must
        // not be called twice for the same file, so caching is required, not just an optimization.
        private readonly Dictionary<string, ActorBundle> _bundleCache = [];

        /// <summary>
        /// A loaded actor bundle together with the cheap lookups that keep dialogue loading off the
        /// main-thread hot path: a set of the contained clip names (so misses cost a hash lookup
        /// instead of an Il2Cpp <c>LoadAsset</c> round-trip) and a cache of already-decoded clips.
        /// </summary>
        private sealed class ActorBundle(AssetBundle bundle, HashSet<string> assetNames)
        {
            public readonly AssetBundle Bundle = bundle;
            // Lower-cased, extension-less names of every asset in the bundle.
            public readonly HashSet<string> AssetNames = assetNames;
            // Resolved clips keyed by the lower-cased candidate key that produced them.
            public readonly Dictionary<string, AudioClip> ClipCache = [];
        }

        /// <summary>
        /// Initializes a new instance of the AssetBundleAudioProvider class and caches the AreaNameSystem.
        /// </summary>
        public AssetBundleAudioProvider()
        {
            _areaNameSystem = WorldEventSystemManager.Instance!.AreaNameSystem;
            GetActorBundle("player"); // Preload player bundle, as it's used for most generic lines and thus more likely to be needed
        }

        /// <inheritdoc/>
        public Task<AudioClip> GetAudioClip(string dialogeName, string filePath, string locaKey, string actorName, int? choiceId)
        {
            if (!ConfigAccessor.TryGetConfigValue(ModdingUI.ModdingUI.EnableDialogueAudioOptionKey, out bool enabled) || !enabled)
            {
                return Task.FromResult<AudioClip>(null);
            }

            var actorBundle = GetActorBundle(actorName);
            if (actorBundle == null)
            {
                AudioLog.Msg($"No audio bundle for actor '{actorName}'");
                return Task.FromResult<AudioClip>(null);
            }

            string normalizedPath = filePath.Replace('/', '_');
            if(!_areaNameSystem)
            {
                _areaNameSystem = WorldEventSystemManager.Instance!.AreaNameSystem;
            }
            var isInCave = _areaNameSystem.IsInCave();
            foreach (var key in GetCandidateKeys(dialogeName, normalizedPath, locaKey, actorName, choiceId, isInCave))
            {
                // AssetBundle assets are addressed by their (case-insensitive) name without extension,
                // which equals the clip key the file-based provider uses minus the ".ogg".
                string lowerKey = key.ToLowerInvariant();

                // Most candidate keys miss. Skip the ones the bundle doesn't contain so we never pay
                // for an Il2Cpp LoadAsset round-trip on a miss — this is the bulk of the per-node cost
                // when a dialogue tree is loaded.
                if (!actorBundle.AssetNames.Contains(lowerKey)) continue;

                // Reuse an already-decoded clip so a re-loaded tree (or a repeated line) doesn't
                // decompress the same audio on the main thread again.
                if (actorBundle.ClipCache.TryGetValue(lowerKey, out var cachedClip))
                {
                    return Task.FromResult(cachedClip);
                }

                // Use the non-generic LoadAsset(name, type) overload rather than LoadAsset<AudioClip>:
                // Il2CppInterop's generic version Cast<T>()s the result internally, and a missing key
                // returns a null pointer, so the Cast throws a NullReferenceException in get_Pointer()
                // instead of returning null. The non-generic overload returns null cleanly.
                var asset = actorBundle.Bundle.LoadAsset(key, Il2CppType.Of<AudioClip>());
                var clip = asset?.TryCast<AudioClip>();
                if (clip != null)
                {
                    actorBundle.ClipCache[lowerKey] = clip;
                    return Task.FromResult(clip);
                }
            }

            AudioLog.Msg($"Audio not found in bundle for {dialogeName}_{locaKey}_{normalizedPath}_{actorName}");
            return Task.FromResult<AudioClip>(null);
        }

        /// <summary>
        /// Returns the cached bundle for the actor, loading it from disk on first use. Returns null
        /// (without caching) when no bundle file exists, so a bundle added later in the session is
        /// still picked up.
        /// </summary>
        private ActorBundle? GetActorBundle(string actorName)
        {
            string key = actorName.ToLowerInvariant();
            if (_bundleCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            string path = Path.Combine(Utils.SavePath, AudioFolderName, BundleSubFolder, key);
            if (!File.Exists(path))
            {
                return null;
            }
            var bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                return null;
            }
            var actorBundle = new ActorBundle(bundle, BuildAssetNameSet(bundle));
            _bundleCache[key] = actorBundle;
            return actorBundle;
        }

        /// <summary>
        /// Builds the set of contained clip names (lower-cased, without directory or audio extension)
        /// so candidate keys can be membership-tested cheaply. <see cref="AssetBundle.GetAllAssetNames"/>
        /// returns lower-cased asset paths such as <c>assets/audio/&lt;key&gt;.ogg</c>; the file base name
        /// equals the clip key the dialogue system builds.
        /// </summary>
        private static HashSet<string> BuildAssetNameSet(AssetBundle bundle)
        {
            HashSet<string> names = [];
            foreach (var assetPath in bundle.GetAllAssetNames())
            {
                int slash = assetPath.LastIndexOf('/');
                string fileName = slash >= 0 ? assetPath[(slash + 1)..] : assetPath;
                names.Add(StripAudioExtension(fileName.ToLowerInvariant()));
            }
            return names;
        }

        private static string StripAudioExtension(string fileName)
        {
            foreach (var ext in AudioExtensions)
            {
                if (fileName.EndsWith(ext, StringComparison.Ordinal))
                {
                    return fileName[..^ext.Length];
                }
            }
            return fileName;
        }

        /// <summary>
        /// Yields the candidate clip keys to try, in the same priority order as the file-based
        /// provider's <c>GetAudioFilePath</c> (cave variants first, then choice/actor variants).
        /// </summary>
        private static IEnumerable<string> GetCandidateKeys(string dialogeName, string filePath, string locaKey, string actorName, int? choiceId, bool isInCave)
        {
            if (isInCave)
            {
                yield return $"{dialogeName}_{locaKey}_{filePath}_{actorName}{AudioManager.DEFAULT_CAVE_AUDIO_PREFIX}";
                yield return $"{dialogeName}_{locaKey}_{filePath}{AudioManager.DEFAULT_CAVE_AUDIO_PREFIX}";
            }

            if (choiceId.HasValue)
            {
                yield return $"{dialogeName}_{locaKey}_{filePath}";
            }

            yield return $"{dialogeName}_{locaKey}_{filePath}_{actorName}";

            if (dialogeName.Contains("DT_WorldDialogue_Template"))
            {
                yield return $"{dialogeName}_{filePath}_{locaKey}_{actorName}";
            }

            yield return $"{dialogeName}_{locaKey}_{filePath}";
        }

        /// <summary>
        /// Unloads all cached bundles. Call when switching providers or shutting down the mod.
        /// </summary>
        public void UnloadAll()
        {
            foreach (var actorBundle in _bundleCache.Values)
            {
                if (actorBundle?.Bundle != null)
                {
                    actorBundle.Bundle.Unload(true);
                }
            }
            _bundleCache.Clear();
        }
    }
}
