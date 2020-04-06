using Input;
using Session.Items;
using Session.Items.Modifiers;
using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Modifiers
{
    public class PlayerItemManagerModiferBehavior : ModifierBehaviorBase<PlayerComponent>
    {
        public const byte NoneIndex = 0;

        public override void ModifyChecked(PlayerComponent componentToModify, PlayerCommandsComponent commands)
        {
            byte wantedIndex = commands.wantedItemIndex;
            PlayerInventoryComponent inventoryComponent = componentToModify.inventory;
            ByteStatusComponent equipStatus = inventoryComponent.equipStatus;
            bool
                isValidWantedIndex = wantedIndex != NoneIndex && inventoryComponent.itemComponents[wantedIndex - 1].id != ItemId.None,
                wantsNewIndex = commands.wantedItemIndex != inventoryComponent.equippedIndex,
                isAlreadyUnequipping = equipStatus.id == ItemEquipStatusId.Unequipping;
            // Set our current item to unequipping if we have a new, valid, input and are not already unequipping
            if (isValidWantedIndex && wantsNewIndex && !isAlreadyUnequipping)
            {
                equipStatus.id.Value = ItemEquipStatusId.Unequipping;
                equipStatus.elapsed.Value = 0.0f;
            }
            if (inventoryComponent.equippedIndex == NoneIndex) return;
            ModifyEquipStatus(inventoryComponent, commands);
            ItemComponent equippedItemComponent = inventoryComponent.EquippedItemComponent;
            ItemModifierBase modifier = ItemManager.GetModifier(equippedItemComponent.id);
            if (equipStatus.id == ItemEquipStatusId.Unequipped)
            {
                modifier.OnUnequip(equippedItemComponent);
                if (isValidWantedIndex)
                    inventoryComponent.equippedIndex.Value = commands.wantedItemIndex;
                else if (FindReplacement(inventoryComponent, out byte replacementIndex))
                    inventoryComponent.equippedIndex.Value = replacementIndex;
                else
                    inventoryComponent.equippedIndex.Value = NoneIndex;
                equipStatus.id.Value = ItemEquipStatusId.Equipping;
            }
            modifier.ModifyChecked((equippedItemComponent, equipStatus), commands);
        }

        private static void ModifyEquipStatus(PlayerInventoryComponent inventoryComponent, PlayerCommandsComponent commands)
        {
            ByteStatusComponent equipStatus = inventoryComponent.equipStatus;
            equipStatus.elapsed.Value += commands.duration;
            ItemStatusModiferProperties modifierProperties;
            while (equipStatus.elapsed >
                   (modifierProperties = ItemManager.GetModifier(inventoryComponent.EquippedItemComponent.id).GetEquipStatusModifierProperties(equipStatus.id)).duration)
            {
                if (equipStatus.id == ItemEquipStatusId.Equipping) equipStatus.id.Value = ItemEquipStatusId.Equipped;
                else if (equipStatus.id == ItemEquipStatusId.Unequipping) equipStatus.id.Value = ItemEquipStatusId.Unequipped;
                if (Mathf.Approximately(modifierProperties.duration, 0.0f))
                    break;
                equipStatus.elapsed.Value -= modifierProperties.duration;
            }
        }

        protected override void SynchronizeBehavior(PlayerComponent componentToApply)
        {
        }

        public override void ModifyCommands(PlayerCommandsComponent commandsToModify)
        {
            InputProvider input = InputProvider.Singleton;
            commandsToModify.SetInput(PlayerInput.UseOne, input.GetInput(InputType.UseOne));
            commandsToModify.SetInput(PlayerInput.UseTwo, input.GetInput(InputType.UseTwo));
            commandsToModify.SetInput(PlayerInput.Reload, input.GetInput(InputType.Reload));
            if (input.GetInput(InputType.ItemOne))
                commandsToModify.wantedItemIndex.Value = 1;
            else if (input.GetInput(InputType.ItemTwo))
                commandsToModify.wantedItemIndex.Value = 2;
            else if (input.GetInput(InputType.ItemThree))
                commandsToModify.wantedItemIndex.Value = 3;
        }

        private static bool FindReplacement(PlayerInventoryComponent inventoryComponent, out byte replacementIndex)
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

        public static void SetItemAtIndex(PlayerInventoryComponent inventoryComponent, byte itemId, int index)
        {
            ItemComponent itemComponent = inventoryComponent.itemComponents[index - 1];
            itemComponent.id.Value = itemId;
            if (itemId == ItemId.None) return;
            itemComponent.status.id.Value = ItemStatusId.Idle;
            itemComponent.status.elapsed.Value = 0.0f;
            itemComponent.gunStatus.ammoInMag.Value = 30;
            itemComponent.gunStatus.ammoInReserve.Value = 240;
            inventoryComponent.equipStatus.id.Value = ItemEquipStatusId.Equipping;
            inventoryComponent.equipStatus.elapsed.Value = 0.0f;
            if (inventoryComponent.equippedIndex != NoneIndex && inventoryComponent.equippedIndex != index) return;
            // If this replaces existing equipped item, find new one to equip
            inventoryComponent.equippedIndex.Value = FindReplacement(inventoryComponent, out byte replacementIndex) ? replacementIndex : NoneIndex;
        }
    }
}