using Swihoni.Components;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class PlayerHudBase : SingletonInterfaceBehavior<PlayerHudBase>
    {
        [SerializeField] private BufferedTextGui m_HealthText = default, m_AmmoText = default;

        // private bool Changed<TElement>(Container container, out TElement component) where TElement : ElementBase
        // {
        //     return container.Has(out component) && (m_Previous == null || !Equals(component, m_Previous.Require<TElement>()));
        // }

        public virtual void Render(Container localPlayer)
        {
            bool isVisible = localPlayer.Without(out HealthProperty health) || health.HasValue && health.IsAlive;

            if (isVisible)
            {
                if (localPlayer.Has<HealthProperty>())
                    m_HealthText.Set(builder => builder.Append("Health: ").Append(health.Value));
                if (localPlayer.Has(out InventoryComponent inventory) && inventory.HasItemEquipped)
                    m_AmmoText.Set(builder =>
                    {
                        ItemComponent equippedItem = inventory.EquippedItemComponent;
                        ItemModifierBase modifier = ItemManager.GetModifier(equippedItem.id);
                        if (modifier is GunModifierBase gunModifier)
                            builder
                               .Append("Ammo ")
                               .Append(equippedItem.gunStatus.ammoInMag)
                               .Append("/")
                               .Append(gunModifier.MagSize)
                               .Append(" x")
                               .Append(equippedItem.gunStatus.ammoInReserve);
                    });
            }
            
            SetInterfaceActive(isVisible);
        }
    }
}