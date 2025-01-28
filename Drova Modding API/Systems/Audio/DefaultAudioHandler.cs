using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
using Il2CppNodeCanvas.DialogueTrees.UI.Examples;
using Il2CppTMPro;
using MelonLoader;
using System.Collections;
using UnityEngine;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Default audio handler, used when no other audio handler is provided.
    /// Handles subtitles by setting the text of the actor speech to the text of the statement
    /// </summary>
    internal class DefaultAudioHandler : IAudioHandler
    {
        public void HandleMultipleChoiceRequest(MultipleChoiceRequestInfo info, DS_DialogueUGUI dialogueUGUI)
        {
            //info.
        }

        public void HandleSubtitleRequest(SubtitlesRequestInfo info, DS_DialogueUGUI dialogueUGUI)
        {
            //MelonLogger.Msg($"Handling subtitle request for: {info.actor.name}");
            MelonCoroutines.Start(WaitForWindowAndSetup(info, dialogueUGUI));
        }

        /// <summary>
        /// Sets the text of the actor speech to the text of the statement when the window is ready
        /// </summary>
        /// <param name="info">The info to set</param>
        /// <param name="dialogueUGUI">The instance of the dialouge GUI</param>
        /// <returns></returns>
        private static IEnumerator WaitForWindowAndSetup(SubtitlesRequestInfo info, DS_DialogueUGUI dialogueUGUI)
        {
            var text = info.statement.text;
            var currentWindow = dialogueUGUI._windowMgr.GetCurrentWindow();
            while (currentWindow == null)
            {
                yield return null;
                currentWindow = dialogueUGUI._windowMgr.GetCurrentWindow();
            }
            var actorSpeech = currentWindow.SubtitlesGroup.ActorSpeech;
            actorSpeech.text = dialogueUGUI._textHandler.GetTextWithoutOptionTags(text);
            actorSpeech.maxVisibleCharacters = 0;
            actorSpeech.ForceMeshUpdate(false, false);
            float audioLength = info.statement.audio.length;
            MelonCoroutines.Start(TypeText(dialogueUGUI, text, audioLength));
        }

        /// <summary>
        /// Coroutine to type the text based on the length of the audio clip
        /// </summary>
        /// <param name="dialogueUGUI">The instance of the dialogue GUI</param>
        /// <param name="text">The text to type</param>
        /// <param name="audioLength">The length of the audio clip</param>
        /// <returns></returns>
        private static IEnumerator TypeText(DS_DialogueUGUI dialogueUGUI, string text, float audioLength)
        {
            var currentWindow = dialogueUGUI._windowMgr.GetCurrentWindow();
            var actorSpeech = currentWindow.SubtitlesGroup.ActorSpeech;

            int totalCharacters = text.Length;
            float elapsedTime = 0f;
            float timePerCharacter = audioLength / totalCharacters;

            while (elapsedTime < audioLength)
            {
                elapsedTime += Time.deltaTime;
                int visibleCharacters = Mathf.FloorToInt(elapsedTime / timePerCharacter);

                actorSpeech.maxVisibleCharacters = Mathf.Clamp(visibleCharacters, 0, totalCharacters);
                try
                {
                    actorSpeech.ForceMeshUpdate(false, false);
                }
                catch (Exception)
                {
                    // this is a workaround for a bug in the dialogue system, when the dialogue window is first opened for the player
                    currentWindow = dialogueUGUI._windowMgr.GetCurrentWindow();
                    actorSpeech = currentWindow.SubtitlesGroup.ActorSpeech;
                    actorSpeech.text = dialogueUGUI._textHandler.GetTextWithoutOptionTags(text);
                    actorSpeech.maxVisibleCharacters = Mathf.Clamp(visibleCharacters, 0, totalCharacters);
                    actorSpeech.ForceMeshUpdate(false, false);
                }
                yield return null;
            }

            // Ensure all characters are visible at the end
            actorSpeech.maxVisibleCharacters = totalCharacters;
            actorSpeech.ForceMeshUpdate(false, false);
        }
    }
}
