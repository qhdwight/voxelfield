using Input;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class ScoreboardInterfaceBase<T> : ArrayViewerInterfaceBase<T, PlayerContainerArrayElement, Container>
        where T : ElementInterfaceBase<Container>
    {
        public override void Render(SessionBase session, Container sessionContainer)
        {
            SetInterfaceActive(InputProvider.Singleton.GetInput(InputType.OpenScoreboard));
            base.Render(session, sessionContainer);
        }

        protected override bool Less(Container e1, Container e2)
        {
            if (e1.Without(out StatsComponent s1) || s1.kills.WithoutValue) return true;
            if (e2.Without(out StatsComponent s2) || s2.kills.WithoutValue) return false;
            return s1.kills < s2.kills;
        }
    }
}