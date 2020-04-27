using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swihoni.Sessions.Player.Visualization
{
    public class PlayerBodyAnimatorBehavior : PlayerVisualsBehaviorBase
    {
        [SerializeField] private Transform m_Head = default;
        [SerializeField] private Renderer[] m_FpvRenders = default;

        public override void Render(int playerId, Container player, bool isLocalPlayer)
        {
            bool usesHealth = player.Has(out HealthProperty health),
                 isVisible = !usesHealth || health.HasValue;
            
            bool isInFpv = isLocalPlayer && (!usesHealth || health.HasValue && health.IsAlive);

            foreach (Renderer render in m_FpvRenders)
            {
                render.enabled = isVisible;
                render.shadowCastingMode = isInFpv ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }

            if (isVisible)
            {
                if (player.Has(out MoveComponent moveComponent))
                    transform.position = moveComponent.position;
                if (player.Has(out CameraComponent cameraComponent))
                {
                    transform.rotation = Quaternion.AngleAxis(cameraComponent.yaw, Vector3.up);
                    m_Head.localRotation = Quaternion.AngleAxis(cameraComponent.pitch, Vector3.right);
                }
            }
        }
    }
}