using Session.Player;
using Session.Player.Components;
using UnityEngine;

namespace Session.Items.Modifiers
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
        [SerializeField] private ushort m_StartingAmmoInReserve = default;
        [SerializeField] private ItemStatusModiferProperties[] m_AdsModifierProperties = default;

        public ItemStatusModiferProperties GetAdsStatusModifierProperties(byte statusId) => m_AdsModifierProperties[statusId];

        protected override bool HasSecondaryUse()
        {
            return false;
        }

        public override void ModifyChecked((ItemComponent, PlayerInventoryComponent) componentToModify, PlayerCommandsComponent commands)
        {
            (ItemComponent itemComponent, PlayerInventoryComponent inventoryComponent) = componentToModify;
            bool reloadInput = commands.GetInput(PlayerInput.Reload);
            if ((reloadInput || itemComponent.gunStatus.ammoInMag == 0) && CanReload(itemComponent, inventoryComponent) && itemComponent.status.id == ItemStatusId.Idle)
                StartStatus(itemComponent, GunStatusId.Reloading);
            base.ModifyChecked(componentToModify, commands);
        }

        protected override byte? FinishStatus(ItemComponent itemComponent, PlayerInventoryComponent inventoryComponent, PlayerCommandsComponent commands)
        {
            switch (itemComponent.status.id)
            {
                case GunStatusId.Reloading:
                    ReloadAmmo(itemComponent);
                    return null;
                case ItemStatusId.PrimaryUsing when itemComponent.gunStatus.ammoInMag == 0 && CanReload(itemComponent, inventoryComponent):
                    return GunStatusId.Reloading;
            }
            return base.FinishStatus(itemComponent, inventoryComponent, commands);
        }

        protected virtual bool CanReload(ItemComponent itemComponent, PlayerInventoryComponent inventoryComponent)
        {
            return inventoryComponent.equipStatus.id == ItemEquipStatusId.Equipped && itemComponent.gunStatus.ammoInReserve > 0 && itemComponent.gunStatus.ammoInMag < m_MagSize;
        }

        protected override bool CanUse(ItemComponent itemComponent, PlayerInventoryComponent inventoryComponent, bool justFinishedUse = false)
        {
            // We want to be able to interrupt reload with firing, and also make sure we can not fire with no ammo
            return itemComponent.gunStatus.ammoInMag > 0 && base.CanUse(itemComponent, inventoryComponent, justFinishedUse);
        }

        protected override void PrimaryUse(ItemComponent itemComponent)
        {
            Fire(itemComponent);
        }

        // private readonly HashSet<PlayerMain> m_HitPlayers = new HashSet<PlayerMain>();

        protected virtual void Fire(ItemComponent itemComponent)
        {
            itemComponent.gunStatus.ammoInMag.Value--;
//             Ray ray = m_Game.GetRayForRaycastingPlayer(m_HoldingPlayer);
//             m_Game.AboutToRaycast(m_HoldingPlayer);
//             // TODO query layer
//             int numberOfHits = Physics.RaycastNonAlloc(ray, RaycastHits, float.PositiveInfinity, LayerManager.PLAYER_RAYCAST_MASK);
//             for (var hitIndex = 0; hitIndex < numberOfHits; hitIndex++)
//             {
//                 RaycastHit hit = RaycastHits[hitIndex];
// //                Debug.Log(hit.collider.name);
//                 var receivingPlayerHitbox = hit.collider.GetComponent<PlayerHitbox>();
//                 if (!receivingPlayerHitbox || m_HitPlayers.Contains(receivingPlayerHitbox.Player)) continue;
//                 m_HitPlayers.Add(receivingPlayerHitbox.Player);
//                 m_Game.PlayerHit(new PlayerHitInformation
//                 {
//                     receivingPlayer = receivingPlayerHitbox.Player,
//                     inflictingPlayer = m_HoldingPlayer,
//                     receivingPlayerHitbox = receivingPlayerHitbox,
//                     weapon = this,
//                     distance = hit.distance
//                 });
//             }
//             m_HitPlayers.Clear();
        }

        internal override void OnUnequip(ItemComponent itemComponent)
        {
            if (itemComponent.status.id == GunStatusId.Reloading)
                StartStatus(itemComponent, ItemStatusId.Idle);
        }

        public void RefillAmmoAndReserve(ItemComponent itemComponents)
        {
            itemComponents.gunStatus.ammoInMag.Value = m_MagSize;
            itemComponents.gunStatus.ammoInReserve.Value = m_StartingAmmoInReserve;
        }

        protected abstract void ReloadAmmo(ItemComponent itemComponents);
    }
}