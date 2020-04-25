using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swihoni.Sessions.Player.Visualization
{
    public abstract class PlayerVisualsBehaviorBase : MonoBehaviour
    {
        internal virtual void Setup() { }

        internal virtual void Cleanup() { }

        public abstract void Render(Container playerContainer, bool isLocalPlayer);
    }

    [SelectionBase]
    public class PlayerVisualsDispatcherBehavior : MonoBehaviour, IPlayerContainerRenderer
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
            m_Camera.enabled = false;
            SetVisible(false, false);
        }

        private void OnDestroy()
        {
            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Cleanup();
        }

        public void Render(Container playerContainer, bool isLocalPlayer)
        {
            var health = playerContainer.Require<HealthProperty>();
            bool isVisible = health.HasValue && health.IsAlive;
            if (isVisible)
            {
                if (playerContainer.Has(out MoveComponent moveComponent))
                    transform.position = moveComponent.position;
                if (playerContainer.Has(out CameraComponent cameraComponent))
                {
                    transform.rotation = Quaternion.AngleAxis(cameraComponent.yaw, Vector3.up);
                    m_Head.localRotation = Quaternion.AngleAxis(cameraComponent.pitch, Vector3.right);
                    m_Camera.transform.localRotation = Quaternion.AngleAxis(cameraComponent.pitch, Vector3.right);
                    m_Camera.enabled = isLocalPlayer;
                }
                foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Render(playerContainer, isLocalPlayer);
            }
            SetVisible(isVisible, isVisible && isLocalPlayer);
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

        public void Dispose()
        {
            if (this)
                Destroy(gameObject);
        }
    }
}