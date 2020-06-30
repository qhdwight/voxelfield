using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    [CreateAssetMenu(fileName = "Shotgun", menuName = "Item/Shotgun", order = 2)]
    public class ShotgunModifier : GunWithMagazineModifier
    {
        protected override void ReloadAmmo(ItemComponent item)
        {
            item.ammoInMag.Value++;
            item.ammoInReserve.Value--;
        }

        protected override byte? FinishStatus(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            if (item.status.id == GunStatusId.Reloading)
            {
                ReloadAmmo(item);
                return CanReload(item, inventory) ? GunStatusId.Reloading : (byte?) null;
            }
            return base.FinishStatus(session, playerId, item, inventory, inputs);
        }
    }
}