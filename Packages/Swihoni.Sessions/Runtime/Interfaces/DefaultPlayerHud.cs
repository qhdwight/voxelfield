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
    public class DefaultPlayerHud : SessionInterfaceBehavior
    {
        [SerializeField] private BufferedTextGui m_HealthText = default, m_AmmoText = default;
        [SerializeField] private Image m_Crosshair = default, m_HitMarker = default;
        [SerializeField] private Image[] m_DamageNotifiers = default;
        [SerializeField] private Color m_KillHitMarkerColor = Color.red;
        [SerializeField] private BufferedTextGui m_InventoryText = default;
        private Sprite m_DefaultCrosshair;
        private Color m_DefaultHitMarkerColor;

        // private Pool<TextMeshPro> m_DamageTextPool;

        // private bool Changed<TElement>(Container container, out TElement component) where TElement : ElementBase
        // {
        //     return container.Has(out component) && (m_Previous == null || !Equals(component, m_Previous.Require<TElement>()));
        // }

        protected override void Awake()
        {
            base.Awake();
            // m_DamageTextPool = new Pool<TextMeshPro>(0, () => Instantiate(m_DamagePrefab), (text, isActive) => text.enabled = isActive);
            m_DefaultHitMarkerColor = m_HitMarker.color;
            m_DefaultCrosshair = m_Crosshair.sprite;
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool IsVisible(out Container sessionLocalPlayer, out HealthProperty localHealth)
            {
                sessionLocalPlayer = default;
                localHealth = default;
                var localPlayerId = sessionContainer.Require<LocalPlayerId>();
                if (localPlayerId.WithoutValue) return false;
                sessionLocalPlayer = session.GetPlayerFromId(localPlayerId);
                return sessionLocalPlayer.Without(out localHealth) || localHealth.WithValue && localHealth.IsAlive;
            }
            bool isActive = IsVisible(out Container localPlayer, out HealthProperty health);
            if (isActive)
            {
                if (localPlayer.With<HealthProperty>())
                    m_HealthText.BuildText(builder => builder.Append("Health: ").Append(health.Value));
                if (localPlayer.With(out InventoryComponent inventory) && inventory.WithItemEquipped(out ItemComponent equippedItem))
                {
                    ItemModifierBase modifier = ItemAssetLink.GetModifier(equippedItem.id);
                    m_AmmoText.BuildText(builder =>
                    {
                        switch (modifier)
                        {
                            case GunModifierBase gunModifier:
                                builder
                                   .Append("Ammo ")
                                   .Append(equippedItem.ammoInMag)
                                   .Append("/")
                                   .Append(gunModifier.MagSize)
                                   .Append(" x")
                                   .Append(equippedItem.ammoInReserve);
                                break;
                            case ThrowableModifierBase _:
                                builder.Append("x").Append(equippedItem.ammoInReserve);
                                break;
                            default:
                                builder.Append("∞");
                                break;
                        }
                    });

                    // Color crosshairColor = m_Crosshair.color;
                    // crosshairColor.a = inventory.adsStatus.id.Else(AdsStatusId.HipAiming) == AdsStatusId.Ads ? 0.0f : 1.0f;
                    // m_Crosshair.color = crosshairColor;
                    ItemVisualBehavior visualPrefab = ItemAssetLink.GetVisualPrefab(equippedItem.id);
                    bool isDefaultCrosshair = visualPrefab.Crosshair == null;
                    m_Crosshair.sprite = isDefaultCrosshair ? m_DefaultCrosshair : visualPrefab.Crosshair;
                    m_Crosshair.rectTransform.sizeDelta = Vector2.one * (isDefaultCrosshair ? 32.0f : 48.0f);

                    m_InventoryText.BuildText(builder =>
                    {
                        var realizedIndex = 0;
                        for (var index = 0; index < inventory.items.Length; index++)
                        {
                            ItemComponent item = inventory.items[index];
                            if (item.id == ItemId.None) continue;
                            if (realizedIndex++ != 0) builder.Append("    ");
                            string itemName = ItemAssetLink.GetModifier(item.id).itemName;
                            // TODO:feature show key bind instead
                            builder.Append("[").Append(index + 1).Append("] ").Append(itemName);
                        }
                    });
                }
                else
                {
                    m_AmmoText.SetText(string.Empty);
                    m_HealthText.SetText(string.Empty);
                    m_InventoryText.SetText(string.Empty);
                }
                // TODO:refactor different durations for hit marker and damage notifier
                if (localPlayer.With(out HitMarkerComponent hitMarker))
                {
                    bool isHitMarkerVisible = hitMarker.elapsedUs > 0.0f;
                    Color color = hitMarker.isKill ? m_KillHitMarkerColor : m_DefaultHitMarkerColor;
                    color.a = isHitMarkerVisible ? 1.0f : 0.0f;
                    m_HitMarker.color = color;
                    float scale = Mathf.Lerp(0.0f, 1.0f, hitMarker.elapsedUs / 1_000_000f);
                    m_HitMarker.rectTransform.localScale = new Vector2(scale, scale);
                }
                if (localPlayer.With(out DamageNotifierComponent damageNotifier))
                {
                    foreach (Image notifierImage in m_DamageNotifiers)
                    {
                        Color color = notifierImage.color;
                        color.a = Mathf.Lerp(0.0f, 1.0f, damageNotifier.elapsedUs / 2_000_000f);
                        notifierImage.color = color;
                    }
                }
            }

            SetInterfaceActive(isActive);
        }
    }
}