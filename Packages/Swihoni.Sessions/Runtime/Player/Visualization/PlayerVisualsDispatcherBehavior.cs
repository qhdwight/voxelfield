using System;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Visualization
{
    public abstract class PlayerVisualsBehaviorBase : MonoBehaviour, IDisposable
    {
        internal virtual void Setup(SessionBase session) { }

        public virtual void Dispose() { }

        public abstract void Render(Container player, bool isLocalPlayer);
    }

    [SelectionBase]
    public class PlayerVisualsDispatcherBehavior : MonoBehaviour, IPlayerContainerRenderer
    {
        private AudioListener m_AudioListener;
        private Camera m_Camera;
        private PlayerVisualsBehaviorBase[] m_Visuals;

        public void Setup(SessionBase session)
        {
            m_Visuals = GetComponents<PlayerVisualsBehaviorBase>();
            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Setup(session);
            m_Camera = GetComponentInChildren<Camera>();
            m_AudioListener = GetComponentInChildren<AudioListener>();
            SetVisible(false, false);
        }

        public void Render(int playerId, Container player, bool isLocalPlayer)
        {
            bool usesHealth = player.Has(out HealthProperty health),
                 isVisible = !usesHealth || health.HasValue;
            if (isVisible)
            {
                if (player.Has(out CameraComponent playerCamera))
                {
                    m_Camera.transform.localRotation = Quaternion.AngleAxis(playerCamera.yaw, Vector3.up)
                                                     * Quaternion.AngleAxis(playerCamera.pitch, Vector3.right);
                }
                if (player.Has(out MoveComponent move))
                {
                    // TODO:refactor magic numbers
                    m_Camera.transform.position = move.position + new Vector3 {y = Mathf.Lerp(1.26f, 1.8f, 1.0f - move.normalizedCrouch)};
                }
            }

            SetVisible(isVisible, isLocalPlayer);

            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Render(player, isLocalPlayer);
        }

        private void SetVisible(bool isVisible, bool isCameraEnabled)
        {
            m_AudioListener.enabled = isCameraEnabled;
            m_Camera.enabled = isCameraEnabled;

            gameObject.hideFlags = isVisible ? HideFlags.None : HideFlags.HideInHierarchy;
        }

        public void Dispose()
        {
            if (m_Visuals != null)
                foreach (PlayerVisualsBehaviorBase visual in m_Visuals)
                    visual.Dispose();
            Destroy(gameObject);
        }
    }
}