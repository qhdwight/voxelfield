using System;
using Session.Items.Modifiers;
using Session.Player.Components;
using Session.Player.Modifiers;
using Session.Player.Visualization;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Util;

namespace Session.Items.Visuals
{
    [Serializable, RequireComponent(typeof(Animator), typeof(ArmIk))]
    public class PlayerItemAnimatorBehavior : PlayerVisualsBehaviorBase
    {
        public const int OutputIndex = 0;

        [SerializeField] private string m_GraphName = default;
        [SerializeField] private Renderer m_ArmsRenderer = default;
        [SerializeField] protected bool m_IsFpv;
        [SerializeField] protected Transform m_OffsetTaker;

        private Animator m_Animator;
        private PlayableGraph m_Graph;
        private ItemVisualBehavior m_Visuals;

        public ArmIk ArmIk { get; private set; }

        public ItemComponent LastRenderedItemComponent { internal get; set; }

        internal override void Setup()
        {
            ArmIk = GetComponent<ArmIk>();
            m_Animator = GetComponent<Animator>();
            m_Graph = PlayableGraph.Create($"{m_GraphName} Item Animator");
            m_Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput.Create(m_Graph, $"{m_GraphName} Output", m_Animator);
            m_Graph.Play();
        }

        public override void Visualize(PlayerComponent playerComponent, bool isLocalPlayer)
        {
            m_Visuals = SetupVisualItem(playerComponent);
            if (m_Visuals == null) return;
            ItemComponent itemComponent = playerComponent.inventory.ActiveItemComponent;
            float duration = m_Visuals.ModiferProperties.GetStatusModifierProperties(itemComponent.statusId).duration,
                  interpolation = itemComponent.statusElapsed / duration;
            if (m_ArmsRenderer) m_ArmsRenderer.enabled = isLocalPlayer && m_IsFpv;
            SampleItemAnimation(itemComponent, playerComponent.pitch, interpolation);
        }

        private static bool RequiresItemVisuals(PlayerComponent component)
        {
            return component.inventory.activeIndex != PlayerItemManagerModiferBehavior.NoneIndex
                && component.inventory.ActiveItemComponent.id != ItemId.None;
        }

        private ItemVisualBehavior SetupVisualItem(PlayerComponent playerComponent)
        {
            if (!RequiresItemVisuals(playerComponent))
            {
                if (m_Visuals) ItemManager.Singleton.ReturnVisuals(m_Visuals);
                return null;
            }
            byte newItemId = playerComponent.inventory.ActiveItemComponent.id;
            if (m_Visuals && newItemId == m_Visuals.ModiferProperties.id) return m_Visuals;
            if (m_Visuals) ItemManager.Singleton.ReturnVisuals(m_Visuals); // We have existing visuals but they are the wrong item id
            ItemVisualBehavior newVisuals = ItemManager.Singleton.ObtainVisuals(newItemId, this, m_Graph);
            newVisuals.transform.SetParent(transform, false);
            m_Visuals = newVisuals;
            if (m_OffsetTaker) m_OffsetTaker.localPosition = newVisuals.Offset;
            return newVisuals;
        }

        private void SampleItemAnimation(ItemComponent itemComponent, float pitch, float statusInterpolation)
        {
            ItemStatusVisualProperties statusVisualProperties = m_Visuals.GetStatusVisualProperties(itemComponent);
            float clampedInterpolation = Mathf.Clamp01(statusInterpolation);
            if (statusVisualProperties.animationEvents != null) m_Visuals.SampleEvents(itemComponent, statusVisualProperties);
            m_Visuals.SampleAnimation(itemComponent.statusId, statusVisualProperties, clampedInterpolation);
        }

        internal override void Cleanup()
        {
            m_Graph.Destroy();
        }
    }
}