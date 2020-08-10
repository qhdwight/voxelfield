using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Player.Components;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class ScoreboardInterfaceBase<T> : ArrayViewerInterfaceBase<T, PlayerContainerArrayElement, Container>
        where T : ElementInterfaceBase<Container>
    {
        public override void Render(in SessionContext context)
        {
            if (context.Mode.ShowScoreboard(context))
            {
                m_InterruptsCommands = true;
                m_NeedsCursor = true;
                SetInterfaceActive(true);
            }
            else
            {
                m_InterruptsCommands = false;
                m_NeedsCursor = false;
                SetInterfaceActive(!SessionBase.InterruptingInterface && InputProvider.GetInput(InputType.OpenScoreboard));
            }
            base.Render(context);
        }

        protected override int Compare(Container e1, Container e2)
        {
            if (e1.Without(out StatsComponent s1) || s1.kills.WithoutValue) return -1;
            if (e2.Without(out StatsComponent s2) || s2.kills.WithoutValue) return 1;
            return s1.kills.Value.CompareTo(s2.kills.Value);
        }
    }
}