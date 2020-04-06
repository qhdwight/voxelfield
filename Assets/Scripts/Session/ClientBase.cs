using System;
using System.Net;
using Collections;
using Components;
using Networking;
using Session.Items.Modifiers;
using Session.Player;
using Session.Player.Components;
using Session.Player.Modifiers;
using UnityEngine;

namespace Session
{
    public abstract class ClientBase<TSessionComponent>
        : SessionBase<TSessionComponent>
        where TSessionComponent : SessionComponentBase
    {
        private const int LocalPlayerId = 0;

        private readonly PlayerCommandsComponent m_LocalCommands = new PlayerCommandsComponent();
        private readonly CyclicArray<StampedPlayerComponent> m_PredictedPlayerComponents =
            new CyclicArray<StampedPlayerComponent>(250, () => new StampedPlayerComponent());
        private readonly TSessionComponent m_RenderSessionComponent = Activator.CreateInstance<TSessionComponent>();
        private readonly PlayerComponent m_TrustedPlayerComponent = new PlayerComponent();
        private ComponentClientSocket m_Socket;

        public override void Start()
        {
            m_Socket = new ComponentClientSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777), TypeToId);
            m_Socket.StartReceiving();
        }

        private void ReadLocalInputs()
        {
            PlayerManager.Singleton.ModifyCommands(LocalPlayerId, m_LocalCommands);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs();
            PlayerManager.Singleton.ModifyTrusted(LocalPlayerId, m_TrustedPlayerComponent, m_LocalCommands);
        }

        protected override void Render(float renderDelta, float timeSinceTick)
        {
            m_RenderSessionComponent.localPlayerId.Value = LocalPlayerId;
            PlayerComponent localPlayerRenderComponent = m_RenderSessionComponent.playerComponents[LocalPlayerId];
            // InterpolateHistoryInto(localPlayerRenderComponent, m_PredictedPlayerComponents, 1.0f / m_Settings.tickRate * 1.2f, timeSinceTick);
            InterpolateHistoryInto(localPlayerRenderComponent, m_PredictedPlayerComponents, DebugBehavior.Singleton.Rollback, timeSinceTick);
            Copier.CopyTo(m_TrustedPlayerComponent, localPlayerRenderComponent);
            Copier.CopyTo(DebugBehavior.Singleton.RenderOverride, localPlayerRenderComponent);
            PlayerManager.Singleton.Visualize(m_RenderSessionComponent);
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);

            ReadLocalInputs();

            StampedPlayerComponent lastPredictedPlayerComponent = m_PredictedPlayerComponents.Peek();
            float lastTickTime = lastPredictedPlayerComponent.time.OrElse(time);
            StampedPlayerComponent predictedPlayerComponent = m_PredictedPlayerComponents.ClaimNext();
            Copier.CopyTo(lastPredictedPlayerComponent, predictedPlayerComponent);
            predictedPlayerComponent.tick.Value = m_Tick;
            predictedPlayerComponent.time.Value = time;
            float duration = time - lastTickTime;
            predictedPlayerComponent.duration.Value = duration;

            if (tick == 0)
            {
                predictedPlayerComponent.component.health.Value = 100;
                PlayerItemManagerModiferBehavior.SetItemAtIndex(predictedPlayerComponent.component.inventory, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(predictedPlayerComponent.component.inventory, ItemId.TestingRifle, 2);
            }

            m_Socket.SendToServer(new PingCheckComponent {tick = new UIntProperty(tick)});

            Copier.CopyTo(m_TrustedPlayerComponent, predictedPlayerComponent.component);
            m_LocalCommands.duration.Value = duration;
            PlayerManager.Singleton.ModifyChecked(LocalPlayerId, predictedPlayerComponent.component, m_LocalCommands);

            DebugBehavior.Singleton.Current = predictedPlayerComponent.component;
        }
    }
}