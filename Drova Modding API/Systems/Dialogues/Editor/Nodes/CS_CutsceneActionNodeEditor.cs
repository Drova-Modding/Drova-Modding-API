using Il2CppDrova.Cutscenes;
using UnityEngine;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Node editor for <see cref="CS_CutsceneActionNode"/>
    /// </summary>
    internal class CS_CutsceneActionNodeEditor : DrawNodeEditor
    {
        private CS_CutsceneActionNode _castedNode;

        public override void Init()
        {
            _castedNode ??= Node.TryCast<CS_CutsceneActionNode>();
        }

        public override Rect DrawNode(Vector2 position)
        {
            if(_castedNode == null)
            {
                return default;
            }
            throw new System.NotImplementedException();
        }
    }
}
