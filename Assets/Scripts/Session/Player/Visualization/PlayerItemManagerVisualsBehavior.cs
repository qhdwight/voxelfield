using System;
using Components;

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

        public override void Render(ContainerBase playerContainer, bool isLocalPlayer)
        {
            ForEachItemAnimator(animator => animator.Render(playerContainer, isLocalPlayer));
        }

        internal override void Cleanup()
        {
            ForEachItemAnimator(animator => animator.Cleanup());
        }

        private void ForEachItemAnimator(Action<PlayerItemAnimatorBehavior> action)
        {
            foreach (PlayerItemAnimatorBehavior animator in m_ItemAnimators)
                if (animator.gameObject.activeSelf)
                    action(animator);
        }
    }
}