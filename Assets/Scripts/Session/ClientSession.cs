using Compound;
using Session.Player;

namespace Session
{
    public class ClientSession : SessionBase
    {
        private readonly PlayerCommands m_LocalCommands = new PlayerCommands();

        private void ReadLocalInputs()
        {
            InputProvider input = InputProvider.Singleton;
            m_LocalCommands.hInput = input.GetAxis(InputType.Right, InputType.Left);
            m_LocalCommands.vInput = input.GetAxis(InputType.Forward, InputType.Backward);
            m_LocalCommands.jumpInput = input.GetInput(InputType.Jump);
            m_LocalCommands.mouseX = InputProvider.GetMouseInput(MouseMovement.X);
            m_LocalCommands.mouseY = InputProvider.GetMouseInput(MouseMovement.Y);
        }

        public override void HandleInput()
        {
        }

        public override void Render()
        {
            base.Render();
        }

        public override void Tick()
        {
            base.Tick();
            ReadLocalInputs();
        }
    }
}