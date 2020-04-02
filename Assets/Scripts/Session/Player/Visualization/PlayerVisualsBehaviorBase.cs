using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Visualization
{
    public abstract class PlayerVisualsBehaviorBase : MonoBehaviour
    {
        internal virtual void Setup()
        {
        }

        internal virtual void Cleanup()
        {
        }

        public virtual void Visualize(PlayerComponent playerComponent, bool isLocalPlayer)
        {
        }
    }
}