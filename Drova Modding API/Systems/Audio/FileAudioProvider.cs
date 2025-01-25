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

        /// <inheritdoc/>
        public Task<AudioClip> GetAudioClip(string dialogeName, string globaPath, string locaKey, string actorName, int? choiceId)
        {
            StringBuilder sb = new();
            sb.Append("Loading audio file: ").Append(dialogeName).Append('_').Append(globaPath.Replace('/', '_')).Append('_').Append(locaKey).Append('_').Append(actorName);
            MelonLoader.MelonLogger.Msg(sb.ToString());
            string path = GetAudioFilePath(dialogeName, globaPath.Replace('/', '_'), locaKey, actorName, choiceId);
            return LoadOggAudioAsync(path);
        }

        private static string GetAudioFilePath(string dialogeName, string globaPath, string locaKey, string actorName, int? choiceId)
        {
            if (choiceId.HasValue)
            {
                return Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_choice_{locaKey}_{globaPath}.ogg");
            }
            if (File.Exists(Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_{locaKey}_{globaPath}_{actorName}.ogg")))
            {
                return Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_{locaKey}_{globaPath}_{actorName}.ogg");
            }
            else
            {
                return Path.Combine(Utils.SavePath, AudioFolderName, $"{dialogeName}_{locaKey}_{globaPath}.ogg");
            }
        }

        private static async Task<AudioClip> LoadOggAudioAsync(string path)
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                using var vorbis = new VorbisReader(path);
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
