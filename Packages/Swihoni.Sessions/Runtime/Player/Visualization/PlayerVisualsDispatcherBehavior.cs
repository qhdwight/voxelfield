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
        private Rigidbody[] m_RagdollRigidbodies;

        public void Setup()
        {
            m_Visuals = GetComponents<PlayerVisualsBehaviorBase>();
            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Setup();
            m_Camera = GetComponentInChildren<Camera>();
            m_AudioListener = GetComponentInChildren<AudioListener>();
            m_RagdollRigidbodies = GetComponentsInChildren<Rigidbody>();
            SetVisible(false, false);
            SetRagdollEnabled(false);
        }

        private void SetRagdollEnabled(bool isActive)
        {
            foreach (Rigidbody part in m_RagdollRigidbodies)
            {
                part.isKinematic = !isActive;
                if (isActive) continue;
                part.velocity = Vector3.zero;
                part.angularVelocity = Vector3.zero;
            }
        }

        public void Render(Container player, bool isLocalPlayer)
        {
            bool isVisible = player.Without(out HealthProperty health) || health.HasValue && health.IsAlive;
            if (isVisible)
            {
                if (player.Has(out MoveComponent moveComponent))
                    transform.position = moveComponent.position;
                if (player.Has(out CameraComponent cameraComponent))
                {
                    transform.rotation = Quaternion.AngleAxis(cameraComponent.yaw, Vector3.up);
                    m_Head.localRotation = Quaternion.AngleAxis(cameraComponent.pitch, Vector3.right);
                    m_Camera.transform.localRotation = Quaternion.AngleAxis(cameraComponent.pitch, Vector3.right);
                }
            }
            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Render(player, isLocalPlayer);
            SetVisible(isVisible, isVisible && isLocalPlayer);
        }

        private void SetVisible(bool isVisible, bool isListenerEnabled)
        {
            m_AudioListener.enabled = isListenerEnabled;
            m_Camera.enabled = isListenerEnabled;
            foreach (Renderer render in m_Renders)
            {
                render.enabled = isVisible;
                render.shadowCastingMode = isListenerEnabled && !m_IsDebugRender ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }
            gameObject.hideFlags = isVisible ? HideFlags.None : HideFlags.HideInHierarchy;
        }
        
        public void Dispose()
        {
            if (m_Visuals != null)
                foreach (PlayerVisualsBehaviorBase visual in m_Visuals)
                    visual.Cleanup();
            if (this)
                Destroy(gameObject);
        }
    }
}