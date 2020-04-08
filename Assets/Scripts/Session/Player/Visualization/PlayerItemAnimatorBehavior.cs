using System;
using Session.Items;
using Session.Items.Modifiers;
using Session.Items.Visuals;
using Session.Player.Components;
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
        [SerializeField] private Camera m_FpvCamera = default;

        private float m_FieldOfView;
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
            if (m_FpvCamera) m_FieldOfView = m_FpvCamera.fieldOfView;
            AnimationPlayableOutput.Create(m_Graph, $"{m_GraphName} Output", m_Animator);
        }

        public override void Visualize(PlayerComponent playerComponent, bool isLocalPlayer)
        {
            m_Visuals = SetupVisualItem(playerComponent.inventory);
            if (m_Visuals == null) return;
            ItemComponent equippedItemComponent = playerComponent.inventory.EquippedItemComponent;
            ByteStatusComponent equipStatus = playerComponent.inventory.equipStatus;
            bool isEquipped = equipStatus.id == ItemEquipStatusId.Equipped;
            ByteStatusComponent expressedStatus = isEquipped ? equippedItemComponent.status : equipStatus;
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
            if (m_IsFpv)
                m_Visuals.SetRenderingMode(isLocalPlayer, ShadowCastingMode.Off);
            else
                m_Visuals.SetRenderingMode(true, isLocalPlayer ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On);

            SampleItemAnimation(equippedItemComponent, equipStatus, interpolation);

            if (m_IsFpv) AnimateAim(playerComponent.inventory);

            if (!m_TpvArmsRotator) return;
            const float armClamp = 60.0f;
            m_TpvArmsRotator.localRotation = Quaternion.AngleAxis(Mathf.Clamp(playerComponent.pitch, -armClamp, armClamp) + 90.0f, Vector3.right);
        }

        private ItemVisualBehavior SetupVisualItem(PlayerInventoryComponent inventoryComponent)
        {
            if (inventoryComponent.HasNoItemEquipped)
            {
                if (m_Visuals) ItemManager.Singleton.ReturnVisuals(m_Visuals);
                return null;
            }
            byte itemId = inventoryComponent.EquippedItemComponent.id;
            if (m_Visuals && itemId == m_Visuals.ModiferProperties.id) return m_Visuals;
            if (m_Visuals) ItemManager.Singleton.ReturnVisuals(m_Visuals); // We have existing visuals but they are the wrong item id
            ItemVisualBehavior newVisuals = ItemManager.Singleton.ObtainVisuals(itemId, this, m_Graph);
            newVisuals.transform.SetParent(transform, false);
            m_Visuals = newVisuals;
            if (m_IsFpv) transform.localPosition = newVisuals.FpvOffset;
            else newVisuals.transform.localPosition = newVisuals.TpvOffset;
            return newVisuals;
        }

        private void SampleItemAnimation(ItemComponent itemComponent, ByteStatusComponent equipStatus, float statusInterpolation)
        {
            m_Visuals.SampleEvents(itemComponent, equipStatus);
            m_Visuals.SampleAnimation(itemComponent, equipStatus, statusInterpolation);
        }

        public void AnimateAim(PlayerInventoryComponent inventoryComponent)
        {
            if (!(m_Visuals is GunVisualBehavior gunVisuals) || !(m_Visuals.ModiferProperties is GunWithMagazineModifier gunModifier)) return;
            float adsInterpolation = GetAimInterpolationValue(inventoryComponent);
            Vector3 adsPosition = -transform.InverseTransformPoint(gunVisuals.AdsTarget.position);
            transform.localPosition = Vector3.Slerp(m_Visuals.FpvOffset, adsPosition, adsInterpolation);
            m_FpvCamera.fieldOfView = Mathf.Lerp(m_FieldOfView, m_FieldOfView / 2, adsInterpolation);
        }

        private static float GetAimInterpolationValue(PlayerInventoryComponent inventoryComponent)
        {
            var aimInterpolationValue = 0.0f;
            ByteStatusComponent adsStatus = inventoryComponent.adsStatus;
            switch (adsStatus.id)
            {
                case AdsStatusId.ExitingAds:
                case AdsStatusId.EnteringAds:
                    var gunModifier = (GunModifierBase) ItemManager.GetModifier(inventoryComponent.EquippedItemComponent.id);
                    float duration = gunModifier.GetAdsStatusModifierProperties(adsStatus.id).duration;
                    aimInterpolationValue = adsStatus.elapsed / duration;
                    break;
                case AdsStatusId.Ads:
                    aimInterpolationValue = 1.0f;
                    break;
            }
            if (adsStatus.id == AdsStatusId.ExitingAds) aimInterpolationValue = 1.0f - aimInterpolationValue;
            return aimInterpolationValue;
        }

        internal override void Cleanup()
        {
            m_Graph.Destroy();
        }
    }
}