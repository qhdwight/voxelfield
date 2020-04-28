using System;
using System.Linq;
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
        private Rigidbody[] m_RagdollRigidbodies;
        private Collider[] m_RagdollColliders;
        private (Vector3 position, Quaternion rotation)[] m_RagdollInitialTransforms;

        public void Setup(SessionBase session)
        {
            m_Visuals = GetComponents<PlayerVisualsBehaviorBase>();
            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Setup(session);
            m_Camera = GetComponentInChildren<Camera>();
            m_AudioListener = GetComponentInChildren<AudioListener>();
            m_RagdollRigidbodies = GetComponentsInChildren<Rigidbody>();
            m_RagdollColliders = GetComponentsInChildren<Collider>();
            m_RagdollInitialTransforms = m_RagdollRigidbodies.Select(r => (r.transform.localPosition, r.transform.localRotation)).ToArray();
            SetVisible(false, false);
        }

        private void SetRagdollEnabled(bool isActive)
        {
            for (var i = 0; i < m_RagdollRigidbodies.Length; i++)
            {
                Rigidbody part = m_RagdollRigidbodies[i];
                part.isKinematic = !isActive;
                if (isActive) continue;
                Transform partTransform = part.transform;
                partTransform.localPosition = m_RagdollInitialTransforms[i].position;
                partTransform.localRotation = m_RagdollInitialTransforms[i].rotation;
                part.velocity = Vector3.zero;
                part.angularVelocity = Vector3.zero;
            }
            foreach (Collider partCollider in m_RagdollColliders)
            {
                partCollider.enabled = isActive;
            }
        }

        public void Render(int playerId, Container player, bool isLocalPlayer)
        {
            bool usesHealth = player.Has(out HealthProperty health),
                 isVisible = !usesHealth || health.HasValue;
            if (isVisible)
            {
                SetRagdollEnabled(health.IsDead);
                
                if (player.Has(out CameraComponent cameraComponent))
                    m_Camera.transform.localRotation = Quaternion.AngleAxis(cameraComponent.pitch, Vector3.right);
            }
            else
                SetRagdollEnabled(false);

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