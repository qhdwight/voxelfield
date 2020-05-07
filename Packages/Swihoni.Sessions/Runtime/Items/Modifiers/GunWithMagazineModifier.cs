using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    [CreateAssetMenu(fileName = "Gun", menuName = "Item/Gun", order = 1)]
    public class GunWithMagazineModifier : GunModifierBase
    {
        protected override void ReloadAmmo(ItemComponent item)
        {
            GunStatusComponent gunStatus = item.gunStatus;
            var addAmount = (ushort) (m_MagSize - gunStatus.ammoInMag);
            if (addAmount > gunStatus.ammoInReserve)
                addAmount = gunStatus.ammoInReserve;
            gunStatus.ammoInMag.Value += addAmount;
            gunStatus.ammoInReserve.Value -= addAmount;
        }
    }
}