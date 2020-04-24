using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Components;

namespace Swihoni.Sessions
{
    public abstract class NetworkedSessionBase : SessionBase
    {
        protected readonly CyclicArray<ServerSessionContainer> m_SessionHistory;
        protected readonly ClientCommandsContainer m_EmptyClientCommands;
        protected readonly ServerSessionContainer m_EmptyServerSession;

        protected NetworkedSessionBase(IGameObjectLinker linker,
                                       IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            IReadOnlyCollection<Type> serverPlayerElements = playerElements.Append(typeof(ServerStampComponent)).Append(typeof(ClientStampComponent)).ToArray(),
                                      clientCommandElements = playerElements.Concat(commandElements).Append(typeof(ClientStampComponent)).ToArray();
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

        public virtual void Disconnect() { Dispose(); }
    }

    internal static class NetworkSessionExtensions
    {
        internal static Container GetPlayer(this Container session, int index) { return session.Require<PlayerContainerArrayProperty>()[index]; }
    }
}