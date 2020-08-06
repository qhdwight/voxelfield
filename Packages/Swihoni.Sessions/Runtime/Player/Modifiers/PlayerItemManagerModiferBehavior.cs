using System;
using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerItemManagerModiferBehavior : PlayerModifierBehaviorBase
    {
        [SerializeField] private float m_DropItemForce = 1.0f, m_MaxPickupDistance = 1.5f;
        [SerializeField] private LayerMask m_ItemEntityMask = default;

        private static readonly RaycastHit[] CachedHits = new RaycastHit[1];

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeCommands() => SessionBase.RegisterSessionCommand("give_item");

        public static void Clear(InventoryComponent inventory)
        {
            inventory.Clear();
            inventory.adsStatus.Zero();
            inventory.equipStatus.Zero();
        }
        
        public override void ModifyChecked(in SessionContext context)
        {
            Container player = context.player;
            if (player.Without(out InventoryComponent inventory) || player.WithPropertyWithValue(out HealthProperty health) && health.IsDead) return;

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

        private void TryPickupItemEntity(in SessionContext context, InventoryComponent inventory)
        {
            if (context.player.Without<ServerTag>() || !context.commands.Require<InputFlagProperty>().GetInput(PlayerInput.Interact)) return;

            Ray ray = context.player.GetRayForPlayer();
            int count = Physics.RaycastNonAlloc(ray, CachedHits, m_MaxPickupDistance, m_ItemEntityMask);
            if (count > 0 && CachedHits[0].collider.TryGetComponent(out ItemEntityModifierBehavior itemEntity)
                          && AddItem(inventory, checked((byte) (itemEntity.id - 100))) is byte index)
            {
                ItemComponent item = inventory[index];
                EntityContainer entity = context.sessionContainer.Require<EntityArrayElement>()[itemEntity.Index];
                var itemOnEntity = entity.Require<ItemComponent>();
                item.ammoInMag.SetTo(itemOnEntity.ammoInMag);
                item.ammoInReserve.SetTo(itemOnEntity.ammoInReserve);
                entity.Clear();
                itemEntity.SetActive(false, itemEntity.Index); // Force update of behavior
            }
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
                    modifier.transform.SetPositionAndRotation(ray.origin + ray.direction * 1.1f, Quaternion.LookRotation(ray.direction));
                    Vector3 force = ray.direction * m_DropItemForce;
                    if (player.With(out MoveComponent move)) force += move.velocity.Value * 0.1f;
                    itemEntityModifier.Rigidbody.AddForce(force, ForceMode.Impulse);
                }
            }

            // Could set status to request removal but then one tick passes
            SetItemAtIndex(inventory, null, inventory.equippedIndex);
        }

        private static void TryExecuteCommands(in SessionContext context)
        {
            if (!WithServerStringCommands(context, out IEnumerable<string[]> commands)) return;
            foreach (string[] arguments in commands)
                if (arguments[0] == "give_item" && ConfigManagerBase.Active.allowCheats)
                {
                    if (arguments.Length > 1 && (byte.TryParse(arguments[1], out byte itemId) || ItemId.Names.TryGetReverse(arguments[1], out itemId)))
                    {
                        ushort count = arguments.Length > 2 && ushort.TryParse(arguments[2], out ushort parsedCount) ? parsedCount : (ushort) 1;
                        AddItem(context.player.Require<InventoryComponent>(), itemId, count);
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
            bool
                hasValidWantedIndex = wantedItemIndex.WithValue && inventory[wantedItemIndex].id.WithValue,
                wantsNewIndex = wantedItemIndex != inventory.equippedIndex,
                isAlreadyUnequipping = equipStatus.id == ItemEquipStatusId.Unequipping;
            if (hasValidWantedIndex && wantsNewIndex && !isAlreadyUnequipping)
            {
                equipStatus.id.Value = ItemEquipStatusId.Unequipping;
                equipStatus.elapsedUs.Value = 0u;
            }

            if (inventory.HasNoItemEquipped) return;
            // We have a current equipped item
            equipStatus.elapsedUs.Value += context.durationUs;
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
            adsStatus.elapsedUs.Value += context.durationUs;

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
            SetInput(PlayerInput.Fly);
            SetInput(PlayerInput.Ads);
            SetInput(PlayerInput.Throw);
            SetInput(PlayerInput.DropItem);
            if (commands.Without(out WantedItemIndexProperty itemIndex)) return;
            if (InputProvider.GetInput(PlayerInput.ItemOne)) itemIndex.Value = 0;
            else if (InputProvider.GetInput(PlayerInput.ItemTwo)) itemIndex.Value = 1;
            else if (InputProvider.GetInput(PlayerInput.ItemThree)) itemIndex.Value = 2;
            else if (InputProvider.GetInput(PlayerInput.ItemFour)) itemIndex.Value = 3;
            else if (InputProvider.GetInput(PlayerInput.ItemFive)) itemIndex.Value = 4;
            else if (InputProvider.GetInput(PlayerInput.ItemSix)) itemIndex.Value = 5;
            else if (InputProvider.GetInput(PlayerInput.ItemSeven)) itemIndex.Value = 6;
            else if (InputProvider.GetInput(PlayerInput.ItemEight)) itemIndex.Value = 7;
            else if (InputProvider.GetInput(PlayerInput.ItemNine)) itemIndex.Value = 8;
            else if (InputProvider.GetInput(PlayerInput.ItemTen)) itemIndex.Value = 9;
            else if (InputProvider.GetInput(PlayerInput.ItemLast))
            {
                ByteProperty previousEquipped = commands.Require<InventoryComponent>().previousEquippedIndex;
                if (previousEquipped.WithValue) itemIndex.Value = previousEquipped;
            }
        }

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

        public static void AddItems(InventoryComponent inventory, params byte[] itemIds)
        {
            foreach (byte itemId in itemIds) AddItem(inventory, itemId);
        }

        public static byte? AddItem(InventoryComponent inventory, byte itemId, ushort count = 1)
        {
            byte? _openIndex;
            if (ItemAssetLink.GetModifier(itemId) is ThrowableItemModifierBase
             && (_openIndex = FindItem(inventory, item => item.id.WithoutValue || item.id == itemId)) is byte existingIndex) // Try to stack throwables if possible
            {
                ItemComponent itemInIndex = inventory[existingIndex];
                bool addingToExisting = itemInIndex.id.WithValue && itemInIndex.id == itemId;
                if (addingToExisting) count += itemInIndex.ammoInReserve;
            }
            else _openIndex = FindEmpty(inventory);
            if (_openIndex is byte openIndex)
                SetItemAtIndex(inventory, itemId, openIndex, count);
            return _openIndex;
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
            item.id.Value = itemId;
            item.status.id.Value = ItemStatusId.Idle;
            item.status.elapsedUs.Value = 0u;
            ItemModifierBase itemModifier = ItemAssetLink.GetModifier(itemId);
            if (itemModifier is GunModifierBase gunModifier)
            {
                item.ammoInMag.Value = gunModifier.MagSize;
                item.ammoInReserve.Value = gunModifier.StartingAmmoInReserve;
            }
            if (itemModifier is ThrowableItemModifierBase)
                item.ammoInReserve.Value = count;
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