using System;
using Components;
using Session.Items.Modifiers;
using Session.Player;
using Session.Player.Components;
using Session.Player.Modifiers;

namespace Session
{
    public abstract class HostBase<TSessionComponent>
        : ServerBase<TSessionComponent>
        where TSessionComponent : SessionComponentBase
    {
        private const int HostPlayerId = 0;

        private readonly PlayerCommandsComponent m_HostCommands = new PlayerCommandsComponent();
        private readonly PlayerComponent m_HostPlayerComponent = new PlayerComponent();
        private readonly TSessionComponent m_RenderSessionComponent = Activator.CreateInstance<TSessionComponent>();

        private float m_RenderTime;

        private void ReadLocalInputs()
        {
            PlayerManager.Singleton.ModifyCommands(HostPlayerId, m_HostCommands);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs();
            m_HostCommands.duration.Value = delta;
            PlayerManager.Singleton.ModifyTrusted(HostPlayerId, m_HostPlayerComponent, m_HostCommands);
            PlayerManager.Singleton.ModifyChecked(HostPlayerId, m_HostPlayerComponent, m_HostCommands);
        }

        protected override void Render(float renderDelta, float timeSinceTick)
        {
            m_RenderSessionComponent.localPlayerId.Value = HostPlayerId;
            PlayerComponent localPlayerRenderComponent = m_RenderSessionComponent.playerComponents[HostPlayerId];
            Copier.CopyTo(m_HostPlayerComponent, localPlayerRenderComponent);
            PlayerManager.Singleton.Visualize(m_RenderSessionComponent);
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            if (tick == 0)
            {
                m_HostPlayerComponent.health.Value = 100;
                PlayerItemManagerModiferBehavior.SetItemAtIndex(m_HostPlayerComponent.inventory, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(m_HostPlayerComponent.inventory, ItemId.TestingRifle, 2);
            }
        }
    }
}