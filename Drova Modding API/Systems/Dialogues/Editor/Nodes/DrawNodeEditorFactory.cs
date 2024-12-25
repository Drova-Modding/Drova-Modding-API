using MelonLoader;

namespace Drova_Modding_API.Systems.Dialogues.Editor.Nodes
{
    /// <summary>
    /// Factory class that helps creating DrawNodeEditors
    /// </summary>
    public class DrawNodeEditorFactory
    {
        private readonly Dictionary<string, Type> nameToNodeMap = new()
        {
            { "DS_StatementNode", typeof(DS_StatementNodeEditor) },
            { "DS_MultipleChoiceNode", typeof(DS_MultipleChoiceNodeEditor) },
            { "DS_GiveExp", typeof(DS_GiveExpNodeEditor) },
            { "DS_ChangeStanceNode", typeof(DS_ChangeStanceNodeEditor) },
            { "DS_DebugNode", typeof(DS_DebugNodeEditor) },
            { "DS_HideDialogWindow", typeof(DS_HideDialogWindowNodeEditor) },
        };

        /// <summary>
        /// Get a DrawNodeEditor by index
        /// </summary>
        /// <param name="index"><see cref="DrawNodeType"/></param>
        /// <returns>The corosponding DrawNodeEditor</returns>
        public virtual DrawNodeEditor GetDrawNodeEditor(int index)
        {
            return index switch
            {
                (int)DrawNodeType.StatementNode => new DS_StatementNodeEditor(),
                (int)DrawNodeType.MultipleChoiceNode => null,
                _ => null,
            };
        }

        /// <summary>
        /// Get a DrawNodeEditor by type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual DrawNodeEditor GetDrawNodeEditorFromType(Il2CppSystem.Type type)
        {
            try
            {
                if (nameToNodeMap.TryGetValue(type.Name, out Type v))
                {
                    return (DrawNodeEditor)Activator.CreateInstance(v);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error creating DrawNodeEditor from type: " + e);
                return null;
            }

        }

        /// <summary>
        /// Enum for the different node types
        /// </summary>
        protected enum DrawNodeType
        {
            /**
             * A node that contains a statement and makes the actor speak.
             */
            StatementNode,
            /**
             * A node that contains multiple choices that the player can select.
             */
            MultipleChoiceNode
        }
    }
}
