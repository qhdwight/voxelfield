using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    [CreateAssetMenu(fileName = "Launcher", menuName = "Item/Launcher", order = 300)]
    public class LauncherModifier : GunWithMagazineModifier
    {
        [SerializeField] protected ThrowableModifierBehavior m_ThrowablePrefab = default;
        [SerializeField] protected float m_LaunchForce = 10.0f;

        protected override void PrimaryUse(in ModifyContext context, InventoryComponent inventory, ItemComponent item) =>
            Release(context, item);

        protected virtual void Release(in ModifyContext context, ItemComponent item)
        {
            checked
            {
                ThrowableItemModifierBase.Throw(context, itemName, m_ThrowablePrefab, m_LaunchForce);
                item.ammoInMag.Value--;
            }
        }
    }
}