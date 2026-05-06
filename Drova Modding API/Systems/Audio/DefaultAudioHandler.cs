using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
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
        private object _firstCoroutine;
        private object _secondCoroutine;
        public void HandleMultipleChoiceRequest(MultipleChoiceRequestInfo info, DS_DialogueUGUI dialogueUGUI)
        {
            //info.
        }

        public void HandleSubtitleRequest(SubtitlesRequestInfo info, DS_DialogueUGUI dialogueUGUI)
        {
            if (_firstCoroutine != null)
            {
                MelonCoroutines.Stop(_firstCoroutine);
            }
            if (_secondCoroutine != null)
            {
                MelonCoroutines.Stop(_secondCoroutine);
            }
            //MelonLogger.Msg($"Handling subtitle request for: {info.actor.name}");
            _firstCoroutine = MelonCoroutines.Start(WaitForWindowAndSetup(info, dialogueUGUI));
        }

        /// <summary>
        /// Sets the text of the actor speech to the text of the statement when the window is ready
        /// </summary>
        /// <param name="info">The info to set</param>
        /// <param name="dialogueUGUI">The instance of the dialouge GUI</param>
        /// <returns></returns>
        private IEnumerator WaitForWindowAndSetup(SubtitlesRequestInfo info, DS_DialogueUGUI dialogueUGUI)
        {
            string text = info.statement.text;
            Il2CppDrova.GUI.Dialogue.DS_DialogueWindow currentWindow = dialogueUGUI._windowMgr.GetCurrentWindow();
            if (currentWindow && !currentWindow.gameObject.transform.parent.name.Contains(info.actor.name, StringComparison.OrdinalIgnoreCase))
            {
                currentWindow = null;
            }
            while (currentWindow == null)
            {

                yield return null;
                currentWindow = dialogueUGUI._windowMgr.GetCurrentWindow();
                if (currentWindow && !currentWindow.gameObject.transform.parent.name.Contains(info.actor.name, StringComparison.OrdinalIgnoreCase))
                {
                    currentWindow = null;
                    yield return null;
                }
            }
            TextMeshProUGUI actorSpeech = currentWindow.SubtitlesGroup.ActorSpeech;
            actorSpeech.text = dialogueUGUI._textHandler.GetTextWithoutOptionTags(text);
            actorSpeech.maxVisibleCharacters = 0;
            try
            {
                actorSpeech.ForceMeshUpdate(false, false);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("ForceMeshUpdate failed during setup for text: {0} - {1}", text, e.Message);
            }
            float audioLength = info.statement.audio.length;
            _secondCoroutine = MelonCoroutines.Start(TypeText(dialogueUGUI, text, audioLength));
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
            Il2CppDrova.GUI.Dialogue.DS_DialogueWindow currentWindow = dialogueUGUI._windowMgr.GetCurrentWindow();
            TextMeshProUGUI actorSpeech = currentWindow.SubtitlesGroup.ActorSpeech;
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
                    // IDK? Sometimes the actorSpeech Text gets reseted and we need to restore it somehow
                    if (actorSpeech.text.Length == 0)
                    {
                        actorSpeech.text = dialogueUGUI._textHandler.GetTextWithoutOptionTags(text);
                    }
                    actorSpeech.ForceMeshUpdate(false, false);
                }
                catch (Exception)
                {
                    // this is a workaround for a bug in the dialogue system, when the dialogue window is first opened for the player
                    currentWindow = dialogueUGUI._windowMgr.GetCurrentWindow();
                    if (currentWindow == null)
                    {
                        MelonLogger.Warning("Window empty for text: {0}", text);
                        break;
                    }
                    actorSpeech = currentWindow.SubtitlesGroup.ActorSpeech;
                    actorSpeech.text = dialogueUGUI._textHandler.GetTextWithoutOptionTags(text);
                    actorSpeech.maxVisibleCharacters = Mathf.Clamp(visibleCharacters, 0, totalCharacters);
                    try
                    {
                        actorSpeech.ForceMeshUpdate(false, false);
                    }
                    catch (Exception innerEx)
                    {
                        MelonLogger.Warning("ForceMeshUpdate failed in recovery for text: {0} - {1}", text, innerEx.Message);
                    }
                }
                yield return null;
            }

            // Ensure all characters are visible at the end
            actorSpeech.maxVisibleCharacters = totalCharacters;
            try
            {
                actorSpeech.ForceMeshUpdate(false, false);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("ForceMeshUpdate failed at end of TypeText for text: {0} - {1}", text, e.Message);
            }
        }
    }
}