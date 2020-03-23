using UnityEngine;

namespace Session.Player
{
    public class PlayerCamera : MonoBehaviour, IPlayerModifier
    {
        [SerializeField] private float m_Sensitivity = 10.0f;

        private float m_Pitch, m_Yaw;

        public void Modify(PlayerData data, PlayerCommands commands)
        {
            m_Pitch -= commands.mouseY * m_Sensitivity;
            m_Yaw += commands.mouseX * m_Sensitivity;
            data.pitch = m_Pitch;
            data.yaw = m_Yaw;
            transform.rotation = Quaternion.AngleAxis(data.yaw, Vector3.up);
        }
    }
}