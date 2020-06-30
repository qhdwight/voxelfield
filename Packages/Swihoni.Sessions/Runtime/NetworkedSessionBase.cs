using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Steamworks;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Components.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;

namespace Swihoni.Sessions
{
    public abstract class NetworkedSessionBase : SessionBase
    {
        protected const int HistoryCount = 250;

        protected readonly CyclicArray<ServerSessionContainer> m_SessionHistory;
        protected readonly ClientCommandsContainer m_EmptyClientCommands;
        protected readonly ServerSessionContainer m_EmptyServerSession;
        protected readonly DebugClientView m_EmptyDebugClientView;
        protected readonly Container m_RollbackSession, m_RenderSession;

        public int ResetErrors { get; protected set; }
        public IPEndPoint IpEndPoint { get; }
        public abstract ComponentSocketBase Socket { get; }

        protected NetworkedSessionBase(SessionElements elements, IPEndPoint ipEndPoint, SessionInjectorBase injector) : base(elements, injector)
        {
            IpEndPoint = ipEndPoint;
            IReadOnlyCollection<Type> sharedPlayerElements = elements.playerElements
                                                                     .Append(typeof(ClientStampComponent))
                                                                     .Append(typeof(AcknowledgedServerTickProperty))
                                                                     .Append(typeof(ServerStampComponent)).ToArray(),
                                      clientCommandElements = elements.playerElements
                                                                      .Append(typeof(ClientStampComponent))
                                                                      .Append(typeof(AcknowledgedServerTickProperty))
                                                                      .Concat(elements.commandElements).ToArray();
            ServerSessionContainer ServerSessionContainerConstructor() =>
                NewSession<ServerSessionContainer>(elements.elements.Append(typeof(ServerStampComponent)), sharedPlayerElements);
            m_SessionHistory = new CyclicArray<ServerSessionContainer>(HistoryCount, ServerSessionContainerConstructor);

            m_RollbackSession = NewSession<Container>(elements.elements, elements.playerElements);
            m_RenderSession = NewSession<Container>(elements.elements, elements.playerElements);

            m_EmptyClientCommands = new ClientCommandsContainer(clientCommandElements);
            m_EmptyServerSession = ServerSessionContainerConstructor();
            m_EmptyDebugClientView = new DebugClientView(sharedPlayerElements);
        }

        protected void ForEachPlayer(Action<Container> action)
        {
            foreach (ServerSessionContainer serverSession in m_SessionHistory)
            foreach (Container player in serverSession.Require<PlayerContainerArrayElement>())
                action(player);
        }

        protected void RegisterMessages(ComponentSocketBase socket)
        {
            socket.RegisterContainer(typeof(ClientCommandsContainer), m_EmptyClientCommands, 0);
            socket.RegisterContainer(typeof(ServerSessionContainer), m_EmptyServerSession, 1);
            socket.RegisterContainer(typeof(DebugClientView), m_EmptyDebugClientView, 2);
        }

        protected static void ZeroCommand(Container command)
        {
            command.Require<MouseComponent>().Zero();
            command.Require<CameraComponent>().Zero();
            command.Require<InputFlagProperty>().Zero();
            command.Require<WantedItemIndexProperty>().Zero();
            command.Require<UsernameProperty>().SetTo(SteamClient.IsValid ? SteamClient.Name : "Client");
        }

        /// <param name="session">If null, return settings from most recent history. Else get from specified session.</param>
        public override ModeBase GetMode(Container session = null) => ModeManager.GetMode((session ?? GetLatestSession()).Require<ModeIdProperty>());

        public override Container GetPlayerFromId(int playerId, Container session = null) => (session ?? GetLatestSession()).GetPlayer(playerId);

        public override Container GetLatestSession() => m_SessionHistory.Peek();

        protected override void Render(uint renderTimeUs)
        {
            Container latestSession = GetLatestSession();
            RenderInterfaces(latestSession);
            GetMode(m_RenderSession).Render(m_RenderSession);
        }

        protected void RenderInterfaces(Container session) => ForEachSessionInterface(sessionInterface => sessionInterface.Render(this, session));

        protected void RenderEntities<TStampComponent>(uint currentRenderTimeUs, uint rollbackUs) where TStampComponent : StampComponent
        {
            uint renderTimeUs = currentRenderTimeUs - rollbackUs;
            var renderEntities = m_RenderSession.Require<EntityArrayElement>();
            for (var i = 0; i < renderEntities.Length; i++)
            {
                int index = i;
                RenderInterpolated(renderTimeUs, renderEntities[index], m_SessionHistory.Size,
                                   h => m_SessionHistory.Get(-h).Require<TStampComponent>(),
                                   h => m_SessionHistory.Get(-h).Require<EntityArrayElement>()[index]);
            }
            EntityManager.RenderAll(renderEntities, (visual, entity) => ((EntityVisualBehavior) visual).Render(entity));
        }

        protected abstract void RollbackHitboxes(int playerId);

        public sealed override void RollbackHitboxesFor(int playerId)
        {
            RollbackHitboxes(playerId);
            base.RollbackHitboxesFor(playerId);
        }

        public virtual void Disconnect()
        {
            if (!IsDisposed) Dispose();
        }

        protected static T NewSession<T>(IEnumerable<Type> sessionElements, IEnumerable<Type> playerElements) where T : Container, new()
        {
            var session = new T();
            session.RegisterAppend(sessionElements);
            session.Require<PlayerContainerArrayElement>().SetAll(() => new Container(playerElements));
            // TODO:refactor standard entity components
            session.Require<EntityArrayElement>().SetAll(() => new EntityContainer(typeof(ThrowableComponent)).Zero());
            session.ZeroIfWith<KillFeedElement>();
            return session;
        }
    }

    internal static class NetworkSessionExtensions
    {
        internal static Container GetPlayer(this Container session, int index) => session.Require<PlayerContainerArrayElement>()[index];
    }
}