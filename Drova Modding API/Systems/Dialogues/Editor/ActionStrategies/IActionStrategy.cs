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
        /// <param name="context">Action context</param>
        void OnStart(GraphEditorActionContext context);

        /// <summary>
        /// On the end of the action with the left click
        /// </summary>
        /// <param name="context">Action context</param>
        void OnEnd(GraphEditorActionContext context);

        /// <summary>
        /// When the users cancel the action, with a right click or escape
        /// </summary>
        /// <param name="context">Action context</param>
        void OnCancel(GraphEditorActionContext context);

        /// <summary>
        /// On GUI event
        /// </summary>
        /// <param name="context">Action context</param>
        void OnGui(GraphEditorActionContext context);
    }
}
