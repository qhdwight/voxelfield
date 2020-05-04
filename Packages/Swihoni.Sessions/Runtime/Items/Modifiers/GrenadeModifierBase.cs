using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    [CreateAssetMenu(fileName = "Grenade", menuName = "Item/Grenade", order = 1)]
    public class GrenadeModifierBase : ThrowableModifierBase
    {
        [Header("Grenade"), SerializeField] private float m_SplashDamage = default;
    }
}