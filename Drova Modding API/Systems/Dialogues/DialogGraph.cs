using Il2CppDrova;
using Il2CppDrova.DialogueNew;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.Framework;
using Il2CppParadoxNotion.Services;
using Il2CppSystem;
using UnityEngine;
using static Il2CppNodeCanvas.DialogueTrees.DialogueTree;
using static Il2CppNodeCanvas.DialogueTrees.DS_MultipleChoiceNode;

namespace Drova_Modding_API.Systems.Dialogues
{
    public static class DialogGraph
    {
        internal static void AddDialogGraph(Actor actor)
        {
            var dt = ScriptableObject.CreateInstance<DialogueTree>();
            dt.actorParameters = new Il2CppSystem.Collections.Generic.List<ActorParameter>();
            var actorId = Il2CppSystem.Guid.NewGuid().ToString();
            var dialogTree = actor.GetComponentInChildren<DS_DialogueTreeController>();
            var playerActor = ActorParameterHelper.GetPlayerActorParameters();

            dt.actorParameters.Add(new ActorParameter { _keyName = "Test", Actor = null, ActorGuid = actor.GetGuidComponent()._guidString, _id = actorId });
            dt.actorParameters.Add(playerActor);
            var startNode = dt.AddNode<DS_StatementNode>();
            var statement = new DS_Statement
            {
                useGlobalLoca = true,
                GlobalLocaPath = "Test/Human",
                locaKey = "Test"
            };
            startNode.statement = statement;
            startNode.actorName = "Test";
            startNode._actorParameterID = actorId;
            startNode.TryGenerateUID();
            var endNode = dt.AddNode<DS_MultipleChoiceNode>();
            endNode.actorName = playerActor._keyName;
            endNode._actorParameterID = playerActor.ID;
            endNode.availableChoices = new Il2CppSystem.Collections.Generic.List<Choice>();
            endNode.availableChoices.Add(new Choice { statement = statement, isEndNode = false, UID = Il2CppSystem.Guid.NewGuid().ToString() });
            endNode.availableChoices.Add(new Choice { statement = statement, isEndNode = true, UID = Il2CppSystem.Guid.NewGuid().ToString() });
            endNode.TryGenerateUID();

            var additionalEndNode = dt.AddNode<DS_StatementNode>();
            additionalEndNode.actorName = "Test";
            additionalEndNode._actorParameterID = actorId;
            additionalEndNode.statement = statement;
            additionalEndNode.TryGenerateUID();

            var experienceNode = dt.AddNode<DS_GiveExp>();
            experienceNode.actorName = "Test";
            experienceNode._actorParameterID = actorId;
            experienceNode.ExpPoints = 500;
            experienceNode.TryGenerateUID();

            var stanceNode = dt.AddNode<DS_ChangeStanceNode>();
            stanceNode.actorName = "Test";
            stanceNode._actorParameterID = actorId;
            stanceNode._interactionMode = Actor.EInteractionMode.Smoke;
            stanceNode.TryGenerateUID();

            var waitNode = dt.AddNode<DS_DebugNode>();
            waitNode.actorName = "Test";
            waitNode._actorParameterID = actorId;
            waitNode._waitTime = 5;
            waitNode.TryGenerateUID();

            var hideDialogNode = dt.AddNode<DS_HideDialogWindow>();
            hideDialogNode.TryGenerateUID();



            dt.ConnectNodes(startNode, endNode);
            dt.ConnectNodes(endNode, additionalEndNode);
            dt.ConnectNodes(additionalEndNode, experienceNode);
            dt.ConnectNodes(experienceNode, stanceNode);
            dt.ConnectNodes(stanceNode, waitNode);
            dt.ConnectNodes(waitNode, hideDialogNode);

            dt.name = "Generated";
            dt.primeNode = startNode;
            dt.Serialize(null);

            dialogTree.graph = dt;
        }
    }

}
