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

        public override void ModifyChecked(Container containerToModify, Container commands, float duration)
        {
            if (!containerToModify.Has(out InventoryComponent inventoryComponent)) return;

            var inputProperty = commands.Require<InputFlagProperty>();
            var wantedItemIndexProperty = commands.Require<WantedItemIndexProperty>();

            ModifyEquipStatus(inventoryComponent, wantedItemIndexProperty, duration);

            if (inventoryComponent.HasNoItemEquipped) return;

            ModifyAdsStatus(inventoryComponent, inputProperty, duration);

            // Modify equipped item component
            ItemComponent equippedItemComponent = inventoryComponent.EquippedItemComponent;
            ItemModifierBase modifier = ItemManager.GetModifier(equippedItemComponent.id);
            modifier.ModifyChecked((equippedItemComponent, inventoryComponent), inputProperty, duration);
        }

        private static void ModifyEquipStatus(InventoryComponent inventoryComponent, WantedItemIndexProperty wantedItemIndexProperty, float duration)
        {
            byte wantedIndex = wantedItemIndexProperty;
            ByteStatusComponent equipStatus = inventoryComponent.equipStatus;
            // Unequip current item if desired
            bool
                hasValidWantedIndex = wantedIndex != NoneIndex && inventoryComponent.itemComponents[wantedIndex - 1].id != ItemId.None,
                wantsNewIndex = wantedIndex != inventoryComponent.equippedIndex,
                isAlreadyUnequipping = equipStatus.id == ItemEquipStatusId.Unequipping;
            if (hasValidWantedIndex && wantsNewIndex && !isAlreadyUnequipping)
            {
                equipStatus.id.Value = ItemEquipStatusId.Unequipping;
                equipStatus.elapsed.Value = 0.0f;
            }

            if (inventoryComponent.HasNoItemEquipped) return;
            // We have a current equipped item
            equipStatus.elapsed.Value += duration;
            ItemModifierBase modifier = ItemManager.GetModifier(inventoryComponent.EquippedItemComponent.id);

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
            ItemComponent equippedItemComponent = inventoryComponent.EquippedItemComponent;
            modifier.OnUnequip(equippedItemComponent);
            if (hasValidWantedIndex)
                inventoryComponent.equippedIndex.Value = wantedIndex;
            else if (FindReplacement(inventoryComponent, out byte replacementIndex))
                inventoryComponent.equippedIndex.Value = replacementIndex;
            else
                inventoryComponent.equippedIndex.Value = NoneIndex;
            equipStatus.id.Value = ItemEquipStatusId.Equipping;
        }

        private static void ModifyAdsStatus(InventoryComponent inventoryComponent, InputFlagProperty inputsProperty, float duration)
        {
            ItemModifierBase modifier = ItemManager.GetModifier(inventoryComponent.EquippedItemComponent.id);
            if (!(modifier is GunModifierBase gunModifier)) return;

            if (inputsProperty.GetInput(PlayerInput.Ads))
            {
                if (inventoryComponent.adsStatus.id == AdsStatusId.HipAiming)
                {
                    inventoryComponent.adsStatus.id.Value = AdsStatusId.EnteringAds;
                    inventoryComponent.adsStatus.elapsed.Value = 0.0f;
                }
            }
            else
            {
                if (inventoryComponent.adsStatus.id == AdsStatusId.Ads)
                {
                    inventoryComponent.adsStatus.id.Value = AdsStatusId.ExitingAds;
                    inventoryComponent.adsStatus.elapsed.Value = 0.0f;
                }
            }

            ByteStatusComponent adsStatus = inventoryComponent.adsStatus;
            adsStatus.elapsed.Value += duration;

            ItemStatusModiferProperties modifierProperties;
            while (adsStatus.elapsed > (modifierProperties = gunModifier.GetAdsStatusModifierProperties(adsStatus.id)).duration)
            {
                if (adsStatus.id == AdsStatusId.EnteringAds) adsStatus.id.Value = AdsStatusId.Ads;
                else if (adsStatus.id == AdsStatusId.ExitingAds) adsStatus.id.Value = AdsStatusId.HipAiming;
                adsStatus.elapsed.Value -= modifierProperties.duration;
            }
        }

        protected override void SynchronizeBehavior(Container componentToApply)
        {
        }

        public override void ModifyCommands(Container commands)
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

        private static bool FindReplacement(InventoryComponent inventoryComponent, out byte replacementIndex)
        {
            var hasFoundReplacement = false;
            replacementIndex = 0;
            for (byte itemIndex = 1; !hasFoundReplacement && itemIndex <= inventoryComponent.itemComponents.Length; itemIndex++)
            {
                if (inventoryComponent.itemComponents[itemIndex - 1].id == ItemId.None) continue;
                replacementIndex = itemIndex;
                hasFoundReplacement = true;
            }
            return hasFoundReplacement;
        }

        public static void SetItemAtIndex(InventoryComponent inventoryComponent, byte itemId, int index)
        {
            if (index <= NoneIndex) throw new ArgumentException("Invalid item index");
            ItemComponent itemComponent = inventoryComponent.itemComponents[index - 1];
            itemComponent.id.Value = itemId;
            if (itemId == ItemId.None) return;
            itemComponent.status.id.Value = ItemStatusId.Idle;
            itemComponent.status.elapsed.Value = 0.0f;
            itemComponent.gunStatus.ammoInMag.Value = 30;
            itemComponent.gunStatus.ammoInReserve.Value = 240;
            inventoryComponent.equipStatus.id.Value = ItemEquipStatusId.Equipping;
            inventoryComponent.equipStatus.elapsed.Value = 0.0f;
            if (inventoryComponent.HasItemEquipped && inventoryComponent.equippedIndex != index) return;
            // If this replaces existing equipped item, find new one to equip
            inventoryComponent.equippedIndex.Value = FindReplacement(inventoryComponent, out byte replacementIndex) ? replacementIndex : NoneIndex;
        }
    }
}