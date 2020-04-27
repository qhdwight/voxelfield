using System;
using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    public abstract class ModeBase : ScriptableObject, IModifier<Container, Container>
    {
        public byte id;

        internal abstract void ResetPlayer(Container player);

        internal abstract void KillPlayer(Container player);

        internal abstract void Modify(Container playerToModify, Container commands, float duration);

        public void ModifyChecked(Container containerToModify, Container commands, float duration)
        {
            
        }

        public void ModifyTrusted(Container containerToModify, Container commands, float duration)
        {
            
        }

        public void ModifyCommands(Container commandsToModify) { throw new NotImplementedException(); }
    }
}