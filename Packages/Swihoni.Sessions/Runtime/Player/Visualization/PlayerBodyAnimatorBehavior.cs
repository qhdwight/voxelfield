using System;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util.Math;
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
        [SerializeField] private Renderer[] m_TpvRenders = default;
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
        private float m_LastNormalizedTime;

        internal override void Setup()
        {
            if (m_Graph.IsValid()) return;

            base.Setup();

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

        private PlayerMovement m_PrefabPlayerMovement; // TODO:refactor make parameter

        public override void Render(SessionBase session, Container player, bool isLocalPlayer)
        {
            var modifierPrefab = (PlayerModifierDispatcherBehavior) session.PlayerManager.GetModifierPrefab(player.Require<ByteIdProperty>());
            m_PrefabPlayerMovement = modifierPrefab.Movement;

            bool withHealth = player.With(out HealthProperty health),
                 withMove = player.With(out MoveComponent move),
                 isVisible = !withHealth || health.WithValue;

            bool isInFpv = isLocalPlayer && (!withHealth || health.IsActiveAndAlive);

            foreach (Renderer render in m_TpvRenders)
            {
                render.enabled = isVisible;
                render.shadowCastingMode = isInFpv ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }

            bool isAnimatorEnabled = false, isRagdollEnabled = false;
            CameraComponent playerCamera = default;
            if (isVisible)
            {
                isAnimatorEnabled = health.IsAlive && player.With(out playerCamera) && withMove && move.position.WithValue;
                isRagdollEnabled = health.IsDead && withMove && move.position.WithValue;
            }

            if (m_RagdollRigidbodies != null) SetRagdollEnabled(isRagdollEnabled);
            m_Animator.enabled = isAnimatorEnabled;
            if (isAnimatorEnabled)
            {
                m_Head.localRotation = Quaternion.AngleAxis(playerCamera.pitch, Vector3.right);
                m_Animator.transform.SetPositionAndRotation(move.position, Quaternion.AngleAxis(playerCamera.yaw, Vector3.up));
                RenderMove(move);
            }
        }

        public override void SetActive(bool isActive)
        {
            if (isActive) return;
            foreach (Renderer render in m_TpvRenders) render.enabled = false;
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
                partCollider.enabled = isActive;
        }

        private void RenderState(int baseIndex, MoveComponent move, float weight)
        {
            bool isStationary = ExtraMath.LateralMagnitude(move.velocity) < 1e-2f,
                 isGrounded = move.groundTick >= 1;

            const int idleOffset = 0, moveOffset = 1, inAirOffset = 2;

            if (isStationary)
            {
                for (var i = 0; i < 3; i++)
                    m_Mixer.SetInputWeight(baseIndex + i, i == idleOffset ? weight : 0.0f);
            }
            else
            {
                if (isGrounded)
                {
                    float normalizedSpeed = Mathf.Clamp01(ExtraMath.LateralMagnitude(move.velocity) / m_PrefabPlayerMovement.MaxSpeed);
                    for (var i = 0; i < 3; i++)
                    {
                        // TODO:refactor confusing
                        m_Mixer.SetInputWeight(baseIndex + i, i == idleOffset
                                                   ? (1.0f - normalizedSpeed) * weight
                                                   : i == moveOffset
                                                       ? normalizedSpeed * weight
                                                       : 0.0f);
                    }
                    float clipTimeSeconds = move.normalizedMove * m_StatusVisualProperties[baseIndex + moveOffset].clip.length;
                    m_Animations[baseIndex + moveOffset].SetTime(clipTimeSeconds);
                }
                else
                {
                    for (var i = 0; i < 3; i++)
                        m_Mixer.SetInputWeight(baseIndex + i, i == inAirOffset ? weight : 0.0f);
                }
            }
        }

        private void RenderMove(MoveComponent move)
        {
            RenderState(0, move, 1.0f - move.normalizedCrouch);
            RenderState(3, move, move.normalizedCrouch);
            if (m_FootstepSource) Footsteps(move);

            m_Graph.Evaluate();
        }

        // TODO:refactor magic numbers
        private void Footsteps(MoveComponent move)
        {
            float normalizedSpeed = Mathf.Clamp01(ExtraMath.LateralMagnitude(move.velocity) / m_PrefabPlayerMovement.MaxSpeed);

            if (normalizedSpeed > 0.5f)
            {
                if (move.normalizedMove > 0.25f && m_LastNormalizedTime <= 0.25f || move.normalizedMove > 0.75f && m_LastNormalizedTime <= 0.75f)
                {
                    int count = Physics.RaycastNonAlloc(m_FootstepSource.transform.position + new Vector3 {y = 0.5f}, Vector3.down, m_CachedHits,
                                                        1.0f, m_PrefabPlayerMovement.GroundMask);
                    m_FootstepSource.pitch = Random.Range(0.95f, 1.05f);
                    if (count >= 1)
                        m_FootstepSource.PlayOneShot(m_BrushClips[Random.Range(0, m_BrushClips.Length)], normalizedSpeed);
                }
            }
            m_LastNormalizedTime = move.normalizedMove;
        }

        public override void Dispose()
        {
            if (m_Graph.IsValid()) m_Graph.Destroy();
        }
    }
}