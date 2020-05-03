using Input;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;

namespace Swihoni.Sessions.Interfaces
{
    public class ScoreboardInterface : ArrayViewerInterfaceBase<ScoreboardEntryInterface, PlayerContainerArrayProperty, Container>
    {
        private void Update()
        {
            // if (ConsoleInterface.Singleton.IsActive || !GameManager.Singleton.IsInGame) return;
            InputProvider inputs = InputProvider.Singleton;
            SetInterfaceActive(inputs.GetInput(InputType.OpenScoreboard));
        }

        protected override bool Less(Container e1, Container e2)
        {
            if (e1.Without(out StatsComponent s1) || s1.kills.WithoutValue) return true;
            if (e2.Without(out StatsComponent s2) || s2.kills.WithoutValue) return false;
            return s1.kills < s2.kills;
        }
    }
}