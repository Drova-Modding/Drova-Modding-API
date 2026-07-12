using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.WorldEvents;
using NVorbis;
using UnityEngine;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Provides audio clips from the file system under the Audio folder
    /// </summary>
    public class FileAudioProvider : IAudioProvider
    {
        /// <summary>
        /// Path to the audio files
        /// </summary>
        const string AudioFolderName = "Audio";
        private readonly AreaNameSystem _areaNameSystem;
        

        /// <summary>
        /// Initializes a new instance of the FileAudioProvider class and caches the AreaNameSystem
        /// </summary>
        public FileAudioProvider()
        {
            _areaNameSystem = WorldEventSystemManager.Instance.AreaNameSystem;
        }

        /// <inheritdoc/>
        public Task<AudioClip> GetAudioClip(string dialogeName, string filePath, string locaKey, string actorName, int? choiceId)
        {
            if (!ConfigAccessor.TryGetConfigValue(ModdingUI.ModdingUI.EnableDialogueAudioOptionKey, out bool enabled) || !enabled)
            {
                return Task.FromResult<AudioClip>(null);
            }
            string path = GetAudioFilePath(dialogeName, filePath.Replace('/', '_'), locaKey, actorName, choiceId, _areaNameSystem.IsInCave());
            return LoadOggAudioAsync(path);
        }

        private static string GetAudioFilePath(string dialogeName, string filePath, string locaKey, string actorName, int? choiceId, bool isInCave)
        {
            if (isInCave)
            {
                var path = Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_{locaKey}_{filePath}_{actorName}{AudioManager.DEFAULT_CAVE_AUDIO_PREFIX}.ogg");
                if(File.Exists(path))
                {
                    return path;
                }
                path = Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_{locaKey}_{filePath}{AudioManager.DEFAULT_CAVE_AUDIO_PREFIX}.ogg");
                if (File.Exists(path))
                {
                    return path;
                }
            }
            if (choiceId.HasValue)
            {
                return Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_{locaKey}_{filePath}.ogg");
            }
            if (File.Exists(Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_{locaKey}_{filePath}_{actorName}.ogg")))
            {
                return Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_{locaKey}_{filePath}_{actorName}.ogg");
            }
            if(dialogeName.Contains("DT_WorldDialogue_Template") && File.Exists(Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_{filePath}_{locaKey}_{actorName}.ogg")))
            {
                return Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_{filePath}_{locaKey}_{actorName}.ogg");
            }
            return Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_{locaKey}_{filePath}.ogg");
        }

        private static async Task<AudioClip> LoadOggAudioAsync(string path)
        {
            // Only the NVorbis decoding (pure C#) may run on the thread pool. AudioClip.Create
            // and SetData are main-thread-only Unity APIs, calling them from a worker thread
            // fails natively with "Graphics device is null" in the IL2CPP player.
            (float[] samples, int channels, int sampleRate)? decoded = await Task.Run(() =>
            {
                if (!File.Exists(path))
                {
                    AudioLog.Msg($"Audio not found for {path.Split("/")[^1]}");
                    return ((float[], int, int)?)null;
                }
                using VorbisReader vorbis = new(path);
                int sampleRate = vorbis.SampleRate;
                int channels = vorbis.Channels;
                float[] samples = new float[vorbis.TotalSamples * channels];
                vorbis.ReadSamples(samples, 0, samples.Length);
                return (samples, channels, sampleRate);
            }).ConfigureAwait(false);

            if (decoded == null) return null;
            var (samples, channels, sampleRate) = decoded.Value;
            return await MainThreadDispatcher.RunOnMainThread(() =>
            {
                AudioClip audioClip = AudioClip.Create("Audio", samples.Length / channels, channels, sampleRate, false);
                audioClip.SetData(samples, 0);
                return audioClip;
            }).ConfigureAwait(false);
        }
    }
}
