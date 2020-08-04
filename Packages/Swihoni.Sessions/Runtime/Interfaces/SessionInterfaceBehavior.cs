using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class SessionInterfaceBehavior : InterfaceBehaviorBase
    {
        [SerializeField] private bool m_IsDuringGame = true;

        public bool IsDuringGame => m_IsDuringGame;

        protected override void Awake()
        {
            base.Awake();
            SetInterfaceActive(false);
        }

        public abstract void Render(SessionBase session, Container sessionContainer);

        public virtual void OnMostRecent(SessionBase session, Container sessionContainer) { }

        public virtual void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands) { }

        public virtual void SessionStateChange(bool isActive)
        {
            if (m_IsDuringGame && !isActive) SetInterfaceActive(false);
        }

        protected static bool HasItemEquipped(SessionBase session, Container sessionContainer, byte modeId, byte itemId, byte? itemId2 = null)
            => sessionContainer.Require<ModeIdProperty>() == modeId
            && session.IsValidLocalPlayer(sessionContainer, out Container localPlayer)
            && localPlayer.Require<InventoryComponent>().WithItemEquipped(out ItemComponent item)
            && (item.id == itemId || item.id == itemId2);

        protected bool NoInterrupting => !SessionBase.InterruptingInterface || SessionBase.InterruptingInterface == this;
    }
}