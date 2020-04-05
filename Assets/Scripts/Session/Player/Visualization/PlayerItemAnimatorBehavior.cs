using System;
using Session.Items;
using Session.Items.Modifiers;
using Session.Items.Visuals;
using Session.Player.Components;
using Session.Player.Modifiers;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using Util;

namespace Session.Player.Visualization
{
    [Serializable, RequireComponent(typeof(Animator), typeof(ArmIk))]
    public class PlayerItemAnimatorBehavior : PlayerVisualsBehaviorBase
    {
        public const int OutputIndex = 0;

        [SerializeField] private string m_GraphName = default;
        [SerializeField] private Transform m_TpvArmsRotator = default;
        [SerializeField] protected bool m_IsFpv;
        [SerializeField] private Renderer m_FpvArmsRenderer = default;

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
        }

        public override void Visualize(PlayerComponent playerComponent, bool isLocalPlayer)
        {
            m_Visuals = SetupVisualItem(playerComponent);
            if (m_Visuals == null) return;
            ItemComponent activeItemComponent = playerComponent.inventory.ActiveItemComponent;
            bool isEquipped = activeItemComponent.equipStatus.id == ItemEquipStatusId.Ready;
            ItemByteStatusComponent expressedStatus = isEquipped ? activeItemComponent.status : activeItemComponent.equipStatus;
            float duration = (isEquipped
                      ? m_Visuals.ModiferProperties.GetStatusModifierProperties(expressedStatus.id)
                      : m_Visuals.ModiferProperties.GetEquipStatusModifierProperties(expressedStatus.id)).duration,
                  interpolation = expressedStatus.elapsed / duration;
            // TODO: generalize logic for viewable
            if (m_FpvArmsRenderer)
            {
                m_FpvArmsRenderer.enabled = isLocalPlayer;
                m_FpvArmsRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            SampleItemAnimation(activeItemComponent, interpolation);
            if (m_IsFpv)
                m_Visuals.SetRenderingMode(isLocalPlayer, ShadowCastingMode.Off);
            else
                m_Visuals.SetRenderingMode(true, isLocalPlayer ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On);
            if (!m_TpvArmsRotator) return;
            const float armClamp = 60.0f;
            m_TpvArmsRotator.localRotation = Quaternion.AngleAxis(Mathf.Clamp(playerComponent.pitch, -armClamp, armClamp) + 90.0f, Vector3.right);
        }

        private ItemVisualBehavior SetupVisualItem(PlayerComponent playerComponent)
        {
            if (playerComponent.inventory.activeIndex == PlayerItemManagerModiferBehavior.NoneIndex
             || playerComponent.inventory.ActiveItemComponent.id == ItemId.None
             || playerComponent.inventory.ActiveItemComponent.equipStatus.id == ItemEquipStatusId.Inactive)
            {
                if (m_Visuals) ItemManager.Singleton.ReturnVisuals(m_Visuals);
                return null;
            }
            byte itemId = playerComponent.inventory.ActiveItemComponent.id;
            if (m_Visuals && itemId == m_Visuals.ModiferProperties.id) return m_Visuals;
            if (m_Visuals) ItemManager.Singleton.ReturnVisuals(m_Visuals); // We have existing visuals but they are the wrong item id
            ItemVisualBehavior newVisuals = ItemManager.Singleton.ObtainVisuals(itemId, this, m_Graph);
            newVisuals.transform.SetParent(transform, false);
            m_Visuals = newVisuals;
            if (m_IsFpv) transform.localPosition = newVisuals.FpvOffset;
            else newVisuals.transform.localPosition = newVisuals.TpvOffset;
            return newVisuals;
        }

        private void SampleItemAnimation(ItemComponent itemComponent, float statusInterpolation)
        {
            m_Visuals.SampleEvents(itemComponent);
            m_Visuals.SampleAnimation(itemComponent, statusInterpolation);
        }

        internal override void Cleanup()
        {
            m_Graph.Destroy();
        }
    }
}