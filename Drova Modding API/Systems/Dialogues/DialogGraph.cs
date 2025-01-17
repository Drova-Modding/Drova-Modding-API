using Il2CppDrova;
using Il2CppDrova.ActorActions;
using Il2CppDrova.DialogueNew;
using Il2CppDrova.Factions;
using Il2CppDrova.Items;
using Il2CppNodeCanvas.DialogueTrees;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Il2CppNodeCanvas.DialogueTrees.DialogueTree;
using static Il2CppNodeCanvas.DialogueTrees.DS_MultipleChoiceNode;

namespace Drova_Modding_API.Systems.Dialogues
{
    public static class DialogGraph
    {
        internal static void AddDialogGraph(Actor actor)
        {
            DialogueTree dt = ScriptableObject.CreateInstance<DialogueTree>();
            dt.actorParameters = new Il2CppSystem.Collections.Generic.List<ActorParameter>();
            string actorId = Il2CppSystem.Guid.NewGuid().ToString();
            DS_DialogueTreeController dialogTree = actor.GetComponentInChildren<DS_DialogueTreeController>();
            ActorParameter playerActor = ActorParameterHelper.GetPlayerActorParameters();

            dt.actorParameters.Add(new ActorParameter { _keyName = "Test", Actor = null, ActorGuid = actor.GetGuidComponent()._guidString, _id = actorId });
            dt.actorParameters.Add(playerActor);
            DS_StatementNode startNode = dt.AddNode<DS_StatementNode>();
            DS_Statement statement = new()
            {
                useGlobalLoca = true,
                GlobalLocaPath = "Test/Human",
                locaKey = "Test"
            };
            startNode.statement = statement;
            startNode.actorName = "Test";
            startNode._actorParameterID = actorId;
            startNode.TryGenerateUID();
            DS_MultipleChoiceNode endNode = dt.AddNode<DS_MultipleChoiceNode>();
            endNode.actorName = playerActor._keyName;
            endNode._actorParameterID = playerActor.ID;
            endNode.availableChoices = new Il2CppSystem.Collections.Generic.List<Choice>();
            endNode.availableChoices.Add(new Choice { statement = statement, isEndNode = false, UID = Il2CppSystem.Guid.NewGuid().ToString() });
            endNode.availableChoices.Add(new Choice { statement = statement, isEndNode = true, UID = Il2CppSystem.Guid.NewGuid().ToString() });
            endNode.TryGenerateUID();

            DS_StatementNode additionalEndNode = dt.AddNode<DS_StatementNode>();
            additionalEndNode.actorName = "Test";
            additionalEndNode._actorParameterID = actorId;
            additionalEndNode.statement = statement;
            additionalEndNode.TryGenerateUID();

            DS_GiveExp experienceNode = dt.AddNode<DS_GiveExp>();
            experienceNode.actorName = "Test";
            experienceNode._actorParameterID = actorId;
            experienceNode.ExpPoints = 500;
            experienceNode.TryGenerateUID();

            DS_ChangeStanceNode stanceNode = dt.AddNode<DS_ChangeStanceNode>();
            stanceNode.actorName = "Test";
            stanceNode._actorParameterID = actorId;
            stanceNode._interactionMode = Actor.EInteractionMode.Smoke;
            stanceNode.TryGenerateUID();

            DS_DebugNode waitNode = dt.AddNode<DS_DebugNode>();
            waitNode.actorName = "Test";
            waitNode._actorParameterID = actorId;
            waitNode._waitTime = 5;
            waitNode.TryGenerateUID();

            DS_HideDialogWindow hideDialogNode = dt.AddNode<DS_HideDialogWindow>();
            hideDialogNode.TryGenerateUID();

            DS_InteractAABaseNode interactionNode = dt.AddNode<DS_InteractAABaseNode>();
            interactionNode.actorName = "Test";
            interactionNode._actorParameterID = actorId;
            interactionNode._hideDialogueWindow = true;
            interactionNode._interactPrefab = Addressables.LoadAssetAsync<GameObject>(Access.AddressableAccess.NPCs.Interactions.AA_Interact_NPC_Axe_MineVein).WaitForCompletion().GetComponent<AA_ABase>();
            interactionNode._waitForFinish = true;
            interactionNode.TryGenerateUID();

            DS_SetFactionNode factionNode = dt.AddNode<DS_SetFactionNode>();
            factionNode.actorName = "Test";
            factionNode._actorParameterID = actorId;
            factionNode._faction = Resources.FindObjectsOfTypeAll<Faction>()[0];
            factionNode.TryGenerateUID();

            DS_GiveItemNode itemNode = dt.AddNode<DS_GiveItemNode>();
            itemNode.actorName = "Test";
            itemNode._actorParameterID = actorId;
            itemNode.ItemStacks = new Il2CppSystem.Collections.Generic.List<DialogItemsExchange>();

            DialogItemsExchange itemStack = new()
            {
                Item = Resources.FindObjectsOfTypeAll<Item>()[0],
                Exchange = DialogItemsExchange.ExchangeDirection.VoidToPlayer,
                Mode = DialogItems.ValueMode.Int,
                Amount = 1,
                UseContainer = false
            };
            itemNode.ItemStacks.Add(itemStack);
            itemNode.TryGenerateUID();

            dt.ConnectNodes(startNode, endNode);
            dt.ConnectNodes(endNode, additionalEndNode);
            dt.ConnectNodes(additionalEndNode, experienceNode);
            dt.ConnectNodes(experienceNode, stanceNode);
            dt.ConnectNodes(stanceNode, waitNode);
            dt.ConnectNodes(waitNode, hideDialogNode);
            dt.ConnectNodes(hideDialogNode, interactionNode);
            dt.ConnectNodes(interactionNode, factionNode);
            dt.ConnectNodes(factionNode, itemNode);

            dt.name = "Generated";
            dt.primeNode = startNode;
            dt.Serialize(null);

            dialogTree.graph = dt;
        }
    }

}
