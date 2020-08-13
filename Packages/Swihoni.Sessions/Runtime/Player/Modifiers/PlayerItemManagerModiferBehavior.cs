using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerItemManagerModiferBehavior : PlayerModifierBehaviorBase
    {
        [SerializeField] private float m_DropItemForce = 1.0f, m_MaxPickupDistance = 1.5f;
        [SerializeField] private LayerMask m_ItemEntityMask = default;
        [SerializeField] private float m_PickupRadius = 2.25f;

        private static readonly RaycastHit[] CachedHits = new RaycastHit[1];
        private static readonly Collider[] CachedColliders = new Collider[1];

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeCommands() => SessionBase.RegisterSessionCommand("give_item");

        public static void ResetEquipStatus(InventoryComponent inventory)
        {
            inventory.adsStatus.Zero();
            inventory.equipStatus.Zero();
        }

        public override void ModifyChecked(in SessionContext context)
        {
            Container player = context.player;
            if (player.Without(out InventoryComponent inventory)) return;

            if (inventory.tracerTimeUs.Subtract(context.durationUs, true))
            {
                inventory.tracerStart.Clear();
                inventory.tracerEnd.Clear();
            }
            
            if (player.WithPropertyWithValue(out HealthProperty health) && health.IsDead) return;

            TryExecuteCommands(context);

            if (context.tickDelta < 1) return;

            var input = context.commands.Require<InputFlagProperty>();
            var wantedItemIndex = context.commands.Require<WantedItemIndexProperty>();

            if (input.GetInput(PlayerInput.DropItem) && inventory.equipStatus.id == ItemEquipStatusId.Equipped
                                                     && inventory.WithItemEquipped(out ItemComponent item) && inventory.equippedIndex != 0)
                ThrowActiveItem(context, item, inventory);
            TryPickupItemEntity(context, inventory);

            ModifyEquipStatus(context, inventory, wantedItemIndex);

            if (inventory.HasNoItemEquipped) return;
            /* Has Item Equipped */

            ModifyAdsStatus(context, inventory, input);

            // Modify equipped item component
            ItemComponent equippedItem = inventory.EquippedItemComponent;
            ItemModifierBase itemModifier = ItemAssetLink.GetModifier(equippedItem.id);
            itemModifier.ModifyChecked(context, equippedItem, inventory, input);

            if (equippedItem.status.id == ItemStatusId.RequestRemoval)
                SetItemAtIndex(inventory, null, inventory.equippedIndex);
        }

        private void TryPickupItemEntity(SessionContext context, InventoryComponent inventory)
        {
            void TryPickupItemFromCollider(Component itemCollider, bool fromNearby)
            {
                if (!itemCollider.TryGetComponent(out ItemEntityModifierBehavior itemEntityModifier)) return;

                EntityContainer entity = context.sessionContainer.Require<EntityArrayElement>()[itemEntityModifier.Index];
                var throwable = entity.Require<ThrowableComponent>();
                if (fromNearby && throwable.throwerId == context.playerId && throwable.thrownElapsedUs < 2_000_000u) return;

                if (!(TryAddItem(inventory, checked((byte) (itemEntityModifier.id - 100))) is byte index)) return;

                var itemOnEntity = entity.Require<ItemComponent>();
                ItemComponent item = inventory[index];
                item.ammoInMag.SetTo(itemOnEntity.ammoInMag);
                item.ammoInReserve.SetTo(itemOnEntity.ammoInReserve);
                entity.Clear();
                itemEntityModifier.SetActive(false, itemEntityModifier.Index); // Force update of behavior
            }

            if (context.player.Without<ServerTag>()) return;

            Vector3 move = context.player.Require<MoveComponent>();
            int nearbyCount = Physics.OverlapSphereNonAlloc(move, m_PickupRadius, CachedColliders, m_ItemEntityMask);
            if (nearbyCount > 0) TryPickupItemFromCollider(CachedColliders.First(), true);

            if (!context.commands.Require<InputFlagProperty>().GetInput(PlayerInput.Interact)) return;

            Ray ray = context.player.GetRayForPlayer();
            int count = Physics.RaycastNonAlloc(ray, CachedHits, m_MaxPickupDistance, m_ItemEntityMask);
            if (CachedHits.TryClosest(count, out RaycastHit hit))
                TryPickupItemFromCollider(hit.collider, false);
        }

        private void ThrowActiveItem(in SessionContext context, ItemComponent item, InventoryComponent inventory)
        {
            Container player = context.player;
            if (player.With<ServerTag>())
            {
                Ray ray = player.GetRayForPlayer();
                (ModifierBehaviorBase modifier, Container entity) = context.session.EntityManager.ObtainNextModifier(context.sessionContainer, 100 + item.id);
                if (modifier is ItemEntityModifierBehavior itemEntityModifier)
                {
                    var itemOnEntity = entity.Require<ItemComponent>();
                    itemOnEntity.ammoInMag.SetTo(item.ammoInMag);
                    itemOnEntity.ammoInReserve.SetTo(item.ammoInReserve);

                    var throwable = entity.Require<ThrowableComponent>();
                    throwable.throwerId.Value = (byte) context.playerId;
                    Vector3 position = ray.origin + ray.direction * 1.2f;
                    Quaternion rotation = Quaternion.LookRotation(ray.direction);
                    throwable.position.Value = position;
                    throwable.rotation.Value = rotation;

                    Vector3 force = ray.direction * m_DropItemForce;
                    if (player.With(out MoveComponent move)) force += move.velocity.Value * 0.1f;
                    modifier.transform.SetPositionAndRotation(position, rotation);
                    itemEntityModifier.Rigidbody.AddForce(force, ForceMode.Impulse);
                }
            }

            // Could set status to request removal but then one tick passes
            SetItemAtIndex(inventory, null, inventory.equippedIndex);
        }

        private static void TryExecuteCommands(in SessionContext context)
        {
            if (!context.WithServerStringCommands(out IEnumerable<string[]> commands)) return;
            foreach (string[] arguments in commands)
                if (arguments.First() == "give_item" && DefaultConfig.Active.allowCheats)
                {
                    if (arguments.Length > 1 && (byte.TryParse(arguments[1], out byte itemId) || ItemId.Names.TryGetReverse(arguments[1], out itemId)))
                    {
                        ushort count = arguments.Length > 2 && ushort.TryParse(arguments[2], out ushort parsedCount) ? parsedCount : (ushort) 1;
                        TryAddItem(context.player.Require<InventoryComponent>(), itemId, count);
                    }
                }
        }

        // public override void ModifyTrusted(SessionBase session, int playerId, Container trustedPlayer, Container commands, Container container, uint durationUs)
        // {
        //     if (trustedPlayer.Require<InventoryComponent>().WithItemEquipped(out ItemComponent item))
        //         ItemAssetLink.GetModifier(item.id).ModifyTrusted(session, playerId, trustedPlayer, commands, container, durationUs);
        // }

        private static void ModifyEquipStatus(in SessionContext context, InventoryComponent inventory, WantedItemIndexProperty wantedItemIndex)
        {
            ByteStatusComponent equipStatus = inventory.equipStatus;
            // Unequip current item if desired
            bool hasValidWantedIndex = wantedItemIndex.WithValue && inventory[wantedItemIndex].id.WithValue,
                 isAlreadyUnequipping = equipStatus.id == ItemEquipStatusId.Unequipping;
            if (hasValidWantedIndex && wantedItemIndex != inventory.equippedIndex && !isAlreadyUnequipping)
            {
                equipStatus.id.Value = ItemEquipStatusId.Unequipping;
                equipStatus.elapsedUs.Value = 0u;
            }

            if (inventory.HasNoItemEquipped) return;
            // We have a current equipped item
            equipStatus.elapsedUs.Add(context.durationUs);
            ItemModifierBase modifier = ItemAssetLink.GetModifier(inventory.EquippedItemComponent.id);

            // Handle finishing equip status
            ItemStatusModiferProperties modifierProperties;
            while (equipStatus.elapsedUs > (modifierProperties = modifier.GetEquipStatusModifierProperties(equipStatus.id)).durationUs)
            {
                if (equipStatus.id == ItemEquipStatusId.Equipping)
                {
                    equipStatus.id.Value = ItemEquipStatusId.Equipped;
                    modifier.OnEquip(context, inventory.EquippedItemComponent);
                }
                else if (equipStatus.id == ItemEquipStatusId.Unequipping)
                {
                    equipStatus.id.Value = ItemEquipStatusId.Unequipped;
                    modifier.OnUnequip(context, inventory, inventory.EquippedItemComponent);
                }
                equipStatus.elapsedUs.Value -= modifierProperties.durationUs;
            }

            if (equipStatus.id != ItemEquipStatusId.Unequipped) return;
            // We have unequipped the current index
            if (hasValidWantedIndex)
            {
                inventory.previousEquippedIndex.Value = inventory.equippedIndex.Value;
                inventory.equippedIndex.SetTo(wantedItemIndex);
            }
            else inventory.equippedIndex.SetToNullable(FindReplacement(inventory));
            equipStatus.id.Value = ItemEquipStatusId.Equipping;
        }

        private static void ModifyAdsStatus(in SessionContext context, InventoryComponent inventory, InputFlagProperty inputs)
        {
            ItemModifierBase modifier = ItemAssetLink.GetModifier(inventory.EquippedItemComponent.id);
            if (!(modifier is GunModifierBase gunModifier)) return;

            if (inputs.GetInput(PlayerInput.Ads))
            {
                if (inventory.adsStatus.id == AdsStatusId.HipAiming)
                {
                    inventory.adsStatus.id.Value = AdsStatusId.EnteringAds;
                    inventory.adsStatus.elapsedUs.Value = 0u;
                }
            }
            else
            {
                if (inventory.adsStatus.id == AdsStatusId.Ads)
                {
                    inventory.adsStatus.id.Value = AdsStatusId.ExitingAds;
                    inventory.adsStatus.elapsedUs.Value = 0u;
                }
            }

            ByteStatusComponent adsStatus = inventory.adsStatus;
            adsStatus.elapsedUs.Add(context.durationUs);

            ItemStatusModiferProperties modifierProperties;
            while (adsStatus.elapsedUs > (modifierProperties = gunModifier.GetAdsStatusModifierProperties(adsStatus.id)).durationUs)
            {
                if (adsStatus.id == AdsStatusId.EnteringAds) adsStatus.id.Value = AdsStatusId.Ads;
                else if (adsStatus.id == AdsStatusId.ExitingAds) adsStatus.id.Value = AdsStatusId.HipAiming;
                adsStatus.elapsedUs.Value -= modifierProperties.durationUs;
            }
        }

        public override void ModifyCommands(SessionBase session, Container commands, int playerId)
        {
            if (commands.Without(out InputFlagProperty inputs)) return;

            void SetInput(byte id) => inputs.SetInput(id, InputProvider.GetInput(id));
            SetInput(PlayerInput.UseOne);
            SetInput(PlayerInput.UseTwo);
            SetInput(PlayerInput.UseThree);
            SetInput(PlayerInput.UseFour);
            SetInput(PlayerInput.Reload);
            SetInput(PlayerInput.Ads);
            SetInput(PlayerInput.Throw);
            SetInput(PlayerInput.DropItem);

            if (commands.Without(out WantedItemIndexProperty wantedItemIndex)) return;

            var inventory = session.GetLocalPlayer().Require<InventoryComponent>();
            float wheel = InputProvider.GetMouseScrollWheel();
            byte Wrap(int index)
            {
                while (index >= InventoryComponent.ItemsCount) index -= InventoryComponent.ItemsCount;
                while (index < 0) index += InventoryComponent.ItemsCount;
                return (byte) index;
            }
            if (Mathf.Abs(wheel) > Mathf.Epsilon)
            {
                byte current = wantedItemIndex.Value = inventory.equippedIndex.Else(wantedItemIndex);
                if (wheel > 0)
                {
                    for (int i = current + 1; i < current + InventoryComponent.ItemsCount; i++)
                        if (inventory[Wrap(i)].id.WithValue)
                            wantedItemIndex.Value = Wrap(i);
                }
                else
                {
                    for (int i = current - 1; i > current - InventoryComponent.ItemsCount; i--)
                        if (inventory[Wrap(i)].id.WithValue)
                            wantedItemIndex.Value = Wrap(i);
                }
            }
            else if (InputProvider.GetInput(PlayerInput.ItemOne)) wantedItemIndex.Value = 0;
            else if (InputProvider.GetInput(PlayerInput.ItemTwo)) wantedItemIndex.Value = 1;
            else if (InputProvider.GetInput(PlayerInput.ItemThree)) wantedItemIndex.Value = 2;
            else if (InputProvider.GetInput(PlayerInput.ItemFour)) wantedItemIndex.Value = 3;
            else if (InputProvider.GetInput(PlayerInput.ItemFive)) wantedItemIndex.Value = 4;
            else if (InputProvider.GetInput(PlayerInput.ItemSix)) wantedItemIndex.Value = 5;
            else if (InputProvider.GetInput(PlayerInput.ItemSeven)) wantedItemIndex.Value = 6;
            else if (InputProvider.GetInput(PlayerInput.ItemEight)) wantedItemIndex.Value = 7;
            else if (InputProvider.GetInput(PlayerInput.ItemNine)) wantedItemIndex.Value = 8;
            else if (InputProvider.GetInput(PlayerInput.ItemTen)) wantedItemIndex.Value = 9;
            else if (InputProvider.GetInput(PlayerInput.ItemLast) && inventory.previousEquippedIndex.TryWithValue(out byte previousEquipped))
                wantedItemIndex.Value = previousEquipped;
        }

        private static int Clamp(int i, int min, int max) => Math.Min(Math.Max(i, min), max);

        private static byte? FindItem(InventoryComponent inventory, Predicate<ItemComponent> predicate)
        {
            for (byte itemIndex = 0; itemIndex < inventory.items.Length; itemIndex++)
            {
                if (!predicate(inventory[itemIndex])) continue;
                return itemIndex;
            }
            return null;
        }

        public static byte? FindEmpty(InventoryComponent inventory)
            => FindItem(inventory, item => item.id.WithoutValue);

        private static byte? FindReplacement(InventoryComponent inventory)
        {
            if (inventory.previousEquippedIndex.WithoutValue || inventory[inventory.previousEquippedIndex].id.WithoutValue)
                return FindItem(inventory, item => item.id.WithValue);
            return inventory.previousEquippedIndex;
        }

        public static byte? TryAddItem(InventoryComponent inventory, byte itemId, ushort count = 1, ushort limit = ushort.MaxValue)
        {
            byte? openIndex;
            if (ItemAssetLink.GetModifier(itemId) is ThrowableItemModifierBase
             && (openIndex = FindItem(inventory, item => item.id.WithoutValue || item.id == itemId)) is byte existingIndex) // Try to stack throwables if possible
            {
                ItemComponent itemInIndex = inventory[existingIndex];
                bool addingToExisting = itemInIndex.id.WithValue && itemInIndex.id == itemId;
                if (addingToExisting)
                {
                    var combined = (ushort) (itemInIndex.ammoInReserve + count);
                    if (combined > limit) return null;
                    count = combined;
                }
            }
            else openIndex = FindEmpty(inventory);
            if (openIndex is byte itemIndex)
                SetItemAtIndex(inventory, itemId, itemIndex, count);
            return openIndex;
        }

        public static void RefillAllAmmo(InventoryComponent inventory)
        {
            for (var i = 0; i < inventory.items.Length; i++)
            {
                ItemComponent item = inventory[i];
                if (item.id.WithoutValue) continue;
                if (ItemAssetLink.GetModifier(item.id) is GunModifierBase gunModifier)
                {
                    item.ammoInMag.Value = gunModifier.MagSize;
                    item.ammoInReserve.Value = gunModifier.StartingAmmoInReserve;
                }
            }
        }

        public static void SetAllItems(InventoryComponent inventory, params byte[] ids)
        {
            for (var i = 0; i < inventory.items.Length; i++)
                SetItemAtIndex(inventory, i < ids.Length ? ids[i] : (byte?) null, i);
        }

        public static void SetItemAtIndex(InventoryComponent inventory, byte? _itemId, int index, ushort count = 1)
        {
            ItemComponent item = inventory[index];
            if (!(_itemId is byte itemId))
            {
                item.Clear();
                if (inventory.equippedIndex == index)
                {
                    inventory.equippedIndex.SetToNullable(FindReplacement(inventory));
                    inventory.equipStatus.id.Value = inventory.HasItemEquipped ? ItemEquipStatusId.Equipping : ItemEquipStatusId.Unequipped;
                    inventory.equipStatus.elapsedUs.Value = 0u;
                }
                return;
            }
            bool keepStatus = inventory.equippedIndex.WithValueEqualTo((byte) index) && item.id.WithValueEqualTo(itemId);
            ItemModifierBase itemModifier = ItemAssetLink.GetModifier(itemId);
            switch (itemModifier)
            {
                case GunModifierBase gunModifier:
                    item.ammoInMag.Value = gunModifier.MagSize;
                    item.ammoInReserve.Value = gunModifier.StartingAmmoInReserve;
                    break;
                case ThrowableItemModifierBase _:
                    item.ammoInReserve.Value = count;
                    break;
            }
            if (keepStatus) return;
            item.id.Value = itemId;
            item.status.id.Value = ItemStatusId.Idle;
            item.status.elapsedUs.Value = 0u;
            if (inventory.HasNoItemEquipped)
            {
                inventory.equippedIndex.SetToNullable(FindReplacement(inventory));
                inventory.equipStatus.id.Value = inventory.HasItemEquipped ? ItemEquipStatusId.Equipping : ItemEquipStatusId.Unequipped;
                inventory.equipStatus.elapsedUs.Value = 0u;
            }
            else if (inventory.equippedIndex == index)
            {
                inventory.equipStatus.id.Value = ItemEquipStatusId.Equipping;
                inventory.equipStatus.elapsedUs.Value = 0u;
            }
        }
    }
}