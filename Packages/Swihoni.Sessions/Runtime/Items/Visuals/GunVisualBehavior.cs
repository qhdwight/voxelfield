using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Visuals
{
    public class GunVisualBehavior : ItemVisualBehavior
    {
        [SerializeField] protected Transform m_AdsTarget;
        [SerializeField] private bool m_IsMagazineShell = default;

        private Renderer m_MagazineRenderer;

        public Transform AdsTarget => m_AdsTarget;

        private void Awake() => m_MagazineRenderer = transform.Find("Item/Magazine").GetComponent<Renderer>();

        protected override bool IsMeshVisible(Renderer meshRenderer, bool isItemVisible, ItemComponent item, ByteStatusComponent equipStatus)
        {
            if (m_IsMagazineShell && isItemVisible && meshRenderer == m_MagazineRenderer && item.ammoInMag == 0 && item.status.id != GunStatusId.Reloading)
                return false;
            return base.IsMeshVisible(meshRenderer, isItemVisible, item, equipStatus);
        }
    }
}