using System.Globalization;
using Drova_Modding_API.Systems.Dialogues.Editor;
using Drova_Modding_API.Systems.Dialogues.Editor.Tasks;
using Drova_Modding_API.Systems.Routines.Store;
using Drova_Modding_API.Systems.Testing;
using Il2CppDrova;
using Il2CppDrova.Routine;
using Il2CppDrova.Utilities.LazyLoading;
using Il2CppNodeCanvas.BehaviourTrees;
using Il2CppNodeCanvas.Framework;
using UnityEngine;

namespace Drova_Modding_API.Systems.Routines.Editor
{
    /// <summary>
    /// The <c>routine</c> test agent command, so routine graphs, overrides and their editors can be checked in a running
    /// game. Debug builds only.
    /// <list type="bullet">
    /// <item><c>routine info &lt;actorGuid&gt;</c>: <c>place= tree= points=x;y|... pos=x;y</c> of the routine the NPC runs.</item>
    /// <item><c>routine dist &lt;actorGuid&gt; &lt;index&gt;</c>: distance from the NPC to a point of the place it runs.</item>
    /// <item><c>routine own &lt;actorGuid&gt;</c>: copy the shared tree into an own tree for exactly this NPC.</item>
    /// <item><c>routine shared &lt;actorGuid&gt;</c>: delete the NPC's override and the own tree <c>routine own</c> stored.</item>
    /// <item><c>routine move &lt;actorGuid&gt; &lt;index&gt; &lt;x&gt; &lt;y&gt;</c>: move a point of the NPC's current place.</item>
    /// <item><c>routine gate &lt;actorGuid&gt; &lt;gx&gt; &lt;gy&gt; &lt;tx&gt; &lt;ty&gt; &lt;x&gt; &lt;y&gt;</c>: replace the NPC's places with one wait point behind a gate; applied when the world loads.</item>
    /// <item><c>routine patchshared</c> / <c>routine unpatchshared</c>: store the shared tree under its own name and patch it, or remove that file.</item>
    /// <item><c>routine tasks &lt;treeId|shared&gt;</c>: each task and its fields as the generic task editor lists them.</item>
    /// <item><c>routine newaction &lt;treeId&gt; &lt;ActionTaskType&gt;</c>: add an action node to a stored tree, save and reload it; answers the action node count.</item>
    /// <item><c>routine window &lt;actorGuid&gt;</c> / <c>routine graphui &lt;actorGuid&gt;</c>: open the routine window, or the NPC's own tree in the graph editor (which pauses the game).</item>
    /// <item><c>routine closegraph</c>: close the graph editor and unpause.</item>
    /// </list>
    /// </summary>
    internal static class RoutineTestCommands
    {
        internal static void Register()
        {
            TestAgent.RegisterCommand("routine", Handle);
        }

        private static string Handle(string rest)
        {
            string[] parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string Arg(int index) => parts.Length > index ? parts[index] : throw new ArgumentException("missing argument " + index);

            return Arg(0) switch
            {
                "info" => Info(Arg(1)),
                "dist" => Distance(Arg(1), int.Parse(Arg(2))),
                "own" => Own(Arg(1)),
                "shared" => Shared(Arg(1)),
                "move" => Move(Arg(1), int.Parse(Arg(2)), new Vector2(Float(Arg(3)), Float(Arg(4)))),
                "gate" => Gate(Arg(1), new Vector2(Float(Arg(2)), Float(Arg(3))), new Vector2(Float(Arg(4)), Float(Arg(5))), new Vector2(Float(Arg(6)), Float(Arg(7)))),
                "patchshared" => PatchShared(),
                "unpatchshared" => UnpatchShared(),
                "tasks" => Tasks(Arg(1)),
                "newaction" => NewAction(Arg(1), Arg(2)),
                "window" => OpenWindow(Arg(1), openGraph: false),
                "graphui" => OpenWindow(Arg(1), openGraph: true),
                "closegraph" => CloseGraph(),
                _ => throw new ArgumentException("unknown routine command " + parts[0])
            };
        }

        private static string Info(string actorGuid)
        {
            if (!RoutineGraphAccess.GetRunningRoutines().TryGetValue(actorGuid, out (string PlaceKey, string TreeName) running))
            {
                return "running=0 pos=" + Position(actorGuid);
            }

            BaseRoutinePlace? place = Resources.FindObjectsOfTypeAll<BaseRoutinePlace>().FirstOrDefault(p => p.Key() == running.PlaceKey);
            string points = place == null
                ? string.Empty
                : string.Join("|", place._routinePlaces.ToArray().Select(point => Format(point.Position.x) + ";" + Format(point.Position.y)));
            return $"running=1 place={running.PlaceKey} tree={running.TreeName} points={points} pos={Position(actorGuid)}";
        }

