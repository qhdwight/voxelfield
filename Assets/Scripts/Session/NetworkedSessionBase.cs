using System;
using System.Collections.Generic;
using System.Linq;
using Collections;
using Components;
using Session.Components;

namespace Session
{
    public abstract class NetworkedSessionBase : SessionBase
    {
        protected readonly CyclicArray<ServerSessionContainer> m_SessionComponentHistory;
        protected readonly ClientCommandsContainer m_ClientCommandsContainer;
        protected readonly ServerSessionContainer m_ServerSessionContainer;

        protected NetworkedSessionBase(IGameObjectLinker linker,
                                       IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            IReadOnlyList<Type> serverElements = playerElements.Append(typeof(ServerStampComponent)).Append(typeof(ClientStampComponent)).ToArray(),
                                clientElements = playerElements.Concat(commandElements).Append(typeof(StampComponent)).ToArray();
            ServerSessionContainer ServerSessionContainerConstructor()
            {
                var sessionContainer = new ServerSessionContainer(sessionElements.Append(typeof(ServerStampComponent)));
                if (sessionContainer.If(out PlayerContainerArrayProperty playersProperty))
                    playersProperty.SetAll(() => new Container(serverElements));
                return sessionContainer;
            }
            m_SessionComponentHistory = new CyclicArray<ServerSessionContainer>(250, ServerSessionContainerConstructor);

            m_ClientCommandsContainer = new ClientCommandsContainer(clientElements);
            m_ServerSessionContainer = ServerSessionContainerConstructor();
        }
    }
}