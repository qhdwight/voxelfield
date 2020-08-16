using System.Text;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
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
        [SerializeField] private Image m_Crosshair = default, m_HitMarker = default, m_FlashOverlayImage = default;
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

        public override void Render(in SessionContext context)
        {
            Container sessionContainer = context.sessionContainer;
            bool IsVisible(out Container sessionLocalPlayer, out HealthProperty localHealth)
            {
                sessionLocalPlayer = default;
                localHealth = default;
                var localPlayerId = sessionContainer.Require<LocalPlayerId>();
                if (localPlayerId.WithoutValue) return false;
                sessionLocalPlayer = sessionContainer.GetPlayer(localPlayerId);
                return sessionLocalPlayer.Without(out localHealth) || localHealth.IsActiveAndAlive;
            }
            bool isActive = IsVisible(out Container localPlayer, out HealthProperty health);
            if (isActive)
            {
                bool showHealth = localPlayer.With<HealthProperty>();
                if (showHealth) m_HealthText.StartBuild().Append("Health: ").Append(health.Value).Commit(m_HealthText);
                m_HealthText.enabled = showHealth;
                var showItems = false;
                if (localPlayer.With(out InventoryComponent inventory) && inventory.WithItemEquipped(out ItemComponent equippedItem))
                {
                    showItems = true;
                    ItemModifierBase modifier = ItemAssetLink.GetModifier(equippedItem.id);
                    StringBuilder builder = m_AmmoText.StartBuild();
                    switch (modifier)
                    {
                        case GunModifierBase gunModifier:
                            builder.Append("Ammo ")
                                   .Append(equippedItem.ammoInMag)
                                   .Append("/")
                                   .Append(gunModifier.MagSize)
                                   .Append(" x")
                                   .Append(equippedItem.ammoInReserve);
                            break;
                        case ThrowableItemModifierBase _:
                            builder.Append("x").Append(equippedItem.ammoInReserve);
                            break;
                        default:
                            builder.Append("∞");
                            break;
                    }
                    m_AmmoText.SetText(builder);

                    ItemVisualBehavior visualPrefab = ItemAssetLink.GetVisualPrefab(equippedItem.id);
                    bool isDefaultCrosshair = visualPrefab.Crosshair == null;
                    m_Crosshair.sprite = isDefaultCrosshair ? m_DefaultCrosshair : visualPrefab.Crosshair;
                    Vector2 size = Vector2.one * (isDefaultCrosshair ? 32.0f : 48.0f);
                    if (isDefaultCrosshair) size *= DefaultConfig.Active.crosshairThickness;
                    m_Crosshair.rectTransform.sizeDelta = size;

                    builder = m_InventoryText.StartBuild();
                    var realizedIndex = 0;
                    for (var index = 0; index < inventory.items.Length; index++)
                    {
                        ItemComponent item = inventory.items[index];
                        if (item.id.WithoutValue) continue;
                        if (realizedIndex++ != 0) builder.Append("  ");
                        string itemName = ItemAssetLink.GetModifier(item.id).itemName;
                        // TODO:feature show key bind instead
                        builder.Append("[").AppendInputKey((byte) (PlayerInput.ItemOne + index)).Append("] ").Append(itemName);
                    }
                    m_InventoryText.SetText(builder);
                }
                m_HealthText.enabled = showItems;
                m_InventoryText.enabled = showItems;
                // TODO:refactor different durations for hit marker and damage notifier
                if (localPlayer.With(out HitMarkerComponent hitMarker))
                {
                    bool isHitMarkerVisible = hitMarker.timeUs.WithValue;
                    if (isHitMarkerVisible)
                    {
                        Color color = hitMarker.isKill ? m_KillHitMarkerColor : m_DefaultHitMarkerColor;
                        m_HitMarker.color = color;
                        float scale = Mathf.Lerp(0.0f, 1.0f, hitMarker.timeUs / 1_000_000f);
                        m_HitMarker.rectTransform.localScale = new Vector2(scale, scale);
                    }
                    m_HitMarker.enabled = isHitMarkerVisible;
                }
                if (localPlayer.With(out DamageNotifierComponent damageNotifier))
                {
                    bool isNotifierVisible = damageNotifier.timeUs.WithValue;
                    foreach (Image notifierImage in m_DamageNotifiers)
                    {
                        if (isNotifierVisible)
                        {
                            Color color = notifierImage.color;
                            color.a = Mathf.Lerp(0.0f, 1.0f, damageNotifier.timeUs / 2_000_000f);
                            notifierImage.color = color;
                        }
                        notifierImage.enabled = isNotifierVisible;
                    }
                }
            }
            Color c = Color.white;
            c.a = isActive && localPlayer.Require<FlashProperty>().TryWithValue(out float flash) ? flash : 0.0f;
            m_FlashOverlayImage.color = c;
            SetInterfaceActive(isActive);
        }

        protected override void OnSetInterfaceActive(bool isActive) => m_FlashOverlayImage.enabled = isActive;
    }
}