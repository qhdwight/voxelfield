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

        public override void ModifyChecked(SessionBase session, int playerId, Container player, Container commands, float duration)
        {
            if (!player.Has(out InventoryComponent inventoryComponent)) return;

            var inputProperty = commands.Require<InputFlagProperty>();
            var wantedItemIndexProperty = commands.Require<WantedItemIndexProperty>();

            ModifyEquipStatus(session, playerId, inventoryComponent, wantedItemIndexProperty, duration);

            if (inventoryComponent.HasNoItemEquipped) return;

            ModifyAdsStatus(inventoryComponent, inputProperty, duration);

            // Modify equipped item component
            ItemComponent equippedItemComponent = inventoryComponent.EquippedItemComponent;
            ItemModifierBase itemModifier = ItemManager.GetModifier(equippedItemComponent.id);
            itemModifier.ModifyChecked(session, playerId, equippedItemComponent, inventoryComponent, inputProperty, duration);
        }

        private static void ModifyEquipStatus(SessionBase session, int playerId, InventoryComponent inventory, WantedItemIndexProperty wantedItemIndex, float duration)
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
                equipStatus.elapsed.Value = 0.0f;
            }

            if (inventory.HasNoItemEquipped) return;
            // We have a current equipped item
            equipStatus.elapsed.Value += duration;
            ItemModifierBase modifier = ItemManager.GetModifier(inventory.EquippedItemComponent.id);

            // Handle finishing equip status
            ItemStatusModiferProperties modifierProperties;
            while (equipStatus.elapsed > (modifierProperties = modifier.GetEquipStatusModifierProperties(equipStatus.id)).duration)
            {
                if (equipStatus.id == ItemEquipStatusId.Equipping) equipStatus.id.Value = ItemEquipStatusId.Equipped;
                else if (equipStatus.id == ItemEquipStatusId.Unequipping) equipStatus.id.Value = ItemEquipStatusId.Unequipped;
                equipStatus.elapsed.Value -= modifierProperties.duration;
            }

            if (equipStatus.id != ItemEquipStatusId.Unequipped) return;
            // We have just unequipped the current index
            ItemComponent equippedItemComponent = inventory.EquippedItemComponent;
            modifier.OnUnequip(session, playerId, equippedItemComponent);
            if (hasValidWantedIndex)
                inventory.equippedIndex.Value = wantedIndex;
            else if (FindReplacement(inventory, out byte replacementIndex))
                inventory.equippedIndex.Value = replacementIndex;
            else
                inventory.equippedIndex.Value = NoneIndex;
            equipStatus.id.Value = ItemEquipStatusId.Equipping;
        }

        private static void ModifyAdsStatus(InventoryComponent inventory, InputFlagProperty inputs, float duration)
        {
            ItemModifierBase modifier = ItemManager.GetModifier(inventory.EquippedItemComponent.id);
            if (!(modifier is GunModifierBase gunModifier)) return;

            if (inputs.GetInput(PlayerInput.Ads))
            {
                if (inventory.adsStatus.id == AdsStatusId.HipAiming)
                {
                    inventory.adsStatus.id.Value = AdsStatusId.EnteringAds;
                    inventory.adsStatus.elapsed.Value = 0.0f;
                }
            }
            else
            {
                if (inventory.adsStatus.id == AdsStatusId.Ads)
                {
                    inventory.adsStatus.id.Value = AdsStatusId.ExitingAds;
                    inventory.adsStatus.elapsed.Value = 0.0f;
                }
            }

            ByteStatusComponent adsStatus = inventory.adsStatus;
            adsStatus.elapsed.Value += duration;

            ItemStatusModiferProperties modifierProperties;
            while (adsStatus.elapsed > (modifierProperties = gunModifier.GetAdsStatusModifierProperties(adsStatus.id)).duration)
            {
                if (adsStatus.id == AdsStatusId.EnteringAds) adsStatus.id.Value = AdsStatusId.Ads;
                else if (adsStatus.id == AdsStatusId.ExitingAds) adsStatus.id.Value = AdsStatusId.HipAiming;
                adsStatus.elapsed.Value -= modifierProperties.duration;
            }
        }

        protected override void SynchronizeBehavior(Container player) { }

        public override void ModifyCommands(SessionBase session, Container commands)
        {
            if (!commands.Has(out InputFlagProperty inputProperty)) return;
            InputProvider input = InputProvider.Singleton;
            inputProperty.SetInput(PlayerInput.UseOne, input.GetInput(InputType.UseOne));
            inputProperty.SetInput(PlayerInput.UseTwo, input.GetInput(InputType.UseTwo));
            inputProperty.SetInput(PlayerInput.Reload, input.GetInput(InputType.Reload));
            inputProperty.SetInput(PlayerInput.Ads, input.GetInput(InputType.Ads));
            if (!commands.Has(out WantedItemIndexProperty itemIndexProperty)) return;
            if (input.GetInput(InputType.ItemOne))
                itemIndexProperty.Value = 1;
            else if (input.GetInput(InputType.ItemTwo))
                itemIndexProperty.Value = 2;
            else if (input.GetInput(InputType.ItemThree))
                itemIndexProperty.Value = 3;
            else
                itemIndexProperty.Value = 0;
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
            itemComponent.status.elapsed.Value = 0.0f;
            itemComponent.gunStatus.ammoInMag.Value = 30;
            itemComponent.gunStatus.ammoInReserve.Value = 240;
            inventory.equipStatus.id.Value = ItemEquipStatusId.Equipping;
            inventory.equipStatus.elapsed.Value = 0.0f;
            if (inventory.HasItemEquipped && inventory.equippedIndex != index) return;
            // If this replaces existing equipped item, find new one to equip
            inventory.equippedIndex.Value = FindReplacement(inventory, out byte replacementIndex) ? replacementIndex : NoneIndex;
        }
    }
}