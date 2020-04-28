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

    public abstract class GunModifierBase : ItemModifierBase
    {
        private const int MaxRaycastDetections = 16;

        private static readonly RaycastHit[] RaycastHits = new RaycastHit[MaxRaycastDetections];

        [SerializeField] protected ushort m_MagSize;
        [SerializeField] private byte m_Damage = default;
        [SerializeField] private ushort m_StartingAmmoInReserve = default;
        [SerializeField] private LayerMask m_PlayerMask = default;
        [SerializeField] private ItemStatusModiferProperties[] m_AdsModifierProperties = default;

        public ushort MagSize => m_MagSize;
        public byte Damage => m_Damage;
        public ItemStatusModiferProperties GetAdsStatusModifierProperties(byte statusId) => m_AdsModifierProperties[statusId];

        protected override bool HasSecondaryUse() { return false; }

        public override void ModifyChecked(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputProperty, float duration)
        {
            bool reloadInput = inputProperty.GetInput(PlayerInput.Reload);
            if ((reloadInput || item.gunStatus.ammoInMag == 0) && CanReload(item, inventory))
                StartStatus(session, playerId, item, GunStatusId.Reloading);
            base.ModifyChecked(session, playerId, item, inventory, inputProperty, duration);
        }

        protected override byte? FinishStatus(ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            switch (item.status.id)
            {
                case GunStatusId.Reloading:
                    ReloadAmmo(item);
                    return null;
                case ItemStatusId.PrimaryUsing when item.gunStatus.ammoInMag == 0 && CanReload(item, inventory):
                    return GunStatusId.Reloading;
            }
            return base.FinishStatus(item, inventory, inputs);
        }

        protected virtual bool CanReload(ItemComponent item, InventoryComponent inventory)
        {
            return item.status.id == ItemStatusId.Idle && inventory.equipStatus.id == ItemEquipStatusId.Equipped
                                                       && item.gunStatus.ammoInReserve > 0 && item.gunStatus.ammoInMag < m_MagSize;
        }

        protected override bool CanUse(ItemComponent item, InventoryComponent inventory, bool justFinishedUse = false)
        {
            // We want to be able to interrupt reload with firing, and also make sure we can not fire with no ammo
            return item.gunStatus.ammoInMag > 0 && base.CanUse(item, inventory, justFinishedUse);
        }

        protected override void PrimaryUse(SessionBase session, int playerId, ItemComponent item) { Fire(playerId, session, item); }

        private readonly HashSet<PlayerHitboxManager> m_HitPlayers = new HashSet<PlayerHitboxManager>();

        protected virtual void Fire(int playerId, SessionBase session, ItemComponent item)
        {
            item.gunStatus.ammoInMag.Value--;
            
            Ray ray = session.GetRayForPlayerId(playerId);
            session.AboutToRaycast(playerId);
            
            ModeBase mode = session.GetMode();
            int hitCount = Physics.RaycastNonAlloc(ray, RaycastHits, float.PositiveInfinity, m_PlayerMask);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                RaycastHit hit = RaycastHits[hitIndex];
                var hitbox = hit.collider.GetComponent<PlayerHitbox>();
                if (!hitbox || hitbox.Manager.PlayerId == playerId || m_HitPlayers.Contains(hitbox.Manager)) continue;
                m_HitPlayers.Add(hitbox.Manager);
                Debug.Log($"Player: {playerId} hit player: {hitbox.Manager.PlayerId}");
                mode.PlayerHit(session.GetPlayerFromId(hitbox.Manager.PlayerId), session.GetPlayerFromId(playerId), hitbox, this, hit.distance);
            }
            m_HitPlayers.Clear();
        }

        internal override void OnUnequip(SessionBase session, int playerId, ItemComponent itemComponent)
        {
            if (itemComponent.status.id == GunStatusId.Reloading)
                StartStatus(session, playerId, itemComponent, ItemStatusId.Idle);
        }

        public void RefillAmmoAndReserve(ItemComponent itemComponents)
        {
            itemComponents.gunStatus.ammoInMag.Value = m_MagSize;
            itemComponents.gunStatus.ammoInReserve.Value = m_StartingAmmoInReserve;
        }

        protected abstract void ReloadAmmo(ItemComponent itemComponents);
    }
}