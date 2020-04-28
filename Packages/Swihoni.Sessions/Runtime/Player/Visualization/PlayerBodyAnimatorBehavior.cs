using System;
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
        private readonly RaycastHit[] m_CachedHits = new RaycastHit[1];
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

            if (isVisible)
            {
                if (player.Has(out MoveComponent move))
                {
                    transform.position = move.position;
                    AnimateBody(move);
                }
                if (player.Has(out CameraComponent playerCamera))
                {
                    transform.rotation = Quaternion.AngleAxis(playerCamera.yaw, Vector3.up);
                    m_Head.localRotation = Quaternion.AngleAxis(playerCamera.pitch, Vector3.right);
                }
            }
        }

        private void AnimateBody(MoveComponent move)
        {
            byte moveId = move.status.id;
            if (moveId == PlayerMovement.Moving)
            {
                float normalizedStateTime = Mathf.Clamp01(move.status.elapsed / m_PlayerMovement.WalkStateDuration);
                RenderStatus(moveId, GetNormalizedSpeed(move.velocity), normalizedStateTime);
            }
            else
                RenderStatus(moveId, 0.0f, 0.0f);
        }

        private float GetNormalizedSpeed(Vector3 velocity) { return Mathf.Clamp01(VectorMath.LateralMagnitude(velocity) / m_PlayerMovement.MaxSpeed); }

        private void RenderStatus(byte state, float normalizedSpeed, float normalizedStateTime)
        {
            AnimationClip animationClip = m_StatusVisualProperties[state].clip;
            switch (state)
            {
                case PlayerMovement.Idle:
                    m_Mixer.SetInputWeight(PlayerMovement.Idle, 1.0f);
                    m_Mixer.SetInputWeight(PlayerMovement.Moving, 0.0f);
                    m_Mixer.SetInputWeight(PlayerMovement.InAir, 0.0f);
                    break;
                case PlayerMovement.Moving:
                {
                    m_Mixer.SetInputWeight(PlayerMovement.Idle, 1.0f - normalizedSpeed);
                    m_Mixer.SetInputWeight(PlayerMovement.Moving, normalizedSpeed);
                    m_Mixer.SetInputWeight(PlayerMovement.InAir, 0.0f);
                    float clipTimeSeconds = normalizedStateTime * animationClip.length;
                    m_Animations[PlayerMovement.Moving].SetTime(clipTimeSeconds);
                    if (normalizedStateTime > 0.25f && m_LastNormalizedTime <= 0.25f || normalizedStateTime > 0.75f && m_LastNormalizedTime <= 0.75f)
                    {
                        if (m_FootstepSource)
                        {
                            int count = Physics.RaycastNonAlloc(transform.position + new Vector3 {y = 0.5f}, Vector3.down, m_CachedHits,
                                                                1.0f, m_PlayerMovement.GroundMask);
                            if (count >= 1)
                            {
                                // RaycastHit hit = m_CachedHits[0];
                                // var chunk = hit.collider.GetComponent<Chunk>();
                                // if (chunk)
                                // {
                                //     Voxel.Voxel? voxel = ChunkManager.Singleton.GetVoxel((Position3Int) (hit.point - new Vector3 {y = 0.5f}));
                                //     if (voxel.HasValue)
                                //     {
                                //         AudioClip[] clips;
                                //         switch (voxel.Value.texture)
                                //         {
                                //             case VoxelTexture.GRASS:
                                //                 clips = m_BrushClips;
                                //                 break;
                                //             case VoxelTexture.DIRT:
                                //                 clips = m_DirtClips;
                                //                 break;
                                //             default:
                                //                 clips = m_StoneClips;
                                //                 break;
                                //         }
                                //         AudioClip clip = clips[Random.Range(0, clips.Length)];
                                //         m_FootstepSource.PlayOneShot(clip);
                                //     }
                                // }
                                m_FootstepSource.PlayOneShot(m_BrushClips[Random.Range(0, m_BrushClips.Length)]);
                            }
                        }
                    }
                    break;
                }
                case PlayerMovement.InAir:
                    m_Mixer.SetInputWeight(PlayerMovement.Idle, 0.0f);
                    m_Mixer.SetInputWeight(PlayerMovement.Moving, 0.0f);
                    m_Mixer.SetInputWeight(PlayerMovement.InAir, 1.0f);
                    break;
            }
            m_LastNormalizedTime = normalizedStateTime;
            m_Graph.Evaluate();
        }

        public override void Dispose()
        {
            base.Dispose(); 
            if (m_Graph.IsValid()) m_Graph.Destroy();
        }
    }
}