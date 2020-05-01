using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Modes;
using Swihoni.Util.Interface;

namespace Swihoni.Sessions
{
    public abstract class NetworkedSessionBase : SessionBase
    {
        protected readonly CyclicArray<ServerSessionContainer> m_SessionHistory;
        protected readonly ClientCommandsContainer m_EmptyClientCommands;
        protected readonly ServerSessionContainer m_EmptyServerSession;

        protected NetworkedSessionBase(ISessionGameObjectLinker linker,
                                       IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker)
        {
            IReadOnlyCollection<Type> serverPlayerElements = playerElements.Append(typeof(ClientStampComponent))
                                                                           .Append(typeof(ServerStampComponent)).ToArray(),
                                      clientCommandElements = playerElements.Append(typeof(ClientStampComponent))
                                                                            .Concat(commandElements).ToArray();
            ServerSessionContainer ServerSessionContainerConstructor()
            {
                var session = new ServerSessionContainer(sessionElements.Append(typeof(ServerStampComponent)));
                if (session.Has(out PlayerContainerArrayProperty players))
                    players.SetAll(() => new Container(serverPlayerElements));
                return session;
            }
            m_SessionHistory = new CyclicArray<ServerSessionContainer>(250, ServerSessionContainerConstructor);

            m_EmptyClientCommands = new ClientCommandsContainer(clientCommandElements);
            m_EmptyServerSession = ServerSessionContainerConstructor();
        }

        protected void ForEachPlayer(Action<Container> action)
        {
            foreach (ServerSessionContainer serverSession in m_SessionHistory)
            foreach (Container player in serverSession.Require<PlayerContainerArrayProperty>())
                action(player);
        }

        protected SessionSettingsComponent GetSettings(Container session = null) =>
            session == null ? m_SessionHistory.Peek().Require<SessionSettingsComponent>() : session.Require<SessionSettingsComponent>();

        /// <param name="session">If null, return settings from most recent history. Else get from specified session.</param>
        public override ModeBase GetMode(Container session = null) => ModeManager.GetMode(GetSettings(session).modeId);

        public override Container GetPlayerFromId(int playerId) => m_SessionHistory.Peek().GetPlayer(playerId);

        protected override void Render(float renderTime)
        {
            foreach (InterfaceBehaviorBase @interface in m_Interfaces)
            {
                if (@interface is SessionInterfaceBehavior sessionInterface)
                    sessionInterface.Render(m_SessionHistory.Peek());
            }
        }

        public virtual void Disconnect()
        {
            if (!IsDisposed) Dispose();
        }
    }

    internal static class NetworkSessionExtensions
    {
        internal static Container GetPlayer(this Container session, int index) => session.Require<PlayerContainerArrayProperty>()[index];
    }
}