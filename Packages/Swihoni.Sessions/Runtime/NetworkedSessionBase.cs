using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Collections;
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
            IReadOnlyList<Type> serverPlayerElements = playerElements.Append(typeof(ServerStampComponent)).Append(typeof(ClientStampComponent))
                                                                     .Append(typeof(SimulatedTimeProperty)).ToArray(),
                                clientCommandElements = playerElements.Concat(commandElements).Append(typeof(ClientStampComponent)).ToArray();
            ServerSessionContainer ServerSessionContainerConstructor()
            {
                var session = new ServerSessionContainer(sessionElements.Append(typeof(ServerStampComponent)));
                if (session.If(out PlayerContainerArrayProperty players))
                    players.SetAll(() => new Container(serverPlayerElements));
                return session;
            }
            m_SessionHistory = new CyclicArray<ServerSessionContainer>(250, ServerSessionContainerConstructor);

            m_EmptyClientCommands = new ClientCommandsContainer(clientCommandElements);
            m_EmptyServerSession = ServerSessionContainerConstructor();
        }
        
        protected bool GetLocalPlayerId(out int localPlayerId)
        {
            if (m_SessionHistory.Peek().If(out LocalPlayerProperty localPlayerProperty) && localPlayerProperty.HasValue)
            {
                localPlayerId = localPlayerProperty;
                return true;
            }
            localPlayerId = default;
            return false;
        }
    }
}