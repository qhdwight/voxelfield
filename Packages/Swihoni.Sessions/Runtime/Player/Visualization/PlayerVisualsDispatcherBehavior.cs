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

        public abstract void Render(in SessionContext context, bool isLocalPlayer);

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

        public void Render(in SessionContext context, bool isLocalPlayer)
        {
            Container player = context.player, sessionContainer = context.sessionContainer;
            bool withHealth = player.With(out HealthProperty health),
                 withMove = player.With(out MoveComponent move),
                 isVisible = !withHealth || health.WithValue,
                 showDamageNotifier = false,
                 showUsername = false;

            if (isVisible)
            {
                if (player.With(out CameraComponent playerCamera))
                    m_Camera.transform.localRotation = Quaternion.AngleAxis(playerCamera.yaw, Vector3.up)
                                                     * Quaternion.AngleAxis(playerCamera.pitch, Vector3.right);
                if (withMove && move.position.WithValue)
                    m_Camera.transform.position = move.position + new Vector3 {y = Mathf.Lerp(m_CrouchedCameraHeight, m_UprightCameraHeight, 1.0f - move.normalizedCrouch)};

                Container localPlayer = sessionContainer.GetPlayer(sessionContainer.Require<LocalPlayerId>());
                bool withNotifier = player.With(out DamageNotifierComponent damageNotifier) && damageNotifier.timeUs.WithValue && health.IsAlive;
                showUsername = !isLocalPlayer && health.IsAlive && localPlayer.Require<TeamProperty>() == player.Require<TeamProperty>();
                showDamageNotifier = m_DamageText && !isLocalPlayer && health.IsAlive && withNotifier;

                // TODO:refactor remove magic number, relying on internal state of audio source here... BAD!
                if (withNotifier && damageNotifier.timeUs > 1_900_000u)
                {
                    if (!m_DamageNotifierSource.isPlaying) m_DamageNotifierSource.Play();
                    // if (m_LastDamageNotifierElapsed < Mathf.Epsilon)
                    //     m_DamageNotifierSource.PlayOneShot(m_DamageNotifierSource.clip);
                }
                else m_DamageNotifierSource.Stop();

                if (showDamageNotifier)
                {
                    Color color = Color.Lerp(Color.green, Color.red, damageNotifier.damage / 100f);
                    color.a = Mathf.Lerp(0.0f, 1.0f, damageNotifier.timeUs / 2_000_000f);
                    m_DamageText.color = color;

                    if (damageNotifier.timeUs > 0u)
                    {
                        LookAtCamera(m_DamageText, context, new Vector3 {y = 0.2f});
                        m_DamageNotifierBuilder.Clear().Append(damageNotifier.damage.Value).Commit(m_DamageText);
                    }
                }

                if (showUsername)
                {
                    LookAtCamera(m_UsernameText, context, new Vector3 {y = 0.2f});
                    m_UsernameBuilder.Clear();
                    ModeManager.GetMode(sessionContainer).AppendUsername(m_UsernameBuilder, player).Commit(m_UsernameText);
                }
            }
            m_UsernameText.enabled = showUsername;
            m_DamageText.enabled = showDamageNotifier;

            SetCameraVisible(isLocalPlayer && move.position.WithValue);

            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.Render(context, isLocalPlayer);

            m_RecentRender = player;
        }

        private static void LookAtCamera(TMP_Text text, in SessionContext context, in Vector3 offset)
        {
            Vector3 cameraPosition = SessionBase.ActiveCamera.transform.position,
                    textPosition = context.player.Require<MoveComponent>().GetPlayerEyePosition() + offset;
            float distanceMultiplier = Mathf.Clamp(Vector3.Distance(cameraPosition, textPosition) * 0.05f, 1.0f, 5.0f);
            var localScale = new Vector3(-distanceMultiplier, distanceMultiplier, 1.0f);
            Transform t = text.transform;
            t.localScale = localScale;
            t.position = textPosition;
            t.LookAt(cameraPosition);
        }

        private void SetCameraVisible(bool isCameraEnabled)
        {
            m_AudioListener.enabled = isCameraEnabled;
            m_Camera.enabled = isCameraEnabled;
            SceneCamera.Singleton.Sync();
        }

        public override void SetActive(bool isActive)
        {
            if (!isActive) SetCameraVisible(false);
            if (m_Visuals == null) return;
            foreach (PlayerVisualsBehaviorBase visual in m_Visuals) visual.SetActive(isActive);
            m_UsernameText.enabled = isActive;
            m_DamageText.enabled = isActive;
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