        private static string Distance(string actorGuid, int index)
        {
            LazyActor lazy = FindLazy(actorGuid);
            if (!RoutineGraphAccess.GetRunningRoutines().TryGetValue(actorGuid, out (string PlaceKey, string TreeName) running))
            {
                throw new InvalidOperationException("the NPC runs no routine");
            }

            BaseRoutinePlace place = Resources.FindObjectsOfTypeAll<BaseRoutinePlace>().First(p => p.Key() == running.PlaceKey);
            Vector2 point = place._routinePlaces[index].Position;
            return Format(Vector2.Distance(lazy.GetFeetPosition(), point));
        }

        private static string Own(string actorGuid)
        {
            Actor actor = FindActor(actorGuid);
            BehaviourTree shared = RoutineGraphAccess.DefaultRoutineGraph ?? throw new InvalidOperationException("no shared routine graph");
            string treeId = "Routine_" + actorGuid;
            BehaviourTree copy = UnityEngine.Object.Instantiate(shared);
            copy.name = treeId;
            if (!BehaviourTreeStore.SaveBehaviourTree(treeId, copy)) throw new InvalidOperationException("saving the copy failed");

            RoutineOverride routineOverride = GetOverride(actor, actorGuid);
            routineOverride.GraphId = treeId;
            return RoutineOverrideStore.Save(routineOverride) ? treeId : throw new InvalidOperationException("saving the override failed");
        }

        private static string Shared(string actorGuid)
        {
            RoutineOverrideStore.Delete(actorGuid);
            string treeFile = BehaviourTreeStore.GetFilePath("Routine_" + actorGuid);
            File.Delete(treeFile);
            File.Delete(Path.ChangeExtension(treeFile, ".graph.dev.json"));
            return "ok";
        }

        private static string Move(string actorGuid, int index, Vector2 position)
        {
            Actor actor = FindActor(actorGuid);
            if (!RoutineGraphAccess.GetRunningRoutines().TryGetValue(actorGuid, out (string PlaceKey, string TreeName) running))
            {
                throw new InvalidOperationException("the NPC runs no routine");
            }

            RoutineOverride routineOverride = GetOverride(actor, actorGuid);
            RoutinePlaceEdit? edit = routineOverride.PlaceEdits.FirstOrDefault(candidate => candidate.PlaceKey == running.PlaceKey);
            if (edit == null)
            {
                edit = new RoutinePlaceEdit { PlaceKey = running.PlaceKey };
                routineOverride.PlaceEdits.Add(edit);
            }
            edit.Points.RemoveAll(point => point.Index == index);
            edit.Points.Add(new RoutinePointEdit { Index = index, Position = position });
            return RoutineOverrideStore.Save(routineOverride) ? "ok" : throw new InvalidOperationException("saving the override failed");
        }

        private static string Gate(string actorGuid, Vector2 gate, Vector2 target, Vector2 point)
        {
            RoutineOverride routineOverride = GetOverride(FindActor(actorGuid), actorGuid);
            routineOverride.ReplaceExisting = true;
            routineOverride.Places = [new RoutinePlaceOverride { Gate = gate, GateTarget = target, Points = [point] }];
            return RoutineOverrideStore.Save(routineOverride) ? "ok" : throw new InvalidOperationException("saving the override failed");
        }

        private static string PatchShared()
        {
            BehaviourTree shared = RoutineGraphAccess.DefaultRoutineGraph ?? throw new InvalidOperationException("no shared routine graph");
            if (!BehaviourTreeStore.SaveBehaviourTree(shared.name, shared)) throw new InvalidOperationException("saving failed");
            return "patched=" + (BehaviourTreeStore.TryPatchInMemoryBehaviourTree(shared.name) ? 1 : 0) + " name=" + shared.name;
        }

        private static string UnpatchShared()
        {
            string name = RoutineGraphAccess.DefaultRoutineGraph?.name ?? throw new InvalidOperationException("no shared routine graph");
            File.Delete(BehaviourTreeStore.GetFilePath(name));
            File.Delete(Path.ChangeExtension(BehaviourTreeStore.GetFilePath(name), ".graph.dev.json"));
            return "ok";
        }

