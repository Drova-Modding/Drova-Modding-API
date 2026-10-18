using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Audio.Dialogue;
using Drova_Modding_API.Systems.WorldEvents;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Provides dialogue audio clips from per-actor AssetBundles. Bundles live under
    /// &lt;SavePath&gt;/Audio/bundles/ and are named after the lower-cased actor (e.g. "jendrik").
    /// Each bundle holds the AudioClips for that actor, addressed by the key the dialogue system
    /// builds for a line, without a file extension. Narration lives in a "narrator" bundle, which is
    /// probed alongside the actor's own bundle for every line (see <see cref="NarratorActors"/>).
    ///
    /// This is the provider <see cref="AudioManager"/> starts with, so it needs no registration.
    /// Re-register it explicitly only to get back to it after swapping in another provider:
    /// <code>
    /// AudioManager.ReplaceDialogueAudioConnector(
    ///     new DefaultDialogueAudioConnector(new AssetBundleAudioProvider()));
    /// </code>
    /// </summary>
    public class AssetBundleAudioProvider : IAudioProvider
    {                   
        const string AudioFolderName = "Audio";
        const string BundleSubFolder = "bundles";
        const string PlayerBundle = "player";

        // Audio extensions a clip may have been imported from; stripped from the bundle's asset names
        // so the membership set can be compared against the extension-less clip keys.
        private static readonly string[] AudioExtensions = [".ogg", ".wav", ".mp3", ".aiff", ".aif"];

        private AreaNameSystem _areaNameSystem;
        // Cached, lazily loaded bundles keyed by lower-cased actor name. AssetBundle.LoadFromFile must
        // not be called twice for the same file, so caching is required, not just an optimization.
        private readonly Dictionary<string, ActorBundle> _bundleCache = [];

        // Bundle files Unity could not read. Not tried again: a failed LoadFromFile logs an engine error, and every line
        // of a conversation asks for its speaker's bundle - one unreadable file was hundreds of errors per talk.
        private readonly HashSet<string> _unreadable = [];

        // Lower-cased clip key -> bundle that holds it, read from the bundles' .manifest files. Bundles are grouped by voice,
        // which the actor name does not always tell: the game renames NPCs (Venia speaks lines exported under "Idefix").
        private readonly Dictionary<string, string> _clipBundles = [];

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
        /// Initialises a new instance of the AssetBundleAudioProvider class and caches the AreaNameSystem.
        /// </summary>
        public AssetBundleAudioProvider()
        {
            _areaNameSystem = WorldEventSystemManager.Instance!.AreaNameSystem;
            IndexManifests();
            GetActorBundle("player"); // Preload player bundle, as it's used for most generic lines and thus more likely to be needed
        }

        /// <summary>
        /// Fills <see cref="_clipBundles"/> from the text manifests Unity writes next to each bundle, which list its assets as
        /// <c>- Assets/.../&lt;key&gt;.wav</c>. Without manifests the index stays empty and clips are found by actor name only.
        /// </summary>
        private void IndexManifests()
        {
            string folder = Path.Combine(Utils.SavePath, AudioFolderName, BundleSubFolder);
            if (!Directory.Exists(folder)) return;

            foreach (string manifest in Directory.EnumerateFiles(folder, "*.manifest"))
            {
                string bundleName = Path.GetFileNameWithoutExtension(manifest).ToLowerInvariant();
                foreach (string line in File.ReadLines(manifest))
                {
                    if (!line.StartsWith("- Assets/", StringComparison.Ordinal)) continue;
                    string key = StripAudioExtension(Path.GetFileName(line).ToLowerInvariant());
                    _clipBundles.TryAdd(key, bundleName);
                }
            }
            AudioLog.Msg($"Indexed {_clipBundles.Count} dialogue clips from bundle manifests");
        }

        /// <summary>
        /// Returns the first candidate key whose bundle, as named by the manifest index, resolves it to a clip, or null.
        /// The player's bundle answers for the player only: comment trees such as <c>DT_Comment_DrinkWater</c> are
        /// authored for a "Player" slot that any actor fills at runtime, and an NPC must not borrow the player's voice.
        /// </summary>
        private AudioClip? TryIndexedClip(List<string> candidateKeys, string actorName)
        {
            bool isPlayer = string.Equals(actorName, PlayerBundle, StringComparison.OrdinalIgnoreCase);
            for (int i = 0; i < candidateKeys.Count; i++)
            {
                if (!_clipBundles.TryGetValue(candidateKeys[i].ToLowerInvariant(), out string bundleName)) continue;
                if (!isPlayer && bundleName == PlayerBundle) continue;
                ActorBundle? bundle = GetActorBundle(bundleName);
                AudioClip? clip = bundle == null ? null : TryLoadClip(bundle, [candidateKeys[i]]);
                if (clip != null) return clip;
            }
            return null;
        }

        /// <inheritdoc/>
        public Task<AudioClip> GetAudioClip(string dialogeName, string filePath, string locaKey, string actorName, int? choiceId)
        {
            if (!ConfigAccessor.TryGetConfigValue(ModdingUI.ModdingUI.EnableDialogueAudioOptionKey, out bool enabled) || !enabled)
            {
                return Task.FromResult<AudioClip>(null);
            }

            string normalizedPath = filePath.Replace('/', '_');
            if(!_areaNameSystem)
            {
                _areaNameSystem = WorldEventSystemManager.Instance!.AreaNameSystem;
            }
            var isInCave = _areaNameSystem.IsInCave();
            List<string> candidateKeys = [.. GetCandidateKeys(dialogeName, normalizedPath, locaKey, actorName, choiceId, isInCave)];

            AudioClip? indexed = TryIndexedClip(candidateKeys, actorName);
            if (indexed != null)
            {
                return Task.FromResult(indexed);
            }

            List<string> probedActors = [.. GetBundleActors(actorName)];
            bool anyBundle = false;
            for (int i = 0; i < probedActors.Count; i++)
            {
                ActorBundle? actorBundle = GetActorBundle(probedActors[i]);
                if (actorBundle == null) continue;

                anyBundle = true;
                AudioClip clip = TryLoadClip(actorBundle, candidateKeys);
                if (clip != null)
                {
                    return Task.FromResult(clip);
                }
            }

            string probed = string.Join("', '", probedActors);
            if (!anyBundle)
            {
                AudioLog.Msg($"No audio bundle for actor '{actorName}', looked for bundles '{probed}'");
                return Task.FromResult<AudioClip>(null);
            }

            AudioLog.Msg($"Audio not found for {dialogeName}_{locaKey}_{normalizedPath}_{actorName} in bundles '{probed}'");
            return Task.FromResult<AudioClip>(null);
        }

        /// <summary>
        /// A clip by its own name - the key it was found under, which a clip loaded from a bundle carries as its name - looked
        /// for in the actor's bundle and the narrator's, then in every bundle already loaded. For a machine that did not run
        /// the line itself (a co-op teammate's conversation): it knows which clip was played, not the dialogue it came from.
        /// </summary>
        /// <param name="actorName">The speaking actor, whose bundle is tried first.</param>
        /// <param name="clipName">The clip's name.</param>
        /// <returns>The clip, or null when no bundle has it or dialogue audio is switched off.</returns>
        public AudioClip? GetClipByName(string actorName, string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return null;
            if (!ConfigAccessor.TryGetConfigValue(ModdingUI.ModdingUI.EnableDialogueAudioOptionKey, out bool enabled) || !enabled) return null;

            List<string> key = [clipName];
            AudioClip? indexed = TryIndexedClip(key, actorName);
            if (indexed != null) return indexed;

            foreach (string actor in GetBundleActors(actorName))
            {
                ActorBundle? bundle = GetActorBundle(actor);
                AudioClip? clip = bundle == null ? null : TryLoadClip(bundle, key);
                if (clip != null) return clip;
            }

            foreach (ActorBundle bundle in _bundleCache.Values)
            {
                AudioClip? clip = TryLoadClip(bundle, key);
                if (clip != null) return clip;
            }

            return null;
        }

        /// <summary>
        /// Yields the actors whose bundles may hold the line, most likely first. The narrator bundle
        /// is always probed as well: narration is authored under the prop name that happens to hold
        /// the tree, and a normally voiced actor also delivers narrated lines, so which of the two
        /// voices recorded a given line cannot be decided from the actor alone. Clip keys are built
        /// from the node's own actor either way, so probing the second bundle cannot cross-match.
        /// </summary>
        private static IEnumerable<string> GetBundleActors(string actorName)
        {
            if (NarratorActors.IsNarratorActor(actorName))
            {
                yield return NarratorActors.NARRATOR;
                yield return actorName;
                yield break;
            }

            yield return actorName;
            yield return NarratorActors.NARRATOR;
        }

        /// <summary>
        /// Returns the first candidate key the bundle actually resolves to a clip, or null.
        /// </summary>
        private static AudioClip? TryLoadClip(ActorBundle actorBundle, List<string> candidateKeys)
        {
            for (int i = 0; i < candidateKeys.Count; i++)
            {
                string key = candidateKeys[i];

                // AssetBundle assets are addressed by their (case-insensitive) name without extension,
                // which is exactly the clip key the dialogue system builds.
                string lowerKey = key.ToLowerInvariant();

                // Most candidate keys miss. Skip the ones the bundle doesn't contain so we never pay
                // for an Il2Cpp LoadAsset round-trip on a miss — this is the bulk of the per-node cost
                // when a dialogue tree is loaded.
                if (!actorBundle.AssetNames.Contains(lowerKey)) continue;

                // Reuse an already-decoded clip so a re-loaded tree (or a repeated line) doesn't
                // decompress the same audio on the main thread again. Unity unloads a clip nothing references
                // any more (Resources.UnloadUnusedAssets on scene changes); a destroyed one compares equal to null
                // and is loaded again from the bundle, which stays loaded.
                if (actorBundle.ClipCache.TryGetValue(lowerKey, out var cachedClip) && cachedClip != null)
                {
                    return cachedClip;
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
                    return clip;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the cached bundle for the actor, loading it from disk on first use. Returns null
        /// (without caching) when no bundle file exists, so a bundle added later in the session is
        /// still picked up.
        /// </summary>
        private ActorBundle? GetActorBundle(string actorName)
        {
            if (string.IsNullOrEmpty(actorName)) return null;

            string key = actorName.ToLowerInvariant();
            if (_bundleCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            string path = Path.Combine(Utils.SavePath, AudioFolderName, BundleSubFolder, key);
            if (_unreadable.Contains(key) || !File.Exists(path))
            {
                return null;
            }
            var bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                _unreadable.Add(key);
                AudioLog.Error($"Unity could not read the audio bundle '{path}'; not tried again this session." + DamageHint(path));
                return null;
            }
            var actorBundle = new ActorBundle(bundle, BuildAssetNameSet(bundle));
            _bundleCache[key] = actorBundle;
            return actorBundle;
        }

        /// <summary>
        /// Why a bundle file may be unreadable, when it can be told: a UnityFS header records the file's whole size, and a
        /// file longer than that went through a text-mode transfer (FTP in ASCII mode turns every line feed into two bytes).
        /// </summary>
        private static string DamageHint(string path)
        {
            try
            {
                using FileStream file = File.OpenRead(path);
                byte[] head = new byte[128];
                int read = file.Read(head, 0, head.Length);
                int at = Array.IndexOf(head, (byte)0) + 1 + 4;                 // "UnityFS ", format version
                for (int text = 0; text < 2; text++) at = Array.IndexOf(head, (byte)0, at) + 1; // engine versions
                if (at <= 0 || at + 8 > read) return string.Empty;
                long declared = 0;
                for (int index = 0; index < 8; index++) declared = (declared << 8) | head[at + index];
                return file.Length == declared ? string.Empty
                    : $" It is {file.Length} bytes where its header says {declared}: copied as text (an FTP transfer in ASCII mode?). Copy it again in binary mode.";
            }
            catch (IOException)
            {
                return string.Empty;
            }
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
        /// Yields the candidate clip keys to try, most specific first: cave variants, then the
        /// choice/actor variants, then the bare tree-and-line key.
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
