using System;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Visualization
{
    public class PlayerItemManagerVisualsBehavior : PlayerVisualsBehaviorBase
    {
        private PlayerItemAnimatorBehavior[] m_ItemAnimators;
        private LineRenderer m_Tracer;
        private GradientAlphaKey[] m_AlphaKeys;
        private GradientColorKey[] m_ColorKeys;
        private readonly Gradient m_Gradient = new Gradient();

        internal override void Setup()
        {
            m_ItemAnimators = GetComponentsInChildren<PlayerItemAnimatorBehavior>(true);
            m_Tracer = GetComponentInChildren<LineRenderer>();
            foreach (PlayerItemAnimatorBehavior animator in m_ItemAnimators) animator.Setup();
            Gradient colorGradient = m_Tracer.colorGradient;
            m_AlphaKeys = colorGradient.alphaKeys;
            m_ColorKeys = colorGradient.colorKeys;
        }

        public override void Render(SessionBase session, Container player, bool isLocalPlayer)
        {
            foreach (PlayerItemAnimatorBehavior animator in m_ItemAnimators) animator.Render(player, isLocalPlayer);
            if (!m_Tracer) return;
            var inventory = player.Require<InventoryComponent>();
            m_Tracer.SetPosition(0, inventory.tracerStart);
            m_Tracer.SetPosition(1, inventory.tracerEnd);
            m_Tracer.enabled = inventory.tracerTimeUs > 0;
            m_AlphaKeys[1].alpha = inventory.tracerTimeUs / 1_000_000.0f;
            m_AlphaKeys[2].alpha = inventory.tracerTimeUs / 1_000_000.0f;
            m_Gradient.SetKeys(m_ColorKeys, m_AlphaKeys);
            m_Tracer.colorGradient = m_Gradient;
        }

        public override void Dispose() => ForEachItemAnimator(animator => animator.Dispose());

        public override void SetActive(bool isActive)
        {
            foreach (PlayerItemAnimatorBehavior animator in m_ItemAnimators) animator.SetActive(isActive);
        }

        private void ForEachItemAnimator(Action<PlayerItemAnimatorBehavior> action)
        {
            foreach (PlayerItemAnimatorBehavior animator in m_ItemAnimators)
                action(animator);
        }
    }
}