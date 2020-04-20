namespace Swihoni.Sessions
{
    internal interface IModifierBase<in TComponent, in TCommands>
    {
        /// <summary>
        ///     Called in FixedUpdate() based on game tick rate
        /// </summary>
        void ModifyChecked(TComponent containerToModify, TCommands commands, float duration);

        /// <summary>
        ///     Called in Update() right after inputs are sampled
        /// </summary>
        void ModifyTrusted(TComponent containerToModify, TCommands commands, float duration);

        void ModifyCommands(TCommands commandsToModify);
    }
}