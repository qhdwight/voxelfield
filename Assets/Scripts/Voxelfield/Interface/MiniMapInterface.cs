using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Voxelfield.Interface
{
    public class MiniMapInterface : SessionInterfaceBehavior
    {
        [SerializeField] private Camera m_Camera = default;

        public override void SetInterfaceActive(bool isActive)
        {
            base.SetInterfaceActive(isActive);
            m_Camera.enabled = isActive;
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isVisible = ((ConfigManager) ConfigManagerBase.Singleton).enableMiniMap;
            if (isVisible && session.IsValidLocalPlayer(sessionContainer, out Container localPlayer)
                          && localPlayer.With(out MoveComponent move) && localPlayer.With(out CameraComponent cam))
            {
                Vector3 position = move.position + new Vector3 {y = 200.0f};
                Quaternion rotation = Quaternion.Euler(90.0f, 0.0f, -cam.yaw);
                m_Camera.transform.SetPositionAndRotation(position, rotation);
            }
            SetInterfaceActive(isVisible);
        }
    }
}