using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    public class WeaponModifierBase : ItemModifierBase
    {
        [SerializeField] protected byte m_Damage = default;
        [SerializeField] protected LayerMask m_PlayerMask = default;
        [SerializeField] private AnimationCurve m_DropOff = AnimationCurve.Constant(0.0f, 1000.0f, 1.0f);

        public virtual float GetDamage(float distance) => m_Damage * Mathf.Clamp01(m_DropOff.Evaluate(distance));
    }
}