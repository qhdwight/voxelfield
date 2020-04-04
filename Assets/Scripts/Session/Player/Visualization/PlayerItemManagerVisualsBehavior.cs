using System.Collections.Generic;
using Session.Items;
using Session.Items.Visuals;
using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Visualization
{
    public class PlayerItemManagerVisualsBehavior : PlayerVisualsBehaviorBase
    {
        [SerializeField] private PlayerItemAnimatorBehavior m_FpvItemAnimator = default, m_TpvItemAnimator = default;
        private List<PlayerItemAnimatorBehavior> m_ItemAnimators;

        internal override void Setup()
        {
            m_ItemAnimators = new List<PlayerItemAnimatorBehavior> {m_FpvItemAnimator, m_TpvItemAnimator};
            foreach (PlayerItemAnimatorBehavior animator in m_ItemAnimators) animator.Setup();
        }

        public override void Visualize(PlayerComponent playerComponent, bool isLocalPlayer)
        {
            foreach (PlayerItemAnimatorBehavior animator in m_ItemAnimators) animator.Visualize(playerComponent, isLocalPlayer);
        }

        internal override void Cleanup()
        {
            foreach (PlayerItemAnimatorBehavior animator in m_ItemAnimators) animator.Cleanup();
        }
    }
}