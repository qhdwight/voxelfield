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
using Swihoni.Sessions.Player.Components;

namespace Swihoni.Sessions
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class NeverCompress : Attribute
    {
    }

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

        public override Container GetModifyingPayerFromId(int playerId, Container session = null) => (session ?? GetLatestSession()).GetPlayer(playerId);

        public override Container GetLatestSession() => m_SessionHistory.Peek();

        protected override void Render(uint renderTimeUs)
        {
            Container latestSession = GetLatestSession();
            RenderInterfaces(latestSession);
            GetMode(m_RenderSession).Render(this, m_RenderSession);
        }

        protected void RenderInterfaces(Container session)
        {
            _session = this;
            _container = session;
            ForEachSessionInterface(sessionInterface => sessionInterface.Render(_session, _container));
        }

        protected static SessionBase _session;
        protected static Container _container;
        private static CyclicArray<ServerSessionContainer> _history;
        protected static int _int;
        
        protected void RenderEntities<TStampComponent>(uint currentRenderTimeUs, uint rollbackUs) where TStampComponent : StampComponent
        {
            _history = m_SessionHistory; // Prevent allocation in closure
            uint renderTimeUs = currentRenderTimeUs - rollbackUs;
            var renderEntities = m_RenderSession.Require<EntityArrayElement>();
            for (var index = 0; index < renderEntities.Length; index++)
            {
                _int = index;
                RenderInterpolated(renderTimeUs, renderEntities[_int], _history.Size,
                                   h => _history.Get(-h).Require<TStampComponent>(),
                                   h => _history.Get(-h).Require<EntityArrayElement>()[_int]);
            }
            EntityManager.RenderAll(renderEntities, (visual, entity) => ((EntityVisualBehavior) visual).Render(entity));
        }

        protected abstract void RollbackHitboxes(int playerId);

        public sealed override void RollbackHitboxesFor(int playerId)
        {
            RollbackHitboxes(playerId);
            base.RollbackHitboxesFor(playerId);
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