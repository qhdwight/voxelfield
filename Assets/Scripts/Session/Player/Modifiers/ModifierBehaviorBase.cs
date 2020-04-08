using UnityEngine;

namespace Session.Player.Modifiers
{
    public abstract class ModifierBehaviorBase<TComponent> : MonoBehaviour, IModifierBase<TComponent>
    {
        internal virtual void Setup()
        {
        }

        /// <summary>
        ///     Called in FixedUpdate() based on game tick rate
        /// </summary>
        public virtual void ModifyChecked(TComponent componentToModify, PlayerCommandsComponent commands)
        {
            SynchronizeBehavior(componentToModify);
        }

        /// <summary>
        ///     Called in Update() right after inputs are sampled
        /// </summary>
        public virtual void ModifyTrusted(TComponent componentToModify, PlayerCommandsComponent commands)
        {
            SynchronizeBehavior(componentToModify);
        }

        public virtual void ModifyCommands(PlayerCommandsComponent commandsToModify)
        {
        }

        protected virtual void SynchronizeBehavior(TComponent componentToApply)
        {
        }
    }
}