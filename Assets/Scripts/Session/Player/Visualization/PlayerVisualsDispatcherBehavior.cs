using Session.Player.Components;
using UnityEngine;
using UnityEngine.Rendering;

namespace Session.Player.Visualization
{
    [SelectionBase]
    public class PlayerVisualsDispatcherBehavior : MonoBehaviour
    {
        private AudioListener m_AudioListener;

        private Camera m_Camera;
        [SerializeField] private Transform m_Head = default;
        [SerializeField] private Renderer[] m_Renders = default;
        [SerializeField] private bool m_IsDebugRender = default;

        private PlayerVisualsBehaviorBase[] m_Visuals;

        public void Setup()
        {
            m_Visuals = GetComponents<PlayerVisualsBehaviorBase>();
            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Setup();
            m_Camera = GetComponentInChildren<Camera>();
            m_AudioListener = GetComponentInChildren<AudioListener>();
            SetVisible(false, false);
        }

        private void OnDestroy()
        {
            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Cleanup();
        }

        public void Visualize(PlayerComponent playerComponent, bool isLocalPlayer)
        {
            transform.position = playerComponent.position;
            transform.rotation = Quaternion.AngleAxis(playerComponent.yaw, Vector3.up);
            m_Head.localRotation = Quaternion.AngleAxis(playerComponent.pitch, Vector3.right);
            m_Camera.transform.localRotation = Quaternion.AngleAxis(playerComponent.pitch, Vector3.right);
            m_Camera.enabled = isLocalPlayer;
            SetVisible(playerComponent.IsAlive, isLocalPlayer && playerComponent.IsAlive);
            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Visualize(playerComponent, isLocalPlayer);
        }

        private void SetVisible(bool isVisible, bool isListenerEnabled)
        {
            m_AudioListener.enabled = isListenerEnabled;
            foreach (Renderer render in m_Renders)
            {
                render.enabled = isVisible;
                render.shadowCastingMode = isListenerEnabled && !m_IsDebugRender ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }
            gameObject.hideFlags = isVisible ? HideFlags.None : HideFlags.HideInHierarchy;
        }
    }
}