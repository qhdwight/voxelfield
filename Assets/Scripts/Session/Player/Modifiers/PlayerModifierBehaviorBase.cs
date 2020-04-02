using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Modifiers
{
    public abstract class PlayerModifierBehaviorBase : MonoBehaviour
    {
        internal virtual void Setup()
        {
        }

        /// <summary>
        ///     Called in FixedUpdate() based on game tick rate
        /// </summary>
        internal virtual void ModifyChecked(PlayerComponent componentToModify, PlayerCommands commands)
        {
            SynchronizeBehavior(componentToModify);
        }

        /// <summary>
        ///     Called in Update() right after inputs are sampled
        /// </summary>
        internal virtual void ModifyTrusted(PlayerComponent componentToModify, PlayerCommands commands)
        {
            SynchronizeBehavior(componentToModify);
        }

        internal virtual void ModifyCommands(PlayerCommands commandsToModify)
        {
        }

        protected virtual void SynchronizeBehavior(PlayerComponent componentToApply)
        {
        }
    }
}