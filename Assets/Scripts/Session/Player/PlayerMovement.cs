using Compound;
using UnityEngine;

namespace Session.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour, IPlayerModifier
    {
        private CharacterController m_Controller;

        private void Awake()
        {
            m_Controller = GetComponent<CharacterController>();
        }

        public void Modify(PlayerData data, PlayerCommands commands)
        {
            m_Controller.Move(new Vector3 {z = InputProvider.Singleton.GetAxis(InputType.Forward, InputType.Backward)});
            data.position = transform.position;
        }
    }
}