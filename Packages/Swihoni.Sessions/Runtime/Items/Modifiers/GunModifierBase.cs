using System.Collections.Generic;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    public static class GunStatusId
    {
        public const byte Reloading = ItemStatusId.Last + 1, DryFiring = ItemStatusId.Last + 2;
    }

    public static class AdsStatusId
    {
        public const byte HipAiming = 0, EnteringAds = 1, Ads = 2, ExitingAds = 3;
    }

    public abstract class GunModifierBase : WeaponModifierBase
    {
        private const int MaxRaycastDetections = 8;

        protected static readonly RaycastHit[] RaycastHits = new RaycastHit[MaxRaycastDetections];

        [SerializeField] protected ushort m_MagSize;
        [SerializeField] private ushort m_StartingAmmoInReserve = default;
        [SerializeField] private ItemStatusModiferProperties[] m_AdsModifierProperties = default;
        [SerializeField] private float m_RaycastThickness = default;
        public bool isPrimary = true;

        public ushort MagSize => m_MagSize;
        public ushort StartingAmmoInReserve => m_StartingAmmoInReserve;
        public ItemStatusModiferProperties GetAdsStatusModifierProperties(byte statusId) => m_AdsModifierProperties[statusId];

        public override void ModifyChecked(in SessionContext context, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            bool reloadInput = inputs.GetInput(PlayerInput.Reload);
            if ((reloadInput || item.ammoInMag == 0) && CanReload(item, inventory) && item.status.id == ItemStatusId.Idle)
                StartStatus(context, inventory, item, GunStatusId.Reloading);
            else if (inputs.GetInput(PlayerInput.UseOne) && item.NoAmmoLeft && item.status.id == ItemStatusId.Idle)
                StartStatus(context, inventory, item, GunStatusId.DryFiring);

            if (inventory.tracerTimeUs.WithValue)
                if (inventory.tracerTimeUs > context.durationUs) inventory.tracerTimeUs.Value -= context.durationUs;
                else
                {
                    inventory.tracerTimeUs.Clear();
                    inventory.tracerStart.Clear();
                    inventory.tracerEnd.Clear();
                }

            base.ModifyChecked(context, item, inventory, inputs);
        }

        protected override byte? FinishStatus(in SessionContext context, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            switch (item.status.id)
            {
                case GunStatusId.Reloading:
                    ReloadAmmo(item);
                    return null;
                case ItemStatusId.PrimaryUsing when item.ammoInMag == 0 && CanReload(item, inventory):
                    return GunStatusId.Reloading;
            }
            return base.FinishStatus(in context, item, inventory, inputs);
        }

        protected virtual bool CanReload(ItemComponent item, InventoryComponent inventory) =>
            inventory.equipStatus.id == ItemEquipStatusId.Equipped
         && item.ammoInReserve > 0 && item.ammoInMag < m_MagSize;

        /// <summary>
        /// We want to be able to interrupt reload with firing, and also make sure we can not fire with no ammo
        /// </summary>
        protected override bool CanPrimaryUse(ItemComponent item, InventoryComponent inventory, bool justFinishedUse = false)
            => item.ammoInMag > 0 && base.CanPrimaryUse(item, inventory, justFinishedUse);

        protected override void PrimaryUse(in SessionContext context, InventoryComponent inventory, ItemComponent item)
            => Fire(context, inventory, item);

        private static readonly HashSet<PlayerHitboxManager> HitPlayers = new HashSet<PlayerHitboxManager>();

        protected virtual int FireRaycast(Ray ray)
        {
            if (m_RaycastThickness > Mathf.Epsilon)
            {
                return Physics.BoxCastNonAlloc(ray.GetPoint(m_RaycastThickness),
                                               new Vector3(m_RaycastThickness, m_RaycastThickness, m_RaycastThickness),
                                               ray.direction, RaycastHits, Quaternion.identity, float.PositiveInfinity, m_RaycastMask);
            }
            return Physics.RaycastNonAlloc(ray, RaycastHits, float.PositiveInfinity, m_RaycastMask);
        }

        protected virtual void Fire(in SessionContext context, InventoryComponent inventory, ItemComponent item)
        {
            if (item.ammoInMag == 0) return;

            item.ammoInMag.Value--;

            SessionBase session = context.session;
            Ray ray = session.GetRayForPlayerId(context.playerId);
            session.RollbackHitboxesFor(context);

            int hitCount = FireRaycast(ray);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                RaycastHit hit = RaycastHits[hitIndex];
                if (!hit.collider.TryGetComponent(out PlayerHitbox hitbox) || hitbox.Manager.PlayerId == context.playerId || HitPlayers.Contains(hitbox.Manager)) continue;
                HitPlayers.Add(hitbox.Manager);
                if (context.player.With<ServerTag>())
                {
                    var hitContext = new PlayerHitContext(context, hitbox, this, hit);
                    session.GetModifyingMode().PlayerHit(hitContext);
                }
            }
            HitPlayers.Clear();

            inventory.tracerStart.Value = ray.origin;
            inventory.tracerEnd.Value = hitCount > 0 ? RaycastHits[0].point : ray.GetPoint(300.0f);
            inventory.tracerTimeUs.Value = 1_000_000u;
        }

        protected internal override void OnUnequip(in SessionContext context, InventoryComponent inventory, ItemComponent item)
        {
            if (item.status.id == GunStatusId.Reloading)
                StartStatus(context, inventory, item, ItemStatusId.Idle);
        }

        protected abstract void ReloadAmmo(ItemComponent item);
    }
}