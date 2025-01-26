using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Interface for handling audio requests, such as subtitles and multiple choice audio
    /// This interface is used to make sure that the audio and text systems can work together
    /// </summary>
    public interface IAudioHandler
    {
        /// <summary>
        /// Handle a subtitle request
        /// </summary>
        /// <param name="info">Info of the request</param>
        /// <param name="dialogueUGUI"></param>
        void HandleSubtitleRequest(SubtitlesRequestInfo info, DS_DialogueUGUI dialogueUGUI);

        /// <summary>
        /// Handle a multiple choice request
        /// </summary>
        /// <param name="info"></param>
        /// <param name="dialogueUGUI"></param>
        void HandleMultipleChoiceRequest(MultipleChoiceRequestInfo info, DS_DialogueUGUI dialogueUGUI);
    }
}
