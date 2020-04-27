using UnityEngine;

namespace Swihoni.Sessions.Player
{
    public class PlayerHitbox : MonoBehaviour
    {
        [SerializeField] private float m_DamageMultiplier = 1.0f;

        public float DamageMultiplier => m_DamageMultiplier;

        public PlayerHitboxManager Manager { get; private set; }

        public void Setup(PlayerHitboxManager manager) { Manager = manager; }
    }
}