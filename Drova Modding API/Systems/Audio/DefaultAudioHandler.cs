using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
using MelonLoader;
using System.Collections;

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
            throw new NotImplementedException();
        }

        public void HandleSubtitleRequest(SubtitlesRequestInfo info, DS_DialogueUGUI dialogueUGUI)
        {
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
            actorSpeech.maxVisibleCharacters = text.Length;
            actorSpeech.ForceMeshUpdate(false, false);
        }
    }
}
