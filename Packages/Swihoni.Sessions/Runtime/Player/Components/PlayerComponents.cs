using System;
using Steamworks;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using UnityEngine;

namespace Swihoni.Sessions.Player.Components
{
    [Serializable, ClientTrusted]
    public class CameraComponent : ComponentBase
    {
        [Angle] public FloatProperty yaw;
        public FloatProperty pitch;

        public Vector3 GetForward()
        {
            float y = yaw * Mathf.Deg2Rad, p = pitch * Mathf.Deg2Rad;
            var forward = new Vector3(Mathf.Cos(p) * Mathf.Sin(y), -Mathf.Sin(p), Mathf.Cos(p) * Mathf.Cos(y));
            forward.Normalize();
            return forward;
        }

        public override string ToString() => $"Yaw: {yaw}, Pitch: {pitch}";
    }

    [Serializable]
    public class MoveType : ByteProperty
    {
        public const byte Grounded = 0, Flying = 1;
    }

    [Serializable, ModeElement, OnlyServerTrusted]
    public class FrozenProperty : BoolProperty
    {
    }

    [Serializable, ModeElement, OnlyServerTrusted]
    public class SuffocatingProperty : BoolProperty
    {
    }

    [Serializable, ModeElement, ClientChecked]
    public class MoveComponent : ComponentBase
    {
        public MoveType type;
        [PredictionTolerance(0.02f), InterpolateRange(2.0f), NeverCompress]
        public VectorProperty position, velocity;
        [NeverCompress] public ByteProperty groundTick;
        [PredictionTolerance(0.02f), NeverCompress] public FloatProperty normalizedCrouch;
        [Cyclic(0.0f, 1.0f)] public FloatProperty normalizedMove;

        public static implicit operator Vector3(MoveComponent move) => move.position;

        public override string ToString() => $"Position: {position}, Velocity: {velocity}";
    }

    [Serializable, OnlyServerTrusted]
    public class HealthProperty : ByteProperty
    {
        public bool IsAlive => !IsDead;
        public bool IsDead => Value == 0;
        public bool IsInactiveOrDead => WithoutValue || IsDead;
        public bool IsActiveAndAlive => WithValue && IsAlive;
    }

    [Serializable, ModeElement, OnlyServerTrusted]
    public class StatsComponent : ComponentBase
    {
        public ByteProperty kills, deaths, damage;
        public UShortProperty ping;
    }

    [Serializable, ModeElement, OnlyServerTrusted]
    public class RespawnTimerProperty : UIntProperty
    {
        public override string ToString() => $"Respawn timer: {base.ToString()}";
    }

    [Serializable, ModeElement, OnlyServerTrusted]
    public class TeamProperty : ByteProperty
    {
        public TeamProperty(byte value) : base(value) { }
        public TeamProperty() { }
    }

    [Serializable, SingleTick]
    public class WantedTeamProperty : ByteProperty
    {
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
                else interpolatedId = s1.id;
                id.Value = interpolatedId;
                elapsedUs.Value = interpolatedElapsedUs;
            }
        }

        public override string ToString() => $"ID: {id}, Elapsed: {elapsedUs}";
    }

    [Serializable, CustomInterpolation]
    public class ItemComponent : ComponentBase
    {
        public ByteProperty id;
        public ByteStatusComponent status;
        public UShortProperty ammoInMag, ammoInReserve;

        private static ItemModifierBase _modifier;

        public bool NoAmmoLeft => ammoInMag == 0 && ammoInReserve == 0;

        // Embedded item components are only explicitly interpolated, since usually it only needs to be done on equipped item
        public void InterpolateFrom(ItemComponent i1, ItemComponent i2, float interpolation)
        {
            if (i1.id.WithoutValue || i2.id.WithoutValue)
            {
                this.SetTo(i2);
                return;
            }
            _modifier = ItemAssetLink.GetModifier(i1.id);
            ammoInMag.SetTo(i2.ammoInMag);
            ammoInReserve.SetTo(i2.ammoInReserve);
            id.Value = i2.id.Value;
            status.InterpolateFrom(i1.status, i2.status, interpolation,
                                   statusId => InventoryComponent.VisualDuration(_modifier.GetStatusModifierProperties(statusId)));
        }
    }

    [Serializable, ModeElement, ClientChecked, CustomInterpolation]
    public class InventoryComponent : ComponentBase
    {
        public const int ItemsCount = 10;

        public ByteProperty equippedIndex, previousEquippedIndex;
        public ByteStatusComponent equipStatus, adsStatus;
        public ArrayElement<ItemComponent> items = new ArrayElement<ItemComponent>(ItemsCount);

        [ClientNonChecked] public TimeUsProperty tracerTimeUs;
        [ClientNonChecked] public VectorProperty tracerStart, tracerEnd;

        public ItemComponent EquippedItemComponent => this[equippedIndex];
        public bool HasItemEquipped => equippedIndex.WithValue;
        public bool HasNoItemEquipped => !HasItemEquipped;

        public bool WithItemEquipped(out ItemComponent equippedItem)
        {
            if (HasItemEquipped)
            {
                equippedItem = EquippedItemComponent;
                return true;
            }
            equippedItem = default;
            return false;
        }

        private static ItemModifierBase _m1;

        public override void CustomInterpolateFrom(ComponentBase c1, ComponentBase c2, float interpolation)
        {
            InventoryComponent i1 = (InventoryComponent) c1, i2 = (InventoryComponent) c2;
            if (i1.HasNoItemEquipped || i2.HasNoItemEquipped || i1.equippedIndex != i2.equippedIndex)
            {
                this.SetTo(i1);
                return;
            }
            // TODO:feature handle when id of equipped weapon changes
            ItemModifierBase m1 = ItemAssetLink.GetModifier(i1.EquippedItemComponent.id);
            _m1 = m1; // Prevent closure allocation
            if (m1 is GunModifierBase)
            {
                adsStatus.InterpolateFrom(i1.adsStatus, i2.adsStatus, interpolation,
                                          aimStatusId => VisualDuration(((GunModifierBase) _m1).GetAdsStatusModifierProperties(aimStatusId)));
            }
            equipStatus.InterpolateFrom(i1.equipStatus, i2.equipStatus, interpolation,
                                        equipStatusId => VisualDuration(_m1.GetEquipStatusModifierProperties(equipStatusId)));
            equippedIndex.Value = i1.equippedIndex;
            for (var i = 0; i < i1.items.Length; i++)
                items[i].InterpolateFrom(i1.items[i], i2.items[i], interpolation);
        }

        public new ItemComponent this[int index] => items[index];

        public static uint VisualDuration(ItemStatusModiferProperties m) => m.isPersistent ? uint.MaxValue : m.durationUs;
    }

    [Serializable, TakeSecondForInterpolation]
    public class SteamIdProperty : ULongProperty
    {
        public Friend AsFriend => new Friend(Value);
    }
}