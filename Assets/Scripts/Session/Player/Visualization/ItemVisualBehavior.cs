using System;
using System.Collections.Generic;
using Session.Items;
using Session.Player.Components;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using Util;

namespace Session.Player.Visualization
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

    [SelectionBase]
    public class ItemVisualBehavior : PlayerVisualsBehaviorBase
    {
        [SerializeField] private ItemId m_Id = default;
        [SerializeField] private ItemStatusVisualProperties[] m_StatusVisualProperties = default;
        [SerializeField] private Vector3 m_Offset = default;

        private AnimationClipPlayable[] m_Animations;
        private PlayableGraph m_PlayerGraph;
        private AnimationMixerPlayable m_Mixer;
        private PlayerItemAnimatorBehavior m_PlayerItemAnimator;
        private Renderer[] m_Renders;
        private Dictionary<ItemStatusId, ItemStatusVisualProperties> m_StatusToVisualProperties;

        public ItemId Id => m_Id;
        public Vector3 Offset => m_Offset;

        public ItemStatusVisualProperties[] StatusVisualProperties => m_StatusVisualProperties;
        public ItemModiferProperties ModiferProperties { get; private set; }

        internal void Setup(PlayerItemAnimatorBehavior playerItemAnimator, in PlayableGraph playerGraph)
        {
            m_PlayerItemAnimator = playerItemAnimator;
            m_PlayerGraph = playerGraph;
            m_Renders = GetComponentsInChildren<Renderer>();
            playerItemAnimator.ArmIk.SetTargets(transform.Find("IK.L"), transform.Find("IK.R"));
            m_Mixer = AnimationMixerPlayable.Create(playerGraph, m_StatusVisualProperties.Length);
            m_Animations = new AnimationClipPlayable[m_StatusVisualProperties.Length];
            ModiferProperties = ItemManager.Singleton.GetModifierProperties(m_Id);
            m_StatusToVisualProperties = m_StatusVisualProperties.ToEnumDictionary<ItemStatusId, ItemStatusVisualProperties>();
            for (var statusIndex = 0; statusIndex < m_StatusVisualProperties.Length; statusIndex++)
            {
                AnimationClip clip = m_StatusVisualProperties[statusIndex].animationClip;
                if (!clip) continue;
                m_Animations[statusIndex] = AnimationClipPlayable.Create(playerGraph, clip);
                m_PlayerGraph.Connect(m_Animations[statusIndex], PlayerItemAnimatorBehavior.OutputIndex, m_Mixer, statusIndex);
            }
            m_PlayerGraph.GetOutput(PlayerItemAnimatorBehavior.OutputIndex).SetSourcePlayable(m_Mixer);
        }

        internal override void Cleanup()
        {
            m_PlayerGraph.DestroySubgraph(m_Mixer);
        }

        public void SampleEvents(ItemComponent component, ItemStatusVisualProperties statusVisualProperties)
        {
            ItemComponent lastItemComponent = m_PlayerItemAnimator.LastRenderedItemComponent;
            float? lastStatusTime = null;
            if (lastItemComponent != null)
            {
                bool isSameAnimation = lastItemComponent.id == component.id && lastItemComponent.status.id == component.status.id,
                     isAfter = component.status.elapsedTime > lastItemComponent.status.elapsedTime;
                if (isSameAnimation && isAfter)
                    lastStatusTime = lastItemComponent.status.elapsedTime;
            }
            ItemStatusVisualProperties.AnimationEvent[] animationEvents = statusVisualProperties.animationEvents;
            foreach (ItemStatusVisualProperties.AnimationEvent animationEvent in animationEvents)
            {
                bool shouldDoEvent = lastStatusTime != component.status.elapsedTime
                                  && (!lastStatusTime.HasValue || lastStatusTime.Value < animationEvent.time)
                                  && component.status.elapsedTime >= animationEvent.time;
                if (!shouldDoEvent) continue;
                if (animationEvent.audioSource) animationEvent.audioSource.PlayOneShot(animationEvent.audioSource.clip);
                if (animationEvent.particleSystem) animationEvent.particleSystem.Play();
            }
            m_PlayerItemAnimator.LastRenderedItemComponent = component;
        }

        public void SampleAnimation(ItemStatusId statusId, ItemStatusVisualProperties statusVisualProperties, float interpolation)
        {
            AnimationClip animationClip = statusVisualProperties.animationClip;
            if (!animationClip)
                return;
            if (statusVisualProperties.isReverseAnimation)
                interpolation = 1.0f - interpolation;
            var statusIndex = (int) statusId;
            for (var animationIndex = 0; animationIndex < m_StatusVisualProperties.Length; animationIndex++)
                m_Mixer.SetInputWeight(animationIndex, statusIndex == animationIndex ? 1.0f : 0.0f);
            ref AnimationClipPlayable itemAnimation = ref m_Animations[statusIndex];
            itemAnimation.SetTime(interpolation * animationClip.length);
            m_PlayerGraph.Evaluate();
        }

        public void SetRenderingMode(int layer, ShadowCastingMode shadowCastingMode)
        {
            if (m_Renders == null) return;
            foreach (Renderer meshRenderer in m_Renders)
            {
                meshRenderer.gameObject.layer = layer;
                meshRenderer.shadowCastingMode = shadowCastingMode;
            }
        }

        public void SetVisible(bool isVisible)
        {
            if (m_Renders == null) return;
            foreach (Renderer meshRenderer in m_Renders)
                meshRenderer.enabled = isVisible;
        }

        public ItemStatusVisualProperties GetStatusVisualProperties(ItemComponent itemComponent) => m_StatusToVisualProperties[(ItemStatusId) itemComponent.status.id.Value];
    }
}