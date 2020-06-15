using System.Collections.Generic;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    public static class GunStatusId
    {
        public const byte Reloading = ItemStatusId.Last + 1;
    }

    public static class AdsStatusId
    {
        public const byte HipAiming = 0, EnteringAds = 1, Ads = 2, ExitingAds = 3;
    }

    public abstract class GunModifierBase : WeaponModifierBase
    {
        private const int MaxRaycastDetections = 16;

        private static readonly RaycastHit[] RaycastHits = new RaycastHit[MaxRaycastDetections];

        [SerializeField] protected ushort m_MagSize;
        [SerializeField] private ushort m_StartingAmmoInReserve = default;
        [SerializeField] private ItemStatusModiferProperties[] m_AdsModifierProperties = default;

        public ushort MagSize => m_MagSize;
        public ushort StartingAmmoInReserve => m_StartingAmmoInReserve;
        public ItemStatusModiferProperties GetAdsStatusModifierProperties(byte statusId) => m_AdsModifierProperties[statusId];

        public override void ModifyChecked(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputProperty, uint durationUs)
        {
            bool reloadInput = inputProperty.GetInput(PlayerInput.Reload);
            if ((reloadInput || item.gunStatus.ammoInMag == 0) && CanReload(item, inventory) && item.status.id == ItemStatusId.Idle)
                StartStatus(session, playerId, item, GunStatusId.Reloading, durationUs);
            base.ModifyChecked(session, playerId, item, inventory, inputProperty, durationUs);
        }

        protected override byte? FinishStatus(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            switch (item.status.id)
            {
                case GunStatusId.Reloading:
                    ReloadAmmo(item);
                    return null;
                case ItemStatusId.PrimaryUsing when item.gunStatus.ammoInMag == 0 && CanReload(item, inventory):
                    return GunStatusId.Reloading;
            }
            return base.FinishStatus(session, playerId, item, inventory, inputs);
        }

        protected virtual bool CanReload(ItemComponent item, InventoryComponent inventory) =>
            inventory.equipStatus.id == ItemEquipStatusId.Equipped
         && item.gunStatus.ammoInReserve > 0 && item.gunStatus.ammoInMag < m_MagSize;

        /// <summary>
        /// We want to be able to interrupt reload with firing, and also make sure we can not fire with no ammo
        /// </summary>
        protected override bool CanUse(ItemComponent item, InventoryComponent inventory, bool justFinishedUse = false) =>
            item.gunStatus.ammoInMag > 0 && base.CanUse(item, inventory, justFinishedUse);

        protected override void PrimaryUse(SessionBase session, int playerId, ItemComponent item, uint durationUs) => Fire(playerId, session, item, durationUs);

        private readonly HashSet<PlayerHitboxManager> m_HitPlayers = new HashSet<PlayerHitboxManager>();

        protected virtual void Fire(int playerId, SessionBase session, ItemComponent item, uint durationUs)
        {
            item.gunStatus.ammoInMag.Value--;

            Ray ray = session.GetRayForPlayerId(playerId);
            session.RollbackHitboxesFor(playerId);

            ModeBase mode = session.GetMode();
            int hitCount = Physics.RaycastNonAlloc(ray, RaycastHits, float.PositiveInfinity, m_PlayerMask);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                RaycastHit hit = RaycastHits[hitIndex];
                if (!hit.collider.TryGetComponent(out PlayerHitbox hitbox) || hitbox.Manager.PlayerId == playerId || m_HitPlayers.Contains(hitbox.Manager)) continue;
                m_HitPlayers.Add(hitbox.Manager);
                // Debug.Log($"Player: {playerId} hit player: {hitbox.Manager.PlayerId}");
                mode.PlayerHit(session, playerId, hitbox, this, hit, durationUs);
            }
            m_HitPlayers.Clear();
        }

        internal override void OnUnequip(SessionBase session, int playerId, ItemComponent itemComponent, uint durationUs)
        {
            if (itemComponent.status.id == GunStatusId.Reloading)
                StartStatus(session, playerId, itemComponent, ItemStatusId.Idle, durationUs);
        }

        protected abstract void ReloadAmmo(ItemComponent item);
    }
}