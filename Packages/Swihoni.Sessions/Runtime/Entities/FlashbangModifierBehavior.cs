using Swihoni.Sessions.Components;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class FlashbangModifierBehavior : ThrowableModifierBehavior
    {
        [SerializeField] private AnimationCurve m_DistanceCurve = AnimationCurve.Linear(0.0f, 1.0f, 200.0f, 0.0f);
        
        protected override void JustPopped(in SessionContext context)
        {
            Vector3 center = transform.position;
            context.ForEachActivePlayer((in SessionContext playerContext) =>
            {
                Ray playerEye = playerContext.player.GetRayForPlayer();
                Vector3 direction = center - playerEye.origin;
                float distance = direction.magnitude;
                direction.Normalize();
                float alignment = Mathf.Clamp(Vector3.Dot(direction, playerEye.direction), 0.0f, 1.0f) * m_DistanceCurve.Evaluate(distance);
                playerContext.player.Require<FlashProperty>().Value = alignment;
            });
        }
    }
}