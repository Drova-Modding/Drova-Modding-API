using Il2CppNodeCanvas.DialogueTrees;
using System.Text;

namespace Drova_Modding_API.Systems.Audio.Dialogue.Generic
{
    internal interface IGenericDialogueHandler
    {
        /// <summary>
        /// Check if this handler can handle the given dialogue tree
        /// </summary>
        /// <param name="tree">Tree</param>
        /// <returns></returns>
        bool CanHandleDialogue(DialogueTree tree);
        /// <summary>
        /// Handle the given dialogue tree
        /// </summary>
        /// <param name="tree">Tree</param>
        /// <param name="dialogStringBuilder">Stringbuilder to append lines to the dialog file</param>
        /// <param name="actorMapping">Mapping of actor names to actor IDs</param>
        void HandleDialogue(DialogueTree tree, StringBuilder dialogStringBuilder, Dictionary<string, int> actorMapping);
    }
}
