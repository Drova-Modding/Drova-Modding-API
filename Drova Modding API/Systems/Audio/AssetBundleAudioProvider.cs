using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Audio.Dialogue;
using Drova_Modding_API.Systems.WorldEvents;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// The default <see cref="AudioManager"/> provider: dialogue clips from per-actor AssetBundles under
    /// &lt;SavePath&gt;/Audio/bundles/, named after the lower-cased actor, with narration in a "narrator" bundle.
    /// </summary>
    public class AssetBundleAudioProvider : IAudioProvider
    {                   
        const string AudioFolderName = "Audio";
        const string BundleSubFolder = "bundles";
        const string PlayerBundle = "player";

        private static readonly string[] AudioExtensions = [".ogg", ".wav", ".mp3", ".aiff", ".aif"];

        private AreaNameSystem? _areaNameSystem;
        // AssetBundle.LoadFromFile must not be called twice for the same file, so this cache is required.
        private readonly Dictionary<string, ActorBundle> _bundleCache = [];

        // Never retried, since every line asks for its bundle and each failed load logs an engine error.
        private readonly HashSet<string> _unreadable = [];

        // Clip key to bundle, from the .manifest files. The game renames NPCs, so the actor name does not always name the bundle.
        private readonly Dictionary<string, string> _clipBundles = [];

        /// <summary>
        /// A loaded bundle with its lower-cased clip names, so misses skip <c>LoadAsset</c>, and its decoded clips.
        /// </summary>
        private sealed class ActorBundle(AssetBundle bundle, HashSet<string> assetNames)
        {
            public readonly AssetBundle Bundle = bundle;
            public readonly HashSet<string> AssetNames = assetNames;
            public readonly Dictionary<string, AudioClip> ClipCache = [];
        }

        /// <summary>
        /// Indexes the bundle manifests and preloads the player bundle.
        /// </summary>
        public AssetBundleAudioProvider()
        {
            IndexManifests();
            GetActorBundle("player");
        }

        /// <summary>
        /// Fills <see cref="_clipBundles"/> from the bundle manifests. Without them clips are found by actor name only.
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
        /// Returns the first candidate clip found through the manifest index, or null.
        /// The player bundle answers only for the player, since comment trees use a "Player" slot any actor can fill.
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
            if (!_areaNameSystem && WorldEventSystemManager.Instance != null)
            {
                _areaNameSystem = WorldEventSystemManager.Instance.AreaNameSystem;
            }
            bool isInCave = _areaNameSystem != null && _areaNameSystem.IsInCave();
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
        /// Finds a clip by its name in the actor's and narrator's bundles, then in every loaded bundle.
        /// For callers that know the clip but not the dialogue line, such as a co-op teammate's conversation.
        /// </summary>
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
        /// Yields the actors whose bundles may hold the line, most likely first.
        /// The narrator is always included, since the actor alone does not tell which voice recorded a line.
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
                string lowerKey = key.ToLowerInvariant();
                if (!actorBundle.AssetNames.Contains(lowerKey)) continue;

                // UnloadUnusedAssets on scene changes destroys cached clips, so a destroyed one is loaded again.
                if (actorBundle.ClipCache.TryGetValue(lowerKey, out var cachedClip) && cachedClip != null)
                {
                    return cachedClip;
                }

                // The generic LoadAsset<T> throws instead of returning null for a missing key.
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
        /// Returns the cached bundle for the actor, loading it on first use.
        /// A missing file is not cached, so a bundle added later in the session is still picked up.
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
        /// Hints at a text-mode copy when the file is longer than the size its UnityFS header records.
        /// </summary>
        private static string DamageHint(string path)
        {
            try
            {
                using FileStream file = File.OpenRead(path);
                byte[] head = new byte[128];
                int read = file.Read(head, 0, head.Length);
                // Skips the signature, format version and two engine version strings to reach the file size.
                int at = Array.IndexOf(head, (byte)0) + 1 + 4;
                for (int text = 0; text < 2; text++) at = Array.IndexOf(head, (byte)0, at) + 1;
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
        /// Builds the set of clip names in the bundle, lower-cased and without directory or extension.
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
        /// Yields the candidate clip keys, most specific first.
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
