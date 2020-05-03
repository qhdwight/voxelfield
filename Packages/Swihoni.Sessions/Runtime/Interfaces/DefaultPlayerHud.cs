using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using UnityEngine.UI;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class DefaultPlayerHud : InterfaceBehaviorBase
    {
        [SerializeField] private BufferedTextGui m_HealthText = default, m_AmmoText = default;
        [SerializeField] private Image m_Crosshair = default, m_HitMarker = default;
        [SerializeField] private Color m_KillHitMarkerColor = Color.red;
        private Color m_DefaultHitMarkerColor;

        // private bool Changed<TElement>(Container container, out TElement component) where TElement : ElementBase
        // {
        //     return container.Has(out component) && (m_Previous == null || !Equals(component, m_Previous.Require<TElement>()));
        // }

        protected override void Awake()
        {
            base.Awake();
            m_DefaultHitMarkerColor = m_HitMarker.color;
        }

        public void Render(Container localPlayer)
        {
            bool isVisible = localPlayer.Without(out HealthProperty health) || health.HasValue && health.IsAlive;
            if (isVisible)
            {
                if (localPlayer.Has<HealthProperty>())
                    m_HealthText.Set(builder => builder.Append("Health: ").Append(health.Value));
                if (localPlayer.Has(out InventoryComponent inventory) && inventory.HasItemEquipped)
                {
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
                    Color crosshairColor = m_Crosshair.color;
                    crosshairColor.a = inventory.adsStatus.id == AdsStatusId.Ads ? 0.0f : 1.0f;
                    m_Crosshair.color = crosshairColor;
                }
                if (localPlayer.Has(out HitMarkerComponent hitMarker))
                {
                    bool isHitMarkerVisible = hitMarker.elapsed > 0.0f;
                    Color color = hitMarker.isKill ? m_KillHitMarkerColor : m_DefaultHitMarkerColor;
                    color.a = isHitMarkerVisible ? 1.0f : 0.0f;
                    m_HitMarker.color = color;
                    float scale = Mathf.Lerp(0.0f, 1.0f, hitMarker.elapsed);
                    m_HitMarker.rectTransform.localScale = new Vector2(scale, scale);
                }
            }

            SetInterfaceActive(isVisible);
        }
    }
}