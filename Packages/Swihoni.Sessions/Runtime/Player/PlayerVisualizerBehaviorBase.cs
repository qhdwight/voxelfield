using System;
using Swihoni.Sessions.Player.Visualization;
using UnityEngine;

namespace Swihoni.Sessions.Player
{
    public abstract class PlayerVisualizerBehaviorBase : MonoBehaviour, IDisposable
    {
        public PlayerBodyAnimatorBehavior BodyAnimator { get; private set; }
        public PlayerItemAnimatorBehavior ItemAnimator { get; private set; }

        public int PlayerId { get; private set; }

        internal virtual void Setup()
        {
            BodyAnimator = GetComponent<PlayerBodyAnimatorBehavior>();
            ItemAnimator = GetComponentInChildren<PlayerItemAnimatorBehavior>();
            BodyAnimator.Setup();
            ItemAnimator.Setup();
        }

        public virtual void Evaluate(in SessionContext context)
        {
            PlayerId = context.playerId;
            if (BodyAnimator) BodyAnimator.Render(context, false);
            if (ItemAnimator) ItemAnimator.Render(context, false);
        }

        public void Dispose()
        {
            BodyAnimator.Dispose();
            ItemAnimator.Dispose();
        }
    }
}