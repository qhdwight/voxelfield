using System;
using Session.Player.Components;

namespace Session.Player.Visualization
{
    public class PlayerItemManagerVisualsBehavior : PlayerVisualsBehaviorBase
    {
        private PlayerItemAnimatorBehavior[] m_ItemAnimators;

        internal override void Setup()
        {
            m_ItemAnimators = GetComponentsInChildren<PlayerItemAnimatorBehavior>(true);
            ForEachItemAnimator(animator => animator.Setup());
        }

        public override void Visualize(PlayerComponent playerComponent, bool isLocalPlayer)
        {
            ForEachItemAnimator(animator => animator.Visualize(playerComponent, isLocalPlayer));
        }

        internal override void Cleanup()
        {
            ForEachItemAnimator(animator => animator.Cleanup());
        }

        private void ForEachItemAnimator(Action<PlayerItemAnimatorBehavior> action)
        {
            foreach (PlayerItemAnimatorBehavior animator in m_ItemAnimators)
                action(animator);
        }
    }
}