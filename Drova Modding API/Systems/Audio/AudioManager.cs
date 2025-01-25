namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// Manager for the audio system
    /// </summary>
    public static class AudioManager
    {
        internal static IAudioConnector _dialogueAudioConnector = new DefaultDialogueAudioConnector(new FileAudioProvider());

        /// <summary>
        /// Replace the current DialougeAudioConnector with a new one
        /// </summary>
        /// <param name="dialogueAudioConnector"></param>
        public static void SetDialogueAudioConnector(IAudioConnector dialogueAudioConnector)
        {
            _dialogueAudioConnector = dialogueAudioConnector;
        }
    }
}
