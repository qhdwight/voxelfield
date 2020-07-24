using UnityEngine;

namespace Swihoni.Sessions.Player
{
    [RequireComponent(typeof(Collider))]
    public class PlayerTrigger : MonoBehaviour
    {
        public int PlayerId { get; private set; }

        public void Setup(int playerId) => PlayerId = playerId;
    }
}