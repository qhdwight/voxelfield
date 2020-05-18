using System;
using Swihoni.Components;
using Swihoni.Sessions.Components;
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
        [SerializeField] private float m_UprightCameraHeight = 1.8f, m_CrouchedCameraHeight = 1.26f;
        [SerializeField] private AudioSource m_DamageNotifierSource = default;
        private float m_LastDamageNotifierElapsed;

        private AudioListener m_AudioListener;
        private Camera m_Camera;
        private PlayerVisualsBehaviorBase[] m_Visuals;

        private Container m_RecentRender;

        public Container GetRecentPlayer() => m_RecentRender;

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
            bool usesHealth = player.With(out HealthProperty health),
                 usesDamageNotifier = player.With(out DamageNotifierComponent damageNotifier),
                 isVisible = !usesHealth || health.WithValue;
            if (isVisible)
            {
                if (player.With(out CameraComponent playerCamera))
                    m_Camera.transform.localRotation = Quaternion.AngleAxis(playerCamera.yaw, Vector3.up)
                                                     * Quaternion.AngleAxis(playerCamera.pitch, Vector3.right);
                if (player.With(out MoveComponent move))
                    m_Camera.transform.position = move.position + new Vector3 {y = Mathf.Lerp(m_CrouchedCameraHeight, m_UprightCameraHeight, 1.0f - move.normalizedCrouch)};
                if (usesDamageNotifier)
                {
                    // TODO:refactor remove magic number, relying on internal state of audio source here... BAD!
                    if (damageNotifier.elapsed > 0.9f)
                    {
                        if (!m_DamageNotifierSource.isPlaying) m_DamageNotifierSource.Play();
                        // if (m_LastDamageNotifierElapsed < Mathf.Epsilon)
                        //     m_DamageNotifierSource.PlayOneShot(m_DamageNotifierSource.clip);
                    }
                    else
                        m_DamageNotifierSource.Stop();
                    m_LastDamageNotifierElapsed = damageNotifier.elapsed;
                }
            }

            SetVisible(isVisible, isLocalPlayer);

            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Render(player, isLocalPlayer);

            m_RecentRender = player;
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