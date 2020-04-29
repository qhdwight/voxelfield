using Input;
using Swihoni.Components;
using Swihoni.Sessions.Components;

namespace Swihoni.Sessions.Interfaces
{
    public class ScoreboardInterface : SessionInterfaceBehavior
    {
        protected override void Awake()
        {
            
        }

        private void Update()
        {
            // if (ConsoleInterface.Singleton.IsActive || !GameManager.Singleton.IsInGame) return;
            InputProvider inputs = InputProvider.Singleton;
            SetInterfaceActive(inputs.GetInput(InputType.OpenScoreboard));
        }

        public override void Render(Container session)
        {
            if (session.Without(out PlayerContainerArrayProperty players)) return;

            for (var i = 0; i < players.Length; i++)
            {
                Container player = players[i];
                
            }
        }
    }
}