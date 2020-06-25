using System;
using Swihoni.Components;

namespace Swihoni.Sessions.Player.Visualization
{
    public class PlayerItemManagerVisualsBehavior : PlayerVisualsBehaviorBase
    {
        private PlayerItemAnimatorBehavior[] m_ItemAnimators;

        internal override void Setup(SessionBase session)
        {
            m_ItemAnimators = GetComponentsInChildren<PlayerItemAnimatorBehavior>(true);
            ForEachItemAnimator(animator => animator.Setup());
        }

        public override void Render(SessionBase session, Container player, bool isLocalPlayer) => ForEachItemAnimator(animator => animator.Render(player, isLocalPlayer));

        public override void Dispose() => ForEachItemAnimator(animator => animator.Dispose());

        private void ForEachItemAnimator(Action<PlayerItemAnimatorBehavior> action)
        {
            foreach (PlayerItemAnimatorBehavior animator in m_ItemAnimators)
                action(animator);
        }
    }
}