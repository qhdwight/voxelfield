using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    [CreateAssetMenu(fileName = "Shotgun", menuName = "Item/Shotgun", order = 100)]
    public class ShotgunModifier : GunWithMagazineModifier
    {
        protected override void ReloadAmmo(ItemComponent item)
        {
            item.ammoInMag.Value++;
            item.ammoInReserve.Value--;
        }

        protected override int FireRaycast(Ray ray)
            => Physics.BoxCastNonAlloc(ray.GetPoint(500.0f), new Vector3(0.1f, 0.1f, 500.0f),
                                       ray.direction, RaycastHits, Quaternion.identity, float.PositiveInfinity, m_RaycastMask);

        protected override byte? FinishStatus(in ModifyContext context, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            if (item.status.id == GunStatusId.Reloading)
            {
                ReloadAmmo(item);
                return CanReload(item, inventory) ? GunStatusId.Reloading : (byte?) null;
            }
            return base.FinishStatus(in context, item, inventory, inputs);
        }
    }
}