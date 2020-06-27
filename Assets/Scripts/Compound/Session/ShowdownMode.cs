using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util.Math;
using UnityEngine;

namespace Compound.Session
{
    [CreateAssetMenu(fileName = "Showdown", menuName = "Session/Mode/Showdown", order = 0)]
    public class ShowdownMode : DeathmatchMode
    {
        protected override float CalculateWeaponDamage(SessionBase session, Container hitPlayer, Container inflictingPlayer, PlayerHitbox hitbox, WeaponModifierBase weapon, in RaycastHit hit)
        {
            float baseDamage = base.CalculateWeaponDamage(session, hitPlayer, inflictingPlayer, hitbox, weapon, hit);
            if (weapon is MeleeModifier) return baseDamage;
            Vector3 velocity = inflictingPlayer.Require<MoveComponent>().velocity;
            var modifierPrefab = (PlayerModifierDispatcherBehavior) session.PlayerManager.GetModifierPrefab(inflictingPlayer.Require<IdProperty>());
            float ratio = 1.0f - Mathf.Clamp01(velocity.LateralMagnitude() / modifierPrefab.Movement.MaxSpeed);
            const float minimumRatio = 0.3f;
            ratio = minimumRatio + ratio * (1.0f - minimumRatio);
            return baseDamage * ratio;
        }
    }
}