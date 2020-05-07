using UnityEngine;

namespace Swihoni.Sessions.Player
{
    public class PlayerTrigger : MonoBehaviour
    {
        public int PlayerId { get; private set; }

        public void Setup(int playerId) { PlayerId = playerId; }
    }
}