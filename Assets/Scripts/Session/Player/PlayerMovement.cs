using Compound;

namespace Session.Player
{
    public static class PlayerMovement
    {
        public static void Move(PlayerData player)
        {
            player.position.z += InputProvider.Singleton.GetAxis(InputType.Forward, InputType.Backward);
        }
    }
}