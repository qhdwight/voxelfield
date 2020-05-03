using System;
using Swihoni.Components;
using Swihoni.Sessions.Player.Visualization;
using UnityEngine;

namespace Swihoni.Sessions.Player
{
    public abstract class PlayerVisualizerBase : MonoBehaviour, IDisposable
    {
        private PlayerBodyAnimatorBehavior m_BodyAnimator;
        private PlayerItemAnimatorBehavior m_ItemAnimator;

        public int PlayerId { get; private set; }

        internal virtual void Setup(SessionBase session)
        {
            m_BodyAnimator = GetComponent<PlayerBodyAnimatorBehavior>();
            m_ItemAnimator = GetComponentInChildren<PlayerItemAnimatorBehavior>();
            m_BodyAnimator.Setup(session);
            m_ItemAnimator.Setup();
        }

        public virtual void Evaluate(int playerId, Container player)
        {
            PlayerId = playerId;
            if (m_BodyAnimator) m_BodyAnimator.Render(player, false);
            if (m_ItemAnimator) m_ItemAnimator.Render(player, false);
        }

        public void Dispose()
        {
            m_BodyAnimator.Dispose();
            m_ItemAnimator.Dispose();
        }
    }
}