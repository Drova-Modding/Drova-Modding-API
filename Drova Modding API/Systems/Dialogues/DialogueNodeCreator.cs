using Il2CppDrova.ActorActions;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.Factions;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;
using static Il2CppDrova.Actor;
using static Il2CppNodeCanvas.DialogueTrees.DialogueTree;

namespace Drova_Modding_API.Systems.Dialogues
{
    /// <summary>
    /// Class that helps creating nodes for a DialogueTree
    /// </summary>
    /// <param name="tree">The dialogue tree for which the nodes should be created</param>
    /// <param name="playerParameter">The player Parameter</param>
    public class DialogueNodeCreator(DialogueTree tree, ActorParameter playerParameter)
    {
        /**
         * The DialogueTree that this NodeCreator is working with
         */
        protected DialogueTree Tree = tree;
        /**
         * The ActorParameter of the player
         */
        protected ActorParameter PlayerParameter = playerParameter;

        /// <summary>
        /// Create a statement node. Basically a node that contains a statement and makes the actor speak.
        /// </summary>
        /// <param name="statement">The Localization/Audio</param>
        /// <param name="actorParameter">The Actor which should speak this dialogue</param>
        /// <returns></returns>
        public virtual DS_StatementNode CreateStatementNode(DS_Statement statement, ActorParameter actorParameter)
        {
            var newNode = Tree.AddNode<DS_StatementNode>();
            newNode.statement = statement;
            newNode._actorName = actorParameter._keyName;
            newNode._actorParameterID = actorParameter.ID;
            newNode.TryGenerateUID();
            return newNode;
        }

        /// <summary>
        /// Create a multiple choice node. A node that contains multiple choices that the player can select.
        /// </summary>
        /// <param name="choices"></param>
        /// <returns></returns>
        public virtual DS_MultipleChoiceNode CreateMultipleChoices(ChoiceCreationData[] choices)
        {
            var newNode = Tree.AddNode<DS_MultipleChoiceNode>();
            newNode.availableChoices = new Il2CppSystem.Collections.Generic.List<DS_MultipleChoiceNode.Choice>();
            for (int i = 0; i < choices.Length; i++)
            {
                ChoiceCreationData choice = choices[i];
                newNode.availableChoices.Add(new DS_MultipleChoiceNode.Choice
                {
                    statement = choice.Statement,
                    isEndNode = choice.IsEndNode,
                    condition = choice.Condition,
                    UID = Il2CppSystem.Guid.NewGuid().ToString()
                });
            }
            newNode._actorName = PlayerParameter._keyName;
            newNode._actorParameterID = PlayerParameter.ID;
            newNode.TryGenerateUID();
            return newNode;
        }

        /// <summary>
        /// Create a node that makes the actor perform an action.
        /// </summary>
        /// <param name="baseInteractionPrefab">Which interaction to perform <see cref="Access.AddressableAccess.NPCs.Interactions"/></param>
        /// <param name="actorParameter">Which actor should perform this action</param>
        /// <returns></returns>
        public virtual DS_InteractAABaseNode CreateInteractionNode(AA_ABase baseInteractionPrefab, ActorParameter actorParameter)
        {
            var newNode = Tree.AddNode<DS_InteractAABaseNode>();
            newNode._actorName = actorParameter._keyName;
            newNode._actorParameterID = actorParameter._id;
            newNode._hideDialogueWindow = true;
            newNode._interactPrefab = baseInteractionPrefab;
            newNode._waitForFinish = true;
            newNode.TryGenerateUID();
            return newNode;
        }

        /// <summary>
        /// Create a node that hides the dialog window. This is useful for actions that should be performed without the dialog window being visible like <see cref="DS_ChangeStanceNode"/>
        /// </summary>
        /// <param name="coroutine">Coroutine for additional logic when the dialog should become visible again, can be null</param>
        /// <returns></returns>
        public virtual DS_HideDialogWindow CreateDialogWindowHideNode(UnityEngine.Coroutine? coroutine)
        {
            var newNode = Tree.AddNode<DS_HideDialogWindow>();
            newNode._routine = coroutine;
            newNode.TryGenerateUID();
            return newNode;
        }

