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
        [NoInterpolation] public ByteProperty health;
        public PlayerInventoryComponent inventory;
        public VectorProperty position;
        public FloatProperty yaw, pitch;

        public bool IsAlive => !IsDead;
        public bool IsDead => health == 0;
    }

    [Serializable]
    public class GunStatusComponent : ComponentBase
    {
        public ByteProperty aimStatus;
        public FloatProperty aimStatusElapsed;
        public UShortProperty ammoInMag, ammoInReserve;
    }

    [Serializable]
    public class ItemComponent : ComponentBase
    {
        public GunStatusComponent gunStatus;
        public ByteProperty id;
        [CustomInterpolation] public FloatProperty statusElapsed;
        [CustomInterpolation] public ByteProperty statusId;

        public override void InterpolateFrom(object c1, object c2, float interpolation)
        {
            if (!(c1 is ItemComponent i1) || !(c2 is ItemComponent i2)) return;
            byte s1 = i1.statusId.Value,
                 itemId1 = i1.id.Value;
            if (itemId1 == ItemId.None) return;
            ItemStatusModiferProperties sp1 = ItemManager.Singleton.GetModifier(itemId1).GetStatusModifierProperties(s1);
            float d1 = sp1.duration,
                  e1 = i1.statusElapsed,
                  e2 = i2.statusElapsed;
            bool isAligned = e1 < e2;
            if (!isAligned) e2 += d1;
            float interpolatedStatusElapsed = Mathf.Lerp(e1, e2, interpolation);
            byte interpolatedStatusId;
            if (e2 > d1)
            {
                interpolatedStatusElapsed -= d1;
                interpolatedStatusId = i2.statusId;
            }
            else
                interpolatedStatusId = i1.statusId;
            statusId.Value = interpolatedStatusId;
            statusElapsed.Value = interpolatedStatusElapsed;
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