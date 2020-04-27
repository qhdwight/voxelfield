using System;
using Swihoni.Components;

namespace Swihoni.Sessions.Player.Visualization
{
    public class PlayerItemManagerVisualsBehavior : PlayerVisualsBehaviorBase
    {
        private PlayerItemAnimatorBehavior[] m_ItemAnimators;

        internal override void Setup()
        {
            m_ItemAnimators = GetComponentsInChildren<PlayerItemAnimatorBehavior>(true);
            ForEachItemAnimator(animator => animator.Setup());
        }

        public override void Render(int playerId, Container player, bool isLocalPlayer)
        {
            ForEachItemAnimator(animator => animator.Render(player, isLocalPlayer));
        }

        internal override void Cleanup() { ForEachItemAnimator(animator => animator.Cleanup()); }

        private void ForEachItemAnimator(Action<PlayerItemAnimatorBehavior> action)
        {
            foreach (PlayerItemAnimatorBehavior animator in m_ItemAnimators)
                action(animator);
        }
    }
}