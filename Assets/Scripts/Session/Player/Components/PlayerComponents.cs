using System;
using Components;
using Session.Items;
using UnityEngine;

// ReSharper disable UnassignedField.Global

namespace Session.Player.Components
{
    [Serializable]
    public class PlayerComponent : ComponentBase
    {
        [NoInterpolate] public Property<byte> health;
        public PlayerInventoryComponent inventory;
        public Property<Vector3> position;
        public Property<float> yaw, pitch;

        public bool IsAlive => !IsDead;
        public bool IsDead => health == 0;
    }

    [Serializable]
    public class ItemStatusComponent : ComponentBase
    {
        public Property<float> elapsedTime;
        public Property<ItemStatusId> id;
    }

    [Serializable]
    public class GunStatusComponent : ComponentBase
    {
        public Property<byte> aimStatus;
        public Property<float> aimStatusElapsedTime;
        public Property<ushort> ammoInMag, ammoInReserve;
    }

    [Serializable]
    public class ItemComponent : ComponentBase
    {
        public GunStatusComponent gunStatus;
        public Property<ItemId> id;
        public ItemStatusComponent status;
    }

    [Serializable]
    public class PlayerInventoryComponent : ComponentBase
    {
        public Property<byte> activeIndex, wantedIndex;
        public ArrayProperty<ItemComponent> itemComponents = new ArrayProperty<ItemComponent>(10);

        public ItemComponent ActiveItemComponent => itemComponents[activeIndex - 1];
    }
}