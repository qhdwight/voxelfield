using System;
using System.Linq;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Visualization;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace Swihoni.Sessions.Items.Visuals
{
    [Serializable]
    public class ItemStatusVisualProperties
    {
        [Serializable]
        public class AnimationEvent
        {
            public uint time;
            public AudioSource audioSource;
            public ParticleSystem particleSystem;
            public GameObject tracer;
        }

        public AnimationClip animationClip;
        public AnimationEvent[] animationEvents;
        public bool isReverseAnimation;
    }

    [SelectionBase, DisallowMultipleComponent]
    public class ItemVisualBehavior : MonoBehaviour
    {
        [SerializeField] private byte m_Id = default;
        [SerializeField] private ItemStatusVisualProperties[] m_StatusVisualProperties = default,
                                                              m_EquipStatusVisualProperties = default;
        [SerializeField] private Transform m_IkL = default, m_IkR = default;
        [SerializeField] private Vector3 m_FpvOffset = default, m_TpvOffset = default;

        private AnimationClipPlayable[] m_Animations;
        private PlayableGraph m_PlayerGraph;
        private AnimationMixerPlayable m_Mixer;
        private PlayerItemAnimatorBehavior m_PlayerItemAnimator;
        private Renderer[] m_Renders;

        public byte Id => m_Id;
        public Vector3 FpvOffset => m_FpvOffset;
        public Vector3 TpvOffset => m_TpvOffset;

        public ItemModifierBase ModiferProperties { get; private set; }

        internal void SetupForPlayerAnimation(PlayerItemAnimatorBehavior playerItemAnimator, in PlayableGraph playerGraph)
        {
            m_PlayerItemAnimator = playerItemAnimator;
            m_PlayerGraph = playerGraph;
            m_Renders = GetComponentsInChildren<Renderer>();
            ModiferProperties = ItemManager.GetModifier(m_Id);
            playerItemAnimator.ArmIk.SetTargets(m_IkL, m_IkR);
            ItemStatusVisualProperties[] properties = m_StatusVisualProperties.Concat(m_EquipStatusVisualProperties).ToArray();
            m_Mixer = AnimationMixerPlayable.Create(playerGraph, properties.Length);
            m_Animations = new AnimationClipPlayable[properties.Length];
            m_Animations = properties.Select((statusProperty, i) =>
            {
                AnimationClip clip = statusProperty.animationClip;
                if (!clip) return default;
                var playable = AnimationClipPlayable.Create(m_PlayerGraph, clip);
                m_PlayerGraph.Connect(playable, PlayerItemAnimatorBehavior.OutputIndex, m_Mixer, i);
                return playable;
            }).ToArray();
            m_PlayerGraph.GetOutput(PlayerItemAnimatorBehavior.OutputIndex).SetSourcePlayable(m_Mixer);
        }

        internal void Cleanup()
        {
            if (m_PlayerGraph.IsValid()) m_PlayerGraph.DestroySubgraph(m_Mixer);
        }

        private (ItemStatusVisualProperties, int) GetVisualProperties(ItemComponent itemComponent, ByteStatusComponent equipStatus)
        {
            bool useStatus = equipStatus.id == ItemEquipStatusId.Equipped;
            ItemStatusVisualProperties statusVisualProperties = useStatus
                ? m_StatusVisualProperties[itemComponent.status.id]
                : m_EquipStatusVisualProperties[equipStatus.id];
            int animationIndex = useStatus ? itemComponent.status.id : equipStatus.id + m_StatusVisualProperties.Length;
            return (statusVisualProperties, animationIndex);
        }

        public void SampleEvents(ItemComponent itemComponent, ByteStatusComponent equipStatus)
        {
            ItemComponent lastItemComponent = m_PlayerItemAnimator.LastRenderedItemComponent;
            float? lastStatusElapsed = null;
            if (lastItemComponent != null)
            {
                bool isSameAnimation = lastItemComponent.id == itemComponent.id && lastItemComponent.status.id == itemComponent.status.id,
                     isAfter = itemComponent.status.elapsed > lastItemComponent.status.elapsed;
                if (isSameAnimation && isAfter)
                    lastStatusElapsed = lastItemComponent.status.elapsed;
            }
            (ItemStatusVisualProperties statusVisualProperties, int _) = GetVisualProperties(itemComponent, equipStatus);
            ItemStatusVisualProperties.AnimationEvent[] animationEvents = statusVisualProperties.animationEvents;
            foreach (ItemStatusVisualProperties.AnimationEvent animationEvent in animationEvents)
            {
                bool shouldDoEvent = (!lastStatusElapsed.HasValue || lastStatusElapsed.Value < animationEvent.time)
                                  && itemComponent.status.elapsed >= animationEvent.time;
                if (!shouldDoEvent) continue;
                if (animationEvent.audioSource) animationEvent.audioSource.PlayOneShot(animationEvent.audioSource.clip);
                if (animationEvent.particleSystem) animationEvent.particleSystem.Play();
            }
            m_PlayerItemAnimator.LastRenderedItemComponent = itemComponent;
        }

        public void SampleAnimation(ItemComponent itemComponent, ByteStatusComponent equipStatus, float interpolation)
        {
            (ItemStatusVisualProperties statusVisualProperties, int animationIndex) = GetVisualProperties(itemComponent, equipStatus);
            AnimationClip animationClip = statusVisualProperties.animationClip;
            if (!animationClip)
                return;
            if (statusVisualProperties.isReverseAnimation)
                interpolation = 1.0f - interpolation;
            for (var i = 0; i < m_Animations.Length; i++)
                m_Mixer.SetInputWeight(i, i == animationIndex ? 1.0f : 0.0f);
            ref AnimationClipPlayable itemAnimation = ref m_Animations[animationIndex];
            itemAnimation.SetTime(interpolation * animationClip.length);
            m_PlayerGraph.Evaluate();
        }

        public void SetRenderingMode(bool isEnabled, ShadowCastingMode shadowCastingMode)
        {
            if (m_Renders == null) return;
            foreach (Renderer meshRenderer in m_Renders)
            {
                meshRenderer.enabled = isEnabled;
                meshRenderer.shadowCastingMode = shadowCastingMode;
            }
        }
    }
}