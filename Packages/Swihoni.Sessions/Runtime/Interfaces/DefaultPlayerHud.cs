using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Items.Visuals;
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
        [SerializeField] private Image[] m_DamageNotifiers = default;
        [SerializeField] private Color m_KillHitMarkerColor = Color.red;
        [SerializeField] private BufferedTextGui m_InventoryText = default;
        private Sprite m_DefaultCrosshair;
        private Color m_DefaultHitMarkerColor;

        // private bool Changed<TElement>(Container container, out TElement component) where TElement : ElementBase
        // {
        //     return container.Has(out component) && (m_Previous == null || !Equals(component, m_Previous.Require<TElement>()));
        // }

        protected override void Awake()
        {
            base.Awake();
            m_DefaultHitMarkerColor = m_HitMarker.color;
            m_DefaultCrosshair = m_Crosshair.sprite;
        }

        public void Render(Container localPlayer)
        {
            bool isVisible = localPlayer.Without(out HealthProperty health) || health.WithValue && health.IsAlive;
            if (isVisible)
            {
                if (localPlayer.With<HealthProperty>())
                    m_HealthText.BuildText(builder => builder.Append("Health: ").Append(health.Value));
                if (localPlayer.With(out InventoryComponent inventory) && inventory.HasItemEquipped)
                {
                    ItemComponent equippedItem = inventory.EquippedItemComponent;
                    ItemModifierBase modifier = ItemAssetLink.GetModifier(equippedItem.id);
                    m_AmmoText.BuildText(builder =>
                    {
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
                    ItemVisualBehavior visualPrefab = ItemAssetLink.GetVisuals(equippedItem.id);
                    bool isDefaultCrosshair = visualPrefab.Crosshair == null;
                    m_Crosshair.sprite = isDefaultCrosshair ? m_DefaultCrosshair : visualPrefab.Crosshair;
                    m_Crosshair.rectTransform.sizeDelta = Vector2.one * (isDefaultCrosshair ? 32.0f : 48.0f);

                    m_InventoryText.BuildText(builder =>
                    {
                        for (var index = 0; index < inventory.itemComponents.Length; index++)
                        {
                            if (index != 0) builder.Append("    ");
                            ItemComponent item = inventory.itemComponents[index];
                            if (item.id == ItemId.None) continue;
                            string itemName = ItemAssetLink.GetModifier(item.id).itemName;
                            // TODO:feature show key bind instead
                            builder.Append("[").Append(index + 1).Append("] ").Append(itemName);
                        }
                    });
                }
                if (localPlayer.With(out HitMarkerComponent hitMarker))
                {
                    bool isHitMarkerVisible = hitMarker.elapsed > 0.0f;
                    Color color = hitMarker.isKill ? m_KillHitMarkerColor : m_DefaultHitMarkerColor;
                    color.a = isHitMarkerVisible ? 1.0f : 0.0f;
                    m_HitMarker.color = color;
                    float scale = Mathf.Lerp(0.0f, 1.0f, hitMarker.elapsed);
                    m_HitMarker.rectTransform.localScale = new Vector2(scale, scale);
                }
                if (localPlayer.With(out DamageNotifierComponent damageNotifier))
                {
                    foreach (Image notifierImage in m_DamageNotifiers)
                    {
                        Color color = notifierImage.color;
                        color.a = Mathf.Lerp(0.0f, 1.0f, damageNotifier.elapsed);
                        notifierImage.color = color;
                    }
                }
            }

            SetInterfaceActive(isVisible);
        }
    }
}