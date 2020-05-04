using System;
using System.Linq;
using Swihoni.Components;
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
            public float time;
            public AudioClip audioSource;
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

        private AudioSource m_AudioSource;
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
            m_AudioSource = GetComponentInChildren<AudioSource>();
            ModiferProperties = ItemAssetLink.GetModifier(m_Id);
            playerItemAnimator.ArmIk.SetTargets(m_IkL, m_IkR);
            ItemStatusVisualProperties[] visualProperties = m_StatusVisualProperties.Concat(m_EquipStatusVisualProperties).ToArray();
            m_Mixer = AnimationMixerPlayable.Create(playerGraph, visualProperties.Length);
            m_Animations = new AnimationClipPlayable[visualProperties.Length];
            for (var i = 0; i < visualProperties.Length; i++)
            {
                ItemStatusVisualProperties statusProperty = visualProperties[i];
                AnimationClip clip = statusProperty.animationClip;
                if (!clip) continue;
                var playable = AnimationClipPlayable.Create(m_PlayerGraph, clip);
                m_PlayerGraph.Connect(playable, PlayerItemAnimatorBehavior.OutputIndex, m_Mixer, i);
                m_Animations[i] = playable;
            }
            m_PlayerGraph.GetOutput(PlayerItemAnimatorBehavior.OutputIndex).SetSourcePlayable(m_Mixer);
        }

        internal void Cleanup()
        {
            if (m_PlayerGraph.IsValid()) m_PlayerGraph.DestroySubgraph(m_Mixer);
        }

        private (ItemStatusVisualProperties, int) GetVisualProperties(ItemComponent item, ByteStatusComponent equipStatus)
        {
            bool useStatus = equipStatus.id == ItemEquipStatusId.Equipped;
            ItemStatusVisualProperties statusVisualProperties = useStatus
                ? m_StatusVisualProperties[item.status.id]
                : m_EquipStatusVisualProperties[equipStatus.id];
            int animationIndex = useStatus ? item.status.id : equipStatus.id + m_StatusVisualProperties.Length;
            return (statusVisualProperties, animationIndex);
        }

        public void SampleEvents(ItemComponent item, InventoryComponent inventory)
        {
            InventoryComponent lastRenderedInventory = m_PlayerItemAnimator.LastRenderedInventory;
            float? lastStatusElapsed = null;
            ByteStatusComponent GetExpressedStatus(InventoryComponent inv) => inv.equipStatus.id == ItemEquipStatusId.Equipped ? inv.EquippedItemComponent.status : inv.equipStatus;
            ByteStatusComponent expressedStatus = GetExpressedStatus(inventory);
            if (lastRenderedInventory != null)
            {
                bool isSameAnimation = lastRenderedInventory.HasItemEquipped && expressedStatus.id == GetExpressedStatus(lastRenderedInventory).id,
                     isAfter = lastRenderedInventory.HasItemEquipped && expressedStatus.elapsed > GetExpressedStatus(lastRenderedInventory).elapsed;
                if (isSameAnimation && isAfter)
                    lastStatusElapsed = lastRenderedInventory.EquippedItemComponent.status.elapsed;
            }
            (ItemStatusVisualProperties statusVisualProperties, int _) = GetVisualProperties(item, inventory.equipStatus);
            ItemStatusVisualProperties.AnimationEvent[] animationEvents = statusVisualProperties.animationEvents;
            foreach (ItemStatusVisualProperties.AnimationEvent animationEvent in animationEvents)
            {
                bool shouldDoEvent = (!lastStatusElapsed.HasValue || lastStatusElapsed.Value < animationEvent.time)
                                  && expressedStatus.elapsed >= animationEvent.time;
                if (!shouldDoEvent) continue;
                if (animationEvent.audioSource) m_AudioSource.PlayOneShot(animationEvent.audioSource);
                if (animationEvent.particleSystem) animationEvent.particleSystem.Play();
            }
            if (lastRenderedInventory == null) m_PlayerItemAnimator.LastRenderedInventory = inventory.Clone();
            else lastRenderedInventory.FastCopyFrom(inventory);
        }

        public void SampleAnimation(ItemComponent item, ByteStatusComponent equipStatus, float interpolation)
        {
            (ItemStatusVisualProperties statusVisualProperties, int animationIndex) = GetVisualProperties(item, equipStatus);
            AnimationClip animationClip = statusVisualProperties.animationClip;
            if (!animationClip) return;

            if (statusVisualProperties.isReverseAnimation)
                interpolation = 1.0f - interpolation;
            for (var i = 0; i < m_Animations.Length; i++)
                m_Mixer.SetInputWeight(i, i == animationIndex ? 1.0f : 0.0f);
            ref AnimationClipPlayable itemAnimation = ref m_Animations[animationIndex];
            itemAnimation.SetTime(interpolation * animationClip.length);
            m_PlayerGraph.Evaluate();
        }

        public void SetRenderingMode(bool isEnabled, ShadowCastingMode? shadowCastingMode = null)
        {
            if (m_Renders == null) return;
            foreach (Renderer meshRenderer in m_Renders)
            {
                meshRenderer.enabled = isEnabled;
                if (shadowCastingMode.HasValue)
                    meshRenderer.shadowCastingMode = shadowCastingMode.Value;
            }
        }
    }
}