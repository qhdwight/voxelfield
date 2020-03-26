using UnityEngine;

namespace Session.Player
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private float m_Sensitivity = 10.0f;

        private float m_Pitch, m_Yaw;

        public void Look(float mouseX, float mouseY, PlayerCommands commands)
        {
            m_Pitch -= mouseX * m_Sensitivity;
            m_Yaw += mouseY * m_Sensitivity;
            commands.pitch = m_Pitch;
            commands.yaw = m_Yaw;
        }
        
        public void Modify(PlayerState state, PlayerCommands commands)
        {
            state.pitch = commands.pitch;
            state.yaw = commands.yaw;
            SetYaw(state.yaw);
        }

        private void SetYaw(float yaw)
        {
            transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up);
        }
    }
}