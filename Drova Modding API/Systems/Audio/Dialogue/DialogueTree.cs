using Il2CppNodeCanvas.DialogueTrees;

namespace Drova_Modding_API.Systems.Audio.Dialogue
{
    internal class DialogueTreeTree
    {
        public string Name { get; set; }
        public List<DialogueTreeNode> Roots { get; set; } = [];
    }

    internal class DialogueTreeNode
    {
        public DialogueTree Tree { get; set; }
        public List<DialogueTreeNode> Children { get; set; } = [];
        public List<DS_Statement> Statements { get; set; } = [];
        public List<DS_MultipleChoiceNode> MultipleChoiceNodes { get; set; } = [];
        public DialogueTreeNode? Parent { get; set; }
    }
}
