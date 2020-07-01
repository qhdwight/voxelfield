using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using UnityEngine;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Warmup", menuName = "Session/Mode/Warmup", order = 0)]
    public class WarmupMode : DeathmatchMode
    {
        protected override float CalculateWeaponDamage(SessionBase session, Container hitPlayer, Container inflictingPlayer, PlayerHitbox hitbox, WeaponModifierBase weapon,
                                                       in RaycastHit hit)
        {
            float baseDamage = base.CalculateWeaponDamage(session, hitPlayer, inflictingPlayer, hitbox, weapon, in hit);
            return ShowdownMode.CalculateDamageWithMovement(session, inflictingPlayer, weapon, baseDamage);
        }
    }
}