using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    public class WeaponModifierBase : ItemModifierBase
    {
        [SerializeField] protected byte m_Damage = default;
        [SerializeField] protected LayerMask m_PlayerMask = default;

        public byte Damage => m_Damage;
    }
}