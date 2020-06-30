using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    [CreateAssetMenu(fileName = "Gun", menuName = "Item/Gun", order = 1)]
    public class GunWithMagazineModifier : GunModifierBase
    {
        protected override void ReloadAmmo(ItemComponent item)
        {
            var addAmount = (ushort) (m_MagSize - item.ammoInMag);
            if (addAmount > item.ammoInReserve)
                addAmount = item.ammoInReserve;
            item.ammoInMag.Value += addAmount;
            item.ammoInReserve.Value -= addAmount;
        }
    }
}