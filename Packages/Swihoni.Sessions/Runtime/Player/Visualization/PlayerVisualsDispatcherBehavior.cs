using System;
using System.Text;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using TMPro;
using UnityEngine;

namespace Swihoni.Sessions.Player.Visualization
{
    public abstract class PlayerVisualsBehaviorBase : MonoBehaviour, IDisposable
    {
        internal virtual void Setup() { }

        public virtual void Dispose() { }

        public abstract void Render(SessionBase session, Container player, bool isLocalPlayer);

        public virtual void SetActive(bool isActive) { }
    }

    [SelectionBase]
    public class PlayerVisualsDispatcherBehavior : VisualBehaviorBase, IDisposable
    {
        [SerializeField] private float m_UprightCameraHeight = 1.8f, m_CrouchedCameraHeight = 1.26f;
        [SerializeField] private AudioSource m_DamageNotifierSource = default;
        [SerializeField] private TextMeshPro m_DamageText = default, m_UsernameText = default;

        private AudioListener m_AudioListener;
        private Camera m_Camera;
        private PlayerVisualsBehaviorBase[] m_Visuals;
        private readonly StringBuilder m_UsernameBuilder = new StringBuilder();

        private Container m_RecentRender;

        public Container GetRecentPlayer() => m_RecentRender;

        internal override void Setup(IBehaviorManager manager)
        {
            base.Setup(manager);
            m_Visuals = GetComponents<PlayerVisualsBehaviorBase>();
            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Setup();
            m_Camera = GetComponentInChildren<Camera>();
            m_AudioListener = GetComponentInChildren<AudioListener>();
        }

        private readonly StringBuilder m_DamageNotifierBuilder = new StringBuilder();

        private static Vector3 GetPosition(Container session, int playerId) => SessionBase.GetPlayerEyePosition(session.GetPlayer(playerId).Require<MoveComponent>());

        public void Render(SessionBase session, Container sessionContainer, int playerId, Container player, bool isLocalPlayer)
        {
            bool usesHealth = player.With(out HealthProperty health),
                 showDamageNotifier = player.With(out DamageNotifierComponent damageNotifier) && !isLocalPlayer,
                 isVisible = !usesHealth || health.WithValue,
                 showUsername = false;

            if (isVisible)
            {
                if (player.With(out CameraComponent playerCamera))
                    m_Camera.transform.localRotation = Quaternion.AngleAxis(playerCamera.yaw, Vector3.up)
                                                     * Quaternion.AngleAxis(playerCamera.pitch, Vector3.right);
                if (player.With(out MoveComponent move))
                    m_Camera.transform.position = move.position + new Vector3 {y = Mathf.Lerp(m_CrouchedCameraHeight, m_UprightCameraHeight, 1.0f - move.normalizedCrouch)};
                Container localPlayer = sessionContainer.GetPlayer(sessionContainer.Require<LocalPlayerId>());

                if (showDamageNotifier)
                {
                    // TODO:refactor remove magic number, relying on internal state of audio source here... BAD!
                    if (damageNotifier.elapsedUs > 1_900_000u)
                    {
                        if (!m_DamageNotifierSource.isPlaying) m_DamageNotifierSource.Play();
                        // if (m_LastDamageNotifierElapsed < Mathf.Epsilon)
                        //     m_DamageNotifierSource.PlayOneShot(m_DamageNotifierSource.clip);
                    }
                    else m_DamageNotifierSource.Stop();

                    if (m_DamageText)
                    {
                        Color color = Color.Lerp(Color.green, Color.red, damageNotifier.damage / 100f);
                        color.a = Mathf.Lerp(0.0f, 1.0f, damageNotifier.elapsedUs / 2_000_000f);
                        m_DamageText.color = color;

                        if (damageNotifier.elapsedUs > 0u)
                        {
                            LookAtPlayer(m_DamageText, sessionContainer, playerId, new Vector3 {y = 0.2f});
                            m_DamageNotifierBuilder.Clear().Append(damageNotifier.damage.Value).Commit(m_DamageText);
                        }
                    }
                }

                showUsername = !isLocalPlayer && localPlayer.Require<TeamProperty>() == player.Require<TeamProperty>();
                if (showUsername)
                {
                    LookAtPlayer(m_UsernameText, sessionContainer, playerId, new Vector3 {y = 0.2f});
                    m_UsernameBuilder.Clear();
                    ModeManager.GetMode(sessionContainer).BuildUsername(m_UsernameBuilder, player).Commit(m_UsernameText);
                }
            }
            m_UsernameText.enabled = showUsername;
            m_DamageText.enabled = showDamageNotifier;

            SetCameraVisible(isLocalPlayer);

            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Render(session, player, isLocalPlayer);

            m_RecentRender = player;
        }

        private static void LookAtPlayer(TMP_Text text, Container session, int playerId, in Vector3 offset)
        {
            Vector3 localPosition = GetPosition(session, session.Require<LocalPlayerId>()),
                    textPosition = GetPosition(session, playerId) + offset;
            float distanceMultiplier = Mathf.Clamp(Vector3.Distance(localPosition, textPosition) * 0.05f, 1.0f, 5.0f);
            var localScale = new Vector3(-distanceMultiplier, distanceMultiplier, 1.0f);
            Transform t = text.transform;
            t.localScale = localScale;
            t.position = textPosition;
            t.LookAt(localPosition);
        }

        private void SetCameraVisible(bool isCameraEnabled)
        {
            m_AudioListener.enabled = isCameraEnabled;
            m_Camera.enabled = isCameraEnabled;
        }

        public override void SetActive(bool isActive)
        {
            if (!isActive) SetCameraVisible(false);
            if (m_Visuals != null)
                foreach (PlayerVisualsBehaviorBase visual in m_Visuals)
                    visual.SetActive(isActive);
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