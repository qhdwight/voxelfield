using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;

namespace Swihoni.Sessions
{
    public abstract class NetworkedSessionBase : SessionBase
    {
        protected readonly CyclicArray<ServerSessionContainer> m_SessionHistory;
        protected readonly ClientCommandsContainer m_EmptyClientCommands;
        protected readonly ServerSessionContainer m_EmptyServerSession;
        protected readonly DebugClientView m_EmptyDebugClientView;
        protected readonly Container m_RollbackSession;
        protected readonly Container m_RenderSession;

        protected NetworkedSessionBase(ISessionGameObjectLinker linker,
                                       IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker)
        {
            IReadOnlyCollection<Type> serverPlayerElements = playerElements.Append(typeof(ClientStampComponent))
                                                                           .Append(typeof(ServerStampComponent)).ToArray(),
                                      clientCommandElements = playerElements.Append(typeof(ClientStampComponent))
                                                                            .Concat(commandElements).ToArray();
            ServerSessionContainer ServerSessionContainerConstructor() =>
                NewSession<ServerSessionContainer>(sessionElements.Append(typeof(ServerStampComponent)), serverPlayerElements);
            m_SessionHistory = new CyclicArray<ServerSessionContainer>(250, ServerSessionContainerConstructor);

            m_RollbackSession = NewSession<Container>(sessionElements, playerElements);

            m_RenderSession = NewSession<Container>(sessionElements, playerElements);

            m_EmptyClientCommands = new ClientCommandsContainer(clientCommandElements);
            m_EmptyServerSession = ServerSessionContainerConstructor();
            m_EmptyDebugClientView = new DebugClientView(serverPlayerElements);
        }

        protected void ForEachPlayer(Action<Container> action)
        {
            foreach (ServerSessionContainer serverSession in m_SessionHistory)
            foreach (Container player in serverSession.Require<PlayerContainerArrayProperty>())
                action(player);
        }

        protected void RegisterMessages(ComponentSocketBase socket)
        {
            socket.RegisterMessage(typeof(ClientCommandsContainer), m_EmptyClientCommands);
            socket.RegisterMessage(typeof(ServerSessionContainer), m_EmptyServerSession);
            socket.RegisterMessage(typeof(DebugClientView), m_EmptyDebugClientView);
            socket.RegisterMessage(typeof(PingCheckComponent));
        }

        protected static void ZeroCommand(Container command)
        {
            command.Require<MouseComponent>().Zero();
            command.Require<CameraComponent>().Zero();
            command.Require<InputFlagProperty>().Zero();
            command.Require<WantedItemIndexProperty>().Zero();
        }

        protected SessionSettingsComponent GetSettings(Container session = null) => (session ?? GetLatestSession()).Require<SessionSettingsComponent>();

        /// <param name="session">If null, return settings from most recent history. Else get from specified session.</param>
        public override ModeBase GetMode(Container session = null) => ModeManager.GetMode(GetSettings(session).modeId);

        public override Container GetPlayerFromId(int playerId) => GetLatestSession().GetPlayer(playerId);

        public override Container GetLatestSession() => m_SessionHistory.Peek();

        protected override void Render(float renderTime)
        {
            foreach (InterfaceBehaviorBase @interface in m_Interfaces)
            {
                if (@interface is SessionInterfaceBehavior sessionInterface)
                    sessionInterface.Render(GetLatestSession());
            }
            float rollback = GetSettings().TickInterval;
            var renderEntities = m_RenderSession.Require<EntityArrayProperty>();
            for (var i = 0; i < renderEntities.Length; i++)
            {
                int index = i;
                RenderInterpolated(renderTime - rollback, renderEntities[index], m_SessionHistory.Size,
                                   h => m_SessionHistory.Get(-h).Require<ServerStampComponent>(),
                                   h => m_SessionHistory.Get(-h).Require<EntityArrayProperty>()[index]);
            }
            EntityManager.Render(renderEntities);
        }

        protected abstract void RollbackHitboxes(int playerId);

        public sealed override void AboutToRaycast(int playerId)
        {
            RollbackHitboxes(playerId);
            base.AboutToRaycast(playerId);
        }

        public virtual void Disconnect()
        {
            if (!IsDisposed) Dispose();
        }

        protected static T NewSession<T>(IEnumerable<Type> sessionElements, IEnumerable<Type> playerElements) where T : Container, new()
        {
            var session = new T();
            session.Add(sessionElements);
            session.Require<PlayerContainerArrayProperty>().SetAll(() => new Container(playerElements));
            // TODO:refactor standard entity components
            session.Require<EntityArrayProperty>().SetAll(() => new EntityContainer(typeof(ThrowableComponent)).Zero());
            session.ZeroIfHas<KillFeedProperty>();
            return session;
        }
    }

    internal static class NetworkSessionExtensions
    {
        internal static Container GetPlayer(this Container session, int index) => session.Require<PlayerContainerArrayProperty>()[index];
    }
}