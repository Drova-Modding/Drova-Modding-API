using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Provides audio clips from the file system
    /// </summary>
    public class FileAudioProvider : MonoBehaviour, IAudioProvider
    {
        /// <inheritdoc/>
        public Task<AudioClip> GetAudioClip(string dialogeName, string uuid, string actorName, int? choiceId)
        {
            string path = GetAudioFilePath(dialogeName, uuid, actorName, choiceId);
            return LoadOggAudioSync(path);
        }

        private static string GetAudioFilePath(string dialogeName, string uuid, string actorName, int? choiceId)
        {
            if (choiceId.HasValue)
            {
                return Path.Combine(Utils.SavePath, $"Audio/{dialogeName}_choice_{uuid}.ogg");
            }
            if (File.Exists(Path.Combine(Utils.SavePath, $"Audio/{dialogeName}_{uuid}_{actorName}.ogg")))
            {
                return Path.Combine(Utils.SavePath, $"Audio/{dialogeName}_{uuid}_{actorName}.ogg");
            }
            else
            {
                return Path.Combine(Utils.SavePath, $"Audio/{dialogeName}_{uuid}.ogg");
            }
        }

        private static async Task<AudioClip> LoadOggAudioSync(string path)
        {
            TaskCompletionSource<AudioClip> tcs = new();
            MelonCoroutines.Start(LoadOggAudio(path, tcs));
            return await tcs.Task;
        }

        private static IEnumerator LoadOggAudio(string path, TaskCompletionSource<AudioClip> tcs)
        {
            // 'file://' prefix is required to point UnityWebRequest to a local file.
            string uri = "file://" + path;

            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS);
            yield return www.SendWebRequest();

            // Check for an error
            if (www.result == UnityWebRequest.Result.ConnectionError
                || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error loading audio file from disk: {www.error}");
                tcs.SetException(new System.Exception(www.error));
            }
            else
            {
                // Get the decoded AudioClip
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                tcs.SetResult(clip);
            }
            www.Dispose();
        }
    }
}
