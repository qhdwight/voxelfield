using UnityEngine;

namespace Swihoni.Sessions.Items.Visuals
{
    public class GunVisualBehavior : ItemVisualBehavior
    {
        [SerializeField] protected Transform m_AdsTarget;

        public Transform AdsTarget => m_AdsTarget;
    }
}