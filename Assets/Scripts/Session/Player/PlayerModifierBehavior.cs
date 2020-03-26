using UnityEngine;

namespace Session.Player
{
    [RequireComponent(typeof(PlayerCamera), typeof(PlayerMovement))]
    public class PlayerModifierBehavior : MonoBehaviour
    {
        private PlayerCamera m_Camera;
        private PlayerMovement m_Movement;

        public PlayerCamera Camera => m_Camera;

        public void Setup()
        {
            m_Camera = GetComponent<PlayerCamera>();
            m_Movement = GetComponent<PlayerMovement>();
            m_Movement.Setup();
        }

        public void Modify(PlayerState state, PlayerCommands commands)
        {
            m_Camera.Modify(state, commands);
            m_Movement.Modify(state, commands);
        }
    }
}