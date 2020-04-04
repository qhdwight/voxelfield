using System;
using Components;
using Session.Items;

// ReSharper disable UnassignedField.Global

namespace Session.Player.Components
{
    [Serializable]
    public class PlayerComponent : ComponentBase
    {
        [NoInterpolate] public ByteProperty health;
        public PlayerInventoryComponent inventory;
        public VectorProperty position;
        public FloatProperty yaw, pitch;

        public bool IsAlive => !IsDead;
        public bool IsDead => health == 0;
    }

    [Serializable]
    public class ItemStatusComponent : ComponentBase
    {
        public FloatProperty elapsedTime;
        public ByteProperty id;
    }

    [Serializable]
    public class GunStatusComponent : ComponentBase
    {
        public ByteProperty aimStatus;
        public FloatProperty aimStatusElapsedTime;
        public UShortProperty ammoInMag, ammoInReserve;
    }

    [Serializable]
    public class ItemComponent : ComponentBase
    {
        public GunStatusComponent gunStatus;
        public ByteProperty id;
        public ItemStatusComponent status;
    }

    [Serializable]
    public class PlayerInventoryComponent : ComponentBase
    {
        public ByteProperty activeIndex, wantedIndex;
        public ArrayProperty<ItemComponent> itemComponents = new ArrayProperty<ItemComponent>(10);

        public ItemComponent ActiveItemComponent => itemComponents[activeIndex - 1];
    }
}