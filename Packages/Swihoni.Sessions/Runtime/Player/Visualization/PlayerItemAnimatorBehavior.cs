using System;
using Swihoni.Components;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Items.Visuals;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Animation;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace Swihoni.Sessions.Player.Visualization
{
    [Serializable, RequireComponent(typeof(Animator), typeof(ArmIk))]
    public class PlayerItemAnimatorBehavior : MonoBehaviour
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
        private ItemVisualBehavior m_ItemVisual;

        public ArmIk ArmIk { get; private set; }

        public ItemComponent LastRenderedItemComponent { internal get; set; }

        internal void Setup()
        {
            ArmIk = GetComponent<ArmIk>();
            m_Animator = GetComponent<Animator>();
            m_Graph = PlayableGraph.Create($"{m_GraphName} Item Animator");
            m_Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            if (m_FpvCamera) m_FieldOfView = m_FpvCamera.fieldOfView;
            AnimationPlayableOutput.Create(m_Graph, $"{m_GraphName} Output", m_Animator);
        }

        public void Render(Container player, bool isLocalPlayer)
        {
            if (!player.Has(out InventoryComponent inventory)) return;

            bool isVisible = player.Without(out HealthProperty health) || health.HasValue && health.IsAlive;

            if (isVisible)
            {
                m_ItemVisual = SetupVisualItem(inventory);
                if (m_ItemVisual == null) return;
                ItemComponent equippedItem = inventory.EquippedItemComponent;
                ByteStatusComponent equipStatus = inventory.equipStatus;
                bool isEquipped = equipStatus.id == ItemEquipStatusId.Equipped;
                ByteStatusComponent expressedStatus = isEquipped ? equippedItem.status : equipStatus;
                float duration = (isEquipped
                          ? m_ItemVisual.ModiferProperties.GetStatusModifierProperties(expressedStatus.id)
                          : m_ItemVisual.ModiferProperties.GetEquipStatusModifierProperties(expressedStatus.id)).duration,
                      interpolation = expressedStatus.elapsed / duration;
                // TODO:refactor generalize logic for viewable
                if (m_FpvArmsRenderer)
                {
                    m_FpvArmsRenderer.enabled = isLocalPlayer;
                    m_FpvArmsRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                if (m_IsFpv)
                    m_ItemVisual.SetRenderingMode(isLocalPlayer, ShadowCastingMode.Off);
                else
                    m_ItemVisual.SetRenderingMode(true, isLocalPlayer ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On);

                SampleItemAnimation(equippedItem, equipStatus, interpolation);

                if (m_IsFpv) AnimateAim(inventory);

                if (!player.Has(out CameraComponent playerCamera) || !m_TpvArmsRotator) return;
                const float armClamp = 60.0f;
                m_TpvArmsRotator.localRotation = Quaternion.AngleAxis(Mathf.Clamp(playerCamera.pitch, -armClamp, armClamp) + 90.0f, Vector3.right);
            }
            else
            {
                if (m_ItemVisual)
                {
                    ItemManager.Singleton.ReturnVisuals(m_ItemVisual);
                    m_ItemVisual = null;
                }
            }
        }

        private ItemVisualBehavior SetupVisualItem(InventoryComponent inventory)
        {
            if (inventory.HasNoItemEquipped)
            {
                if (m_ItemVisual) ItemManager.Singleton.ReturnVisuals(m_ItemVisual);
                return null;
            }
            byte itemId = inventory.EquippedItemComponent.id;
            if (m_ItemVisual && itemId == m_ItemVisual.ModiferProperties.id) return m_ItemVisual;
            if (m_ItemVisual) ItemManager.Singleton.ReturnVisuals(m_ItemVisual); // We have existing visuals but they are the wrong item id
            ItemVisualBehavior newVisuals = ItemManager.Singleton.ObtainVisuals(itemId, this, m_Graph);
            newVisuals.transform.SetParent(transform, false);
            m_ItemVisual = newVisuals;
            if (m_IsFpv) transform.localPosition = newVisuals.FpvOffset;
            else newVisuals.transform.localPosition = newVisuals.TpvOffset;
            return newVisuals;
        }

        private void SampleItemAnimation(ItemComponent item, ByteStatusComponent equipStatus, float statusInterpolation)
        {
            m_ItemVisual.SampleEvents(item, equipStatus);
            m_ItemVisual.SampleAnimation(item, equipStatus, statusInterpolation);
        }

        public void AnimateAim(InventoryComponent inventory)
        {
            if (!(m_ItemVisual is GunVisualBehavior gunVisuals) || !(m_ItemVisual.ModiferProperties is GunWithMagazineModifier gunModifier)) return;
            float adsInterpolation = GetAimInterpolationValue(inventory);
            Vector3 adsPosition = -transform.InverseTransformPoint(gunVisuals.AdsTarget.position);
            transform.localPosition = Vector3.Slerp(m_ItemVisual.FpvOffset, adsPosition, adsInterpolation);
            m_FpvCamera.fieldOfView = Mathf.Lerp(m_FieldOfView, m_FieldOfView / 2, adsInterpolation);
        }

        private static float GetAimInterpolationValue(InventoryComponent inventory)
        {
            var aimInterpolationValue = 0.0f;
            ByteStatusComponent adsStatus = inventory.adsStatus;
            switch (adsStatus.id)
            {
                case AdsStatusId.ExitingAds:
                case AdsStatusId.EnteringAds:
                    var gunModifier = (GunModifierBase) ItemManager.GetModifier(inventory.EquippedItemComponent.id);
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

        internal void Cleanup() { m_Graph.Destroy(); }
    }
}