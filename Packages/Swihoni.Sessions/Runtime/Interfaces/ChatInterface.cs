using Swihoni.Components;
using Swihoni.Sessions.Config;

namespace Swihoni.Sessions.Interfaces
{
    public class ChatInterface : SessionInterfaceBehavior
    {
        public override void Render(SessionBase session, Container sessionContainer)
        {
            if (NoInterrupting && InputProvider.GetInputDown(InputType.ToggleChat)) ToggleInterfaceActive();
        }
    }
}