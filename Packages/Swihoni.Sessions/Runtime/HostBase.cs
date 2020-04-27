using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions
{
    public abstract class HostBase : ServerBase
    {
        private const int HostPlayerId = 0;

        private readonly Container m_HostCommands, m_RenderSession;

        protected HostBase(IGameObjectLinker linker,
                           IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            // TODO:refactor zeroing
            m_HostCommands = new Container(commandElements.Concat(playerElements).Append(typeof(ServerStampComponent)).Append(typeof(ServerTag)));
            m_HostCommands.Zero();
            m_HostCommands.Require<ServerStampComponent>().Reset();

            m_RenderSession = new Container(sessionElements);
            if (m_RenderSession.Has(out PlayerContainerArrayProperty players))
                players.SetAll(() => new Container(playerElements));
        }

        private void ReadLocalInputs(SessionBase session, Container commandsToFill) { m_Modifier[HostPlayerId].ModifyCommands(this, commandsToFill); }

        protected override void Input(float time, float delta)
        {
            if (!m_SessionHistory.Peek().Has(out ServerStampComponent serverStamp) || !serverStamp.tick.HasValue)
                return;

            ReadLocalInputs(this, m_HostCommands);
            m_Modifier[HostPlayerId].ModifyTrusted(this, HostPlayerId, m_HostCommands, m_HostCommands, delta);
            m_Modifier[HostPlayerId].ModifyChecked(this, HostPlayerId, m_HostCommands, m_HostCommands, delta);
            GetMode().Modify(m_HostCommands, m_HostCommands, delta);
            var stamp = m_HostCommands.Require<ServerStampComponent>();
            stamp.time.Value = time;
            stamp.tick.Value = serverStamp.tick;
        }

        protected override void Render(float renderTime)
        {
            if (m_RenderSession.Without(out PlayerContainerArrayProperty renderPlayers)
             || m_RenderSession.Without(out LocalPlayerProperty localPlayer)) return;

            localPlayer.Value = HostPlayerId;
            for (var playerId = 0; playerId < renderPlayers.Length; playerId++)
            {
                if (playerId == localPlayer)
                {
                    // Inject host player component
                    renderPlayers[playerId].CopyFrom(m_HostCommands);
                }
                else
                {
                    int copiedPlayerId = playerId;
                    Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).Require<PlayerContainerArrayProperty>()[copiedPlayerId];

                    SessionSettingsComponent settings = GetSettings();
                    float rollback = DebugBehavior.Singleton.RollbackOverride.OrElse(settings.TickInterval) * 3;

                    RenderInterpolatedPlayer<ServerStampComponent>(renderTime - rollback, renderPlayers[playerId],
                                                                   m_SessionHistory.Size, GetInHistory);
                }
                m_Visuals[playerId].Render(playerId, renderPlayers[playerId], playerId == localPlayer);
            }
            m_PlayerHud.Render(renderPlayers[HostPlayerId]);
        }

        protected override void PreTick(Container tickSession)
        {
            Container hostPlayer = tickSession.Require<PlayerContainerArrayProperty>()[HostPlayerId];
            // Inject our current player component before normal update cycle
            hostPlayer.MergeSet(m_HostCommands);
            // Set up new player component data
            if (tickSession.Require<ServerStampComponent>().tick == 0u) SetupNewPlayer(tickSession, hostPlayer);
        }

        protected override void PostTick(Container tickSession)
        {
            // Merge host component updates that happen on normal server update cycle
            m_HostCommands.MergeSet(tickSession.Require<PlayerContainerArrayProperty>()[HostPlayerId]);
        }

        public override Ray GetRayForPlayer(int holdingPlayer)
        {
            if (holdingPlayer == HostPlayerId)
            {
                var camera = m_HostCommands.Require<CameraComponent>();
                float yaw = camera.yaw * Mathf.Deg2Rad, pitch = camera.pitch * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Sin(yaw), -Mathf.Sin(pitch), Mathf.Cos(yaw));
                Vector3 position = m_HostCommands.Require<MoveComponent>().position + new Vector3 {y = 1.8f};
                var ray = new Ray(position, direction);
                return ray;
            }
            return base.GetRayForPlayer(holdingPlayer);
        }
    }
}