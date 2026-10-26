using Il2CppDrova.DialogueNew;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;

namespace Drova_Modding_API.Systems.Audio
{
    /// <summary>
    /// A connector for the dialogue system to connect to the audio system
    /// </summary>
    /// <param name="audioProvider">Audio provider to use</param>
    public class DefaultDialogueAudioConnector(IAudioProvider audioProvider) : IAudioConnector
    {
        /// <summary>
        /// The audio provider to use for the dialogue system
        /// </summary>
        public readonly IAudioProvider AudioProvider = audioProvider;

        internal static readonly HashSet<string> _loadedAudioTress = [];

        // Guards, hauers and bandits the game names by one shared, localized speaker name ("Wache", "Hauer"), by their own
        // EntityInfo without the EntityInfo_ and NPC_ prefixes.
        private static readonly HashSet<string> IndividualVoices = new(StringComparer.Ordinal)
        {
            "BanditBridge1", "BanditBridge2",
            "Bandit_Expedition_01", "Bandit_Expedition_02", "Bandit_Expedition_03", "Bandit_Expedition_04",
            "Bandit_Ruins_01", "Bandit_Ruins_02", "Bandit_Ruins_03", "Bandit_Ruins_04", "Bandit_Ruins_05",
            "KasernBandit_01", "KasernBandit_02", "KasernBandit_03",
            "GuardRedTower", "GuardRedTower2", "NemetonGuard01", "NemetonGuard02", "NemetonGuard03",
            "Ruinenlager_Guard_T2_01", "Ruinenlager_Guard_T2_02", "Ruinenlager_Guard_T2_03",
            "Hauer_01", "Hauer_02", "Hauer_03", "Hauer_04", "Hauer_07", "RuinenlagerHauer01", "RuinenlagerHauer02",
        };

        // Bandits placed several times with one EntityInfo, by the LazyActor guid stamped onto the spawned actor.
        private static readonly Dictionary<string, string> VoicesByGuid = new(StringComparer.OrdinalIgnoreCase)
        {
            ["24e396c3-ab24-47ed-95b8-652417ab900d"] = "Bandit_Mine_00",
            ["18b399eb-d3b3-467d-9f38-efabda6f8a59"] = "Bandit_Mine_01",
            ["de6c2cc7-72aa-40b3-b198-9aecf77d2a27"] = "Bandit_Mine_02",
            ["81710e4b-a587-4e4b-9647-6bf0408aa031"] = "Bandit_Normal_01",
            ["7413de53-f69e-490a-b2e3-a530a25a48e0"] = "Bandit_Normal_02",
            ["6ef4b6c5-cb25-449e-8dd7-e7b0ef546154"] = "Bandit_Expedition_05",
            ["2d3f10a6-95dd-42d7-8da6-c26ec85b1aa6"] = "Bandit_Expedition_06",
        };

        /// <summary>
        /// The name a speaker's voice is filed under: its own voice for a guard, hauer or bandit (see <see cref="HasOwnVoice"/>),
        /// otherwise the speaker's name.
        /// </summary>
        /// <param name="speaker">The speaking actor of a statement, may be null.</param>
        /// <returns>The voice name, or null without a speaker.</returns>
        public static string? VoiceName(IDialogueActor? speaker) => speaker == null ? null : OwnVoice(speaker) ?? speaker.name;

        /// <summary>
        /// Whether the speaker is a guard, hauer or bandit with a voice of its own. The game gives the whole group one
        /// speaker name, so a clip loaded for one member must not be reused for the next.
        /// </summary>
        /// <param name="speaker">The speaking actor of a statement, may be null.</param>
        public static bool HasOwnVoice(IDialogueActor? speaker) => OwnVoice(speaker) != null;

