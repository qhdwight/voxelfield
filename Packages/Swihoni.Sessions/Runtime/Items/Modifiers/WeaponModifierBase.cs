using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    public class WeaponModifierBase : ItemModifierBase
    {
        [SerializeField] protected byte m_Damage = default;
        [SerializeField] protected LayerMask m_RaycastMask = default;
        [SerializeField] private AnimationCurve m_DropOff = AnimationCurve.Constant(0.0f, 1000.0f, 1.0f);
        [SerializeField] private float m_HeadShotMultiplier = 1.0f;

        public float HeadShotMultiplier => m_HeadShotMultiplier;

        public virtual float GetDamage(float distance) => m_Damage * Mathf.Clamp01(m_DropOff.Evaluate(distance));
    }
}