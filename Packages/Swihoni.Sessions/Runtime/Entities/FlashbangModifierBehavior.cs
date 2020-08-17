using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class FlashbangModifierBehavior : ThrowableModifierBehavior
    {
        [SerializeField] private AnimationCurve m_DistanceCurve = AnimationCurve.Linear(0.0f, 1.0f, 200.0f, 0.0f);
        [SerializeField] private LayerMask m_FlashMask = default;

        protected override void JustPopped(in SessionContext context, Container entity)
        {
            Vector3 center = transform.position;
            context.ForEachActivePlayer((in SessionContext playerContext) =>
            {
                Ray playerEye = playerContext.player.GetRayForPlayer();
                Vector3 direction = center - playerEye.origin;
                float distance = direction.magnitude;
                direction.Normalize();
                if (!Physics.Linecast(center, playerEye.origin, m_FlashMask))
                {
                    float alignment = Mathf.Clamp01(Vector3.Dot(direction, playerEye.direction)) * m_DistanceCurve.Evaluate(distance);
                    playerContext.player.Require<FlashProperty>().Value = alignment;   
                }
            });
        }
    }
}