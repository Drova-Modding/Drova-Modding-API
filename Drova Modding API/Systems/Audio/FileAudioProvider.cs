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
        readonly Dictionary<string, CachedAudio> actorToCachedAudio = [];

        /// <inheritdoc/>
        public Task<AudioClip> GetAudioClip(string dialogeName, string filePath, string locaKey, string actorName, int? choiceId)
        {
            string path = GetAudioFilePath(dialogeName, filePath.Replace('/', '_'), locaKey, actorName, choiceId);   
            return LoadOggAudioAsync(path);
        }

        private static string GetAudioFilePath(string dialogeName, string filePath, string locaKey, string actorName, int? choiceId)
        {
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

        private class CachedAudio
        {
            internal AudioClip AudioClip;
            internal bool IsLoaded;
        }
    }
}
