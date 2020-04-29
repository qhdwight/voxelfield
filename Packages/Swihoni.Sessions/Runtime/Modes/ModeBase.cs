using System;
using Swihoni.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    public abstract class ModeBase : ScriptableObject
    {
        public byte id;

        internal abstract void ResetPlayer(Container player);

        internal virtual void KillPlayer(Container player)
        {
            if (player.Has(out HealthProperty health)) health.Value = 0;
        }

        internal abstract void Modify(Container playerToModify, Container commands, float duration);

        public void ModifyChecked(ModeBase mode, Container containerToModify, Container commands, float duration) { }

        public void ModifyTrusted(ModeBase mode, Container containerToModify, Container commands, float duration) { }

        public void ModifyCommands(ModeBase mode, Container commandsToModify) { throw new NotImplementedException(); }

        public virtual void PlayerHit(Container hitPlayer, Container inflictingPlayer, PlayerHitbox hitbox, GunModifierBase gun, float distance)
        {
            if (hitPlayer.Present(out HealthProperty health) && health.IsAlive)
            {
                checked
                {
                    if (gun.Damage >= health)
                    {
                       KillPlayer(hitPlayer);
                       if (inflictingPlayer.Has(out StatsComponent stats))
                           stats.kills.Value++;
                    }
                    else
                    {
                        health.Value -= gun.Damage;
                    }
                }
            }
        }
    }
}