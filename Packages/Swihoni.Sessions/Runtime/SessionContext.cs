using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions
{
    public readonly struct SessionContext
    {
        public readonly SessionBase session;
        public readonly Container entity, sessionContainer, commands;
        public readonly int playerId;
        public readonly Container player;
        public readonly uint timeUs, durationUs;
        public readonly int tickDelta;

        public SessionContext(SessionBase session = null, Container sessionContainer = null, Container commands = null,
                              int? playerId = null, Container player = null,
                              Container entity = null,
                              uint? timeUs = null, uint? durationUs = null, int? tickDelta = null, in SessionContext? existing = null)
        {
            if (existing is SessionContext context)
            {
                this.session = session ?? context.session;
                this.entity = entity ?? context.entity;
                this.sessionContainer = sessionContainer ?? context.sessionContainer;
                this.commands = commands ?? context.commands;
                this.playerId = playerId ?? context.playerId;
                this.player = player ?? context.player;
                this.timeUs = timeUs ?? context.timeUs;
                this.durationUs = durationUs ?? context.durationUs;
                this.tickDelta = tickDelta ?? context.tickDelta;
            }
            else
            {
                this.session = session;
                this.entity = entity;
                this.sessionContainer = sessionContainer;
                this.commands = commands;
                this.playerId = playerId.GetValueOrDefault();
                this.player = player;
                this.timeUs = timeUs.GetValueOrDefault();
                this.durationUs = durationUs.GetValueOrDefault();
                this.tickDelta = tickDelta.GetValueOrDefault();
            }
        }

        public Color TeamColor => Mode.GetTeamColor(player.Require<TeamProperty>());

        public ModeBase Mode => ModeManager.GetMode(sessionContainer.Require<ModeIdProperty>());

        public Container ModifyingPlayer => session.GetModifyingPayerFromId(playerId, sessionContainer);

        public Container GetModifyingPlayer(int otherPlayerId) => session.GetModifyingPayerFromId(otherPlayerId, sessionContainer);

        public Container GetPlayer(int otherPlayerId) => sessionContainer.GetPlayer(otherPlayerId);
        
        public bool IsValidLocalPlayer(out Container localPlayer, out byte localPlayerId, bool needsToBeAlive = true)
        {
            var localPlayerIdProperty = sessionContainer.Require<LocalPlayerId>();
            if (localPlayerIdProperty.WithoutValue)
            {
                localPlayer = default;
                localPlayerId = default;
                return false;
            }
            localPlayerId = localPlayerIdProperty;
            localPlayer = GetModifyingPlayer(localPlayerId);
            return !needsToBeAlive || localPlayer.Require<HealthProperty>().IsActiveAndAlive;
        }
    }
}