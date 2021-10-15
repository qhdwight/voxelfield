using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class SessionInterfaceBehavior : InterfaceBehaviorBase
    {
        [SerializeField] private bool m_IsDuringGame = true;

        public bool IsDuringGame => m_IsDuringGame;

        public virtual void Initialize() { }

        public abstract void Render(in SessionContext context);

        public virtual void RenderVerified(in SessionContext context) { }

        public virtual void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands) { }

        public virtual void SessionStateChange(bool isActive)
        {
            if (m_IsDuringGame)
                SetInterfaceActive(false);
            else
                SetInterfaceActive(!isActive);
        }

        protected static bool HasItemEquipped(in SessionContext context, byte modeId, byte itemId, byte? itemId2 = null)
            => context.sessionContainer.Require<ModeIdProperty>() == modeId
            && context.IsValidLocalPlayer(out Container localPlayer, out byte _)
            && localPlayer.Require<InventoryComponent>().WithItemEquipped(out ItemComponent item)
            && (item.id == itemId || item.id == itemId2);

        protected bool NoInterrupting => !SessionBase.InterruptingInterface || SessionBase.InterruptingInterface == this;
    }
}