using UnityEngine;

namespace Session.Player
{
    public abstract class PlayerModifierBehaviorBase : MonoBehaviour
    {
        internal virtual void Setup()
        {
        }

        /// <summary>
        /// Called in FixedUpdate() based on game tick rate
        /// </summary>
        internal virtual void ModifyChecked(PlayerState stateToModify, PlayerCommands commands)
        {
            SynchronizeBehavior(stateToModify);
        }

        /// <summary>
        /// Called in Update() right after inputs are sampled
        /// </summary>
        internal virtual void ModifyTrusted(PlayerState stateToModify, PlayerCommands commands)
        {
            SynchronizeBehavior(stateToModify);
        }

        internal virtual void ModifyCommands(PlayerCommands commandsToModify)
        {
        }

        protected virtual void SynchronizeBehavior(PlayerState stateToApply)
        {
        }
    }
}