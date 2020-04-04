using System;
using Components;
using Session.Items;
using Session.Items.Modifiers;
using UnityEngine;

// ReSharper disable UnassignedField.Global

namespace Session.Player.Components
{
    [Serializable]
    public class PlayerComponent : ComponentBase
    {
        [TakeSecondForInterpolation] public ByteProperty health;
        public PlayerInventoryComponent inventory;
        public VectorProperty position;
        public FloatProperty yaw, pitch;

        public bool IsAlive => !IsDead;
        public bool IsDead => health == 0;
    }

    [Serializable]
    public class GunStatusComponent : ComponentBase
    {
        public ItemByteStatusComponent aimStatus;
        public UShortProperty ammoInMag, ammoInReserve;
    }

    [Serializable]
    public class ItemByteStatusComponent : ByteStatusComponentBase
    {
        public void InterpolateFrom(ItemByteStatusComponent i1, ItemByteStatusComponent i2, float interpolation, byte itemId)
        {
            InterpolateFrom(i1, i2, interpolation, statusId => ItemManager.Singleton.GetModifier(itemId).GetStatusModifierProperties(statusId).duration);
        }
    }

    [Serializable]
    public abstract class ByteStatusComponentBase : ComponentBase
    {
        [CustomInterpolation] public ByteProperty id;
        [CustomInterpolation] public FloatProperty elapsed;

        protected void InterpolateFrom(ByteStatusComponentBase s1, ByteStatusComponentBase s2, float interpolation, Func<byte, float> getStatusDuration)
        {
            if (s1.id == ItemId.None) return;
            float d1 = getStatusDuration(s1.id),
                  e1 = s1.elapsed,
                  e2 = s2.elapsed;
            bool isAligned = e1 < e2;
            if (!isAligned) e2 += d1;
            float interpolatedStatusElapsed = Mathf.Lerp(e1, e2, interpolation);
            byte interpolatedStatusId;
            if (e2 > d1)
            {
                interpolatedStatusElapsed -= d1;
                interpolatedStatusId = s2.id;
            }
            else
                interpolatedStatusId = s1.id;
            id.Value = interpolatedStatusId;
            elapsed.Value = interpolatedStatusElapsed;
        }
    }

    [Serializable]
    public class ItemComponent : ComponentBase
    {
        public GunStatusComponent gunStatus;
        public ByteProperty id;
        public ItemByteStatusComponent status;
        public ItemByteStatusComponent equipStatus;

        public override void InterpolateFrom(object c1, object c2, float interpolation)
        {
            var i1 = (ItemComponent) c1;
            var i2 = (ItemComponent) c2;
            status.InterpolateFrom(i1.status, i2.status, interpolation, id);
            equipStatus.InterpolateFrom(i1.equipStatus, i2.equipStatus, interpolation, id);
            gunStatus.aimStatus.InterpolateFrom(i1.gunStatus.aimStatus, i2.gunStatus.aimStatus, interpolation, id);
        }
    }

    [Serializable]
    public class PlayerInventoryComponent : ComponentBase
    {
        public ByteProperty activeIndex, wantedIndex;
        public ArrayProperty<ItemComponent> itemComponents = new ArrayProperty<ItemComponent>(10);

        public ItemComponent ActiveItemComponent => itemComponents[activeIndex - 1];
    }
}