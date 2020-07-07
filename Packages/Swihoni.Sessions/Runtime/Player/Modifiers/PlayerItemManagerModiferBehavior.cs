using System;
using Input;
using Swihoni.Components;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerItemManagerModiferBehavior : PlayerModifierBehaviorBase
    {
        public const byte NoneIndex = 0;

        public override void ModifyChecked(SessionBase session, int playerId, Container player, Container commands, uint durationUs, int tickDelta)
        {
            if (!player.With(out InventoryComponent inventory) || player.WithPropertyWithValue(out HealthProperty health) && health.IsDead) return;

            var input = commands.Require<InputFlagProperty>();
            var wantedItemIndex = commands.Require<WantedItemIndexProperty>();

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
                inventory.equipStatus.id.Value = ItemEquipStatusId.Equipping;
                inventory.equipStatus.elapsedUs.Value = 0u;
            }
        }

        public override void ModifyTrusted(SessionBase session, int playerId, Container trustedPlayer, Container commands, Container container, uint durationUs)
        {
            if (trustedPlayer.Require<InventoryComponent>().WithItemEquipped(out ItemComponent item))
                ItemAssetLink.GetModifier(item.id).ModifyTrusted(session, playerId, trustedPlayer, commands, container, durationUs);
        }

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
                    modifier.OnUnequip(session, playerId, inventory.EquippedItemComponent, durationUs);
                }
                equipStatus.elapsedUs.Value -= modifierProperties.durationUs;
            }

            if (equipStatus.id != ItemEquipStatusId.Unequipped) return;
            // We have unequipped the current index
            if (hasValidWantedIndex)
                inventory.equippedIndex.Value = wantedIndex;
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

        public override void ModifyCommands(SessionBase session, Container commands)
        {
            if (commands.Without(out InputFlagProperty inputs)) return;
            InputProvider provider = InputProvider.Singleton;
            inputs.SetInput(PlayerInput.UseOne, provider.GetInput(InputType.UseOne));
            inputs.SetInput(PlayerInput.UseTwo, provider.GetInput(InputType.UseTwo));
            inputs.SetInput(PlayerInput.Reload, provider.GetInput(InputType.Reload));
            inputs.SetInput(PlayerInput.Fly, provider.GetInput(InputType.Fly));
            inputs.SetInput(PlayerInput.Ads, provider.GetInput(InputType.Ads));
            inputs.SetInput(PlayerInput.Throw, provider.GetInput(InputType.Throw));
            if (commands.Without(out WantedItemIndexProperty itemIndex)) return;
            if (provider.GetInput(InputType.ItemOne)) itemIndex.Value = 1;
            else if (provider.GetInput(InputType.ItemTwo)) itemIndex.Value = 2;
            else if (provider.GetInput(InputType.ItemThree)) itemIndex.Value = 3;
            else if (provider.GetInput(InputType.ItemFour)) itemIndex.Value = 4;
            else if (provider.GetInput(InputType.ItemFive)) itemIndex.Value = 5;
            else if (provider.GetInput(InputType.ItemSix)) itemIndex.Value = 6;
            else if (provider.GetInput(InputType.ItemSeven)) itemIndex.Value = 7;
            else if (provider.GetInput(InputType.ItemEight)) itemIndex.Value = 8;
            else if (provider.GetInput(InputType.ItemNine)) itemIndex.Value = 9;
            else if (provider.GetInput(InputType.ItemTen)) itemIndex.Value = 10;
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
            => FindItem(inventory, item => item.id != ItemId.None, out replacementIndex);

        public static void AddItems(InventoryComponent inventory, params byte[] itemIds)
        {
            foreach (byte itemId in itemIds) AddItem(inventory, itemId);
        }

        public static void AddItem(InventoryComponent inventory, byte itemId, ushort count = 1)
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