        private static string Tasks(string treeId)
        {
            Graph tree = LoadTree(treeId);
            List<string> lines = [];
            foreach (Node node in tree.allNodes)
            {
                Il2CppNodeCanvas.Framework.Task? task = node.TryCast<ITaskAssignable>()?.task;
                if (task == null) continue;

                GenericTaskEditor editor = new() { Task = task };
                editor.Init();
                lines.Add(task.GetIl2CppType().Name + "{" + string.Join(",", editor.FieldSummary) + "}");
            }
            return $"count={lines.Count} " + string.Join(" ", lines);
        }

        private static string NewAction(string treeId, string actionType)
        {
            if (!BTActionNodeEditor.ActionTypes.TryGetValue(actionType, out Type? type)) throw new ArgumentException("no action type " + actionType);
            if (!BehaviourTreeStore.TryLoadBehaviourTree(treeId, out BehaviourTree? tree)) throw new InvalidOperationException("no stored tree " + treeId);

            ActionNode node = tree.AddNode<ActionNode>();
            node.TryGenerateUID();
            node.action = Il2CppSystem.Activator.CreateInstance(Il2CppInterop.Runtime.Il2CppType.From(type)).Cast<ActionTask>();
            if (!BehaviourTreeStore.SaveBehaviourTree(treeId, tree)) throw new InvalidOperationException("saving failed");
            RoutineGraphAccess.Invalidate(treeId);

            if (!BehaviourTreeStore.TryLoadBehaviourTree(treeId, out BehaviourTree? reloaded)) throw new InvalidOperationException("reloading failed");
            int actionNodes = reloaded.allNodes.ToArray().Count(candidate => candidate.TryCast<ActionNode>()?.action?.GetIl2CppType().Name == actionType);
            return "actions=" + actionNodes;
        }

        private static string OpenWindow(string actorGuid, bool openGraph)
        {
            Actor actor = FindActor(actorGuid);
            RoutineEditorWindow window = UnityEngine.Object.FindObjectOfType<RoutineEditorWindow>() ?? throw new InvalidOperationException("no editor UI (cheat mode)");
            GraphEditorManager graphEditor = Resources.FindObjectsOfTypeAll<GraphEditorManager>().FirstOrDefault() ?? throw new InvalidOperationException("no graph editor");
            window.Open(actor, graphEditor);
            if (!openGraph) return "ok";

            if (!RoutineGraphAccess.TryGetAssigned(actorGuid, out string treeId)) throw new InvalidOperationException("the NPC has no own tree");
            if (!BehaviourTreeStore.TryLoadBehaviourTree(treeId, out BehaviourTree? tree)) throw new InvalidOperationException("no stored tree " + treeId);
            graphEditor.gameObject.SetActive(true);
            graphEditor.Graph = tree;
            return "ok";
        }

        private static string CloseGraph()
        {
            Resources.FindObjectsOfTypeAll<GraphEditorManager>().FirstOrDefault()?.CloseGraph();
            return "ok";
        }

        private static Graph LoadTree(string treeId)
        {
            if (treeId == "shared") return RoutineGraphAccess.DefaultRoutineGraph ?? throw new InvalidOperationException("no shared routine graph");
            return BehaviourTreeStore.TryLoadBehaviourTree(treeId, out BehaviourTree? tree) ? tree : throw new InvalidOperationException("no stored tree " + treeId);
        }

        private static RoutineOverride GetOverride(Actor actor, string actorGuid)
        {
            if (RoutineOverrideStore.TryGet(actorGuid, out RoutineOverride? stored)) return stored;

            string entityGuid = actor.GetEntityInfo()?.GUID ?? throw new InvalidOperationException("the NPC has no EntityInfo");
            return new RoutineOverride { EntityGuid = entityGuid, ActorGuid = actorGuid, Name = actor.name };
        }

        private static LazyActor FindLazy(string actorGuid)
        {
            return Resources.FindObjectsOfTypeAll<LazyActor>().FirstOrDefault(candidate => candidate != null && candidate.GUID == actorGuid)
                ?? throw new InvalidOperationException("no actor " + actorGuid);
        }

        private static Actor FindActor(string actorGuid)
        {
            return Resources.FindObjectsOfTypeAll<Actor>().FirstOrDefault(actor => actor != null && actor.GetGuidComponent()?.GuidString == actorGuid)
                ?? throw new InvalidOperationException("no actor " + actorGuid);
        }

        private static string Position(string actorGuid)
        {
            Vector2 position = FindLazy(actorGuid).GetFeetPosition();
            return Format(position.x) + ";" + Format(position.y);
        }

        private static float Float(string text) => float.Parse(text, CultureInfo.InvariantCulture);

        private static string Format(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
