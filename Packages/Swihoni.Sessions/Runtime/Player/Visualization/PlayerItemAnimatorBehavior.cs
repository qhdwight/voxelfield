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
    public class PlayerItemAnimatorBehavior : MonoBehaviour, IDisposable
    {
        public const int OutputIndex = 0;

        [SerializeField] private string m_GraphName = default;
        [SerializeField] private Transform m_TpvArmsRotator = default;
        [SerializeField] protected bool m_IsFpv, m_RenderItems = true;
        [SerializeField] private Renderer m_FpvArmsRenderer = default;
        [SerializeField] private Camera m_FpvCamera = default;

        private float m_FieldOfView;
        private Animator m_Animator;
        private PlayableGraph m_Graph;
        private ItemVisualBehavior m_ItemVisual;

        public ArmIk ArmIk { get; private set; }

        public InventoryComponent LastRenderedInventory { internal get; set; }

        internal void Setup()
        {
            if (m_Graph.IsValid()) return;

            ArmIk = GetComponent<ArmIk>();
            m_Animator = GetComponent<Animator>();
            m_Graph = PlayableGraph.Create($"{transform.root.name} {m_GraphName} Item Animator");
            m_Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            if (m_FpvCamera) m_FieldOfView = m_FpvCamera.fieldOfView;
            AnimationPlayableOutput.Create(m_Graph, $"{transform.root.name} {m_GraphName} Output", m_Animator);
        }

        public void Render(Container player, bool isLocalPlayer)
        {
            if (player.Without(out InventoryComponent inventory)) return;

            bool isVisible = (player.Without(out HealthProperty health) || health.WithValue && health.IsAlive) && inventory.HasItemEquipped;

            if (isVisible)
            {
                m_ItemVisual = SetupVisualItem(inventory);
                if (m_ItemVisual)
                {
                    if (m_IsFpv) transform.localPosition = m_ItemVisual.FpvOffset;
                    else m_ItemVisual.transform.localPosition = m_ItemVisual.TpvOffset;

                    if (m_FpvArmsRenderer)
                    {
                        m_FpvArmsRenderer.enabled = isLocalPlayer;
                        m_FpvArmsRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    }
                    if (m_RenderItems)
                    {
                        if (m_IsFpv)
                            m_ItemVisual.SetRenderingMode(isLocalPlayer, ShadowCastingMode.Off);
                        else
                            m_ItemVisual.SetRenderingMode(true, isLocalPlayer ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On);
                    }
                    else
                        m_ItemVisual.SetRenderingMode(false);

                    ItemComponent equippedItem = inventory.EquippedItemComponent;
                    ByteStatusComponent equipStatus = inventory.equipStatus;
                    bool isEquipped = equipStatus.id == ItemEquipStatusId.Equipped;
                    ByteStatusComponent expressedStatus = isEquipped ? equippedItem.status : equipStatus;
                    float duration = (isEquipped
                              ? m_ItemVisual.ModiferProperties.GetStatusModifierProperties(expressedStatus.id)
                              : m_ItemVisual.ModiferProperties.GetEquipStatusModifierProperties(expressedStatus.id)).duration,
                          interpolation = expressedStatus.elapsed / duration;

                    if (m_RenderItems && m_IsFpv == isLocalPlayer)
                        m_ItemVisual.SampleEvents(equippedItem, inventory);
                    m_ItemVisual.SampleAnimation(equippedItem, equipStatus, interpolation);

                    if (m_IsFpv) AnimateAim(inventory);

                    if (player.With(out CameraComponent playerCamera) && m_TpvArmsRotator)
                    {
                        const float armClamp = 60.0f;
                        m_TpvArmsRotator.localRotation = Quaternion.AngleAxis(Mathf.Clamp(playerCamera.pitch, -armClamp, armClamp) + 90.0f, Vector3.right);
                    }
                }
            }
            else
            {
                if (m_ItemVisual)
                {
                    ItemAssetLink.ReturnVisuals(m_ItemVisual);
                    m_ItemVisual = null;
                }
                if (m_FpvArmsRenderer) m_FpvArmsRenderer.enabled = false;
            }
            m_Animator.enabled = isVisible;
            ArmIk.enabled = isVisible;
        }

        /// <summary>
        /// For some reason Animator.Rebind() does not work in normal Update()
        /// </summary>
        private void LateUpdate()
        {
            if (m_ItemVisual && m_WasNewItemVisualThisRenderFrame)
            {
                m_Animator.Rebind();
                m_WasNewItemVisualThisRenderFrame = false;
            }
        }

        private bool m_WasNewItemVisualThisRenderFrame;

        private ItemVisualBehavior SetupVisualItem(InventoryComponent inventory)
        {
            if (inventory.HasNoItemEquipped)
            {
                if (m_ItemVisual) ItemAssetLink.ReturnVisuals(m_ItemVisual);
                m_ItemVisual = null;
                return null;
            }
            byte itemId = inventory.EquippedItemComponent.id;
            if (m_ItemVisual && itemId == m_ItemVisual.ModiferProperties.id) return m_ItemVisual;

            if (m_ItemVisual) ItemAssetLink.ReturnVisuals(m_ItemVisual); // We have existing visuals but they are the wrong item id
            ItemVisualBehavior newVisuals = ItemAssetLink.ObtainVisuals(itemId, this, m_Graph);
            Transform itemTransform = newVisuals.transform;
            itemTransform.SetParent(transform);
            itemTransform.localPosition = Vector3.zero;
            itemTransform.localRotation = Quaternion.identity;
            m_ItemVisual = newVisuals;
            m_WasNewItemVisualThisRenderFrame = true;
            return newVisuals;
        }

        public void AnimateAim(InventoryComponent inventory)
        {
            Transform t = transform;
            if (m_ItemVisual is GunVisualBehavior gunVisuals && m_ItemVisual.ModiferProperties is GunWithMagazineModifier)
            {
                float adsInterpolation = GetAimInterpolationValue(inventory);

                Quaternion targetRotation = Quaternion.Inverse(Quaternion.Inverse(t.rotation) * gunVisuals.AdsTarget.rotation);
                t.localRotation = Quaternion.Slerp(Quaternion.identity, targetRotation, adsInterpolation);

                Vector3 adsPosition = targetRotation * -t.InverseTransformPoint(gunVisuals.AdsTarget.position);
                t.localPosition = Vector3.Slerp(m_ItemVisual.FpvOffset, adsPosition, adsInterpolation);

                m_FpvCamera.fieldOfView = Mathf.Lerp(m_FieldOfView, m_FieldOfView / 2, adsInterpolation);
            }
            else
            {
                m_FpvCamera.fieldOfView = m_FieldOfView;
                t.localRotation = Quaternion.identity;
                t.localPosition = m_ItemVisual.FpvOffset;
            }
        }

        private static float GetAimInterpolationValue(InventoryComponent inventory)
        {
            var aimInterpolation = 0.0f;
            ByteStatusComponent adsStatus = inventory.adsStatus;
            switch (adsStatus.id)
            {
                case AdsStatusId.ExitingAds:
                case AdsStatusId.EnteringAds:
                    var gunModifier = (GunModifierBase) ItemAssetLink.GetModifier(inventory.EquippedItemComponent.id);
                    float duration = gunModifier.GetAdsStatusModifierProperties(adsStatus.id).duration;
                    aimInterpolation = adsStatus.elapsed / duration;
                    break;
                case AdsStatusId.Ads:
                    aimInterpolation = 1.0f;
                    break;
            }
            if (adsStatus.id == AdsStatusId.ExitingAds) aimInterpolation = 1.0f - aimInterpolation;
            return aimInterpolation;
        }

        public void Dispose() => m_Graph.Destroy();
    }
}