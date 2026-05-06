using Drova_Modding_API.Systems.WorldEvents;
using NVorbis;
using System.Text;
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
            _areaNameSystem = WorldEventSystemManager.Instance.areaNameSystem;
        }

        /// <inheritdoc/>
        public Task<AudioClip> GetAudioClip(string dialogeName, string filePath, string locaKey, string actorName, int? choiceId)
        {
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
            return await Task.Run(() =>
            {
                if (!File.Exists(path))
                {
                    MelonLoader.MelonLogger.Msg($"Audio not found for {path.Split("/")[^1]}");
                    return null;
                }
                using VorbisReader vorbis = new(path);
                int sampleRate = vorbis.SampleRate;
                int channels = vorbis.Channels;
                float[] samples = new float[vorbis.TotalSamples * channels];
                vorbis.ReadSamples(samples, 0, samples.Length);

                AudioClip audioClip = AudioClip.Create("Audio", samples.Length / channels, channels, sampleRate, false);
                audioClip.SetData(samples, 0);
                return audioClip;
            });
        }
    }
}