        // Decided by guid and EntityInfo, never by the speaker name: that one is localized.
        private static string? OwnVoice(IDialogueActor? speaker)
        {
            DS_DialogueActor? dialogueActor = speaker?.TryCast<DS_DialogueActor>();
            Il2CppDrova.Actor? actor = dialogueActor != null ? dialogueActor.Actor : null;
            if (actor == null) return null;
            Il2CppDrova.GuidComponent? guid = actor.GetGuidComponent();
            if (guid != null && VoicesByGuid.TryGetValue(guid.GuidString, out string? byGuid)) return byGuid;

            Il2CppDrova.EntityInfo? info = actor.GetEntityInfo();
            if (info == null) return null;
            string voice = info.name;
            if (voice.StartsWith("EntityInfo_", StringComparison.Ordinal)) voice = voice["EntityInfo_".Length..];
            while (voice.StartsWith("NPC_", StringComparison.Ordinal)) voice = voice["NPC_".Length..];
            return IndividualVoices.Contains(voice) ? voice : null;
        }

        /// <inheritdoc/>
        public void OnDialogueTreeLoaded(DialogueTree dialogueTree)
        {
            if (!_loadedAudioTress.Add(dialogueTree.name)) return;
            Dictionary<Task<AudioClip>, DS_StatementNode> taskToStatement = [];
            Dictionary<Task<AudioClip>, DS_MultipleChoiceNode.Choice> taskToChoice = [];
            for (int i = 0; i < dialogueTree.allNodes.Count; i++)
            {
                Il2CppNodeCanvas.Framework.Node node = dialogueTree.allNodes[i];
                DS_StatementNode statement = node.TryCast<DS_StatementNode>();
                if (statement != null)
                {
                    taskToStatement.TryAdd(AudioProvider.GetAudioClip(statement.DLGTree.name, statement.statement.useGlobalLoca ? statement.statement.GlobalLocaPath : dialogueTree.LocaPath, statement.statement.locaKey, statement.actorName, null), statement);
                    continue;
                }
                DS_MultipleChoiceNode multipleChoice = node.TryCast<DS_MultipleChoiceNode>();
                if (multipleChoice != null)
                {
                    for (int j = 0; j < multipleChoice.availableChoices.Count; j++)
                    {
                        DS_MultipleChoiceNode.Choice choice = multipleChoice.availableChoices[j];
                        taskToChoice.TryAdd(AudioProvider.GetAudioClip(multipleChoice.DLGTree.name, choice.statement.useGlobalLoca ? choice.statement.GlobalLocaPath : dialogueTree.LocaPath, choice.statement.locaKey, multipleChoice.actorName, j), choice);
                    }
                }
            }
            // Pump the dispatcher while waiting: clip creation may itself be queued for the
            // main thread, so a plain Task.WaitAll here would deadlock.
            MainThreadDispatcher.WaitForTasks([.. taskToStatement.Keys]);
            for (int i = 0; i < taskToStatement.Count; i++)
            {
                Task<AudioClip> task = taskToStatement.Keys.ElementAt(i);
                if (task.IsCompletedSuccessfully)
                {
                    taskToStatement[task].statement.audio = task.Result;
                }
                else
                {
                    AudioLog.Error($"Failed to load audio for statement {taskToStatement[task].statement.locaKey}");
                }
            }
            MainThreadDispatcher.WaitForTasks([.. taskToChoice.Keys]);
            for (int i = 0; i < taskToChoice.Count; i++)
            {
                Task<AudioClip> task = taskToChoice.Keys.ElementAt(i);
                if (task.IsCompletedSuccessfully)
                {
                    taskToChoice[task].statement.audio = task.Result;
                }
                else
                {
                    AudioLog.Error($"Failed to load audio for choice {taskToChoice[task].statement.locaKey}");
                }
            }
        }

        /// <inheritdoc/>
        public void OnWorldDialogueStatement(DS_StatementNode statementNode)
        {
            Task<AudioClip> task = AudioProvider.GetAudioClip(statementNode.DLGTree.name, statementNode.statement.useGlobalLoca ? statementNode.statement.GlobalLocaPath : statementNode.DLGTree.LocaPath, statementNode.statement.locaKey, VoiceName(statementNode.finalActor), null);
            MainThreadDispatcher.WaitForTasks(task);
            if (task.IsCompletedSuccessfully)
            {
                statementNode.statement.audio = task.Result;
            }
            else
            {
                AudioLog.Error($"Failed to load audio for statement {statementNode.statement.locaKey}");
            }
        }
    }
}
