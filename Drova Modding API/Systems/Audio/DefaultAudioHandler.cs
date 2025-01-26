using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
using MelonLoader;
using System.Collections;

namespace Drova_Modding_API.Systems.Audio
{
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
