using System;
using Components;
using Session.Items;
using Session.Items.Modifiers;
using Session.Player.Modifiers;
using UnityEngine;

namespace Session.Player.Components
{
    [Serializable]
    public class CameraComponent : ComponentBase
    {
        public FloatProperty yaw, pitch;
    }

    [Serializable]
    public class MoveComponent : ComponentBase
    {
        public VectorProperty position, velocity;
        public ByteProperty groundTick;
    }

    [Serializable]
    public class HealthProperty : ByteProperty
    {
        public bool IsAlive => !IsDead;
        public bool IsDead => Value == 0;
    }

    [Serializable]
    public class GunStatusComponent : ComponentBase
    {
        public UShortProperty ammoInMag, ammoInReserve;
    }

    [Serializable]
    public class ByteStatusComponent : ComponentBase
    {
        [CustomInterpolation] public ByteProperty id;
        [CustomInterpolation] public FloatProperty elapsed;

        public void InterpolateFrom(ByteStatusComponent s1, ByteStatusComponent s2, float interpolation, Func<byte, float> getStatusDuration)
        {
            float d1 = getStatusDuration(s1.id),
                  e1 = s1.elapsed,
                  e2 = s2.elapsed;
            if (s1.id != s2.id) e2 += d1;
            else if (e1 > e2) e2 += d1;
            float interpolatedElapsed = Mathf.Lerp(e1, e2, interpolation);
            byte interpolatedId;
            if (interpolatedElapsed > d1)
            {
                interpolatedElapsed -= d1;
                interpolatedId = s2.id;
            }
            else
                interpolatedId = s1.id;
            id.Value = interpolatedId;
            elapsed.Value = interpolatedElapsed;
        }

        public override string ToString()
        {
            return $"ID: {id}, Elapsed: {elapsed}";
        }
    }

    [Serializable]
    public class ItemComponent : ComponentBase
    {
        public GunStatusComponent gunStatus;
        public ByteProperty id;
        public ByteStatusComponent status;

        // Embedded item components are only explicitly interpolated, since usually it only needs to be done on equipped item
        public void InterpolateFrom(ItemComponent i1, ItemComponent i2, float interpolation)
        {
            ItemModifierBase modifier = ItemManager.GetModifier(i1.id);
            status.InterpolateFrom(i1.status, i2.status, interpolation, statusId => modifier.GetStatusModifierProperties(statusId).duration);
        }
    }

    [Serializable]
    public class InventoryComponent : ComponentBase
    {
        public ByteProperty equippedIndex;
        public ByteStatusComponent equipStatus, adsStatus;
        public ArrayProperty<ItemComponent> itemComponents = new ArrayProperty<ItemComponent>(10);

        public ItemComponent EquippedItemComponent => itemComponents[equippedIndex - 1];
        public bool HasItemEquipped => !HasNoItemEquipped;
        public bool HasNoItemEquipped => equippedIndex == PlayerItemManagerModiferBehavior.NoneIndex;

        public override void InterpolateFrom(object c1, object c2, float interpolation)
        {
            var i1 = (InventoryComponent) c1;
            var i2 = (InventoryComponent) c2;
            if (i1.HasNoItemEquipped || i2.HasNoItemEquipped)
            {
                equippedIndex.Value = PlayerItemManagerModiferBehavior.NoneIndex;
                return;
            }
            ItemModifierBase modifier = ItemManager.GetModifier(i1.EquippedItemComponent.id);
            if (modifier is GunModifierBase gunModifier)
                adsStatus.InterpolateFrom(i1.adsStatus, i2.adsStatus, interpolation, aimStatusId => gunModifier.GetAdsStatusModifierProperties(aimStatusId).duration);
            equipStatus.InterpolateFrom(i1.equipStatus, i2.equipStatus, interpolation, equipStatusId => modifier.GetEquipStatusModifierProperties(equipStatusId).duration);
            equippedIndex = i1.equippedIndex;
            // TODO: handle when id of equipped weapon changes
            EquippedItemComponent.InterpolateFrom(i1.EquippedItemComponent, i2.EquippedItemComponent, interpolation);
        }
    }
}