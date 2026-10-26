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
        /// <summary>
        /// One conversation's typing: the coroutine waiting for its window, the one revealing its line, and the line it reveals.
        /// A line clicked through or followed by the next request stops them before they end, and would stay cut off mid-word
        /// while its audio plays on, so the stopped line is shown in full.
        /// </summary>
        private sealed class Typing
        {
            public object? Setup;
            public object? Reveal;
            public TextMeshProUGUI? Speech;
        }

        /// <summary>
        /// Typing per conversation, by its dialogue tree's name. World dialogues run at the same time, and one shared state let a line
        /// far away cancel the setup of a line on screen, whose bubble then stayed empty while its clip played.
        /// </summary>
        private readonly Dictionary<string, Typing> _typing = [];

        /// <summary>
        /// Shows the line still being typed in full: clicking through it moves the dialogue on to the answers.
        /// </summary>
        public void HandleMultipleChoiceRequest(MultipleChoiceRequestInfo info, DS_DialogueUGUI dialogueUGUI)
        {
            StopTyping(TypingOf(dialogueUGUI));
        }

        public void HandleSubtitleRequest(SubtitlesRequestInfo info, DS_DialogueUGUI dialogueUGUI)
        {
            Typing typing = TypingOf(dialogueUGUI);
            StopTyping(typing);
            typing.Setup = MelonCoroutines.Start(WaitForWindowAndSetup(info, dialogueUGUI, typing));
        }

        private Typing TypingOf(DS_DialogueUGUI dialogueUGUI)
        {
            string key = dialogueUGUI.DLGTree != null ? dialogueUGUI.DLGTree.name : dialogueUGUI.name;
            if (!_typing.TryGetValue(key, out Typing? typing)) _typing[key] = typing = new Typing();
            return typing;
        }

        private static void StopTyping(Typing typing)
        {
            if (typing.Setup != null)
            {
                MelonCoroutines.Stop(typing.Setup);
            }
            if (typing.Reveal != null)
            {
                MelonCoroutines.Stop(typing.Reveal);
            }
            if (typing.Speech)
            {
                typing.Speech!.maxVisibleCharacters = 99999;
            }
            typing.Setup = typing.Reveal = null;
            typing.Speech = null;
        }

        /// <summary>
        /// Sets the text of the actor speech to the text of the statement when the window is ready
        /// </summary>
        private static IEnumerator WaitForWindowAndSetup(SubtitlesRequestInfo info, DS_DialogueUGUI dialogueUGUI, Typing typing)
        {
            string text = info.statement.text;
            string speaker = info.actor.name;
            Il2CppDrova.GUI.Dialogue.DS_DialogueWindow? currentWindow = SpeakerWindow(dialogueUGUI, speaker);
            while (currentWindow == null)
            {
                yield return null;
                currentWindow = SpeakerWindow(dialogueUGUI, speaker);
            }
            TextMeshProUGUI actorSpeech = currentWindow.SubtitlesGroup.ActorSpeech;
            actorSpeech.text = dialogueUGUI._textHandler.GetTextWithoutOptionTags(text);
            actorSpeech.maxVisibleCharacters = 0;
            try
            {
                actorSpeech.ForceMeshUpdate();
            }
            catch (Exception e)
            {
                AudioLog.Warning("ForceMeshUpdate failed during setup for text: {0} - {1}", text, e.Message);
            }
            float audioLength = info.statement.audio.length;
            typing.Speech = actorSpeech;
            typing.Reveal = MelonCoroutines.Start(TypeText(dialogueUGUI, speaker, typing, text, audioLength));
        }

        /// <summary>
        /// The window the dialogue UI currently shows, if it belongs to the speaker. Another conversation's line, such as a world
        /// dialogue off screen, can be current by then, and typing into its window left the speaker's own bubble empty.
        /// </summary>
        private static Il2CppDrova.GUI.Dialogue.DS_DialogueWindow? SpeakerWindow(DS_DialogueUGUI dialogueUGUI, string speaker)
        {
            Il2CppDrova.GUI.Dialogue.DS_DialogueWindow window = dialogueUGUI._windowMgr.GetCurrentWindow();
            return window != null && window.gameObject.transform.parent.name.Contains(speaker, StringComparison.OrdinalIgnoreCase) ? window : null;
        }

        /// <summary>
        /// Coroutine to type the text based on the length of the audio clip
        /// </summary>
        private static IEnumerator TypeText(DS_DialogueUGUI dialogueUGUI, string speaker, Typing typing, string text, float audioLength)
        {
            TextMeshProUGUI actorSpeech = typing.Speech!;
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
                    // The game sometimes clears the speech text mid-line.
                    if (actorSpeech.text.Length == 0)
                    {
                        actorSpeech.text = dialogueUGUI._textHandler.GetTextWithoutOptionTags(text);
                    }
                    actorSpeech.ForceMeshUpdate();
                }
                catch (Exception)
                {
                    // ForceMeshUpdate throws when the player's dialogue window is first opened, so the window is looked up again.
                    Il2CppDrova.GUI.Dialogue.DS_DialogueWindow? currentWindow = SpeakerWindow(dialogueUGUI, speaker);
                    if (currentWindow == null)
                    {
                        AudioLog.Warning("Window empty for text: {0}", text);
                        yield break;
                    }
                    actorSpeech = currentWindow.SubtitlesGroup.ActorSpeech;
                    typing.Speech = actorSpeech;
                    actorSpeech.text = dialogueUGUI._textHandler.GetTextWithoutOptionTags(text);
                    actorSpeech.maxVisibleCharacters = Mathf.Clamp(visibleCharacters, 0, totalCharacters);
                    try
                    {
                        actorSpeech.ForceMeshUpdate();
                    }
                    catch (Exception innerEx)
                    {
                        AudioLog.Warning("ForceMeshUpdate failed in recovery for text: {0} - {1}", text, innerEx.Message);
                    }
                }
                yield return null;
            }

            actorSpeech.maxVisibleCharacters = totalCharacters;
            try
            {
                actorSpeech.ForceMeshUpdate();
            }
            catch (Exception e)
            {
                AudioLog.Warning("ForceMeshUpdate failed at end of TypeText for text: {0} - {1}", text, e.Message);
            }
        }
    }
}