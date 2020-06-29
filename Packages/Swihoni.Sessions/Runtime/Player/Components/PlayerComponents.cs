using System;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Modifiers;

namespace Swihoni.Sessions.Player.Components
{
    [Serializable, ClientTrusted]
    public class CameraComponent : ComponentBase
    {
        [Angle] public FloatProperty yaw;
        public FloatProperty pitch;

        public override string ToString() => $"Yaw: {yaw}, Pitch: {pitch}";
    }

    [Serializable]
    public class MoveType : ByteProperty
    {
        public const byte Grounded = 0, Flying = 1;
    }

    [Serializable, OnlyServerTrusted]
    public class FrozenProperty : BoolProperty
    {
    }

    [Serializable, ClientChecked]
    public class MoveComponent : ComponentBase
    {
        public MoveType type;
        [PredictionTolerance(0.01f), InterpolateRange(2.0f)]
        public VectorProperty position, velocity;
        public ByteProperty groundTick;
        [PredictionTolerance(0.01f)] public FloatProperty normalizedCrouch;
        [Cyclic(0.0f, 1.0f)] public FloatProperty normalizedMove;

        public override string ToString() => $"Position: {position}, Velocity: {velocity}";
    }

    [Serializable, OnlyServerTrusted]
    public class HealthProperty : ByteProperty
    {
        public bool IsAlive => !IsDead;
        public bool IsDead => Value == 0;
    }

    [Serializable, OnlyServerTrusted]
    public class StatsComponent : ComponentBase
    {
        public ByteProperty kills, deaths, damage;
        public UShortProperty ping;
    }

    [Serializable, OnlyServerTrusted]
    public class RespawnTimerProperty : UIntProperty
    {
        public override string ToString() => $"Respawn timer: {base.ToString()}";
    }

    [Serializable, OnlyServerTrusted]
    public class TeamProperty : ByteProperty
    {
        public TeamProperty(byte value) : base(value) { }
        public TeamProperty() { }
    }

    [Serializable]
    public class WantedTeamProperty : ByteProperty
    {
    }

    [Serializable]
    public class GunStatusComponent : ComponentBase
    {
        public UShortProperty ammoInMag, ammoInReserve;
    }

    [Serializable, CustomInterpolation]
    public class ByteStatusComponent : ComponentBase
    {
        public ByteProperty id;
        public UIntProperty elapsedUs;

        /// <summary>
        /// Note: Interpolation must be explicitly called for this type.
        /// </summary>
        public void InterpolateFrom(ByteStatusComponent s1, ByteStatusComponent s2, float interpolation, Func<byte, uint> getStatusDurationUs)
        {
            uint d1 = getStatusDurationUs(s1.id),
                 e1 = s1.elapsedUs,
                 e2 = s2.elapsedUs;
            if (d1 == uint.MaxValue)
            {
                id.Value = s2.id;
                elapsedUs.Value = UIntProperty.InterpolateUInt(s1.id == s2.id ? e1 : 0u, e2, interpolation);
            }
            else
            {
                if (s1.id != s2.id) e2 += d1;
                else if (e1 > e2) e2 += d1;
                uint interpolatedElapsedUs = UIntProperty.InterpolateUInt(e1, e2, interpolation);
                byte interpolatedId;
                if (interpolatedElapsedUs > d1)
                {
                    interpolatedElapsedUs -= d1;
                    interpolatedId = s2.id;
                }
                else
                    interpolatedId = s1.id;
                id.Value = interpolatedId;
                elapsedUs.Value = interpolatedElapsedUs;
            }
        }

        public override string ToString() => $"ID: {id}, Elapsed: {elapsedUs}";
    }

    [Serializable, CustomInterpolation]
    public class ItemComponent : ComponentBase
    {
        public GunStatusComponent gunStatus;
        public ByteProperty id;
        public ByteStatusComponent status;

        // Embedded item components are only explicitly interpolated, since usually it only needs to be done on equipped item
        public void InterpolateFrom(ItemComponent i1, ItemComponent i2, float interpolation)
        {
            if (i1.id == ItemId.None)
            {
                this.CopyFrom(i1);
                return;
            }
            ItemModifierBase modifier = ItemAssetLink.GetModifier(i1.id);
            Interpolator.InterpolateInto(i1.gunStatus, i2.gunStatus, gunStatus, interpolation);
            id.Value = i2.id.Value;
            status.InterpolateFrom(i1.status, i2.status, interpolation,
                                   statusId => InventoryComponent.VisualDuration(modifier.GetStatusModifierProperties(statusId)));
        }
    }

    [Serializable, ClientChecked, CustomInterpolation]
    public class InventoryComponent : ComponentBase
    {
        public ByteProperty equippedIndex;
        public ByteStatusComponent equipStatus, adsStatus;
        public ArrayElement<ItemComponent> itemComponents = new ArrayElement<ItemComponent>(10);

        public ItemComponent EquippedItemComponent => itemComponents[equippedIndex - 1];
        public bool HasItemEquipped => !HasNoItemEquipped;
        public bool HasNoItemEquipped => equippedIndex == PlayerItemManagerModiferBehavior.NoneIndex;

        public override void InterpolateFrom(ComponentBase c1, ComponentBase c2, float interpolation)
        {
            var i1 = (InventoryComponent) c1;
            var i2 = (InventoryComponent) c2;
            if (i1.HasNoItemEquipped || i2.HasNoItemEquipped || i1.equippedIndex != i2.equippedIndex)
            {
                this.CopyFrom(i1);
                return;
            }
            // TODO:feature handle when id of equipped weapon changes
            ItemModifierBase m1 = ItemAssetLink.GetModifier(i1.EquippedItemComponent.id);
            if (m1 is GunModifierBase gm1)
                adsStatus.InterpolateFrom(i1.adsStatus, i2.adsStatus, interpolation,
                                          aimStatusId => VisualDuration(gm1.GetAdsStatusModifierProperties(aimStatusId)));
            equipStatus.InterpolateFrom(i1.equipStatus, i2.equipStatus, interpolation,
                                        equipStatusId => VisualDuration(m1.GetEquipStatusModifierProperties(equipStatusId)));
            equippedIndex.Value = i1.equippedIndex;
            for (var i = 0; i < i1.itemComponents.Length; i++)
                itemComponents[i].InterpolateFrom(i1.itemComponents[i], i2.itemComponents[i], interpolation);
        }

        public static uint VisualDuration(ItemStatusModiferProperties m) => m.isPersistent ? uint.MaxValue : m.durationUs;
    }
}