using Il2CppDrova.Audio;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Provides audio clips from the file system
    /// </summary>
    public class FileAudioProvider : IAudioProvider
    {
        /// <inheritdoc/>
        public AudioClip GetAudioClip(string dialogeName, string uuid, string actorName, int? choiceId)
        {
            throw new NotImplementedException();
        }

        private IEnumerator LoadOggAudio(string path)
        {
            // 'file://' prefix is required to point UnityWebRequest to a local file.
            string uri = "file://" + path;

            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS);
            {
                yield return www.SendWebRequest();

                // Check for an error
                if (www.result == UnityWebRequest.Result.ConnectionError
                    || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error loading audio file from disk: {www.error}");
                }
                else
                {
                    // Get the decoded AudioClip
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                }
            }
            www.Dispose();
        }
    }
}
