using System;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Swihoni.Sessions.Player.Visualization
{
    [Serializable]
    public struct PlayerVisualBodyState
    {
        public AnimationClip clip;
    }

    public class PlayerBodyAnimatorBehavior : PlayerVisualsBehaviorBase
    {
        [SerializeField] private Transform m_Head = default;
        [SerializeField] private Renderer[] m_FpvRenders = default;
        [SerializeField] private PlayerVisualBodyState[] m_StatusVisualProperties = default;
        [SerializeField] private AudioSource m_FootstepSource = default;
        [SerializeField] private AudioClip[] m_BrushClips = default;
        [SerializeField] private Animator m_Animator = default;
        [SerializeField] private bool m_IsHitbox = default;
        private readonly RaycastHit[] m_CachedHits = new RaycastHit[1];
        private Rigidbody[] m_RagdollRigidbodies;
        private Collider[] m_RagdollColliders;
        private (Vector3 position, Quaternion rotation)[] m_RagdollInitialTransforms;
        private PlayableGraph m_Graph;
        private AnimationClipPlayable[] m_Animations;
        private AnimationMixerPlayable m_Mixer;
        private PlayerMovement m_PlayerMovement;
        private float m_LastNormalizedTime;

        internal override void Setup(SessionBase session)
        {
            base.Setup(session);
            if (m_Graph.IsValid()) return;

            m_PlayerMovement = session.PlayerModifierPrefab.GetComponent<PlayerMovement>();
            m_Graph = PlayableGraph.Create("Body Animator");
            m_Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            m_Mixer = AnimationMixerPlayable.Create(m_Graph, m_StatusVisualProperties.Length);
            m_Animations = new AnimationClipPlayable[m_StatusVisualProperties.Length];
            for (var visualStateIndex = 0; visualStateIndex < m_StatusVisualProperties.Length; visualStateIndex++)
            {
                m_Animations[visualStateIndex] = AnimationClipPlayable.Create(m_Graph, m_StatusVisualProperties[visualStateIndex].clip);
                m_Graph.Connect(m_Animations[visualStateIndex], 0, m_Mixer, visualStateIndex);
            }

            var output = AnimationPlayableOutput.Create(m_Graph, "Body Output", m_Animator);
            output.SetSourcePlayable(m_Mixer);
            /* Ragdoll */
            if (m_IsHitbox) return;
            m_RagdollRigidbodies = GetComponentsInChildren<Rigidbody>();
            m_RagdollColliders = GetComponentsInChildren<Collider>();
            m_RagdollInitialTransforms = m_RagdollRigidbodies.Select(r => (r.transform.localPosition, r.transform.localRotation)).ToArray();
        }

        public override void Render(Container player, bool isLocalPlayer)
        {
            bool usesHealth = player.Has(out HealthProperty health),
                 isVisible = !usesHealth || health.HasValue;

            bool isInFpv = isLocalPlayer && (!usesHealth || health.HasValue && health.IsAlive);

            foreach (Renderer render in m_FpvRenders)
            {
                render.enabled = isVisible;
                render.shadowCastingMode = isInFpv ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }

            bool isAnimatorEnabled;
            if (isVisible)
            {
                isAnimatorEnabled = health.IsAlive;
                if (player.Has(out MoveComponent move))
                {
                    m_Animator.transform.position = move.position;
                    if (m_RagdollRigidbodies != null) SetRagdollEnabled(health.IsDead);
                    RenderMove(move);
                }
                if (player.Has(out CameraComponent playerCamera))
                {
                    m_Animator.transform.rotation = Quaternion.AngleAxis(playerCamera.yaw, Vector3.up);
                    m_Head.localRotation = Quaternion.AngleAxis(playerCamera.pitch, Vector3.right);
                }
            }
            else
            {
                if (m_RagdollRigidbodies != null) SetRagdollEnabled(false);
                isAnimatorEnabled = true;
            }

            m_Animator.enabled = isAnimatorEnabled;
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

        private void RenderMove(MoveComponent move)
        {
            bool isStationary = VectorMath.LateralMagnitude(move.velocity) < 1e-2f,
                 isGrounded = move.groundTick >= 1;

            byte animationBaseIndex = 0;
            if (move.stateId == PlayerMovement.Crouched) animationBaseIndex += 3;

            const int moveOffset = 1, inAirOffset = 2;

            if (isStationary)
            {
                for (var i = 0; i < m_Animations.Length; i++)
                    m_Mixer.SetInputWeight(i, i == animationBaseIndex ? 1.0f : 0.0f);
            }
            else
            {
                if (isGrounded)
                {
                    // TODO:refactor
                    float normalizedSpeed = Mathf.Clamp01(VectorMath.LateralMagnitude(move.velocity) / m_PlayerMovement.MaxSpeed),
                          normalizedState = Mathf.Clamp01(move.moveElapsed / m_PlayerMovement.WalkStateDuration);
                    for (var i = 0; i < m_Animations.Length; i++)
                    {
                        m_Mixer.SetInputWeight(i, i == animationBaseIndex
                                                   ? 1.0f - normalizedSpeed
                                                   : i == animationBaseIndex + moveOffset ? normalizedSpeed : 0.0f);
                    }
                    float clipTimeSeconds = normalizedState * m_StatusVisualProperties[animationBaseIndex + moveOffset].clip.length;
                    m_Animations[animationBaseIndex + moveOffset].SetTime(clipTimeSeconds);
                    if (normalizedState > 0.25f && m_LastNormalizedTime <= 0.25f || normalizedState > 0.75f && m_LastNormalizedTime <= 0.75f)
                    {
                        if (m_FootstepSource)
                        {
                            int count = Physics.RaycastNonAlloc(transform.position + new Vector3 {y = 0.5f}, Vector3.down, m_CachedHits,
                                                                1.0f, m_PlayerMovement.GroundMask);
                            if (count >= 1)
                                m_FootstepSource.PlayOneShot(m_BrushClips[Random.Range(0, m_BrushClips.Length)]);
                        }
                    }
                    m_LastNormalizedTime = normalizedState;
                }
                else
                {
                    for (var i = 0; i < m_Animations.Length; i++)
                        m_Mixer.SetInputWeight(i, i == animationBaseIndex + inAirOffset ? 1.0f : 0.0f);
                }
            }
            m_Graph.Evaluate();
        }

        public override void Dispose()
        {
            base.Dispose();
            if (m_Graph.IsValid()) m_Graph.Destroy();
        }
    }
}