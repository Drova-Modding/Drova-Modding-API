namespace Drova_Modding_API.Systems.Dialogues.Editor.ActionStrategies
{
    /// <summary>
    /// Interface for action strategies
    /// </summary>
    public interface IActionStrategy
    {
        /// <summary>
        /// On start of the action
        /// </summary>
        void OnStart(GraphEditorActionContext context);

        /// <summary>
        /// On the end of the action with the left click
        /// </summary>
        void OnEnd(GraphEditorActionContext context);

        /// <summary>
        /// When the user cancels the action, with a right click or escape
        /// </summary>
        void OnCancel(GraphEditorActionContext context);

        /// <summary>
        /// On GUI event
        /// </summary>
        void OnGui(GraphEditorActionContext context);
    }
}
