using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Components.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using UnityEditor;

namespace Swihoni.Sessions
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class NeverCompress : Attribute
    {
    }

    public abstract class NetworkedSessionBase : SessionBase
    {
        protected const int HistoryCount = 250;
        protected const byte ClientCommandsCode = 0, ServerSessionCode = 1, DebugClientViewCode = 2;

        protected readonly CyclicArray<ServerSessionContainer> m_SessionHistory;
        protected readonly ClientCommandsContainer m_EmptyClientCommands;
        protected readonly ServerSessionContainer m_EmptyServerSession;
        protected readonly DebugClientView m_EmptyDebugClientView;
        protected readonly Container m_RollbackSession, m_RenderSession;

        public int ResetErrors { get; protected set; }
        public IPEndPoint IpEndPoint { get; }
        public abstract ComponentSocketBase Socket { get; }

        private static bool _isInGame;
        protected static int _indexer;
        protected static CyclicArray<ServerSessionContainer> _serverHistory;

        protected NetworkedSessionBase(SessionElements elements, IPEndPoint ipEndPoint, SessionInjectorBase injector) : base(elements, injector)
        {
            IpEndPoint = ipEndPoint;
            IpEndPoint = ipEndPoint;
            // Type[] prefixElements = {typeof(ClientStampComponent), typeof(AcknowledgedServerTickProperty)};
            // IReadOnlyCollection<Type> sharedPlayerElements = prefixElements.Concat(elements.playerElements)
            //                                                                .Append(typeof(ServerStampComponent)).ToArray(),
            //                           clientCommandElements = prefixElements.Concat(elements.playerElements)
            //                                                                 .Concat(elements.commandElements).ToArray();
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
            m_RenderSession = NewSession<Container>(elements.elements.Append(typeof(SpectatingPlayerId)), elements.playerElements);

            m_EmptyClientCommands = new ClientCommandsContainer(clientCommandElements);
            m_EmptyServerSession = ServerSessionContainerConstructor();
            m_EmptyDebugClientView = new DebugClientView(sharedPlayerElements);
        }

        private static T NewSession<T>(IEnumerable<Type> sessionElements, IEnumerable<Type> playerElements) where T : Container, new()
        {
            var session = new T();
            session.RegisterAppend(sessionElements);
            session.Require<PlayerContainerArrayElement>().SetAll(() => new Container(playerElements));
            // TODO:refactor standard entity components
            session.Require<EntityArrayElement>().SetAll(() => new EntityContainer(typeof(ThrowableComponent), typeof(ItemComponent)));
            session.ZeroIfWith<KillFeedElement>();
            return session;
        }

        protected static void RegisterMessages(ComponentSocketBase socket)
        {
            socket.Register(typeof(ClientCommandsContainer), ClientCommandsCode, ClientCommandsCode);
            socket.Register(typeof(ServerSessionContainer), ServerSessionCode, ServerSessionCode);
            socket.Register(typeof(DebugClientView), DebugClientViewCode, DebugClientViewCode);
        }

        protected static void SetFirstCommand(Container command)
        {
            command.Require<MouseComponent>().Zero();
            command.Require<CameraComponent>().Zero();
            command.Require<InputFlagProperty>().Zero();
            command.Require<WantedItemIndexProperty>().Zero();
        }

        public override Container GetModifyingPlayerFromId(int playerId, Container session = null) => (session ?? GetLatestSession()).GetPlayer(playerId);

        public override Container GetLatestSession() => m_SessionHistory.Peek();

        protected override void Render(uint renderTimeUs)
        {
            var context = new SessionContext(this, m_RenderSession, timeUs: renderTimeUs);
            RenderInterfaces(context);
            ModeManager.GetMode(m_RenderSession).Render(context);
        }

        protected void RenderInterfaces(SessionContext context)
        {
            _context = context;
            _isInGame = !IsLoading;
            ForEachSessionInterface(sessionInterface =>
            {
                if (!sessionInterface.IsDuringGame || _isInGame)
                    sessionInterface.Render(_context);
            });
        }

        protected void RenderEntities<TStampComponent>(uint currentRenderTimeUs, uint rollbackUs) where TStampComponent : StampComponent
        {
            _serverHistory = m_SessionHistory; // Prevent allocation in closure
            uint renderTimeUs = currentRenderTimeUs - rollbackUs;
            var renderEntities = m_RenderSession.Require<EntityArrayElement>();
            for (var index = 0; index < renderEntities.Length; index++)
            {
                _indexer = index;
                RenderInterpolated(renderTimeUs, renderEntities[_indexer], _serverHistory.Size,
                                   h => _serverHistory.Get(-h).Require<TStampComponent>(),
                                   h => _serverHistory.Get(-h).Require<EntityArrayElement>()[_indexer]);
            }
            EntityManager.RenderAll(renderEntities, (visual, entity) => ((EntityVisualBehavior) visual).Render(entity));
        }

        protected abstract void RollbackHitboxes(in SessionContext context);

        public sealed override void RollbackHitboxesFor(in SessionContext context)
        {
            RollbackHitboxes(context);
            base.RollbackHitboxesFor(context);
        }
    }
}