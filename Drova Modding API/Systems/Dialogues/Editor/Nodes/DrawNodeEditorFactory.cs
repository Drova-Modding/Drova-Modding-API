using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                { "DS_MultipleChoiceNode", typeof(DS_MultipleChoiceNodeEditor) }
            };

        /// <summary>
        /// Get a DrawNodeEditor by index
        /// </summary>
        /// <param name="index"><see cref="DrawNodeType"/></param>
        /// <returns>The corosponding DrawNodeEditor</returns>
        public virtual DrawNodeEditor GetDrawNodeEditor(int index)
        {
            switch (index)
            {
                case (int)DrawNodeType.StatementNode:
                    return new DS_StatementNodeEditor();
                case (int)DrawNodeType.MultipleChoiceNode:
                    return null;
                default:
                    return null;
            }
        }

        public virtual DrawNodeEditor GetDrawNodeEditorFromType(Il2CppSystem.Type type)
        {
            try {
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
