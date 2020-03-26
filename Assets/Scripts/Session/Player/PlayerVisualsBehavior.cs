using UnityEngine;

namespace Session.Player
{
    public class PlayerVisualsBehavior : MonoBehaviour
    {
        [SerializeField] private Transform m_Head = default;

        private Camera m_Camera;
        private AudioListener m_AudioListener;
        private Renderer[] m_Renders;

        public void Setup()
        {
            m_Camera = GetComponentInChildren<Camera>();
            m_AudioListener = GetComponentInChildren<AudioListener>();
            m_Renders = GetComponentsInChildren<Renderer>();
            SetVisible(false, false);
        }

        public void Visualize(PlayerState state, bool isLocalPlayer)
        {
            transform.position = state.position;
            transform.rotation = Quaternion.AngleAxis(state.yaw, Vector3.up);
            m_Head.localRotation = Quaternion.AngleAxis(state.pitch, Vector3.right);
            m_Camera.transform.localRotation = Quaternion.AngleAxis(state.pitch, Vector3.right);
            m_Camera.enabled = isLocalPlayer;
            SetVisible(state.isAlive, isLocalPlayer);
        }

        private void SetVisible(bool isVisible, bool isListenerEnabled)
        {
            m_AudioListener.enabled = isListenerEnabled;
            foreach (Renderer render in m_Renders)
            {
                render.enabled = isVisible;
                gameObject.hideFlags = isVisible ? HideFlags.None : HideFlags.HideInHierarchy;
            }
        }
    }
}