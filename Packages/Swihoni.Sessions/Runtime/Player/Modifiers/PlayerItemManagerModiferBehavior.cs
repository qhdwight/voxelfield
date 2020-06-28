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

        public override void ModifyChecked(SessionBase session, int playerId, Container player, Container commands, uint durationUs)
        {
            if (!player.With(out InventoryComponent inventoryComponent) || player.WithPropertyWithValue(out HealthProperty health) && health.IsDead) return;

            var inputProperty = commands.Require<InputFlagProperty>();
            var wantedItemIndexProperty = commands.Require<WantedItemIndexProperty>();

            ModifyEquipStatus(session, playerId, inventoryComponent, wantedItemIndexProperty, durationUs);

            if (inventoryComponent.HasNoItemEquipped) return;

            ModifyAdsStatus(inventoryComponent, inputProperty, durationUs);

            // Modify equipped item component
            ItemComponent equippedItemComponent = inventoryComponent.EquippedItemComponent;
            ItemModifierBase itemModifier = ItemAssetLink.GetModifier(equippedItemComponent.id);
            itemModifier.ModifyChecked(session, playerId, player, equippedItemComponent, inventoryComponent, inputProperty, durationUs);
        }

        public override void ModifyTrusted(SessionBase session, int playerId, Container trustedPlayer, Container commands, Container container, uint durationUs) { }

        private static void ModifyEquipStatus(SessionBase session, int playerId, InventoryComponent inventory, WantedItemIndexProperty wantedItemIndex, uint durationUs)
        {
            byte wantedIndex = wantedItemIndex;
            ByteStatusComponent equipStatus = inventory.equipStatus;
            // Unequip current item if desired
            bool
                hasValidWantedIndex = wantedIndex != NoneIndex && inventory.itemComponents[wantedIndex - 1].id != ItemId.None,
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
                if (equipStatus.id == ItemEquipStatusId.Equipping) equipStatus.id.Value = ItemEquipStatusId.Equipped;
                else if (equipStatus.id == ItemEquipStatusId.Unequipping) equipStatus.id.Value = ItemEquipStatusId.Unequipped;
                equipStatus.elapsedUs.Value -= modifierProperties.durationUs;
            }

            if (equipStatus.id != ItemEquipStatusId.Unequipped) return;
            // We have just unequipped the current index
            ItemComponent equippedItemComponent = inventory.EquippedItemComponent;
            modifier.OnUnequip(session, playerId, equippedItemComponent, durationUs);
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
            InputProvider inputProvider = InputProvider.Singleton;
            inputs.SetInput(PlayerInput.UseOne, inputProvider.GetInput(InputType.UseOne));
            inputs.SetInput(PlayerInput.UseTwo, inputProvider.GetInput(InputType.UseTwo));
            inputs.SetInput(PlayerInput.Reload, inputProvider.GetInput(InputType.Reload));
            inputs.SetInput(PlayerInput.Fly, inputProvider.GetInput(InputType.Fly));
            inputs.SetInput(PlayerInput.Ads, inputProvider.GetInput(InputType.Ads));
            if (commands.Without(out WantedItemIndexProperty itemIndex)) return;
            if (inputProvider.GetInput(InputType.ItemOne)) itemIndex.Value = 1;
            else if (inputProvider.GetInput(InputType.ItemTwo)) itemIndex.Value = 2;
            else if (inputProvider.GetInput(InputType.ItemThree)) itemIndex.Value = 3;
            else if (inputProvider.GetInput(InputType.ItemFour)) itemIndex.Value = 4;
            else if (inputProvider.GetInput(InputType.ItemFive)) itemIndex.Value = 5;
            else if (inputProvider.GetInput(InputType.ItemSix)) itemIndex.Value = 6;
            else if (inputProvider.GetInput(InputType.ItemSeven)) itemIndex.Value = 7;
            else if (inputProvider.GetInput(InputType.ItemEight)) itemIndex.Value = 8;
            else if (inputProvider.GetInput(InputType.ItemNine)) itemIndex.Value = 9;
        }

        private static bool FindReplacement(InventoryComponent inventory, out byte replacementIndex)
        {
            var hasFoundReplacement = false;
            replacementIndex = 0;
            for (byte itemIndex = 1; !hasFoundReplacement && itemIndex <= inventory.itemComponents.Length; itemIndex++)
            {
                if (inventory.itemComponents[itemIndex - 1].id == ItemId.None) continue;
                replacementIndex = itemIndex;
                hasFoundReplacement = true;
            }
            return hasFoundReplacement;
        }

        public static void SetItemAtIndex(InventoryComponent inventory, byte itemId, int index)
        {
            if (index <= NoneIndex) throw new ArgumentException("Invalid item index");
            ItemComponent itemComponent = inventory.itemComponents[index - 1];
            itemComponent.id.Value = itemId;
            if (itemId == ItemId.None) return;
            itemComponent.status.id.Value = ItemStatusId.Idle;
            itemComponent.status.elapsedUs.Value = 0u;
            ItemModifierBase itemModifier = ItemAssetLink.GetModifier(itemId);
            if (itemModifier is GunModifierBase gunModifier)
            {
                itemComponent.gunStatus.ammoInMag.Value = gunModifier.MagSize;
                itemComponent.gunStatus.ammoInReserve.Value = gunModifier.StartingAmmoInReserve;
            }
            inventory.equipStatus.id.Value = ItemEquipStatusId.Equipping;
            inventory.equipStatus.elapsedUs.Value = 0u;
            if (inventory.HasItemEquipped && inventory.equippedIndex != index) return;
            // If this replaces existing equipped item, find new one to equip
            inventory.equippedIndex.Value = FindReplacement(inventory, out byte replacementIndex) ? replacementIndex : NoneIndex;
        }
    }
}