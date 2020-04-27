using System;
using Swihoni.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    public abstract class ModeBase : ScriptableObject
    {
        public byte id;

        internal abstract void ResetPlayer(Container player);

        internal abstract void KillPlayer(Container player);

        internal abstract void Modify(Container playerToModify, Container commands, float duration);

        public void ModifyChecked(ModeBase mode, Container containerToModify, Container commands, float duration) { }

        public void ModifyTrusted(ModeBase mode, Container containerToModify, Container commands, float duration) { }

        public void ModifyCommands(ModeBase mode, Container commandsToModify) { throw new NotImplementedException(); }

        public virtual void PlayerHit(int hitPlayerId, int inflictingPlayerId, PlayerHitbox receivingPlayerHitbox, GunModifierBase gun, float distance)
        {
            Debug.Log($"Player: {inflictingPlayerId} hit player: {hitPlayerId}");
        }
    }
}