        /// <summary>
        /// Create a node that changes the stance of an actor.
        /// </summary>
        /// <param name="interactionMode">The interaction to change at</param>
        /// <param name="actor">The actor which should change to the interaction</param>
        /// <returns></returns>
        public virtual DS_ChangeStanceNode CreateChangeStanceNode(EInteractionMode interactionMode, ActorParameter actor)
        {
            var newNode = Tree.AddNode<DS_ChangeStanceNode>();
            newNode._interactionMode = interactionMode;
            newNode._actorName = actor._keyName;
            newNode._actorParameterID = actor.ID;
            newNode.TryGenerateUID();
            return newNode;
        }

        /// <summary>
        /// Create a node that gives the player experience points.
        /// </summary>
        /// <param name="exp">How much of exp you want to give</param>
        /// <returns></returns>
        public virtual DS_GiveExp CreateExpNode(int exp)
        {
            var newNode = Tree.AddNode<DS_GiveExp>();
            newNode.ExpPoints = exp;
            newNode.TryGenerateUID();
            return newNode;
        }

        /// <summary>
        /// Create a node that gives the player/npc/void items from void/npc/player.
        /// </summary>
        /// <param name="itemsExchanges">The items to exchange (TODO: Make it more user friendly if this will get exposed)</param>
        /// <param name="actor">The Actor to execute this node</param>
        /// <param name="equip">If the reciever should equip it</param>
        /// <param name="equipInEmptyActiveSlot">If the receiver should equip it in a empty active slot</param>
        /// <returns></returns>
        public virtual DS_GiveItemNode CreateGiveItemNode(ICollection<DialogItemsExchange> itemsExchanges, ActorParameter actor, bool equip = false, bool equipInEmptyActiveSlot = false)
        {
            var newNode = Tree.AddNode<DS_GiveItemNode>();
            newNode.ItemStacks = new Il2CppSystem.Collections.Generic.List<DialogItemsExchange>();
            for (int i = 0; i < itemsExchanges.Count; i++)
            {
                newNode.ItemStacks.Add(itemsExchanges.ElementAt(i));
            }
            newNode._actorName = actor._keyName;
            newNode._actorParameterID = actor.ID;
            newNode._equip = equip;
            newNode._equipInEmptyActiveSlot = equipInEmptyActiveSlot;
            newNode.TryGenerateUID();
            return newNode;
        }

        /// <summary>
        /// Create a node that waits for a certain amount of time.
        /// </summary>
        /// <param name="time">The time to wait (in seconds)</param>
        /// <returns></returns>
        public virtual DS_DebugNode CreateWaitNode(float time)
        {
            var newNode = Tree.AddNode<DS_DebugNode>();
            newNode._waitTime = time;
            newNode.TryGenerateUID();
            return newNode;
        }

        /// <summary>
        /// Create a node that sets the first chapter of the dialogue. Normally used by Asmus at the beginning.
        /// </summary>
        /// <returns></returns>
        public virtual DS_SetFirstChapter CreateSetFirstChapterNode()
        {
            var newNode = Tree.AddNode<DS_SetFirstChapter>();
            newNode.TryGenerateUID();
            return newNode;
        }

        /// <summary>
        /// Create a node that sets the faction for the player.
        /// </summary>
        /// <param name="faction">The faction to set</param>
        /// <returns></returns>
        public virtual DS_SetFactionNode CreateSetFactionNode(Faction faction)
        {
            var newNode = Tree.AddNode<DS_SetFactionNode>();
            newNode._actorName = PlayerParameter._keyName;
            newNode._actorParameterID = PlayerParameter.ID;
            newNode._faction = faction;
            newNode.TryGenerateUID();
            return newNode;
        }


    }

    /// <summary>
    /// Data for creating a choice
    /// </summary>
    public struct ChoiceCreationData
    {
        /**
         * The choice is only available if a condition is met, can be null
         */
        public ConditionTask? Condition;
        /**
         * Statement that can be selected by the player
         */
        public DS_Statement Statement;

        /**
         * If this choice is an end node, meaning that the dialogue ends after this choice
         */
        public bool IsEndNode;
    }
}
