using System;
using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerItemManagerModiferBehavior : PlayerModifierBehaviorBase
    {
        public const byte NoneIndex = 0;

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeCommands() => SessionBase.RegisterSessionCommand("give_item");

        public override void ModifyChecked(SessionBase session, int playerId, Container player, Container commands, uint durationUs, int tickDelta)
        {
            if (tickDelta < 1 || !player.With(out InventoryComponent inventory) || player.WithPropertyWithValue(out HealthProperty health) && health.IsDead) return;

            HandleCommands(player);
            
            var input = commands.Require<InputFlagProperty>();
            var wantedItemIndex = commands.Require<WantedItemIndexProperty>();

            if (input.GetInput(PlayerInput.DropItem) && inventory.equippedIndex != 1 && inventory.equipStatus.id == ItemEquipStatusId.Equipped
             && inventory.WithItemEquipped(out ItemComponent item))
            {
                // Could set status to request removal but then one tick passes
                item.id.Value = ItemId.None;
                inventory.equippedIndex.Value = FindReplacement(inventory, out byte replacementIndex) ? replacementIndex : NoneIndex;
                inventory.equipStatus.id.Value = inventory.HasItemEquipped ? ItemEquipStatusId.Equipping : ItemEquipStatusId.Unequipped;
                inventory.equipStatus.elapsedUs.Value = 0u;
            }

            ModifyEquipStatus(session, playerId, inventory, wantedItemIndex, durationUs);

            if (inventory.HasNoItemEquipped) return;
            /* Has Item Equipped */

            ModifyAdsStatus(inventory, input, durationUs);

            // Modify equipped item component
            ItemComponent equippedItem = inventory.EquippedItemComponent;
            ItemModifierBase itemModifier = ItemAssetLink.GetModifier(equippedItem.id);
            itemModifier.ModifyChecked(session, playerId, player, equippedItem, inventory, input, durationUs);

            if (equippedItem.status.id == ItemStatusId.RequestRemoval)
            {
                equippedItem.id.Value = ItemId.None;
                inventory.equippedIndex.Value = FindReplacement(inventory, out byte replacementIndex) ? replacementIndex : NoneIndex;
                inventory.equipStatus.id.Value = inventory.HasItemEquipped ? ItemEquipStatusId.Equipping : ItemEquipStatusId.Unequipped;
                inventory.equipStatus.elapsedUs.Value = 0u;
            }
        }

        private static void HandleCommands(Container player)
        {
            if (TryServerCommands(player, out IEnumerable<string[]> commands))
            {
                foreach (string[] args in commands)
                {
                    if (args[0] == "give_item" && ConfigManagerBase.Active.allowCheats)
                    {
                        if (args.Length > 1 && byte.TryParse(args[1], out byte itemId))
                        {
                            ushort count = args.Length > 2 && ushort.TryParse(args[2], out ushort parsedCount) ? parsedCount : (ushort) 1;
                            AddItem(player.Require<InventoryComponent>(), itemId, count);
                        }
                    }   
                }
            }
        }

        // public override void ModifyTrusted(SessionBase session, int playerId, Container trustedPlayer, Container commands, Container container, uint durationUs)
        // {
        //     if (trustedPlayer.Require<InventoryComponent>().WithItemEquipped(out ItemComponent item))
        //         ItemAssetLink.GetModifier(item.id).ModifyTrusted(session, playerId, trustedPlayer, commands, container, durationUs);
        // }

        private static void ModifyEquipStatus(SessionBase session, int playerId, InventoryComponent inventory, WantedItemIndexProperty wantedItemIndex, uint durationUs)
        {
            byte wantedIndex = wantedItemIndex;
            ByteStatusComponent equipStatus = inventory.equipStatus;
            // Unequip current item if desired
            bool
                hasValidWantedIndex = wantedIndex != NoneIndex && inventory[wantedIndex].id != ItemId.None,
                wantsNewIndex = wantedIndex != inventory.equippedIndex,
                isAlreadyUnequipping = equipStatus.id == ItemEquipStatusId.Unequipping;
            if (hasValidWantedIndex && wantsNewIndex && !isAlreadyUnequipping)
            {
                equipStatus.id.Value = ItemEquipStatusId.Unequipping;
                equipStatus.elapsedUs.Value = 0u;
            }

            if (inventory.HasNoItemEquipped) return;
            // We have a current equipped item
            equipStatus.elapsedUs.Value += durationUs;
            ItemModifierBase modifier = ItemAssetLink.GetModifier(inventory.EquippedItemComponent.id);

            // Handle finishing equip status
            ItemStatusModiferProperties modifierProperties;
            while (equipStatus.elapsedUs > (modifierProperties = modifier.GetEquipStatusModifierProperties(equipStatus.id)).durationUs)
            {
                if (equipStatus.id == ItemEquipStatusId.Equipping)
                {
                    equipStatus.id.Value = ItemEquipStatusId.Equipped;
                    modifier.OnEquip(session, playerId, inventory.EquippedItemComponent, durationUs);
                }
                else if (equipStatus.id == ItemEquipStatusId.Unequipping)
                {
                    equipStatus.id.Value = ItemEquipStatusId.Unequipped;
                    modifier.OnUnequip(session, playerId, inventory, inventory.EquippedItemComponent, durationUs);
                }
                equipStatus.elapsedUs.Value -= modifierProperties.durationUs;
            }

            if (equipStatus.id != ItemEquipStatusId.Unequipped) return;
            // We have unequipped the current index
            if (hasValidWantedIndex)
            {
                inventory.previousEquippedIndex.Value = inventory.equippedIndex.Value;
                inventory.equippedIndex.Value = wantedIndex;
            }
            else if (FindReplacement(inventory, out byte replacementIndex))
                inventory.equippedIndex.Value = replacementIndex;
            else
                inventory.equippedIndex.Value = NoneIndex;
            equipStatus.id.Value = ItemEquipStatusId.Equipping;
        }

        private static void ModifyAdsStatus(InventoryComponent inventory, InputFlagProperty inputs, uint durationUs)
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
            adsStatus.elapsedUs.Value += durationUs;

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
            inputs.SetInput(PlayerInput.UseOne, InputProvider.GetInput(PlayerInput.UseOne));
            inputs.SetInput(PlayerInput.UseTwo, InputProvider.GetInput(PlayerInput.UseTwo));
            inputs.SetInput(PlayerInput.UseThree, InputProvider.GetInput(PlayerInput.UseThree));
            inputs.SetInput(PlayerInput.Reload, InputProvider.GetInput(PlayerInput.Reload));
            inputs.SetInput(PlayerInput.Fly, InputProvider.GetInput(PlayerInput.Fly));
            inputs.SetInput(PlayerInput.Ads, InputProvider.GetInput(PlayerInput.Ads));
            inputs.SetInput(PlayerInput.Throw, InputProvider.GetInput(PlayerInput.Throw));
            inputs.SetInput(PlayerInput.DropItem, InputProvider.GetInput(PlayerInput.DropItem));
            if (commands.Without(out WantedItemIndexProperty itemIndex)) return;
            if (InputProvider.GetInput(PlayerInput.ItemOne)) itemIndex.Value = 1;
            else if (InputProvider.GetInput(PlayerInput.ItemTwo)) itemIndex.Value = 2;
            else if (InputProvider.GetInput(PlayerInput.ItemThree)) itemIndex.Value = 3;
            else if (InputProvider.GetInput(PlayerInput.ItemFour)) itemIndex.Value = 4;
            else if (InputProvider.GetInput(PlayerInput.ItemFive)) itemIndex.Value = 5;
            else if (InputProvider.GetInput(PlayerInput.ItemSix)) itemIndex.Value = 6;
            else if (InputProvider.GetInput(PlayerInput.ItemSeven)) itemIndex.Value = 7;
            else if (InputProvider.GetInput(PlayerInput.ItemEight)) itemIndex.Value = 8;
            else if (InputProvider.GetInput(PlayerInput.ItemNine)) itemIndex.Value = 9;
            else if (InputProvider.GetInput(PlayerInput.ItemTen)) itemIndex.Value = 10;
            else if (InputProvider.GetInput(PlayerInput.ItemLast))
            {
                ByteProperty previousEquipped = commands.Require<InventoryComponent>().previousEquippedIndex;
                if (previousEquipped.WithValue) itemIndex.Value = previousEquipped;
            }
        }

        private static bool FindItem(InventoryComponent inventory, Predicate<ItemComponent> predicate, out byte index)
        {
            index = default;
            for (byte itemIndex = 1; itemIndex <= inventory.items.Length; itemIndex++)
            {
                if (!predicate(inventory[itemIndex])) continue;
                index = itemIndex;
                return true;
            }
            return false;
        }

        public static bool FindEmpty(InventoryComponent inventory, out byte emptyIndex)
            => FindItem(inventory, item => item.id == ItemId.None, out emptyIndex);

        private static bool FindReplacement(InventoryComponent inventory, out byte replacementIndex)
        {
            if (inventory.previousEquippedIndex == NoneIndex || inventory[inventory.previousEquippedIndex].id == ItemId.None)
                return FindItem(inventory, item => item.id != ItemId.None, out replacementIndex);
            replacementIndex = inventory.previousEquippedIndex;
            return true;
        }

        public static void AddItems(InventoryComponent inventory, params byte[] itemIds)
        {
            foreach (byte itemId in itemIds) AddItem(inventory, itemId);
        }

        public static bool AddItem(InventoryComponent inventory, byte itemId, ushort count = 1)
        {
            bool isAnOpenSlot;
            if (ItemAssetLink.GetModifier(itemId) is ThrowableModifierBase &&
                (isAnOpenSlot = FindItem(inventory, item => item.id == ItemId.None || item.id == itemId, out byte index))) // Try to stack throwables if possible
            {
                ItemComponent itemInIndex = inventory[index];
                bool addingToExisting = itemInIndex.id == itemId;
                if (addingToExisting) count += itemInIndex.ammoInReserve;
            }
            else isAnOpenSlot = FindEmpty(inventory, out index);
            if (isAnOpenSlot) SetItemAtIndex(inventory, itemId, index, count);
            return isAnOpenSlot;
        }

        public static void RefillAllAmmo(InventoryComponent inventory)
        {
            for (var i = 1; i <= inventory.items.Length; i++)
            {
                ItemComponent item = inventory[i];
                if (item.id == ItemId.None) continue;
                if (ItemAssetLink.GetModifier(item.id) is GunModifierBase gunModifier)
                {
                    item.ammoInMag.Value = gunModifier.MagSize;
                    item.ammoInReserve.Value = gunModifier.StartingAmmoInReserve;
                }           
            }
        }

        public static void SetItemAtIndex(InventoryComponent inventory, byte itemId, int index, ushort count = 1)
        {
            ItemComponent item = inventory[index];
            item.id.Value = itemId;
            if (itemId == ItemId.None)
            {
                if (inventory.equippedIndex == index)
                {
                    inventory.equippedIndex.Value = FindReplacement(inventory, out byte replacementIndex) ? replacementIndex : NoneIndex;
                    inventory.equipStatus.id.Value = ItemEquipStatusId.Equipping;
                    inventory.equipStatus.elapsedUs.Value = 0u;
                }
                return;
            }
            item.status.id.Value = ItemStatusId.Idle;
            item.status.elapsedUs.Value = 0u;
            ItemModifierBase itemModifier = ItemAssetLink.GetModifier(itemId);
            if (itemModifier is GunModifierBase gunModifier)
            {
                item.ammoInMag.Value = gunModifier.MagSize;
                item.ammoInReserve.Value = gunModifier.StartingAmmoInReserve;
            }
            if (itemModifier is ThrowableModifierBase)
                item.ammoInReserve.Value = count;
            if (inventory.HasNoItemEquipped)
            {
                inventory.equippedIndex.Value = FindReplacement(inventory, out byte replacementIndex) ? replacementIndex : NoneIndex;
                inventory.equipStatus.id.Value = ItemEquipStatusId.Equipping;
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