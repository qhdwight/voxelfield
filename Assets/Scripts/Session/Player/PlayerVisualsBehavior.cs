using UnityEngine;

namespace Session.Player
{
    public class PlayerVisualsBehavior : MonoBehaviour
    {
        [SerializeField] private Transform m_Head = default;
        private Camera m_Camera;

        private void Awake()
        {
            m_Camera = GetComponentInChildren<Camera>();
        }

        public void Visualize(PlayerData data)
        {
            transform.position = data.position;
            transform.rotation = Quaternion.AngleAxis(data.yaw, Vector3.up);
            m_Head.localRotation = Quaternion.AngleAxis(data.pitch, Vector3.right);
            m_Camera.transform.localRotation = Quaternion.AngleAxis(data.pitch, Vector3.right);
        }

        public void SetVisible(bool isVisible)
        {
        }
    }
}